# Cost catalog — exploration

> **Status:** Stub — seeded as pre-plan exploration for Bucket 11 of `full-game-mvp-master-plan.md`. Awaits `/design-explore docs/cost-catalog-exploration.md` run (answer interview poll → approach select → expansion). Once locked, feeds `/master-plan-new docs/cost-catalog-exploration.md` to author `ia/projects/cost-catalog-master-plan.md`.
>
> **Parent umbrella:** [`ia/projects/full-game-mvp-master-plan.md`](../ia/projects/full-game-mvp-master-plan.md) — Bucket 11 row + cross-cutting rule.
>
> **Sibling pre-plan:** [`docs/citystats-overhaul-exploration.md`](citystats-overhaul-exploration.md) — same pre-plan pattern, different domain.

---

## Problem

Game-balance constants (construction cost, maintenance rate, bond ceiling, tax floor, utility pool unit cost, landmark cost + footprint, big-project savings threshold, service budget floor) are hardcoded today across many per-entity files: `BuildingPlacer`, `ZoneManager`, `UtilityPoolService` (planned), `LandmarkPlacementService` (planned), `BondIssuer` (planned), `TaxController` (planned), plus ScriptableObject data rows per-building / per-zone. Review requires grep across N files + guesswork about which constant belongs where. Balance iteration is slow, error-prone, and review-resistant.

The MVP (`full-game-mvp-exploration.md`) spawns several new cost consumers across Buckets 2 / 3 / 4 / 6. Without a central catalog, the cost matrix grows in N uncoordinated places. Balance decisions become implicit (the values scattered in code), not explicit (one reviewable file).

**Target:** one central `CostTable` asset storing every cost / footprint constant. Every consumer reads via `ICostCatalog.Get(row)`. Editor UI (custom drawer or dedicated window) renders the full matrix for review + tuning. Values remain design-time constants — no runtime mutation, no dynamic pricing, no formula scaling. Only the storage location moves.

**Scope boundary (locked upfront):**

- IN: storage schema + read API + editor UI + migration of existing hardcoded constants + parity tests.
- OUT: dynamic / population-scaled / tier-scaled pricing; economy-reactive pricing (demand modifiers, inflation); modder-authored overrides; in-game balance editor; runtime cost mutation via events; localisation of cost display.

---

## Approaches surveyed

Three viable approaches for the catalog storage + access pattern. Each listed with Pros / Cons. Approach D (hybrid) listed as an explicit composition to clarify the decision surface.

### Approach A — single flat `CostTable.asset` with enum row keys

One ScriptableObject asset `CostTable.asset` under `Assets/ScriptableObjects/Balance/`. Body = `List<CostRow>` where `CostRow` is a struct { `CostRowId id` (enum), `int value`, `Vector2Int footprint` (optional), `string note` (inspector doc) }. Read API: `ICostCatalog.Get(CostRowId id) -> CostRow`. All categories (zones, utilities, landmarks, bonds, tax, maintenance) share the single enum namespace (e.g. `CostRowId.ZoneRResidentialSpawn`, `CostRowId.UtilityWaterUnitCost`, `CostRowId.LandmarkTownHallCost`).

**Pros.** Simplest possible schema. One asset to review. Enum type-safety at call sites. Migration is mechanical (hardcoded number → `catalog.Get(CostRowId.X).value`). Custom property drawer on `CostRow` renders all rows in one Inspector scroll. Grep-friendly — every row key searchable across consumers.

**Cons.** Enum grows large (estimated 40–80 rows at MVP scope). Recompile hit when adding rows (enum change). Row taxonomy mixes categories in one flat namespace — Inspector review scroll gets long. No compile-time enforcement that a consumer in Bucket 2 reads only Bucket 2 rows (cross-bucket misuse possible).

### Approach B — per-category ScriptableObject assets + category interfaces

Multiple ScriptableObject assets — `ZoneCostTable.asset`, `UtilityCostTable.asset`, `LandmarkCostTable.asset`, `FinanceCostTable.asset`. Each holds its own `List<CostRow>` + own enum `ZoneCostRowId` / `UtilityCostRowId` / `LandmarkCostRowId` / `FinanceCostRowId`. Read API: `IZoneCostCatalog.Get(ZoneCostRowId)` + `IUtilityCostCatalog.Get(UtilityCostRowId)` + ... Each consumer depends on its category interface only. `CostCatalogService` MonoBehaviour holds refs to all tables + implements all category interfaces.

