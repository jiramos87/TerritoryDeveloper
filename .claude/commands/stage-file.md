---
description: DB-backed single-skill filing. Loads shared Stage MCP bundle once; gates cardinality (≥2 Tasks per Stage) + sizing (H1–H6); batch-verifies Depends-on ids via single `backlog_list`; resolves target BACKLOG.md section from master-plan H1 title; per-Task writes via `task_insert` MCP tool (DB-backed monotonic id from per-prefix sequence — no reserve-id.sh); appends manifest entry to `ia/state/backlog-sections.json`; bootstraps `ia/projects/{ISSUE_ID}.md` spec stub from template; runs `materialize-backlog.sh` (DB source default) + `validate:dead-project-specs`; atomic task-table flip + R1/R2 Status flips. No yaml file written under `ia/backlog/`. Triggers: "/stage-file {orchestrator-path} Stage 1.2", "file stage tasks", "bulk create stage issues", "create backlog rows for Stage X.Y", "bootstrap issues for pending stage tasks", "compress stage tasks", "merge draft tasks". Argument order (explicit): ORCHESTRATOR_SPEC first, STAGE_ID second.
argument-hint: "{master-plan-path} Stage {X.Y} [--force-model {model}]"
---

# /stage-file — DB-backed single-skill stage-file: mode detection + cardinality + sizing gates + per-task task_insert MCP writes + manifest append + spec stub + task-table flip + R1/R2 Status flips.

Drive `$ARGUMENTS` via the [`stage-file`](../agents/stage-file.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, BACKLOG row text + spec stub prose (Notes + acceptance caveman; row structure verbatim per agent-output-caveman-authoring). Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- /stage-file {orchestrator-path} Stage 1.2
- file stage tasks
- bulk create stage issues
- create backlog rows for Stage X.Y
- bootstrap issues for pending stage tasks
- compress stage tasks
- merge draft tasks
<!-- skill-tools:body-override -->

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
> - Do NOT emit next-step handoff — chain continues to Step 2 (stage-authoring).
> - Do NOT commit — user decides.

`stage-file` must return success + N rows filed + spec stubs written + task-table flipped before Step 2. Escalation → abort chain.

## Step 2 — Dispatch `stage-authoring` (Opus Stage-scoped bulk; replaces retired plan-author + plan-digest chain)

Forward via Agent tool with `subagent_type: "stage-authoring"` (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`):

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/stage-authoring/SKILL.md` end-to-end on Stage `{STAGE_ID}` of `{MASTER_PLAN_PATH}`. Bulk-author `§Plan Digest` direct (no §Plan Author intermediate) across ALL N filed Task specs of target Stage in one Opus pass. Per Task: Goal / Acceptance / Test Blueprint / Examples / sequential Mechanical Steps with Edits + Gate + STOP + MCP hints + invariant_touchpoints + validator_gate + optional Scene Wiring step. Persist body via `task_spec_section_write` MCP (DB source of truth) + transitional filesystem mirror to `ia/projects/{ISSUE_ID}.md`. Self-lint via `plan_digest_lint` (cap=1 retry per Task). Mechanicalization preflight via `mechanicalization_preflight_lint`.
>
> ## Hard boundaries
>
> - Do NOT write code, run verify, or flip Task status.
> - Do NOT author specs outside target Stage.
> - Do NOT commit.
> - Do NOT fall back to filesystem-only on `db_unavailable`.
> - Idempotent on re-entry: skip Tasks whose `§Plan Digest` is already populated AND lint passes.

`stage-authoring` must return success + N specs with populated `§Plan Digest` + lint PASS before Step 3. Failure → abort chain with handoff `/stage-authoring {MASTER_PLAN_PATH} {STAGE_ID}`.

## Step 3 — Dispatch `plan-reviewer-mechanical` (Sonnet pair-head A)

Forward via Agent tool with `subagent_type: "plan-reviewer-mechanical"` (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`):

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run mechanical drift scan (checks 3–8) across N Task specs of Stage `{STAGE_ID}` of `{MASTER_PLAN_PATH}`. Compose `§Plan Fix` tuples from MCP query output (`lifecycle_stage_context`, `spec_section`, `glossary_*`, `invariant_preflight`, `master_plan_locate`, `mechanicalization_preflight_lint`).
>
> ## Hard boundaries
>
> - Do NOT mutate Task specs directly.
> - Do NOT run semantic checks 1–2 — those belong to pair-head B (`plan-reviewer-semantic`).
> - Do NOT commit.

Mechanical pass must return mechanical-tuple-list (possibly empty) before Step 3b.

## Step 3b — Dispatch `plan-reviewer-semantic` (Opus pair-head B)

Forward via Agent tool with `subagent_type: "plan-reviewer-semantic"` (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`):

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run semantic drift scan (checks 1 goal–intent, 2 impl-plan completeness) over Stage `{STAGE_ID}` of `{MASTER_PLAN_PATH}`. Read mechanical pass output as input bundle. Emit `§Plan Fix — SEMANTIC` tuple appendix under Stage block per `ia/rules/plan-apply-pair-contract.md`.
>
> ## Hard boundaries
>
> - Do NOT mutate Task specs directly — critical → emit tuples + hand off to `plan-applier` Mode plan-fix.
> - Do NOT edit master-plan task table.
> - Do NOT run validators.
> - Do NOT commit.

Branching:

- **PASS** (no critical tuples from either pair-head) → continue to Step 4 (STOP).
- **critical** (tuples written) → dispatch `plan-applier` Mode plan-fix (Sonnet pair-tail) to apply tuples verbatim; re-dispatch `plan-reviewer-mechanical` + `plan-reviewer-semantic`. Re-entry cap = 1. Second critical → abort chain with `STOPPED at plan-review — STAGE_PLAN_REVIEW_CRITICAL_TWICE` + handoff `/plan-review {MASTER_PLAN_PATH} {STAGE_ID}` for human review.

### Step 3c — Dispatch `plan-applier` Mode plan-fix (Sonnet pair-tail; only on critical) (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`)

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/plan-applier/SKILL.md` — Mode plan-fix on Stage `{STAGE_ID}` of `{MASTER_PLAN_PATH}`. Read `§Plan Fix` tuples verbatim (mechanical + semantic appendix). Apply in declared order. Run `npm run validate:master-plan-status` gate after all edits. Idempotent.
>
> ## Hard boundaries
>
> - Do NOT re-query MCP for anchor resolution.
> - Do NOT re-order tuples or interpret payloads.
> - Do NOT write normative prose.
> - Do NOT commit.

After applier success → re-dispatch Step 3 + Step 3b.

## Step 4 — Boundary stop (NO auto-chain to ship-stage)

`/stage-file` STOPS at plan-review PASS. Do NOT auto-invoke `/ship-stage`. User decides when to ship.

Rationale: preserve explicit user gate between authoring and shipping; user can inspect populated specs before shipping.

## Output

Chain completion summary: tasks filed ids + `§Plan Digest` populated per spec (after stage-authoring) + plan-review PASS + validators ok + next-step proposal. Emit exactly:

- **N≥2:** `Next: claude-personal "/ship-stage {MASTER_PLAN_PATH} Stage {STAGE_ID}"` — runs implement + verify + code-review + inline closeout.
- **N=1:** `Next: claude-personal "/ship {ISSUE_ID}"` — single-task path (no ship-stage).

Hard rule: `/ship-stage` is multi-task only — for N=1 use `/ship`. `/ship-stage` readiness gate is idempotent on populated `§Plan Digest`, so re-invocation on partial-failure recovery is safe.
