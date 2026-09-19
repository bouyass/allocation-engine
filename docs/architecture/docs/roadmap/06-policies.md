# Phase 01.6 --- Policy Model

Policies are generic Allocation Engine constraints and cannot depend on
external business concepts or interpret Metadata.

MVP PolicySet: - PolicyVersion - RequestPolicy: MaxQuantityPerAcquire? -
OwnerPolicy: MaxHeldQuantity?, MaxActiveHolds? - HoldPolicy: DefaultTtl,
MinTtl, MaxTtl

PolicySet is immutable and versioned. Updating it creates a new version.
Existing Holds retain their PolicyVersion and are not retroactively
invalidated. Each Acquire uses one coherent version.

## Pure constraints

Examples: positive requested quantity, MaxQuantityPerAcquire, TTL
bounds. These can be evaluated in memory.

## Transactional constraints

Examples: available capacity, owner held quantity, active Hold count,
Resource status and active PolicyVersion.

Their check and the mutation they authorize must share an appropriate
atomic boundary.

Do not pass `OwnerActiveHoldCount`, `OwnerHeldQuantity` or similar
mutable shared snapshots as ordinary trusted Acquire context fields
obtained before the transaction.

**A value derived from mutable shared state cannot authorize a critical
mutation unless its validity is guaranteed until that mutation
commits.**

Do not build a generic IPolicy/rule-engine framework until concrete
complexity justifies it.
