---
purpose: "TECH-847 — Scratchpad ledger MCP + audit-mode skill + audit-summary output-style (Tier A1+A3+C4 compaction-loop mitigation)."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-847 — Scratchpad ledger MCP + audit-mode skill + audit-summary output-style (Tier A1+A3+C4 compaction-loop mitigation)

> **Issue:** [TECH-847](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-05-08
> **Last updated:** 2026-05-08

## 1. Summary

Three-piece bundle mitigating compaction-loop token blowup mid-atomization-sweep. (1) Persistent DB table ia_scratchpad_ledger + 3 MCP tools (append/get/list) mirroring project-spec-journal.ts shape. (2) New skill audit-mode forcing per-finding ledger append BEFORE chat emission, final render via scratchpad_ledger_get(slug). (3) New output-style audit-summary forcing structured table (File|Line|Severity|Finding) wired into audit-mode Stage 3 only. Outcome: compaction summary cost degenerates from O(finding_size × turn_count) to O(turn_count).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Bundle Tier A1+A3+C4 mitigations from docs/audit/compaction-loop-mitigation.md into single coordinated issue.
2. Add persistent DB table ia_scratchpad_ledger (migration 0111) mirroring project-spec-journal.ts shape.
3. Wire 3 MCP tools (scratchpad_ledger_append, scratchpad_ledger_get, scratchpad_ledger_list) into tools/mcp-ia-server/src/index.ts.
4. Create skill ia/skills/audit-mode forcing per-finding ledger append BEFORE chat, final render via scratchpad_ledger_get(slug).
5. Create output-style .claude/output-styles/audit-summary.md forcing structured table wired into audit-mode Stage 3 only.
6. Verify compaction summary cost degenerates from O(finding_size × turn_count) to O(turn_count).

### 2.2 Non-Goals (Out of Scope)

1. Run full atomization sweep (~25-30 stages) — deferred, plan-of-record at /Users/javier/.claude-personal/plans/create-a-plan-for-splendid-fountain.md.
2. Implement Tier A2 / A4 / B / C1–C3 / C5+ mitigations — separate issues.
3. Modify existing project-spec-journal.ts surface — ledger mirrors shape only.
4. Restructure audit findings flow — ledger appends, skill surface consumes.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | I want persistent ledger logging per audit finding so audit summary cost doesn't blow up mid-sweep | Ledger append + final scratchpad_ledger_get render; structured audit-summary output in Stage 3 |
| 2 | Developer | I want audit-mode skill enforcing ledger write BEFORE chat so compaction summaries stay O(turn_count) | Skill Stage 1 ledger append gate; Stage 3 final render from DB |

## 4. Current State

### 4.1 Domain behavior

Compaction-loop token blowup during large-file atomization-refactor measured in docs/audit/compaction-loop-mitigation.md. Audit findings accumulate turn-by-turn; summary emitted at end = O(finding_size × turn_count) cost. Tier A1+A3+C4 collectively address this via persistent ledger + structured output-style.

### 4.2 Systems map

Backlog Files: tools/mcp-ia-server/src/index.ts (MCP tools), tools/db/migrations/0111-ia-scratchpad-ledger.sql (DB), ia/skills/audit-mode/SKILL.md (skill), .claude/output-styles/audit-summary.md (output-style). Reference: project-spec-journal.ts shape; arch-drift-scan/SKILL.md (MCP ledger consumption pattern); verification-report.md (output-style template).

### 4.3 Implementation investigation notes

Ledger schema: id (uuid), slug (task id), finding_type (enum: config, semantic, perf, etc.), file (path), line (int), severity (enum: info, warning, error), finding_text (long string), timestamp (utc). MCP tools (append/get/list) match pattern from existing MCP surfaces; skill consumes via Stage 1 append gate before any chat output; output-style Stage 3 only via structured table render.

## 5. Proposed Design

### 5.1 Target behavior (product)

Audit findings logged persistently to ia_scratchpad_ledger as they're discovered (Tier A1). Skill audit-mode forces per-finding append BEFORE chat emission (Tier A3). Final audit summary rendered via scratchpad_ledger_get(slug) + structured output-style audit-summary (Tier C4). Result: summary cost drops from quadratic (findings × turns) to linear (turns only).

### 5.2 Architecture / implementation

**DB table (migration 0111):** id (pk uuid), slug (task id, indexed), finding_type (enum or string), file, line, severity (enum or string), finding_text, timestamp. **MCP tools (index.ts):** scratchpad_ledger_append (POST finding, return id), scratchpad_ledger_get (GET by slug, return array), scratchpad_ledger_list (GET all, pagination). **Skill (audit-mode/SKILL.md):** frontmatter defines trigger (audit findings); Stage 1 appends each to ledger before chat; Stage 3 fetches via scratchpad_ledger_get, renders via audit-summary output-style. **Output-style (audit-summary.md):** structured table (File|Line|Severity|Finding) populated from ledger array, emitted Stage 3 only.

### 5.3 Method / algorithm notes

Append operation idempotent via (slug, file, line, severity) composite key or explicit upsert. Ledger get returns sorted by (file, line, severity). Skill Stage 1 gate: append only if ledger accepts (retry logic TBD by auditor agent). Output-style Stage 3: iterate ledger array, emit row per finding; stage 1–2 renders stay silent.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-05-08 | Bundle A1+A3+C4 into single TECH | Mid-sweep deferral; three pieces coordinate; easier to stage than separate issues | Separate issues per tier (higher coordination overhead) |
| 2026-05-08 | Mirror project-spec-journal.ts shape | Consistency with existing ledger patterns in codebase | Custom minimal schema (less context-reusable) |

## 7. Implementation Plan

### Phase 1 — DB + MCP tools

- [ ] Create tools/db/migrations/0111-ia-scratchpad-ledger.sql with schema (id, slug, finding_type, file, line, severity, finding_text, timestamp).
- [ ] Register scratchpad_ledger_append, scratchpad_ledger_get, scratchpad_ledger_list in tools/mcp-ia-server/src/index.ts.
- [ ] Document tool signatures + examples in inline comments.

### Phase 2 — audit-mode skill

- [ ] Create ia/skills/audit-mode/SKILL.md frontmatter (trigger: audit findings, stages: 1–3).
- [ ] Stage 1: append each finding to ledger via scratchpad_ledger_append; gate on success.
- [ ] Stage 3: fetch ledger via scratchpad_ledger_get(slug); pass array to output-style.

### Phase 3 — audit-summary output-style

- [ ] Create .claude/output-styles/audit-summary.md template (structured table: File|Line|Severity|Finding).
- [ ] Wire into audit-mode Stage 3 emitter.
- [ ] Verify stages 1–2 stay silent; stage 3 emits table.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| DB migration 0111 + schema validation | Node | `npm run db:migrate` | Runs on repo Postgres (`territory_ia_dev`) |
| MCP tools registered + callable | Agent / MCP | `mcp__territory-ia__*` introspect + `scratchpad_ledger_append` / `scratchpad_ledger_get` / `scratchpad_ledger_list` smoke | Validates schema, signature parsing |
| Skill frontmatter + stages parse | Node | `npm run validate:all` (chains validate:frontmatter) | Checks SKILL.md § headers exist, stages 1–3 populated |
| Output-style audit-summary.md syntax | Node | `npm run validate:all` | Caveman prose + table structure valid |
| Integration: append → fetch → render | Agent | `/implement` audit-mode skill + Stage 3 output; manual ledger query via Postgres | Verifies round-trip: finding → DB → MCP → skill → output |

## 8. Acceptance Criteria

- [ ] DB migration 0111 applied; schema matches project-spec-journal.ts shape.
- [ ] scratchpad_ledger_append, scratchpad_ledger_get, scratchpad_ledger_list registered + callable.
- [ ] ia/skills/audit-mode/SKILL.md implemented with Stage 1 append gate, Stage 3 fetch + render.
- [ ] .claude/output-styles/audit-summary.md renders structured table (File|Line|Severity|Finding).
- [ ] Stages 1–2 silent; Stage 3 emits audit summary from ledger.
- [ ] Manual integration test: append finding → fetch → verify output matches table structure.
- [ ] npm run validate:all exits 0; no FP warnings on new surfaces.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| (none yet) | — | — | — |

## 10. Lessons Learned

- …

## §Plan Digest

_pending — populated by `/stage-authoring {MASTER_PLAN_PATH} {STAGE_ID}`. Sub-sections: §Goal / §Acceptance / §Test Blueprint / §Examples / §Mechanical Steps (each step carries Goal / Edits / Gate / STOP / MCP hints). Template: `ia/templates/plan-digest-section.md`._

### §Goal

<!-- 1–2 sentences — task outcome in product / domain terms. -->

### §Acceptance

<!-- Refined per-Task acceptance — narrower than Stage Exit. Checkbox list. -->

### §Test Blueprint

<!-- Structured tuples consumed by `/implement` + `/verify-loop`. -->

### §Examples

<!-- Concrete inputs/outputs + edge cases. Tables or code blocks. -->

### §Mechanical Steps

<!-- Sequential, pre-decided. Each step: Goal / Edits (before+after) / Gate / STOP / MCP hints. -->

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.

---

## §Audit

<!-- Pair-head: `opus-audit` Opus stage (post-verify). Upstream of `/ship-stage` Pass B inline closeout (`stage_closeout_apply` MCP). Per-Task `§Closeout Plan` section retired — closeout digest written by Pass B in one shot. -->

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

<!-- Pair-head: `opus-code-review` Opus stage. Pair-tail: `plan-applier` Sonnet Mode code-fix (only when critical). -->

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

<!-- Pair-head: `opus-code-review` writes here only when verdict = critical. Pair-tail: `code-fix-apply` Sonnet. -->

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
