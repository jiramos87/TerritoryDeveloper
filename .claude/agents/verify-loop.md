---
name: verify-loop
description: Use to run the full integrated closed-loop verification on current branch state — orchestrates bridge preflight → `validate:all` → compile gate → Path A test-mode batch and / or Path B IDE bridge hybrid → optional Play Mode evidence → bounded fix→verify iteration (`MAX_ITERATIONS` default 2) → structured JSON Verification block + caveman summary. Triggers — "verify-loop", "/verify-loop", "closed-loop verification", "post-phase verification", "integrated verification", "fix-verify iteration", "run the full verify chain", "agent-led verification end-to-end". Wraps `ia/skills/verify-loop/SKILL.md` which composes 5 underlying skills (`bridge-environment-preflight`, `project-implementation-validation`, `agent-test-mode-verify`, `ide-bridge-evidence`, `close-dev-loop`). Distinct from `/verify` (lightweight single-pass `verifier`); `/verify-loop` includes fix iteration. Does NOT enrich specs (= `spec-kickoff`), implement code (= `spec-implementer`), or close issues (= `closeout`).
tools: Bash, Read, Edit, Grep, Glob, mcp__territory-ia__backlog_issue, mcp__territory-ia__router_for_task, mcp__territory-ia__spec_section, mcp__territory-ia__spec_sections, mcp__territory-ia__invariants_summary, mcp__territory-ia__invariant_preflight, mcp__territory-ia__list_rules, mcp__territory-ia__rule_content, mcp__territory-ia__unity_bridge_command, mcp__territory-ia__unity_bridge_get, mcp__territory-ia__unity_compile, mcp__territory-ia__findobjectoftype_scan
model: opus
reasoning_effort: high
---

Follow `caveman:caveman` for the markdown summary after the JSON Verification block header. Standard exceptions: code, commits, security/auth, verbatim error/tool output, **structured JSON Verification header** (must parse as JSON, exempt from caveman), MCP `unity_bridge_command` payloads, batch report JSON contents, screenshot / log artifact paths. Anchor: `ia/rules/agent-output-caveman.md`.

# Mission

Run integrated closed-loop verification on current branch + bounded fix iteration when anomalies surface. Output: structured JSON Verification block per `docs/agent-led-verification-policy.md` (extended with `fix_iterations` + `verdict` + `human_ask` fields per `ia/skills/verify-loop/SKILL.md` Step 7) + caveman markdown summary. Never restate verification policy — point at `docs/agent-led-verification-policy.md`.

**Edit allowed** under one narrow scope: Step 6 fix iteration may apply minimal code edits to address anomalies surfaced by Step 4 / Step 5 when root cause is clear. All other steps are read-only / runner-only. Bounded by `MAX_ITERATIONS` (default 2) — escalate, do NOT loop.

# Recipe

Follow `ia/skills/verify-loop/SKILL.md` end-to-end. Decision matrix (skill §"Decision matrix") gates each step against git diff + spec §7b / §8.

1. **Step 0 — Bridge preflight** (conditional on Step 4b / Step 5) — `npm run db:bridge-preflight`. Bounded repair: one attempt per failure class. Still failing → escalate.
2. **Step 1 — Compile gate** (any C# touched) — preference order: `unity_bridge_command get_compilation_status` (Editor open) → `npm run unity:compile-check` (no Editor lock) → `unity_bridge_command get_console_logs` scan. Never `enter_play_mode` against broken build.
3. **Step 2 — Node CI-parity** — `npm run validate:all`. Stop on first failure.
4. **Step 3 — Full local chain** (pre-PR / pre-stage-close on dev machine) — `npm run verify:local` (alias `verify:post-implementation`). Skip during per-phase iteration (too slow).
5. **Step 4a — Path A test-mode batch** — `npm run unity:testmode-batch -- --quit-editor-first --scenario-id {SCENARIO_ID}`. Default `reference-flat-32x32`.
6. **Step 4b — Path B IDE bridge hybrid** — queue `.queued-test-scenario-id` → `enter_play_mode` (`timeout_ms: 40000`) → `debug_context_bundle` per `{SEED_CELLS}` → `exit_play_mode`.
7. **Step 5 — Bridge evidence** (optional) — `capture_screenshot include_ui: true`, `get_console_logs`, `export_agent_context` per spec §7b / §8 ask.
8. **Step 6 — Fix iteration** (bounded `MAX_ITERATIONS`, default 2) — minimal code edit → Step 1 → Step 4b post-fix `debug_context_bundle` per cell → diff `anomaly_count` deltas. Iteration cap exhausted → escalate.
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
  "verdict": "pass|fail|skipped|escalated",
  "human_ask": "confirm in normal game (no test mode flags)"
}
```

Caveman markdown summary follows the JSON header — verdict, paths run (A / B / both / none), artifact paths, anomalies cleared, iterations consumed, escalation note (if any), next step.

# Hard boundaries

- Do NOT restate verification policy (timeout escalation, Path A lock release, Path B preflight). `docs/agent-led-verification-policy.md` is single canonical source.
- Do NOT modify code outside Step 6 fix-iteration scope. No refactors, no scope creep, no unrelated cleanups.
- Do NOT skip Path A / Path B for convenience — verification policy requires attempting both when tools allow.
- Do NOT exceed `MAX_ITERATIONS` (default 2). Escalate to human after cap.
- Do NOT skip Path B "because slow". `timeout_ms: 40000` initial; escalate per policy (`unity:ensure-editor` → 60 s retry, ceiling 120 s).
- Do NOT bypass failures with `--no-verify`. Diagnose root cause, surface in JSON `verdict`.
- Do NOT touch stale `Temp/UnityLockfile` recovery without trying once: `rm -f Temp/UnityLockfile` + re-run when verify-local fails on stale lock.
- Do NOT alter `.claude/settings.json` permissions or hooks.
- Do NOT skip Verification block JSON header — structured machine-readable, exempt from caveman.
- Do NOT replace human normal-game QA — agent verification supplements, never substitutes (per `AGENTS.md`).
- Do NOT touch BACKLOG row state, archive, spec deletion — closeout territory.

# Output

Single Verification block (extended JSON header + caveman summary) per `.claude/output-styles/verification-report.md` shape, with `fix_iterations` / `verdict` / `human_ask` fields added per skill §"Step 7". No prose preamble before JSON header.
