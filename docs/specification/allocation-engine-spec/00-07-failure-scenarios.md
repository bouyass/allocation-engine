# 00.7 — Failure Scenarios

## Lost Acquire response
Si Acquire a été commité mais la réponse perdue, un retry avec la même Idempotency Key retourne le même résultat/Hold sans consommer davantage de capacité.

## Crash during Acquire
État durable autorisé : Acquire entièrement engagé ou aucun effet. Jamais de capacité Held sans Hold correspondant.

## Consumer crash after Acquire
Le Hold reste `HELD`, devient reclaimable après TTL, puis peut être récupéré par GC ou Acquire-driven reclaim.

## External business success before missing Confirm
Si paiement/opération métier réussit mais le consommateur crash avant Confirm, Allocation Engine ne peut pas le deviner. Le consommateur est responsable de recovery/reconciliation/compensation.

## Lost Confirm/Release response
Retries convergent vers le même état grâce à l’idempotence naturelle des transitions.

## Slow consumer
Après ExpiresAt, Confirm peut encore réussir tant que le Hold reste `HELD`. S’il a déjà été reclaimé, Confirm retourne `HOLD_EXPIRED`.

## GC crash
N’affecte pas la correctness : les Holds reclaimables restent réservés. Il s’agit d’une dégradation d’efficacité, pas d’une violation d’invariant.

## Multiple GC workers
Un seul peut réussir `HELD -> EXPIRED`; capacité restituée une seule fois.

## Crash during Acquire-driven reclaim
État durable autorisé : soit ancien Hold toujours `HELD` et nouvel Acquire absent, soit ancien Hold `EXPIRED` et nouveau Hold créé. Jamais reclaim seul si celui-ci faisait partie de l’allocation atomique de la requête.

## Allocation Engine instance loss
Les Holds ne sont pas propriétaires d’une instance API. Une autre instance doit pouvoir reprendre Confirm/Release/Acquire.

## Authority unavailable
Fail closed : si les invariants ne peuvent pas être vérifiés, retourner indisponibilité plutôt que faire une allocation spéculative.

## Retry storm
Peut dégrader les performances mais ne doit jamais multiplier les résultats logiques d’une même Idempotency Key.

## Policy update race
Une opération utilise une version cohérente complète, ancienne ou nouvelle selon l’ordre atomique, jamais un mélange.
