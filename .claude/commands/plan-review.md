---
description: Drift-scan a Stage's §Plan Digest sections + master-plan Stage block. Dispatches `plan-reviewer` (Opus pair-head seam #1) → `plan-applier` Mode plan-fix if drift found. PASS verdict → no applier dispatched. Fires once per Stage after `/plan-digest` and before per-Task `/implement` loop.
argument-hint: "{master-plan-path} Stage {X.Y} [--force-model {model}]"
---

# /plan-review — dispatch seam #1 pair (plan-review → plan-applier Mode plan-fix)

Use `plan-reviewer` subagent (`.claude/agents/plan-reviewer.md`) to scan Stage `{STAGE_ID}` of `{MASTER_PLAN_PATH}` for drift between `§Plan Digest` sections (from `plan-digest` after `/author`), master-plan Stage block, Task spec §1 / §2 / §7, invariants, and glossary. On drift → writes `§Plan Fix` tuple list + auto-dispatches **`plan-applier`** (Sonnet pair-tail, Mode plan-fix) to apply tuples + run `validate:master-plan-status` + `validate:backlog-yaml` gate.

## Argument parsing

Split `$ARGUMENTS` on whitespace. First token = `{MASTER_PLAN_PATH}`. Second token = `{STAGE_ID}` (e.g. `7.2`). Missing either → print usage + abort. If `--force-model {model}` present: extract `{model}` (valid: `sonnet`, `opus`, `haiku`); store as `FORCE_MODEL`. Absent or invalid → `FORCE_MODEL` unset.

## Step 1 — Dispatch `plan-reviewer` (Opus pair-head)

Forward via Agent tool with `subagent_type: "plan-reviewer"` (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`):

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/plan-review/SKILL.md` end-to-end on Stage `{STAGE_ID}` of `{MASTER_PLAN_PATH}`. Phase 1 Load Stage context: Stage block + all N Task §Plan Digest / §Implementation Plan / §Acceptance Criteria / §Open Questions + shared MCP bundle (glossary / router / invariants) + master-plan Stage Objective / Exit criteria. Phase 2 Drift scan (12-check matrix: canonical-term drift, acceptance/exit mismatch, invariant-touch gaps, glossary-intro missing, dep-cycle, etc.). Zero drift → PASS sentinel under Stage block. Drift → write `§Plan Fix` tuple list (contract 4-key shape — `operation`, `target_path`, `target_anchor`, `payload`). Phase 3 Hand-off: escalate to pair-tail when tuples present.
>
> ## Hard boundaries
>
> - Do NOT mutate spec / master-plan / glossary / rules — writes only under `§Plan Fix` Stage block.
> - Do NOT run validators — pair-tail runs gate.
> - Do NOT guess ambiguous anchors — escalate per `ia/rules/plan-apply-pair-contract.md`.
> - Do NOT emit `§Plan Fix` on PASS verdict.
> - Do NOT commit — user decides.

Planner returns `{verdict: "PASS"|"fix"}`. PASS → skip Step 2 + emit summary. Fix → proceed to Step 2.

## Step 2 — Dispatch `plan-applier` (Sonnet pair-tail, Mode plan-fix) — conditional

On fix verdict: forward via Agent tool with `subagent_type: "plan-applier"` (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`):

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/plan-applier/SKILL.md` — **Mode: plan-fix**. Read `§Plan Fix` tuples verbatim from Stage block. Resolve every `target_anchor` to single match before applying. Apply tuples in declared order (one atomic edit per tuple). Run `npm run validate:master-plan-status` + `npm run validate:backlog-yaml` gate (seam #1 scope). 1-retry bound on validate fail. Second fail → escalate to Opus pair-head. Idempotent.
>
> ## Hard boundaries
>
> - Do NOT re-review drift — read tuples verbatim.
> - Do NOT run `validate:all` — seam #1 gate is `validate:master-plan-status` + `validate:backlog-yaml` only.
> - Do NOT reorder tuples — declared order only.
> - Do NOT interpret ambiguous anchors — escalate.
> - Do NOT commit — user decides.

## Output

Chain summary: verdict + drift count (if any) + tuples applied + validator exit. Next step: per-Task `/ship {ISSUE_ID}` loop across Stage tasks → Stage-end `/audit` + `/closeout`.
