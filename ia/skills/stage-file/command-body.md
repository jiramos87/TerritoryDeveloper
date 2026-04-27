## Argument parsing

Split `$ARGUMENTS` on whitespace. First token = `{SLUG}` (bare master-plan slug, e.g. `blip`). Second token = `{STAGE_ID}` (e.g. `Stage 7.2` → `7.2`). Missing either → print usage + abort. If `--force-model {model}` present: extract `{model}` (valid: `sonnet`, `opus`, `haiku`); store as `FORCE_MODEL`. Absent or invalid → `FORCE_MODEL` unset (each subagent uses its own frontmatter model).

Verify slug exists via `master_plan_state(slug=SLUG)`. Missing → STOPPED + `Next: claude-personal "/master-plan-new ..."` handoff. Capture `master_plan_title` from MCP result.

## Step 1 — Dispatch `stage-file` (merged DB-backed single-skill)

Forward via Agent tool with `subagent_type: "stage-file"` (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`):

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, BACKLOG row text + spec stub prose. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/stage-file/SKILL.md` end-to-end on Stage `{STAGE_ID}` of slug `{SLUG}`. 8 phases: Mode detection → Load Stage MCP bundle once (`lifecycle_stage_context`) → Stage block + cardinality + sizing gates → Batch Depends-on verify via single `backlog_list` → Resolve target BACKLOG.md manifest section (slug heuristic / user prompt) → Per-task iterator via `task_insert` MCP (DB-backed per-prefix monotonic id; NO yaml, NO `reserve-id.sh`) + manifest append (`ia/state/backlog-sections.json`) + spec stub body persisted via `task_spec_section_write` MCP → Post-loop: short-circuit on `filed_tasks.length === 0` (skip materialize + validate); else `materialize-backlog.sh` (DB source default) + `validate:dead-project-specs` + atomic task-table flip + R2 Stage Status flip + R1 plan-top Status flip → Return.
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
> Run `ia/skills/stage-authoring/SKILL.md` end-to-end on Stage `{STAGE_ID}` of slug `{SLUG}`. Bulk-author `§Plan Digest` direct (RELAXED shape) across ALL N filed Task specs of target Stage in one Opus pass. Authoring prompt embeds the 10-point rubric verbatim as hard constraints (9 contract rules + per-section soft byte caps) — NO post-author `plan_digest_lint` MCP call, NO retry loop. Per Task: §Goal / §Acceptance / §Pending Decisions / §Implementer Latitude / §Work Items (flat rows, 1-line intent, NO verbatim before/after code) / §Test Blueprint / §Invariants & Gate (ONE block: invariant_touchpoints + validator_gate + escalation_enum + Gate + STOP). Optional Scene Wiring row appears in §Work Items when triggered. Persist body via `task_spec_section_write` MCP (DB sole source of truth — no filesystem mirror). Per-section overruns counted as `n_section_overrun` (warn-only).
>
> ## Hard boundaries
>
> - Do NOT write code, run verify, or flip Task status.
> - Do NOT author specs outside target Stage.
> - Do NOT commit.
> - Do NOT write task spec bodies to filesystem — DB only via `task_spec_section_write`.
> - Do NOT call `plan_digest_lint` MCP — rubric is enforced in-prompt only.
> - Idempotent on re-entry: skip Tasks whose `§Plan Digest` is already populated.

`stage-authoring` must return success + N specs with populated `§Plan Digest` + `validate:master-plan-status` exit 0 before chain stops. Failure → abort chain with handoff `/stage-authoring {SLUG} {STAGE_ID}`.

## Step 3 — Boundary stop (NO auto-chain to ship-stage)

`/stage-file` STOPS at `stage-authoring` success. Do NOT auto-invoke `/ship-stage`. User decides when to ship.

Rationale: preserve explicit user gate between authoring and shipping; user can inspect populated specs before shipping.

## Output

Chain completion summary: tasks filed ids + `§Plan Digest` populated per spec (after stage-authoring) + validators ok + next-step proposal. Emit exactly:

- **N≥2:** `Next: claude-personal "/ship-stage {SLUG} Stage {STAGE_ID}"` — runs implement + verify + code-review + inline closeout.
- **N=1:** `Next: claude-personal "/ship {ISSUE_ID}"` — single-task path (no ship-stage).

Hard rule: `/ship-stage` is multi-task only — for N=1 use `/ship`. `/ship-stage` readiness gate is idempotent on populated `§Plan Digest` (DB-resident), so re-invocation on partial-failure recovery is safe.
