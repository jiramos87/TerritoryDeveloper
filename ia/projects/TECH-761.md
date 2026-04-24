---
purpose: "TECH-761 — Play Mode smoke checklist."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/grid-asset-visual-registry-master-plan.md"
task_key: "T3.2.5"
---
# TECH-761 — Play Mode smoke checklist

> **Issue:** [TECH-761](../../BACKLOG.md)
> **Status:** Draft | In Review | In Progress | Final
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Manual Play Mode smoke checklist doc (no automated Play test). Covers ghost tint
valid/invalid per reason, tooltip render, `sortingOrder` intact, no world-tile
collider regression.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Checklist enumerates scenarios: valid placement tint, each `PlacementFailReason`
   branch, tooltip text correctness, cursor-leave revert.
2. States explicitly: manual per policy; no automated Play test.
3. Lives in `docs/` (likely under `docs/implementation/` or verify-loop scenario folder).
4. Verify-loop operator can follow end-to-end in one session.

### 2.2 Non-Goals (Out of Scope)

1. Automated Play Mode test or CI integration.
2. Performance profiling or load testing.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Verifier | Manual smoke checklist guides end-to-end Play Mode validation | Checklist clear + executable in one session |
| 2 | Developer | Document explicitly: manual smoke, no automated test | Policy stated; no Play test in CI |

## 4. Current State

### 4.1 Domain behavior

TECH-757..760 implement ghost preview validation + tint + tooltip. No manual smoke doc exists yet. Stage Exit criteria require manual smoke documented.

### 4.2 Systems map

- New doc file under `docs/implementation/` (filename TBD at implement time).
- Scenarios reference TECH-757..760 surfaces.
- No code changes.

### 4.3 Implementation investigation notes (optional)

- Enumerate all `PlacementFailReason` values for scenario coverage.
- Identify `sortingOrder` visual checkpoints.

## 5. Proposed Design

### 5.1 Target behavior (product)

Verifier follows manual Play Mode checklist: move cursor over valid/invalid cells, confirm tint colors, read tooltip text, verify no collider regression.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- List all `PlacementFailReason` branches (one smoke scenario per value).
- Include cursor-leave revert check.
- Include `sortingOrder` visual verification.
- State explicitly: manual smoke only, no automated Play test.

### 5.3 Method / algorithm notes (optional)

None — documentation only.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| TBD | Doc location and filename | TBD | TBD |

## 7. Implementation Plan

### Phase 1 — Doc author + verify

- [ ] Draft scenarios; walk through once in Play Mode; amend as needed.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Manual smoke doc complete | Document review | `docs/implementation/` file (TBD name) | Verify checklist covers all scenarios |
| Policy stated: manual only | Document review | Explicit statement: no automated Play test | Policy visibility |
| Verifier can execute in one session | Manual execution | Follow checklist steps in Play Mode | Time check — should take <10 min |

## 8. Acceptance Criteria

- [ ] Checklist enumerates scenarios: valid placement tint, each `PlacementFailReason`
      branch, tooltip text correctness, cursor-leave revert.
- [ ] States explicitly: manual per policy; no automated Play test.
- [ ] Lives in `docs/implementation/` or verify-loop scenario folder.
- [ ] Verify-loop operator can follow end-to-end in one session.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| | | | |

## 10. Lessons Learned

- 

## §Plan Digest

```yaml
mechanicalization_score:
  overall: fully_mechanical
  fields:
    edits_have_anchors: pass
    gates_present: pass
    invariant_touchpoints_present: pass
    stop_clauses_present: pass
    picks_resolved: pass
```

### §Goal

Author the Stage 3.2 manual Play Mode smoke checklist at `docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md` so verify-loop operators can walk every `PlacementFailReason` branch + valid path + cursor-leave revert + sortingOrder + Collider2D invariant in one Play Mode session under 10 minutes. Doc IS the Stage Exit "manual smoke documented" deliverable.

### §Acceptance

