using AllocationEngine.Domain.Holds;
using AllocationEngine.Domain.ValueObjects;

public class HoldTests
{
    [Fact]
    public void Constructor_CreatesHeldHold()
    {
        var hold = CreateHold();

        Assert.Equal(HoldStatus.Held, hold.Status);
        Assert.Equal(new Quantity(10), hold.Quantity);
    }

    [Fact]
    public void Hold_BeforeExpiration_IsNotReclaimable()
    {
        var hold = CreateHold();

        Assert.False(
            hold.IsReclaimable(
                hold.ExpiresAt.AddSeconds(-1)));
    }

    [Fact]
    public void Hold_AtExpiration_IsReclaimable()
    {
        var hold = CreateHold();

        Assert.True(
            hold.IsReclaimable(hold.ExpiresAt));
    }

    [Fact]
    public void Confirm_WhenHeld_ChangesStatusToConfirmed()
    {
        var hold = CreateHold();

        hold.Confirm();

        Assert.Equal(HoldStatus.Confirmed, hold.Status);
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_IsIdempotent()
    {
        var hold = CreateHold();

        hold.Confirm();
        hold.Confirm();

        Assert.Equal(HoldStatus.Confirmed, hold.Status);
    }

    [Fact]
    public void Confirm_AfterExpirationTime_ButBeforeReclaim_Succeeds()
    {
        var hold = CreateHold();

        var afterExpiration =
            hold.ExpiresAt.AddMinutes(1);

        Assert.True(
            hold.IsReclaimable(afterExpiration));

        hold.Confirm();

        Assert.Equal(
            HoldStatus.Confirmed,
            hold.Status);
    }

    [Fact]
    public void Reclaim_WhenExpirationReached_ExpiresHold()
    {
        var hold = CreateHold();

        hold.Reclaim(hold.ExpiresAt);

        Assert.Equal(HoldStatus.Expired, hold.Status);
    }

    [Fact]
    public void Reclaim_BeforeExpiration_Throws()
    {
        var hold = CreateHold();

        Assert.Throws<InvalidOperationException>(
            () => hold.Reclaim(
                hold.ExpiresAt.AddSeconds(-1)));
    }

    [Fact]
    public void Confirm_AfterReclaim_Throws()
    {
        var hold = CreateHold();

        hold.Reclaim(hold.ExpiresAt);

        Assert.Throws<InvalidOperationException>(
            () => hold.Confirm());
    }

    private static Hold CreateHold()
    {
        var createdAt = new DateTimeOffset(
            2026, 9, 20,
            10, 0, 0,
            TimeSpan.Zero);

        return new Hold(
            new HoldId(Guid.NewGuid()),
            new ResourceId(Guid.NewGuid()),
            new OwnerId("customer-42"),
            new Quantity(10),
            createdAt,
            createdAt.AddMinutes(5),
            PolicyVersion.Initial);
    }
}