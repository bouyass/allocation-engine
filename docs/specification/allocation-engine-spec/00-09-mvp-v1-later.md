# 00.9 — MVP / V1 / Later

## MVP
Promesse : plusieurs consommateurs concurrents peuvent réserver une capacité limitée sans surallocation, avec Holds temporaires, idempotence et récupération sûre de capacité abandonnée.

Inclus : Resource CRUD fonctionnel sans hard delete, Capacity, ACTIVE/PAUSED/CLOSED, Acquire/GetHold/Confirm/Release, atomicité, Acquire idempotency, TTL configurable, reclaim semantics, background GC, Acquire-driven atomic reclaim, policies de base (max quantity/request, max held/owner, max active holds/owner), tests d’invariants/concurrence, correctness multi-instance.

## V1
Observabilité, métriques/tracing/logs, audit, dashboard, live allocation stream, Load Lab, Chaos Lab, events/outbox, SDK .NET.

## Later
Allocation strategies avancées, FIFO/priorités fortes, waitlists, ressources uniques/sièges numérotés, bulk/multi-resource atomic Acquire, ExtendHold, deallocation/cancellation d’une allocation confirmée, capacity scheduling, quotas avancés, webhooks, multi-region, optimisations Redis, intégration paiement, plugins de policy.

## Scope rule
Une idée entre dans le MVP uniquement si elle est nécessaire pour prouver la promesse centrale. Sinon elle va au backlog.
