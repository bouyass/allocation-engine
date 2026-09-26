namespace AllocationEngine.Domain.ValueObjects;

public readonly record struct IdempotencyKey
{
    public const int MaxLength = 256;

    public string Value { get; }

    public IdempotencyKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (value.Length > MaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value.Length,
                $"IdempotencyKey cannot exceed {MaxLength} characters.");
        }

        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }
}
