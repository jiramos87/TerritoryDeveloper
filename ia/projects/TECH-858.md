---
purpose: "TECH-858 — Cross-check `related` ids exist."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/backlog-yaml-mcp-alignment-master-plan.md"
task_key: "T5.1"
---
# TECH-858 — Cross-check `related` ids exist

> **Issue:** [TECH-858](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-24
> **Last updated:** 2026-04-24

## 1. Summary

In `tools/validate-backlog-yaml.mjs`, after loading both dirs, iterate records + assert every id in `related: []` exists in the combined set (open + archive). Emit error with source record + missing id. Scoped to Stage 5 of `ia/projects/backlog-yaml-mcp-alignment-master-plan.md` — validator extensions (IP8). Tooling-only, no runtime C# touches.

## 2. Goals and Non-Goals

### 2.1 Goals

1. validator cross-record check: related ids resolve.
2. Land under `tools/validate-backlog-yaml.mjs` shared lint core where applicable; cross-record checks (need whole set) stay in script body.
3. `npm run validate:backlog-yaml` green on passing fixture, red on failing fixture (via fixture-runner harness).

### 2.2 Non-Goals (Out of Scope)

1. No schema changes to `backlog-record-schema.ts`.
2. No materialize-script touch.
3. No archive backfill (Stage 16 territory).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a developer, I want validator cross-record check: related ids resolve so that backlog yaml drift surfaces before merge. | `npm run validate:backlog-yaml` exits non-zero on failing fixture. |

## 4. Current State

### 4.1 Domain behavior

`tools/validate-backlog-yaml.mjs` currently validates per-record shape (Stage 1 + Stage 4). Cross-record checks not yet implemented.

### 4.2 Systems map

- `tools/validate-backlog-yaml.mjs` — validator script (cross-record loop lives here).
- `tools/mcp-ia-server/src/parser/backlog-record-schema.ts` — shared lint core (per-record checks only).
- `tools/scripts/test-fixtures/` — fixture root for pass/fail cases.

## 5. Proposed Design

### 5.1 Target behavior (product)

In `tools/validate-backlog-yaml.mjs`, after loading both dirs, iterate records + assert every id in `related: []` exists in the combined set (open + archive). Emit error with source record + missing id.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Agent decides — see task intent in orchestrator Stage 5 row.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-24 | Scope locked to Stage 5 exit criteria | Inherited from orchestrator Exit. | — |

## 7. Implementation Plan

### Phase 1 — Implement + fixtures

- [ ] validator cross-record check: related ids resolve.
- [ ] Fixture pair(s) under `tools/scripts/test-fixtures/`.
- [ ] Fixture-runner harness asserts pass/fail text.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Validator check lands + fixture harness green | Node | `npm run validate:backlog-yaml` + `npm run validate:all` | Exit 0 on pass fixture; non-zero on fail fixture. |

## 8. Acceptance Criteria

- [ ] validator cross-record check: related ids resolve.
- [ ] Fixture(s) land under `tools/scripts/test-fixtures/`.
- [ ] `validate:backlog-yaml` green on passing fixture, red on failing fixture.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

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

Extend `tools/validate-backlog-yaml.ts` with a cross-record check: every id listed under `related: []` in any open or archived yaml record must resolve to an existing id in the combined open+archive set. Surfaces stale `related` references before merge.

### §Acceptance

- [ ] `tools/validate-backlog-yaml.ts` iterates the combined open+archive record set after Stage 4 (id-uniqueness check) and emits a structured error per unresolved `related` id (source record id + missing id).
- [ ] Pass fixture under `tools/scripts/test-fixtures/backlog-yaml/related-resolves/` exits 0 via `npm run validate:backlog-yaml`.
- [ ] Fail fixture under `tools/scripts/test-fixtures/backlog-yaml/related-unresolved/` exits non-zero via the same command.
- [ ] No schema change to `tools/mcp-ia-server/src/parser/backlog-record-schema.ts` (cross-record concern stays in script body per §2.2 Non-Goal #1).

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| related-resolves-pass | fixture dir with two records: `TECH-001 related: [TECH-002]`, `TECH-002 related: []` | exit 0 | node |
| related-unresolved-fail | fixture dir with one record `TECH-001 related: [TECH-999]` (no TECH-999) | exit 1; stderr contains `TECH-001` + `TECH-999` | node |
| related-resolves-archive | open record references archived id; archived id present in archive dir | exit 0 (combined set covers archive) | node |

### §Examples

Failing case stderr shape:

```
[validate-backlog-yaml] Stage 5 cross-record check failed:
  TECH-001 → related id 'TECH-999' not found in open+archive set
exit 1
```

Passing case (silent success at Stage 5; `[validate-backlog-yaml] OK` at end).

### §Mechanical Steps

#### Step 1 — Add cross-record `related` resolver in validate-backlog-yaml.ts

**Goal:** After Stage 4 (id uniqueness across `loadDir(BACKLOG_DIR)` + `loadDir(ARCHIVE_DIR)`), iterate the combined record set and assert every id under `record.scalars.related` exists in the combined-id set; emit `{source_id, missing_id}` errors via the existing `errors.push()` channel.

**Edits:**
- `tools/validate-backlog-yaml.ts` — **operation**: edit; **anchor**: insert after end-of-Stage-4 id-uniqueness loop; **after** — append:
  ```typescript
  // Stage 5 — cross-record: related ids resolve in combined set
  const combinedIds = new Set<string>([
    ...openRecords.map((r) => r.scalars.id),
    ...archiveRecords.map((r) => r.scalars.id),
  ]);
  for (const r of [...openRecords, ...archiveRecords]) {
    const related = Array.isArray(r.scalars.related) ? r.scalars.related : [];
    for (const id of related) {
      if (!combinedIds.has(id)) {
        errors.push(`${r.file}: related id '${id}' not found in open+archive set`);
      }
    }
  }
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `npm run validate:backlog-yaml`

**Gate:**
```bash
npm run validate:backlog-yaml
```
Expectation: exit 0 against current open+archive set (no stale `related` ids today).

**STOP:** if Stage 5 block does not appear in `git diff tools/validate-backlog-yaml.ts` after edit → re-open Step 1; do NOT advance to Step 2.

**MCP hints:** `plan_digest_resolve_anchor` (confirm `// Stage 4` anchor unique), `plan_digest_render_literal` (quote final block verbatim).

#### Step 2 — Fixture pair under tools/scripts/test-fixtures/backlog-yaml/

**Goal:** Land matching pass + fail fixtures under `tools/scripts/test-fixtures/backlog-yaml/related-{resolves,unresolved}/` so the existing fixture-runner harness exercises the new Stage 5 check.

**Edits:**
- `tools/scripts/test-fixtures/backlog-yaml/related-resolves/TECH-001.yaml` — **operation**: create; **after**:
  ```yaml
  id: TECH-001
  type: tech-debt
  title: smoke pass parent
  status: open
  priority: medium
  section: Tech Debt
  related: [TECH-002]
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `npm run validate:backlog-yaml -- --fixture-pass tools/scripts/test-fixtures/backlog-yaml/related-resolves`
- `tools/scripts/test-fixtures/backlog-yaml/related-resolves/TECH-002.yaml` — **operation**: create; **after**:
  ```yaml
  id: TECH-002
  type: tech-debt
  title: smoke pass child
  status: open
  priority: medium
  section: Tech Debt
  related: []
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `npm run validate:backlog-yaml -- --fixture-pass tools/scripts/test-fixtures/backlog-yaml/related-resolves`
- `tools/scripts/test-fixtures/backlog-yaml/related-unresolved/TECH-001.yaml` — **operation**: create; **after**:
  ```yaml
  id: TECH-001
  type: tech-debt
  title: smoke fail parent
  status: open
  priority: medium
  section: Tech Debt
  related: [TECH-999]
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `npm run validate:backlog-yaml -- --fixture-fail tools/scripts/test-fixtures/backlog-yaml/related-unresolved`

**Gate:**
```bash
npm run validate:backlog-yaml -- --fixture-pass tools/scripts/test-fixtures/backlog-yaml/related-resolves
npm run validate:backlog-yaml -- --fixture-fail tools/scripts/test-fixtures/backlog-yaml/related-unresolved
```
Expectation: pass fixture exits 0; fail fixture exits non-zero with stderr containing `TECH-001` + `TECH-999`.

**STOP:** if either fixture invocation contradicts expectation (pass exits non-zero or fail exits zero) → re-open Step 1 to inspect Stage 5 logic; do NOT mark Acceptance criterion 2/3 as complete.

**MCP hints:** `plan_digest_verify_paths` (confirm fixture parent dir `tools/scripts/test-fixtures/` exists).

#### Step 3 — Run full validate:all + commit gate

**Goal:** Confirm Stage 5 addition does not regress other validators (`validate:all` is the umbrella for IA / fixtures / counter / specs).

**Edits:**
- (no source edits — verification-only step)
- `invariant_touchpoints`:
  - id: backlog-yaml-id-uniqueness
    gate: npm run validate:backlog-yaml
    expected: pass
- `validator_gate`: `npm run validate:all`

**Gate:**
```bash
npm run validate:all
```
Expectation: exit 0.

**STOP:** any non-zero exit from `validate:all` → re-open the offending Step (1 or 2); do NOT commit until exit 0.

**MCP hints:** none.

### §Escalation enum

| code | trigger | resolution |
|------|---------|------------|
| anchor_not_found | `// Stage 4` anchor missing in `tools/validate-backlog-yaml.ts` | re-locate end-of-uniqueness loop boundary; update Step 1 anchor reference |
| fixture_path_collision | TECH-001 fixture filename clashes with a real backlog id | scoped under `related-resolves/` + `related-unresolved/` subdirs (already isolated) |
| validate_all_red | unrelated validator regression after Step 1 edit | bisect via `git diff tools/validate-backlog-yaml.ts`; isolate to Stage 5 block |
| db_unavailable | `task_spec_section_write` fails to persist Plan Digest body | halt + escalate to dispatcher; no filesystem-only fallback (skill Hard boundary) |

## Open Questions (resolve before / during implementation)

1. None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
