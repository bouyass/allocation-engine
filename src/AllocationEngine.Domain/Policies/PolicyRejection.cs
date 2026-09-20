namespace AllocationEngine.Domain.Policies;

public enum PolicyRejection
{
    MaxQuantityPerAcquireExceeded,
    OwnerHeldQuantityLimitExceeded,
    OwnerActiveHoldLimitExceeded,
    TtlOutOfRange
}