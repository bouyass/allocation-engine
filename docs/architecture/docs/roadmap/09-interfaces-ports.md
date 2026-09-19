# Phase 01.9 --- Interfaces and Ports

Ports are defined from required consistency guarantees, not future
database tables.

A generic `Get -> decide -> Save` workflow is unsafe on the correctness
path when the read may become stale before the write. A Unit of Work
alone does not solve this.

Separate non-critical readers (GET Resource/Hold, dashboard/admin views)
from the authoritative correctness path.

Critical persistence must support the atomic authority needed for
Resource state, Hold state, owner usage, PolicyVersion, reclaim and
idempotency without exposing PostgreSQL-specific mechanics prematurely.

Do not yet choose row locks, SERIALIZABLE, optimistic concurrency, CAS,
etc.

Use a Clock abstraction for deterministic time tests; exact .NET time
types remain undecided.

Idempotency check + allocation + Hold creation + idempotency result must
be coordinated atomically enough to prevent duplicate semantic
allocations.

Reclaim candidate discovery grants no right to expire a Hold.
Eligibility and HELD -\> EXPIRED are revalidated atomically.

**A port used on the correctness path must describe the consistency
guarantee it provides, not merely the data it returns.**
