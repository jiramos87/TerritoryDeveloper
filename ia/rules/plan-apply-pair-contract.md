---
purpose: "Canonical contract every Plan-Apply pair seam reads — §Plan tuple shape, 5 pair seams, validation gate, escalation rule, idempotency requirement."
audience: agent
loaded_by: ondemand
slices_via: none
description: "Plan-Apply pair contract: Opus writes structured §Plan tuples; Sonnet pair-tail applies verbatim; ambiguous → escalate; re-runs idempotent."
alwaysApply: false
---

# Plan-Apply pair contract

Applies to all 5 Plan-Apply pair seams in lifecycle. Every pair-head Opus skill writes a `§Plan` payload conforming to this contract; every pair-tail Sonnet skill reads + applies it verbatim.

## §Plan tuple shape

A `§Plan` payload = ordered list of tuples. One tuple = one atomic edit. Keys:

| Key | Type | Rules |
|-----|------|-------|
| `operation` | enum | `replace_section` \| `insert_after` \| `insert_before` \| `append_row` \| `delete_section` \| `set_frontmatter` \| `archive_record` \| `delete_file` \| `write_file` |
| `target_path` | string | Repo-relative path. Exact, never glob. |
| `target_anchor` | string | Resolved anchor — exact heading text (`## §Audit`), exact line number (`L42`), glossary row id (`glossary:HeightMap`), or table row key (`task_key:T1.2.5`). Opus MUST resolve to a single match before writing the tuple. |
| `payload` | string \| object | Literal content for write operations; `null` for `delete_*`; structured object for `archive_record` / `set_frontmatter`. |

Tuples execute in declared order. Sonnet pair-tail does NOT reorder, merge, or interpret — applies verbatim.

## Pair seams (5)

| # | Pair-head (Opus) | §Plan section | Pair-tail (Sonnet) | Scope |
|---|------------------|---------------|--------------------|-------|
| 1 | `plan-review` | `§Plan Fix` (under Stage block in master plan) | `plan-fix-apply` | Stage planning seam — review all Tasks of a Stage + master-plan header + invariants; emit fix tuples. |
| 2 | `stage-file-plan` | `§Stage File Plan` (under Stage block in master plan) | `stage-file-apply` | Stage materialization seam — reserve ids + author backlog yaml + project-spec stubs for all Tasks of one Stage. |
| 3 | `project-new-plan` | `§Project-New Plan` (in `ia/projects/{ISSUE_ID}.md`) | `project-new-apply` | Single-issue spec authoring seam — fill spec sections §1–§10 from kickoff context. |
| 4 | `code-review` | `§Code Fix Plan` (in `ia/projects/{ISSUE_ID}.md`) | `code-fix-apply` | Post-implementation review seam — diff vs spec + invariants + glossary; emit fix tuples; re-enter `/verify-loop` after. |
| 5 | `audit` | `§Closeout Plan` (in `ia/projects/{ISSUE_ID}.md`) | `closeout-apply` | Closeout seam — migrate canonical knowledge to glossary / specs / rules / docs; archive backlog row; delete spec; persist journal. |

## Seam #2 — `§Stage File Plan` tuple shape (extended)

Seam #2 tuples carry a domain-specific shape instead of the generic 4-key shape. The `stage-file-apply` pair-tail reads these fields directly; the generic `operation` / `target_path` / `target_anchor` / `payload` keys are NOT used for seam #2.

| Key | Type | Rules |
|-----|------|-------|
| `reserved_id` | string | Always blank — pair-tail fills atomically via `reserve-id.sh`. Planner MUST leave blank. |
| `title` | string | Verbatim Task Intent from master-plan task table. |
| `priority` | string | From task Priority column. Default `medium`. |
| `notes` | string | 1–3 sentence scope note; caveman prose; references key files/subsystems. |
| `depends_on` | string[] | Verified dep ids from planner Phase 2 `backlog_list` call. Applier reads verbatim — no re-query. |
| `related` | string[] | Sibling task ids within same Stage. May be empty. |
| `stub_body` | object | Sub-fields: `summary` (§1), `goals` (§2.1), `systems_map` (§4.2), `impl_plan_sketch` (§7). Applier writes these into spec stub verbatim. |

Pair skills: [`stage-file-plan/SKILL.md`](../skills/stage-file-plan/SKILL.md) (head) · [`stage-file-apply/SKILL.md`](../skills/stage-file-apply/SKILL.md) (tail) · [`stage-compress/SKILL.md`](../skills/stage-compress/SKILL.md) (Compress mode cold path).

## Validation gate

After applying all tuples, Sonnet pair-tail MUST run the validator appropriate to the seam:

| Seam | Validator |
|------|-----------|
| 1, 2 | `npm run validate:master-plan-status` + `npm run validate:backlog-yaml` |
| 3 | `npm run validate:dead-project-specs` + `npm run validate:backlog-yaml` |
| 4 | `npm run verify:local` (or stage-appropriate Path A) |
| 5 | `npm run validate:all` |

On non-zero exit: pair-tail STOPS, returns control to Opus pair-head with `{exit_code, stderr, failing_tuple_index}`. Opus revises the `§Plan`; Sonnet re-applies from scratch (idempotency clause guarantees safety).

## Escalation rule

Sonnet pair-tail NEVER guesses an ambiguous anchor. Triggers for immediate return-to-Opus:

- `target_anchor` matches zero lines/headings/rows in `target_path`.
- `target_anchor` matches more than one location.
- `target_path` does not exist (for non-`write_file` operations).
- `payload` references a glossary term not in `ia/specs/glossary.md`.
- `operation` enum value not recognized.

Return shape: `{escalation: true, tuple_index, reason, candidate_matches?: []}`. Opus re-resolves; never the applier.

## Idempotency requirement

Every applier MUST be safe to re-run on partially-applied or fully-applied state. Implementation:

- `replace_section` / `set_frontmatter`: write desired final state; ok if already matches.
- `insert_*` / `append_row`: detect target content already present at anchor → no-op.
- `archive_record` / `delete_file`: ok if target already moved/missing.
- `write_file`: overwrite ok; verify content matches `payload` after write.

Re-running an applied `§Plan` from scratch = exit 0 + zero diff. This unblocks Opus revision loops without rollback machinery.

## Cross-references

- `ia/rules/project-hierarchy.md` — Stage/Task lifecycle the pair seams operate on.
- `ia/rules/orchestrator-vs-spec.md` — status flip matrix; pair-tails (esp. seam 2) flip status per R1/R2.
- `ia/templates/project-spec-template.md` — defines §Project-New Plan / §Audit / §Code Review / §Code Fix Plan / §Closeout Plan section anchors.
- `ia/templates/master-plan-template.md` — defines §Stage File Plan / §Plan Fix section anchors.
