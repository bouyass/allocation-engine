# Phase 01.2 --- Aggregate Boundaries

-   Resource is an Aggregate Root.
-   Hold is a separate Aggregate Root.
-   Hold references Resource through ResourceId.
-   Resource does not contain a Hold collection.
-   PolicySet conceptually belongs to Resource for MVP.
-   Idempotency state is an application/persistence concern.

**Aggregate boundary != storage atomic boundary.**

Critical operations may atomically affect several aggregates: - Acquire:
Resource + new Hold. - Confirm: Resource + Hold. - Release: Resource +
Hold. - Reclaim: Resource + Hold. - Acquire-driven reclaim: Resource + N
Holds + new Hold.

Global Hold checks use shared durable authority. A naive
`SELECT SUM/COUNT -> decide -> INSERT` is race-prone unless the read
remains valid through commit.
