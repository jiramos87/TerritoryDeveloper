---
description: Run the full integrated closed-loop verification on current branch — bridge preflight → `validate:all` → compile gate → Path A test-mode batch and / or Path B IDE bridge hybrid → optional Play Mode evidence → bounded fix→verify iteration (`MAX_ITERATIONS` default 2) → extended JSON Verification block + caveman summary. Dispatches the `verify-loop` subagent. Distinct from `/verify` (lightweight single-pass `verifier`, no fix iteration).
argument-hint: "[ISSUE_ID] [--scenario {id}] [--seed-cells x,y[,x,y...]] [--max-iterations N] [--tooling-only]"
---

# /verify-loop — dispatch `verify-loop` subagent

Use `verify-loop` subagent (`.claude/agents/verify-loop.md`) to orchestrate the integrated closed-loop verification recipe in `ia/skills/verify-loop/SKILL.md`. Composes 5 underlying skills (`bridge-environment-preflight`, `project-implementation-validation`, `agent-test-mode-verify`, `ide-bridge-evidence`, `close-dev-loop`).

`$ARGUMENTS` carries optional inputs: leading `ISSUE_ID` (active BACKLOG id), `--scenario {SCENARIO_ID}` (Path A / Path B gate; default `reference-flat-32x32`), `--seed-cells x,y[,x,y...]` (Path B `debug_context_bundle` seeds), `--max-iterations N` (fix loop cap; default 2), `--tooling-only` (pre-matrix bypass for pure tooling refactors — skill §"Pre-matrix mode gate" asserts no `Assets|Packages|ProjectSettings` dirty paths then runs Step 2 + Step 7 only). All optional — subagent infers from git diff + active spec §7b / §8 when omitted.

Distinct from `/verify`: `/verify` runs the lightweight `verifier` subagent (single pass, no code edits, policy-only reporting). `/verify-loop` runs the full closed-loop recipe with bounded fix iteration (Edit allowed narrowly for Step 6 fixes only).

## Step 0 — Context banner (before dispatch, when ISSUE_ID present)

If `$ARGUMENTS` contains a leading ISSUE_ID (`BUG-` / `FEAT-` / `TECH-` / `ART-` / `AUDIO-`), resolve and print before dispatching:

1. Glob `ia/projects/*-master-plan.md` → grep each for the ISSUE_ID → identify owning master plan.
2. Print:
   ```
   VERIFY-LOOP {ISSUE_ID} — {issue title from BACKLOG.md}
     master plan : {Plan Name} (ia/projects/{master-plan-filename})
   ```
   If no master plan found: `master plan: (none — standalone issue)`. If no ISSUE_ID in args, skip banner.

## Subagent prompt (forward verbatim)

Forward via Agent tool with `subagent_type: "verify-loop"`:

