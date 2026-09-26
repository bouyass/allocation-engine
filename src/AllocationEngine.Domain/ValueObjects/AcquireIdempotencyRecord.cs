namespace AllocationEngine.Domain.ValueObjects;

public sealed record AcquireIdempotencyRecord(
    IdempotencyKey Key,
    AcquireFingerprint Fingerprint,
    HoldId HoldId);
