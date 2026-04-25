---
description: Bulk-file all pending tasks of one orchestrator Stage as DB rows + project spec stubs + §Plan Author populated → §Plan Digest mechanized (§Plan Author dropped) + plan-review PASS. Dispatches `stage-file` (merged single-skill, DB-backed) → `plan-author` (bulk Stage 1×N) → `plan-digest` (bulk Stage 1×N, always-on) → `plan-reviewer` (→ `plan-applier` Mode plan-fix on critical, re-entry cap=1) → STOP. Step 6 merge (2026-04-24) retired `stage-file-planner` + `stage-file-applier` pair into single `stage-file` subagent. Handoff: `/ship-stage` (N≥2) OR `/ship` (N=1).
argument-hint: "{master-plan-path} Stage {X.Y} [--force-model {model}]"
---

# /stage-file — dispatch seam #2 chain (stage-file → author → digest → review → STOP)

Use `stage-file` (DB-backed single-skill; replaces retired `-planner` + `-applier` pair) → `plan-author` → `plan-digest` → `plan-reviewer` (→ `plan-applier` Mode plan-fix on critical) to bulk-file + author + digest + review all `_pending_` tasks of `$ARGUMENTS` in ONE command. Chain STOPS at plan-review PASS (or cap=1 critical-twice). **Next:** `/ship-stage` (N≥2 — runs implement + verify + code-review + audit + closeout) OR `/ship` (N=1).

## Argument parsing

Split `$ARGUMENTS` on whitespace. First token = `{MASTER_PLAN_PATH}` (repo-relative, `ia/projects/*-master-plan.md`). Second token = `{STAGE_ID}` (e.g. `Stage 7.2` → `7.2`). Missing either → print usage + abort. If `--force-model {model}` present: extract `{model}` (valid: `sonnet`, `opus`, `haiku`); store as `FORCE_MODEL`. Absent or invalid → `FORCE_MODEL` unset (each subagent uses its own frontmatter model).

## Step 1 — Dispatch `stage-file` (merged DB-backed single-skill)

