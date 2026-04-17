---
purpose: "Single integrated closed-loop verification recipe orchestrating bridge preflight, Node CI-parity checks, compile gate, agent test mode batch, IDE bridge Play Mode evidence, and bounded fix→verify iteration. Replaces ad-hoc choreography across the 5 underlying skills."
audience: agent
loaded_by: skill:verify-loop
slices_via: invariants_summary, router_for_task
name: verify-loop
description: >
  Use after substantive implementation (per phase or per stage / spec close-out) when one canonical
  closed-loop verification pass is needed. Orchestrates: bridge preflight → Node validate:all →
  compile gate → test-mode batch (Path A) and / or IDE agent bridge (Path B) → optional Play Mode
  evidence → diff anomalies → bounded fix→verify iteration → structured Verification block. Defers
  to the 5 underlying skills (bridge-environment-preflight, project-implementation-validation,
  agent-test-mode-verify, ide-bridge-evidence, close-dev-loop) for atomic mechanics — this skill
  is the one place that wires them together. Triggers: "/verify-loop", "closed-loop verification",
  "post-phase verification", "integrated verification", "fix-verify iteration", "run the full
  verify chain", "agent-led verification end-to-end".
---

# Verify loop — integrated closed-loop verification

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Exceptions: Verification block JSON (must parse), `unity_bridge_command` payloads, batch report JSON, screenshot/log artifact paths.

**Policy:** [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md) — contract. This skill = operational recipe.

