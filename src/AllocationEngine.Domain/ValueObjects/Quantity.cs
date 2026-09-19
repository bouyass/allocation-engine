namespace AllocationEngine.Domain.ValueObjects;

public readonly record struct Quantity : IComparable<Quantity>
{
    public static Quantity Zero { get; } = new(0);

    public long Value { get; }

    public Quantity(long value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Quantity cannot be negative.");

        Value = value;
    }

    public int CompareTo(Quantity other)
        => Value.CompareTo(other.Value);

    public static Quantity operator +(Quantity a, Quantity b)
        => new(checked(a.Value + b.Value));

    public static Quantity operator -(Quantity a, Quantity b)
    {
        if (a < b)
            throw new InvalidOperationException(
                "Resulting quantity cannot be negative.");

        return new Quantity(a.Value - b.Value);
    }

    public static bool operator >(Quantity a, Quantity b)
        => a.Value > b.Value;

    public static bool operator <(Quantity a, Quantity b)
        => a.Value < b.Value;

    public static bool operator >=(Quantity a, Quantity b)
        => a.Value >= b.Value;

    public static bool operator <=(Quantity a, Quantity b)
        => a.Value <= b.Value;

    public override string ToString()
        => Value.ToString();
}