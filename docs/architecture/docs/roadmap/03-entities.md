# Phase 01.3 --- Entities

## Resource

Conceptual properties: ResourceId, Capacity, HeldQuantity,
AllocatedQuantity, Status, PolicySet, PolicyVersion, CreatedAt,
UpdatedAt.

`AvailableQuantity = Capacity - HeldQuantity - AllocatedQuantity` and is
derived.

Status: ACTIVE, PAUSED, CLOSED. ACTIVE \<-\> PAUSED; ACTIVE/PAUSED -\>
CLOSED; CLOSED terminal.

Capacity is changed only through `AddCapacity(q)` and
`RemoveCapacity(q)`, with `q > 0`. There is no generic UpdateCapacity.

RemoveCapacity requires
`Capacity - q >= HeldQuantity + AllocatedQuantity`; otherwise
`CAPACITY_BELOW_COMMITTED`.

## Hold

Properties: HoldId, ResourceId, OwnerId, Quantity, Status, CreatedAt,
ExpiresAt, PolicyVersion, Metadata.

Everything except Status is immutable after creation in MVP.

States: HELD -\> CONFIRMED \| RELEASED \| EXPIRED. All terminal.

`IsReclaimable(now) = Status == HELD && now >= ExpiresAt`.

Reclaimable does not mean expired: capacity remains reserved until
`HELD -> EXPIRED` succeeds atomically.

Time is passed explicitly to domain behavior; entities do not call the
system clock directly.
