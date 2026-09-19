namespace AllocationEngine.Domain.Errors;

public sealed class CapacityBelowCommittedException : Exception
{
    public CapacityBelowCommittedException()
        : base(
            "Capacity cannot be reduced below held and allocated quantity.")
    {
    }
}