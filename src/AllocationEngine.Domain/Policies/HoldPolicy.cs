using AllocationEngine.Domain.ValueObjects;

namespace AllocationEngine.Domain.Policies;

public sealed record HoldPolicy
{
    public HoldTtl DefaultTtl { get; }

    public HoldTtl MinTtl { get; }

    public HoldTtl MaxTtl { get; }

    public HoldPolicy(
        HoldTtl defaultTtl,
        HoldTtl minTtl,
        HoldTtl maxTtl)
    {
        if (minTtl > maxTtl)
            throw new ArgumentException(
                "Minimum TTL cannot exceed maximum TTL.");

        if (defaultTtl < minTtl ||
            defaultTtl > maxTtl)
            throw new ArgumentOutOfRangeException(
                nameof(defaultTtl),
                "Default TTL must be within the allowed TTL range.");

        DefaultTtl = defaultTtl;
        MinTtl = minTtl;
        MaxTtl = maxTtl;
    }
}