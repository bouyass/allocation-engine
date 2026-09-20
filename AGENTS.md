# MAIN RULE
NEVER AND EVER DO SOMEHTING NOT EXPLICITLY ASKED AND REQUESTED

# Allocation Engine — Agent Instructions

## Knowledge base

Before making architectural or correctness-sensitive changes, read:

- `docs/INDEX.md`
- `docs/specification/invariants.md`
- relevant documents under `docs/architecture/`

Do not invent behavior when the specification already defines it.

## Core rule

Correctness takes precedence over throughput, availability, or implementation convenience.

Never weaken an invariant to make a test pass.

## Critical allocation behavior

Changes involving any of the following are correctness-sensitive:

- Acquire
- Confirm
- Release
- Reclaim
- capacity accounting
- owner limits
- idempotency
- TTL
- Resource status
- policy versions
- concurrency
- transaction boundaries

For these changes, inspect the relevant specification before implementation.

## Architecture

Dependency direction:

API / Infrastructure -> Application -> Domain

Domain must not depend on:
- EF Core
- ASP.NET
- PostgreSQL/Npgsql
- Infrastructure
- Application

Domain behavior must remain deterministic and perform no I/O.

## Time and IDs

Do not call `DateTime.UtcNow`, `Guid.NewGuid()` or equivalent nondeterministic APIs from Domain entities.

Time and generated identifiers are supplied by the caller.

## Concurrency

Never assume an in-process lock provides system correctness.

The service must eventually support multiple Allocation Engine instances.

Never implement:

read shared mutable state
-> make critical decision
-> write later

unless the validity of that state is guaranteed through the atomic commit.

## Tests

Every correctness-sensitive change must test:
- nominal behavior
- rejection behavior
- affected invariants
- relevant state transitions

Concurrency-sensitive behavior additionally requires concurrency/invariant tests when that phase is implemented.

## Forbidden shortcuts

Do not:
- add `Resource.Holds`
- interpret opaque Metadata in Domain policies
- introduce business-specific concepts into Domain
- silently clamp invalid quantities
- use events to asynchronously repair core accounting
- introduce Redis/distributed locks without a documented problem and decision