**Pros.** Clean category separation — Bucket 2 never sees Bucket 3's enum. Adding a row in one category only recompiles that enum. Inspector scrolls stay short (per-category view). Matches domain boundaries (zones vs utilities vs landmarks vs finance).

**Cons.** N assets + N enums + N interfaces to maintain. Editor UI for "whole cost matrix" becomes multi-pane (4 inspectors or one custom window aggregating 4 tables). Balance review loses "one scroll shows all" virtue of Approach A. Cross-category comparisons (is landmark cost 10× zone cost?) require bouncing between assets.

### Approach C — scriptable data rows embedded on existing entity assets (no central table)

Each existing entity ScriptableObject (e.g. `ZoneRDefinition.asset`, `UtilityPoolDefinition.asset`, `LandmarkDefinition.asset`) grows a `CostField` (new serialized struct) holding the cost + footprint + maintenance for that entity. No central table — the "catalog" is the union of all entity assets. Editor UI = new `CostMatrixWindow` that scans the project for entity assets with `CostField` + renders aggregate view.

**Pros.** Cost data lives next to the entity it governs (co-location principle). No enum growth — the entity asset IS the key. Migration is lightest (move the existing hardcoded constant into the matching entity asset's new `CostField`). Feels Unity-native (data lives on ScriptableObjects).

**Cons.** Defeats the original goal. "Review one reviewable file" becomes "scan N entity files". Editor matrix window is the ONLY reviewable surface (hard dependency on the scan). Cross-cutting rows (tax floor, bond ceiling, country-level constants) have no obvious entity asset to live on — escape hatch needed (a residual `GlobalCostTable.asset`). Adding a new cost dimension (e.g. landmark upkeep rate) requires a new field on the entity asset, recompile. Cost-model change = entity-asset-schema change.

### Approach D — hybrid (Approach A + per-category facade)

One flat `CostTable.asset` (per Approach A) as the SOURCE-OF-TRUTH storage + per-category read facades (`IZoneCostCatalog`, `IUtilityCostCatalog`, ...) that dispatch to the single table. Facades filter the enum namespace per category at the interface boundary (Bucket 2 sees only zone rows; Bucket 3 sees only finance rows). Editor UI renders the single table (Approach A virtues) BUT consumer code depends only on its category facade (Approach B boundary hygiene).

**Pros.** Best of A (one reviewable asset + editor simplicity) + best of B (category boundary enforcement at call sites). Adding a row = one enum change + one table row. Cross-category comparisons still trivial (one file).

**Cons.** Two-layer API — `CostTable` + category facades. Minor indirection cost. Requires convention that every new row declares its category (naming prefix or enum attribute). Facade implementations are mechanical but N files to maintain.

---

## Open questions (resolve via `/design-explore` interview)

1. **Approach selection.** A / B / C / D (hybrid)? Expected direction = A or D (simplest review surface). Cast via Phase 2 user confirm.
2. **Row taxonomy scope at MVP.** Enumerate the ~40–80 rows expected at Bucket 11 Step 1 cut? Or defer row inventory to Stage 1.1 migration scan + assemble empirically? Recommend empirical — grep the codebase for hardcoded numeric literals in consumer files + triage.
3. **Editor UI surface.** Custom property drawer on `CostRow` (default Inspector scroll works fine) vs dedicated `CostTableWindow` editor window with matrix + CSV export button? Recommend: ship custom property drawer at Step 1 (minimal effort, good-enough review). Defer `CostTableWindow` to Step 3 when balance-review friction is observed.
4. **Matrix export format.** CSV only? Markdown table? Both? Recommend markdown table (pasteable into BACKLOG balance-review issues) at Step 3; CSV optional add-on.
5. **Migration marker convention.** `// TODO(bucket-11): migrate to CostTable` comment on every inline constant pre-bucket-11? Or a `[Obsolete("Move to CostTable")]` attribute on wrapper constants? Recommend: comment marker (simplest, grep-friendly) + Step 1 Stage 1.1 runs `grep -rn "TODO(bucket-11)" Assets/Scripts/` to generate migration inventory.
6. **Test parity enforcement.** EditMode tests that assert pre- vs post-migration `Get(row).value` equality? Golden-file of expected values locked at Step 1? Recommend: per-migration PR ships a parity test — list pre-migration constant + assert `catalog.Get(row).value == PRE_MIGRATION_VALUE`. Catalog value change = explicit new PR with balance rationale.
7. **Footprint constants vs cost constants — same table?** Some footprints are cost-adjacent (utility pool pad size) but some are pure geometry (landmark 4×4 plot size). Keep in `CostTable` for review locality, or split into `FootprintTable.asset`? Recommend: keep in `CostTable` (Inspector section divider in property drawer separates `CostRow.value` from `CostRow.footprint`).
8. **Maintenance rates — per-tick or per-month?** Rate unit scheme already mixed in code today. Catalog locks one convention. Recommend: per-month (matches budget tick + tax unit). Step 1 Stage 1.1 audits + normalizes.
9. **Glossary + IA impact.** New glossary rows needed: "cost catalog", "CostTable", "cost row", "footprint constant". New reference spec `ia/specs/cost-catalog.md`? Or fold into `managers-reference.md`? Recommend: both — short reference spec (API + migration rules) + managers-reference section for CostCatalogService.
10. **Post-MVP runtime mutation path.** User explicitly deferred dynamic pricing. BUT: is the catalog read-only-forever (harder to add later), or read-only-at-MVP with a clear reserved extension hook (e.g. `ICostCatalogMutable` sub-interface gated behind a feature flag)? Recommend: read-only-at-MVP, no extension hook. Post-MVP dynamic pricing = new bucket, new API; current bucket stays minimal.

---

## Recommendation (pre-interview provisional)

**Approach D — hybrid (flat CostTable + per-category facades).** Matches user intent ("centralize to better analyze, arrange, test the final cost matrix") via the single reviewable asset + keeps consumer boundaries clean via category facades. Adds modest indirection (two-layer API) for meaningful boundary hygiene.

Fallback: **Approach A** if the facade layer is judged premature — ship flat, add facades later when a specific consumer needs boundary enforcement. Approach B rejected — editor matrix review friction unacceptable. Approach C rejected — defeats the "one reviewable file" goal.

**User confirms at `/design-explore` Phase 2 gate.**

---

## Dependencies

- **Upstream.** None. Bucket 11 is pure infra — can start in Tier A lane with no gameplay bucket dependency.
- **Downstream (consumers).** Buckets 2 (city-sim-depth — construction + maintenance cost), 3 (zone-s-economy — S-building cost + bond ceiling + tax floor), 4 (utilities-and-landmarks — utility pool unit cost + landmark cost + footprint), 6 (ui-polish — tooltip reads cost from catalog for info panels). Umbrella cross-cutting rule enforces `ICostCatalog.Get(row)` at all consumer sites.
- **Save schema.** None. `CostTable` is asset-only, immutable at runtime; cost values never persist to save.

---

## Non-scope pointers

- No dynamic / population-scaled / tier-scaled pricing → catalog is read-only ScriptableObject, no runtime mutation API.
- No economy-reactive pricing (inflation, demand modifiers) → pricing formulas live in consumers (DemandManager, etc.), not in the catalog.
- No modder overrides → `CostTable.asset` ships inside the Unity project; no external override file.
- No in-game balance editor → editor UI is Unity Editor only, not exposed to game runtime.
- No localisation of cost display → cost rendering in UI is Bucket 6 concern, catalog stores numeric values only.

---

## Next step

Run `/design-explore docs/cost-catalog-exploration.md`. Interview resolves the ~10 open questions above. Expansion emits `## Design Expansion` block with architecture Mermaid (CostTable → ICostCatalog → consumer buckets flow), subsystem impact, implementation points (Phases A–F matching Step 1 / 2 / 3 breakdown), examples (one migration diff sample, one editor UI interaction flow, one test parity failure case), review notes. Then `/master-plan-new docs/cost-catalog-exploration.md` authors `ia/projects/cost-catalog-master-plan.md`.
