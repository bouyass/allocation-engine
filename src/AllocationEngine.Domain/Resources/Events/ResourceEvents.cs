
using AllocationEngine.Domain.Events;
using AllocationEngine.Domain.ValueObjects;

namespace AllocationEngine.Domain.Resources.Events;

public sealed record ResourcePaused(
    ResourceId ResourceId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record ResourceResumed(
    ResourceId ResourceId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record ResourceClosed(
    ResourceId ResourceId,
    DateTimeOffset OccurredAt) : IDomainEvent;
