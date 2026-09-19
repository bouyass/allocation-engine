# Phase 01.1 --- Domain Boundaries

## Scope

Allocation Engine owns Resource, Capacity, Hold, allocation state,
Policies, expiration/reclaim and generic allocation constraints.

It does not know Payment, Order, Customer, Concert, Hotel, Product or
other business-specific concepts. Metadata may store opaque external
references, but the domain never interprets it.

## Responsibilities

1.  Resource management.
2.  Hold lifecycle.
3.  Allocation correctness.

Conceptual layers: `API/SDK -> Application -> Domain -> Infrastructure`.

Application owns use-case/transaction/idempotency/reclaim orchestration.
Infrastructure owns persistence, atomic storage mechanisms and clock
implementation.

The domain expresses invariants and transitions. Distributed correctness
is not achieved merely by in-memory encapsulation. Resource has a
logical 1:N relationship to Holds but no `Resource.Holds` collection is
required.
