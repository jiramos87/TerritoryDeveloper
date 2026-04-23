---
purpose: "TECH-744 — DAS §5 R11 amendment: replace hard-coded tiled-row-3/4 with parametric tiled-(row|column)-N grammar."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T9.2.4
---
# TECH-744 — DAS §5 R11 amendment — parametric slot grammar

> **Issue:** [TECH-744](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Amend `docs/sprite-gen-art-design-system.md` §5 R11 (slot grammar table) to replace hard-coded `tiled-row-3/4` / `tiled-column-3` entries with a single parametric row documenting `tiled-(row|column)-N` for `N ≥ 2`. Add a forward pointer to `row_houses_3x` preset (TECH-734) as a consumer. Capstone of the Stage 9 addendum — writes last so doc reflects the actual parser + resolver.

## 2. Goals and Non-Goals

### 2.1 Goals

1. §5 R11 documents `tiled-(row|column)-N` with `N ≥ 2`.
2. Hard-coded `tiled-row-3/4` entries removed or redirected.
3. Forward pointer to `row_houses_3x` preset (TECH-734) present.

### 2.2 Non-Goals

1. Implementing the grammar — TECH-741/742.
2. Tests — TECH-743.
3. DAS sections outside §5 R11.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Spec author | Discover grammar from DAS | §5 R11 parametric entry is scannable |
| 2 | Future Stage 9 reader | Trust doc-code alignment | Row mentions `N ≥ 2` explicitly |
| 3 | Reviewer | Cross-reference consumer | Forward pointer to TECH-734 present |

## 4. Current State

### 4.1 Domain behavior

DAS §5 R11 (authored in the Stage 9 master plan block T9.2) lists hard-coded slot names: `centered`, `front-left`, …, `tiled-row-3`, `tiled-row-4`, `tiled-column-3`.

### 4.2 Systems map

- `docs/sprite-gen-art-design-system.md` §5 R11 — amend here.

### 4.3 Implementation investigation notes

Replacement row must cite the regex shape + `N ≥ 2` lower bound. Keep the hard-coded row as a redirect note rather than silently deleting — preserves audit trail for readers familiar with legacy names.

## 5. Proposed Design

### 5.1 Target behavior

Before:

```markdown
| `tiled-row-3`    | 3 buildings evenly spaced N→S. |
| `tiled-row-4`    | 4 buildings evenly spaced N→S. |
| `tiled-column-3` | 3 buildings evenly spaced E→W. |
```

After:

```markdown
| `tiled-(row|column)-N` | N ≥ 2 buildings evenly spaced along the named axis. Integer-pixel anchors. Example preset: [`row_houses_3x`](../tools/sprite-gen/presets/row_houses_3x.yaml) uses `tiled-row-3`. |

> Legacy names `tiled-row-3`, `tiled-row-4`, `tiled-column-3` continue to parse through the parametric grammar.
```

### 5.2 Architecture / implementation

- Docs-only change to one table row + one note.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Replace hard-coded rows + legacy-note | Single source of truth; audit trail preserved | Keep hard-coded rows — rejected, drift risk |
| 2026-04-23 | Cite `row_houses_3x` as example | Live consumer anchors the abstraction | Generic example — rejected, loses traceability |

## 7. Implementation Plan

### Phase 1 — Locate §5 R11 slot table

### Phase 2 — Replace hard-coded rows with parametric entry

### Phase 3 — Add legacy-compat note + preset forward pointer

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Parametric grammar present | Grep | `grep "tiled-(row\|column)-N" docs/sprite-gen-art-design-system.md` | — |
| `N ≥ 2` phrase present | Grep | `grep -E "N ≥ 2\|N >= 2" docs/sprite-gen-art-design-system.md` | — |
| Forward pointer | Grep | `grep "row_houses_3x" docs/sprite-gen-art-design-system.md` | — |

## 8. Acceptance Criteria

- [ ] §5 R11 documents `tiled-(row|column)-N` with `N ≥ 2`.
- [ ] Hard-coded `tiled-row-3/4` entries removed or redirected.
- [ ] Forward pointer to `row_houses_3x` preset (TECH-734) present.
- [ ] Grep check confirms parametric grammar phrase in §5 R11.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Capstone docs written last catch drift — a doc written before the parser would likely miss the `N ≥ 2` edge case and the count-mismatch raise.

## §Plan Digest

### §Goal

Finalise the Stage 9 addendum by amending DAS §5 R11 to reflect the parametric slot grammar + legacy-name compatibility, with a forward pointer to the `row_houses_3x` preset as a live consumer.

### §Acceptance

- [ ] DAS §5 R11 contains a row for `tiled-(row|column)-N`
- [ ] Row specifies `N ≥ 2` lower bound
- [ ] Hard-coded `tiled-row-3`, `tiled-row-4`, `tiled-column-3` rows removed or replaced with a legacy-note
- [ ] Forward pointer to `tools/sprite-gen/presets/row_houses_3x.yaml` present
- [ ] Grep passes: `tiled-(row|column)-N`, `N ≥ 2`, `row_houses_3x`

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| grep_parametric_grammar | DAS §5 R11 | `tiled-(row\|column)-N` present | bash |
| grep_n_lower_bound | DAS §5 R11 | `N ≥ 2` or `N >= 2` present | bash |
| grep_forward_pointer | DAS | `row_houses_3x` referenced | bash |
| grep_legacy_note | DAS §5 R11 | `tiled-row-3` mentioned in legacy context | bash |

### §Examples

See §5.1 above for the target Markdown.

### §Mechanical Steps

#### Step 1 — Locate §5 R11

**Edits:** none.

**Gate:**

```bash
grep -n "R11" docs/sprite-gen-art-design-system.md
```

#### Step 2 — Replace hard-coded rows

**Edits:**

- `docs/sprite-gen-art-design-system.md` §5 R11 — swap hard-coded `tiled-row-3/4` + `tiled-column-3` rows for single parametric entry.

**Gate:**

```bash
grep -E "tiled-\(row\|column\)-N" docs/sprite-gen-art-design-system.md
```

#### Step 3 — Legacy note + forward pointer

**Edits:**

- Same file — append legacy-compat note + `row_houses_3x` preset pointer.

**Gate:**

```bash
grep "row_houses_3x" docs/sprite-gen-art-design-system.md
grep -E "N ≥ 2|N >= 2" docs/sprite-gen-art-design-system.md
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Are R11's hard-coded rows already present in DAS or do they land only when Stage 9 master block files? **Resolution:** inspect at merge time — if not yet present, TECH-744 creates the R11 row afresh with the parametric grammar.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
