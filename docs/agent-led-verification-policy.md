# Agent-led verification policy

> **IA placement:** Doc part of **Information Architecture** stack. Overview + layer diagram: [`docs/information-architecture-overview.md`](information-architecture-overview.md). Day-to-day workflow: [`AGENTS.md`](../AGENTS.md). **Cursor rule (always-on):** [`ia/rules/agent-verification-directives.md`](../ia/rules/agent-verification-directives.md).

## Purpose

During **implementation** + **fix** work with Cursor agents on repo, **Unity Editor**, **Play Mode**, **save-shaped scenarios** = **test surfaces** — not production. **Owner** treats agent-driven runs as disposable: closing / restarting Editor to unblock **Path A** (**Agent test mode batch**) or **Path B** (**IDE agent bridge**) acceptable when pursuing closed **implement → verify** loop. **Human normal-game QA** remains final gate per [`AGENTS.md`](../AGENTS.md).

## Integration expectations

- **Prefer proving over assuming:** **Postgres** + **Unity** available → agents must **attempt** **glossary** **IDE agent bridge** commands after **`npm run db:bridge-preflight`**, not decline by default for wall-clock cost.
- **Environment:** Use full agent capabilities (terminal, MCP, network) as allowed by host. Sandbox blocks step → **retry** with broader permissions or report **host** error — do not treat "might be slow" as skip reason.
- **Timeouts:** **`unity_bridge_command`** supports **`timeout_ms`** up to **120000** (120 seconds; `UNITY_BRIDGE_TIMEOUT_MS_MAX`). Use **`40000`** for **initial** agent-led call. On timeout, follow **timeout escalation protocol** below. Waiting for **Unity** or **Play Mode** normal (same idea as **E2E** tests).
- **Editor launch:** Unity Editor not running → agents must run **`npm run unity:ensure-editor`** (macOS; exit 0 = ready, exit 2 = not macOS, exit 3 = binary not found) **before** concluding human must open Unity. Script launches Editor on `REPO_ROOT` + waits up to 90 s for lockfile.
- **Path A — project lock:** **`npm run unity:testmode-batch`** starts **second** Unity process. **Unity Editor** already has **`REPO_ROOT`** open → batchmode aborts (*"another Unity instance is running"*, often exit **134**). **Before Path A**, agents **must** release lock: preferred one-liner **`npm run unity:testmode-batch -- --quit-editor-first --scenario-id reference-flat-32x32`** (runs **`tools/scripts/unity-quit-project.sh`** first), or quit Editor manually / run **`tools/scripts/unity-quit-project.sh`** then invoke batch without **`--quit-editor-first`**. **Both Path A + Path B in one session:** run **Path A** first (with **`--quit-editor-first`** when Editor might be open), then **`npm run unity:ensure-editor`** (macOS) so **Path B** has Editor on **`REPO_ROOT`** again.

## Stop hook enforcement

The **Verification block** requirement above is enforced by a Claude Code **Stop hook** — [`tools/scripts/claude-hooks/stop-verification-required.sh`](../tools/scripts/claude-hooks/stop-verification-required.sh). The hook runs at session end, scans the final assistant message for a fenced `Verification` block, and **exits 2** (denying session completion) when the block is absent after substantive implementation. Exit 0 = block present or session is non-implementation (doc-only, query). Hook registration: `.claude/settings.json` under `hooks.Stop[]`. This hook is the **enforcement layer** for the policy prose above — editing one without the other creates drift.

## Verification block (required in agent completion messages)

When reporting **Verification** after substantive implementation (especially when **§7b** / **Load pipeline** / **test mode** applies), include **all** of following that were run:

