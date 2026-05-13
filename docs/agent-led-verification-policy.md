# Agent-led verification policy

> **IA placement:** This document is part of the **Information Architecture** stack. Overview and layer diagram: [`docs/information-architecture-overview.md`](information-architecture-overview.md). Day-to-day workflow: [`AGENTS.md`](../AGENTS.md). **Cursor rule (always-on):** [`ia/rules/agent-verification-directives.md`](../ia/rules/agent-verification-directives.md).

## Purpose

During **implementation** and **fix** work with Cursor agents on this repository, the **Unity Editor**, **Play Mode**, and **save-shaped scenarios** are **test surfaces** — not production. The **owner** treats agent-driven runs as disposable: closing or restarting the Editor to unblock **Path A** (**Agent test mode batch**) or **Path B** (**IDE agent bridge**) is acceptable when pursuing a closed **implement → verify** loop. **Human normal-game QA** remains the final gate per [`AGENTS.md`](../AGENTS.md).

## Integration expectations

- **Prefer proving over assuming:** When **Postgres** and **Unity** are available, agents should **attempt** **glossary** **IDE agent bridge** commands after **`npm run db:bridge-preflight`**, not decline by default because of wall-clock cost.
- **Environment:** Use full agent capabilities (terminal, MCP, network) as allowed by the host. If a sandbox blocks a step, **retry** with broader permissions or report the **host** error — do not treat “might be slow” as a skip reason.
- **Timeouts:** **`unity_bridge_command`** supports **`timeout_ms`** up to **120000** (120 seconds; `UNITY_BRIDGE_TIMEOUT_MS_MAX`). Use **`40000`** for the **initial** agent-led call. On timeout, follow the **timeout escalation protocol** below. Waiting for **Unity** or **Play Mode** is normal (same idea as **E2E** tests).
- **Editor launch:** If the Unity Editor is not running, agents should run **`npm run unity:ensure-editor`** (macOS; exit 0 = ready, exit 2 = not macOS, exit 3 = binary not found) **before** concluding that the human must open Unity. The script launches the Editor on `REPO_ROOT` and waits up to 90 s for the lockfile.
- **Path A — project lock:** **`npm run unity:testmode-batch`** starts a **second** Unity process. If the **Unity Editor** already has **`REPO_ROOT`** open, batchmode aborts (*"another Unity instance is running"*, often exit **134**). **Before Path A**, agents **must** release the lock: preferred one-liner **`npm run unity:testmode-batch -- --quit-editor-first --scenario-id reference-flat-32x32`** (runs **`tools/scripts/unity-quit-project.sh`** first), or quit the Editor manually / run **`tools/scripts/unity-quit-project.sh`** then invoke batch without **`--quit-editor-first`**. **Both Path A and Path B in one session:** run **Path A** first (with **`--quit-editor-first`** when the Editor might be open), then **`npm run unity:ensure-editor`** (macOS) so **Path B** has an Editor on **`REPO_ROOT`** again.

## Verification block (required in agent completion messages)

When reporting **Verification** after substantive implementation (especially when **§7b** / **Load pipeline** / **test mode** applies), include **all** of the following that were run:

| Check | Report |
|-------|--------|
| **Node / IA** | `npm run validate:all` — exit code (and note if skipped with reason). |
| **Unity compile** | `npm run unity:compile-check` when **`Assets/`** **C#** changed — exit code; or **N/A** with reason. |
| **NUnit EditMode tests** | `npm run unity:test-editmode` — run immediately after `unity:compile-check` in the `verify:local` / Path A neighborhood. Script: `tools/scripts/unity-run-tests.sh --platform editmode --quit-editor-first`. Stdout contract: `Passed: N  Failed: M  Errors: K  Skipped: S` + one `FAILED: <fullname>` line per failing test. XML result written to `tools/reports/unity-tests/editmode-results.xml` (gitignored). XML parsed by `tools/scripts/parse-nunit-xml.mjs` (Node — no `xmllint` dep). Exit 0 = all pass; exit 1 = any failure or error. `validate:all` does **not** include NUnit tests — it stays Unity-free for CI. Deferred Tier B: bridge-based `run_nunit_tests` command via `UnityEditor.TestTools.TestRunner.Api.TestRunnerApi` for Editor-open dev-machine path (batchmode requires Editor closed; bridge handler would enqueue job, return `{passed, failed, errors, skipped, failures}`) — file separate issue when `verify-loop` autonomy on dev machines with open Editor is needed. |
| **Path A — Agent test mode batch** | `npm run unity:testmode-batch` — exit code; path to newest **`tools/reports/agent-testmode-batch-*.json`** and **`ok` / `exit_code`** (report **`schema_version`** **2** may include **`city_stats`** and golden fields). Use **`--quit-editor-first`** when an Editor might hold **`REPO_ROOT`** (see **Path A — project lock** above). Optional **`--golden-path`** (forwarded **`-testGoldenPath`**) asserts integer **CityStats** fields against a committed JSON — mismatch → exit **8**. Example: **`npm run unity:testmode-batch -- --quit-editor-first --scenario-id reference-flat-32x32`**. Full matrix, **CI** tick bounds, golden regeneration: [`tools/fixtures/scenarios/README.md`](../tools/fixtures/scenarios/README.md); stage **31c** trace: [`projects/TECH-31c-verification-pipeline.md`](../projects/TECH-31c-verification-pipeline.md). |
| **Path B — IDE agent bridge** | After **`db:bridge-preflight`**: acquire play_mode lease via **`unity_bridge_lease(acquire)`** → at least **`get_play_mode_status`** or full **`enter_play_mode`** → **`debug_context_bundle`** (optional) → **`exit_play_mode`** → **`unity_bridge_lease(release)`** with **`timeout_ms`:** **`40000`** (initial; follow **timeout escalation protocol** on timeout) — **`ok`**, **`error`**, or **`timeout`** plus **`command_id`** if present. If lease returns **`lease_unavailable`**, retry every 60 s up to 10 min then report **`play_mode_lease: skipped_busy`**. |

