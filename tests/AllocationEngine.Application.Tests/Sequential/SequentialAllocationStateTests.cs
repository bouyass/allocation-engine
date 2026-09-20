using AllocationEngine.Domain.Errors;
using AllocationEngine.Domain.Holds;
using AllocationEngine.Domain.Policies;
using AllocationEngine.Domain.Resources;
using AllocationEngine.Domain.ValueObjects;

public class SequentialAllocationStateTests
{
    private readonly SequentialAllocationState _state;

    public SequentialAllocationStateTests()
    {
        _state = new SequentialAllocationState();
    }

    [Fact]
    public void AddResource_WhenResourceDoesNotExist_AddsResource()
    {
        // Arrange
        PolicySet policies = new PolicySet(
            new PolicyVersion(1),
            new RequestPolicy(new Quantity(3)),
            new OwnerPolicy(new Quantity(10), 2),
            new HoldPolicy(new HoldTtl(TimeSpan.FromMinutes(1)), new HoldTtl(TimeSpan.FromSeconds(30)), new HoldTtl(TimeSpan.FromMinutes(5)))
        );

        var resource = new Resource(new ResourceId(Guid.NewGuid()), new Quantity(100), new PolicyVersion(1), policies, DateTimeOffset.UtcNow);

        // Act
        _state.AddResource(resource);

        // Assert
        var retrievedResource = _state.GetResource(resource.Id);
        Assert.Equal(resource, retrievedResource);
    }

    [Fact]
    public void GetResource_WhenResourceExists_ReturnsResource()
    {
        // Arrange
        PolicySet policies = new PolicySet(
            new PolicyVersion(1),
            new RequestPolicy(new Quantity(3)),
            new OwnerPolicy(new Quantity(10), 2),
            new HoldPolicy(new HoldTtl(TimeSpan.FromMinutes(1)), new HoldTtl(TimeSpan.FromSeconds(30)), new HoldTtl(TimeSpan.FromMinutes(5)))
        );

        var resource = new Resource(new ResourceId(Guid.NewGuid()), new Quantity(100), new PolicyVersion(1), policies, DateTimeOffset.UtcNow);
        _state.AddResource(resource);

        // Act
        var retrievedResource = _state.GetResource(resource.Id);

        // Assert
        Assert.Equal(resource, retrievedResource);
    }

