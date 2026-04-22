---
description: Bulk-file all pending tasks of one orchestrator Stage as BACKLOG issues + project spec stubs + §Plan Author populated → §Plan Digest mechanized (§Plan Author dropped) + plan-review PASS. Dispatches `stage-file-planner` (Opus pair-head) → `stage-file-applier` (Sonnet pair-tail) → `plan-author` (bulk Stage 1×N) → `plan-digest` (bulk Stage 1×N, always-on) → `plan-reviewer` (→ `plan-applier` Mode plan-fix on critical, re-entry cap=1) → STOP. Chain tail per F6 re-fold (2026-04-20) + plan-digest insertion (2026-04-22). Handoff: `/ship-stage` (N≥2) OR `/ship` (N=1).
argument-hint: "{master-plan-path} Stage {X.Y} [--force-model {model}]"
---

# /stage-file — dispatch seam #2 chain (planner → applier → author → digest → review → STOP)

Use `stage-file-planner` → `stage-file-applier` → `plan-author` → `plan-digest` → `plan-reviewer` (→ `plan-applier` Mode plan-fix on critical) to bulk-file + author + digest + review all `_pending_` tasks of `$ARGUMENTS` in ONE command. Chain STOPS at plan-review PASS (or cap=1 critical-twice). **Next:** `/ship-stage` (N≥2 — runs implement + verify + code-review + audit + closeout) OR `/ship` (N=1).

## Argument parsing

Split `$ARGUMENTS` on whitespace. First token = `{MASTER_PLAN_PATH}` (repo-relative, `ia/projects/*-master-plan.md`). Second token = `{STAGE_ID}` (e.g. `Stage 7.2` → `7.2`). Missing either → print usage + abort. If `--force-model {model}` present: extract `{model}` (valid: `sonnet`, `opus`, `haiku`); store as `FORCE_MODEL`. Absent or invalid → `FORCE_MODEL` unset (each subagent uses its own frontmatter model).

## Step 1 — Dispatch `stage-file-planner` (Opus pair-head)

