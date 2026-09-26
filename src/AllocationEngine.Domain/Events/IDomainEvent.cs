namespace AllocationEngine.Domain.Events;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
