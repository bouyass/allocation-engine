using AllocationEngine.Domain.Errors;
using AllocationEngine.Domain.Events;
using AllocationEngine.Domain.Holds;
using AllocationEngine.Domain.Holds.Events;
using AllocationEngine.Domain.Policies;
using AllocationEngine.Domain.Policies.Events;
using AllocationEngine.Domain.Resources;
using AllocationEngine.Domain.Resources.Events;
using AllocationEngine.Domain.ValueObjects;

public sealed class SequentialAllocationState
{
    private readonly Dictionary<ResourceId, Resource> _resources = [];
    private readonly Dictionary<HoldId, Hold> _holds = [];

    private readonly Dictionary<IdempotencyKey, AcquireIdempotencyRecord> _acquireOperations = [];
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

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
    IdempotencyKey idempotencyKey,
    HoldTtl? requestedTtl,
    DateTimeOffset now)
    {

        var fingerprint = new AcquireFingerprint(
        resourceId,
        ownerId,
        quantity,
        requestedTtl);


        // idempotency 
        if (_acquireOperations.TryGetValue(idempotencyKey, out var existingRecord))
        {
            if (existingRecord.Fingerprint != fingerprint)
            {
                throw new InvalidOperationException(
                    $"Idempotency key '{idempotencyKey}' was already used with a different request."
                );
            }

            return GetHold(existingRecord.HoldId);
        }

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

        _acquireOperations.Add(
            idempotencyKey,
            new(idempotencyKey,
            fingerprint,
            holdId));

        _domainEvents.Add(
            new HoldCreated(
                hold.Id,
                hold.ResourceId,
                hold.OwnerId,
                hold.Quantity,
                now));

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
            _domainEvents.Add(
            new HoldConfirmed(
                hold.Id,
                hold.ResourceId,
                hold.OwnerId,
                hold.Quantity,
                now));

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
        _domainEvents.Add(
            new HoldReleased(
                hold.Id,
                hold.ResourceId,
                hold.OwnerId,
                hold.Quantity,
                now));
    }

    public void ReclaimExpiredHolds(DateTimeOffset now)
    {
        var expiredHolds = _holds.Values
            .Where(h => h.IsReclaimable(now))
            .ToList();

        foreach (var hold in expiredHolds)
        {
            if (!_resources.TryGetValue(hold.ResourceId, out var resource))
            {
                throw new KeyNotFoundException(
                    $"Resource with id {hold.ResourceId} not found.");
            }

            ReclaimHold(hold, resource, now);
        }
    }

    public void AddCapacity(
        ResourceId resourceId,
        Quantity quantity,
        DateTimeOffset now)
    {
        var resource = GetResource(resourceId);
        resource.AddCapacity(quantity, now);

        _domainEvents.Add(
        new CapacityAdded(
            resource.Id,
            quantity,
            resource.Capacity,
            now));
    }

    public void RemoveCapacity(
        ResourceId resourceId,
        Quantity quantity,
        DateTimeOffset now)
    {
        var resource = GetResource(resourceId);
        resource.RemoveCapacity(quantity, now);

        _domainEvents.Add(
        new CapacityRemoved(
            resource.Id,
            quantity,
            resource.Capacity,
            now));
    }

    public void PauseResource(
        ResourceId resourceId,
        DateTimeOffset now)
    {
        var resource = GetResource(resourceId);

        if (resource.Status == ResourceStatus.Paused)
        {
            return;
        }

        resource.Pause(now);
        _domainEvents.Add(
        new ResourcePaused(
            resource.Id,
            now));
    }

    public void ResumeResource(
        ResourceId resourceId,
        DateTimeOffset now)
    {
        var resource = GetResource(resourceId);

        if (resource.Status == ResourceStatus.Active)
        {
            return;
        }

        resource.Resume(now);

        _domainEvents.Add(
        new ResourceResumed(
            resource.Id,
            now));
    }

    public void CloseResource(
        ResourceId resourceId,
        DateTimeOffset now)
    {
        var resource = GetResource(resourceId);

        if (resource.Status == ResourceStatus.Closed)
        {
            return;
        }

        resource.Close(now);

        _domainEvents.Add(
        new ResourceClosed(
            resource.Id,
            now));
    }

    public void UpdateResourcePolicies(ResourceId resourceId, PolicySet policies, DateTimeOffset now)
    {
        var resource = GetResource(resourceId);
        var previousVersion = resource.PolicyVersion;
        resource.UpdatePolicies(policies, now);
        _domainEvents.Add(
        new PolicySetUpdated(
            resource.Id,
            previousVersion,
            resource.PolicyVersion,
            now));
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

            ReclaimHold(hold, resource, now);

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

    private void ReclaimHold(
        Hold hold,
        Resource resource,
        DateTimeOffset now)
    {
        hold.Reclaim(now);
        resource.ReclaimReservation(hold.Quantity, now);

        _domainEvents.Add(
            new HoldExpired(
                hold.Id,
                hold.ResourceId,
                hold.OwnerId,
                hold.Quantity,
                now));
    }
}

