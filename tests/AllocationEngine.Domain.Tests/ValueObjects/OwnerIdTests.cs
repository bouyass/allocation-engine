using AllocationEngine.Domain.ValueObjects;

namespace AllocationEngine.Domain.Tests.ValueObjects;

public class OwnerIdTests
{
    [Fact]
    public void Constructor_WithValidValue_CreatesOwnerId()
    {
        var ownerId = new OwnerId("customer-42");

        Assert.Equal("customer-42", ownerId.Value);
    }

    [Fact]
    public void Constructor_WithNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new OwnerId(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhitespaceValue_Throws(string value)
    {
        Assert.Throws<ArgumentException>(
            () => new OwnerId(value));
    }

    [Fact]
    public void Constructor_WhenValueExceedsMaximumLength_Throws()
    {
        var value = new string('a', OwnerId.MaxLength + 1);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new OwnerId(value));
    }

    [Fact]
    public void Constructor_WithMaximumLengthValue_Succeeds()
    {
        var value = new string('a', OwnerId.MaxLength);

        var ownerId = new OwnerId(value);

        Assert.Equal(value, ownerId.Value);
    }

    [Fact]
    public void OwnerIds_WithSameValue_AreEqual()
    {
        Assert.Equal(
            new OwnerId("customer-42"),
            new OwnerId("customer-42"));
    }

    [Fact]
    public void OwnerIdComparison_IsCaseSensitive()
    {
        Assert.NotEqual(
            new OwnerId("customer-42"),
            new OwnerId("CUSTOMER-42"));
    }
}