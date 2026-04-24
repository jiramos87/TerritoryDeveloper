### Stage 2 — Facade + Store Infra (additive, no consumer migration) / CityStatsFacade MonoBehaviour + tick bracket

**Status:** Final (TECH-602, TECH-603 archived 2026-04-21)

**Objectives:** Wire `CityStatsFacade` into the scene and thread `BeginTick`/`EndTick` into `SimulationManager` without altering tick execution order.

**Exit:**

- `CityStatsFacade : MonoBehaviour, IStatsReadModel` compiles; `[SerializeField]`-wired in scene Inspector alongside existing `CityStats`.
- `SimulationManager.ProcessSimulationTick` calls `_facade.BeginTick()` before step 1 and `_facade.EndTick()` inside existing `finally` block (`SimulationManager.cs:85`); steps 1-5 order unchanged (per `sim §Tick execution order`).
- `Action OnTickEnd` event fires on each `EndTick`; zero consumers yet (wired in Stage 2.1).
- Phase 1 — Add CityStatsFacade + wire tick bracket in SimulationManager.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T2.1 | **TECH-602** | Done (archived) | Add `CityStatsFacade.cs` : `MonoBehaviour`, `IStatsReadModel`; owns `ColumnarStatsStore _store` (composition, instantiated in `Awake`); exposes `BeginTick()` (resets per-tick accumulator), `Publish(StatKey, float delta)`, `Set(StatKey, float)`, `EndTick()` (calls `_store.FlushToSeries()` + fires `public event Action OnTickEnd`); delegates `GetScalar`/`GetSeries`/`EnumerateRows` to `_store`. `[SerializeField]` Inspector wire — no singleton (invariant #4). |
| T2.2 | **TECH-603** | Done (archived) | Add `[SerializeField] private CityStatsFacade _facade` to `SimulationManager.cs`; call `_facade?.BeginTick()` before step 1 inside `try` body (`SimulationManager.cs:63`) and `_facade?.EndTick()` in the existing `finally` block (`:85`). Null-guard throughout. Tick execution order (steps 1-5 per `sim §Tick execution order`) unchanged — bracket wraps, does not reorder. |

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: ""
  title: "Add `CityStatsFacade.cs` — MonoBehaviour implementing `IStatsReadModel`; owns `ColumnarStatsStore`; `BeginTick` / `Publish` / `Set` / `EndTick` + `OnTickEnd`; delegates readers to store (Stage 2 T2.1)"
  priority: medium
  notes: |
    New `GameManagers` MonoBehaviour wired in scene next to legacy `CityStats`. Composes `ColumnarStatsStore` from Stage 1 (`TECH-303`/`TECH-304`). No consumers of `OnTickEnd` yet (Stage 4). Invariant #4: Inspector `[SerializeField]` only, no singleton. Invariant #6: not on `GridManager`.
  depends_on:
    - TECH-303
    - TECH-304
  related: []
  stub_body:
    summary: |
      Introduce `CityStatsFacade` as the tick-scoped writer/reader facade over `ColumnarStatsStore`: per-tick accumulator reset in `BeginTick`, delta/`Set` during tick, `EndTick` flushes ring + raises `OnTickEnd` for future UI/producers.
    goals: |
      1. `CityStatsFacade : MonoBehaviour, IStatsReadModel` with composed store (new in `Awake`).
      2. `BeginTick` / `Publish` / `Set` / `EndTick` + `GetScalar` / `GetSeries` / `EnumerateRows` delegate to `_store`.
      3. `public event Action OnTickEnd` fires once per tick after flush.
      4. Scene: Inspector wire only; no `FindObjectOfType` in hot paths.
    systems_map: |
      - New: `Assets/Scripts/Managers/GameManagers/CityStatsFacade.cs` (or adjacent path per repo convention).
      - Uses: `IStatsReadModel`, `StatKey`, `ColumnarStatsStore` (`Assets/Scripts/Managers/GameManagers/`, `UnitManagers/`).
      - Spec: `ia/specs/simulation-system.md` §Tick execution order — bracket must not reorder steps 1–5.
    impl_plan_sketch: |
      ### Phase 1 — CityStatsFacade MonoBehaviour

      - [ ] Add class implementing `IStatsReadModel`; instantiate `ColumnarStatsStore` in `Awake`; wire capacity default 256.
      - [ ] Implement `BeginTick`, `Publish`, `Set`, `EndTick` (flush + `OnTickEnd?.Invoke()`), delegate read APIs to store.
      - [ ] Add component to scene / prefab alongside `CityStats`; document Inspector fields in commit notes.

- reserved_id: ""
  title: "Wire `SimulationManager` tick bracket — `BeginTick` before step 1, `EndTick` in `finally` (Stage 2 T2.2)"
  priority: medium
  notes: |
    `SimulationManager.ProcessSimulationTick`: null-safe `_facade?.BeginTick()` at start of `try` before simulation steps; `_facade?.EndTick()` in existing `finally` so flush runs even on failure. Line refs in orchestrator (`:63`, `:85`) are hints — verify current file. Order of steps 1–5 unchanged per `simulation-system` tick contract. Depends on `CityStatsFacade` type from T2.1.
  depends_on:
    - TECH-303
    - TECH-304
  related: []
  stub_body:
    summary: |
      Connect `CityStatsFacade` into the simulation tick pipeline: bracket all per-tick work with `BeginTick`/`EndTick` without reordering or expanding step responsibilities.
    goals: |
      1. `[SerializeField] private CityStatsFacade _facade` on `SimulationManager`.
      2. `BeginTick` invoked before step 1 inside `try`.
      3. `EndTick` invoked in `finally` after steps (matches existing structure).
      4. Null-safe throughout; compile clean.
    systems_map: |
      - `Assets/Scripts/Managers/GameManagers/SimulationManager.cs` — `ProcessSimulationTick` try/finally.
      - Facade: `CityStatsFacade` (T2.1).
      - Spec: `ia/specs/simulation-system.md` §Tick execution order.
    impl_plan_sketch: |
      ### Phase 1 — SimulationManager bracket

      - [ ] Add serialized facade field; assign in Inspector (or document for scene pass).
      - [ ] Insert `_facade?.BeginTick()` before existing step 1 entry point in `try`.
      - [ ] Insert `_facade?.EndTick()` in `finally` block; verify no double-`EndTick` paths.
      - [ ] Run `npm run unity:compile-check`.
```

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — TECH-602 + TECH-603 §Plan Author aligned w/ Stage 2 block, Exit criteria, invariants #3/#4/#6. Spec frontmatter `parent_plan` + `task_key` mirror orchestrator T2.1 / T2.2. No fix tuples. Downstream: `/ship-stage ia/projects/citystats-overhaul-master-plan.md Stage 2`.

#### §Stage Closeout Plan

> **Applied** 2026-04-21 — Stage 2 closeout: **TECH-602** + **TECH-603** archived under `ia/backlog-archive/`, temporary specs removed, task rows **Done (archived)**. Implementation: `feat(TECH-602):` / `feat(TECH-603):` on branch; `npm run unity:compile-check` + `npm run validate:all` exit 0. Tuple batch not retained in orchestrator (same pattern as `skill-training-master-plan` post-apply).

---