| Check | Report |
|-------|--------|
| **Node / IA** | `npm run validate:all` — exit code (and note if skipped with reason). |
| **Unity compile** | `npm run unity:compile-check` when **`Assets/`** **C#** changed — exit code; or **N/A** with reason. |
| **NUnit EditMode tests** | `npm run unity:test-editmode` — run immediately after `unity:compile-check` in the `verify:local` / Path A neighborhood. Script: `tools/scripts/unity-run-tests.sh --platform editmode --quit-editor-first`. Stdout contract: `Passed: N  Failed: M  Errors: K  Skipped: S` + one `FAILED: <fullname>` line per failing test. XML result written to `tools/reports/unity-tests/editmode-results.xml` (gitignored). XML parsed by `tools/scripts/parse-nunit-xml.mjs` (Node — no `xmllint` dep). Exit 0 = all pass; exit 1 = any failure or error. `validate:all` does **not** include NUnit tests — it stays Unity-free for CI. Deferred Tier B: bridge-based `run_nunit_tests` command via `UnityEditor.TestTools.TestRunner.Api.TestRunnerApi` for Editor-open dev-machine path (batchmode requires Editor closed; bridge handler would enqueue job, return `{passed, failed, errors, skipped, failures}`) — file separate issue when `verify-loop` autonomy on dev machines with open Editor is needed. |
| **Path A — Agent test mode batch** | `npm run unity:testmode-batch` — exit code; path to newest **`tools/reports/agent-testmode-batch-*.json`** and **`ok` / `exit_code`** (report **`schema_version`** **2** may include **`city_stats`** and golden fields). Use **`--quit-editor-first`** when an Editor might hold **`REPO_ROOT`** (see **Path A — project lock** above). Optional **`--golden-path`** (forwarded **`-testGoldenPath`**) asserts integer **CityStats** fields against a committed JSON — mismatch → exit **8**. Example: **`npm run unity:testmode-batch -- --quit-editor-first --scenario-id reference-flat-32x32`**. Full matrix, **CI** tick bounds, golden regeneration: [`tools/fixtures/scenarios/README.md`](../tools/fixtures/scenarios/README.md); stage **31c** trace: [`projects/TECH-31c-verification-pipeline.md`](../projects/TECH-31c-verification-pipeline.md). |
| **Path B — IDE agent bridge** | After **`db:bridge-preflight`**: acquire play_mode lease via **`unity_bridge_lease(acquire)`** → at least **`get_play_mode_status`** or full **`enter_play_mode`** → **`debug_context_bundle`** (optional) → **`exit_play_mode`** → **`unity_bridge_lease(release)`** with **`timeout_ms`:** **`40000`** (initial; follow **timeout escalation protocol** on timeout) — **`ok`**, **`error`**, or **`timeout`** plus **`command_id`** if present. If lease returns **`lease_unavailable`**, retry every 60 s up to 10 min then report **`play_mode_lease: skipped_busy`**. |

**Path B** not run → state **why** (e.g. no Editor, preflight non-zero) — do not omit row.

**Skills:** [`ia/skills/agent-test-mode-verify/SKILL.md`](../ia/skills/agent-test-mode-verify/SKILL.md), [`ia/skills/ide-bridge-evidence/SKILL.md`](../ia/skills/ide-bridge-evidence/SKILL.md), [`ia/skills/close-dev-loop/SKILL.md`](../ia/skills/close-dev-loop/SKILL.md).

## Close Dev Loop vs bridge export sugar

- **`close-dev-loop`** ([`ia/skills/close-dev-loop/SKILL.md`](../ia/skills/close-dev-loop/SKILL.md)) — full before/after loop using **`debug_context_bundle`** in Play Mode (Moore export + optional Game view screenshot + console + **`anomaly_count`**). Use for visual/terrain regressions + acceptance-style evidence when rich bundle worth token + Play Mode cost.
- **`unity_export_cell_chunk`** / **`unity_export_sorting_debug`** ([`docs/mcp-ia-server.md`](mcp-ia-server.md) — **Bridge export sugar tools**) — thin MCP wrappers around **`export_cell_chunk`** / **`export_sorting_debug`** only: same **`agent_bridge_job`** queue + JSON response as **`unity_bridge_command`**, less call boilerplate. Use for bounded **Editor** JSON exports (e.g. sorting math checks with **`spec_section`** **geo** §7 via [`ia/skills/debug-sorting-order/SKILL.md`](../ia/skills/debug-sorting-order/SKILL.md)).
- **Registry staging** — older one-shot CLI (`npm run db:bridge-agent-context`, etc.) still hits same bridge path; does not replace **`debug_context_bundle`** for layered evidence. Prefer skills above instead of duplicating policy text in chat.

## Multi-agent concurrency (Play Mode lease)

