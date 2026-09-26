namespace AllocationEngine.Domain.ValueObjects;

public readonly record struct AcquireFingerprint(
    ResourceId ResourceId,
    OwnerId OwnerId,
    Quantity Quantity,
    HoldTtl? RequestedTtl);
