### Stage 3.1 — Session-start preamble + compact-survival (D2 + D4)


**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Refactor `session-start-prewarm.sh` so the cacheable prefix is stable across sessions (volatile data to stderr; deterministic block to stdout). Add compact-survival hook writing `.claude/last-compact-summary.md` so agents can re-orient after context compaction.

**Exit:**

- `tools/scripts/claude-hooks/session-start-prewarm.sh`: `stderr` carries branch + dirty count; `stdout` emits fixed block: `[territory-developer] MCP: territory-ia v{version} | Ruleset: invariants + lifecycle + caveman | Freeze: {active|lifted}`.
- `.claude/settings.json` Stop/PostCompact hook entry: runs `tools/scripts/claude-hooks/compact-summary.sh`.
- `tools/scripts/claude-hooks/compact-summary.sh` (new): writes `.claude/last-compact-summary.md` with fields `active_task_id`, `active_stage`, `last_3_tools`, `ts`.
- `.claude/last-compact-summary.md` gitignored (session-ephemeral state).
- `npm run validate:all` green.

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T3.1.1 | Session-start deterministic preamble | _pending_ | _pending_ | Refactor `tools/scripts/claude-hooks/session-start-prewarm.sh`: move `branch=$(git branch ...)` + dirty-count line to emit via `>&2` (stderr); add fixed stdout block: `echo "[territory-developer] MCP: territory-ia v$(…) | Ruleset: invariants+lifecycle+caveman | Freeze: $(cat ia/state/lifecycle-refactor-migration.json | jq -r '.status')"`. Volatile suffix no longer destabilises cached prefix. |
| T3.1.2 | Runtime-state.json skeleton (F4 prep) | _pending_ | _pending_ | Committed schema `tools/schemas/runtime-state.schema.json` + `ia/state/runtime-state.example.json`; live `ia/state/runtime-state.json` **gitignored** (per clone). Fields: `last_verify_exit_code`, `last_bridge_preflight_exit_code`, `queued_test_scenario_id`, `updated_at`. `active_task_id` / `active_stage` live only in `.claude/active-session.json` or `.cursor/active-session.json` — not in shared runtime-state file. Ship MCP `runtime_state` (read + write under flock). SessionStart reads `ia/state/runtime-state.json` + optional active-session for preamble. |
| T3.1.3 | Compact-survival hook | _pending_ | _pending_ | Author `tools/scripts/claude-hooks/compact-summary.sh`: on Stop/PostCompact event reads `ia/state/runtime-state.json` + last 3 entries from `.claude/telemetry/{session-id}.jsonl`; writes `.claude/last-compact-summary.md` (`active_task_id`, `active_stage`, `last_3_tools`, `ts`). Add Stop hook entry to `.claude/settings.json` hooks array. Add `.claude/last-compact-summary.md` to `.gitignore`. |
| T3.1.4 | Compact re-orientation test | _pending_ | _pending_ | Manual test: run session → compact → resume; verify `.claude/last-compact-summary.md` present + readable; confirm SessionStart preamble emits `active_task_id` from it. Confirm `npm run validate:all` green. Document compact-survival UX in `docs/agent-led-verification-policy.md` §Session continuity (new 3-line sub-section). |

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