> Follow `caveman:caveman` for the markdown summary after the JSON Verification block header. Standard exceptions: code, commits, security/auth, verbatim error/tool output, **structured JSON Verification header** (must parse as JSON, exempt from caveman), MCP `unity_bridge_command` payloads, batch report JSON contents, screenshot / log artifact paths. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run integrated closed-loop verification on current branch + bounded fix iteration when anomalies surface. Follow `ia/skills/verify-loop/SKILL.md` end-to-end. Output: structured JSON Verification block per `docs/agent-led-verification-policy.md` (extended with `fix_iterations` + `verdict` + `human_ask` fields per skill §"Step 7") + caveman markdown summary. Never restate verification policy — point at `docs/agent-led-verification-policy.md`.
>
> ## Inputs
>
> Parse from user invocation (all optional):
>
> ```
> $ARGUMENTS
> ```
>
> - `ISSUE_ID` — active BACKLOG id (`BUG-` / `FEAT-` / `TECH-` / `ART-` / `AUDIO-`) for `backlog_issue` context.
> - `SCENARIO_ID` — kebab-case id under `tools/fixtures/scenarios/`; default `reference-flat-32x32`.
> - `SEED_CELLS` — 1–3 `"x,y"` for Path B `debug_context_bundle`; infer from spec §7b if omitted.
> - `MAX_ITERATIONS` — default 2.
> - `TOOLING_ONLY` — flag (default false). When present as `--tooling-only` in `$ARGUMENTS`: apply skill §"Pre-matrix mode gate" — assert no `Assets|Packages|ProjectSettings` dirty paths, bypass Decision matrix, run Step 2 + Step 7 only. Use only for pure tooling surface refactors (MCP TypeScript / web Next.js / skills-agents-commands markdown / docs / scripts). Never on mixed diffs.
>
> ## Execution sequence (IF `--tooling-only` → pre-matrix bypass; ELSE decision matrix gates each step)
>
> **Pre-matrix mode gate** (fires only when `--tooling-only` set): run `git status --porcelain` + `git diff --name-only` against branch base; assert zero matches for regex `^(Assets|Packages|ProjectSettings)/`. Miss → emit `verdict: "fail"` citing dirty Unity paths + abort. Hit → skip Steps 0, 1, 3, 4a, 4b, 5, 6 (record each in `skipped` array with reason `"tooling_only_flag"`); run Step 2 + Step 7 only; JSON header records `mode: "tooling_only"` + `path_b: "skipped_not_required"`.
>
> 1. **Step 0 — Bridge preflight** (conditional on Step 4b / Step 5) — `npm run db:bridge-preflight`. Bounded repair: one attempt per failure class; still failing → escalate.
> 2. **Step 1 — Compile gate** (any C# touched) — preference order: `unity_bridge_command get_compilation_status` → `npm run unity:compile-check` → `unity_bridge_command get_console_logs` scan. Never `enter_play_mode` against broken build.
> 3. **Step 2 — Node CI-parity** — `npm run validate:all`. Stop on first failure.
> 4. **Step 3 — Full local chain** (pre-PR / pre-stage-close only) — `npm run verify:local` (alias `verify:post-implementation`). Skip during per-phase iteration (too slow).
> 5. **Step 4a — Path A test-mode batch** — `npm run unity:testmode-batch -- --quit-editor-first --scenario-id {SCENARIO_ID}`.
> 6. **Step 4b — Path B IDE bridge hybrid** — queue `.queued-test-scenario-id` → `enter_play_mode` (`timeout_ms: 40000`) → `debug_context_bundle` per `{SEED_CELLS}` → `exit_play_mode`.
> 7. **Step 5 — Bridge evidence** (optional) — `capture_screenshot include_ui: true`, `get_console_logs`, `export_agent_context` per spec §7b / §8 ask.
> 8. **Step 6 — Fix iteration** (bounded `MAX_ITERATIONS`) — minimal code edit → Step 1 → Step 4b post-fix `debug_context_bundle` per cell → diff `anomaly_count` deltas. Cap exhausted → escalate.
> 9. **Step 7 — Verification block + handoff** — emit single block (JSON header + caveman summary).
>
> Both paths in one session → Path A first (`--quit-editor-first`), then `npm run unity:ensure-editor` before Path B.
>
> ## Hard boundaries
>
> - Do NOT restate verification policy (timeout escalation, Path A lock release, Path B preflight). `docs/agent-led-verification-policy.md` is single canonical source.
> - Do NOT modify code outside Step 6 fix-iteration scope. No refactors, no scope creep, no unrelated cleanups.
> - Do NOT skip Path A / Path B for convenience — policy requires attempting both when tools allow.
> - Do NOT exceed `MAX_ITERATIONS` (default 2). Escalate to human after cap.
> - Do NOT skip Path B "because slow". `timeout_ms: 40000` initial; escalate per policy (`unity:ensure-editor` → 60 s retry, ceiling 120 s).
> - Do NOT bypass failures with `--no-verify`. Diagnose root cause, surface in JSON `verdict`.
> - Do NOT alter `.claude/settings.json` permissions or hooks.
> - Do NOT skip Verification block JSON header — structured machine-readable, exempt from caveman.
> - Do NOT replace human normal-game QA — agent verification supplements, never substitutes (per `AGENTS.md`).
> - Do NOT touch BACKLOG row state, archive, spec deletion — closeout territory.
> - Do NOT pass `--tooling-only` on mixed diffs. Skill Pre-matrix mode gate fails loud when `Assets|Packages|ProjectSettings` dirty. Full `/verify-loop` is required whenever Unity surface is touched.
>
> ## Output
>
> Single Verification block (extended JSON header + caveman summary) per `.claude/output-styles/verification-report.md` shape, with `fix_iterations` / `verdict` / `human_ask` fields added per skill §"Step 7". No prose preamble before JSON header. Required header keys:
>
> ```json
> {
>   "issue_id": "{ISSUE_ID}",
>   "ran": ["preflight","compile","validate_all","testmode_batch","bridge_hybrid","evidence","fix_loop"],
>   "skipped": [{"step":"...","reason":"..."}],
>   "validate_all_exit": 0,
>   "compile_check_exit": 0,
>   "compile_gate_path": "bridge|cli|console",
>   "testmode_batch": {"path_a_exit": 0, "report_json": "...", "ok": true},
>   "bridge_hybrid": {"preflight_exit": 0, "play_mode_state": "edit_mode", "bundle_paths": ["..."], "anomaly_count_after": 0},
>   "evidence": {"screenshots": ["..."], "logs": ["..."]},
>   "fix_iterations": 0,
>   "path_b": "ran|skipped_batched|skipped_not_required",
>   "mode": "full|tooling_only",
>   "verdict": "pass|fail|skipped|escalated",
>   "human_ask": "confirm in normal game (no test mode flags)"
> }
> ```
