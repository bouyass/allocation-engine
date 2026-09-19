# Allocation Engine — Knowledge Base

This directory contains the authoritative specification, architecture decisions, and implementation roadmap for Allocation Engine.

The documentation is part of the project source of truth.

When implementation and documentation disagree, do not silently choose one. Determine whether the implementation is incorrect or whether an explicit specification change is required.

---

# Start Here

Before modifying correctness-sensitive allocation behavior, read at minimum:

1. `specification/invariants.md`
2. `specification/state-machines.md`
3. `architecture/domain-operations.md`
4. `architecture/policies.md`
5. `architecture/interfaces-ports.md`

Also read the relevant Architecture Decision Records when the change affects an established architectural decision.

---

# Documentation Structure

```text
docs/
├── INDEX.md
│
├── specification/
│   ├── vision-and-scope.md
│   ├── glossary.md
│   ├── invariants.md
│   ├── state-machines.md
│   ├── failure-scenarios.md
│   ├── api-contract.md
│   └── definition-of-done.md
│
├── architecture/
│   ├── domain-boundaries.md
│   ├── aggregate-boundaries.md
│   ├── entities.md
│   ├── value-objects.md
│   ├── domain-operations.md
│   ├── policies.md
│   ├── errors-results.md
│   ├── domain-events.md
│   ├── interfaces-ports.md
│   └── domain-model-review.md
│
├── adr/
│   └── ...
│
└── roadmap/
    ├── README.md
    ├── phase-00-specification.md
    ├── phase-01-domain-model.md
    ├── phase-02-sequential-engine.md
    └── ...
```

---

# Specification

The specification defines **what Allocation Engine guarantees**.

Implementation choices must preserve these guarantees.

## `specification/vision-and-scope.md`

Defines:

* the problem Allocation Engine solves;
* project goals;
* responsibilities;
* explicit non-goals;
* MVP boundaries.

Read this when deciding whether a feature belongs in Allocation Engine.

## `specification/glossary.md`

Authoritative terminology for:

* Resource;
* Capacity;
* Available;
* Held;
* Allocated;
* Hold;
* Owner;
* Acquire;
* Confirm;
* Release;
* Reclaim;
* TTL;
* Policies;
* Idempotency.

Use these terms consistently in code, documentation and APIs.

## `specification/invariants.md`

One of the most important documents in the repository.

Defines properties that must never be violated, including capacity conservation, Hold accounting, terminal-state guarantees, idempotency guarantees and policy constraints.

Read this before changing:

* Acquire;
* Confirm;
* Release;
* Reclaim;
* capacity;
* owner limits;
* idempotency;
* concurrency;
* persistence;
* transaction boundaries.

**Never weaken an invariant merely to make an implementation or test easier.**

## `specification/state-machines.md`

Defines valid Resource and Hold transitions.

Read before modifying:

* Hold lifecycle;
* Resource status;
* Confirm;
* Release;
* Reclaim;
* Pause;
* Resume;
* Close.

## `specification/failure-scenarios.md`

Defines expected behavior under:

* process crashes;
* lost responses;
* retries;
* abandoned Holds;
* GC failures;
* multi-instance execution;
* shared storage failures;
* policy updates;
* reclaim races.

Read before implementing resilience, persistence, retries, idempotency, multi-instance execution or chaos tests.

## `specification/api-contract.md`

Defines the conceptual public API independently of transport details.

Includes operations such as:

* CreateResource;
* GetResource;
* AddCapacity;
* RemoveCapacity;
* UpdatePolicies;
* PauseResource;
* ResumeResource;
* CloseResource;
* Acquire;
* GetHold;
* Confirm;
* Release.

Read before changing API-facing behavior.

## `specification/definition-of-done.md`

Defines what is required before a correctness-sensitive feature is considered complete.

This includes nominal behavior, rejection paths, invariants, concurrency, retries, crash behavior, tests and documentation where applicable.

---

# Architecture

Architecture documents define **how the system is conceptually structured to satisfy the specification**.

## `architecture/domain-boundaries.md`

Defines what belongs inside the Allocation Engine domain and what remains the consumer application's responsibility.

## `architecture/aggregate-boundaries.md`

Defines Resource and Hold aggregate boundaries.

Important rule:

> Aggregate boundary is not the same thing as transactional boundary.

## `architecture/entities.md`

Defines the conceptual Resource and Hold models and their local state.

## `architecture/value-objects.md`

Defines concepts such as:

* ResourceId;
* HoldId;
* OwnerId;
* Quantity;
* PolicyVersion;
* Metadata;
* time values.

Read before introducing or changing domain primitive types.

## `architecture/domain-operations.md`

Defines the semantics of:

* Acquire;
* Confirm;
* Release;
* Reclaim;
* AddCapacity;
* RemoveCapacity;
* Pause;
* Resume;
* Close.

Also defines which operations require atomic coordination.

## `architecture/policies.md`

Defines the policy model and the distinction between pure constraints and transactional constraints.

Important rule:

> A value derived from shared mutable state cannot authorize a critical mutation unless its validity is guaranteed until commit of that mutation.

## `architecture/errors-results.md`

Defines:

* business rejection;
* technical failure;
* uncertain outcome;
* retry semantics;
* domain error behavior.

## `architecture/domain-events.md`

Defines Domain Event semantics.

Important rule:

> Domain Events describe committed facts. They must never be required to asynchronously restore a core invariant.

## `architecture/interfaces-ports.md`