Forward via Agent tool with `subagent_type: "stage-file-planner"` (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`):

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, BACKLOG row text + spec stub prose. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/stage-file-plan/SKILL.md` end-to-end on Stage `{STAGE_ID}` of `{MASTER_PLAN_PATH}`. Load shared Stage MCP bundle once (`domain-context-load`). Read Stage block + cardinality gate (≥2 tasks per phase — single-task phase → warn + pause). Batch-verify every Depends-on / Related id via `backlog_issue`. Batch-reserve ids via `reserve_backlog_ids` (monotonic per prefix). Emit `§Stage File Plan` tuple list under Stage block (one tuple per task: `{operation: file_task, reserved_id, title, priority, issue_type, notes, depends_on, related, stub_body}`). Resolve every anchor to single match before emitting. Hand off to Sonnet pair-tail.
>
> ## Hard boundaries
>
> - Do NOT reserve ids per-task — batch via `reserve_backlog_ids` only.
> - Do NOT write yaml / spec stubs / edit master plan — that is pair-tail.
> - Do NOT run validators — applier runs gate.
> - Do NOT file tasks outside target Stage.
> - Do NOT pre-file for Steps whose Status is not `In Progress`.
> - Do NOT guess ambiguous anchors — escalate per `ia/rules/plan-apply-pair-contract.md`.
> - Do NOT commit — user decides.

Planner must return success + `§Stage File Plan` written before Step 2. Escalation → abort chain.

## Step 2 — Dispatch `stage-file-applier` (Sonnet pair-tail)

Forward via Agent tool with `subagent_type: "stage-file-applier"` (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`):

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, BACKLOG row text + spec stub prose. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/stage-file-apply/SKILL.md` end-to-end on `{MASTER_PLAN_PATH}` `{STAGE_ID}`. Read `§Stage File Plan` tuples verbatim. Loop tuples in declared order: compose yaml, `backlog_record_validate`, write `ia/backlog/{reserved_id}.yaml`, bootstrap `ia/projects/{reserved_id}.md` from template. Post-loop: `bash tools/scripts/materialize-backlog.sh` + `npm run validate:dead-project-specs` + `npm run validate:backlog-yaml` once. Atomic Edit pass on orchestrator task table flips `_pending_` → `{reserved_id}` + `Draft`. Idempotent.
>
> ## Hard boundaries
>
> - Do NOT re-query MCP for Depends-on — planner batch-verified.
> - Do NOT re-reserve ids — planner reserved via `reserve_backlog_ids`.
> - Do NOT re-order tuples — declared order only.
> - Do NOT write normative spec prose beyond stub — `plan-author` writes spec body in Step 3.
> - Do NOT edit `BACKLOG.md` directly — `materialize-backlog.sh` regenerates it.
> - Do NOT run `validate:all` — seam #2 gate is `validate:dead-project-specs` + `validate:backlog-yaml` only.
> - Do NOT update task table mid-loop — atomic pass after all writes.
> - Do NOT commit — user decides.
> - Do NOT emit next-step handoff — chain continues to Step 3 (plan-author).

Applier must return success + N spec stubs + task table flipped before Step 3. Validator failure → abort chain.

## Step 3 — Dispatch `plan-author` (bulk Stage 1×N)

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

Plan-author must return success + N specs with populated `§Plan Author` before Step 4. Failure → abort chain with handoff `/author --stage {MASTER_PLAN_PATH} {STAGE_ID}`.

## Step 4 — Dispatch `plan-digest` (Opus Stage-scoped bulk non-pair)

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

Plan-digest must return success + N specs with populated `§Plan Digest` (and `§Plan Author` dropped) + aggregate doc written + lint PASS before Step 5. Failure → abort chain with handoff `/plan-digest {MASTER_PLAN_PATH} {STAGE_ID}`.

## Step 5 — Dispatch `plan-reviewer` (Sonnet pair-head; cap=1 on critical)

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

- **PASS** → continue to Step 6 (STOP).
- **critical** (tuples written) → dispatch `plan-applier` Mode plan-fix (Sonnet pair-tail) to apply tuples verbatim; re-dispatch `plan-reviewer`. Re-entry cap = 1. Second critical → abort chain with `STOPPED at plan-review — STAGE_PLAN_REVIEW_CRITICAL_TWICE` + handoff `/plan-review {MASTER_PLAN_PATH} {STAGE_ID}` for human review.

### Step 5a — Dispatch `plan-applier` Mode plan-fix (Sonnet pair-tail; only on critical) (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`)

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

After applier success → re-dispatch `plan-reviewer` (Step 5).

## Step 6 — Boundary stop (NO auto-chain to ship-stage)

Per F6 re-fold (2026-04-20): `/stage-file` STOPS at plan-review PASS. Do NOT auto-invoke `/ship-stage`. User decides when to ship.

Rationale: collapse stage-entry from 3 commands (`/stage-file` + `/author` + `/plan-review`) to 1 (`/stage-file`); preserve explicit user gate between authoring and shipping; user can inspect populated specs before shipping.

## Output

Chain completion summary: tasks filed ids + `§Plan Digest` populated per spec (after plan-digest) + plan-review PASS + validators ok + next-step proposal. Emit exactly:

- **N≥2:** `Next: claude-personal "/ship-stage {MASTER_PLAN_PATH} Stage {STAGE_ID}"` — runs implement + verify + code-review + audit + closeout.
- **N=1:** `Next: claude-personal "/ship {ISSUE_ID}"` — single-task path (no ship-stage).

Hard rule: `/ship-stage` is multi-task only — for N=1 use `/ship`. `/ship-stage` Phase 1.5 readiness gate is idempotent on populated `§Plan Digest`, so re-invocation on partial-failure recovery is safe.
