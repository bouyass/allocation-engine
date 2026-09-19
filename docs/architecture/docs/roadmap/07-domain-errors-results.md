# Phase 01.7 --- Domain Errors and Results

Distinguish Success, Business Rejection, and Technical Failure/Uncertain
Outcome.

Business rejections include RESOURCE_NOT_FOUND, RESOURCE_PAUSED,
RESOURCE_CLOSED, INVALID_QUANTITY, MAX_QUANTITY_EXCEEDED,
OWNER_HELD_QUANTITY_LIMIT_EXCEEDED, OWNER_ACTIVE_HOLD_LIMIT_EXCEEDED,
TTL_OUT_OF_RANGE, INSUFFICIENT_CAPACITY, IDEMPOTENCY_CONFLICT,
HOLD_NOT_FOUND, HOLD_EXPIRED, INVALID_TRANSITION and
CAPACITY_BELOW_COMMITTED.

A rejection means the engine successfully determined that the operation
is not allowed. Expected business outcomes should not normally use
exceptions for control flow.

Technical inability to determine state safely must never be converted
into a business rejection. The engine fails closed.

A commit may succeed while the response is lost. This creates an
uncertain outcome, not proof of failure. Acquire recovery uses the same
idempotency key/reconciliation.

Storage concurrency details remain internal when the engine can
eventually return a reliable business result. Domain errors are
transport-independent; HTTP mapping occurs later.

Retryability should be explicit at the API/SDK contract level.
IDEMPOTENCY_CONFLICT means the same key was reused for a different
logical request.
