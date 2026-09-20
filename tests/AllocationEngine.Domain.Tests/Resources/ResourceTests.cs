using AllocationEngine.Domain.Errors;
using AllocationEngine.Domain.Policies;
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
        var policies = new PolicySet(
            version,
            new RequestPolicy(new Quantity(3)),
            new OwnerPolicy(new Quantity(10), 2),
            new HoldPolicy(new HoldTtl(TimeSpan.FromMinutes(1)), new HoldTtl(TimeSpan.FromSeconds(30)), new HoldTtl(TimeSpan.FromMinutes(5))));

        var resource = new Resource(
            id,
            capacity,
            version,
            policies,
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

    [Fact]
    public void Reserve_MovesAvailableQuantityToHeld()
    {
        var resource = CreateResource();

        resource.Reserve(
            new Quantity(30),
            resource.CreatedAt.AddMinutes(1));

        Assert.Equal(new Quantity(30), resource.HeldQuantity);
        Assert.Equal(Quantity.Zero, resource.AllocatedQuantity);
        Assert.Equal(new Quantity(70), resource.AvailableQuantity);
    }

    [Fact]
    public void Reserve_WhenCapacityIsInsufficient_Throws()
    {
        var resource = CreateResource();

        Assert.Throws<InsufficientCapacityException>(
            () => resource.Reserve(
                new Quantity(101),
                resource.CreatedAt.AddMinutes(1)));
    }

    [Fact]
    public void ConfirmReservation_MovesHeldQuantityToAllocated()
    {
        var resource = CreateResource();

        resource.Reserve(
            new Quantity(30),
            resource.CreatedAt.AddMinutes(1));

        resource.ConfirmReservation(
            new Quantity(20),
            resource.CreatedAt.AddMinutes(2));

        Assert.Equal(new Quantity(10), resource.HeldQuantity);
        Assert.Equal(new Quantity(20), resource.AllocatedQuantity);

        // Confirm ne libère aucune capacité.
        Assert.Equal(new Quantity(70), resource.AvailableQuantity);
    }

    [Fact]
    public void ReleaseReservation_ReturnsHeldQuantityToAvailability()
    {
        var resource = CreateResource();

        resource.Reserve(
            new Quantity(30),
            resource.CreatedAt.AddMinutes(1));

        resource.ReleaseReservation(
            new Quantity(20),
            resource.CreatedAt.AddMinutes(2));

        Assert.Equal(new Quantity(10), resource.HeldQuantity);
        Assert.Equal(new Quantity(90), resource.AvailableQuantity);
    }

    [Fact]
    public void RemoveCapacity_CannotGoBelowCommittedQuantity()
    {
        var resource = CreateResource();

        resource.Reserve(
            new Quantity(40),
            resource.CreatedAt.AddMinutes(1));

        resource.ConfirmReservation(
            new Quantity(20),
            resource.CreatedAt.AddMinutes(2));

        // Held = 20
        // Allocated = 20
        // Committed = 40

        Assert.Throws<CapacityBelowCommittedException>(
            () => resource.RemoveCapacity(
                new Quantity(61),
                resource.CreatedAt.AddMinutes(3)));

        Assert.Equal(new Quantity(100), resource.Capacity);
    }


    [Fact]
    public void RemoveCapacity_CanReduceCapacityToCommittedQuantity()
    {
        var resource = CreateResource();

        resource.Reserve(
            new Quantity(40),
            resource.CreatedAt.AddMinutes(1));

        resource.ConfirmReservation(
            new Quantity(20),
            resource.CreatedAt.AddMinutes(2));

        resource.RemoveCapacity(
            new Quantity(60),
            resource.CreatedAt.AddMinutes(3));

        Assert.Equal(new Quantity(40), resource.Capacity);
        Assert.Equal(new Quantity(20), resource.HeldQuantity);
        Assert.Equal(new Quantity(20), resource.AllocatedQuantity);
        Assert.Equal(Quantity.Zero, resource.AvailableQuantity);
    }


    private static Resource CreateResource()
    {
        var policies = new PolicySet(
            PolicyVersion.Initial,
            new RequestPolicy(new Quantity(3)),
            new OwnerPolicy(new Quantity(10), 2),
            new HoldPolicy(new HoldTtl(TimeSpan.FromMinutes(1)), new HoldTtl(TimeSpan.FromSeconds(30)), new HoldTtl(TimeSpan.FromMinutes(5)))
        );

        return new Resource(
            new ResourceId(Guid.NewGuid()),
            new Quantity(100),
            PolicyVersion.Initial,
            policies,
            new DateTimeOffset(
                2026, 9, 20,
                10, 0, 0,
                TimeSpan.Zero));
    }

}
