using AllocationEngine.Domain.Errors;
using AllocationEngine.Domain.Holds;
using AllocationEngine.Domain.Policies;
using AllocationEngine.Domain.Resources;
using AllocationEngine.Domain.ValueObjects;

public sealed class SequentialAllocationState
{
    private readonly Dictionary<ResourceId, Resource> _resources = [];
    private readonly Dictionary<HoldId, Hold> _holds = [];

    public void AddResource(Resource resource)
    {
        ArgumentNullException.ThrowIfNull(resource, nameof(resource));

        if (!_resources.TryAdd(resource.Id, resource))
        {
            throw new InvalidOperationException($"Resource with id {resource.Id} already exists.");
        }
    }

    public Resource GetResource(ResourceId resourceId)
    {

        if (!_resources.TryGetValue(resourceId, out var resource))
        {
            throw new KeyNotFoundException($"Resource with id {resourceId} not found.");
        }

        return resource;
    }

    internal void AddHold(Hold hold)
    {
        ArgumentNullException.ThrowIfNull(hold, nameof(hold));

        if (!_holds.TryAdd(hold.Id, hold))
        {
            throw new InvalidOperationException($"Hold with id {hold.Id} already exists.");
        }
    }

    public Hold GetHold(HoldId holdId)
    {

        if (!_holds.TryGetValue(holdId, out var hold))
        {
            throw new KeyNotFoundException($"Hold with id {holdId} not found.");
        }

        return hold;
    }

    public Hold Acquire(
    ResourceId resourceId,
    HoldId holdId,
    OwnerId ownerId,
    Quantity quantity,
    HoldTtl? requestedTtl,
    DateTimeOffset now)
    {
        if (!_resources.TryGetValue(resourceId, out var resource))
        {
            throw new KeyNotFoundException($"Resource with id {resourceId} not found.");
        }

        var (heldQuantity, activeHoldCount) = GetOwnerUsage(resourceId, ownerId);

        var policyResult = PolicyEvaluator.EvaluateAcquire(
            resource.Policies,
            quantity,
            heldQuantity,
            activeHoldCount,
            requestedTtl);

        if (!policyResult.IsAllowed)
        {
            throw new InvalidOperationException($"Acquire request rejected: {policyResult.Rejection}");
        }

        var effectiveTtl = PolicyEvaluator.ResolveTtl(
            resource.Policies.Hold,
            requestedTtl);

        if (quantity > resource.AvailableQuantity)
        {
            // Attempt to reclaim holds if the requested quantity exceeds available quantity
            var quantityMissing = quantity - resource.AvailableQuantity;
            ReclaimCapacityForAcquire(resourceId, ref quantityMissing, now);

            if (quantityMissing > Quantity.Zero)
            {
                throw new InsufficientCapacityException(quantity, resource.AvailableQuantity);
            }
        }

        resource.Reserve(quantity, now);

        Hold hold = new Hold(
            holdId,
            resourceId,
            ownerId,
            quantity,
            now,
            effectiveTtl.GetExpirationFrom(now),
            resource.PolicyVersion);

        AddHold(hold);
        return hold;
    }

    public void ConfirmHold(HoldId holdId, DateTimeOffset now)
    {
        if (!_holds.TryGetValue(holdId, out var hold))
        {
            throw new KeyNotFoundException($"Hold with id {holdId} not found.");
        }

        if (!_resources.TryGetValue(hold.ResourceId, out var resource))
        {
            throw new KeyNotFoundException($"Resource with id {hold.ResourceId} not found.");
        }

        if (hold.Status == HoldStatus.Confirmed)
        {
            return;
        }

        if (hold.Status == HoldStatus.Held)
        {
            hold.Confirm();
            resource.ConfirmReservation(hold.Quantity, now);
            return;
        }

        throw new InvalidOperationException($"Hold with id {holdId} is not in a held state and cannot be confirmed.");
    }

