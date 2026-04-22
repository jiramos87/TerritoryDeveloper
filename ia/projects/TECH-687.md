---
purpose: "TECH-687 — EditMode tests for seven Zone S subTypeIds → catalog costs + display names."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T2.3.4
phases:
  - "Phase 1 — Fixture + table-driven tests"
---
# TECH-687 — EditMode tests

> **Issue:** [TECH-687](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-22
> **Last updated:** 2026-04-22

## 1. Summary

Extend or add `Assets/Tests/EditMode/Economy/` coverage so `0..6` sub-type ids resolve to expected **display** strings and **cent** costs through `ZoneSubTypeRegistry` + `GridAssetCatalog` using a `TextAsset` or `StreamingAssets` snapshot fixture that matches the repo export format. Fails with English messages on drift (seed vs map vs snapshot).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Table-driven (or loop) test for ids `0..6`.
2. Fixture path documented in §7b and stable under `Assets/` (e.g. `Resources` or `StreamingAssets` test copy).
3. NUnit + Unity EditMode only (no Play Mode).

### 2.2 Non-Goals (Out of Scope)

1. No Postgres at test time; file fixture only.
2. No screenshot / bridge tests.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|------------------------|
| 1 | Developer | I need regression when seed or map changes | Test fails on mismatch |

## 4. Current State

### 4.1 Domain behavior

`ZoneSubTypeRegistryTests` assert JSON load + `GetById`. New tests assert **catalog** agreement after TECH-684–686.

### 4.2 Systems map

- `Assets/Tests/EditMode/Economy/ZoneSubTypeRegistryTests.cs` — extend or new file
- `GridAssetCatalog` test patterns from Stage 2.2 (`min_snapshot.json` style)
- Fixture: small JSON `TextAsset` under `Assets/Tests/.../Fixtures/` if required

## 5. Proposed Design

### 5.1 Target behavior (product)

N/A — test harness.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Construct `GameObject` with `GridAssetCatalog` + `ZoneSubTypeRegistry` + test snapshot; call public registry API under test; `Assert` expected name + cents from known fixture row.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|-------------------------|
| 2026-04-22 | File fixture vs `Resources.Load` of shipping snapshot | Isolation; CI-friendly | — |

## 7. Implementation Plan

### Phase 1 — Tests

- [ ] Add or extend test class; reference fixture `TextAsset`.
- [ ] Assert seven rows for display + cents; clear failure strings.
- [ ] `npm run unity:compile-check` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| EditMode suite | Unity | nunit pass | |
| Batch compile | Node | `npm run unity:compile-check` | After all Stage 2.3 C# |

## 8. Acceptance Criteria

- [ ] At least one test file covers catalog-backed `0..6` path.
- [ ] Fixture path listed in this §7b table.
- [ ] `unity:compile-check` success recorded in §Verification at ship time.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## §Plan Digest

### §Goal

EditMode test(s) under `Assets/Tests/EditMode/Economy/` assert `subTypeId` `0..6` resolve to catalog-backed **cent** costs and **display** strings that match a committed JSON `TextAsset` (or `GridAssetCatalog` load from test path), complementing existing `GetById` / JSON tests. Failure messages in English; seven cases covered.

### §Acceptance

- [ ] New or extended `[Test]` method(s) in `ZoneSubTypeRegistryTests` or sibling file; namespace `Territory.Tests.EditMode.Economy` unchanged.
- [ ] `TextAsset` fixture under `Assets/Tests/EditMode/.../Fixtures/` (path listed in §7b) with snapshot shape accepted by `GridAssetCatalog` parser.
- [ ] `npm run unity:compile-check` exit 0 after test + fixture add.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| seven_catalog_values | `TextAsset` fixture + registry w/ `GridAssetCatalog` | 7 `Assert` pass | Unity EditMode |
| compile | solution | exit 0 | `npm run unity:compile-check` |

### §Examples

| subTypeId | check |
|-----------|--------|
| 0..6 | `TryGetAssetIdForSubType` + `GridAssetCatalog` row → name + `cost_cents` (field name per DTO) equals fixture |

### §Mechanical Steps

#### Step 1 — Add `TextAsset` fixture to repo

**Goal:** A `catalog/grid-asset-snapshot-fragment` JSON `TextAsset` under `Assets/Tests/EditMode/.../Fixtures/` containing at least the seven Zone S rows needed for assertions (or full `min_snapshot` if parser requires full shape).

**Edits:** New file `Assets/Tests/EditMode/Economy/Fixtures/zone_s_catalog_fragment.json.txt` (or `.json` with `.meta` — follow existing test fixture convention under repo). Gate: `unity:compile-check` (Unity imports new asset).

**STOP:** Parser rejects fragment → use same full-file pattern as `GridAssetCatalogParseTests` in Stage 2.2; copy `min_snapshot` layout from that test directory.

**MCP hints:** `backlog_issue` (TECH-687), `glossary_lookup` (grid asset catalog).

#### Step 2 — Add `[Test] SubTypeId_CatalogBackedCostsMatchFixture`

**Goal:** In `Assets/Tests/EditMode/Economy/ZoneSubTypeRegistryTests.cs`, append a test that instantiates `GridAssetCatalog` + `ZoneSubTypeRegistry` (with serialized refs in code via `AddComponent` + `FindObjectOfType` as existing tests do), loads fixture, and loops `0..6`.

**Edits:**

- `Assets/Tests/EditMode/Economy/ZoneSubTypeRegistryTests.cs` — **before** (unique tail: last test method from `// Reset` through EOF — verify single `Assert.AreEqual(3, zone.SubTypeId` in file):
  ```
                // Reset to default sentinel, then overwrite from json.
                zone.SubTypeId = -1;
                JsonUtility.FromJsonOverwrite(json, zone);
                Assert.AreEqual(3, zone.SubTypeId,
                    "Zone.SubTypeId must survive JsonUtility round-trip (save-format drift guard)");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
  }
  ```
  **after** (same method body + new test before class/namespace closings):
  ```
                // Reset to default sentinel, then overwrite from json.
                zone.SubTypeId = -1;
                JsonUtility.FromJsonOverwrite(json, zone);
                Assert.AreEqual(3, zone.SubTypeId,
                    "Zone.SubTypeId must survive JsonUtility round-trip (save-format drift guard)");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>TECH-687: subType 0..6 display + cent costs match TextAsset snapshot fragment.</summary>
        [Test]
        public void SubTypeIds_CatalogBacked_DisplayAndCents_MatchFixture()
        {
            Assert.Inconclusive("Replaced: load fixture, wire registry + catalog, assert seven ids.");
        }
    }
  }
  ```
  Implementer replaces `Assert.Inconclusive` with real assertions; removes placeholder message when done.

**Gate:** `cd /Users/javier/bacayo-studio/territory-developer && npm run unity:compile-check`

**STOP:** Any `Inconclusive` left in body after work → mark task not complete.

**MCP hints:** `plan_digest_resolve_anchor` on `Zone.subTypeId survives a JsonUtility` comment block.

## Open Questions (resolve before / during implementation)

**Trimmed snapshot:** a seven-row Zone S slice is acceptable in test fixtures; parser path must match production `GridAssetCatalog` `TryParse` / load entry points.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