**Composes:** [`bridge-environment-preflight`](../bridge-environment-preflight/SKILL.md) (Step 0) · [`project-implementation-validation`](../project-implementation-validation/SKILL.md) (Step 2) · [`agent-test-mode-verify`](../agent-test-mode-verify/SKILL.md) (Steps 4a/4b) · [`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md) (Step 5) · [`close-dev-loop`](../close-dev-loop/SKILL.md) (Step 6).

**Related:** [`project-spec-implement`](../project-spec-implement/SKILL.md) · [`project-stage-close`](../project-stage-close/SKILL.md) · [`project-spec-close`](../project-spec-close/SKILL.md).

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `ISSUE_ID` | User prompt OR active spec | `BUG-`/`FEAT-`/`TECH-`/`ART-`/`AUDIO-` for `backlog_issue` context |
| `CHANGED_AREAS` | Git diff inspection | C#? Fixtures? IA? MCP? Indexes? — drives gate decisions |
| `SCENARIO_ID` | Spec §7b OR user | Default `reference-flat-32x32` for Path A. `_pending_` if no test-mode gate fires |
| `SEED_CELLS` | Spec §7b OR repro | 1–3 `"x,y"` for Path B `debug_context_bundle` + `close-dev-loop` |
| `MAX_ITERATIONS` | Default 2 | Fix→verify cycles before escalation |

---

## Decision matrix — which steps run

Inspect git diff + spec §7b / §8 against this table. Skip steps with **all rows N/A** and document why in the Verification block.

| Step | Run when | Skip when |
|------|----------|-----------|
| 0 — Bridge preflight | Step 4b Path B will run; OR Step 5 evidence will run | No bridge / Postgres operations needed |
| 1 — Compile gate | Any C# / Unity asset edits | IA / docs / fixture-only edits |
| 2 — Node validate:all | MCP / fixtures / IA index / glossary / spec body changes | Pure runtime C# only (rely on Step 1) |
| 3 — `verify:local` (full chain) | Pre-PR / pre-stage-close on dev machine | Per-phase iteration (too slow); CI-only environment |
| 4a — Path A test-mode batch | Save / load pipeline; `GameSaveManager`; scenario JSON; `GridManager` init; sim tick; spec §7b row asks for batch | Pure UI / authoring / docs |
| 4b — Path B bridge hybrid | Spec §7b row asks for Play Mode assertion; Path A unavailable; need `debug_context_bundle` | No Play Mode evidence required |
| 5 — Bridge evidence | Spec §7b / §8 explicitly asks for screenshots or Console capture | Acceptance covered by 4a JSON or batch golden |
| 6 — Fix iteration | Step 4 / Step 5 surface anomalies AND root cause clear | All previous steps green; OR cause unclear → escalate |

---

## Tool recipe (ordered) — one canonical sequence

### Step 0 — Bridge preflight (conditional on Step 4b / Step 5)

`npm run db:bridge-preflight` — exit codes 0/1/2/3/4 per [`bridge-environment-preflight`](../bridge-environment-preflight/SKILL.md). Bounded repair: one attempt per failure class (`db:setup-local` for code 2; `db:migrate` for code 3). Still failing → escalate, do NOT loop.

Skip entirely when no bridge / Postgres operations queued.

### Step 1 — Compile gate (any C# touched)

Preference order, do NOT run `enter_play_mode` until clean:

1. **Editor open + bridge available** → `mcp__territory-ia__unity_bridge_command` `kind: get_compilation_status` (alias `unity_compile`). Read `response.compilation_status` (`compiling` / `compilation_failed` / `last_error_excerpt` / `recent_error_messages`). Poll while `compiling` (5–8 attempts, ~2–3 s).
2. **No Editor lock on `projectPath`** → `npm run unity:compile-check` from repo root. Script sources `.env` / `.env.local` — do NOT skip because `$UNITY_EDITOR_PATH` is empty in agent shell.
3. **Bridge open but ambiguous** → `unity_bridge_command` `kind: get_console_logs`, scan `error CS` / compiler errors.
4. **Confirmed errors** → fix (Step 6) before continuing.

Full mechanics: [`close-dev-loop`](../close-dev-loop/SKILL.md) § Compile gate.

### Step 2 — Node CI-parity checks

`npm run validate:all` from repo root. Manifest = `compute-lib:build` + `validate:dead-project-specs` + `test:ia` + `validate:fixtures` + `generate:ia-indexes -- --check`. Stop on first failure → fix → re-run from Step 1.

Full skip table: [`project-implementation-validation`](../project-implementation-validation/SKILL.md) § "When to skip".

### Step 3 — Full local chain (pre-PR / pre-close, dev machine only)

`npm run verify:local` (alias `verify:post-implementation`) — `validate:all` → Lockfile check → save/quit Editor → `unity:compile-check` → `db:migrate` → `db:bridge-preflight` → reopen Editor → `db:bridge-playmode-smoke`. Skip during per-phase iteration (too slow); run before submitting PR or closing a stage / spec.

### Step 4a — Path A test mode batch (when gate fires)

```bash
npm run unity:testmode-batch -- --quit-editor-first --scenario-id {SCENARIO_ID}
```

- `--quit-editor-first` releases REPO_ROOT lock (batchmode needs exclusive).
- Default `{SCENARIO_ID}`: `reference-flat-32x32`. Ad-hoc JSON → `--scenario-path` (absolute).
- `--golden-path` → asserts CityStats vs committed JSON; mismatch → exit 8.
- Artifacts: `tools/reports/agent-testmode-batch-*.json` (schema_version 2), `unity-testmode-batch-*.log`.
- Optional: `DATABASE_URL` + migration `0009` → `MetricsRecorder` appends `city_metrics_history` (queryable via `city_metrics_query`).

Exit code reference: [`agent-test-mode-verify`](../agent-test-mode-verify/SKILL.md) § Exit codes.

### Step 4b — Path B IDE agent bridge hybrid (when gate fires)

Use when batch CLI unavailable or need `debug_context_bundle`/screenshots.

1. Write `{SCENARIO_ID}` (single line) to `tools/fixtures/scenarios/.queued-test-scenario-id` (gitignored). Path-based loads use `-testScenarioPath` instead.
2. Step 0 must be green (Postgres + `agent_bridge_job`).
3. `unity_bridge_command` `kind: enter_play_mode`, `timeout_ms: 40000` → poll `get_play_mode_status` until `play_mode_ready` + `ready: true` + grid dims when `has_grid_dimensions`.
4. `unity_bridge_command` `kind: debug_context_bundle`, `timeout_ms: 40000`, `seed_cell: "x,y"` per `{SEED_CELLS}` — store `response.bundle` (`anomaly_count`, `anomalies`, `cell_export`, screenshot, console).
5. Optional: `get_console_logs`, `capture_screenshot` (`include_ui: true`) per [`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md).
6. `unity_bridge_command` `kind: exit_play_mode`, `timeout_ms: 40000`.

Both paths in one session → run Path A first (`--quit-editor-first`), then `npm run unity:ensure-editor` before Path B.

Timeout escalation: `40000` initial → on timeout `npm run unity:ensure-editor` → retry 60 s. Ceiling 120 s (`UNITY_BRIDGE_TIMEOUT_MS_MAX`).

### Step 5 — Bridge Play Mode evidence (optional, on top of Step 4b)

When spec §7b / §8 explicitly asks for screenshots or buffered Console:

- `capture_screenshot` `include_ui: true` → `tools/reports/bridge-screenshots/*.png` (gitignored). Game tab must be visible; ~15 s timeout → `ok: false`.
- `get_console_logs` filters: `since_utc`, `severity_filter` (all|log|warning|error), `tag_filter`, `max_lines` (1–2000).
- `export_agent_context` Reports → optional `seed_cell "x,y"` for Moore center.

