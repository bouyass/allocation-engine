using AllocationEngine.Domain.Events;
using AllocationEngine.Domain.ValueObjects;

namespace AllocationEngine.Domain.Holds.Events;

public sealed record HoldReleased(
    HoldId HoldId,
    ResourceId ResourceId,
    OwnerId OwnerId,
    Quantity Quantity,
    DateTimeOffset OccurredAt)
    : IDomainEvent;
