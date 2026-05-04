---
purpose: "Pre-retrofit snapshot of `multi-scale` plan Stages 10–15. Captures plumbing-first shape before TECH-10312 dispatch reshapes per §7.2 of `prototype-first-methodology-design.md`. Paired with `multi-scale-retrofit-snapshot-post.md` for diff verification (TECH-10312 §Acceptance row 3)."
audience: agent
loaded_by: ondemand
slices_via: none
---

# Multi-scale retrofit — pre-dispatch snapshot

> Captured 2026-05-04 by `/ship-stage prototype-first-methodology Stage 1.5`.
> Source: `mcp__territory-ia__master_plan_render({slug: 'multi-scale'})` + per-stage `stage_render` calls.
> All 6 stages currently `status: pending`.

## Stage 10 — Region playability / Region grid + scene wiring

- **Status:** pending
- **Objective:** `RegionGridManager` MonoBehaviour + extracted helper services + `RegionManager` orchestrator + `TimeManager` subscriber wiring. Establishes scene-level region surface; consumes Stage 8 catalog + Stage 9 snapshot shape.
- **Player output:** None — managers + helper services + orchestrator wiring; not a playable scene.
- **Tasks (4 pending):** T10.1 RegionGridManager MonoBehaviour; T10.2 helper services; T10.3 RegionManager orchestrator + Inspector wiring; T10.4 TimeManager.Tick subscription + smoke.
- **Arch surfaces:** `layers/helper-services`.

## Stage 11 — Region playability / Region sim + treasury + UI shell

- **Status:** pending
- **Objective:** `RegionSimManager` 7 flow channels + `RegionTreasury` 4 money channels + `RegionPolicyStore` + `RegionUIManager` MonoBehaviour + 4 region UI panels + `EconomyManager IRegionTaxContributor` hook.
- **Player output:** None until all 7 channels + 4 panels land — all-or-nothing.
- **Tasks (6 pending):** T11.1 RegionSimManager.SolveFlows + 7 per-channel Service classes; T11.2 RegionTreasury 4-channel ledger; T11.3 RegionPolicyStore; T11.4 RegionUIManager + RegionHUD; T11.5 CityDashboardPanel + BuildModeToolbar + RegionAlertFeed; T11.6 EconomyManager IRegionTaxContributor hook.

## Stage 12 — Region playability / Scale switch + dormant evolve dispatch

- **Status:** pending
- **Objective:** `ScaleController` mouse-wheel zoom + dissolve VFX + re-entry city-pick + `CityEvolveService.EvolveDormant` per-tick foreach + dormant evolve cost profile + active-scale enum + transition state machine.
- **Player output:** First playable scene — only after Stages 10+11 fully done.
- **Tasks (4 pending):** T12.1 ActiveScale enum + ScaleController shell; T12.2 dissolve-out / dissolve-in; T12.3 CityEvolveService.EvolveDormant; T12.4 budget profile + round-trip smoke.

## Stage 13 — Region playability / Founding + build mode

- **Status:** pending
- **Objective:** `CityFoundingService` viability rules + founding cost debit + city stub seed + `RegionBuildService` strokes (highway/rail/canal/bridge) + `BuildModeToolbar` bindings + cost preview hover.
- **Player output:** Iterate on already-playable Region (founding + build mode).
- **Tasks (5 pending):** T13.1 ViabilityResult + ICityFoundingViability; T13.2 CityFoundingService.Check 4 viability rules; T13.3 city stub seed; T13.4 RegionBuildService strokes; T13.5 BuildModeToolbar bindings.

## Stage 14 — Region playability / Persistence + migration + catalog wire

- **Status:** pending
- **Objective:** `RegionSaveV4` root + serializer + `CitySaveMigrationV3toV4` + schema-version gate + restore order assertion + `GridAssetCatalog.Reload` subscription + `wire_asset_from_catalog` bridge call + v3+v4 fixtures + `db:migrate` validation.
- **Player output:** Iterate on already-playable Region (persistence + migration).
- **Tasks (6 pending):** T14.1 RegionSaveV4 wire types; T14.2 schema-version gate + migration; T14.3 restore order; T14.4 GridAssetCatalog.Reload subscription; T14.5 wire_asset_from_catalog bridge call; T14.6 fixtures + tests.
- **Arch surfaces:** `data-flows/persistence`.

## Stage 15 — Region playability / Verification + smoke

- **Status:** pending
- **Objective:** EditMode + PlayMode + bridge smoke + `verify:local` green + glossary spec-ref cleanup.
- **Player output:** MVP gate — final close on Step 3.
- **Tasks (4 pending):** T15.1 EditMode test sweep; T15.2 PlayMode smoke; T15.3 db:bridge-playmode-smoke + verify:local; T15.4 glossary specRef cleanup + MEMORY.md collision memo.

## Net pre-shape

- **First playable scene:** end of Stage 12 (after Stages 10+11 plumbing).
- **Plumbing-first count:** Stages 10+11 = 100% plumbing, no player verb.
- **Stage 10 §Tracer Slice block:** absent.
- **Stages 11–15 §Visibility Delta line:** absent on all 5.
