# Phase 01.5 --- Domain Operations

## Core correction

**Locality of a business rule does not imply absence of transactional
requirements.**

Any operation modifying data used by another operation to preserve an
invariant must be atomically coordinated with that operation.

## Resource operations

-   AddCapacity(q): q \> 0; ACTIVE/PAUSED allowed; CLOSED rejected.
-   RemoveCapacity(q): q \> 0; requires resulting Capacity \>= Held +
    Allocated; CLOSED rejected.
-   Pause: ACTIVE -\> PAUSED; PAUSED is idempotent no-op.
-   Resume: PAUSED -\> ACTIVE; ACTIVE is idempotent no-op; CLOSED
    invalid.
-   Close: ACTIVE/PAUSED -\> CLOSED; CLOSED idempotent no-op.

PAUSED/CLOSED prevent new Acquire, but existing Holds may still
Confirm/Release/Reclaim.

## Critical operations

Acquire is an application-orchestrated transactional operation depending
on authoritative Resource state, policies, owner usage, reclaimable
Holds, idempotency and concurrency.

Confirm atomically performs HELD -\> CONFIRMED, `Held -= q`,
`Allocated += q`. CONFIRMED retry succeeds; RELEASED invalid; EXPIRED
returns HOLD_EXPIRED. TTL passing alone does not prevent Confirm while
status remains HELD.

Release atomically performs HELD -\> RELEASED and `Held -= q`. RELEASED
and EXPIRED are success/no-op; CONFIRMED invalid.

Reclaim requires HELD and `now >= ExpiresAt`, then atomically performs
HELD -\> EXPIRED and `Held -= q`.

Acquire-driven reclaim must reclaim candidate Holds and create the
requesting Hold in one atomic boundary. Reclaimed capacity needed by
that Acquire cannot become observable to competing requests first.

An in-memory transition is not a successful system operation until its
required durable atomic operation commits.