Forward via Agent tool with `subagent_type: "stage-file"` (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`):

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, BACKLOG row text + spec stub prose. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/stage-file/SKILL.md` end-to-end on Stage `{STAGE_ID}` of `{MASTER_PLAN_PATH}`. 8 phases: Mode detection → Load Stage MCP bundle once (`lifecycle_stage_context`) → Stage block + cardinality + sizing gates → Batch Depends-on verify via single `backlog_list` → Resolve target BACKLOG.md manifest section (slug heuristic / user prompt) → Per-task iterator via `task_insert` MCP (DB-backed per-prefix monotonic id; NO yaml, NO `reserve-id.sh`) + manifest append (`ia/state/backlog-sections.json`) + spec stub (`ia/projects/{ISSUE_ID}.md` from template) → Post-loop `materialize-backlog.sh` (DB source default) + `validate:dead-project-specs` + atomic task-table flip + R2 Stage Status flip + R1 plan-top Status flip → Return.
>
> ## Hard boundaries
>
> - Do NOT write yaml under `ia/backlog/` — DB is source of truth.
> - Do NOT call `reserve-id.sh` — per-prefix DB sequences own id assignment.
> - Do NOT run `validate:backlog-yaml` — no yaml written on DB path.
> - Do NOT run `validate:all` — gate is `validate:dead-project-specs` only.
> - Do NOT file tasks outside target Stage.
> - Do NOT pre-file for Steps whose Status is not `In Progress`.
> - Do NOT guess ambiguous manifest section — prompt user.
> - Do NOT emit next-step handoff — chain continues to Step 2 (plan-author).
> - Do NOT commit — user decides.

`stage-file` must return success + N rows filed + spec stubs written + task-table flipped before Step 2. Escalation → abort chain.

### Step 1b — Branch guardrail (feature/ia-dev-db-refactor)

Per `docs/ia-dev-db-refactor-implementation.md §3`: "No §Plan Digest ceremony. Do not invoke /author, /plan-digest, /plan-review on this branch." On this branch the chain STOPS after Step 1 — Steps 2–4 skipped. Dispatcher emits Step 5 boundary stop directly.

## Step 2 — Dispatch `plan-author` (bulk Stage 1×N)

Forward via Agent tool with `subagent_type: "plan-author"` (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`):

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/plan-author/SKILL.md` end-to-end on Stage `{STAGE_ID}` of `{MASTER_PLAN_PATH}`. Bulk-author `§Plan Author` section (audit notes + examples + test blueprint + acceptance + canonical-term fold) across ALL N filed Task specs of target Stage in one Opus pass. Read Stage block + N spec stubs + MCP bundle (glossary / router / invariants / spec_sections) once. Write per-spec `§Plan Author` in place.
>
> ## Hard boundaries
>
> - Do NOT write code, run verify, or flip Task status.
> - Do NOT author specs outside target Stage.
> - Do NOT commit.
> - Idempotent on re-entry: skip specs whose `§Plan Author` is already populated.

Plan-author must return success + N specs with populated `§Plan Author` before Step 3. Failure → abort chain with handoff `/author --stage {MASTER_PLAN_PATH} {STAGE_ID}`.

## Step 3 — Dispatch `plan-digest` (Opus Stage-scoped bulk non-pair)

Forward via Agent tool with `subagent_type: "plan-digest"` (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`):

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/plan-digest/SKILL.md` end-to-end on Stage `{STAGE_ID}` of `{MASTER_PLAN_PATH}`. For every Task spec whose `§Plan Author` is populated, mechanize into `§Plan Digest` (rich format: Goal / Acceptance / Test Blueprint / Examples / sequential Mechanical Steps with Edits + Gate + STOP + MCP hints) and DROP `§Plan Author` in the same write pass. Compile aggregate doc at `docs/implementation/{slug}-stage-{STAGE_ID}-plan.md` via `plan_digest_compile_stage_doc`. Self-lint via `plan_digest_lint` (cap=1 retry).
>
> ## Hard boundaries
>
> - Do NOT write code, run verify, or flip Task status.
> - Do NOT resolve picks — `plan_digest_scan_for_picks` is lint-only. Leak → abort + `/author` handoff.
> - Every `before_string` in a digest edit tuple must resolve to exactly 1 hit via `plan_digest_resolve_anchor`.
> - Mode `audit` is flag-gated (`PLAN_DIGEST_AUDIT_MODE=1`); do NOT dispatch it from this chain.
> - Idempotent on re-entry: if `§Plan Digest` already populated AND lint passes, skip.

Plan-digest must return success + N specs with populated `§Plan Digest` (and `§Plan Author` dropped) + aggregate doc written + lint PASS before Step 4. Failure → abort chain with handoff `/plan-digest {MASTER_PLAN_PATH} {STAGE_ID}`.

## Step 4 — Dispatch `plan-reviewer` (Sonnet pair-head; cap=1 on critical)

Forward via Agent tool with `subagent_type: "plan-reviewer"` (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`):

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/plan-review/SKILL.md` end-to-end on Stage `{STAGE_ID}` of `{MASTER_PLAN_PATH}`. Bulk drift scan across N Task specs + invariants + glossary. Write ONE of: PASS sentinel OR `§Plan Fix` tuple list under Stage block per `ia/rules/plan-apply-pair-contract.md`.
>
> ## Hard boundaries
>
> - Do NOT mutate Task specs directly — critical → emit tuples + hand off to `plan-applier` Mode plan-fix.
> - Do NOT edit master-plan task table.
> - Do NOT run validators.
> - Do NOT commit.

Branching:

- **PASS** → continue to Step 5 (STOP).
- **critical** (tuples written) → dispatch `plan-applier` Mode plan-fix (Sonnet pair-tail) to apply tuples verbatim; re-dispatch `plan-reviewer`. Re-entry cap = 1. Second critical → abort chain with `STOPPED at plan-review — STAGE_PLAN_REVIEW_CRITICAL_TWICE` + handoff `/plan-review {MASTER_PLAN_PATH} {STAGE_ID}` for human review.

### Step 4a — Dispatch `plan-applier` Mode plan-fix (Sonnet pair-tail; only on critical) (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`)

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/plan-applier/SKILL.md` — Mode plan-fix on Stage `{STAGE_ID}` of `{MASTER_PLAN_PATH}`. Read `§Plan Fix` tuples verbatim. Apply in declared order. Run `npm run validate:master-plan-status` + `npm run validate:backlog-yaml` gate after all edits. Idempotent.
>
> ## Hard boundaries
>
> - Do NOT re-query MCP for anchor resolution.
> - Do NOT re-order tuples or interpret payloads.
> - Do NOT write normative prose.
> - Do NOT commit.

After applier success → re-dispatch `plan-reviewer` (Step 4).

## Step 5 — Boundary stop (NO auto-chain to ship-stage)

Per F6 re-fold (2026-04-20): `/stage-file` STOPS at plan-review PASS. Do NOT auto-invoke `/ship-stage`. User decides when to ship.

Rationale: collapse stage-entry from 3 commands (`/stage-file` + `/author` + `/plan-review`) to 1 (`/stage-file`); preserve explicit user gate between authoring and shipping; user can inspect populated specs before shipping.

## Output

Chain completion summary: tasks filed ids + `§Plan Digest` populated per spec (after plan-digest) + plan-review PASS + validators ok + next-step proposal. Emit exactly:

- **N≥2:** `Next: claude-personal "/ship-stage {MASTER_PLAN_PATH} Stage {STAGE_ID}"` — runs implement + verify + code-review + audit + closeout.
- **N=1:** `Next: claude-personal "/ship {ISSUE_ID}"` — single-task path (no ship-stage).

Hard rule: `/ship-stage` is multi-task only — for N=1 use `/ship`. `/ship-stage` Phase 1.5 readiness gate is idempotent on populated `§Plan Digest`, so re-invocation on partial-failure recovery is safe.

**Branch guardrail:** on `feature/ia-dev-db-refactor` the chain stops after Step 1 (Steps 2–4 skipped per `docs/ia-dev-db-refactor-implementation.md §3`). Post-Step-6 branches run full chain.
