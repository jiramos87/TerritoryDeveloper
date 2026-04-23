---
purpose: "TECH-708 — DAS §2.6 pointer: signatures are the canonical runtime calibration source."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.2.5
---
# TECH-708 — DAS §2.6 pointer — signatures are the canonical runtime calibration source

> **Issue:** [TECH-708](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Add §2.6 to `docs/sprite-gen-art-design-system.md`: a short forward-pointer block naming `tools/sprite-gen/signatures/` + `src/signature.py` as the canonical runtime calibration source. No JSON shape duplication — the authoritative spec lives in the signature module docstring.

## 2. Goals and Non-Goals

### 2.1 Goals

1. New §2.6 block in DAS.
2. Pointer cites `tools/sprite-gen/signatures/` + `src/signature.py`.
3. Brief — one paragraph.

### 2.2 Non-Goals

1. Documenting JSON shape in DAS — lives in module docstring.
2. Documenting L15 policy in DAS — lives in TECH-704 spec + module docstring.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | New contributor | Find calibration docs via DAS | DAS §2.6 explicitly names `signatures/` dir + module |

## 4. Current State

### 4.1 Domain behavior

DAS §2 covers geometry (§2.1 diamond, §2.2 canvas formula / pivot UV, §2.3 reference metrics, §2.4 level heights, §2.5 `LEVEL_H` table). No pointer to signatures today.

### 4.2 Systems map

- `docs/sprite-gen-art-design-system.md` (target).
- References: `tools/sprite-gen/signatures/` + `src/signature.py`.

### 4.3 Implementation investigation notes

Existing §2.5 ends just before §3 decoration primitives. Insert §2.6 between them.

## 5. Proposed Design

### 5.1 Target content

```markdown
### §2.6 Calibration signatures

Calibration signatures are the canonical runtime calibration source. Per-class envelopes (bbox / palette / silhouette / ground / decoration hints) live under [`tools/sprite-gen/signatures/<class>.signature.json`](../tools/sprite-gen/signatures/). Authoritative schema + sample-size policy in the [`src/signature.py`](../tools/sprite-gen/src/signature.py) module docstring. Regenerate via `python3 -m src refresh-signatures [class?]`.
```

### 5.2 Architecture / implementation

Pure doc edit.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Pointer-only in DAS; schema stays in module docstring | Single source of truth for the JSON shape | Duplicate in DAS — rejected, drift risk |

## 7. Implementation Plan

### Phase 1 — Insert §2.6 block

- [ ] Locate end of §2.5 in `docs/sprite-gen-art-design-system.md`.
- [ ] Append §2.6 content.
- [ ] Confirm markdown renders via repo's doc linter if one exists.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Section present | Grep | `grep -n "§2.6 Calibration signatures" docs/sprite-gen-art-design-system.md` | ≥1 hit |
| Links valid | Manual | Render preview in markdown viewer | Relative paths resolve |

## 8. Acceptance Criteria

- [ ] §2.6 block added.
- [ ] Cites `signatures/` + `src/signature.py`.
- [ ] Grep hit confirms section header.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- DAS should forward-point, not re-document; living docs belong next to code.

## §Plan Digest

### §Goal

Insert DAS §2.6 — a one-paragraph forward-pointer to `tools/sprite-gen/signatures/` + `src/signature.py` as the canonical runtime calibration source.

### §Acceptance

- [ ] `docs/sprite-gen-art-design-system.md` contains `### §2.6 Calibration signatures` (or equivalent heading level)
- [ ] Block cites `tools/sprite-gen/signatures/` + `src/signature.py`
- [ ] `grep -n "signatures/" docs/sprite-gen-art-design-system.md` → ≥1 hit in §2.6

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| section_header_present | `docs/sprite-gen-art-design-system.md` | `grep` hit on `§2.6 Calibration signatures` | shell |
| pointer_present | same | `grep` hit on `tools/sprite-gen/signatures/` | shell |

### §Examples

Target insert (between §2.5 and §3):

```markdown
### §2.6 Calibration signatures

Calibration signatures are the canonical runtime calibration source. Per-class envelopes (bbox / palette / silhouette / ground / decoration hints) live under [`tools/sprite-gen/signatures/<class>.signature.json`](../tools/sprite-gen/signatures/). Authoritative schema + sample-size policy in the [`src/signature.py`](../tools/sprite-gen/src/signature.py) module docstring. Regenerate via `python3 -m src refresh-signatures [class?]`.
```

### §Mechanical Steps

#### Step 1 — Locate §2.5 end

**Gate:**

```bash
grep -n "^### §2\." docs/sprite-gen-art-design-system.md
```

**STOP:** Look for `§2.5`; target insert immediately after its final paragraph.

#### Step 2 — Insert §2.6 block

**Edits:**

- `docs/sprite-gen-art-design-system.md` — append §2.6 content above `### §3` (or wherever the next section boundary lives).

**Gate:**

```bash
grep -n "§2.6 Calibration signatures" docs/sprite-gen-art-design-system.md
```

**STOP:** 0 hits → insert failed.

**MCP hints:** none — pure doc edit.

## Open Questions (resolve before / during implementation)

1. Heading level — DAS uses `###` for §2.x subsections? **Resolution:** match whatever `§2.5` uses (read DAS before editing).

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
