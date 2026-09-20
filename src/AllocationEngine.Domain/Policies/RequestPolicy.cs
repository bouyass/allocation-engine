using AllocationEngine.Domain.ValueObjects;

namespace AllocationEngine.Domain.Policies;

public sealed record RequestPolicy
{
    public Quantity? MaxQuantityPerAcquire { get; }

    public RequestPolicy(Quantity? maxQuantityPerAcquire = null)
    {
        if (maxQuantityPerAcquire == Quantity.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxQuantityPerAcquire),
                "Maximum quantity per acquire must be greater than zero.");
        }

        MaxQuantityPerAcquire = maxQuantityPerAcquire;
    }
}