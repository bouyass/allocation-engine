# Allocation Engine

Allocation Engine is a generic service for safely allocating scarce, quantity-based resources across concurrent consumers.

It provides a centralized allocation authority responsible for capacity management, temporary reservations, confirmation, expiration, idempotency, and allocation policies.

The engine is business-agnostic: it does not know whether a resource represents stock, seats, quotas, tickets, training capacity, or something else.

## Why?

Allocating limited capacity becomes difficult when several application instances operate concurrently.

A typical application may need to guarantee that:

* capacity is never over-allocated;
* concurrent requests respect allocation limits;
* retries do not create duplicate allocations;
* abandoned reservations eventually release capacity;
* multiple service instances can safely operate on the same resources;
* crashes and network failures never silently violate invariants.

Application-level locks are insufficient once multiple processes or instances are involved.

Allocation Engine centralizes this responsibility behind a small API.

## Core Model

### Resource

A `Resource` represents a limited quantity that can be allocated.

Examples:

* 100 concert tickets;
* 50 units of stock;
* 20 training seats;
* 10 API quota units.

Allocation Engine does not interpret the business meaning of the Resource.

### Hold

A `Hold` temporarily reserves a quantity from a Resource.

```text
HELD
 ├──> CONFIRMED
 ├──> RELEASED
 └──> EXPIRED
```

A confirmed Hold represents a definitive allocation in the MVP.

### Owner

An `Owner` identifies who or what requests an allocation.

It is an opaque identifier from Allocation Engine's perspective.

Examples may include a user, order, session, company, device, or another system.

### Policies

Resources can define generic allocation constraints such as:

* maximum quantity per Acquire;
* maximum held quantity per Owner;
* maximum active Holds per Owner;
* default Hold TTL;
* minimum and maximum TTL.

Business-specific eligibility rules remain outside Allocation Engine.

## Example

Given:

```text
Resource capacity = 100
```

A consumer can request:

```text
Acquire
Resource = R1
Owner = customer-42
Quantity = 3
TTL = 5 minutes
```

If accepted:

```text
Capacity  = 100
Held      = 3
Allocated = 0
Available = 97
```

The Hold can then be confirmed:

```text
Held      = 0
Allocated = 3
Available = 97
```

or released/reclaimed:

```text
Held      = 0
Allocated = 0
Available = 100
```

## Core Invariant

Allocation Engine must always preserve:

```text
Capacity = Available + Held + Allocated
```

with:

```text
Available >= 0
Held >= 0
Allocated >= 0
```

and therefore:

```text
Held + Allocated <= Capacity
```

Correctness takes precedence over throughput or availability.

A failed operation may return an error, require a retry, or temporarily reduce availability.

It must never silently violate an allocation invariant.

## Architecture

The project follows a domain-centered architecture:

```text
API
 │
 ▼
Application
 │
 ▼
Domain

Infrastructure
     │
     └── implements persistence and external capabilities
```

The Domain contains deterministic business behavior and does not depend on:

* ASP.NET;
* EF Core;
* PostgreSQL;
* infrastructure services;
* system clock access;
* network I/O.

Critical persistence and concurrency mechanisms are designed from the invariants and required atomic boundaries rather than from database tables.

```text
Invariants
    ↓
Atomic Boundaries
    ↓
Application Design
    ↓
Persistence Design
```

## Project Structure

```text
.
├── AGENTS.md
├── README.md
├── docs/
│   ├── INDEX.md
│   ├── specification/
│   ├── architecture/
│   ├── adr/
│   └── roadmap/
│
├── src/
│   ├── AllocationEngine.Domain/
│   └── AllocationEngine.Application/
│
└── tests/
    ├── AllocationEngine.Domain.Tests/
    └── AllocationEngine.Application.Tests/
```

## Documentation

The project documentation is part of the source of truth.

Start with:

```text
docs/INDEX.md
```

Documentation is organized into:

* `docs/specification/` — invariants, state machines, failure semantics and API contracts;
* `docs/architecture/` — domain model and architectural decisions;
* `docs/adr/` — Architecture Decision Records;
* `docs/roadmap/` — implementation phases and project progress.

`AGENTS.md` contains instructions for coding agents working on the repository.

## Development Roadmap

The project is intentionally developed in stages:

```text
00  Specification
01  Domain Model
02  Sequential Engine
03  .NET API + PostgreSQL
04  Concurrency Core
05  Concurrency Test Suite
06  Idempotency
07  TTL + Expiration
08  Multi-instance Execution
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

Concurrency optimizations and additional infrastructure are introduced only after a correct baseline exists.

## Build

Requirements:

* .NET 10 SDK

Restore and build:

```bash
dotnet restore
dotnet build
```

Run tests:

```bash
dotnet test
```

The repository contains a `global.json` defining the expected .NET SDK policy.

## Current Status

Phase 00 — Specification: completed.

Phase 01 — Domain Model: completed.

Phase 02 — Sequential Engine: in progress.

The current objective is to implement a deterministic sequential version of the allocation engine before introducing persistence and concurrent execution.

