---
purpose: "TECH-701 — Formalize pivot_pad hotfix + DAS-cited comment at compose.py:256."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.1.1
---
# TECH-701 — Formalize pivot_pad patch + DAS-cited comment at compose.py:256

> **Issue:** [TECH-701](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Retroactive filing of in-session pivot hotfix at `tools/sprite-gen/src/compose.py:256`. Composer anchors building primitives 17 px above canvas bottom when ground diamond is present; inline comment must cite DAS §2.1 (diamond bottom at `y = canvas_h − 17`) + §2.2 (pivot UV = `16 / canvas_h`; `+1` for PIL inclusive pixel indexing). No code-path change — patch already live in working tree.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Confirm `pivot_pad = 17 if spec.get("ground") != "none" else 0` at `compose.py:256`.
2. Confirm `adjusted_y0 = y0 - pivot_pad - offset_z` at `compose.py:260`.
3. Inline comment explicitly names DAS §2.1 + §2.2.

### 2.2 Non-Goals (Out of Scope)

1. Re-applying or modifying the pivot patch — already landed in working tree.
2. Moving `pivot_pad` to a named constant — wording lock only.
3. Tightening tests — belongs to TECH-702 / TECH-703.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Sprite-gen dev | DAS §2.1/§2.2 source-of-truth is cited at the patch site so future edits do not drift | Inline comment references the DAS sections; pytest green |

## 4. Current State

### 4.1 Domain behavior

Composer already applies `pivot_pad = 17` when a ground diamond is present. Visual output matches `House1-64.png` reference. No runtime bug.

### 4.2 Systems map

- `tools/sprite-gen/src/compose.py` — pivot_pad block at line 256 (live).
- `docs/sprite-gen-art-design-system.md` §2.1 (diamond geometry), §2.2 (canvas formula + pivot UV).

### 4.3 Implementation investigation notes (optional)

Patch landed in-session during 2026-04-23 sprite-gen improvement session (`/tmp/sprite-gen-improvement-session.md` §1 L14). Comment already reads "DAS §2.1/§2.2" — this task locks that wording and confirms the DAS sections are the authoritative derivation source.

## 5. Proposed Design

### 5.1 Target behavior (product)

Comment at `compose.py:256` reads (canonical):

```
# DAS §2.1/§2.2: diamond bottom row is at y = canvas_h - 17 (16 px pad + 1 for
# PIL inclusive pixel indexing). Building primitives anchor at diamond bottom,
# not canvas bottom. Stage 6.1 hotfix.
```

### 5.2 Architecture / implementation

Pure comment lock. `pivot_pad` remains a ternary expression inline; no extraction.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-23 | Keep ternary inline, do not extract named constant | Single call-site; extraction would add indirection without payoff | Named `PIVOT_PAD_PX` in `constants.py` — deferred |

## 7. Implementation Plan

### Phase 1 — Comment wording lock

- [ ] Read `tools/sprite-gen/src/compose.py:253-260`.
- [ ] Confirm comment cites DAS §2.1 + §2.2 and explains `17 = 16 + 1`.
- [ ] Adjust wording if drifted (no functional change).
- [ ] Run `cd tools/sprite-gen && python3 -m pytest tests/ -q` — expect 218+ green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Patch still compiles | Python | `cd tools/sprite-gen && python3 -m pytest tests/ -q` | Expect 218+ green; no new tests in this task |
| Comment cites DAS | Review | `grep -n "DAS §2.1" tools/sprite-gen/src/compose.py` | Expect ≥1 hit near line 256 |

## 8. Acceptance Criteria

- [ ] `pivot_pad = 17 if spec.get("ground") != "none" else 0` lives at `compose.py:256`.
- [ ] `adjusted_y0 = y0 - pivot_pad - offset_z` follows it.
- [ ] Comment names DAS §2.1 + §2.2.
- [ ] Pytest green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- In-session hotfixes need retroactive filing before the stage line moves on, otherwise the invariant loses its citation trail.

## §Plan Digest

### §Goal

Lock the DAS §2.1/§2.2 citation on the in-session pivot hotfix at `tools/sprite-gen/src/compose.py:256`. Code is already live; this task prevents comment drift so future readers can trace `pivot_pad = 17` back to `canvas_h − 16 − 1 (inclusive pixel)`.

### §Acceptance

- [ ] `compose.py:256` literal `pivot_pad = 17 if spec.get("ground") != "none" else 0`
- [ ] `compose.py:260` literal `adjusted_y0 = y0 - pivot_pad - offset_z`
- [ ] Inline comment (lines 256–258) cites `DAS §2.1` and `DAS §2.2`
- [ ] `cd tools/sprite-gen && python3 -m pytest tests/ -q` → 218+ passed

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| pytest_full_suite | `cd tools/sprite-gen` | 218+ passed | `python3 -m pytest tests/ -q` |
| comment_cites_DAS | `compose.py` around line 256 | `"DAS §2.1"` + `"DAS §2.2"` both match | `grep -n "DAS §2" tools/sprite-gen/src/compose.py` |

### §Examples

Canonical comment block (target state):

```python
material = str(entry.get("material", ""))
offset_z = int(entry.get("offset_z", 0))
# DAS §2.1/§2.2: diamond bottom row is at y = canvas_h - 17 (16 px pad + 1 for
# PIL inclusive pixel indexing). Building primitives anchor at diamond bottom,
# not canvas bottom. Stage 6.1 hotfix.
pivot_pad = 17 if spec.get("ground") != "none" else 0
adjusted_y0 = y0 - pivot_pad - offset_z
```

### §Mechanical Steps

#### Step 1 — Read current comment block

**Goal:** Confirm live state matches target wording.

**Edits:**

- `tools/sprite-gen/src/compose.py` — read lines 253–262.

**Gate:**

```bash
grep -n "DAS §2" tools/sprite-gen/src/compose.py
```

**STOP:** If no DAS citation on any line near 256 → proceed to Step 2 edit. If citation present but wording drift → proceed to Step 2 rewrite.

#### Step 2 — Lock comment wording (only if drift detected)

**Goal:** Re-author the comment block using the §Examples canonical form.

**Edits:**

- `tools/sprite-gen/src/compose.py` — replace comment block above `pivot_pad = 17 …` with canonical 3-line form.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/ -q
```

**STOP:** Any test failure → revert edit (comment-only change should be a no-op for tests).

**MCP hints:** none — pure file edit.

## Open Questions (resolve before / during implementation)

1. None — wording lock only; DAS §2.1/§2.2 already derive the 17-px pad unambiguously.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
