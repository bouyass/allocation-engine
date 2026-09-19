# 00.11 — Initial Architecture Decision Records

## ADR-001 — Allocation Engine is the allocation authority
Toute allocation d’une Resource gérée passe par le moteur. Les pods consommateurs n’ont pas à se coordonner directement.

## ADR-002 — Business-agnostic engine
Le moteur connaît Resource, Owner, Capacity, Quantity, Hold et Policies, mais pas Customer, Payment, Order, Concert, Product, etc.

## ADR-003 — Policy-driven architecture
Les contraintes génériques sont configurables/extensibles ; les règles métier restent hors moteur.

## ADR-004 — Reclaim-based expiration
ExpiresAt rend un Hold reclaimable. Seule `HELD -> EXPIRED` libère la capacité. Background GC et Acquire-driven reclaim sont supportés conceptuellement.

## ADR-005 — Consumer-defined idempotent intention
L’Idempotency Key représente l’intention logique définie par le consommateur. Le moteur ne déduit pas l’intention depuis l’égalité des payloads.

## ADR-006 — Stateless Allocation Engine API instances
La correctness ne dépend pas d’un lock ou état mémoire local d’une instance. Le passage de 1 à N instances ne change pas les garanties.

## ADR-007 — Correctness before availability
En cas d’impossibilité de vérifier les invariants, le moteur échoue fermé plutôt que d’allouer de façon spéculative.

Ces ADR ne choisissent volontairement ni PostgreSQL, ni Redis, ni Kafka, ni Kubernetes. Les technologies viendront après les contraintes.
