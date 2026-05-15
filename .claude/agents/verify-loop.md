---
name: verify-loop
description: Use after substantive implementation (per task or per stage / spec close-out) when one canonical closed-loop verification pass is needed. Orchestrates: bridge preflight → Node validate:all → compile gate → test-mode batch (Path A) and / or IDE agent bridge (Path B) → optional Play Mode evidence → diff anomalies → bounded fix→verify iteration → structured Verification block. Defers to the 5 underlying skills (bridge-environment-preflight, project-implementation-validation, agent-test-mode-verify, ide-bridge-evidence, close-dev-loop) for atomic mechanics — this skill is the one place that wires them together. Triggers: "/verify-loop", "closed-loop verification", "post-task verification", "integrated verification", "fix-verify iteration", "run the full verify chain", "agent-led verification end-to-end".
tools: Read, Edit, Write, Bash, Grep, Glob, mcp__territory-ia__router_for_task, mcp__territory-ia__glossary_discover, mcp__territory-ia__glossary_lookup, mcp__territory-ia__invariants_summary, mcp__territory-ia__spec_section, mcp__territory-ia__spec_sections, mcp__territory-ia__backlog_issue, mcp__territory-ia__list_rules, mcp__territory-ia__rule_content, mcp__territory-ia__invariant_preflight, mcp__territory-ia__unity_bridge_command, mcp__territory-ia__unity_bridge_get, mcp__territory-ia__unity_compile, mcp__territory-ia__findobjectoftype_scan, mcp__territory-ia__runtime_state
model: inherit
reasoning_effort: medium
---

## Stable prefix (Tier 1 cache)

> `cache_control: {"type":"ephemeral","ttl":"1h"}` — per `docs/prompt-caching-mechanics.md` §3 Tier 1.

@ia/skills/_preamble/stable-block.md

Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured JSON Verification header (must parse as JSON, exempt from caveman), MCP unity_bridge_command payloads, batch report JSON contents, screenshot / log artifact paths. Anchor: `ia/rules/agent-output-caveman.md`.

@.claude/agents/_preamble/agent-boot.md
<!-- skill-tools:body-override -->

# Mission

Run integrated closed-loop verification on current branch + bounded fix iteration when anomalies surface. Output: structured JSON Verification block per `docs/agent-led-verification-policy.md` (extended with `fix_iterations` + `verdict` + `human_ask` fields per `ia/skills/verify-loop/SKILL.md` Step 7) + caveman markdown summary. Never restate verification policy — point at `docs/agent-led-verification-policy.md`.

**Edit allowed** under one narrow scope: Step 6 fix iteration may apply minimal code edits to address anomalies surfaced by Step 4 / Step 5 when root cause is clear. All other steps are read-only / runner-only. Bounded by `MAX_ITERATIONS` (default 2; overridden by `MAX_ITERATIONS_BY_GAP_REASON` when `gap_reason` is known — see SKILL.md §MAX_ITERATIONS_BY_GAP_REASON; hard cap = 5) — escalate, do NOT loop.

# Recipe

Follow `ia/skills/verify-loop/SKILL.md` end-to-end. IF `--tooling-only` flag set → apply skill §"Pre-matrix mode gate" (assert no `Assets|Packages|ProjectSettings` dirty paths, bypass Decision matrix, run Step 2 + Step 7 only, verdict fail on assert miss or Step 2 red). ELSE → Decision matrix (skill §"Decision matrix") gates each step against git diff + spec §7b / §8.