Multiple agent sessions share one Unity Editor + Postgres instance → use **`unity_bridge_lease`** (migration `0010_agent_bridge_lease.sql`) to serialize Play Mode access:

1. **Before `enter_play_mode`** — call `unity_bridge_lease(action: acquire, agent_id: "{ISSUE_ID}", kind: play_mode)`. Store returned `lease_id`.
2. **After `exit_play_mode`** — call `unity_bridge_lease(action: release, lease_id: "{lease_id}")`.
3. **On `lease_unavailable`** — wait 60 s, retry. After 10 min total, skip Play Mode evidence + report `play_mode_lease: skipped_busy` in Verification block.
4. **TTL safety** — leases expire after 8 min. Crashed agent's lease self-clears; call `unity_bridge_lease(action: status)` to confirm before waiting.

Non-Play-Mode commands (`export_agent_context`, `get_compilation_status`, `get_console_logs`, `economy_balance_snapshot`, `prefab_manifest`) do **not** require lease — Postgres FIFO queue serializes naturally. `npm run unity:compile-check` (batchmode) fully independent + never requires lease.

## Escalation taxonomy — `gap_reason` for `verdict: escalated`

Closed-loop agent verify = default. Agent cannot close verification gap → Verification block MUST include `escalation` object with typed `gap_reason`:

| `gap_reason` | Meaning | Required fields | Next action |
|--------------|---------|-----------------|-------------|
| `unity_api_limit` | Genuine Unity / `UnityEditor` API gap — no tooling task can close the loop. Rare. | `detail` (which API surface falls short) | Human handles; note limit in issue / spec. |
| `bridge_kind_missing` | Unity API supports the op but `unity_bridge_command` has no matching `kind`. | `missing_kind` (e.g. `refresh_asset_database`), `tooling_issue_id` (open BACKLOG id tracking the bridge expansion), `detail` | File / cite tooling issue; human performs one-shot Editor action to unblock while the bridge kind is implemented. |
| `human_judgment_required` | True human-only gate — design review, visual QA, cross-feature tradeoff. | `detail` (judgment class) | Human reviews; agent resumes after sign-off. |

**Rules:**

1. Agents MUST NOT escalate as `human_judgment_required` when missing bridge kind could close loop. Before picking `gap_reason`, cross-check current kind enum in [`Assets/Scripts/Editor/AgentBridgeCommandRunner.cs`](../Assets/Scripts/Editor/AgentBridgeCommandRunner.cs). Mutation kind missing → `gap_reason = bridge_kind_missing` with `missing_kind` + `tooling_issue_id`. **TECH-412 landed** 20 mutation kinds (Edit Mode only) covering component, GameObject, scene, prefab, asset lifecycle plus `execute_menu_item` catch-all — before escalating, verify needed kind not already in `AgentBridgeCommandRunner.Mutations.cs` (full list: `attach_component`, `remove_component`, `assign_serialized_field`, `create_gameobject`, `delete_gameobject`, `find_gameobject`, `set_transform`, `set_gameobject_active`, `set_gameobject_parent`, `save_scene`, `open_scene`, `new_scene`, `instantiate_prefab`, `apply_prefab_overrides`, `create_scriptable_object`, `modify_scriptable_object`, `refresh_asset_database`, `move_asset`, `delete_asset`, `execute_menu_item`).
2. `bridge_kind_missing` escalations MUST cite open BACKLOG issue (or file one) so gap tracked. File new TECH when genuinely missing kind identified; TECH-412 now closed (landed).
3. Human-in-loop messages MUST name concrete reason. Do NOT write generic "Human review required" — write "Escalated: `bridge_kind_missing` — `<missing_kind>` — tracked in <TECH-id>" (or equivalent).
4. Full JSON shape: [`ia/skills/verify-loop/SKILL.md`](../ia/skills/verify-loop/SKILL.md) § Step 7.

## Timeout escalation protocol

**`unity_bridge_command`** call returns **`timeout`** → follow ordered recovery before concluding "human needed":

