using AllocationEngine.Domain.ValueObjects;

namespace AllocationEngine.Domain.Tests.ValueObjects;

public class HoldTtlTests
{
    [Fact]
    public void Constructor_WithPositiveDuration_CreatesHoldTtl()
    {
        var duration = TimeSpan.FromMinutes(5);

        var ttl = new HoldTtl(duration);

        Assert.Equal(duration, ttl.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveDuration_Throws(int seconds)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new HoldTtl(TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void GetExpirationFrom_ReturnsCreatedAtPlusTtl()
    {
        var createdAt = new DateTimeOffset(
            2026, 9, 20, 10, 0, 0, TimeSpan.Zero);

        var ttl = new HoldTtl(TimeSpan.FromMinutes(5));

        var expiresAt = ttl.GetExpirationFrom(createdAt);

        Assert.Equal(
            new DateTimeOffset(2026, 9, 20, 10, 5, 0, TimeSpan.Zero),
            expiresAt);
    }

    [Fact]
    public void HoldTtls_CanBeCompared()
    {
        var shortTtl = new HoldTtl(TimeSpan.FromMinutes(5));
        var longTtl = new HoldTtl(TimeSpan.FromMinutes(10));

        Assert.True(shortTtl < longTtl);
        Assert.True(longTtl > shortTtl);
        Assert.True(shortTtl <= longTtl);
        Assert.True(longTtl >= shortTtl);
    }
}