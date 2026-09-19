# 00.12 — Final Coherence Review

## Scénarios validés
- Flash sale avec plusieurs pods consommateurs : aucune surallocation.
- Double retry de même Acquire : un seul résultat logique.
- Confirm vs Reclaim après TTL : un seul gagne.
- Resource pleine : Acquire-driven reclaim récupère et affecte atomiquement la capacité à la requête déclencheuse.
- Plusieurs instances Allocation Engine : aucune propriété critique ne dépend de l’instance API.

## Clarifications issues de la revue

### Reclaim is whole-Hold
Un Hold est indivisible. Si `quantity=4`, un reclaim expire les 4 unités ; il ne transforme pas silencieusement le Hold en quantity=1.

### Reclaim ordering
Aucune garantie forte FIFO/oldest-first dans le MVP. Le moteur peut choisir un Hold reclaimable valide. Les stratégies avancées sont reportées.

### No ExtendHold in MVP
L’extension introduit `Extend || Reclaim` et reste hors MVP.

### No hard delete
`Close(Resource)` est la fin fonctionnelle. La purge physique relève de politiques administratives/rétention futures.

### Confirmed is terminal in MVP
Une allocation confirmée reste consommée. Une future opération `Deallocate/CancelAllocation` sera distincte de `Release`.

### OwnerId required
Chaque Acquire possède un OwnerId opaque obligatoire, utile pour policies, audit, support et corrélation.

### Accounting authority remains an implementation decision
La spec définit les concepts `Available`, `Held`, `Allocated` et leurs invariants, mais ne décide pas encore quels compteurs sont persistés ou dérivés. La source d’autorité comptable sera décidée lors de la conception technique.

## Phase 00 conclusion
Le contrat fonctionnel et les garanties sont suffisamment cohérents pour démarrer la Phase 01 — Domain Model sans choisir prématurément l’infrastructure.
