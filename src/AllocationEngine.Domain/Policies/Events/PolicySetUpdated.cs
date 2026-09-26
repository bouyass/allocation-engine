using AllocationEngine.Domain.Events;
using AllocationEngine.Domain.ValueObjects;

namespace AllocationEngine.Domain.Policies.Events;

public sealed record PolicySetUpdated(
    ResourceId ResourceId,
    PolicyVersion PreviousVersion,
    PolicyVersion NewVersion,
    DateTimeOffset OccurredAt) : IDomainEvent;
