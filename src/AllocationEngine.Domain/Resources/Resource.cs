using AllocationEngine.Domain.Errors;
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

    public PolicyVersion PolicyVersion { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Resource(
        ResourceId id,
        Quantity capacity,
        PolicyVersion policyVersion,
        DateTimeOffset createdAt)
    {
        Id = id;
        Capacity = capacity;
        PolicyVersion = policyVersion;

        HeldQuantity = Quantity.Zero;
        AllocatedQuantity = Quantity.Zero;

        Status = ResourceStatus.Active;

        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public void Pause(DateTimeOffset now)
    {
        if (Status == ResourceStatus.Closed)
            throw new InvalidResourceTransitionException(
                Status,
                nameof(Pause));

        if (Status == ResourceStatus.Paused)
            return;

        Status = ResourceStatus.Paused;
        UpdatedAt = now;
    }

    public void Resume(DateTimeOffset now)
    {
        if (Status == ResourceStatus.Closed)
            throw new InvalidResourceTransitionException(
                Status,
                nameof(Resume));

        if (Status == ResourceStatus.Active)
            return;

        Status = ResourceStatus.Active;
        UpdatedAt = now;
    }

    public void Close(DateTimeOffset now)
    {
        if (Status == ResourceStatus.Closed)
            return;

        Status = ResourceStatus.Closed;
        UpdatedAt = now;
    }

    public void AddCapacity(
    Quantity quantity,
    DateTimeOffset now)
    {
        if (Status == ResourceStatus.Closed)
            throw new InvalidResourceTransitionException(
                Status,
                nameof(AddCapacity));

        if (quantity == Quantity.Zero)
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity to add must be greater than zero.");

        Capacity += quantity;
        UpdatedAt = now;
    }

    public void RemoveCapacity(
    Quantity quantity,
    DateTimeOffset now)
    {
        if (Status == ResourceStatus.Closed)
            throw new InvalidResourceTransitionException(
                Status,
                nameof(RemoveCapacity));

        if (quantity == Quantity.Zero)
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity to remove must be greater than zero.");

        var committedQuantity =
            HeldQuantity + AllocatedQuantity;

        if (quantity > Capacity)
            throw new CapacityBelowCommittedException();

        var newCapacity = Capacity - quantity;

        if (newCapacity < committedQuantity)
            throw new CapacityBelowCommittedException();

        Capacity = newCapacity;
        UpdatedAt = now;
    }
}