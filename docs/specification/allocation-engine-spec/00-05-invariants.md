# 00.5 — Invariants

Ces règles sont indépendantes de l’implémentation technique.

## Capacity conservation
- `Available >= 0`
- `Held >= 0`
- `Allocated >= 0`
- `Available + Held + Allocated = Capacity`
- `Held + Allocated <= Capacity`

## No over-allocation
Aucune concurrence, aucun retry, crash, GC ou nombre d’instances ne peut conduire à engager plus que `Capacity`.

## HELD owns capacity
Un Hold `HELD`, même après `ExpiresAt`, conserve sa capacité jusqu’à la réussite atomique de `HELD -> EXPIRED`.

## Single terminal transition
Un Hold ne peut atteindre qu’un seul état terminal. Une même quantité ne peut être confirmée et libérée, ni libérée plusieurs fois.

## Confirm vs Reclaim
`Confirm` et `Reclaim` sur le même Hold sont mutuellement exclusifs : un seul peut gagner la transition depuis `HELD`.

## Idempotency
Une même opération logique Acquire ne peut produire qu’un seul résultat sémantique. Même key + payload/fingerprint différent => `IDEMPOTENCY_CONFLICT`.

## Policies
Lorsqu’une policy est applicable, elle devient une contrainte de correctness. Exemples :
- `heldQuantity(owner, resource) <= maxHeldQuantityPerOwner`
- `activeHolds(owner, resource) <= maxActiveHoldsPerOwner`

## Policy changes
Une nouvelle Policy ne réécrit pas rétroactivement les Holds existants. Chaque décision est rattachable à une PolicyVersion.

## Capacity update
Une réduction immédiate de capacité n’est valide que si `newCapacity >= Held + Allocated`.

## Hold quantity
La quantité d’un Hold est immutable. Un reclaim expire le Hold entier ; il ne réduit jamais implicitement sa quantité.
