---
purpose: "TECH-736 — DAS §6 addendum: preset: <name> key, merge rule, vary preservation, seeded preset catalogue."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.6.7
---
# TECH-736 — DAS §6 addendum — preset contract + catalogue

> **Issue:** [TECH-736](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Amend `docs/sprite-gen-art-design-system.md` §6 to document the new preset system: the `preset: <name>` top-level key grammar + resolution rule, the author-override merge rule, the `vary:` preservation + wipe-guard semantic, and a catalogue of the three seeded presets. Forward-pointer to `presets/` dir for discoverability. Doc is written only after all code tasks (TECH-730..734) ship so it reflects actual behavior.

## 2. Goals and Non-Goals

### 2.1 Goals

1. §6 documents `preset: <name>` grammar + resolution rule.
2. §6 documents merge rule (author overrides; `vary:` union; wipe raises).
3. §6 catalogues the three seeded presets with short descriptions.

### 2.2 Non-Goals

1. Implementing the preset system — TECH-730..734.
2. DAS sections outside §6.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Spec author | Learn preset grammar from docs | §6 addendum covers all 3 grammar rules |
| 2 | Reviewer | Cross-reference preset catalogue | Table lists all 3 seeded presets |
| 3 | Repo guardian | Grep verifies doc-code alignment | Grep check finds `preset:`, `vary:` merge rule, all 3 preset names |

## 4. Current State

### 4.1 Domain behavior

DAS §6 documents the pre-preset spec grammar; no preset references exist.

### 4.2 Systems map

- `docs/sprite-gen-art-design-system.md` §6 — amend here.
- Forward-pointer: `tools/sprite-gen/presets/`.

### 4.3 Implementation investigation notes

Doc task is last in dependency chain so merge-time reality — including any late shifts in axis names or `strategy: per_tile` key — is captured accurately. Use a table for the catalogue (one row per preset).

## 5. Proposed Design

### 5.1 Target behavior

New subsection under §6:

```markdown
### §6.N Preset system

A spec may bootstrap from a named preset via `preset: <name>` at the top level.
The loader reads `tools/sprite-gen/presets/<name>.yaml` as the base spec, then
applies author-supplied fields as overrides (author wins per-field; deep merge
for nested dicts). Missing presets raise `SpecError` with a valid-names list.

**`vary:` merge rule:**
- Preset axes survive unless explicitly overridden per axis.
- Author may add new axes (set-union).
- Author-supplied `vary: {}` or `vary: null` raises `SpecError` — wiping the
  block silently disables preset-driven variation and is refused.

**Seeded presets** (under `tools/sprite-gen/presets/`):

| Name | Footprint | Ground | Vary axes | Notes |
|------|-----------|--------|-----------|-------|
| `suburban_house_with_yard` | 1×1 | grass | roof, facade, ground | residential_small |
| `strip_mall_with_parking`  | 1×1 (wide) | pavement | facade, ground | commercial strip |
| `row_houses_3x`            | 2×2 | grass (shared) | facade, roof (per_tile) | uses Stage 9 tiled-row-3 slot |
```

### 5.2 Architecture / implementation

- Docs-only change.
- Validated via grep check in acceptance.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Single subsection under §6 | DAS convention — grammar docs live in §6 | Separate §6.1 file — rejected, fragmentation |
| 2026-04-23 | Table catalogue | Scannable; matches existing DAS tables | Prose — rejected, harder to extend |

## 7. Implementation Plan

### Phase 1 — Locate §6 insertion point

### Phase 2 — Write contract + merge rule + wipe semantic

### Phase 3 — Catalogue table + forward-pointer

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Grammar present | Grep | `grep -E "^preset: <name>" docs/sprite-gen-art-design-system.md` | — |
| Merge rule present | Grep | `grep -E "vary.*union\|author overrides" docs/sprite-gen-art-design-system.md` | — |
| All 3 presets named | Grep | `grep -E "suburban_house_with_yard\|strip_mall_with_parking\|row_houses_3x" docs/sprite-gen-art-design-system.md` | Must return 3+ hits |

## 8. Acceptance Criteria

- [ ] §6 documents `preset: <name>` grammar + resolution rule.
- [ ] §6 documents merge rule (author overrides; `vary:` union; wipe raises).
- [ ] §6 catalogues the three seeded presets with short descriptions.
- [ ] Grep check: `preset:`, `vary:` merge rule, and all three preset names present.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- DAS addenda written last catch drift; writing docs first would bake in pre-merge assumptions that ~always shift during implementation.

## §Plan Digest

### §Goal

Document the preset system contract in DAS §6 — grammar, merge rule, wipe-guard, and seeded preset catalogue — so future authors + reviewers have a single source of truth.

### §Acceptance

- [ ] `docs/sprite-gen-art-design-system.md` §6 contains a "Preset system" subsection
- [ ] Subsection covers: `preset: <name>` grammar, author-override merge, `vary:` union, wipe-raises
- [ ] Subsection carries a table listing all 3 seeded presets (name / footprint / ground / vary axes)
- [ ] Forward-pointer to `tools/sprite-gen/presets/` present
- [ ] `grep` returns hits for each preset name + `preset:` grammar + `vary:` merge-rule phrases

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| grep_preset_grammar | DAS §6 | `preset: <name>` phrase present | bash |
| grep_merge_rule | DAS §6 | "author overrides" and "union" present | bash |
| grep_all_seeded_presets | DAS §6 | 3 preset name matches | bash |
| grep_forward_pointer | DAS §6 | `tools/sprite-gen/presets/` phrase present | bash |

### §Examples

See §5.1 above for the target subsection Markdown.

### §Mechanical Steps

#### Step 1 — Locate §6 insertion point

**Edits:** none yet.

**Gate:**

```bash
grep -n "^## §6" docs/sprite-gen-art-design-system.md
```

#### Step 2 — Write contract + merge rule

**Edits:**

- `docs/sprite-gen-art-design-system.md` — insert new subsection after §6 landing text with grammar + merge rule.

**Gate:**

```bash
grep -E "preset: <name>" docs/sprite-gen-art-design-system.md
grep -E "(author overrides|vary.*union|wipe.*raise)" docs/sprite-gen-art-design-system.md
```

#### Step 3 — Catalogue table + forward-pointer

**Edits:**

- Same file — insert catalogue table + `tools/sprite-gen/presets/` pointer.

**Gate:**

```bash
for p in suburban_house_with_yard strip_mall_with_parking row_houses_3x; do
  grep -q "$p" docs/sprite-gen-art-design-system.md || { echo "missing $p"; exit 1; }
done
echo "all preset names present"
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Does DAS §6 already have subsection numbering we should follow (§6.1, §6.2…)? **Resolution:** inspect file at merge time; insert with the next available number. If §6 is flat, append as `### §6 Preset system`.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
