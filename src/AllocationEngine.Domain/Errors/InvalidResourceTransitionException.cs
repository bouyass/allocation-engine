using AllocationEngine.Domain.Resources;

namespace AllocationEngine.Domain.Errors;

public sealed class InvalidResourceTransitionException : Exception
{
    public InvalidResourceTransitionException(
        ResourceStatus currentStatus,
        string operation)
        : base(
            $"Cannot execute '{operation}' when resource status is '{currentStatus}'.")
    {
    }
}