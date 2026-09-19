# Phase 01.4 --- Value Objects

Use strongly typed identifiers such as ResourceId and HoldId around an
underlying UUID/Guid to prevent accidental ID interchange at compile
time.

OwnerId is an opaque, non-empty, bounded string-like identifier.
Allocation Engine never interprets its business meaning.

Use one non-negative `Quantity` value object. Zero is valid for
accounting counters. Operations such as Acquire, Hold creation,
AddCapacity and RemoveCapacity enforce `quantity > 0`.

Avoid separate Capacity/HeldQuantity/PositiveQuantity types until
concrete complexity justifies them.

PolicyVersion is strongly typed and conceptually \>= 1.

Instant and Duration are conceptual time types; no .NET time library is
selected yet. Acquire may receive a Duration, while an existing Hold
stores authoritative ExpiresAt.

Metadata is opaque, immutable and bounded; policies never interpret it.

IdempotencyKey and RequestFingerprint belong primarily to
application/idempotency concerns.
