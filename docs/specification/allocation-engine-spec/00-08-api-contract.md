# 00.8 — Conceptual API Contract

Le contrat reste indépendant de REST/gRPC et des codes HTTP précis.

## Resource operations
- `CreateResource`
- `GetResource`
- `UpdateCapacity`
- `UpdatePolicies`
- `PauseResource`
- `ResumeResource`
- `CloseResource`

Pas de hard-delete fonctionnel dans le MVP.

## Allocation operations
- `Acquire(resourceId, ownerId, quantity, idempotencyKey, ttl?, metadata?)`
- `GetHold(holdId)`
- `Confirm(holdId)`
- `Release(holdId)`

Reclaim/Expire est principalement une mécanique interne.

## Acquire success
Retourne notamment HoldId, ResourceId, OwnerId, Quantity, Status, ExpiresAt et PolicyVersion. `ExpiresAt` est calculé par le moteur.

## Machine-readable failures
Exemples :
- `INSUFFICIENT_CAPACITY`
- `OWNER_LIMIT_EXCEEDED`
- `MAX_QUANTITY_EXCEEDED`
- `RESOURCE_PAUSED`
- `RESOURCE_CLOSED`
- `TTL_OUT_OF_RANGE`
- `IDEMPOTENCY_CONFLICT`

## Idempotency scope
La clé doit être namespacée au minimum par tenant/client et type d’opération. Elle n’est pas globalement unique à toute la plateforme.

## Confirm / Release idempotency
Pas d’Idempotency Key dédiée nécessaire pour le MVP : HoldId + state machine fournissent une identité naturelle.

## Operation lookup
Le modèle doit permettre ultérieurement un lookup d’opération par Idempotency Key pour reconciliation/debugging, même si l’endpoint n’est pas obligatoire dans le MVP.
