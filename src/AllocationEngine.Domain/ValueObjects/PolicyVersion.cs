namespace AllocationEngine.Domain.ValueObjects;

public readonly record struct PolicyVersion : IComparable<PolicyVersion>
{
    public static PolicyVersion Initial { get; } = new(1);

    public long Value { get; }

    public PolicyVersion(long value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "PolicyVersion must be greater than zero.");
        }

        Value = value;
    }

    public PolicyVersion Next()
    {
        return new(checked(Value + 1));
    }

    public int CompareTo(PolicyVersion other)
    {
        return Value.CompareTo(other.Value);
    }

    public static bool operator >(PolicyVersion a, PolicyVersion b)
        => a.Value > b.Value;

    public static bool operator <(PolicyVersion a, PolicyVersion b)
        => a.Value < b.Value;

    public static bool operator >=(PolicyVersion a, PolicyVersion b)
        => a.Value >= b.Value;

    public static bool operator <=(PolicyVersion a, PolicyVersion b)
        => a.Value <= b.Value;

    public override string ToString()
    {
        return Value.ToString();
    }
}