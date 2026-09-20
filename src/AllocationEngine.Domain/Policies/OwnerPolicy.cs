using AllocationEngine.Domain.ValueObjects;

namespace AllocationEngine.Domain.Policies;

public sealed record OwnerPolicy
{
    public Quantity? MaxHeldQuantity { get; }

    public int? MaxActiveHolds { get; }

    public OwnerPolicy(
        Quantity? maxHeldQuantity = null,
        int? maxActiveHolds = null)
    {
        if (maxHeldQuantity == Quantity.Zero)
            throw new ArgumentOutOfRangeException(
                nameof(maxHeldQuantity),
                "Maximum held quantity must be greater than zero.");

        if (maxActiveHolds <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(maxActiveHolds),
                "Maximum active holds must be greater than zero.");

        MaxHeldQuantity = maxHeldQuantity;
        MaxActiveHolds = maxActiveHolds;
    }
}