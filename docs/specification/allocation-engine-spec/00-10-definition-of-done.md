# 00.10 — Definition of Done

Une fonctionnalité critique n’est pas terminée lorsqu’elle fonctionne uniquement dans le scénario nominal.

## DoD critique
- comportement nominal implémenté ;
- validation et erreurs définies ;
- invariants affectés identifiés ;
- atomic boundary identifié ;
- races identifiées ;
- scénarios de crash identifiés ;
- retry/idempotence définis ;
- tests unitaires ;
- tests d’intégration ;
- tests de concurrence ;
- assertions d’invariants ;
- documentation mise à jour ;
- plus tard : métriques, traces, logs, load tests sur chemin critique.

## Invariant checker
L’infrastructure de test doit permettre de vérifier systématiquement la conservation de capacité, l’absence de surallocation, l’unicité des transitions terminales, les policies et l’idempotence.

## Repeated concurrency tests
Les scénarios concurrents doivent être exécutables de nombreuses fois afin d’explorer des interleavings différents.

## Rule for humans and AI agents
Ne jamais affaiblir/modifier un invariant uniquement pour faire passer une implémentation ou un test. Toute modification d’invariant est une décision de spécification explicite et documentée.

## Traceability
Les exigences critiques reçoivent des IDs stables (HOLD-xxx, CON-xxx, IDEM-xxx, TTL-xxx...) reliant requirement, implémentation, tests et documentation.
