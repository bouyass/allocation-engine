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

        var resource = new Resource(new ResourceId(Guid.NewGuid()), new Quantity(100), policies, DateTimeOffset.UtcNow);

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

        var resource = new Resource(new ResourceId(Guid.NewGuid()), new Quantity(100), policies, DateTimeOffset.UtcNow);
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

        var resource = new Resource(new ResourceId(Guid.NewGuid()), new Quantity(100), policies, DateTimeOffset.UtcNow);
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

    [Fact]
    public void Acquire_WhenReclaimableCapacityIsSufficient_ReclaimsAndAcquires()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource(capacity: 10);

        state.AddResource(resource);

        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        var oldHold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            new OwnerId("owner-1"),
            new Quantity(10),
            new HoldTtl(TimeSpan.FromMinutes(1)),
            now);

        var later = now.AddMinutes(2);

        var newHold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            new OwnerId("owner-2"),
            new Quantity(5),
            null,
            later);

        Assert.Equal(HoldStatus.Expired, oldHold.Status);
        Assert.Equal(HoldStatus.Held, newHold.Status);

        Assert.Equal(new Quantity(10), resource.Capacity);
        Assert.Equal(new Quantity(5), resource.HeldQuantity);
        Assert.Equal(Quantity.Zero, resource.AllocatedQuantity);
        Assert.Equal(new Quantity(5), resource.AvailableQuantity);
    }

    [Fact]
    public void Acquire_WhenCapacityIsAlreadySufficient_DoesNotReclaimExpiredHold()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource(capacity: 10);
        state.AddResource(resource);

        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        var expiredHold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            new OwnerId("owner-1"),
            new Quantity(2),
            new HoldTtl(TimeSpan.FromMinutes(1)),
            now);

        var later = now.AddMinutes(2);

        var newHold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            new OwnerId("owner-2"),
            new Quantity(5),
            null,
            later);

        Assert.Equal(HoldStatus.Held, expiredHold.Status);
        Assert.Equal(HoldStatus.Held, newHold.Status);

        Assert.Equal(new Quantity(7), resource.HeldQuantity);
        Assert.Equal(new Quantity(3), resource.AvailableQuantity);
    }

    [Fact]
    public void Acquire_WhenMultipleExpiredHoldsAreRequired_ReclaimsEnoughCapacity()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource(capacity: 10);
        state.AddResource(resource);

        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        var firstExpiredHold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            new OwnerId("owner-1"),
            new Quantity(2),
            new HoldTtl(TimeSpan.FromMinutes(1)),
            now);

        var secondExpiredHold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            new OwnerId("owner-2"),
            new Quantity(4),
            new HoldTtl(TimeSpan.FromMinutes(1)),
            now);

        var activeHold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            new OwnerId("owner-3"),
            new Quantity(4),
            new HoldTtl(TimeSpan.FromMinutes(10)),
            now);

        var later = now.AddMinutes(2);

        // Available = 0
        // Need 5
        // Reclaim 2 + 4 = 6
        // Reserve 5
        // Available = 1
        var newHold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            new OwnerId("owner-4"),
            new Quantity(5),
            null,
            later);

        Assert.Equal(HoldStatus.Expired, firstExpiredHold.Status);
        Assert.Equal(HoldStatus.Expired, secondExpiredHold.Status);
        Assert.Equal(HoldStatus.Held, activeHold.Status);
        Assert.Equal(HoldStatus.Held, newHold.Status);

        Assert.Equal(new Quantity(9), resource.HeldQuantity);
        Assert.Equal(Quantity.Zero, resource.AllocatedQuantity);
        Assert.Equal(new Quantity(1), resource.AvailableQuantity);
    }

    [Fact]
    public void Acquire_WhenExpiredHoldIsLargerThanMissingQuantity_ReclaimsWholeHold()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource(capacity: 10);
        state.AddResource(resource);

        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        var expiredHold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            new OwnerId("owner-1"),
            new Quantity(6),
            new HoldTtl(TimeSpan.FromMinutes(1)),
            now);

        var activeHold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            new OwnerId("owner-2"),
            new Quantity(4),
            new HoldTtl(TimeSpan.FromMinutes(10)),
            now);

        var later = now.AddMinutes(2);

        // Missing = 3, but the expired Hold owns 6.
        // Whole-Hold reclaim => all 6 are released.
        var newHold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            new OwnerId("owner-3"),
            new Quantity(3),
            null,
            later);

        Assert.Equal(HoldStatus.Expired, expiredHold.Status);
        Assert.Equal(HoldStatus.Held, activeHold.Status);
        Assert.Equal(HoldStatus.Held, newHold.Status);

        Assert.Equal(new Quantity(7), resource.HeldQuantity);
        Assert.Equal(new Quantity(3), resource.AvailableQuantity);
    }

    [Fact]
    public void Acquire_WhenReclaimableCapacityIsInsufficient_DoesNotReclaimAnything()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource(capacity: 10);
        state.AddResource(resource);

        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        var expiredHold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            new OwnerId("owner-1"),
            new Quantity(3),
            new HoldTtl(TimeSpan.FromMinutes(1)),
            now);

        var activeHold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            new OwnerId("owner-2"),
            new Quantity(7),
            new HoldTtl(TimeSpan.FromMinutes(10)),
            now);

        var later = now.AddMinutes(2);

        Assert.Throws<InsufficientCapacityException>(() =>
            state.Acquire(
                resource.Id,
                new HoldId(Guid.NewGuid()),
                new OwnerId("owner-3"),
                new Quantity(5),
                null,
                later));

        // Acquire failed => opportunistic reclaim must not mutate state.
        Assert.Equal(HoldStatus.Held, expiredHold.Status);
        Assert.Equal(HoldStatus.Held, activeHold.Status);

        Assert.Equal(new Quantity(10), resource.HeldQuantity);
        Assert.Equal(Quantity.Zero, resource.AllocatedQuantity);
        Assert.Equal(Quantity.Zero, resource.AvailableQuantity);
    }

    [Fact]
    public void Acquire_DoesNotReclaimExpiredHoldFromAnotherResource()
    {
        var state = new SequentialAllocationState();

        var resourceA = CreateResource(capacity: 10);
        var resourceB = CreateResource(capacity: 10);

        state.AddResource(resourceA);
        state.AddResource(resourceB);

        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        var holdA = state.Acquire(
            resourceA.Id,
            new HoldId(Guid.NewGuid()),
            new OwnerId("owner-a"),
            new Quantity(10),
            new HoldTtl(TimeSpan.FromMinutes(10)),
            now);

        var expiredHoldB = state.Acquire(
            resourceB.Id,
            new HoldId(Guid.NewGuid()),
            new OwnerId("owner-b"),
            new Quantity(10),
            new HoldTtl(TimeSpan.FromMinutes(1)),
            now);

        var later = now.AddMinutes(2);

        Assert.Throws<InsufficientCapacityException>(() =>
            state.Acquire(
                resourceA.Id,
                new HoldId(Guid.NewGuid()),
                new OwnerId("owner-c"),
                new Quantity(1),
                null,
                later));

        Assert.Equal(HoldStatus.Held, holdA.Status);

        // Even though B is reclaimable, an Acquire on A must not touch it.
        Assert.Equal(HoldStatus.Held, expiredHoldB.Status);

        Assert.Equal(Quantity.Zero, resourceA.AvailableQuantity);
        Assert.Equal(Quantity.Zero, resourceB.AvailableQuantity);
    }

    [Fact]
    public void Acquire_DoesNotReclaimHoldThatHasNotExpired()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource(capacity: 10);
        state.AddResource(resource);

        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        var activeHold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            new OwnerId("owner-1"),
            new Quantity(10),
            new HoldTtl(TimeSpan.FromMinutes(10)),
            now);

        var later = now.AddMinutes(2);

        Assert.Throws<InsufficientCapacityException>(() =>
            state.Acquire(
                resource.Id,
                new HoldId(Guid.NewGuid()),
                new OwnerId("owner-2"),
                new Quantity(1),
                null,
                later));

        Assert.Equal(HoldStatus.Held, activeHold.Status);
        Assert.Equal(new Quantity(10), resource.HeldQuantity);
        Assert.Equal(Quantity.Zero, resource.AvailableQuantity);
    }

    [Fact]
    public void Acquire_DoesNotReclaimConfirmedHold()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource(capacity: 10);
        state.AddResource(resource);

        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        var confirmedHold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            new OwnerId("owner-1"),
            new Quantity(10),
            new HoldTtl(TimeSpan.FromMinutes(1)),
            now);

        state.ConfirmHold(confirmedHold.Id, now);

        var later = now.AddMinutes(2);

        Assert.Throws<InsufficientCapacityException>(() =>
            state.Acquire(
                resource.Id,
                new HoldId(Guid.NewGuid()),
                new OwnerId("owner-2"),
                new Quantity(1),
                null,
                later));

        Assert.Equal(HoldStatus.Confirmed, confirmedHold.Status);
        Assert.Equal(Quantity.Zero, resource.HeldQuantity);
        Assert.Equal(new Quantity(10), resource.AllocatedQuantity);
        Assert.Equal(Quantity.Zero, resource.AvailableQuantity);
    }

    [Fact]
    public void AddCapacity_IncreasesResourceCapacity()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource(capacity: 10);
        state.AddResource(resource);

        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        state.AddCapacity(resource.Id, new Quantity(5), now);

        Assert.Equal(new Quantity(15), resource.Capacity);
        Assert.Equal(new Quantity(15), resource.AvailableQuantity);
    }

    [Fact]
    public void RemoveCapacity_DecreasesResourceCapacity()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource(capacity: 10);
        state.AddResource(resource);

        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        state.RemoveCapacity(resource.Id, new Quantity(4), now);

        Assert.Equal(new Quantity(6), resource.Capacity);
        Assert.Equal(new Quantity(6), resource.AvailableQuantity);
    }

    [Fact]
    public void RemoveCapacity_WhenCapacityWouldFallBelowHeldQuantity_Throws()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource(capacity: 10);
        state.AddResource(resource);

        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            new OwnerId("owner-1"),
            new Quantity(8),
            null,
            now);

        Assert.Throws<CapacityBelowCommittedException>(() =>
            state.RemoveCapacity(
                resource.Id,
                new Quantity(3),
                now));

        Assert.Equal(new Quantity(10), resource.Capacity);
        Assert.Equal(new Quantity(8), resource.HeldQuantity);
    }

    [Fact]
    public void PauseResource_PreventsNewAcquire()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource();
        state.AddResource(resource);

        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        state.PauseResource(resource.Id, now);

        Assert.Equal(ResourceStatus.Paused, resource.Status);

        Assert.Throws<InvalidResourceTransitionException>(() =>
            state.Acquire(
                resource.Id,
                new HoldId(Guid.NewGuid()),
                new OwnerId("owner-1"),
                new Quantity(1),
                null,
                now));
    }

    [Fact]
    public void ResumeResource_AllowsAcquireAgain()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource();
        state.AddResource(resource);

        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        state.PauseResource(resource.Id, now);
        state.ResumeResource(resource.Id, now);

        var hold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            new OwnerId("owner-1"),
            new Quantity(1),
            null,
            now);

        Assert.Equal(ResourceStatus.Active, resource.Status);
        Assert.Equal(HoldStatus.Held, hold.Status);
    }

    [Fact]
    public void CloseResource_PreventsNewAcquire()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource();
        state.AddResource(resource);

        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        state.CloseResource(resource.Id, now);

        Assert.Equal(ResourceStatus.Closed, resource.Status);

        Assert.Throws<InvalidResourceTransitionException>(() =>
            state.Acquire(
                resource.Id,
                new HoldId(Guid.NewGuid()),
                new OwnerId("owner-1"),
                new Quantity(1),
                null,
                now));
    }

    [Fact]
    public void UpdateResourcePolicies_NewAcquireUsesNewVersionWhileExistingHoldKeepsOldVersion()
    {
        var state = new SequentialAllocationState();
        var resource = CreateResource();
        state.AddResource(resource);

        var now = DateTimeOffset.Parse("2026-09-20T12:00:00Z");

        var firstHold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            new OwnerId("owner-1"),
            new Quantity(1),
            null,
            now);

        Assert.Equal(new PolicyVersion(1), firstHold.PolicyVersion);

        var newPolicies = new PolicySet(
            new PolicyVersion(2),
            resource.Policies.Request,
            resource.Policies.Owner,
            resource.Policies.Hold);

        state.UpdateResourcePolicies(
            resource.Id,
            newPolicies,
            now.AddMinutes(1));

        var secondHold = state.Acquire(
            resource.Id,
            new HoldId(Guid.NewGuid()),
            new OwnerId("owner-2"),
            new Quantity(1),
            null,
            now.AddMinutes(2));

        Assert.Equal(new PolicyVersion(1), firstHold.PolicyVersion);
        Assert.Equal(new PolicyVersion(2), secondHold.PolicyVersion);

        Assert.Equal(new PolicyVersion(2), resource.PolicyVersion);
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
            policies,
            DateTimeOffset.Parse("2026-09-20T12:00:00Z"));
    }
}
