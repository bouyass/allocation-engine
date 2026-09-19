# 00.4 — State Machines

## Hold
Transitions autorisées :
- `HELD -> CONFIRMED` via Confirm.
- `HELD -> RELEASED` via Release.
- `HELD -> EXPIRED` via Reclaim.

`CONFIRMED`, `RELEASED` et `EXPIRED` sont terminaux.

### Idempotence des commandes terminales
- `Confirm(CONFIRMED)` : succès idempotent.
- `Release(RELEASED)` : succès idempotent.
- `Release(CONFIRMED)` : transition invalide.
- `Confirm(RELEASED)` : transition invalide.
- `Confirm(EXPIRED)` : `HOLD_EXPIRED`.
- `Release(EXPIRED)` : succès/no-op acceptable car l’intention de libération est déjà satisfaite.

## Expiration / lease semantics
`ExpiresAt` ne signifie pas « mort instantanément ». À partir de cet instant, le Hold devient **reclaimable**. Tant que son état reste `HELD`, il conserve sa capacité et peut encore être confirmé.

Seule la transition atomique `HELD -> EXPIRED` restitue réellement la capacité.

Deux mécanismes de reclaim :
1. Background GC périodique.
2. Acquire-driven reclaim lorsqu’une nouvelle allocation manque de capacité.

Confirm et Reclaim sont en compétition atomique : un seul gagne.

## TTL
Le consommateur choisit un TTL adapté à son workflow dans les limites de `HoldPolicy` (`defaultTtl`, `minTtl`, `maxTtl`). Confirm/Release explicites restent le workflow nominal ; TTL est un filet de sécurité pour les Holds abandonnés.

## Resource
- `ACTIVE <-> PAUSED`
- `ACTIVE -> CLOSED`
- `PAUSED -> CLOSED`
- `CLOSED` est terminal.

`ACTIVE`: toutes opérations normales. `PAUSED/CLOSED`: nouveaux Acquire refusés, mais Confirm/Release/Reclaim des Holds existants restent possibles.
