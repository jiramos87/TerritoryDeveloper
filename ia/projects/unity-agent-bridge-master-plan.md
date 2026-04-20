# Unity IDE Agent Bridge — Master Plan (Post–Phase 1 program)

> **Status:** Draft — Step 1 / Stage 1.1 pending (no BACKLOG rows filed yet)
>
> **Scope:** Tiered hardening + transport + optional depth on top of shipped **Postgres** **`agent_bridge_job`** + **`unity_bridge_command`** / **`unity_bridge_get`** + **`AgentBridgeCommandRunner`** (`docs/unity-ide-agent-bridge-analysis.md` **Design Expansion**). **Out of program:** headless CI, `-batchmode` / Test Framework as delivery goals, file-only queue replacing **`agent_bridge_job`**, rewrite of **`ia/specs/unity-development-context.md`** §10 JSON contracts. Optional deferrals → recommend companion `docs/unity-agent-bridge-post-mvp-extensions.md` (not authored by this pass).
>
> **Exploration source:** `docs/unity-ide-agent-bridge-analysis.md` (**§ Design Expansion** — Chosen Approach, Architecture, Subsystem Impact, Implementation Points, Examples).
>
> **Locked decisions (do not reopen in this plan):**
> - **Phase 1 already shipped:** queue + MCP + Editor runner — this plan is **§10-B → §10-C → §10-D** tiers, not a second MVP bridge choice.
> - Keep **`agent_bridge_job`** + existing **`kind`** surface; additive migrations only when new **`kind`** values need DB contract.
> - **Developer machine + Unity Editor open**; glossary-aligned command names; **`DATABASE_URL`** + migration **0008** remain the persistence path.
> - Reuse existing **`[MenuItem]`** export bodies — dispatch-only changes; no duplicate grid read logic.
> - **Grid reads:** **`GridManager.GetCell`** only where bridge touches cells — **invariant #5**.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Read first if landing cold:**
> - `docs/unity-ide-agent-bridge-analysis.md` — full analysis + **Design Expansion** (ground truth).
> - `ia/specs/unity-development-context.md` §10 — Editor agent diagnostics, **`editor_export_*`**, **`agent_bridge_job`**.
> - `docs/mcp-ia-server.md` — MCP tool catalog + bridge tools.
> - `ia/skills/ide-bridge-evidence/SKILL.md` — evidence / **`debug_context_bundle`** contract.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase/task cardinality (≥2 tasks per phase).
> - `ia/rules/invariants.md` — **#5** (no direct **`gridArray`** / **`cellArray`** outside **`GridManager`**), **#3** (no hot-loop **`FindObjectOfType`** — bridge polling stays Editor update), **#6** (do not grow **`GridManager`** — extract helpers if new play-mode probes).
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Steps

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | Skeleton | Planned | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`; `Skeleton` + `Planned` authored by `master-plan-new` / `stage-decompose`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file-plan` + `stage-file-apply` → task rows gain `Issue` id + `Draft` status; `stage-file-apply` also flips Stage header `Draft/Planned → In Progress` (R2) and plan top Status `Draft → In Progress — Step {N} / Stage {N.M}` on first task ever filed (R1); `stage-decompose` → Step header `Skeleton → Draft (tasks _pending_)` (R7); `/author` → `In Review`; `/implement` → `In Progress`; `/closeout` (Stage-scoped) → `Done (archived)` + phase box when last task of phase closes + stage `Final` + step rollup; `master-plan-extend` → plan top Status `Final → In Progress — Step {N_new} / Stage {N_new}.1` when new Steps appended to a Final plan (R6).

---

### Step 1 — Hardening: parameterized exports + MCP sugar + skills

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 1):** 0 filed

**Objectives:** Align **`AgentBridgeCommandRunner`** + **`[MenuItem]`** exports with bounded **`params`** (cell chunk bounds, sorting seeds, optional agent-context seeds) per **`unity-development-context`** §10. Add thin **`unity_export_*`** MCP wrappers where token cost warrants. Ship **`.claude/skills/debug-sorting-order`** recipe (bridge + **`spec_section`** **`geo`** §7). Confirm **Close Dev Loop** / registry supersession narrative in durable docs.

