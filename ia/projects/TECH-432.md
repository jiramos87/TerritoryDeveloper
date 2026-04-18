---
purpose: "TECH-432 — Cross-read stanza consistency across 6 wired skills (Stage 2.1 T2.1.3)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/skill-training-master-plan.md"
task_key: "T2.1.3"
---
# TECH-432 — Cross-read stanza consistency across 6 wired skills

> **Issue:** [TECH-432](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

## 1. Summary

Byte-for-byte audit of Phase-N-tail stanza across 6 wired SKILL.md files (output of TECH-430 + TECH-431). Verify verbatim template match, identical `schema_version` stamps, and `## Changelog` presence. Any deviation → patch inline + log `source: wiring-review` entry on affected skill.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Cross-read 6 files: `design-explore`, `master-plan-new`, `master-plan-extend`, `stage-decompose`, `stage-file`, `project-new`.
2. Verify stanza text matches canonical `skill-train/SKILL.md §Emitter stanza template` byte-for-byte (placeholders exempted).
3. Verify all 6 `schema_version` date-stamps are identical.
4. Verify all 6 carry `## Changelog` section.
5. Any deviation fixed inline; recorded as `source: wiring-review` §Changelog entry.

### 2.2 Non-Goals

1. No re-validation of 7 Stage 2.2 skills (out of scope; separate stage).
2. No edits to template source of truth.
3. No `validate:all` run here — deferred to TECH-433.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a reviewer of Stage 2.1 output, I want confidence that all 6 stanza copies match canonical template exactly so `skill-train` aggregation sees consistent schema across producer surfaces. | Diff audit passes on 6 files; any mismatch fixed + logged. |

## 4. Current State

### 4.1 Domain behavior

Post TECH-430 + TECH-431 the 6 skills each carry a stanza copy. Paste errors / auto-formatter drift / placeholder typos possible. Consumer (`skill-train`) warns on `schema_version` mismatch but still aggregates — audit catches mismatches proactively.

### 4.2 Systems map

- Canonical template: `ia/skills/skill-train/SKILL.md §Emitter stanza template`.
- 6 audit targets: 3 authoring (TECH-430 output) + 3 filing (TECH-431 output).
- Audit tool: `diff` or manual side-by-side read; MCP `spec_section` for template slice.
- §Changelog entry format: `source: wiring-review` when deviation found.

### 4.3 Implementation investigation notes

Audit is read-only by default. Edit path only when mismatch found. Expected mismatches: whitespace drift, JSON field reordering by formatter, missing `schema_version` line, wrong date substitution.

## 5. Proposed Design

### 5.1 Target behavior

Audit produces PASS/FAIL per file. FAIL → inline patch + §Changelog entry noting deviation and correction. Final state: 6 stanzas byte-identical (minus placeholders).

### 5.2 Architecture / implementation

1. MCP-load canonical stanza block once: `mcp__territory-ia__spec_section skill-train "Emitter stanza template"`.
2. For each of 6 files: extract its stanza block; compare line-by-line vs canonical (substituting placeholder values back).
3. Mismatch → edit target file; restore canonical text; append `source: wiring-review` entry to that file's `## Changelog` noting what drifted and what was restored.
4. Confirm final state: 6 files match; 6 `schema_version` stamps equal; 6 §Changelog sections present.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-18 | Audit is separate task from wiring tasks | Single-responsibility keeps wiring PR small and audit PR focused; also provides a checkpoint before `validate:all` | Fold audit into TECH-430/431 rejected — mixes concerns, hides drift |

## 7. Implementation Plan

### Phase 1 — Read canonical + read 6 targets

- [ ] MCP slice `skill-train/SKILL.md §Emitter stanza template`.
- [ ] Read 6 SKILL.md stanza sections.
- [ ] Tabulate mismatches (expected 0; non-zero triggers Phase 2).

### Phase 2 — Patch mismatches + log

- [ ] For each mismatch: edit target; restore canonical text; append `source: wiring-review` §Changelog entry.
- [ ] Confirm 6-file match after patches.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| 6 stanzas byte-match canonical | Manual diff | side-by-side read or `diff` | Placeholders substituted back before compare |
| `schema_version` identical across 6 | Manual | grep `schema_version` in 6 files | All dates equal |

## 8. Acceptance Criteria

- [ ] 6 SKILL.md files carry stanza byte-matching canonical template (placeholders substituted).
- [ ] 6 `schema_version` date-stamps identical.
- [ ] 6 `## Changelog` sections present.
- [ ] Any deviation logged as `source: wiring-review` entry + fixed.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

None — tooling only; see §8 Acceptance criteria.