If **Path B** was not run, state **why** (e.g. no Editor, preflight non-zero) — do not omit the row.

**Skills:** [`ia/skills/agent-test-mode-verify/SKILL.md`](../ia/skills/agent-test-mode-verify/SKILL.md), [`ia/skills/ide-bridge-evidence/SKILL.md`](../ia/skills/ide-bridge-evidence/SKILL.md), [`ia/skills/close-dev-loop/SKILL.md`](../ia/skills/close-dev-loop/SKILL.md).

## Close Dev Loop vs bridge export sugar

- **`close-dev-loop`** ([`ia/skills/close-dev-loop/SKILL.md`](../ia/skills/close-dev-loop/SKILL.md)) — full before/after loop using **`debug_context_bundle`** in Play Mode (Moore export + optional Game view screenshot + console + **`anomaly_count`**). Use for visual/terrain regressions and acceptance-style evidence when the rich bundle is worth the token and Play Mode cost.
- **`unity_export_cell_chunk`** / **`unity_export_sorting_debug`** ([`docs/mcp-ia-server.md`](mcp-ia-server.md) — **Bridge export sugar tools**) — thin MCP wrappers around **`export_cell_chunk`** / **`export_sorting_debug`** only: same **`agent_bridge_job`** queue and JSON response as **`unity_bridge_command`**, less call boilerplate. Use for bounded **Editor** JSON exports (e.g. sorting math checks with **`spec_section`** **geo** §7 via [`ia/skills/debug-sorting-order/SKILL.md`](../ia/skills/debug-sorting-order/SKILL.md)).
- **Registry staging** — older one-shot CLI (`npm run db:bridge-agent-context`, etc.) still hits the same bridge path; it does not replace **`debug_context_bundle`** for layered evidence. Prefer the skills above instead of duplicating policy text in chat.

## Multi-agent concurrency (Play Mode lease)

When multiple agent sessions share one Unity Editor and Postgres instance, use **`unity_bridge_lease`** (migration `0010_agent_bridge_lease.sql`) to serialize Play Mode access:

1. **Before `enter_play_mode`** — call `unity_bridge_lease(action: acquire, agent_id: "{ISSUE_ID}", kind: play_mode)`. Store the returned `lease_id`.
2. **After `exit_play_mode`** — call `unity_bridge_lease(action: release, lease_id: "{lease_id}")`.
3. **On `lease_unavailable`** — wait 60 s, retry. After 10 min total, skip Play Mode evidence and report `play_mode_lease: skipped_busy` in the Verification block.
4. **TTL safety** — leases expire after 8 min. A crashed agent's lease self-clears; call `unity_bridge_lease(action: status)` to confirm before waiting.

Non-Play-Mode commands (`export_agent_context`, `get_compilation_status`, `get_console_logs`, `economy_balance_snapshot`, `prefab_manifest`) do **not** require a lease — the Postgres FIFO queue serializes them naturally. `npm run unity:compile-check` (batchmode) is fully independent and never requires a lease.

## Escalation taxonomy — `gap_reason` for `verdict: escalated`

Closed-loop agent verify is the default. When an agent cannot close a verification gap, the Verification block MUST include an `escalation` object with a typed `gap_reason`:

