# Phase 01.8 --- Domain Events

Candidate events: HoldCreated, HoldConfirmed, HoldReleased, HoldExpired,
ResourceCreated, ResourcePaused, ResourceResumed, ResourceClosed,
CapacityAdded, CapacityRemoved, PolicySetUpdated.

Events describe facts already produced. Domain Events and public
Integration Events are distinct concepts.

A durable-state event must not become externally observable before the
transaction producing that state commits. Reliable external publication
is deferred to Transactional Outbox.

Events must never restore or complete a core invariant asynchronously.
Confirm, for example, updates Hold state and Resource accounting in the
original atomic operation.

Rejected operations generally belong to telemetry rather than Domain
Events because no durable domain state changed.

Acquire-driven reclaim may produce HoldExpired(old) and HoldCreated(new)
from the same commit.

Do not automatically propagate opaque Metadata into events.
