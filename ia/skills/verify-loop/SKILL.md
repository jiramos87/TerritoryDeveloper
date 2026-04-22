---
purpose: "Single integrated closed-loop verification recipe orchestrating bridge preflight, Node CI-parity checks, compile gate, agent test mode batch, IDE bridge Play Mode evidence, and bounded fix→verify iteration. Replaces ad-hoc choreography across the 5 underlying skills."
audience: agent
loaded_by: skill:verify-loop
slices_via: invariants_summary, router_for_task
name: verify-loop
description: >
  Use after substantive implementation (per task or per stage / spec close-out) when one canonical
  closed-loop verification pass is needed. Orchestrates: bridge preflight → Node validate:all →
  compile gate → test-mode batch (Path A) and / or IDE agent bridge (Path B) → optional Play Mode
  evidence → diff anomalies → bounded fix→verify iteration → structured Verification block. Defers
  to the 5 underlying skills (bridge-environment-preflight, project-implementation-validation,
  agent-test-mode-verify, ide-bridge-evidence, close-dev-loop) for atomic mechanics — this skill
  is the one place that wires them together. Triggers: "/verify-loop", "closed-loop verification",
  "post-task verification", "integrated verification", "fix-verify iteration", "run the full
  verify chain", "agent-led verification end-to-end".
model: inherit
phases:
  - "Bridge preflight"
  - "Compile gate"
  - "Node CI-parity checks"
  - "Full local chain"
  - "Path A test mode"
  - "Path B bridge hybrid"
  - "Play Mode evidence"
  - "Fix iteration"
  - "Verification block"
---

# Verify loop — integrated closed-loop verification

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Exceptions: Verification block JSON (must parse), `unity_bridge_command` payloads, batch report JSON, screenshot/log artifact paths.

**Policy:** [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md) — contract. This skill = operational recipe.