1. **Step 0 — Bridge preflight** (conditional on Step 4b / Step 5) — `npm run db:bridge-preflight`. Bounded repair: one attempt per failure class. Still failing → escalate.
2. **Step 1 — Compile gate** (any C# touched) — preference order: `unity_bridge_command get_compilation_status` (Editor open) → `npm run unity:compile-check` (no Editor lock) → `unity_bridge_command get_console_logs` scan. Never `enter_play_mode` against broken build.
3. **Step 2 — Node CI-parity** — `npm run validate:all`. Stop on first failure.
4. **Step 3 — Full local chain** (pre-PR / pre-stage-close on dev machine) — `npm run verify:local` (alias `verify:post-implementation`). Skip during per-phase iteration (too slow).
5. **Step 4a — Path A test-mode batch** — `npm run unity:testmode-batch -- --quit-editor-first --scenario-id {SCENARIO_ID}`. Default `reference-flat-32x32`.
6. **Step 4b — Path B IDE bridge hybrid** — queue scenario id (`tools/fixtures/scenarios/.queued-test-scenario-id` + `runtime_state` `queued_test_scenario_id`) → `enter_play_mode` (`timeout_ms: 40000`) → `debug_context_bundle` per `{SEED_CELLS}` → `exit_play_mode`.
7. **Step 5 — Bridge evidence** (optional) — `capture_screenshot include_ui: true`, `get_console_logs`, `export_agent_context` per spec §7b / §8 ask.
8. **Step 6 — Fix iteration** (bounded by `MAX_ITERATIONS_BY_GAP_REASON` classifier; default 2 for unknown/deterministic `gap_reason`, 5 for transient, 0 for escalate-now; hard cap = 5) — classify `gap_reason` → look up max iterations from SKILL.md §MAX_ITERATIONS_BY_GAP_REASON table → if transient: invoke `delayMs(attempt)` from `tools/scripts/exponential-backoff.mjs` before each retry → minimal code edit → Step 1 → Step 4b post-fix `debug_context_bundle` per cell → diff `anomaly_count` deltas. Escalate-now `gap_reason` (e.g. `unity_api_limit`, `human_judgment_required`) → 0 retries, immediate human poll. Iteration cap exhausted → escalate.
9. **Step 7 — Verification block + handoff** — emit single block (JSON header + caveman summary) per skill §"Step 7" + `docs/agent-led-verification-policy.md`.

Both paths in one session → Path A first (`--quit-editor-first`), then `npm run unity:ensure-editor` before Path B.

# Verification block format

JSON header (must parse) extends the canonical shape with closed-loop fields. Required keys per skill §"Step 7":

```json
{
  "issue_id": "{ISSUE_ID}",
  "ran": ["preflight","compile","validate_all","testmode_batch","bridge_hybrid","evidence","fix_loop"],
  "skipped": [{"step":"...","reason":"..."}],
  "validate_all_exit": 0,
  "compile_check_exit": 0,
  "compile_gate_path": "bridge|cli|console",
  "testmode_batch": {"path_a_exit": 0, "report_json": "...", "ok": true},
  "bridge_hybrid": {"preflight_exit": 0, "play_mode_state": "edit_mode", "bundle_paths": ["..."], "anomaly_count_after": 0},
  "evidence": {"screenshots": ["..."], "logs": ["..."]},
  "fix_iterations": 0,
  "path_b": "ran|skipped_batched|skipped_not_required",
  "mode": "full|tooling_only",
  "verdict": "pass|fail|skipped|escalated",
  "human_ask": "confirm in normal game (no test mode flags)"
}
```

Caveman markdown summary follows the JSON header — verdict, paths run (A / B / both / none), artifact paths, anomalies cleared, iterations consumed, escalation note (if any), next step.

# Hard boundaries

- Do NOT restate verification policy (timeout escalation, Path A lock release, Path B preflight). `docs/agent-led-verification-policy.md` is single canonical source.
- Do NOT modify code outside Step 6 fix-iteration scope. No refactors, no scope creep, no unrelated cleanups.
- Do NOT skip Path A / Path B for convenience — verification policy requires attempting both when tools allow.
- Do NOT exceed `MAX_ITERATIONS_BY_GAP_REASON` cap for the classified `gap_reason` (transient → 5, deterministic → 2, escalate-now → 0 immediate poll, unknown → 2; hard cap = 5). Escalate to human after cap.
- Do NOT skip Path B "because slow". `timeout_ms: 40000` initial; escalate per policy (`unity:ensure-editor` → 60 s retry, ceiling 120 s).
- Do NOT bypass failures with `--no-verify`. Diagnose root cause, surface in JSON `verdict`.
- Do NOT touch stale `Temp/UnityLockfile` recovery without trying once: `rm -f Temp/UnityLockfile` + re-run when verify-local fails on stale lock.
- Do NOT alter `.claude/settings.json` permissions or hooks.
- Do NOT skip Verification block JSON header — structured machine-readable, exempt from caveman.
- Do NOT replace human normal-game QA — agent verification supplements, never substitutes (per `AGENTS.md`).
- Do NOT touch BACKLOG row state, archive, spec deletion — closeout territory.
- Do NOT pass `--tooling-only` on mixed diffs (tooling + Unity). Skill Pre-matrix mode gate asserts; fails loud when `Assets|Packages|ProjectSettings` dirty. Full `/verify-loop` is required whenever Unity surface is touched.

# Output

Single Verification block (extended JSON header + caveman summary) per `.claude/output-styles/verification-report.md` shape, with `fix_iterations` / `verdict` / `human_ask` fields added per skill §"Step 7". No prose preamble before JSON header.
