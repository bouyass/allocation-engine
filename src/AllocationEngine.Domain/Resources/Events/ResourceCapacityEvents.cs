using AllocationEngine.Domain.Events;
using AllocationEngine.Domain.ValueObjects;

namespace AllocationEngine.Domain.Resources.Events;

public sealed record CapacityAdded(
    ResourceId ResourceId,
    Quantity Quantity,
    Quantity NewCapacity,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record CapacityRemoved(
    ResourceId ResourceId,
    Quantity Quantity,
    Quantity NewCapacity,
    DateTimeOffset OccurredAt) : IDomainEvent;