**Exit criteria:**

- **`export_cell_chunk`**, **`export_sorting_debug`**, **`export_agent_context`** bridge paths accept documented **`params`**; Play Mode / grid gates return **`failed`** + clear error string when preconditions not met.
- At least one **`unity_export_*`** sugar tool registered in **`tools/mcp-ia-server`** with Zod validation + tests mirroring bridge **`request`** shape.
- **`docs/mcp-ia-server.md`** lists new **`kind`** / sugar tools + params; cross-links §10.
- **`.claude/skills/debug-sorting-order/SKILL.md`** exists with end-to-end tool recipe (no **`ia/skills/`** clone per analysis §6).
- **`npm run validate:all`** green after Step 1 lands.

**Art:** None.

**Relevant surfaces (load when step opens):**

- `docs/unity-ide-agent-bridge-analysis.md` — **Design Expansion** Implementation Points **Phase A**
- `ia/specs/unity-development-context.md` §10 (lines ~141–185) — Reports + bridge artifacts
- `ia/specs/isometric-geography-system.md` §7 — sorting formula authority for debug-sorting skill
- `Assets/Scripts/Editor/AgentBridgeCommandRunner.cs` (exists) — dispatch extension
- `Assets/Scripts/Editor/AgentBridgeCommandRunner.Mutations.cs` (exists)
- `Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs` (exists) — sorting + agent context
- `Assets/Scripts/Editor/InterchangeJsonReportsMenu.cs` (exists) — cell chunk + world snapshot
- `tools/mcp-ia-server/` (exists) — `registerTool` + tests
- `.claude/skills/debug-sorting-order/SKILL.md` **(new)**
- `ia/skills/ide-bridge-evidence/SKILL.md` (exists) — update only if response DTOs change

---

