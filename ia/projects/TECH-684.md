---
purpose: "TECH-684 — Wire ZoneSubTypeRegistry to GridAssetCatalog (serialized ref + Awake resolution)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T2.3.1
phases:
  - "Phase 1 — SerializeField + Awake resolution"
---
# TECH-684 — Wire registry to catalog

> **Issue:** [TECH-684](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-22
> **Last updated:** 2026-04-22

## 1. Summary

Add a `[SerializeField] GridAssetCatalog` reference on `ZoneSubTypeRegistry` and resolve it in `Awake` (fallback: single `FindObjectOfType<GridAssetCatalog>()` if unset) so later Stage 2.3 work reads catalog rows from the same instance that loads the boot snapshot (TECH-672). No new singleton; English error if catalog is missing.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `GridAssetCatalog` reference is serializable in Inspector; optional one-shot discovery when null.
2. Resolution runs only in `Awake` (not per-frame) per `unity-invariants` #3.
3. Clear `Debug.LogError` if catalog still missing after resolution.

### 2.2 Non-Goals (Out of Scope)

1. No change to `GridAssetCatalog` load pipeline or snapshot format.
2. No consumption of `asset_id` mapping (TECH-685).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|------------------------|
| 1 | Developer | I need the registry to see the same catalog as the rest of the scene | Registry holds non-null catalog after `Awake` when scene is wired correctly |

## 4. Current State

### 4.1 Domain behavior

`ZoneSubTypeRegistry` loads JSON from `Resources/Economy/zone-sub-types` in `LoadFromJson`; Zone S work will shift costs/names to Postgres-backed snapshot (Stage 2.3). Catalog reference must exist before mapping + caller updates.

### 4.2 Systems map

- `Assets/Scripts/Managers/GameManagers/ZoneSubTypeRegistry.cs` — this task
- `Assets/Scripts/Managers/GameManagers/GridAssetCatalog.cs` (or partials) — read-only ref
- `ia/specs/economy-system.md` — Zone sub-type registry vocabulary

### 4.3 Implementation investigation notes (optional)

Confirm scene order: `GridAssetCatalog` `Awake` should run before or same frame as `ZoneSubTypeRegistry`; if script execution order is ambiguous, document `DefaultExecutionOrder` or serialized ref as fix.

## 5. Proposed Design

### 5.1 Target behavior (product)

N/A — wiring only; no player-visible change until follow-on tasks use catalog.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Private field or `[SerializeField] protected GridAssetCatalog gridAssetCatalog` plus `ResolveCatalog()` in `Awake` before or after `LoadFromJson` per chosen ordering (prefer resolving catalog first if later code in same class will need it).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|-------------------------|
| 2026-04-22 | Single `FindObjectOfType` fallback in `Awake` | Matches `unity-invariants` #4 (scene MonoBehaviour, not new singleton) | Service locator (rejected) |

## 7. Implementation Plan

### Phase 1 — Catalog reference

- [ ] Add `[SerializeField] GridAssetCatalog` (name aligns with project naming) on `ZoneSubTypeRegistry`.
- [ ] In `Awake`, if null, assign `FindObjectOfType<GridAssetCatalog>(true)` once; if still null, `LogError` and return.
- [ ] Expose read-only `GridAssetCatalog Catalog` or internal accessor for sibling tasks in same assembly.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| C# + scene wiring | Node | `npm run unity:compile-check` | After C# change |

## 8. Acceptance Criteria

- [ ] `[SerializeField] GridAssetCatalog` on `ZoneSubTypeRegistry`.
- [ ] One-time `FindObjectOfType<GridAssetCatalog>()` in `Awake` only when field null.
- [ ] `Debug.LogError` in English if unresolved.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## §Plan Digest

### §Goal

`ZoneSubTypeRegistry` holds a valid `GridAssetCatalog` reference after `Awake` via Inspector assignment or a single `FindObjectOfType<GridAssetCatalog>()` call, then existing `LoadFromJson` runs. Missing catalog: English `LogError`, no new singletons.

### §Acceptance

- [ ] `[SerializeField] private GridAssetCatalog` (or project naming) field on `ZoneSubTypeRegistry` with tooltip if pattern uses headers elsewhere.
- [ ] `Awake` body: null-check → at most one `FindObjectOfType<GridAssetCatalog>()`; second null check → `Debug.LogError("[ZoneSubTypeRegistry] ...")` + early return; else call `LoadFromJson()`.
- [ ] `npm run unity:compile-check` exit 0 from repo root after edit.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| compile_gate | edited `ZoneSubTypeRegistry.cs` | exit code 0 | `npm run unity:compile-check` |

### §Examples

| Step | `GridAssetCatalog` in scene | Expected field state after `Awake` |
|------|-----------------------------|-------------------------------------|
| Inspector linked | yes | same reference |
| Not linked, one catalog in scene | yes | `FindObjectOfType` assigns |
| None | no | `LogError` + `LoadFromJson` not run after return |

### §Mechanical Steps

#### Step 1 — Add catalog field and expand `Awake`

**Goal:** Wire serialized reference; replace expression-bodied `Awake` with a body that resolves catalog and gates `LoadFromJson`.

**Edits:**

- `Assets/Scripts/Managers/GameManagers/ZoneSubTypeRegistry.cs` — **before**:
  ```
    private ZoneSubTypeEntry[] _entries = Array.Empty<ZoneSubTypeEntry>();

    public System.Collections.Generic.IReadOnlyList<ZoneSubTypeEntry> Entries => _entries;

    private void Awake() => LoadFromJson();
  ```
  **after**:
  ```
    [SerializeField] private GridAssetCatalog _gridAssetCatalog;

    private ZoneSubTypeEntry[] _entries = Array.Empty<ZoneSubTypeEntry>();

    public System.Collections.Generic.IReadOnlyList<ZoneSubTypeEntry> Entries => _entries;

    private void Awake()
    {
        if (_gridAssetCatalog == null)
            _gridAssetCatalog = FindObjectOfType<GridAssetCatalog>();
        if (_gridAssetCatalog == null)
        {
            Debug.LogError("[ZoneSubTypeRegistry] GridAssetCatalog not found in scene.");
            return;
        }
        LoadFromJson();
    }

    /// <summary>Scene catalog instance resolved in <see cref="Awake"/>; used by Stage 2.3 map + UI.</summary>
    internal GridAssetCatalog Catalog => _gridAssetCatalog;
  ```

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && npm run unity:compile-check
```

**STOP:** Re-open Step 1 edit if gate prints compile errors; do not add a static singleton for `GridAssetCatalog`.

**MCP hints:** `backlog_issue` (TECH-684), `plan_digest_resolve_anchor` on `private void Awake() => LoadFromJson();`

## Open Questions (resolve before / during implementation)

None — implementation path fixed by master-plan Stage 2.3; catalog schema owned by prior stages.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