**Composes:** [`bridge-environment-preflight`](../bridge-environment-preflight/SKILL.md) (Step 0) · [`project-implementation-validation`](../project-implementation-validation/SKILL.md) (Step 2) · [`agent-test-mode-verify`](../agent-test-mode-verify/SKILL.md) (Steps 4a/4b) · [`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md) (Step 5) · [`close-dev-loop`](../close-dev-loop/SKILL.md) (Step 6).

**Related:** [`project-spec-implement`](../project-spec-implement/SKILL.md) · Stage-scoped closeout pair: [`stage-closeout-plan`](../stage-closeout-plan/SKILL.md) → [`plan-applier`](../plan-applier/SKILL.md) Mode stage-closeout (absorbs retired `project-stage-close` + `project-spec-close` per M6 collapse).

---

## Stage MCP bundle contract

Stage opener calls [`domain-context-load`](../domain-context-load/SKILL.md) once; returned payload `{glossary_anchors, router_domains, spec_sections, invariants}` kept in Stage scope. All Sonnet pair-tail invocations within the Stage read from that payload — no re-query of `glossary_discover`, `glossary_lookup`, `router_for_task`, `spec_sections`, or `invariants_summary` inside a Stage. The 5-tool recipe (`glossary_discover → glossary_lookup → router_for_task → spec_sections → invariants_summary`) is encapsulated entirely in `domain-context-load`; callers never inline it.

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `ISSUE_ID` | User prompt OR active spec | `BUG-`/`FEAT-`/`TECH-`/`ART-`/`AUDIO-` for `backlog_issue` context |
| `CHANGED_AREAS` | Git diff inspection | C#? Fixtures? IA? MCP? Indexes? — drives gate decisions |
| `SCENARIO_ID` | Spec §7b OR user | Default `reference-flat-32x32` for Path A. `_pending_` if no test-mode gate fires |
| `SEED_CELLS` | Spec §7b OR repro | 1–3 `"x,y"` for Path B `debug_context_bundle` + `close-dev-loop` |
| `MAX_ITERATIONS` | Default 2 | Fix→verify cycles before escalation |
| `--skip-path-b` | Flag (default off) | When set: Path A compile gate runs; Path B (IDE bridge hybrid, Step 4b) is skipped; JSON verdict records `path_b: skipped_batched`. Used by `/ship-stage` chain for batched stage-boundary Path B. NOT surfaced on `/verify` (single-pass, no batching consumer). |
| `--tooling-only` | Flag (default off) | When set: Decision matrix bypassed; Steps 0, 1, 3, 4a, 4b, 5, 6 all skipped up-front; only Step 2 (Node CI-parity) + Step 7 (Verification block) run. JSON verdict records `mode: "tooling_only"` + `path_b: "skipped_not_required"`. Use ONLY when current git diff is pure tooling surface (MCP TypeScript under `tools/mcp-ia-server/`, web Next.js under `web/`, skills / agents / commands markdown under `ia/skills/` + `.claude/`, docs under `docs/` + `ia/rules/` + `ia/specs/`, scripts under `tools/scripts/`) — never when `Assets/**`, `Packages/**`, or `ProjectSettings/**` are dirty. Precondition guard: skill asserts no Unity-surface paths in `git status` before bypass; fails loud if asserted. Designed for lifecycle-refactor work (orchestrator: `ia/projects/lifecycle-refactor-master-plan.md`) and similar tooling-only umbrellas. |

---

## Stage-scoped input mode

When invoked as **Pass 2 of `/ship-stage`** (called once at Stage end, not per Task), the following contract governs the verify-loop run:

**Cumulative delta anchor:**
- `git diff {FIRST_TASK_COMMIT_PARENT}..HEAD` — where `{FIRST_TASK_COMMIT_PARENT}` is the commit SHA immediately before the first Pass 1 Task commit in the Stage.
- **EXCLUDE Stage closeout commits** — closeout runs AFTER Pass 2; at the time Pass 2 fires, no closeout commits exist yet on HEAD. The anchor is therefore naturally correct; do NOT adjust for closeout.
- Caller (`ship-stage` Step 3.1) passes the anchor SHA as context. If absent, derive via `git log --oneline` and the known Task commit messages.

**Single boot contract:**
- Path A (`unity:testmode-batch --quit-editor-first`): runs ONCE for the Stage, not per Task. One `--quit-editor-first` Editor boot + test run covers all N Tasks' cumulative delta.
- Path B (IDE bridge `enter_play_mode` + `debug_context_bundle`): runs ONCE. One `enter_play_mode` → one or more `debug_context_bundle` calls → one `exit_play_mode`.

**Input fields when called from `/ship-stage` Pass 2:**
- `ISSUE_ID`: last Task id in the Stage (used for `backlog_issue` context lookup; the Stage-level diff is the actual review surface).
- `CHANGED_AREAS`: all file paths touched across all Pass 1 Task commits (derived from `git diff --name-only` against anchor).
- `--skip-path-b`: NOT set (Pass 2 runs full Path A+B by default).

**Verification block `path_b` value:** `"ran"` (full Path B executed) — not `"skipped_batched"`.

**Failure handling:** on `verdict: "fail"` or `"escalated"`, caller (`ship-stage` Step 3.1) emits `STAGE_VERIFY_FAIL`; no automatic retry; human review required.

---

## Pre-matrix mode gate

IF `--tooling-only` flag set:

1. Run `git status --porcelain` + `git diff --name-only` against branch base — assert zero matches for regex `^(Assets|Packages|ProjectSettings)/`. If any match → emit `verdict: "fail"` with `detail: "--tooling-only flag set but Unity surface paths dirty: {paths}"` + abort; do NOT proceed to Step 2. Never silently relax the assertion.
2. Bypass Decision matrix entirely. Record `mode: "tooling_only"` in JSON header (new key). `path_b` → `"skipped_not_required"`. `skipped` list MUST enumerate Steps 0, 1, 3, 4a, 4b, 5, 6 with reason `"tooling_only_flag"`.
3. Run Step 2 (Node CI-parity `npm run validate:all`) + Step 7 (Verification block emit) only. Nothing else.
4. Step 6 fix iteration is UNREACHABLE under `--tooling-only` — if Step 2 fails, escalate with `verdict: "fail"` immediately (no Unity-bridge diff loop applies to tooling surface; fix requires human or separate `/implement` pass).

IF `--tooling-only` NOT set: Decision matrix (below) gates each step as usual.

---

## Decision matrix — which steps run

Inspect git diff + spec §7b / §8 against this table. Skip steps with **all rows N/A** and document why in the Verification block.

| Step | Run when | Skip when |
|------|----------|-----------|
| 0 — Bridge preflight | Step 4b Path B will run; OR Step 5 evidence will run | No bridge / Postgres operations needed |
| 1 — Compile gate | Any C# / Unity asset edits | IA / docs / fixture-only edits |
| 2 — Node validate:all | MCP / fixtures / IA index / glossary / spec body changes | Pure runtime C# only (rely on Step 1) |
| 3 — `verify:local` (full chain) | Pre-PR / pre-stage-close on dev machine | Per-task iteration (too slow); CI-only environment |
| 4a — Path A test-mode batch | Save / load pipeline; `GameSaveManager`; scenario JSON; `GridManager` init; sim tick; spec §7b row asks for batch | Pure UI / authoring / docs |
| 4b — Path B bridge hybrid | Spec §7b row asks for Play Mode assertion; Path A unavailable; need `debug_context_bundle` | No Play Mode evidence required; OR `--skip-path-b` flag set (batched by caller — record `path_b: skipped_batched` in JSON verdict) |
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

`npm run verify:local` (alias `verify:post-implementation`) — `validate:all` → Lockfile check → save/quit Editor → `unity:compile-check` → `db:migrate` → `db:bridge-preflight` → reopen Editor → `db:bridge-playmode-smoke`. Skip during per-task iteration (too slow); run before submitting PR or closing a stage / spec.

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

1. Write `{SCENARIO_ID}` (single line) to `tools/fixtures/scenarios/.queued-test-scenario-id` (gitignored) for Unity `TestModeCommandLineBootstrap`. Prefer `mcp__territory-ia__runtime_state` `action: write`, `patch: { "queued_test_scenario_id": "{SCENARIO_ID}" }` so harnesses see the queue without relying on flat files alone. Path-based loads use `-testScenarioPath` instead.
2. Step 0 must be green (Postgres + `agent_bridge_job`).
3. `unity_bridge_command` `kind: enter_play_mode`, `timeout_ms: 40000` → poll `get_play_mode_status` until `play_mode_ready` + `ready: true` + grid dims when `has_grid_dimensions`.
4. `unity_bridge_command` `kind: debug_context_bundle`, `timeout_ms: 40000`, `seed_cell: "x,y"` per `{SEED_CELLS}` — store `response.bundle` (`anomaly_count`, `anomalies`, `cell_export`, screenshot, console).
5. Optional: `get_console_logs`, `capture_screenshot` (`include_ui: true`) per [`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md).
6. **Scene-wiring reachability (when triggers fired per [`ia/rules/unity-scene-wiring.md`](../../rules/unity-scene-wiring.md)):** confirm the wired component is live in the scene — `unity_bridge_command` `kind: find_gameobject` with the wired parent/name, then `debug_context_bundle` should show the component active (no NPE on `Awake`, expected `[SerializeField]` values present). Absent component under a fired trigger = escalate with `gap_reason: bridge_kind_missing` OR record anomaly for Step 6 fix iteration.
7. `unity_bridge_command` `kind: exit_play_mode`, `timeout_ms: 40000`.

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
  "path_b": "ran|skipped_batched|skipped_not_required",
  "mode": "full|tooling_only",
  "verdict": "pass|fail|skipped|escalated",
  "escalation": {
    "gap_reason": "unity_api_limit|bridge_kind_missing|human_judgment_required",
    "missing_kind": "attach_component",
    "tooling_issue_id": "TECH-412",
    "detail": "short caveman phrase explaining the concrete gap"
  },
  "human_ask": "confirm in normal game (no test mode flags)"
}
```

`path_b` values: `"ran"` (Path B executed), `"skipped_batched"` (`--skip-path-b` flag set by chain caller — batched at stage end), `"skipped_not_required"` (decision matrix skipped, not batched).

**Runtime state:** After Step 3 when `npm run verify:local` runs, persist its exit code — prefer `mcp__territory-ia__runtime_state` with `action: write`, `patch: { "last_verify_exit_code": <exit> }`. Fallback (no MCP): `REPO_ROOT=$(pwd) bash tools/scripts/runtime-state-write.sh` with a one-line temp JSON file containing the patch object (uses `ia/state/.runtime-state.lock`).

After Step 0 records bridge preflight, persist — prefer `runtime_state` `patch: { "last_bridge_preflight_exit_code": <exit> }`; same bash fallback as above.

`escalation` present only when `verdict == "escalated"`. `gap_reason` enum — MUST be one of:

- `unity_api_limit` — genuine Unity / `UnityEditor` API does not expose the capability; no tooling task can close the gap. Rare. `missing_kind` + `tooling_issue_id` MAY be null; `detail` MUST cite the API surface that falls short.
- `bridge_kind_missing` — Unity API supports the operation but `unity_bridge_command` has no matching `kind`. `missing_kind` REQUIRED (e.g. `attach_component`, `assign_serialized_field`, `save_scene`). `tooling_issue_id` REQUIRED — cite the open BACKLOG issue tracking the bridge-kind expansion (TECH-412 landed the initial 20 mutation kinds; file a new TECH if a genuinely missing kind is still needed). Never file a new human-review ask without also citing or filing that issue.
- `human_judgment_required` — true human-only gate (design review, visual QA, cross-feature judgment call). `detail` MUST name the judgment class.

Agent MUST NOT escalate as `human_judgment_required` when a missing bridge kind could close the loop. Before escalating, cross-check the current kind enum in `Assets/Scripts/Editor/AgentBridgeCommandRunner.cs` (incl. `AgentBridgeCommandRunner.Mutations.cs`) against the operation needed — TECH-412 landed 20 mutation kinds; if a kind is still missing, escalate as `bridge_kind_missing` and cite an open successor tooling issue as `tooling_issue_id`.

Markdown summary (caveman): verdict, paths run (A / B / both / none), artifact paths, anomalies cleared, iterations consumed, escalation note (if any; include `gap_reason` + `missing_kind` / `tooling_issue_id` when applicable), next step (human QA / next task / stage close / umbrella close / file new bridge kind).

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
- IF verdict is `escalated` → `gap_reason` field REQUIRED. Use `bridge_kind_missing` (not `human_judgment_required`) whenever a missing `unity_bridge_command` kind could close the loop — cite the exact missing kind + an open tooling issue id (TECH-412 landed the initial 20 mutation kinds; file a new TECH for genuinely missing kinds). Closed-loop agent verify is the default; human-in-loop is the exception reserved for true Unity API limits or design/visual judgment.
- IF escalating with `gap_reason: bridge_kind_missing` → verify the kind is actually absent by reading `Assets/Scripts/Editor/AgentBridgeCommandRunner.cs` switch branches; do NOT claim a gap that already has a kind implemented.
- IF `--tooling-only` flag set AND `git status --porcelain` matches `^(Assets|Packages|ProjectSettings)/` → REFUSE fast-path; emit `verdict: "fail"` citing dirty Unity paths. Never silently drop the assertion to let the bypass through.
- IF `--tooling-only` flag set → Steps 0, 1, 3, 4a, 4b, 5, 6 MUST appear in `skipped` array with reason `"tooling_only_flag"`; running any of them violates the flag contract.
- Do NOT pass `--tooling-only` on mixed diffs (tooling + Unity). Split the commit or run full `/verify-loop`; the flag is for surface-pure refactors only.

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
  TOOLING_ONLY: {default false — set true only when diff is pure tooling surface (tools/, web/, ia/skills, ia/rules, ia/specs markdown, docs/, .claude/) and no Assets|Packages|ProjectSettings paths are dirty}

IF TOOLING_ONLY true: apply Pre-matrix mode gate (§Pre-matrix mode gate) — assert clean Unity surface; skip Steps 0, 1, 3, 4a, 4b, 5, 6; run Step 2 + Step 7 only; verdict fail on asserted fail or Step 2 red.
ELSE: apply Decision matrix to gate Steps 0–6 as usual.
Emit Verification block per docs/agent-led-verification-policy.md (JSON header + caveman summary). Stop on first failure; bounded repair only — escalate rather than loop.
```

---

## Handoff (required shape)

- **Verdict:** `pass` / `fail` / `skipped (with reason)` / `escalated`.
- **Paths run:** A / B / both / none (with reason if none).
- **Artifacts:** newest `agent-testmode-batch-*.json`; bridge `bundle` + screenshot paths; `validate:all` log if failure.
- **Iterations:** consumed / max (e.g. `1 / 2`).
- **Human ask:** confirm in normal game (no test-mode flags); approve PR / next stage / umbrella close.

---

## Changelog

### 2026-04-19 — Out-of-scope test-failure attribution worked correctly (F10 positive signal)

**Status:** observed (no fix required)

**Symptom:**
M8 dry-run verify surfaced 10× `BlipGoldenFixtureTests` + 3× `TreasuryFloorClampServiceTests` failures during lifecycle-refactor Stage 8 ship. Agent correctly attributed Blip failures → `ia/projects/blip-master-plan.md`; Zone-S failures → `ia/projects/zone-s-economy-master-plan.md`; escalated per T8.4 bounded-fix rule instead of attempting remediation.

**Root cause:**
Positive signal — issue-attribution discipline (`ia/rules/agent-tooling-hints.md` — verify id open in `BACKLOG.md` before naming owner) held under load. Bounded-fix escalation rule fired correctly.

**Fix:**
none required.

**Rollout row:** m8-retrospective

**Tracker aggregator:** [`ia/projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator`](../../projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator)

---