| `gap_reason` | Meaning | Required fields | Next action |
|--------------|---------|-----------------|-------------|
| `unity_api_limit` | Genuine Unity / `UnityEditor` API gap — no tooling task can close the loop. Rare. | `detail` (which API surface falls short) | Human handles; note limit in issue / spec. |
| `bridge_kind_missing` | Unity API supports the op but `unity_bridge_command` has no matching `kind`. | `missing_kind` (e.g. `refresh_asset_database`), `tooling_issue_id` (open BACKLOG id tracking the bridge expansion), `detail` | File / cite tooling issue; human performs one-shot Editor action to unblock while the bridge kind is implemented. |
| `human_judgment_required` | True human-only gate — design review, visual QA, cross-feature tradeoff. | `detail` (judgment class) | Human reviews; agent resumes after sign-off. |

**Rules:**

1. Agents MUST NOT escalate as `human_judgment_required` when a missing bridge kind could close the loop. Before picking a `gap_reason`, cross-check the current kind enum in [`Assets/Scripts/Editor/AgentBridgeCommandRunner.cs`](../Assets/Scripts/Editor/AgentBridgeCommandRunner.cs). If a mutation kind is missing, `gap_reason = bridge_kind_missing` with `missing_kind` + `tooling_issue_id`. **TECH-412 landed** 20 mutation kinds (Edit Mode only) covering component, GameObject, scene, prefab, and asset lifecycle plus a `execute_menu_item` catch-all — before escalating, verify the needed kind is not already in `AgentBridgeCommandRunner.Mutations.cs` (full list: `attach_component`, `remove_component`, `assign_serialized_field`, `create_gameobject`, `delete_gameobject`, `find_gameobject`, `set_transform`, `set_gameobject_active`, `set_gameobject_parent`, `save_scene`, `open_scene`, `new_scene`, `instantiate_prefab`, `apply_prefab_overrides`, `create_scriptable_object`, `modify_scriptable_object`, `refresh_asset_database`, `move_asset`, `delete_asset`, `execute_menu_item`).
2. `bridge_kind_missing` escalations MUST cite an open BACKLOG issue (or file one) so the gap is tracked. File a new TECH when a genuinely missing kind is identified; TECH-412 is now closed (landed).
3. Human-in-loop messages MUST name the concrete reason. Do NOT write generic "Human review required" — write "Escalated: `bridge_kind_missing` — `<missing_kind>` — tracked in <TECH-id>" (or equivalent).
4. Full JSON shape: [`ia/skills/verify-loop/SKILL.md`](../ia/skills/verify-loop/SKILL.md) § Step 7.

## Timeout escalation protocol

When a **`unity_bridge_command`** call returns **`timeout`**, follow this ordered recovery before concluding "human needed":

1. **First call** — use **`timeout_ms`:** **`40000`** (40 s, the recommended agent-led default).
2. **On timeout** — run **`npm run unity:ensure-editor`** (exit 0 = Editor running or just launched; exit 2 = not macOS; exit 3 = Unity binary not found). On exit 0, proceed to step 3. On non-zero, report the exit code and escalate to the human.
3. **Retry** — repeat the bridge command with **`timeout_ms`:** **`60000`** (60 s). This accommodates Editor startup + domain reload + `AgentBridgeCommandRunner` initialization.
4. **On second timeout** — run **`npm run db:bridge-preflight`** and check Console logs (**`get_console_logs`** if the Editor responds). Report findings and escalate to the human.

The ceiling is **120 s** (`UNITY_BRIDGE_TIMEOUT_MS_MAX`); the escalation protocol intentionally stops at **60 s** to avoid silent long waits. Do not retry more than once.

## territory-ia MCP and **`timeout_ms`**

The **`unity_bridge_command`** / **`unity_compile`** **120 s** ceiling is enforced in [`tools/mcp-ia-server/src/tools/unity-bridge-command.ts`](../tools/mcp-ia-server/src/tools/unity-bridge-command.ts) (`UNITY_BRIDGE_TIMEOUT_MS_MAX`). After pulling a change that adjusts this cap, **restart the territory-ia MCP server** (or reload the Cursor window) so the host picks up the new tool schema — otherwise the client may still validate **`timeout_ms`** against the old maximum.

## validate:all sub-chain — IA gate validators

`npm run validate:all` runs a sequenced chain of sub-validators. The table below documents each IA-methodology gate (non-Unity, non-web validators) in chain order.