- [ ] File `docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md` exists.
- [ ] Doc body mentions all 6 `PlacementFailReason` enum names (`None`, `Footprint`, `Zoning`, `Locked`, `Unaffordable`, `Occupied`).
- [ ] Doc states verbatim: `Manual smoke per Stage Exit policy; no automated Play Mode test wired in CI.`
- [ ] `sortingOrder` visual checkpoint scenario row present.
- [ ] `Collider2D` invariant checkpoint scenario row present (zero new world-tile collider).
- [ ] Every scenario row carries `[ ]` checkbox + `**Result:**` placeholder for operator capture.
- [ ] Doc author runs the checklist once during `/implement` Play Mode pass-through; inline `**Result:**` lines captured + ambiguous wording revised.
- [ ] Full walk completes in one Play Mode session (target <10 min).

### §Test Blueprint

| test_name | inputs | expected | harness |
|---|---|---|---|
| Doc exists at canonical path | `test -f docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md` | exit 0 | manual (path check) |
| Doc covers all `PlacementFailReason` values | `for r in None Footprint Zoning Locked Unaffordable Occupied; do grep -q "$r" docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md \|\| echo "MISSING: $r"; done` | no `MISSING:` output | manual (grep guard) |
| Doc states manual policy explicitly | `grep -c "Manual smoke per Stage Exit policy; no automated Play Mode test wired in CI." docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md` | `1` | manual (grep) |
| Each scenario has checkbox + Result line | Visual review of every `### Scenario` block | Every scenario row carries `[ ]` and `**Result:**` placeholder | manual (review) |
| sortingOrder + Collider2D scenarios present | `grep -c "sortingOrder" docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md && grep -c "Collider2D" docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md` | both `>= 1` | manual (grep) |
| End-to-end walk completes in one session | Operator follows checklist in Play Mode | Walk completes <10 min; all `[x]` boxes ticked or `**Result:**` captures failure | manual (operator-driven) |

### §Examples

Doc body section shape (one scenario block — repeated per row):

```markdown
### Scenario 2 — Occupied cell

- Setup: cell with existing building.
- Action: cursor over cell.
- Pass criterion: ghost red; tooltip text `Cell already occupied.`.
- [ ] **Result:** _operator pastes outcome here during verify-loop._
```

Scenario coverage matrix (verbatim authoring source for the 9 rows):

| # | Name | Setup | Action | Pass criterion |
|---|------|-------|--------|----------------|
| 1 | Valid placement (None) | Empty residential cell, treasury > base cost | Cursor over cell | Ghost green; tooltip hidden |
| 2 | Occupied | Cell with existing building | Cursor over cell | Ghost red; tooltip `Cell already occupied.` |
| 3 | Footprint (out of bounds) | Cursor at grid edge | Cursor crosses bound | Ghost red; tooltip `Out of bounds or unsupported footprint.` |
| 4 | Zoning mismatch | `state_service` asset, residential zone cell | Cursor over cell | Ghost red; tooltip `Wrong zone for this asset.` |
| 5 | Unaffordable | Treasury = 0, base_cost > 0 asset | Cursor over valid cell | Ghost red; tooltip `Insufficient funds.` |
| 6 | Locked (dormant in Stage 3.2) | Asset with `unlocks_after`, no tech | Cursor over valid cell | Ghost green per Stage 3.1 default; tooltip hidden. Doc note: `Locked path inactive in Stage 3.2; revisit when tech tree lands.` |
| 7 | Cursor leave revert | Ghost red on invalid cell | Move cursor off grid | Ghost destroyed; tooltip hidden |
| 8 | sortingOrder check | Ghost over building cluster, toggle red/green via cell move | Visual inspection | Ghost stays in correct draw order; no z-fighting |
| 9 | Collider2D invariant | After full smoke run | Inspect scene hierarchy + Physics2D | Zero new `Collider2D` on world tiles; preview colliders remain disabled per `CursorManager` |

### §Mechanical Steps

#### Step 1 — Verify Stage 3.2 manual smoke checklist doc on HEAD

