# 00.3 — Resource / Hold Conceptual Model

## Resource
Propriétés conceptuelles :
- `ResourceId`
- `Capacity`
- `Status`: `ACTIVE`, `PAUSED`, `CLOSED`
- `PolicySet`
- `PolicyVersion`
- `CreatedAt`, `UpdatedAt`

`PAUSED` refuse les nouveaux Acquire mais laisse les Holds existants terminer. `CLOSED` interdit définitivement les nouveaux Acquire mais ne détruit pas les Holds existants.

## Capacity
Conceptuellement :
`Capacity = Available + Held + Allocated`.
La représentation persistée exacte sera décidée plus tard.

## Hold
- `HoldId`
- `ResourceId`
- `OwnerId` — obligatoire
- `Quantity` — immutable
- `Status`: `HELD`, `CONFIRMED`, `RELEASED`, `EXPIRED`
- `CreatedAt`
- `ExpiresAt`
- `PolicyVersion`
- `Metadata` opaque optionnelle

La metadata peut aider à la corrélation/audit mais n’est jamais interprétée par les Policies.

## Idempotency operation
L’Idempotency Key appartient conceptuellement à l’opération Acquire, pas au Hold, car un Acquire rejeté peut ne créer aucun Hold. Une opération idempotente contient au minimum : key, request fingerprint, résultat, timestamps et éventuellement HoldId.

## Allocation
Pour le MVP, une allocation confirmée est représentée par `Hold.Status == CONFIRMED`. Pas d’entité Allocation séparée.
