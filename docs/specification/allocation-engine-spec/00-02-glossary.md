# 00.2 — Glossary

- **Resource** : élément abstrait dont le moteur contrôle une capacité limitée.
- **Capacity** : quantité totale que la Resource peut allouer.
- **Available** : quantité disponible pour de nouveaux Holds.
- **Held** : quantité actuellement réservée par des Holds actifs.
- **Allocated** : quantité définitivement allouée via des Holds confirmés.
- **Owner** : identifiant opaque de l’entité pour laquelle une allocation est demandée. Ce n’est pas nécessairement un utilisateur humain.
- **Acquire** : commande demandant de réserver temporairement une quantité d’une Resource.
- **Hold** : réservation temporaire créée par un Acquire réussi.
- **Quantity** : nombre d’unités porté par un Hold. Cette quantité est immutable pendant le cycle de vie du Hold.
- **Confirm** : transforme un Hold actif en allocation définitive.
- **Release** : abandon explicite d’un Hold actif et restitution de sa capacité.
- **Expire/Reclaim** : récupération par le moteur d’un Hold devenu reclaimable.
- **TTL** : durée choisie par le consommateur, dans les limites de la policy, après laquelle un Hold devient reclaimable.
- **Idempotency Key** : identité fournie par le consommateur pour une opération logique Acquire.
- **Policy** : contrainte générique configurable relative à l’allocation.
- **Policy Set** : ensemble cohérent de Policies appliquées à une Resource.
- **Policy Version** : version du Policy Set utilisée pour une décision.
- **Allocation** : capacité définitivement consommée après confirmation d’un Hold.

## Distinctions importantes
- `Resource` n’est pas un objet métier.
- `Owner` n’est pas nécessairement un User.
- `Acquire` est une commande ; `Hold` est un résultat possible.
- `Release` est explicite ; `Expire` résulte d’un reclaim par le moteur.
- Même Owner + nouvelle Idempotency Key = nouvelle intention potentielle.
- Même Idempotency Key = retry de la même intention.