    [Fact]
    public void AddResource_WhenResourceAlreadyExists_Throws()
    {

        // Arrange
        PolicySet policies = new PolicySet(
            new PolicyVersion(1),
            new RequestPolicy(new Quantity(3)),
            new OwnerPolicy(new Quantity(10), 2),
            new HoldPolicy(new HoldTtl(TimeSpan.FromMinutes(1)), new HoldTtl(TimeSpan.FromSeconds(30)), new HoldTtl(TimeSpan.FromMinutes(5)))
        );

        var resource = new Resource(new ResourceId(Guid.NewGuid()), new Quantity(100), new PolicyVersion(1), policies, DateTimeOffset.UtcNow);
        _state.AddResource(resource);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _state.AddResource(resource));
    }

    [Fact]
    public void GetResource_WhenResourceDoesNotExist_Throws()
    {
        // Arrange
        var resourceId = new ResourceId(Guid.NewGuid());

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => _state.GetResource(resourceId));
    }

    [Fact]
    public void Acquire_WhenOwnerActiveHoldLimitIsReached_RejectsNextAcquire()
    {
        var state = new SequentialAllocationState();

        var resource = CreateResource(
            capacity: 100,
            maxQuantityPerAcquire: 10,
            maxHeldQuantity: 20,
            maxActiveHolds: 2);

        state.AddResource(resource);

        var owner = new OwnerId("owner-1");
        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        state.Acquire(
        resource.Id,
        new HoldId(Guid.NewGuid()),
        owner,
        new Quantity(1),
        null,
        now);


        state.Acquire(
        resource.Id,
        new HoldId(Guid.NewGuid()),
        owner,
        new Quantity(1),
        null,
        now);

        Assert.Throws<InvalidOperationException>(() =>
        {
            state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            owner,
            new Quantity(1),
            null,
            now);
        });

        Assert.Equal(new Quantity(2), resource.HeldQuantity);
        Assert.Equal(new Quantity(98), resource.AvailableQuantity);
    }

    [Fact]
    public void Acquire_WhenRequestIsValid_CreatesHoldAndReservesCapacity()
    {
        var state = new SequentialAllocationState();

        var resource = CreateResource();

        state.AddResource(resource);

        var owner = new OwnerId("owner-1");
        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        var hold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            owner,
            new Quantity(5),
            new HoldTtl(TimeSpan.FromMinutes(10)),
            now);

        Assert.Equal(owner, hold.OwnerId);
        Assert.Equal(new Quantity(5), resource.HeldQuantity);
        Assert.Equal(new Quantity(95), resource.AvailableQuantity);
        Assert.Equal(owner, hold.OwnerId);
        Assert.Equal(new Quantity(5), hold.Quantity);
        Assert.Equal(Quantity.Zero, resource.AllocatedQuantity);

        Assert.Equal(
        now.AddMinutes(10),
        hold.ExpiresAt);
    }


    [Fact]
    public void Acquire_WhenTtlIsNotSpecified_UsesDefaultTtl()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource();

        state.AddResource(resource);

        var owner = new OwnerId("owner-1");
        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        var hold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            owner,
            new Quantity(5),
            null,
            now);

        Assert.Equal(now.AddMinutes(5), hold.ExpiresAt);
    }


    [Fact]
    public void Acquire_WhenCapacityIsInsufficient_DoesNotCreateHold()
    {
        var state = new SequentialAllocationState();

        var resource = CreateResource(
            capacity: 5,
            maxQuantityPerAcquire: null,
            maxHeldQuantity: null);

        state.AddResource(resource);

        var owner = new OwnerId("owner-1");
        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        Assert.Throws<InsufficientCapacityException>(() =>
        {
            state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            owner,
            new Quantity(10),
            null,
            now);
        });

        Assert.Equal(Quantity.Zero, resource.HeldQuantity);
        Assert.Equal(new Quantity(5), resource.AvailableQuantity);

    }

    [Fact]
    public void Acquire_WhenMaxQuantityPerAcquireIsExceeded_DoesNotReserveCapacity()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource(
            maxQuantityPerAcquire: 10);

        state.AddResource(resource);

        Assert.Throws<InvalidOperationException>(() =>
            state.Acquire(
                resource.Id,
                new HoldId(Guid.NewGuid()),
                new OwnerId("owner-1"),
                new Quantity(11),
                requestedTtl: null,
                DateTimeOffset.Parse("2026-09-20T12:00:00Z")));

        Assert.Equal(Quantity.Zero, resource.HeldQuantity);
        Assert.Equal(new Quantity(100), resource.AvailableQuantity);
    }

    [Fact]
    public void Acquire_WhenOwnerHeldQuantityLimitWouldBeExceeded_RejectsSecondAcquire()
    {
        var state = new SequentialAllocationState();

        var resource = CreateResource(
            maxQuantityPerAcquire: null,
            maxHeldQuantity: 10);

        state.AddResource(resource);

        var owner = new OwnerId("owner-1");
        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            owner,
            new Quantity(6),
            null,
            now);

        Assert.Throws<InvalidOperationException>(() =>
        {
            state.Acquire(
                resource.Id,
                new HoldId(Guid.NewGuid()),
                owner,
                new Quantity(5),
                null,
                now);
        });

        Assert.Equal(new Quantity(6), resource.HeldQuantity);
        Assert.Equal(new Quantity(94), resource.AvailableQuantity);
    }

    [Fact]
    public void ConfirmHold_WhenHoldExists_ConfirmsHoldAndAllocatesCapacity()
    {
        var state = new SequentialAllocationState();

        var resource = CreateResource();

        state.AddResource(resource);

        var owner = new OwnerId("owner-1");
        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        var hold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            owner,
            new Quantity(5),
            null,
            now);

        state.ConfirmHold(hold.Id, now);

        Assert.Equal(new Quantity(5), resource.AllocatedQuantity);
        Assert.Equal(new Quantity(95), resource.AvailableQuantity);
    }

    [Fact]
    public void ConfirmHold_WhenHoldDoesNotExist_Throws()
    {
        var state = new SequentialAllocationState();
        var holdId = new HoldId(Guid.NewGuid());

        Assert.Throws<KeyNotFoundException>(() => state.ConfirmHold(holdId, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ConfirmHold_WhenHoldIsAlreadyConfirmed_DoesNotChangeState()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource();

        state.AddResource(resource);

        var owner = new OwnerId("owner-1");
        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        var hold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            owner,
            new Quantity(5),
            null,
            now);

        state.ConfirmHold(hold.Id, now);

        // Confirm the hold again
        state.ConfirmHold(hold.Id, now);

        Assert.Equal(new Quantity(5), resource.AllocatedQuantity);
        Assert.Equal(new Quantity(95), resource.AvailableQuantity);
    }

    [Fact]
    public void ConfirmHold_WhenHoldIsReleased_Throws()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource();

        state.AddResource(resource);

        var owner = new OwnerId("owner-1");
        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        var hold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            owner,
            new Quantity(5),
            null,
            now);

        state.ReleaseHold(hold.Id, now);

        Assert.Throws<InvalidOperationException>(() => state.ConfirmHold(hold.Id, now));
    }

    [Fact]
    public void ConfirmHold_WhenTtlHasElapsedButHoldWasNotReclaimed_ConfirmsHold()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource();

        state.AddResource(resource);

        var owner = new OwnerId("owner-1");
        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        var hold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            owner,
            new Quantity(5),
            new HoldTtl(TimeSpan.FromMinutes(1)),
            now);

        // Simulate time passing beyond the hold's expiration
        var later = now.AddMinutes(2);

        // Confirm the hold after its TTL has elapsed
        state.ConfirmHold(hold.Id, later);

        Assert.Equal(new Quantity(5), resource.AllocatedQuantity);
        Assert.Equal(new Quantity(95), resource.AvailableQuantity);
    }


    [Fact]
    public void ReleaseHold_WhenHoldExists_ReleasesHoldAndFreesCapacity()
    {
        var state = new SequentialAllocationState();

        var resource = CreateResource();

        state.AddResource(resource);

        var owner = new OwnerId("owner-1");
        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        var hold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            owner,
            new Quantity(5),
            null,
            now);

        state.ReleaseHold(hold.Id, now);

        Assert.Equal(Quantity.Zero, resource.HeldQuantity);
        Assert.Equal(new Quantity(100), resource.AvailableQuantity);
    }

    [Fact]
    public void ReleaseHold_WhenHoldDoesNotExist_Throws()
    {
        var state = new SequentialAllocationState();
        var holdId = new HoldId(Guid.NewGuid());

        Assert.Throws<KeyNotFoundException>(() => state.ReleaseHold(holdId, DateTimeOffset.UtcNow));

    }

    [Fact]
    public void ReleaseHold_WhenHoldIsAlreadyReleased_DoesNotChangeState()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource();

        state.AddResource(resource);

        var owner = new OwnerId("owner-1");
        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        var hold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            owner,
            new Quantity(5),
            null,
            now);

        state.ReleaseHold(hold.Id, now);

        // Release the hold again
        state.ReleaseHold(hold.Id, now);

        Assert.Equal(Quantity.Zero, resource.HeldQuantity);
        Assert.Equal(new Quantity(100), resource.AvailableQuantity);
    }

    [Fact]
    public void ReleaseHold_WhenHoldIsConfirmed_Throws()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource();

        state.AddResource(resource);

        var owner = new OwnerId("owner-1");
        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        var hold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            owner,
            new Quantity(5),
            null,
            now);

        state.ConfirmHold(hold.Id, now);

        Assert.Throws<InvalidOperationException>(() => state.ReleaseHold(hold.Id, now));
    }

    [Fact]
    public void ReclaimExpiredHolds_WhenHoldIsExpired_ReclaimsHoldAndFreesCapacity()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource();

        state.AddResource(resource);

        var owner = new OwnerId("owner-1");
        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        var hold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            owner,
            new Quantity(5),
            new HoldTtl(TimeSpan.FromMinutes(1)),
            now);

        // Simulate time passing beyond the hold's expiration
        var later = now.AddMinutes(2);

        state.ReclaimExpiredHolds(later);

        Assert.Equal(Quantity.Zero, resource.HeldQuantity);
        Assert.Equal(new Quantity(100), resource.AvailableQuantity);

        Assert.Equal(HoldStatus.Expired, hold.Status);

    }

    [Fact]
    public void ReclaimExpiredHolds_WhenHoldIsNotExpired_DoesNotReclaimHold()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource();

        state.AddResource(resource);

        var owner = new OwnerId("owner-1");
        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        var hold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            owner,
            new Quantity(5),
            new HoldTtl(TimeSpan.FromMinutes(5)),
            now);

        // Simulate time passing but not beyond the hold's expiration
        var later = now.AddMinutes(2);

        state.ReclaimExpiredHolds(later);

        Assert.Equal(new Quantity(5), resource.HeldQuantity);
        Assert.Equal(new Quantity(95), resource.AvailableQuantity);

        Assert.Equal(HoldStatus.Held, hold.Status);
    }

    [Fact]
    public void ReclaimExpiredHolds_WhenHoldIsConfirmed_DoesNotReclaimHold()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource();

        state.AddResource(resource);

        var owner = new OwnerId("owner-1");
        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        var hold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            owner,
            new Quantity(5),
            new HoldTtl(TimeSpan.FromMinutes(1)),
            now);

        state.ConfirmHold(hold.Id, now);

        // Simulate time passing beyond the hold's expiration
        var later = now.AddMinutes(2);

        state.ReclaimExpiredHolds(later);

        Assert.Equal(new Quantity(5), resource.AllocatedQuantity);
        Assert.Equal(new Quantity(95), resource.AvailableQuantity);

        Assert.Equal(HoldStatus.Confirmed, hold.Status);
    }

    [Fact]
    public void ReclaimExpiredHolds_WhenCalledTwice_DoesNotReleaseCapacityTwice()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource();

        state.AddResource(resource);

        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        var hold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            new OwnerId("owner-1"),
            new Quantity(5),
            new HoldTtl(TimeSpan.FromMinutes(1)),
            now);

        var later = now.AddMinutes(2);

        state.ReclaimExpiredHolds(later);
        state.ReclaimExpiredHolds(later);

        Assert.Equal(HoldStatus.Expired, hold.Status);
        Assert.Equal(Quantity.Zero, resource.HeldQuantity);
        Assert.Equal(new Quantity(100), resource.AvailableQuantity);
    }

    [Fact]
    public void ConfirmHold_WhenHoldWasReclaimed_Throws()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource();

        state.AddResource(resource);

        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        var hold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            new OwnerId("owner-1"),
            new Quantity(5),
            new HoldTtl(TimeSpan.FromMinutes(1)),
            now);

        var later = now.AddMinutes(2);

        state.ReclaimExpiredHolds(later);

        Assert.Equal(HoldStatus.Expired, hold.Status);

        Assert.Throws<InvalidOperationException>(
            () => state.ConfirmHold(hold.Id, later));

        Assert.Equal(Quantity.Zero, resource.HeldQuantity);
        Assert.Equal(Quantity.Zero, resource.AllocatedQuantity);
        Assert.Equal(new Quantity(100), resource.AvailableQuantity);
    }


    private static Resource CreateResource(
        long capacity = 100,
        long? maxQuantityPerAcquire = 10,
        long? maxHeldQuantity = 20,
        int? maxActiveHolds = 3)
    {
        var policies = new PolicySet(
            PolicyVersion.Initial,
            new RequestPolicy(
                maxQuantityPerAcquire is null
                    ? null
                    : new Quantity(maxQuantityPerAcquire.Value)),
            new OwnerPolicy(
                maxHeldQuantity is null
                    ? null
                    : new Quantity(maxHeldQuantity.Value),
                maxActiveHolds),
            new HoldPolicy(
                new HoldTtl(TimeSpan.FromMinutes(5)),
                new HoldTtl(TimeSpan.FromMinutes(1)),
                new HoldTtl(TimeSpan.FromMinutes(30))));

        return new Resource(
            new ResourceId(Guid.NewGuid()),
            new Quantity(capacity),
            PolicyVersion.Initial,
            policies,
            DateTimeOffset.Parse("2026-09-20T12:00:00Z"));
    }
}
