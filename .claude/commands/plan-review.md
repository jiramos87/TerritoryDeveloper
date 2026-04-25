---
description: Drift-scan a Stage's §Plan Digest sections + master-plan Stage block. Dispatches `plan-reviewer-mechanical` → `plan-reviewer-semantic` → `plan-applier` Mode plan-fix if drift found. PASS verdict → no applier dispatched. Fires once per Stage after `/stage-authoring` and before `/ship-stage`.
argument-hint: "{master-plan-path} Stage {X.Y} [--force-model {model}]"
---

# /plan-review — dispatch plan-review pair (mechanical → semantic) + plan-applier on drift

Use `plan-reviewer-mechanical` → `plan-reviewer-semantic` subagents to scan Stage `{STAGE_ID}` of `{MASTER_PLAN_PATH}` for drift. On drift → writes `§Plan Fix` tuple list + auto-dispatches **`plan-applier`** (Sonnet pair-tail, Mode plan-fix).

## Argument parsing

Split `$ARGUMENTS` on whitespace. First token = `{MASTER_PLAN_PATH}`. Second token = `{STAGE_ID}` (e.g. `7.2`). Missing either → print usage + abort. If `--force-model {model}` present: extract `{model}` (valid: `sonnet`, `opus`, `haiku`); store as `FORCE_MODEL`. Absent or invalid → `FORCE_MODEL` unset.

## Step 1 — Dispatch `plan-reviewer-mechanical` (Haiku, mechanical checks 3–8)

Forward via Agent tool with `subagent_type: "plan-reviewer-mechanical"`:

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/plan-review-mechanical/SKILL.md` end-to-end on Stage `{STAGE_ID}` of `{MASTER_PLAN_PATH}`. Checks 3–8: anchor uniqueness, path existence, gate completeness, invariant coverage, glossary consistency, schema drift. Zero drift → PASS sentinel. Drift → write `§Plan Fix — MECHANICAL` tuple list per `ia/rules/plan-apply-pair-contract.md`. Emit `mechanicalization_score` header.
>
> ## Hard boundaries
>
> - Do NOT run semantic checks (1, 2).
> - Do NOT mutate spec / master-plan — writes only under `§Plan Fix — MECHANICAL`.
> - Do NOT commit.

Mechanical returns `{verdict: "PASS"|"fix", output_bundle: ...}`. Proceed to Step 2 regardless.

## Step 2 — Dispatch `plan-reviewer-semantic` (Sonnet, semantic checks 1–2)

Forward via Agent tool with `subagent_type: "plan-reviewer-semantic"`, passing Step 1 output bundle as context:

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/plan-review-semantic/SKILL.md` end-to-end on Stage `{STAGE_ID}` of `{MASTER_PLAN_PATH}`. Read mechanical output bundle from Step 1. Checks 1–2: goal/intent alignment, impl-plan completeness vs acceptance criteria. Zero drift → PASS. Drift → write `§Plan Fix — SEMANTIC` tuple list per `ia/rules/plan-apply-pair-contract.md`. Emit combined verdict (mechanical + semantic).
>
> ## Hard boundaries
>
> - Do NOT run mechanical checks (3–8).
> - Do NOT mutate spec / master-plan — writes only under `§Plan Fix — SEMANTIC`.
> - Do NOT commit.

Combined verdict: PASS only when both mechanical and semantic PASS. Fix → proceed to Step 3.

## Step 3 — Dispatch `plan-applier` (Mode plan-fix) — conditional

On fix verdict from either Step 1 or Step 2: forward via Agent tool with `subagent_type: "plan-applier"`:

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/plan-applier/SKILL.md` — **Mode: plan-fix**. Read `§Plan Fix — MECHANICAL` + `§Plan Fix — SEMANTIC` tuples verbatim from Stage block. Resolve every anchor to single match before applying. Apply tuples in declared order. Run `npm run validate:master-plan-status` + `npm run validate:backlog-yaml` gate. 1-retry bound on validate fail. Second fail → escalate. Idempotent.
>
> ## Hard boundaries
>
> - Do NOT re-review drift — read tuples verbatim.
> - Do NOT run `validate:all` — plan-review gate only.
> - Do NOT reorder tuples.
> - Do NOT commit.

## Output

Chain summary: verdict + drift count (if any) + tuples applied + validator exit. Next step: `/ship-stage {MASTER_PLAN_PATH} Stage {STAGE_ID}` (Pass A implement + Pass B verify + inline closeout).