#### Stage 1.1 — Parameterized Editor bridge + menu dispatch

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Extend **`AgentBridgeCommandRunner`** + menu exports so MCP **`request.params`** drives bounded export parameters without duplicating export bodies. Harden **`failed`** status when Play Mode / grid preconditions fail (**invariant #5** on any new cell reads).

**Exit:**

- **`BridgeCommand`** / DTO path parses **`params`** for **`export_cell_chunk`**, **`export_sorting_debug`**, **`export_agent_context`** (and documents defaults when omitted).
- **`AgentDiagnosticsReportsMenu`** / **`InterchangeJsonReportsMenu`** expose parameterized static entry points (or thin wrappers) callable from runner — existing menu items remain human baseline.
- Integration smoke: enqueue job → **`unity_bridge_get`** returns **`completed`** or **`failed`** with stable error string for uninitialized grid.

**Phases:**

- [ ] Phase 1 — Runner **`params`** parsing + switch dispatch for chunk / sorting / agent context.
- [ ] Phase 2 — Menu static methods / wrappers accept bounded parameters aligned with §10.
- [ ] Phase 3 — Preconditions: **`get_play_mode_status`** / grid init gating + **`failed`** JSON contract.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.1.1 | Runner params DTO + dispatch | 1 | _pending_ | _pending_ | Extend **`AgentBridgeCommandRunner`** (and shared DTOs) to deserialize **`params`** for **`export_cell_chunk`** (**`origin_*`**, **`width`**, **`height`**), **`export_sorting_debug`** / **`export_agent_context`** optional **`seed_cell`**; route to menu statics without new **`gridArray`** reads (**invariant #5**). |
| T1.1.2 | MCP Zod alignment for new params | 1 | _pending_ | _pending_ | Update **`tools/mcp-ia-server`** **`unity_bridge_command`** / job **`request`** Zod so enqueued rows match Unity DTOs; add fixture or unit test for param round-trip. |
| T1.1.3 | Menu parameterized entry points | 2 | _pending_ | _pending_ | Refactor **`AgentDiagnosticsReportsMenu`** + **`InterchangeJsonReportsMenu`** so bridge calls **`Export*`** methods with explicit parameter structs; preserve existing **`MenuItem`** behavior via defaults. |
| T1.1.4 | Menu regression pass | 2 | _pending_ | _pending_ | Manual or automated check: **Territory Developer → Reports** still runs for all §10 items; no duplicate file writes; **`TryPersistReport`** paths unchanged for registry exports. |
| T1.1.5 | Play Mode + grid gate errors | 3 | _pending_ | _pending_ | Before Play-only exports, verify **`GridManager.isInitialized`** (and documented **`TerrainManager`** needs); return **`failed`** + human-readable **`error`** field; align with analysis §8.3 risk table. |
| T1.1.6 | Bridge response contract tests | 3 | _pending_ | _pending_ | Add EditMode or MCP-side tests asserting **`completed`** / **`failed`** shapes for **`export_cell_chunk`** + sorting debug when grid absent — snapshot keys only, not full JSON bodies. |

---

#### Stage 1.2 — MCP sugar tools + catalog

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Register thin **`unity_export_*`** tools that wrap **`unity_bridge_command`** + poll **`unity_bridge_get`** for common flows. Keep surface small to avoid tool sprawl (analysis **Phase A** risk).

**Exit:**

- At least **two** sugar tools shipped (e.g. **`unity_export_cell_chunk`**, **`unity_export_sorting_debug`**) with shared helper for poll/backoff.
- **`docs/mcp-ia-server.md`** documents sugar vs raw bridge; **`kind`** table updated.
- MCP integration tests cover happy path + timeout/error.

**Phases:**

- [ ] Phase 1 — Register sugar tools + shared enqueue/poll helper.
- [ ] Phase 2 — Documentation + cross-links to **`unity-development-context`** §10.
- [ ] Phase 3 — Tests + **`validate:all`** gate.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.2.1 | Sugar tool registration | 1 | _pending_ | _pending_ | Add **`unity_export_cell_chunk`** + **`unity_export_sorting_debug`** (names per glossary / existing patterns) in **`tools/mcp-ia-server/src/**`; thin wrappers — call **`unity_bridge_command`**, poll **`unity_bridge_get`** by **`command_id`**, return parsed body / path refs. |
| T1.2.2 | Shared poll helper + limits | 1 | _pending_ | _pending_ | Extract shared TypeScript helper: timeout aligned with agent-led verification policy (40 s initial / escalation documented in tool description); surface **`BRIDGE_TIMEOUT`** env if already used elsewhere. |
| T1.2.3 | mcp-ia-server.md catalog update | 2 | _pending_ | _pending_ | Document sugar tools, params, and when to prefer raw **`unity_bridge_command`**; link **`agent_bridge_job`** migration + dequeue scripts. |
| T1.2.4 | §10 cross-link from spec | 2 | _pending_ | _pending_ | Add short pointer in **`ia/specs/unity-development-context.md`** §10 “See also” to MCP catalog section for sugar tools (minimal edit — no contract rewrite). |
| T1.2.5 | MCP integration tests | 3 | _pending_ | _pending_ | Extend **`tools/mcp-ia-server`** tests: mock or stub bridge responses if needed; assert Zod + tool handler paths for sugar tools. |
| T1.2.6 | validate:all + index | 3 | _pending_ | _pending_ | Run **`npm run validate:all`**; update **`generate:ia-indexes`** if tool catalog indexed; fix any **`registerTool`** descriptor drift. |

---

#### Stage 1.3 — Cursor skill + narrative alignment

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship **`.claude/skills/debug-sorting-order`** (Cursor-only). Patch **`ia/skills/ide-bridge-evidence`** only if Step 1 changes evidence DTOs. Align **Close Dev Loop** / staging supersession text with exploration §7.1 / §10-B.

**Exit:**

- **`.claude/skills/debug-sorting-order/SKILL.md`** committed with phases: bridge calls → **`spec_section`** **`geo`** §7 → compare → fix loop.
- **`ide-bridge-evidence`** updated OR explicit “no delta” note in Stage exit if DTOs unchanged.
- Docs note how **`debug_context_bundle`** relates to sugar tools (no contradiction with **`close-dev-loop`**).

**Phases:**

- [ ] Phase 1 — Author **`debug-sorting-order`** skill + symlink if repo uses **`.claude/skills/`** pattern.
- [ ] Phase 2 — **`ide-bridge-evidence`** alignment pass.
- [ ] Phase 3 — Durable doc narrative + optional backlog pointer.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.3.1 | debug-sorting-order SKILL body | 1 | _pending_ | _pending_ | Author **`.claude/skills/debug-sorting-order/SKILL.md`**: triggers, prerequisites (**`DATABASE_URL`**, Unity on **`REPO_ROOT`**), recipe calling **`unity_export_sorting_debug`** + **`unity_export_cell_chunk`**, **`spec_section`** **`geo`** §7, comparison checklist (**BUG-28**-style). |
| T1.3.2 | Symlink + skill index | 1 | _pending_ | _pending_ | If required by repo convention, symlink **`ia/skills/...`** → **`.claude/skills/...`**; add row to **`ia/skills/README.md`** only if this repo lists Cursor-packaged skills (minimal). |
| T1.3.3 | ide-bridge-evidence diff | 2 | _pending_ | _pending_ | Read **`ia/skills/ide-bridge-evidence/SKILL.md`**; update tool names / bundle fields if Step 1 changed responses; otherwise add single-line “no bridge DTO change” exit note in task report. |
| T1.3.4 | Glossary / router spot-check | 2 | _pending_ | _pending_ | Verify **`glossary_lookup`** “IDE agent bridge” + **`router_for_task`** domains still accurate; no new glossary row unless new public term introduced (terminology rule). |
| T1.3.5 | Close Dev Loop doc alignment | 3 | _pending_ | _pending_ | Update **`docs/agent-led-verification-policy.md`** or **`docs/mcp-ia-server.md`** short subsection: **`close-dev-loop`** + **`debug_context_bundle`** vs sugar tools — supersession of registry staging (per analysis). |
| T1.3.6 | Optional backlog spec pointer | 3 | _pending_ | _pending_ | If **`ia/backlog/TECH-552.yaml`** (or successor) tracks bridge program, add **`spec:`** → this orchestrator path + **`npm run materialize-backlog.sh`** — only if issue record exists; do not invent issue id in orchestrator body. |

---

### Step 2 — HTTP transport + observability

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 2):** 0 filed

**Objectives:** Add **localhost** **`HttpListener`** transport (same JSON envelope as **`agent_bridge_job`** commands) for sub-second round-trips. Extend observability: log forwarding, screenshot / health **`kind`** hardening per analysis **Phase B** / §5.

**Exit criteria:**

- **`POST`** **`localhost:{port}/...`** accepts bridge JSON; **`AgentBridgeCommandRunner`** (or sibling static class) executes on main thread via **`EditorApplication.update`** queue (**§10-C** risk: marshaling).
- Log capture path documented: **`Application.logMessageReceived`** → bridge buffer / response (aligned with existing **`AgentBridgeConsoleBuffer`**).
- New or hardened **`kind`** values for screenshot / health automation documented in §10 + MCP.

**Art:** None.

**Relevant surfaces (load when step opens):**

- `docs/unity-ide-agent-bridge-analysis.md` — §4.5 HTTP upgrade + **Design Expansion** **Phase B**
- `Assets/Scripts/Editor/AgentBridgeCommandRunner.cs` — shared dispatch extraction target
- `Assets/Scripts/Editor/AgentBridgeConsoleBuffer.cs` (exists)
- `Assets/Scripts/Editor/AgentBridgeScreenshotCapture.cs` (exists)
- `tools/mcp-ia-server/` — optional HTTP client tool or documented **`curl`** recipe

---

#### Stage 2.1 — Localhost HTTP bridge

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Implement **`HttpListener`** on loopback only; marshal command execution to Unity main thread; share dispatch with existing job runner.

**Exit:**

- Default port **7780** (configurable **`EditorPrefs`**) with conflict detection.
- Same command envelope as **`unity_bridge_command`** **`request`** jsonb.
- Automated or scripted smoke: **`curl`** POST → **`completed`** response when Editor idle.

**Phases:**

- [ ] Phase 1 — Listener bootstrap + thread-safe main-thread queue.
- [ ] Phase 2 — Wire queue to shared **`ExecuteCommand`** / dispatch table used by **`agent_bridge_job`** path.
- [ ] Phase 3 — EditorPrefs port + fallback behavior when HTTP disabled.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.1.1 | HttpListener Editor class | 1 | _pending_ | _pending_ | New Editor static (e.g. **`AgentBridgeHttpHost`**) registering **`localhost`** prefix only; reject non-loopback; start/stop tied to Editor play mode preference (documented). |
| T2.1.2 | Main-thread command queue | 1 | _pending_ | _pending_ | Queue **`BridgeCommand`** payloads from listener thread; drain on **`EditorApplication.update`** (same pump pattern as screenshot deferral). |
| T2.1.3 | Shared dispatch extraction | 2 | _pending_ | _pending_ | Refactor **`AgentBridgeCommandRunner`** so dequeue + HTTP paths call single **`ExecuteBridgeCommand`** internal API — no duplicate switch bodies. |
| T2.1.4 | HTTP integration smoke | 2 | _pending_ | _pending_ | Repo script under **`tools/scripts/`** or MCP test: POST sample **`get_play_mode_status`** → JSON **`completed`**; document **`curl`** in **`docs/mcp-ia-server.md`**. |
| T2.1.5 | EditorPrefs port + enable flag | 3 | _pending_ | _pending_ | **`EditorPrefs`** keys for port, enable HTTP; log clear error on **`HttpListenerException`** (address in use). |
| T2.1.6 | Security note in docs | 3 | _pending_ | _pending_ | Document localhost-only binding, no secrets in payloads, **`DATABASE_URL`** stays env — analysis §4.1. |

---

#### Stage 2.2 — Logs, screenshots, health kinds

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Close gaps for **§10-C** observability: log forwarding, screenshot / health automation **`kind`** values, MCP docs + tests.

**Exit:**

- Forwarding path from **`logMessageReceived`** to bridge responses (or ring buffer merge) specified and shipped.
- Screenshot / health **`kind`** behavior matches **`unity-development-context`** §10 table; **`docs/mcp-ia-server.md`** updated.
- **`npm run validate:all`** green.

**Phases:**

- [ ] Phase 1 — Log forwarding + buffer merge semantics.
- [ ] Phase 2 — Screenshot / health **`kind`** hardening + §10 table update.
- [ ] Phase 3 — Tests + manual verify checklist.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.2.1 | Log forwarding handler | 1 | _pending_ | _pending_ | Wire **`Application.logMessageReceived`** (Editor play) to **`AgentBridgeConsoleBuffer`** or parallel buffer; define ordering with existing dequeue (**invariant #3** — no per-frame heavy work). |
| T2.2.2 | Response merge rules | 1 | _pending_ | _pending_ | When **`kind`** requests logs in HTTP or job response, specify merge with **`since_utc`** / filters; document limits (max lines). |
| T2.2.3 | Screenshot / health kinds | 2 | _pending_ | _pending_ | Align **`capture_screenshot`** + health-check export **`kind`** with **`AgentBridgeScreenshotCapture`** deferred pump; update §10 artifact table rows. |
| T2.2.4 | Anomaly scanner hook | 2 | _pending_ | _pending_ | If **`AgentBridgeAnomalyScanner`** exposes new entry for health **`kind`**, wire without duplicating grid reads (**invariant #5**). |
| T2.2.5 | MCP + docs parity | 3 | _pending_ | _pending_ | Update tool descriptors + **`docs/mcp-ia-server.md`** for any new **`kind`** / HTTP discovery; link **IDE bridge evidence** skill. |
| T2.2.6 | Manual verify checklist | 3 | _pending_ | _pending_ | Short **`docs/`** or **`ia/skills`** pointer: steps for human to validate logs + screenshot in Play Mode (agent-led verification policy alignment). |

---

### Step 3 — Optional depth: streaming, comparison, replay

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 3):** 0 filed

**Objectives:** Deliver **optional** **§10-D** items: richer streaming / comparison helpers; spike deterministic replay + visual diff — gated to avoid scope creep. Prefer bucketing heavy deferrals to **`docs/unity-agent-bridge-post-mvp-extensions.md`**.

**Exit criteria:**

- Comparison helper (**`unity_validate_fix`**-class) either shipped as thin MCP wrapper or explicitly deferred with extensions-doc pointer.
- Replay / visual diff: spike outcome documented — proceed vs defer — **no** accidental CI mandate.

**Art:** None.

**Relevant surfaces (load when step opens):**

- `docs/unity-ide-agent-bridge-analysis.md` — **Design Expansion** **Phase C**, **Deferred / out of scope**
- `docs/unity-agent-bridge-post-mvp-extensions.md` **(recommended, not required)** — bucket for deferrals

---

#### Stage 3.1 — Streaming / comparison helpers

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Add structured before/after export comparison (MCP or Editor). Optional chunked / streaming responses for large payloads **without** breaking **`agent_bridge_job`** contract.

**Exit:**

- Comparison tool OR documented deferral + extensions appendix entry.
- If streaming shipped: documented size limits + fallback to disk artifact paths.

**Phases:**

- [ ] Phase 1 — Before/after comparison DTO + MCP tool sketch.
- [ ] Phase 2 — Optional streaming / chunking strategy (if needed).

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.1.1 | Comparison DTO + diff algorithm | 1 | _pending_ | _pending_ | Define stable JSON diff for two **`editor_export_*`** **`document`** bodies or file paths (sorting debug + cell chunk) — ignore noisy fields (`exported_at_utc`). |
| T3.1.2 | unity_validate_fix wrapper | 1 | _pending_ | _pending_ | Thin MCP tool: enqueue two exports (before/after) or accept paths; return structured diff summary for agents. |
| T3.1.3 | Streaming spike | 2 | _pending_ | _pending_ | Evaluate chunked HTTP or job **`response`** fields for large artifacts; default stays disk path + **`document jsonb`**. |
| T3.1.4 | Extensions doc deferral row | 2 | _pending_ | _pending_ | If streaming not shipped, append deferral paragraph to **`docs/unity-agent-bridge-post-mvp-extensions.md`** (create file only if Step 3 proceeds and file missing — coordinate with user). |

---

#### Stage 3.2 — Deterministic replay + visual diff (spike / defer)

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Time-box spike for seed + action log capture + visual diff automation; explicit gate whether to fold into backlog or extensions-only.

**Exit:**

- Spike doc section: **feasible / not feasible** + rough effort.
- No **headless CI** language introduced — analysis §4.1 guardrail.

**Phases:**

- [ ] Phase 1 — Replay spike scope + capture points.
- [ ] Phase 2 — Visual diff automation vs defer.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.2.1 | Replay capture scope | 1 | _pending_ | _pending_ | Identify minimal hooks: **`GameSaveManager`** seed + input queue vs full action log — document data written to **`tools/reports/`** gitignored paths. |
| T3.2.2 | Replay spike prototype | 1 | _pending_ | _pending_ | Optional throwaway Editor script: load fixture + N ticks — **not** CI — prove deterministic snapshot equality for one scenario. |
| T3.2.3 | Visual diff automation assessment | 2 | _pending_ | _pending_ | Compare **`ScreenCapture`** pairs + structural diff from Step 3.1; decide ship vs **`post-mvp`** bucket. |
| T3.2.4 | Gate decision + extensions pointer | 2 | _pending_ | _pending_ | Write **Decision** paragraph in exploration doc OR extensions doc; if defer-only, no production code requirement. |

---

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `/closeout` (Stage-scoped pair) runs.
- Run `claude-personal "/stage-file ia/projects/unity-agent-bridge-master-plan.md Stage {N}.{M}"` (routes to `stage-file-plan` + `stage-file-apply` pair) to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand except via documented skills.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to exploration doc + optional extensions doc.
- Keep **`unity-development-context`** §10 authoritative for Reports + artifact tables — patch with cross-links only unless contract intentionally versioned.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only the terminal step landing triggers a final `Status: Final`; the file stays.
- Silently promote post-MVP items into Step 1–2 — bucket to **`docs/unity-agent-bridge-post-mvp-extensions.md`**.
- Merge partial stage state — every stage must land on a green bar (`npm run validate:all` + `npm run unity:compile-check` when C# touched).
- Insert BACKLOG rows directly into this doc — only `stage-file` materializes them.
- Replace **`agent_bridge_job`** with file-only transport — locked out.
- Add **headless CI** or **`-batchmode`** delivery goals to this plan — analysis explicitly excludes.

---
