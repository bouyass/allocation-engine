using AllocationEngine.Domain.Events;
using AllocationEngine.Domain.ValueObjects;


public sealed record HoldConfirmed(
    HoldId HoldId,
    ResourceId ResourceId,
    OwnerId OwnerId,
    Quantity Quantity,
    DateTimeOffset OccurredAt)
    : IDomainEvent;
