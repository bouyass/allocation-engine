namespace AllocationEngine.Domain.ValueObjects;

public readonly record struct HoldTtl : IComparable<HoldTtl>
{
    public TimeSpan Value { get; }

    public HoldTtl(TimeSpan value)
    {
        if (value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Hold TTL must be greater than zero.");
        }

        Value = value;
    }

    public DateTimeOffset GetExpirationFrom(DateTimeOffset createdAt)
    {
        return createdAt + Value;
    }

    public int CompareTo(HoldTtl other)
    {
        return Value.CompareTo(other.Value);
    }

    public static bool operator <(HoldTtl a, HoldTtl b)
        => a.Value < b.Value;

    public static bool operator >(HoldTtl a, HoldTtl b)
        => a.Value > b.Value;

    public static bool operator <=(HoldTtl a, HoldTtl b)
        => a.Value <= b.Value;

    public static bool operator >=(HoldTtl a, HoldTtl b)
        => a.Value >= b.Value;

    public override string ToString()
    {
        return Value.ToString();
    }
}