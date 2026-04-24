### Stage 2.3 — Slash-command dispatch flattening (C1 + C2)


**Status:** Draft (tasks _pending_ — not yet filed)

**Pre-conditions:** lifecycle-refactor Stage 10 T10.2 Done (stable-block in agent bodies) + T10.4 Done (F5 tool-uniformity validator for pair-seam agents).

**Objectives:** Strip "Subagent prompt (forward verbatim)" mission-restatement blocks from `/implement`, `/verify-loop`, `/closeout`, `/ship`, `/ship-stage` command bodies. Commands become parameter-forwarding dispatchers (≤60 lines each); subagent bodies remain authoritative. Specific focus on `/ship` which is 192 lines (C2).

**Exit:**

- `.claude/commands/implement.md`: ≤60 lines; Mission + Phase loop stripped; ISSUE_ID forwarding retained.
- `.claude/commands/verify-loop.md`, `closeout.md`, `ship-stage.md`: ≤60 lines each.
- `.claude/commands/ship.md`: ≤60 lines (down from 192); gate logic + parameter forwarding only.
- Human-readable "What this does" block (10–20 lines) preserved at top per C5 Q5 resolution.
- `npm run validate:all` green.

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T2.3.1 | Collapse implement + verify-loop + closeout | _pending_ | _pending_ | In `.claude/commands/implement.md`, `verify-loop.md`, `closeout.md`: remove "Subagent prompt (forward verbatim)" block restating subagent Mission + Phase loop. Retain: "What this does" block (10–20 lines human summary, per Q5 resolution), parameter list (ISSUE_ID / MASTER_PLAN_PATH / STAGE_ID), gate boundary lines. Target ≤60 lines each. `npm run validate:all`. |
| T2.3.2 | Collapse ship-stage + kickoff commands | _pending_ | _pending_ | In `.claude/commands/ship-stage.md` and any remaining command bodies carrying full mission restatement: apply same collapse (≤60 lines; "What this does" header + parameters + gate). Check `.claude/commands/_retired/` for stale references; clean drift (no action if clean). `npm run validate:all`. |
| T2.3.3 | /ship command slim | _pending_ | _pending_ | `.claude/commands/ship.md` (currently ~192 lines): collapse to ≤60 lines. Keep: "What this does" (≤15 lines), ISSUE_ID / MASTER_PLAN_PATH params, gate-boundary check (master plan located?), dispatch line. Strip: full Phase loop restatement, hard-boundaries repeat, example invocations already in subagent body. Model after condensed ship-stage shape from T2.3.2. |
| T2.3.4 | Integration smoke + token delta | _pending_ | _pending_ | Run full `/ship {ISSUE_ID}` dispatch on a dry-run issue; confirm subagent body authoritative (no degraded behavior from stripped command). Estimate per-`/ship` token saving vs pre-C1/C2 baseline: diff collapsed command byte count × invocation frequency from telemetry. Commit finding to `docs/session-token-latency-audit-exploration.md` §Provenance. `npm run validate:all` green. |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_pending — populated by `/audit {{this-doc}} Stage {{N.M}}` once all Tasks reach Done post-verify._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
