# 00.1 — Vision & Scope

## Vision
Allocation Engine est un moteur générique permettant de réserver temporairement puis d’allouer de manière sûre une quantité limitée d’une ressource, y compris lorsque plusieurs consommateurs tentent d’obtenir cette ressource simultanément.

## Promesse centrale
Si une `Resource` possède une capacité de 100, aucune combinaison de requêtes concurrentes, retries, expirations ou instances multiples ne doit permettre au moteur d’engager plus de 100 unités.

## Modèle mental
`Resource -> Capacity -> Holds -> Confirm / Release / Expire`

Le moteur ignore la sémantique métier de la ressource : places de concert, stock, inscriptions, licences, machines, quotas API, etc.

## Responsabilités du moteur
- Resource et capacité.
- Holds temporaires.
- Allocation atomique.
- Concurrence.
- Expiration/reclaim.
- Idempotence.
- Policies génériques d’allocation.

## Hors périmètre métier
Le consommateur reste responsable des utilisateurs, paiements, commandes, catalogue, tarification, éligibilité métier et orchestration métier.

## Architecture policy-driven
Les contraintes génériques d’allocation doivent être configurables et extensibles sans modifier le cœur du moteur. Exemples : quantité maximale par Owner, nombre maximal de Holds actifs, TTL. Les règles métier (`age > 18`, abonnement Premium, etc.) restent chez le consommateur.
