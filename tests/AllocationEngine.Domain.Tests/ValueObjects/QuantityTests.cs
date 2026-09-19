using AllocationEngine.Domain.ValueObjects;

namespace AllocationEngine.Domain.Tests.ValueObjects;

public class QuantityTests
{
    [Fact]
    public void Constructor_WithPositiveValue_CreatesQuantity()
    {
        var quantity = new Quantity(10);

        Assert.Equal(10, quantity.Value);
    }

    [Fact]
    public void Constructor_WithZero_CreatesQuantity()
    {
        var quantity = new Quantity(0);

        Assert.Equal(Quantity.Zero, quantity);
    }

    [Fact]
    public void Constructor_WithNegativeValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Quantity(-1));
    }

    [Fact]
    public void Addition_ReturnsSum()
    {
        var left = new Quantity(10);
        var right = new Quantity(5);

        var result = left + right;

        Assert.Equal(new Quantity(15), result);
    }

    [Fact]
    public void Addition_WhenOverflowing_Throws()
    {
        var left = new Quantity(long.MaxValue);
        var right = new Quantity(1);

        Assert.Throws<OverflowException>(
            () => _ = left + right);
    }

    [Fact]
    public void Subtraction_ReturnsDifference()
    {
        var left = new Quantity(10);
        var right = new Quantity(4);

        var result = left - right;

        Assert.Equal(new Quantity(6), result);
    }

    [Fact]
    public void Subtraction_ResultingInZero_IsAllowed()
    {
        var quantity = new Quantity(10);

        var result = quantity - quantity;

        Assert.Equal(Quantity.Zero, result);
    }

    [Fact]
    public void Subtraction_ResultingInNegativeValue_Throws()
    {
        var left = new Quantity(2);
        var right = new Quantity(5);

        Assert.Throws<InvalidOperationException>(
            () => _ = left - right);
    }

    [Fact]
    public void Quantities_CanBeCompared()
    {
        var small = new Quantity(5);
        var large = new Quantity(10);

        Assert.True(small < large);
        Assert.True(small <= large);
        Assert.True(large > small);
        Assert.True(large >= small);
    }

    [Fact]
    public void Quantities_WithSameValue_AreEqual()
    {
        var first = new Quantity(10);
        var second = new Quantity(10);

        Assert.Equal(first, second);
    }
}