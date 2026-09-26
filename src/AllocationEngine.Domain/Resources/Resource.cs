using AllocationEngine.Domain.Errors;
using AllocationEngine.Domain.Policies;
using AllocationEngine.Domain.ValueObjects;

namespace AllocationEngine.Domain.Resources;

public sealed class Resource
{
    public ResourceId Id { get; }

    public Quantity Capacity { get; private set; }

    public Quantity HeldQuantity { get; private set; }

    public Quantity AllocatedQuantity { get; private set; }

    public Quantity AvailableQuantity
        => Capacity - HeldQuantity - AllocatedQuantity;

    public ResourceStatus Status { get; private set; }

    public PolicySet Policies { get; private set; }

    public PolicyVersion PolicyVersion { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Resource(
        ResourceId id,
        Quantity capacity,
        PolicyVersion policyVersion,
        PolicySet policies,
        DateTimeOffset createdAt)
    {
        Id = id;
        Capacity = capacity;
        PolicyVersion = policyVersion;
        Policies = policies;

        HeldQuantity = Quantity.Zero;
        AllocatedQuantity = Quantity.Zero;

        Status = ResourceStatus.Active;

        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public void Pause(DateTimeOffset now)
    {
        if (Status == ResourceStatus.Closed)
        {
            throw new InvalidResourceTransitionException(
                Status,
                nameof(Pause));
        }

        if (Status == ResourceStatus.Paused)
        {
            return;
        }

        Status = ResourceStatus.Paused;
        UpdatedAt = now;
    }

    public void Resume(DateTimeOffset now)
    {
        if (Status == ResourceStatus.Closed)
        {
            throw new InvalidResourceTransitionException(
                Status,
                nameof(Resume));
        }

        if (Status == ResourceStatus.Active)
        {
            return;
        }

        Status = ResourceStatus.Active;
        UpdatedAt = now;
    }

    public void Close(DateTimeOffset now)
    {
        if (Status == ResourceStatus.Closed)
        {
            return;
        }

        Status = ResourceStatus.Closed;
        UpdatedAt = now;
    }

    public void AddCapacity(
        Quantity quantity,
        DateTimeOffset now)
    {
        if (Status == ResourceStatus.Closed)
        {
            throw new InvalidResourceTransitionException(
                Status,
                nameof(AddCapacity));
        }

        if (quantity == Quantity.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity to add must be greater than zero.");
        }

        Capacity += quantity;
        UpdatedAt = now;
    }

    public void RemoveCapacity(
        Quantity quantity,
        DateTimeOffset now)
    {
        if (Status == ResourceStatus.Closed)
        {
            throw new InvalidResourceTransitionException(
                Status,
                nameof(RemoveCapacity));
        }

        if (quantity == Quantity.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity to remove must be greater than zero.");
        }

        var committedQuantity =
            HeldQuantity + AllocatedQuantity;

        if (quantity > Capacity)
        {
            throw new CapacityBelowCommittedException();
        }

        var newCapacity = Capacity - quantity;

        if (newCapacity < committedQuantity)
        {
            throw new CapacityBelowCommittedException();
        }

        Capacity = newCapacity;
        UpdatedAt = now;
    }

    public void Reserve(
        Quantity quantity,
        DateTimeOffset now)
    {
        if (Status != ResourceStatus.Active)
        {
            throw new InvalidResourceTransitionException(
                Status,
                nameof(Reserve));
        }

        if (quantity <= Quantity.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity to reserve must be greater than zero.");
        }

        if (quantity > AvailableQuantity)
        {
            throw new InsufficientCapacityException(
                quantity,
                AvailableQuantity);
        }

        HeldQuantity += quantity;
        UpdatedAt = now;
    }

    public void ConfirmReservation(
        Quantity quantity,
        DateTimeOffset now)
    {
        EnsurePositiveQuantity(quantity, nameof(quantity));

        if (quantity > HeldQuantity)
        {
            throw new InvalidOperationException(
                "Quantity to confirm exceeds held quantity.");
        }

        HeldQuantity -= quantity;
        AllocatedQuantity += quantity;
        UpdatedAt = now;
    }

    public void ReleaseReservation(
        Quantity quantity,
        DateTimeOffset now)
    {
        EnsurePositiveQuantity(quantity, nameof(quantity));

        if (quantity > HeldQuantity)
        {
            throw new InvalidOperationException(
                "Quantity to release exceeds held quantity.");
        }

        HeldQuantity -= quantity;
        UpdatedAt = now;
    }

    public void ReclaimReservation(
        Quantity quantity,
        DateTimeOffset now)
    {
        EnsurePositiveQuantity(quantity, nameof(quantity));

        if (quantity > HeldQuantity)
        {
            throw new InvalidOperationException(
                "Quantity to reclaim exceeds held quantity.");
        }

        HeldQuantity -= quantity;
        UpdatedAt = now;
    }


    public void UpdatePolicies(PolicySet policies, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(policies);

        if (Status == ResourceStatus.Closed)
        {
            throw new InvalidResourceTransitionException(Status, nameof(UpdatePolicies));
        }

        var expectedVersion = Policies.Version.Next();

        if (Policies.Version != expectedVersion)
        {
            throw new InvalidOperationException($"Policy version must {expectedVersion}, but was {Policies.Version}");
        }

        Policies = policies;
        UpdatedAt = now;
    }

    private void EnsurePositiveQuantity(
        Quantity quantity,
        string parameterName)
    {
        if (quantity <= Quantity.Zero)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Quantity must be greater than zero.");
        }
    }

}
