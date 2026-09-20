using AllocationEngine.Domain.ValueObjects;

namespace AllocationEngine.Domain.Policies;

public static class PolicyEvaluator
{
    public static PolicyEvaluationResult EvaluateRequest(
        RequestPolicy policy,
        Quantity requestedQuantity)
    {
        if (requestedQuantity == Quantity.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestedQuantity),
                "Requested quantity must be greater than zero.");
        }

        if (policy.MaxQuantityPerAcquire is { } maxQuantity &&
            requestedQuantity > maxQuantity)
        {
            return PolicyEvaluationResult.Rejected(
                PolicyRejection.MaxQuantityPerAcquireExceeded);
        }

        return PolicyEvaluationResult.Allowed();
    }

    public static PolicyEvaluationResult EvaluateOwner(
        OwnerPolicy policy,
        Quantity requestedQuantity,
        Quantity currentOwnerHeldQuantity,
        int currentOwnerActiveHolds)
    {
        if (requestedQuantity == Quantity.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestedQuantity),
                "Requested quantity must be greater than zero.");
        }

        if (currentOwnerActiveHolds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentOwnerActiveHolds),
                "Current owner active holds cannot be negative.");
        }

        if (policy.MaxActiveHolds is { } maxActiveHolds &&
            currentOwnerActiveHolds >= maxActiveHolds)
        {
            return PolicyEvaluationResult.Rejected(
                PolicyRejection.OwnerActiveHoldLimitExceeded);
        }

        if (policy.MaxHeldQuantity is { } maxHeldQuantity &&
            currentOwnerHeldQuantity + requestedQuantity > maxHeldQuantity)
        {
            return PolicyEvaluationResult.Rejected(
                PolicyRejection.OwnerHeldQuantityLimitExceeded);
        }

        return PolicyEvaluationResult.Allowed();
    }

    public static PolicyEvaluationResult EvaluateTtl(
    HoldPolicy policy,
    HoldTtl? requestedTtl)
    {
        if (requestedTtl is null)
        {
            return PolicyEvaluationResult.Allowed();
        }

        if (requestedTtl < policy.MinTtl ||
            requestedTtl > policy.MaxTtl)
        {
            return PolicyEvaluationResult.Rejected(
                PolicyRejection.TtlOutOfRange);
        }

        return PolicyEvaluationResult.Allowed();
    }

    public static HoldTtl ResolveTtl(
    HoldPolicy policy,
    HoldTtl? requestedTtl)
    {
        return requestedTtl ?? policy.DefaultTtl;
    }

    public static PolicyEvaluationResult EvaluateAcquire(
    PolicySet policies,
    Quantity requestedQuantity,
    Quantity currentOwnerHeldQuantity,
    int currentOwnerActiveHolds,
    HoldTtl? requestedTtl)
    {

        var requestResult = EvaluateRequest(
            policies.Request,
            requestedQuantity);

        if (!requestResult.IsAllowed)
        {
            return requestResult;
        }

        var ownerResult = EvaluateOwner(
            policies.Owner,
            requestedQuantity,
            currentOwnerHeldQuantity,
            currentOwnerActiveHolds);

        if (!ownerResult.IsAllowed)
        {
            return ownerResult;
        }

        var ttlResult = EvaluateTtl(
            policies.Hold,
            requestedTtl);

        if (!ttlResult.IsAllowed)
        {
            return ttlResult;
        }

        return PolicyEvaluationResult.Allowed();
    }
}
