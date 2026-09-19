namespace AllocationEngine.Domain.ValueObjects;

public readonly record struct HoldId
{
    public Guid Value { get; }

    public HoldId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException(
                "HoldId cannot be empty.",
                nameof(value));

        Value = value;
    }

    public override string ToString()
        => Value.ToString();
}