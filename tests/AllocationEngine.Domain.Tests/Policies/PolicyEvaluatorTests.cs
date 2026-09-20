using AllocationEngine.Domain.Policies;
using AllocationEngine.Domain.ValueObjects;

public class PolicyEvaluatorTests
{
    [Fact]
    public void EvaluateRequest_WhenNoMaximumExists_AllowsRequest()
    {
        var policy = new RequestPolicy();

        var result = PolicyEvaluator.EvaluateRequest(
            policy,
            new Quantity(1000));

        Assert.True(result.IsAllowed);
        Assert.Null(result.Rejection);
    }

    [Fact]
    public void EvaluateRequest_WhenQuantityIsBelowMaximum_AllowsRequest()
    {
        var policy = new RequestPolicy(
            new Quantity(10));

        var result = PolicyEvaluator.EvaluateRequest(
            policy,
            new Quantity(5));

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void EvaluateRequest_WhenQuantityEqualsMaximum_AllowsRequest()
    {
        var policy = new RequestPolicy(
            new Quantity(10));

        var result = PolicyEvaluator.EvaluateRequest(
            policy,
            new Quantity(10));

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void EvaluateRequest_WhenQuantityExceedsMaximum_RejectsRequest()
    {
        var policy = new RequestPolicy(
            new Quantity(10));

        var result = PolicyEvaluator.EvaluateRequest(
            policy,
            new Quantity(11));

        Assert.False(result.IsAllowed);

        Assert.Equal(
            PolicyRejection.MaxQuantityPerAcquireExceeded,
            result.Rejection);
    }

    [Fact]
    public void EvaluateOwner_WhenNoOwnerLimitsExist_AllowsAcquire()
    {
        var policy = new OwnerPolicy();

        var result = PolicyEvaluator.EvaluateOwner(
            policy,
            requestedQuantity: new Quantity(100),
            currentOwnerHeldQuantity: new Quantity(500),
            currentOwnerActiveHolds: 50);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void EvaluateOwner_WhenResultingHeldQuantityEqualsLimit_AllowsAcquire()
    {
        var policy = new OwnerPolicy(
            maxHeldQuantity: new Quantity(20));

        var result = PolicyEvaluator.EvaluateOwner(
            policy,
            requestedQuantity: new Quantity(5),
            currentOwnerHeldQuantity: new Quantity(15),
            currentOwnerActiveHolds: 0);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void EvaluateOwner_WhenResultingHeldQuantityExceedsLimit_RejectsAcquire()
    {
        var policy = new OwnerPolicy(
            maxHeldQuantity: new Quantity(20));

        var result = PolicyEvaluator.EvaluateOwner(
            policy,
            requestedQuantity: new Quantity(6),
            currentOwnerHeldQuantity: new Quantity(15),
            currentOwnerActiveHolds: 0);

        Assert.False(result.IsAllowed);

        Assert.Equal(
            PolicyRejection.OwnerHeldQuantityLimitExceeded,
            result.Rejection);
    }

    [Fact]
    public void EvaluateOwner_WhenActiveHoldCountIsBelowLimit_AllowsAcquire()
    {
        var policy = new OwnerPolicy(
            maxActiveHolds: 3);

        var result = PolicyEvaluator.EvaluateOwner(
            policy,
            requestedQuantity: new Quantity(1),
            currentOwnerHeldQuantity: Quantity.Zero,
            currentOwnerActiveHolds: 2);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void EvaluateOwner_WhenActiveHoldLimitIsReached_RejectsAcquire()
    {
        var policy = new OwnerPolicy(
            maxActiveHolds: 3);

        var result = PolicyEvaluator.EvaluateOwner(
            policy,
            requestedQuantity: new Quantity(1),
            currentOwnerHeldQuantity: Quantity.Zero,
            currentOwnerActiveHolds: 3);

        Assert.False(result.IsAllowed);

        Assert.Equal(
            PolicyRejection.OwnerActiveHoldLimitExceeded,
            result.Rejection);
    }

    [Fact]
    public void EvaluateTtl_WhenNoTtlIsRequested_Allows()
    {
        var policy = CreateHoldPolicy();

        var result = PolicyEvaluator.EvaluateTtl(
            policy,
            requestedTtl: null);

        Assert.True(result.IsAllowed);
        Assert.Null(result.Rejection);
    }

    [Fact]
    public void EvaluateTtl_WhenRequestedTtlIsWithinRange_Allows()
    {
        var policy = CreateHoldPolicy();

        var result = PolicyEvaluator.EvaluateTtl(
            policy,
            new HoldTtl(TimeSpan.FromMinutes(10)));

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void EvaluateTtl_WhenRequestedTtlEqualsMinimum_Allows()
    {
        var policy = CreateHoldPolicy();

        var result = PolicyEvaluator.EvaluateTtl(
            policy,
            new HoldTtl(TimeSpan.FromMinutes(1)));

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void EvaluateTtl_WhenRequestedTtlEqualsMaximum_Allows()
    {
        var policy = CreateHoldPolicy();

        var result = PolicyEvaluator.EvaluateTtl(
            policy,
            new HoldTtl(TimeSpan.FromMinutes(30)));

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void EvaluateTtl_WhenRequestedTtlIsBelowMinimum_Rejects()
    {
        var policy = CreateHoldPolicy();

        var result = PolicyEvaluator.EvaluateTtl(
            policy,
            new HoldTtl(TimeSpan.FromSeconds(30)));

        Assert.False(result.IsAllowed);
        Assert.Equal(
            PolicyRejection.TtlOutOfRange,
            result.Rejection);
    }

    [Fact]
    public void EvaluateTtl_WhenRequestedTtlExceedsMaximum_Rejects()
    {
        var policy = CreateHoldPolicy();

        var result = PolicyEvaluator.EvaluateTtl(
            policy,
            new HoldTtl(TimeSpan.FromMinutes(31)));

        Assert.False(result.IsAllowed);
        Assert.Equal(
            PolicyRejection.TtlOutOfRange,
            result.Rejection);
    }

    [Fact]
    public void ResolveTtl_WhenNoTtlIsRequested_ReturnsDefaultTtl()
    {
        var policy = CreateHoldPolicy();

        var result = PolicyEvaluator.ResolveTtl(
            policy,
            requestedTtl: null);

        Assert.Equal(
            new HoldTtl(TimeSpan.FromMinutes(5)),
            result);
    }

    [Fact]
    public void ResolveTtl_WhenTtlIsRequested_ReturnsRequestedTtl()
    {
        var policy = CreateHoldPolicy();

        var requestedTtl =
            new HoldTtl(TimeSpan.FromMinutes(10));

        var result = PolicyEvaluator.ResolveTtl(
            policy,
            requestedTtl);

        Assert.Equal(requestedTtl, result);
    }

    [Fact]
    public void EvaluateAcquire_WhenAllPoliciesAreSatisfied_Allows()
    {
        var policies = CreatePolicySet();

        var result = PolicyEvaluator.EvaluateAcquire(
            policies,
            requestedQuantity: new Quantity(5),
            currentOwnerHeldQuantity: new Quantity(10),
            currentOwnerActiveHolds: 1,
            requestedTtl: new HoldTtl(
                TimeSpan.FromMinutes(10)));

        Assert.True(result.IsAllowed);
        Assert.Null(result.Rejection);
    }

    [Fact]
    public void EvaluateAcquire_WhenRequestLimitIsExceeded_Rejects()
    {
        var policies = CreatePolicySet();

        var result = PolicyEvaluator.EvaluateAcquire(
            policies,
            requestedQuantity: new Quantity(11),
            currentOwnerHeldQuantity: Quantity.Zero,
            currentOwnerActiveHolds: 0,
            requestedTtl: null);

        Assert.False(result.IsAllowed);

        Assert.Equal(
            PolicyRejection.MaxQuantityPerAcquireExceeded,
            result.Rejection);
    }

    [Fact]
    public void EvaluateAcquire_WhenOwnerHeldQuantityLimitIsExceeded_Rejects()
    {
        var policies = CreatePolicySet();

        var result = PolicyEvaluator.EvaluateAcquire(
            policies,
            requestedQuantity: new Quantity(5),
            currentOwnerHeldQuantity: new Quantity(18),
            currentOwnerActiveHolds: 1,
            requestedTtl: null);

        Assert.False(result.IsAllowed);

        Assert.Equal(
            PolicyRejection.OwnerHeldQuantityLimitExceeded,
            result.Rejection);
    }

    [Fact]
    public void EvaluateAcquire_WhenOwnerActiveHoldLimitIsReached_Rejects()
    {
        var policies = CreatePolicySet();

        var result = PolicyEvaluator.EvaluateAcquire(
            policies,
            requestedQuantity: new Quantity(5),
            currentOwnerHeldQuantity: new Quantity(10),
            currentOwnerActiveHolds: 3,
            requestedTtl: null);

        Assert.False(result.IsAllowed);

        Assert.Equal(
            PolicyRejection.OwnerActiveHoldLimitExceeded,
            result.Rejection);
    }

    [Fact]
    public void EvaluateAcquire_WhenTtlIsOutOfRange_Rejects()
    {
        var policies = CreatePolicySet();

        var result = PolicyEvaluator.EvaluateAcquire(
            policies,
            requestedQuantity: new Quantity(5),
            currentOwnerHeldQuantity: new Quantity(10),
            currentOwnerActiveHolds: 1,
            requestedTtl: new HoldTtl(
                TimeSpan.FromMinutes(45)));

        Assert.False(result.IsAllowed);

        Assert.Equal(
            PolicyRejection.TtlOutOfRange,
            result.Rejection);
    }

    [Fact]
    public void EvaluateAcquire_WhenTtlIsNotSpecified_AllowsUsingPolicyDefault()
    {
        var policies = CreatePolicySet();

        var result = PolicyEvaluator.EvaluateAcquire(
            policies,
            requestedQuantity: new Quantity(5),
            currentOwnerHeldQuantity: new Quantity(10),
            currentOwnerActiveHolds: 1,
            requestedTtl: null);

        Assert.True(result.IsAllowed);

        var effectiveTtl = PolicyEvaluator.ResolveTtl(
            policies.Hold,
            requestedTtl: null);

        Assert.Equal(
            policies.Hold.DefaultTtl,
            effectiveTtl);
    }

    private static HoldPolicy CreateHoldPolicy()
    {
        return new HoldPolicy(
            defaultTtl: new HoldTtl(TimeSpan.FromMinutes(5)),
            minTtl: new HoldTtl(TimeSpan.FromMinutes(1)),
            maxTtl: new HoldTtl(TimeSpan.FromMinutes(30)));
    }

    private static PolicySet CreatePolicySet()
    {
        return new PolicySet(
            PolicyVersion.Initial,
            new RequestPolicy(
                maxQuantityPerAcquire: new Quantity(10)),
            new OwnerPolicy(
                maxHeldQuantity: new Quantity(20),
                maxActiveHolds: 3),
            CreateHoldPolicy());
    }
}