# Unity IDE Agent Bridge — Master Plan (Post–Phase 1 program)

> **Last updated:** 2026-04-24
>
> **Status:** In Progress — Stage 2.1
>
> **Scope:** Tiered hardening + transport + optional depth on top of shipped **Postgres** **`agent_bridge_job`** + **`unity_bridge_command`** / **`unity_bridge_get`** + **`AgentBridgeCommandRunner`** (`docs/unity-ide-agent-bridge-analysis.md` **Design Expansion**). **Out of program:** headless CI, `-batchmode` / Test Framework as delivery goals, file-only queue replacing **`agent_bridge_job`**, rewrite of **`ia/specs/unity-development-context.md`** §10 JSON contracts. Optional deferrals → recommend companion `docs/unity-agent-bridge-post-mvp-extensions.md` (not authored by this pass).
>
> **Exploration source:** `docs/unity-ide-agent-bridge-analysis.md` (**§ Design Expansion** — Chosen Approach, Architecture, Subsystem Impact, Implementation Points, Examples).
>
> **Locked decisions (do not reopen in this plan):**
>
> - **Phase 1 already shipped:** queue + MCP + Editor runner — this plan is **§10-B → §10-C → §10-D** tiers, not a second MVP bridge choice.
> - Keep **`agent_bridge_job`** + existing **`kind`** surface; additive migrations only when new **`kind`** values need DB contract.
> - **Developer machine + Unity Editor open**; glossary-aligned command names; **`DATABASE_URL`** + migration **0008** remain the persistence path.
> - Reuse existing **`[MenuItem]`** export bodies — dispatch-only changes; no duplicate grid read logic.
> - **Grid reads:** **`GridManager.GetCell`** only where bridge touches cells — **invariant #5**.
>
> **Hierarchy rules:** `docs/MASTER-PLAN-STRUCTURE.md` (canonical Stage > Task 2-level shape — authoritative) · `ia/rules/project-hierarchy.md` · `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable) · `ia/rules/plan-apply-pair-contract.md`.
>
> **Read first if landing cold:**
>
> - `docs/unity-ide-agent-bridge-analysis.md` — full analysis + **Design Expansion** (ground truth).
> - `ia/specs/unity-development-context.md` §10 — Editor agent diagnostics, **`editor_export_*`**, **`agent_bridge_job`**.
> - `docs/mcp-ia-server.md` — MCP tool catalog + bridge tools.
> - `ia/skills/ide-bridge-evidence/SKILL.md` — evidence / **`debug_context_bundle`** contract.
> - `docs/MASTER-PLAN-STRUCTURE.md` + `ia/rules/project-hierarchy.md` — doc semantics + Stage / Task cardinality rule (≥2 Tasks per Stage).
> - `ia/rules/invariants.md` — **#5** (no direct **`gridArray`** / **`cellArray`** outside **`GridManager`**), **#3** (no hot-loop **`FindObjectOfType`** — bridge polling stays Editor update), **#6** (do not grow **`GridManager`** — extract helpers if new play-mode probes).
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Stage `Status:` uses enum `Draft | In Review | In Progress | Final` (per `docs/MASTER-PLAN-STRUCTURE.md` §6.2). Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file-apply` → task rows gain `Issue` id + `Draft` status; `plan-author` / `plan-digest` → `In Review`; `spec-implementer` → `In Progress`; `plan-applier` Mode stage-closeout → `Done (archived)` + Stage `Final` rollup.

### Stage index

- [Stage 1.1 — Parameterized Editor bridge + menu dispatch](stage-1.1-parameterized-editor-bridge-menu-dispatch.md) — _Final_
- [Stage 1.2 — MCP sugar tools + catalog](stage-1.2-mcp-sugar-tools-catalog.md) — _Final_
- [Stage 1.3 — Cursor skill + narrative alignment](stage-1.3-cursor-skill-narrative-alignment.md) — _Final_
- [Stage 2.1 — Localhost HTTP bridge](stage-2.1-localhost-http-bridge.md) — _Draft_
- [Stage 2.2 — Logs, screenshots, health kinds](stage-2.2-logs-screenshots-health-kinds.md) — _Draft_
- [Stage 3.1 — Streaming / comparison helpers](stage-3.1-comparison-helpers.md) — _Draft_
- [Stage 3.2 — Deterministic replay + visual diff (spike / defer)](stage-3.2-defer.md) — _Draft_

## Orchestration guardrails

**Do:**

- Open one Stage at a time. Next Stage opens only after current Stage's closeout runs.
- Run `/stage-file ia/projects/unity-agent-bridge-master-plan.md Stage N.M` to materialize `_pending_` Tasks.
- Update Stage + Task `Status` via lifecycle skills — do NOT edit by hand.
- Preserve locked decisions. Changes require explicit re-decision + sync edit to exploration + scope-boundary docs.
- Extend via `/master-plan-extend ia/projects/unity-agent-bridge-master-plan.md {source-doc}` — do NOT hand-insert new Stage blocks.
- Keep **`unity-development-context`** §10 authoritative for Reports + artifact tables — patch with cross-links only unless contract intentionally versioned.

**Do not:**

- Close the orchestrator via `/closeout` — orchestrators are permanent (`orchestrator-vs-spec.md`).
- Silently promote post-MVP items into Stage 1–2 — bucket to **`docs/unity-agent-bridge-post-mvp-extensions.md`**.
- Merge partial Stage state — every Stage lands on a green bar (`npm run validate:all` + `npm run unity:compile-check` when C# touched).
- Insert BACKLOG rows directly into this doc — only `stage-file-apply` materializes them.
- Hand-insert new Stages past the last persisted `### Stage N.M` block — run `/master-plan-extend`.
- Replace **`agent_bridge_job`** with file-only transport — locked out.
- Add **headless CI** or **`-batchmode`** delivery goals to this plan — analysis explicitly excludes.