1. **First call** — use **`timeout_ms`:** **`40000`** (40 s, recommended agent-led default).
2. **On timeout** — run **`npm run unity:ensure-editor`** (exit 0 = Editor running or just launched; exit 2 = not macOS; exit 3 = Unity binary not found). On exit 0, proceed to step 3. On non-zero, report exit code + escalate to human.
3. **Retry** — repeat bridge command with **`timeout_ms`:** **`60000`** (60 s). Accommodates Editor startup + domain reload + `AgentBridgeCommandRunner` initialization.
4. **On second timeout** — run **`npm run db:bridge-preflight`** + check Console logs (**`get_console_logs`** if Editor responds). Report findings + escalate to human.

Ceiling = **120 s** (`UNITY_BRIDGE_TIMEOUT_MS_MAX`); escalation protocol intentionally stops at **60 s** to avoid silent long waits. Do not retry more than once.

## territory-ia MCP + **`timeout_ms`**

**`unity_bridge_command`** / **`unity_compile`** **120 s** ceiling enforced in [`tools/mcp-ia-server/src/tools/unity-bridge-command.ts`](../tools/mcp-ia-server/src/tools/unity-bridge-command.ts) (`UNITY_BRIDGE_TIMEOUT_MS_MAX`). After pulling change adjusting cap, **restart territory-ia MCP server** (or reload Cursor window) so host picks up new tool schema — otherwise client may still validate **`timeout_ms`** against old maximum.

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

### 1 — Capture loop

Steps in order:

1. **Bake candidates** — `npm run unity:bake-ui` emits candidate PNGs under
   `Library/UiBaselines/_candidate/{slug}.png`.
2. **Diff against active baseline** — `ui_visual_baseline_get(panel_slug)` returns the
   active `ia_visual_baseline` row; `visualDiffRepo.run(baseline, candidate)` computes
   `diff_pct` respecting region masks and per-panel tolerance.
3. **Human gate** — when diff_pct exceeds tolerance or no baseline exists, agent emits
   an `AskUserQuestion` poll:

   > Baseline for **{panel_slug}** differs by {diff_pct}%.
   > Options: `approve` / `reject` / `refresh` / `skip`

4. **Promote or reject** — `approve` → `ui_visual_baseline_record(panel_slug, candidate_sha)`
   writes new `ia_visual_baseline` row (status=active, supersedes_id=prev); `reject` →
   no write; `skip` → deferred; `refresh` → see §4 below.
5. **Audit trail** — sweep writes to `Library/UiBaselines/_sweep-{ts}.jsonl`.

Sweep orchestrator: `tools/scripts/sweep-visual-baselines.mjs`. Enumerates every
published panel from `Assets/UI/Snapshots/panels.json`, groups by archetype, fires
one approval prompt per group (approve_all / approve_subset / skip / refresh),
then promotes candidate PNGs to `ia_visual_baseline` rows.

### 2 — Region mask sidecar authoring

Live-state panels (hud-bar, budget-panel, time-strip) carry sidecar mask files at
`Assets/UI/VisualBaselines/{slug}.masks.json`.

Schema (each entry in the `masks` array):

```json
{
  "label": "budget-counter",
  "x": 120, "y": 8,
  "width": 80, "height": 24
}
```

`visualDiffRepo.run` loads sidecars automatically and zeroes masked pixels on both
baseline and candidate before diff. Masks survive baseline refresh — they are authored
once per panel, not per baseline version. Add masks for any region that changes on each
frame (counters, timers, animated icons).

### 3 — Per-panel tolerance_pct override semantics

Default tolerance: `0.005` (0.5% of total pixels). Panels with dynamic content or
anti-aliasing variation may need a higher floor.

Override recorded on the `ia_visual_baseline` row (`tolerance_pct` column):

- `budget-panel` → `0.01` (budget counter changes each sim tick)
- Static panels (pause-menu, toolbar) → `0.005` (default)

Agent sets override at baseline-record time. To inspect: `SELECT panel_slug, tolerance_pct
FROM ia_visual_baseline WHERE status='active' ORDER BY panel_slug;`

### 4 — Baseline refresh trigger semantics

Three categories:

| Trigger | Action |
|---|---|
| Intentional design change (deliberate restyle) | Agent files TECH/ART task → on completion, runs capture loop → `AskUserQuestion(approve/reject)` |
| Regression (unexpected pixel shift) | CI reports `VISUAL_REGRESSION_STRICT=1` red → agent investigates root cause before refresh |
| Token-bump cascade (minor anti-aliasing shift across N panels) | Sweep orchestrator shows approve_all prompt per archetype group; human approves in single poll |

