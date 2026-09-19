# 00.6 — Atomic Requirements

Toute décision susceptible de violer un invariant doit être atomique vis-à-vis des données qui déterminent cet invariant.

## Acquire
Doivent appartenir à une décision cohérente :
- validation de capacité ;
- validation des Policies ;
- réservation de quantité ;
- création du Hold ;
- association au résultat idempotent.

## Confirm
Atomiquement : vérifier `HELD`, passer à `CONFIRMED`, déplacer la quantité de Held vers Allocated.

## Release
Atomiquement : vérifier `HELD`, passer à `RELEASED`, restituer la quantité.

## Reclaim
Atomiquement : vérifier que le Hold est `HELD` et reclaimable, passer à `EXPIRED`, restituer la quantité.

## Acquire-driven reclaim
Lorsqu’un Acquire récupère de la capacité pour lui-même, le reclaim nécessaire et la création du nouveau Hold appartiennent au **même atomic boundary**. Une autre requête ne peut pas voler la capacité spécifiquement récupérée pour cet Acquire.

Cela ne constitue pas une garantie FIFO globale entre Acquire concurrents.

## UpdateCapacity
La validation `Held + Allocated <= newCapacity` et la modification de Capacity doivent être atomiques.

## Policy version consistency
Un Acquire doit être évalué intégralement contre une seule version cohérente du PolicySet ; aucun mélange de versions n’est autorisé.
