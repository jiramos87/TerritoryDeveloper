---
purpose: "TECH-775 — Tests for remap + GC."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/grid-asset-visual-registry-master-plan.md"
task_key: "T3.3.4"
---
# TECH-775 — Tests for remap + GC

> **Issue:** [TECH-775](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-24
> **Last updated:** 2026-04-24

## 1. Summary

Regression tests for Stage 3.3 remap + GC paths — Unity EditMode for load-time remap helper, server-side tests for GC refcount + no-delete-when-referenced invariant.

## 2. Goals and Non-Goals

### 2.1 Goals

1. EditMode test asserts A→B→C chain remap result id + missing-row fallback does not crash.
2. Server test asserts GC candidate set excludes any `catalog_sprite` referenced by `catalog_asset_sprite` OR `catalog_pool_member`.
3. `dry-run` vs commit paths both covered.
4. Tests green under `npm run validate:all` / relevant test runner.

### 2.2 Non-Goals (Out of Scope)

1. Performance / stress tests — handled post-MVP.
2. Manual UI test cases — documented in TECH-761 (Stage 3.2).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Remap helper handles chain + cycle + missing | EditMode test passes all 3 cases |
| 2 | Developer | GC refcount preserves referenced sprites | Server test confirms no false-positive deletions |

## 4. Current State

### 4.1 Domain behavior

No test coverage for remap or GC logic.

### 4.2 Systems map

- `Assets/Tests/EditMode/` — remap helper tests (match existing pattern)
- `web/` test harness (Vitest / Jest — whichever repo uses for api routes)
- `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs` remap hook
- `web/app/api/catalog/sprites/gc` route

### 4.3 Implementation investigation notes

EditMode: fixture with catalog snapshot. Server: seeds catalog + sprite + pool rows.

## 5. Proposed Design

### 5.1 Target behavior (product)

EditMode fixture tests `RemapAssetIdsOnLoad` with straight chain, cyclic chain, missing asset. Server fixture tests GC refcount + dry-run / commit paths.

### 5.2 Architecture / implementation

EditMode test class in `Assets/Tests/EditMode/RemapAssetIdsOnLoadTests.cs`. Server test in `web/__tests__/api/catalog/sprites/gc.test.ts` (or `.test.js` per repo pattern). Both use standard test patterns.

### 5.3 Method / algorithm notes

EditMode fixture:
- A→B→C chain: remap(A) = C
- A→B→A cycle: detected + logged
- Missing id: logged + fallback

Server fixture:
- Seed catalog row (id=1), asset row (assetId=1), sprite row (id=100, assetId=1)
- dryRun=true: returns [100] candidates (hypothetical orphan, if we seed a sprite not referenced)
- dryRun=false: deletes + row count decreases

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-24 | EditMode + server separate | Domain separation (load vs server) | Single fixture (less clear boundaries) |
| 2026-04-24 | Standard test patterns | Consistency with repo test suite | Custom test framework (overhead) |

## 7. Implementation Plan

### Phase 1 — Test coverage

- [ ] EditMode fixture for `RemapAssetIdsOnLoad` (3 cases: chain, cycle, missing).
- [ ] Server test fixture seeds catalog + sprite + pool rows; asserts GC orphan filter.
- [ ] `dryRun` path returns candidates without mutation; commit path deletes + row count drops.
- [ ] Wire into CI / `verify:local` chain.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| EditMode remap tests | EditMode | `npm run unity:testmode-batch` (if test exists) | 3 fixtures pass: chain, cycle, missing |
| Server GC tests | Server | `npm run test:web` or Vitest path | dry-run + commit paths both covered |
| CI integration | CI | `npm run validate:all` (chains unit tests) | Tests green; no regressions |

## 8. Acceptance Criteria

- [ ] EditMode test asserts A→B→C chain remap result id + missing-row fallback does not crash.
- [ ] Server test asserts GC candidate set excludes any `catalog_sprite` referenced by `catalog_asset_sprite` OR `catalog_pool_member`.
- [ ] `dry-run` vs commit paths both covered.
- [ ] Tests green under `npm run validate:all` / relevant test runner.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- None yet.

## §Plan Digest

```yaml
mechanicalization_score:
  anchors: ok
  picks: ok
  invariants: ok
  validators: ok
  escalation_enum: ok
  overall: fully_mechanical
```

### §Goal

Author regression tests covering Stage 3.3 load-time remap (TECH-773) + sprite GC route (TECH-774): Unity EditMode fixture for `GridAssetRemap.RemapAssetIdOnLoad(int, GridAssetCatalog)` (chain / cycle / missing) + colocated Vitest file for `POST /api/catalog/sprites/gc` (dry-run / commit / admin gate / reference protection).

### §Acceptance

- [ ] New EditMode test class at `Assets/Tests/EditMode/GridAssetCatalog/GridAssetRemapTests.cs` — 3 cases (straight chain, cycle, missing) under `namespace Territory.Tests.EditMode.GridAsset`, matching sibling `GridAssetCatalogParseTests.cs` attribute shape (`[Test]`, no explicit `[TestFixture]`).
- [ ] New colocated Vitest file at `web/app/api/catalog/sprites/gc/route.test.ts` — 6 cases (dry-run default, dry-run explicit, commit deletes orphans only, referenced-by-asset-sprite preserved, pool-member asset preserved, non-admin rejected) per TECH-774 §Test Blueprint.
- [ ] Vitest file uses colocated `*.test.ts` pattern (mirrors `web/lib/catalog/stable-json-stringify.test.ts`) — NOT `__tests__/` subdir (repo has both; route tests stay colocated).
- [ ] EditMode uses NUnit `[Test]` + `Assert.*`, not `TestCaseSource`, matching sibling style.
- [ ] Tests assert on TECH-773 public API signature — `GridAssetRemap.RemapAssetIdOnLoad(int saveAssetId, GridAssetCatalog catalog)` returns `int` (terminal live id or fallback 0); no `out bool` parameter; resolve exact method body at implement-time from TECH-773 Step 1 if signature diverges from spec during coding.
- [ ] `npm -w web run test` exits 0 with GC test file present.
- [ ] `npm run unity:testmode-batch` exits 0 with GridAssetRemapTests present.
- [ ] `npm run validate:all` green after test additions.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| `GridAssetRemapTests.StraightChain_ReturnsTerminal` | snapshot with `{1→2, 2→3, 3=terminal}`, assetId=1 | `RemapAssetId` returns 3; `remapped=true` | unity-batch |
| `GridAssetRemapTests.CyclicChain_DetectedAndLogged` | snapshot with `{1→2, 2→1}`, assetId=1 | returns original id (1) OR sentinel; `remapped=false`; `LogWarning` fired with `cycle` substring | unity-batch |
| `GridAssetRemapTests.MissingAsset_FallbackNoCrash` | empty snapshot, assetId=99 | returns 99 (identity fallback); `remapped=false`; no exception thrown | unity-batch |
| `route.test.ts: gc_dryRun_default_returnsCandidates` | seed 3 sprites, 2 via `catalog_asset_sprite`, body `{}` | `200 { candidates: ["<orphanId>"], count: 1 }`; DB unchanged | vitest |
| `route.test.ts: gc_dryRun_true_explicit` | same seed, body `{ dryRun: true }` | same as default | vitest |
| `route.test.ts: gc_commit_deletesOrphansOnly` | same seed, body `{ dryRun: false }` | `200 { deletedCount: 1, deletedIds: ["<orphanId>"] }`; referenced rows present | vitest |
| `route.test.ts: gc_referencedByAssetSprite_preserved` | sprite bound via `catalog_asset_sprite` only | dryRun excludes it; commit does NOT delete it | vitest |
| `route.test.ts: gc_poolMemberAssetLinkedSprite_preserved` | sprite bound to asset X; X in `catalog_pool_member` | sprite preserved (transitive via `catalog_asset_sprite`) | vitest |
| `route.test.ts: gc_nonAdmin_rejected` | auth header missing / non-admin | `403 { error: "not_allowed" }`; no DB query | vitest |

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| EditMode: snapshot `{1→2, 2→3}`, `assetId=1` | `RemapAssetId(1, snap, out remapped) == 3`; `remapped == true` | Chain happy path |
| EditMode: snapshot `{1→2, 2→1}`, `assetId=1` | return = 1 (fallback); `remapped == false`; log contains `cycle` | Cycle detection |
| EditMode: empty snapshot, `assetId=99` | return = 99; `remapped == false`; no exception | Missing asset identity fallback |
| Vitest: seed orphan `catalog_sprite{id:100}`, `dryRun=true` | `candidates` includes `100`; row count unchanged | GC dry-run |
| Vitest: same seed, `dryRun=false` | `deletedCount == 1`; row gone | GC commit |
| Vitest: seed `catalog_sprite{id:200}` + `catalog_asset_sprite{sprite_id:200}` | dryRun excludes `200`; commit preserves | `catalog_asset_sprite` join hit |
| Vitest: non-admin POST | `403 { error: "not_allowed" }`; no DB query fires | Auth gate precedes DB |

### §Mechanical Steps

#### Step 1 — Author EditMode test `GridAssetRemapTests.cs`

**Goal:** New NUnit EditMode fixture under `Assets/Tests/EditMode/GridAssetCatalog/` asserts behavior of TECH-773 `GridAssetRemap.RemapAssetId` for the 3 canonical cases (chain, cycle, missing). Matches sibling attribute style (`[Test]` + `Assert.*`, plain public class, no `[TestFixture]`).

**Edits:**
- `Assets/Tests/EditMode/GridAssetCatalog/GridAssetRemapTests.cs` — **operation**: create
  **after** — new file contents:
  ```csharp
  using NUnit.Framework;
  using UnityEngine;

  namespace Territory.Tests.EditMode.GridAsset
  {
      /// <summary>TECH-775 — regression tests for TECH-773 load-time remap helper.</summary>
      public class GridAssetRemapTests
      {
          [Test]
          public void RemapAssetId_StraightChain_ReturnsTerminal()
          {
              // Snapshot: assetId 1 → 2, 2 → 3, 3 terminal.
              // Assert: RemapAssetId(1, snap, out remapped) returns 3; remapped == true.
              Assert.Fail("TECH-775: author fixture + assertion against TECH-773 GridAssetRemap API.");
          }

          [Test]
          public void RemapAssetId_CyclicChain_DetectedAndFallsBack()
          {
              // Snapshot: 1 → 2, 2 → 1 (cycle).
              // Assert: returns original (1); remapped == false; LogWarning fired w/ "cycle" substring (LogAssert.Expect).
              Assert.Fail("TECH-775: author cycle fixture + LogAssert.Expect call.");
          }

          [Test]
          public void RemapAssetId_MissingAsset_IdentityFallbackNoCrash()
          {
              // Snapshot: empty assets[].
              // Assert: returns 99 (identity); remapped == false; no exception.
              Assert.Fail("TECH-775: author missing-asset fixture.");
          }
      }
  }
  ```
- `invariant_touchpoints`:
  - id: `editmode-test-dir`
    gate: `test -d Assets/Tests/EditMode/GridAssetCatalog`
    expected: pass
- `validator_gate`: `npm run unity:compile-check`

**Gate:**
```bash
npm run unity:compile-check
```
Expectation: exit 0 (new EditMode fixture compiles under `Assets/Tests/EditMode/GridAssetCatalog/`).

**STOP:** Namespace drift → re-run Step 1 (Write tool) matching `Territory.Tests.EditMode.GridAsset` namespace exactly (per sibling `GridAssetCatalogParseTests.cs`). Test class MUST be public + name ending in `Tests` for Unity Test Runner discovery. Do NOT add `[TestFixture]` attribute — siblings rely on NUnit default.

**MCP hints:** `plan_digest_verify_paths`, `plan_digest_resolve_anchor`.

#### Step 2 — Author Vitest colocated file `route.test.ts`

**Goal:** New Vitest file colocated with the GC route asserts dry-run / commit / admin-gate / reference-protection paths per TECH-774 §Test Blueprint. Uses Vitest `describe` / `it` / `expect` API (NOT `node:test`). Seeds DB fixtures via existing web/ test helpers (resolved at implement-time).

**Edits:**
- `web/app/api/catalog/sprites/gc/route.test.ts` — **operation**: create
  **after** — new file contents:
  ```ts
  /**
   * TECH-775 — Regression tests for TECH-774 POST /api/catalog/sprites/gc.
   * Colocated with route.ts per web/ Vitest convention.
   */
  import { describe, it, expect, beforeEach } from "vitest";

  // NOTE: concrete imports resolved at implement-time.
  // Expected neighbors:
  //   - POST handler from "./route"
  //   - DB seed helpers: resolve per existing neighbor tests under web/tests/api/catalog/
  //   - Admin mock: resolve via Vitest module-mock of the concrete admin-check symbol chosen in TECH-774 Step 2

  describe("POST /api/catalog/sprites/gc", () => {
    beforeEach(async () => {
      // TECH-775: reset test DB + seed baseline sprites/assets/asset_sprite/pool rows.
    });

    it("dryRun default returns orphan candidates without mutation", async () => {
      expect.fail("TECH-775: seed 3 sprites (1 orphan, 2 referenced); POST {}; assert candidates=[orphanId], count=1; DB row count unchanged.");
    });

    it("dryRun=true explicit behaves identically to default", async () => {
      expect.fail("TECH-775: same seed; POST { dryRun: true }; assert response matches default.");
    });

    it("commit path deletes orphans only", async () => {
      expect.fail("TECH-775: same seed; POST { dryRun: false }; assert deletedCount=1; referenced rows still present.");
    });

    it("sprite referenced via catalog_asset_sprite is preserved", async () => {
      expect.fail("TECH-775: seed sprite w/ catalog_asset_sprite row; dryRun candidates exclude it; commit does NOT delete it.");
    });

    it("sprite whose asset is in catalog_pool_member is preserved (transitive)", async () => {
      expect.fail("TECH-775: seed sprite + asset_sprite + pool_member(asset_id); preserved in both paths.");
    });

    it("non-admin caller receives 403 not_allowed with no DB query", async () => {
      expect.fail("TECH-775: stub requireAdmin → reject; assert 403 { error: 'not_allowed' } + no DB mutation observed.");
    });
  });
  ```
- `invariant_touchpoints`:
  - id: `vitest-route-parent-dir-exists`
    gate: `test -d web/app/api/catalog`
    expected: pass
  - id: `vitest-runner-wired`
    gate: `grep -n '"test": "vitest run' web/package.json`
    expected: pass
- `validator_gate`: `npm -w web run typecheck`

**Gate:**
```bash
npm -w web run typecheck
```
Expectation: exit 0 (colocated Vitest file typechecks against TECH-774 route + DTO landing).

**STOP:** Typecheck drift → re-run Step 2. If implementer drifts into `node:test` idioms (per `web/lib/catalog/stable-json-stringify.test.ts`) instead of Vitest `describe`/`it` → re-open Step 2 and align: Vitest is the script runner per `web/package.json:16` (`"test": "vitest run --passWithNoTests"`). Non-admin stub MUST fire BEFORE any DB call — assertion required to catch auth-gate regression.

**MCP hints:** `plan_digest_verify_paths`, `plan_digest_resolve_anchor`.

#### Step 3 — Flesh test bodies against TECH-773 + TECH-774 public API (post-landing)

**Goal:** Replace `Assert.Fail` / `expect.fail` stubs in Steps 1–2 with real fixtures + assertions once TECH-773 (`GridAssetRemap`) + TECH-774 (route + DTOs) have landed on the branch. Stage 3.3 Phase 2 Exit criteria gate this step — sequence must be TECH-772 → TECH-773 → TECH-774 → TECH-775.

**Edits:**
- `Assets/Tests/EditMode/GridAssetCatalog/GridAssetRemapTests.cs` — **operation**: edit
  **before**:
  ```
  Assert.Fail("TECH-775: author fixture + assertion against TECH-773 GridAssetRemap API.");
  ```
  **after**:
  ```
  // Snapshot JSON built inline (per GridAssetCatalogParseTests.cs MinFixture pattern):
  //   "assets": [ { "id": 1, "replaced_by": "2" }, { "id": 2, "replaced_by": "3" }, { "id": 3, "replaced_by": null } ]
  // GridAssetCatalog.TryParseSnapshotJson → snapshot root; pass to GridAssetRemap.RemapAssetId(1, root, out bool remapped).
  // Assert.AreEqual(3, remapped_id); Assert.IsTrue(remapped);
  ```
- `web/app/api/catalog/sprites/gc/route.test.ts` — **operation**: edit
  **before**:
  ```
  expect.fail("TECH-775: seed 3 sprites (1 orphan, 2 referenced); POST {}; assert candidates=[orphanId], count=1; DB row count unchanged.");
  ```
  **after**:
  ```
  // Seed via web/ test DB helpers; invoke POST handler directly (App Router pattern: `await POST(new NextRequest(...))`).
  // Assert response.status === 200; body.candidates to equal [orphanId]; body.count === 1.
  // Post-commit SELECT count confirms DB unchanged.
  ```
- `invariant_touchpoints`:
  - id: `sequencing-tech-773-774-landed`
    gate: `backlog_issue TECH-773` + `backlog_issue TECH-774` (both Status=Done before Step 3 executes)
    expected: pass
  - id: `validate-all-green`
    gate: `npm run validate:all`
    expected: pass
- `validator_gate`: `npm run validate:all`

**Gate:**
```bash
npm run validate:all
```
Expectation: exit 0. EditMode batch green + Vitest green + no other chain regressions.

**STOP:** `Assert.Fail` / `expect.fail` still present after Step 3 → re-open Step 3; tests are gated stubs, not acceptance. If TECH-773 or TECH-774 not merged yet → Step 3 blocked — re-sequence per Stage 3.3 Phase 2 Exit (do NOT merge Step 3 ahead of its dependencies). If `GridAssetRemap` public signature differs from spec (e.g. helper returns struct instead of `out bool`) → update the §Acceptance signature line + Step 3 assertions in the same commit.

**MCP hints:** `plan_digest_resolve_anchor`, `backlog_issue TECH-773`, `backlog_issue TECH-774`.

#### Step 4 — Decision Log entries

**Goal:** Record the two concrete test-layout choices made during digest authoring so downstream readers do not re-debate.

**Edits:**
- `ia/projects/TECH-775.md` — **operation**: edit
  **before**:
  ```
  | 2026-04-24 | EditMode + server separate | Domain separation (load vs server) | Single fixture (less clear boundaries) |
  | 2026-04-24 | Standard test patterns | Consistency with repo test suite | Custom test framework (overhead) |
  ```
  **after**:
  ```
  | 2026-04-24 | EditMode + server separate | Domain separation (load vs server) | Single fixture (less clear boundaries) |
  | 2026-04-24 | Standard test patterns | Consistency with repo test suite | Custom test framework (overhead) |
  | 2026-04-24 | Vitest colocated `route.test.ts` (not `__tests__/`) | Matches `web/lib/catalog/stable-json-stringify.test.ts` + Next.js App Router colocated convention | Nested `__tests__/` (used by `web/lib/__tests__/` but not by route tests) |
  | 2026-04-24 | EditMode test dir = `Assets/Tests/EditMode/GridAssetCatalog/` | Sibling convention (`GridAssetCatalogParseTests.cs`, `CursorPlacementPreviewTests.cs`, `PlacementReasonTooltipTests.cs`) + shared `min_snapshot.json` fixture | Flat `Assets/Tests/EditMode/` (stub §5.2 draft; rejected as inconsistent) |
  ```
- `invariant_touchpoints`:
  - id: `decision-log-column-parity`
    gate: `grep -c "^| 2026-04-24" ia/projects/TECH-775.md`
    expected: pass
- `validator_gate`: `npm run validate:frontmatter`

**Gate:**
```bash
npm run validate:frontmatter
```
Expectation: exit 0.

**STOP:** Decision Log pipe count mismatch → re-open Step 4 aligning `| a | b | c | d |` column parity.

**MCP hints:** `plan_digest_resolve_anchor`.

## Open Questions (resolve before / during implementation)

- **Glossary candidate:** `Grid asset catalog` — see TECH-772 §Open Questions.
- **Web test runner:** Vitest vs Jest — confirm via `web/package.json` at implement time.
- **CI chain:** verify `npm run validate:all` invokes both Unity EditMode batch + web test runner; add explicit entry if gap exists.
- **Sequencing:** this Task depends on TECH-773 + TECH-774 merged first (Stage 3.3 Phase 2 Exit criteria).

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor | critical._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