`AskUserQuestion` poll shape for baseline refresh:

> Baseline refresh requested for **{panel_slug}**.
> Reason: {reason}. New diff: {diff_pct}%.
> Options: `approve` (record new baseline) / `reject` (keep current) / `refresh` (re-capture + re-poll) / `skip` (defer)

Manual-every-time decision: auto-refresh on token bump is intentionally excluded
(Q7=a decision; see ui-visual-regression exploration doc).

### 5 — VISUAL_REGRESSION_STRICT env + CI integration

`VISUAL_REGRESSION_STRICT=1` flips `validate:visual-regression` from warn-only to
exit-1-on-regression.

Behavior:

- **Strict** (`=1`): resolves touched panel slugs from `Assets/UI/Snapshots/panels.json`
  ∩ `git diff --name-only origin/main...HEAD` path scan; queries `ia_visual_diff` for
  `verdict='regression'` rows intersecting the touched set; exits 1 with JSON error
  report when any regression found.
- **Warn-only** (env unset or `=0`): always exits 0; no behavior change for local
  `verify:local`.

CI wiring: `.github/workflows/ia-tools.yml` sets `VISUAL_REGRESSION_STRICT: "1"` on the
`node` job env block. Deliberate single-pixel shift on a PR branch turns CI red.

Local operators: unset env keeps `verify:local` warn-only. Set explicitly when
testing strict mode locally: `VISUAL_REGRESSION_STRICT=1 npm run validate:visual-regression`.

Script: `tools/scripts/validate-visual-regression.mjs`.
Test: `tools/scripts/test/validate-visual-regression-strict.test.mjs`.
Fixture pair: `tools/scripts/test/fixtures/visual-regression-strict/` (baseline + candidate differing by 1px).

### 6 — LFS storage budget

Baseline PNGs stored in `Library/UiBaselines/` (gitignored). Promoted baselines
referenced by `image_ref` in `ia_visual_baseline` (path relative to repo root).

Initial estimate: ~20 published panels × ~200 KB each = ~4 MB active set.
Retired baselines superseded by `supersedes_id` chain; GC deferred to future plan.

Monitoring command: `du -sh Library/UiBaselines/` (local only — Library is gitignored).

LFS is NOT used for this plan: baseline PNGs are stored on disk + DB row reference only.
If baseline corpus exceeds 50 MB active set, file a TECH task to evaluate Git LFS or
S3 blob storage. Current signoff threshold: **non-blocking** — monitor quarterly.

### 7 — ui_calibration_verdict_record parallel retention

`ui_calibration_verdict_record` continues writing to `ia_ui_calibration_verdict` table
(migration 0157) in parallel with new `ia_visual_baseline` / `ia_visual_diff` pipeline.

Legacy JSONL files (`ia/state/ui-calibration-verdicts.jsonl`,
`ia/state/ui-calibration-corpus.jsonl`) migrated to DB via
`tools/scripts/migrate-calibration-jsonl-to-db.mjs --apply`; frozen snapshot in
`.archive/ui-calibration-jsonl-frozen/`.

Full retirement of JSONL read path deferred to UI Toolkit migration plan. Until then,
both `ui_calibration_verdict_record` (DB) and legacy JSONL readers coexist.
Handoff: when UI Toolkit migration plan is filed, cross-link to this section and retire
the JSONL path in that plan's Stage 1.

---

## Cursor Memory (optional)

Paste into **Cursor → Memory** for same policy across projects / sessions without opening repo:

- Territory Developer: During agent implementation, Unity is a **test** environment; **attempt** **Agent test mode batch** and **IDE agent bridge** verification; for **Path A**, release the **project lock** first (**`npm run unity:testmode-batch -- --quit-editor-first …`** or quit Editor), then **`unity:ensure-editor`** before **Path B** if needed; use **`timeout_ms` 40000** initial for bridge commands, follow **timeout escalation protocol** on timeout (`npm run unity:ensure-editor` → retry 60 s); report **Verification** with **validate:all**, **compile-check** if C# changed, **batch JSON result**, and **bridge** outcome. **IA** overview: `docs/information-architecture-overview.md`; policy: `docs/agent-led-verification-policy.md`.
