namespace AllocationEngine.Domain.ValueObjects;

public readonly record struct OwnerId
{
    public const int MaxLength = 256;

    public string Value { get; }

    public OwnerId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (value.Length > MaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value.Length,
                $"OwnerId cannot exceed {MaxLength} characters.");
        }

        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }
}