**Goal:** confirm `docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md` exists on HEAD with full 9-scenario coverage (authored during the plan-digest pass; this Step is the implementer's HEAD-state verification). If file missing or coverage incomplete, recreate from the verbatim body in this Step's `after` literal.

**invariant_touchpoints:** `none (utility)` — doc-only, no runtime surface, no `Assets/**/*.cs` touch.

**Edits:**

- `docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md` — **before** (HEAD anchor — first heading line, must resolve to exactly 1 hit):
  ```
  # Stage 3.2 — Ghost preview validation Play Mode smoke checklist
  ```
  **after** (no change — file already authored during plan-digest pass; verification only):
  ```
  # Stage 3.2 — Ghost preview validation Play Mode smoke checklist
  ```

**Gate:**
```bash
test -f docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md && echo OK
```
Expectation: prints `OK`.

**Secondary gate (coverage):**
```bash
for r in None Footprint Zoning Locked Unaffordable Occupied; do grep -q "$r" docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md || echo "MISSING: $r"; done
```
Expectation: no `MISSING:` line emitted.

**Tertiary gate (manual policy phrase):**
```bash
grep -c "Manual smoke per Stage Exit policy; no automated Play Mode test wired in CI." docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md
```
Expectation: `1`.

**STOP:** if any gate fails (file missing, enum value missing, policy phrase absent) → restore the file from the spec's authored body via `git checkout HEAD -- docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md`; if the file was never committed, re-author from the §Plan Digest §Examples scenario coverage matrix in this spec. Do NOT close the Task without all three gates green. If `PlacementFailReason` enum values drift between authoring + verify, escalate to `/plan-review` to extend the scenario list.

**MCP hints:** `plan_digest_verify_paths` (confirm doc on HEAD), `plan_digest_render_literal` (re-render `PlacementValidator.cs` enum block when checking coverage drift), `plan_digest_resolve_anchor` (anchor first heading line for diff).

#### Step 2 — Operator pass-through during `/implement`

**Goal:** doc author runs the checklist once in Play Mode immediately after Step 1 write; captures real outcomes inline under each `**Result:**` line; revises ambiguous wording in-place.

**invariant_touchpoints:** `none (utility)` — operator-driven verification, no code touch.

**Edits:**

- `docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md` — **before**:
  ```
  - [ ] **Result:** _paste observed outcome here during verify-loop._
  ```
  **after** (per-scenario, populated during the Play Mode pass):
  ```
  - [x] **Result:** observed: ghost tint <green|red>; tooltip text `<exact string or "hidden">`; sortingOrder OK; no new colliders.
  ```

**Gate:**
```bash
grep -c "**Result:** observed" docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md
```
Expectation: `>= 9` (one per scenario row).

**STOP:** if any scenario diverges from the authored Pass criterion → file a BUG-* against the upstream Task (TECH-757..760) before flipping this Task Done; do NOT mark `[x]` on a scenario whose `**Result:**` captures failure.

**MCP hints:** `unity_bridge_command` (Play Mode entry / cell-cursor placement when bridge supports it), `plan_digest_resolve_anchor` (locate the per-scenario `**Result:**` template line for in-place edit).

### §Implementer hand-off

- Order: Step 1 (write doc) → Step 2 (operator Play Mode pass + inline result capture). No code edits in this Task; no `npm run unity:compile-check` needed; gate set is `test -f` + `grep`.
- Cross-Task dependency: TECH-757..760 must be Done before Step 2 can produce non-failing `**Result:**` lines (Scenarios 2–7 depend on tint + tooltip wiring; Scenario 9 depends on absence of new `Collider2D` introduced by TECH-757..760 changes).
- This doc IS the Stage 3.2 Stage Exit "manual smoke documented" deliverable; without it ship-stage halts.

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._

## Open Questions (resolve before / during implementation)

1. Doc filename and folder — `docs/implementation/grid-asset-visual-registry-3.2-smoke.md` or similar?
2. Which `PlacementFailReason` values exist in TECH-689? (need exact list for scenario rows)
3. Estimate session time — target <10 min end-to-end walk?

---
