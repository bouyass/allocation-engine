# Phase 01.10 --- Domain Model Review

Core MVP concepts retained: Resource, Hold, PolicySet, Value Objects,
Domain Errors/Results and Domain Events.

Do not add yet: separate Allocation entity, Owner entity, Reservation
entity, Resource.Holds collection, generic rule engine, custom policy
framework or OwnerAllocationState domain entity. A CONFIRMED Hold
represents definitive allocation in MVP.

`Available = Capacity - Held - Allocated`.

Resource operational counters are retained. Required consistency: -
HeldQuantity equals total quantity of HELD Holds. - AllocatedQuantity
equals total quantity of CONFIRMED Holds.

Every Hold transition affecting accounting updates Hold and Resource
accounting in the same atomic boundary. Exact physical accounting
representation is deferred.

Owner policies may later justify persisted accounting keyed by
`(ResourceId, OwnerId)`, but policy concepts and physical enforcement
structures remain separate.

## Concurrency matrix

Critical pairs include Acquire\|\|Acquire, Acquire\|\|Confirm,
Acquire\|\|Release, Acquire\|\|Reclaim, Acquire\|\|AddCapacity,
Acquire\|\|RemoveCapacity, Acquire\|\|Pause, Acquire\|\|Resume,
Acquire\|\|Close, Acquire\|\|UpdatePolicies, Confirm\|\|Reclaim,
Confirm\|\|Release, Release\|\|Reclaim,
RemoveCapacity\|\|RemoveCapacity, Close\|\|Resume,
UpdatePolicies\|\|UpdatePolicies and Acquire-driven-reclaim\|\|Confirm.

Coordination does not imply one global lock. Unrelated Resources should
not contend unnecessarily.

Confirm\|\|Reclaim: exactly one HELD-\>terminal transition wins.

Acquire\|\|Close: Acquire may win then Close; or Close may win and
Acquire returns RESOURCE_CLOSED. A stale pre-Close read cannot commit
afterward.

Acquire\|\|RemoveCapacity: if Acquire wins, removal may return
CAPACITY_BELOW_COMMITTED; if removal wins, Acquire may return
INSUFFICIENT_CAPACITY. Never commit Held+Allocated \> Capacity.

Owner limit race: if MaxActiveHoldsPerOwner=1, concurrent Acquire calls
for the same Resource/Owner cannot both create active Holds.

Acquire-driven reclaim\|\|Confirm: if Confirm wins, that Hold cannot be
reclaimed; if atomic reclaim+allocation wins, later Confirm returns
HOLD_EXPIRED.

## Architectural direction

`Invariants -> Atomic Boundaries -> Application Design -> Persistence Design`

Two explicit review corrections: 1. A locally expressible Entity
operation may still require transactional coordination. 2. Mutable
shared snapshots cannot authorize critical mutations unless protected
through commit.