    public void ReleaseHold(HoldId holdId, DateTimeOffset now)
    {
        if (!_holds.TryGetValue(holdId, out var hold))
        {
            throw new KeyNotFoundException($"Hold with id {holdId} not found.");
        }

        if (!_resources.TryGetValue(hold.ResourceId, out var resource))
        {
            throw new KeyNotFoundException($"Resource with id {hold.ResourceId} not found.");
        }

        if (hold.Status == HoldStatus.Released || hold.Status == HoldStatus.Expired)
        {
            return;
        }

        hold.Release();
        resource.ReleaseReservation(hold.Quantity, now);
    }

    public void ReclaimExpiredHolds(DateTimeOffset now)
    {
        var expiredHolds = _holds.Values.Where(h => h.IsReclaimable(now)).ToList();

        foreach (var hold in expiredHolds)
        {
            if (!_resources.TryGetValue(hold.ResourceId, out var resource))
            {
                throw new KeyNotFoundException($"Resource with id {hold.ResourceId} not found.");
            }

            hold.Reclaim(now);
            resource.ReleaseReservation(hold.Quantity, now);
        }
    }

    public void AddCapacity(
        ResourceId resourceId,
        Quantity quantity,
        DateTimeOffset now)
    {
        var resource = GetResource(resourceId);
        resource.AddCapacity(quantity, now);
    }

    public void RemoveCapacity(
        ResourceId resourceId,
        Quantity quantity,
        DateTimeOffset now)
    {
        var resource = GetResource(resourceId);
        resource.RemoveCapacity(quantity, now);
    }

    public void PauseResource(
        ResourceId resourceId,
        DateTimeOffset now)
    {
        var resource = GetResource(resourceId);
        resource.Pause(now);
    }

    public void ResumeResource(
        ResourceId resourceId,
        DateTimeOffset now)
    {
        var resource = GetResource(resourceId);
        resource.Resume(now);
    }

    public void CloseResource(
        ResourceId resourceId,
        DateTimeOffset now)
    {
        var resource = GetResource(resourceId);
        resource.Close(now);
    }

    public void UpdateResourcePolicies(ResourceId resourceId, PolicySet policies, DateTimeOffset now)
    {
        GetResource(resourceId).UpdatePolicies(policies, now);
    }

    private void ReclaimCapacityForAcquire(ResourceId resourceId, ref Quantity quantityMissing, DateTimeOffset now)
    {
        var reclaimableHolds = _holds.Values
            .Where(h => h.ResourceId == resourceId && h.IsReclaimable(now))
            .ToList();

        var reclaimableQuantity = reclaimableHolds.Aggregate(Quantity.Zero, (sum, hold) => sum + hold.Quantity);

        if (reclaimableQuantity < quantityMissing)
        {
            return;
        }

        foreach (var hold in reclaimableHolds)
        {
            if (!_resources.TryGetValue(hold.ResourceId, out var resource))
            {
                throw new KeyNotFoundException($"Resource with id {hold.ResourceId} not found.");
            }

            hold.Reclaim(now);
            resource.ReclaimReservation(hold.Quantity, now);

            if (hold.Quantity >= quantityMissing)
            {
                quantityMissing = Quantity.Zero;
                break;
            }

            quantityMissing -= hold.Quantity;
        }
    }

    private (Quantity HeldQuantity, int ActiveHoldCount) GetOwnerUsage(
    ResourceId resourceId,
    OwnerId ownerId)
    {
        if (!_resources.TryGetValue(resourceId, out var resource))
        {
            throw new KeyNotFoundException($"Resource with id {resourceId} not found.");
        }

        var ownerHolds = _holds.Values.Where(h => h.ResourceId == resourceId && h.OwnerId == ownerId && h.Status == HoldStatus.Held);

        var heldQuantity = ownerHolds.Aggregate(Quantity.Zero, (sum, hold) => sum + hold.Quantity);
        var activeHoldCount = ownerHolds.Count();

        return (heldQuantity, activeHoldCount);
    }

}

