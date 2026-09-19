using AllocationEngine.Domain.ValueObjects;

namespace AllocationEngine.Domain.Tests.ValueObjects;

public class HoldIdTests
{
    [Fact]
    public void Constructor_WithValidGuid_CreatesHoldId()
    {
        var value = Guid.NewGuid();

        var holdId = new HoldId(value);

        Assert.Equal(value, holdId.Value);
    }

    [Fact]
    public void Constructor_WithEmptyGuid_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new HoldId(Guid.Empty));
    }

    [Fact]
    public void HoldIds_WithSameValue_AreEqual()
    {
        var value = Guid.NewGuid();

        var first = new HoldId(value);
        var second = new HoldId(value);

        Assert.Equal(first, second);
    }

    [Fact]
    public void HoldIds_WithDifferentValues_AreNotEqual()
    {
        var first = new HoldId(Guid.NewGuid());
        var second = new HoldId(Guid.NewGuid());

        Assert.NotEqual(first, second);
    }
}