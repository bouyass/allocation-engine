using AllocationEngine.Domain.Errors;
using AllocationEngine.Domain.Resources;
using AllocationEngine.Domain.ValueObjects;

namespace AllocationEngine.Domain.Tests.Resources;

public class ResourceTests
{
    [Fact]
    public void Constructor_CreatesActiveResourceWithFullAvailability()
    {
        var id = new ResourceId(Guid.NewGuid());
        var capacity = new Quantity(100);
        var version = PolicyVersion.Initial;
        var now = new DateTimeOffset(
            2026, 9, 20,
            10, 0, 0,
            TimeSpan.Zero);

        var resource = new Resource(
            id,
            capacity,
            version,
            now);

        Assert.Equal(id, resource.Id);
        Assert.Equal(new Quantity(100), resource.Capacity);

        Assert.Equal(Quantity.Zero, resource.HeldQuantity);
        Assert.Equal(Quantity.Zero, resource.AllocatedQuantity);

        Assert.Equal(
            new Quantity(100),
            resource.AvailableQuantity);

        Assert.Equal(ResourceStatus.Active, resource.Status);

        Assert.Equal(version, resource.PolicyVersion);

        Assert.Equal(now, resource.CreatedAt);
        Assert.Equal(now, resource.UpdatedAt);
    }

    [Fact]
    public void Pause_WhenActive_ChangesStatusToPaused()
    {
        var resource = CreateResource();
        var now = new DateTimeOffset(
            2026, 9, 20, 11, 0, 0, TimeSpan.Zero);

        resource.Pause(now);

        Assert.Equal(ResourceStatus.Paused, resource.Status);
        Assert.Equal(now, resource.UpdatedAt);
    }

    [Fact]
    public void Pause_WhenAlreadyPaused_IsNoOp()
    {
        var resource = CreateResource();

        var pausedAt = new DateTimeOffset(
            2026, 9, 20, 11, 0, 0, TimeSpan.Zero);

        resource.Pause(pausedAt);

        var later = pausedAt.AddHours(1);

        resource.Pause(later);

        Assert.Equal(ResourceStatus.Paused, resource.Status);

        // Le no-op ne constitue pas une modification.
        Assert.Equal(pausedAt, resource.UpdatedAt);
    }

    [Fact]
    public void Resume_WhenPaused_ChangesStatusToActive()
    {
        var resource = CreateResource();

        var pausedAt = new DateTimeOffset(
            2026, 9, 20, 11, 0, 0, TimeSpan.Zero);

        var resumedAt = pausedAt.AddHours(1);

        resource.Pause(pausedAt);
        resource.Resume(resumedAt);

        Assert.Equal(ResourceStatus.Active, resource.Status);
        Assert.Equal(resumedAt, resource.UpdatedAt);
    }

    [Fact]
    public void Close_WhenActive_ChangesStatusToClosed()
    {
        var resource = CreateResource();

        var closedAt = new DateTimeOffset(
            2026, 9, 20, 11, 0, 0, TimeSpan.Zero);

        resource.Close(closedAt);

        Assert.Equal(ResourceStatus.Closed, resource.Status);
        Assert.Equal(closedAt, resource.UpdatedAt);
    }

    [Fact]
    public void Close_WhenPaused_ChangesStatusToClosed()
    {
        var resource = CreateResource();

        var pausedAt = new DateTimeOffset(
            2026, 9, 20, 11, 0, 0, TimeSpan.Zero);

        var closedAt = pausedAt.AddHours(1);

        resource.Pause(pausedAt);
        resource.Close(closedAt);

        Assert.Equal(ResourceStatus.Closed, resource.Status);
    }

    [Fact]
    public void Resume_WhenClosed_Throws()
    {
        var resource = CreateResource();

        var closedAt = new DateTimeOffset(
            2026, 9, 20, 11, 0, 0, TimeSpan.Zero);

        resource.Close(closedAt);

        Assert.Throws<InvalidResourceTransitionException>(
            () => resource.Resume(closedAt.AddMinutes(1)));
    }

    [Fact]
    public void Pause_WhenClosed_Throws()
    {
        var resource = CreateResource();

        var closedAt = new DateTimeOffset(
            2026, 9, 20, 11, 0, 0, TimeSpan.Zero);

        resource.Close(closedAt);

        Assert.Throws<InvalidResourceTransitionException>(
            () => resource.Pause(closedAt.AddMinutes(1)));
    }


    private static Resource CreateResource()
    {
        return new Resource(
            new ResourceId(Guid.NewGuid()),
            new Quantity(100),
            PolicyVersion.Initial,
            new DateTimeOffset(
                2026, 9, 20,
                10, 0, 0,
                TimeSpan.Zero));
    }

}