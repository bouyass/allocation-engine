using AllocationEngine.Domain.ValueObjects;

namespace AllocationEngine.Domain.Errors;

public sealed class InsufficientCapacityException : Exception
{
    public Quantity Requested { get; }
    public Quantity Available { get; }

    public InsufficientCapacityException(
        Quantity requested,
        Quantity available)
        : base(
            $"Requested quantity '{requested}' exceeds available quantity '{available}'.")
    {
        Requested = requested;
        Available = available;
    }
}