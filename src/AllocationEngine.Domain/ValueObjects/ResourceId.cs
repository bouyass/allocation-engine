namespace AllocationEngine.Domain.ValueObjects;

public readonly record struct ResourceId
{
    public Guid Value { get; }

    public ResourceId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "ResourceId cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}