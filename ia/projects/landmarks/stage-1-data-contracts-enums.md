### Stage 1 — Catalog + data model + glossary/spec seed / Data contracts + enums

**Status:** In Progress (TECH-335, TECH-336, TECH-337, TECH-338 filed)

**Objectives:** Define the row type + gate discriminator + tier enum. No runtime logic — typed scaffolding that Steps 2–4 consume. Same Stage 1.1 shape as utilities — data lands before services.

**Exit:**

- `LandmarkTier` enum (`City`, `Region`, `Country`) with XML doc per value (city = base tier, region = post city→region transition, country = post region→country transition).
- `LandmarkPopGate` polymorphic — abstract base + two concrete subclasses `ScaleTransitionGate` (carries `fromTier`) and `IntraTierGate` (carries `pop`). Tagged for YAML deserialization (`kind: scale_transition` / `kind: intra_tier`).
- `LandmarkCatalogRow` serializable class w/ all 9 fields.
- Files compile clean (`npm run unity:compile-check`); no references from runtime code yet.
- Phase 1 — Tier enum + gate discriminator.
- Phase 2 — Catalog row class + compile check.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T1.1 | LandmarkTier enum | **TECH-335** | Draft | Add `Assets/Scripts/Data/Landmarks/LandmarkTier.cs` — `City`, `Region`, `Country` enum values. XML doc each value explaining scale coupling (region = unlocked on city→region scale transition). No behavior. |
| T1.2 | LandmarkPopGate discriminator | **TECH-336** | Draft | Add `Assets/Scripts/Data/Landmarks/LandmarkPopGate.cs` — abstract base + `ScaleTransitionGate { LandmarkTier fromTier }` + `IntraTierGate { int pop }`. YAML-deserializable via tag field `kind`. Unit test for YAML round-trip lands in T1.3.4. |
| T1.3 | LandmarkCatalogRow class | **TECH-337** | Draft | Add `Assets/Scripts/Data/Landmarks/LandmarkCatalogRow.cs` — serializable class w/ `id`, `displayName`, `tier`, `popGate`, `spritePath`, `commissionCost`, `buildMonths`, `utilityContributorRef` (nullable), `contributorScalingFactor` (default 1.0). XML doc each field. |
| T1.4 | Compile check + asmdef alignment | **TECH-338** | Draft | Run `npm run unity:compile-check`; ensure new types land in correct assembly (main asm unless Landmarks asmdef exists). No runtime refs yet — just compile green. |