Defines the requirements for application/infrastructure boundaries.

Important rule:

> A port used on the correctness path must describe the consistency guarantee it provides, not merely the data it returns.

## `architecture/domain-model-review.md`

Contains the consolidated Phase 01 review, accounting decisions and important concurrency interactions.

Read this when changing the overall domain model or designing persistence/concurrency behavior.

---

# Architecture Decision Records

`adr/` contains durable architectural decisions and their rationale.

ADRs should be created when a decision:

* significantly constrains future implementation;
* has meaningful alternatives;
* would otherwise be repeatedly reconsidered;
* affects system-wide correctness or architecture.

Examples include:

* Allocation Engine as allocation authority;
* business-agnostic domain;
* policy-driven constraints;
* reclaimable TTL semantics;
* hybrid reclaim strategy;
* Acquire idempotency model;
* correctness-before-availability strategy.

ADRs document **why** a decision exists, not merely what the current implementation does.

---

# Roadmap

`roadmap/` describes implementation sequencing.

The roadmap is not the authoritative definition of domain behavior.

If a roadmap document conflicts with the specification, the specification wins unless an explicit specification change is made.

Current phases:

```text
00  Specification
01  Domain Model
02  Sequential Engine
03  .NET API + PostgreSQL
04  Concurrency Core
05  Concurrency Test Suite
06  Idempotency
07  TTL + Expiration
08  Multi-instance
09  Resilience
10  Observability
11  Realtime Dashboard
12  Load Lab
13  Chaos Lab
14  Events + Transactional Outbox
15  SDKs
16  Redis Experiments
17  Security + Multi-tenancy
18  Packaging + Deployment
19  Public Documentation
20  Final Demonstration
```

---

# Reading Guide by Task

## Changing Acquire

Read:

1. `specification/invariants.md`
2. `architecture/domain-operations.md`
3. `architecture/policies.md`
4. `architecture/errors-results.md`
5. `architecture/interfaces-ports.md`

For concurrent Acquire behavior also read:

* `architecture/domain-model-review.md`
* `specification/failure-scenarios.md`

## Changing Confirm / Release

Read:

1. `specification/state-machines.md`
2. `specification/invariants.md`
3. `architecture/domain-operations.md`

If touching persistence or concurrency, also read `architecture/domain-model-review.md`.

## Changing expiration / reclaim

Read:

1. `specification/state-machines.md`
2. `specification/failure-scenarios.md`
3. `architecture/domain-operations.md`
4. `architecture/domain-model-review.md`

Pay particular attention to the `Confirm || Reclaim` race.

## Changing policies

Read:

1. `architecture/policies.md`
2. `specification/invariants.md`
3. `architecture/domain-operations.md`

Policy evaluation must never rely on an unprotected mutable snapshot.

## Changing persistence

Read:

1. `specification/invariants.md`
2. `architecture/interfaces-ports.md`
3. `architecture/domain-model-review.md`
4. `specification/failure-scenarios.md`

Persistence design follows required atomic boundaries.

Do not design repositories directly from database tables.

## Changing concurrency

Read:

1. `specification/invariants.md`
2. `architecture/domain-model-review.md`
3. `architecture/domain-operations.md`
4. `architecture/interfaces-ports.md`
5. `specification/failure-scenarios.md`

Correctness comes before throughput.

## Changing idempotency

Read:

1. `specification/failure-scenarios.md`
2. `architecture/errors-results.md`
3. `architecture/interfaces-ports.md`
4. relevant ADRs.

Acquire idempotency must be coordinated with allocation strongly enough that one logical operation cannot create multiple semantic allocations.

## Changing Domain Events

Read:

1. `architecture/domain-events.md`
2. `specification/invariants.md`

Never use asynchronous event processing to complete critical accounting.

---

# Core Engineering Principles

## Correctness First

A benchmark that violates an invariant is a failed benchmark regardless of throughput.

Optimization comes after correctness.

## Fail Closed

If Allocation Engine cannot safely determine whether an allocation is valid, it must not optimistically allocate from stale or uncertain state.

## Deterministic Domain

Domain behavior should remain deterministic.

Given the same:

```text
initial state
+ command
+ time
+ identifiers
```

the Domain should produce the same result and final state.

Domain code performs no network, database or filesystem I/O.

## Atomicity Follows Invariants

Do not choose transaction boundaries because they are convenient for repositories or database tables.

Start with the invariant and determine which state must remain coherent while the decision and mutation execute.

## No Process-Local Correctness

Allocation Engine is designed for multiple service instances.

In-process locks, static state or process-local synchronization cannot be relied upon for distributed correctness.

## Explicit Complexity

Do not introduce Redis, distributed locks, queues, actors, sharding or other infrastructure without identifying the precise problem they solve and measuring the simpler baseline first.

---

# Source of Truth Priority

When determining intended behavior, use this order:

```text
1. Explicit invariants
2. State machines
3. Specification
4. Accepted ADRs
5. Architecture documents
6. Tests
7. Implementation
8. Roadmap
```

Tests and implementation are evidence of current behavior, but they do not automatically override an explicit invariant.

If a contradiction is discovered, stop and resolve it explicitly rather than silently adapting one side.

---

# Agent Instructions

Coding agents must also read the repository-level:

`AGENTS.md`

`AGENTS.md` contains operational instructions for modifying the codebase.

This INDEX describes **where the project's knowledge lives**.

`AGENTS.md` describes **how an agent must work with that knowledge**.