| Validator | Purpose | Exit codes | Notes |
|-----------|---------|------------|-------|
| `validate:master-plan-status` | Header `Status` ↔ Stage `Status` ↔ Task row status ↔ `ia_tasks` row consistency (R1–R6). | 0 green / 1 violation / 2 DB error | Always runs first. |
| `validate:plan-prototype-first` | Asserts every non-grandfathered master plan Stage 1.0/1.1 carries a complete §Tracer Slice block (5 fields) and every Stage 2+ carries a non-empty, unique §Visibility Delta line. | 0 green / 1 violation / 2 DB error | Grandfathers plans created before 2026-05-03. |
| `validate:plan-red-stage` | CI red on any non-closed master plan Stage that lacks a complete §Red-Stage Proof block (4 fields: `red_test_anchor`, `target_kind`, `proof_artifact_id`, `proof_status`). Skip-clause: `target_kind=design_only` Stages may use `proof_artifact_id=n/a`. | 0 green / 1 ≥1 hard violation / 2 DB error | Runs after `validate:plan-prototype-first`, before `validate:arch-coherence`. Grandfathers plans created before 2026-05-03. |
| `validate:arch-coherence` | Arch-surface drift scan — every Stage `arch_surfaces` slug must exist in `arch_surfaces` table (Invariant #12). | 0 green / non-zero violation | — |

---

## Validator bands — fast vs deferred

Two bands exist to balance pre-commit speed with full coverage:

**`validate:fast` band** (runs pre-commit; stays under typical ~10 s budget):

| Validator | What it checks |
|-----------|----------------|
| `validate:claude-imports` | Every `@`-import in `CLAUDE.md` resolves + line budget |
| `validate:frontmatter` | SKILL.md frontmatter schema (exits 0 on warnings — gate on stdout, not exit code) |
| `validate:cache-block-sizing` | Subagent preamble cache-block byte floors |
| `validate:skill-drift` | Generated shadow files (`.claude/agents/`, `.claude/commands/`, `.cursor/rules/`) match SKILL.md sources |
| `validate:retired-skill-refs` | Scans `ia/skills`, `.claude/agents`, `.claude/commands`, `.cursor/rules`, `docs` for retired-slug hits. Soft-fail (exit 0 + warn) until `2026-05-12`; hard-fail (exit 1) after. |
| `validate:plan-digest-coverage` | Non-done tasks with empty body → exit 1; seeded tasks (backfilled marker) → exit 0 (warn) |
| `validate:seeded-task-stale` | Backfilled tasks older than 30 days → warn (exit 0 always) |

Fast band runs as part of `validate:all:readonly` (read-only; no DB writes). Run manually: `npm run validate:retired-skill-refs`, `npm run validate:plan-digest-coverage`, etc.

**`test:ia` deferred band** (runs in `/ship-final` Pass B — heavier surfaces):

| Surface | What it runs |
|---------|-------------|
| Full Jest `test:ia` suite | `ia/skills/**/__tests__/**/*.spec.ts` + `tools/scripts/__tests__/**/*.spec.ts` |
| `compute-lib:build` | TypeScript compile check on compute-lib bundle |
| Integration tests | DB migration smoke + bridge preflight |

Rationale: fast band runs pre-commit to catch drift early; heavy band runs at plan-close time (ship-final Pass B) when the full DB state is known and the Editor is available.

---

## UI Visual Regression

Visual regression baseline sweep for published panels (ui-visual-regression plan).

Sweep orchestrator: `tools/scripts/sweep-visual-baselines.mjs`. Enumerates every
published panel from `Assets/UI/Snapshots/panels.json`, groups by archetype, fires
one approval prompt per group (approve_all / approve_subset / skip / refresh),
then promotes candidate PNGs to `ia_visual_baseline` rows.

Full workflow: run `unity:bake-ui --capture-baselines --panels=all` first to emit
candidate PNGs under `Library/UiBaselines/_candidate/`, then run the orchestrator
to promote approved candidates. Audit trail written to `Library/UiBaselines/_sweep-{ts}.jsonl`.

Region mask sidecars for live-state panels (hud-bar, budget-panel, time-strip)
stored at `Assets/UI/VisualBaselines/{slug}.masks.json`; `visualDiffRepo.run`
loads sidecars automatically and zeroes masked pixels on both images before diff.

Per-panel tolerance_pct overrides recorded on `ia_visual_baseline` rows where
defaults diverge (e.g. budget-panel = 0.01 vs default 0.005).

Full §UI Visual Regression workflow documentation (CI strict-mode gate, JSONL→DB
migration, complete operator runbook) expands at Task 3.0.3.

---

## Cursor Memory (optional)

Paste the following into **Cursor → Memory** if you want the same policy across projects or sessions without opening this repo:

- Territory Developer: During agent implementation, Unity is a **test** environment; **attempt** **Agent test mode batch** and **IDE agent bridge** verification; for **Path A**, release the **project lock** first (**`npm run unity:testmode-batch -- --quit-editor-first …`** or quit Editor), then **`unity:ensure-editor`** before **Path B** if needed; use **`timeout_ms` 40000** initial for bridge commands, follow **timeout escalation protocol** on timeout (`npm run unity:ensure-editor` → retry 60 s); report **Verification** with **validate:all**, **compile-check** if C# changed, **batch JSON result**, and **bridge** outcome. **IA** overview: `docs/information-architecture-overview.md`; policy: `docs/agent-led-verification-policy.md`.
