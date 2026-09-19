using AllocationEngine.Domain.ValueObjects;

namespace AllocationEngine.Domain.Tests.ValueObjects;

public class PolicyVersionTests
{
    [Fact]
    public void Constructor_WithPositiveValue_CreatesPolicyVersion()
    {
        var version = new PolicyVersion(42);

        Assert.Equal(42, version.Value);
    }

    [Fact]
    public void Initial_IsVersionOne()
    {
        Assert.Equal(new PolicyVersion(1), PolicyVersion.Initial);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveValue_Throws(long value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PolicyVersion(value));
    }

    [Fact]
    public void Next_ReturnsNextVersion()
    {
        var current = new PolicyVersion(4);

        var next = current.Next();

        Assert.Equal(new PolicyVersion(5), next);
    }

    [Fact]
    public void Next_DoesNotModifyCurrentVersion()
    {
        var current = new PolicyVersion(4);

        _ = current.Next();

        Assert.Equal(new PolicyVersion(4), current);
    }

    [Fact]
    public void Next_WhenOverflowing_Throws()
    {
        var version = new PolicyVersion(long.MaxValue);

        Assert.Throws<OverflowException>(
            () => version.Next());
    }

    [Fact]
    public void PolicyVersions_CanBeCompared()
    {
        var oldVersion = new PolicyVersion(2);
        var newVersion = new PolicyVersion(3);

        Assert.True(oldVersion < newVersion);
        Assert.True(newVersion > oldVersion);
        Assert.True(oldVersion <= newVersion);
        Assert.True(newVersion >= oldVersion);
    }
}