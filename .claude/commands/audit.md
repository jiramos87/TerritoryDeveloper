---
description: Stage-scoped bulk `§Audit` author — dispatches `opus-auditor` subagent to synthesize N `§Audit` paragraphs across all Tasks of one Stage in a single Opus pass. Fires ONCE per Stage after verify-loop + code-review complete (all Task §Findings non-empty). Feeds `stage-closeout-planner` downstream. Phase 0 R11 gate blocks if any Task §Findings empty.
argument-hint: "{master-plan-path} Stage {X.Y} [--force-model {model}]"
---

# /audit — dispatch `opus-auditor` subagent (Stage-scoped bulk)

Use `opus-auditor` subagent (`.claude/agents/opus-auditor.md`) to bulk-author `§Audit` paragraphs across all N Tasks of Stage `{STAGE_ID}` in one Opus pass. Runs between per-Task `/code-review` (verify-loop tail) and Stage-scoped `/closeout`. Replaces retired per-Task audit pattern absorbed into seam #4 pair per T7.4 / TECH-471.

## Argument parsing

Split `$ARGUMENTS` on whitespace. First token = `{MASTER_PLAN_PATH}` (repo-relative, `ia/projects/*-master-plan.md`). Second token = `{STAGE_ID}` (e.g. `7.2` or `Stage 7.2`). Missing either → print usage + abort. If `--force-model {model}` present: extract `{model}` (valid: `sonnet`, `opus`, `haiku`); store as `FORCE_MODEL`. Absent or invalid → `FORCE_MODEL` unset.

## Subagent prompt (forward verbatim)

Forward via Agent tool with `subagent_type: "opus-auditor"` (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`):

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/opus-audit/SKILL.md` end-to-end on Stage `{STAGE_ID}` of `{MASTER_PLAN_PATH}`. Phase 0 R11 §Findings gate: verify every non-archived Task has non-empty `## §Findings`; any missing → STOP + block. Phase 1 Load Stage MCP bundle via `domain-context-load` subskill (single call). Phase 2 Read ALL N Task spec sections (§7 Implementation Plan / §Findings / §Verification). Phase 3 Single synthesis round → N `§Audit` paragraphs (consistent voice; no per-Task MCP re-query). Phase 4 Apply via `replace_section` on `## §Audit` (or `insert_after ## §Verification` if absent). Phase 5 Hand-off caveman summary. Does NOT proceed to `§Stage Closeout Plan` — that is `stage-closeout-planner`.
>
> ## Hard boundaries
>
> - Do NOT proceed if any Task has empty `§Findings` — R11 gate blocks; direct user to re-run `/verify-loop`.
> - Do NOT edit other spec sections (§1 / §2 / §7 / §8 / §Code Review / §Findings / §Verification) — audit touches `§Audit` only.
> - Do NOT re-query glossary / router / invariants per-Task — shared bundle loaded once in Phase 1.
> - Do NOT write `§Closeout Plan` / `§Stage Closeout Plan` — that is `stage-closeout-planner` (seam #4 head).
> - Do NOT run validators — seam-scoped writes only.
> - Do NOT guess ambiguous anchors — escalate per `ia/rules/plan-apply-pair-contract.md`.
> - Do NOT commit — user decides.

## Output

Chain summary: Stage {STAGE_ID} — N `§Audit` paragraphs written; Task ids audited; R11 gate result. Next step: `claude-personal "/closeout {MASTER_PLAN_PATH} {STAGE_ID}"` (dispatches `stage-closeout-planner` → `stage-closeout-applier`).