Full `kind` reference: [`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md) § MCP tools.

### Step 6 — Fix iteration (bounded by `{MAX_ITERATIONS}`)

If Step 4 / Step 5 surface anomalies AND root cause clear:

1. Edit C# / assets — minimal diff. English comments / logs.
2. Step 1 (compile gate) → must be clean before re-entering Play Mode.
3. Step 4b post-fix `debug_context_bundle` per cell → diff `anomaly_count` deltas, added/removed `anomalies`, height/child-name hints, screenshot diff.
4. Verdict: anomalies cleared → continue to handoff. Anomalies remain + cause clear → repeat from 1. Iteration count == `{MAX_ITERATIONS}` (default 2) → escalate, do NOT loop.

Full diff structure: [`close-dev-loop`](../close-dev-loop/SKILL.md) §§ DIFF / VERDICT / ITERATE.

### Step 7 — Verification block + handoff

Emit single Verification block per [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md). JSON header (must parse) + caveman markdown summary. Required fields:

```json
{
  "issue_id": "{ISSUE_ID}",
  "ran": ["preflight","compile","validate_all","testmode_batch","bridge_hybrid","evidence","fix_loop"],
  "skipped": [{"step":"...","reason":"..."}],
  "validate_all_exit": 0,
  "compile_check_exit": 0,
  "compile_gate_path": "bridge|cli|console",
  "testmode_batch": {"path_a_exit": 0, "report_json": "tools/reports/agent-testmode-batch-2026-04-13T...json", "ok": true},
  "bridge_hybrid": {"preflight_exit": 0, "play_mode_state": "edit_mode", "bundle_paths": ["..."], "anomaly_count_after": 0},
  "evidence": {"screenshots": ["..."], "logs": ["..."]},
  "fix_iterations": 0,
  "verdict": "pass|fail|skipped|escalated",
  "human_ask": "confirm in normal game (no test mode flags)"
}
```

Markdown summary (caveman): verdict, paths run (A / B / both / none), artifact paths, anomalies cleared, iterations consumed, escalation note (if any), next step (human QA / next phase / stage close / umbrella close).

---

## Placeholders

| Key | Default / meaning |
|-----|-------------------|
| `{ISSUE_ID}` | Active BACKLOG id |
| `{SCENARIO_ID}` | Kebab-case id under `tools/fixtures/scenarios/`; `reference-flat-32x32` default |
| `{SEED_CELLS}` | 1–3 `"x,y"` from spec §7b or repro |
| `{MAX_ITERATIONS}` | 2 |

---

## Optional territory-ia preface

Session maps to `{ISSUE_ID}` → `mcp__territory-ia__backlog_issue` for Files / Acceptance / §7b rows. `mcp__territory-ia__invariants_summary` once when paired with guardrail / runtime C# edits.

---

## Guardrails

- IF Step 0 fails after one bounded repair → escalate; do NOT loop preflight.
- IF Step 1 reports `compilation_failed` → fix before any Step 4 / Step 5 attempt; never `enter_play_mode` against a broken build.
- IF Step 2 / Step 3 fail → stop, fix, re-run from Step 1; do NOT continue to Play Mode steps with red Node checks.
- IF `{MAX_ITERATIONS}` exhausted → escalate to human; do NOT silently retry.
- IF Step 4b times out → run escalation protocol (`unity:ensure-editor` → 60 s retry, ceiling 120 s); do NOT raise `timeout_ms` blindly.
- Do NOT skip Path A/B for convenience — verification policy requires attempting both when tools allow.
- Do NOT replace human normal-game QA — agent verification supplements, never substitutes (per `AGENTS.md`).
- IF attributing a failure to a named issue id (e.g. "TECH-227 territory") → FIRST verify that id appears as open (`- [ ]`) in `BACKLOG.md`. If not found (closed or never filed), report the failure as "pre-existing / unowned" and do NOT name an issue id.
- Do NOT commit verification artifact paths in spec prose — keep paths in Verification block / handoff only.

---

## Seed prompt

```markdown
Run the verify-loop workflow for {ISSUE_ID}.

Follow ia/skills/verify-loop/SKILL.md end-to-end. Inputs:
  ISSUE_ID: {id}
  CHANGED_AREAS: {summary from git diff}
  SCENARIO_ID: {kebab-case id or _pending_}
  SEED_CELLS: {"x,y" list or _pending_}
  MAX_ITERATIONS: {default 2}

Apply Decision matrix to gate Steps 0–6; emit Verification block per docs/agent-led-verification-policy.md (JSON header + caveman summary). Stop on first failure; bounded repair only — escalate rather than loop.
```

---

## Handoff (required shape)

- **Verdict:** `pass` / `fail` / `skipped (with reason)` / `escalated`.
- **Paths run:** A / B / both / none (with reason if none).
- **Artifacts:** newest `agent-testmode-batch-*.json`; bridge `bundle` + screenshot paths; `validate:all` log if failure.
- **Iterations:** consumed / max (e.g. `1 / 2`).
- **Human ask:** confirm in normal game (no test-mode flags); approve PR / next stage / umbrella close.
