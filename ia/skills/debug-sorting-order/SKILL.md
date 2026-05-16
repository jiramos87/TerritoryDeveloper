---
name: debug-sorting-order
purpose: >-
  Agent-led diagnosis when isometric sorting order looks wrong: bridge exports vs geography spec §7
  authority.
audience: agent
loaded_by: "skill:debug-sorting-order"
slices_via: spec_section
description: >-
  Debug isometric sorting order using IDE agent bridge export sugar (unity_export_sorting_debug,
  unity_export_cell_chunk) and territory-ia spec_section geo §7. Triggers: "sorting order wrong",
  "sortingOrder debug", "isometric draw order", "compare sorting to spec §7", "Moore neighbor
  sorting". Prerequisites: DATABASE_URL, migration 0008, Unity Editor on REPO_ROOT,
  db:bridge-preflight green.
phases: []
triggers:
  - sorting order wrong
  - sortingOrder debug
  - isometric draw order
  - compare sorting to spec §7
  - Moore neighbor sorting
model: inherit
tools_role: custom
tools_extra: []
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
hard_boundaries: []
---

# Debug sorting order — bridge exports + geography §7

**Canonical formula:** [`ia/specs/isometric-geography-system.md`](../../specs/isometric-geography-system.md) §7 (retrieve via **`spec_section`** key **`geo`**, section **§7**).

**Not** a substitute for **`close-dev-loop`** when you need **`debug_context_bundle`** (screenshot + console + anomaly scan + Moore export). Use this skill when the question is **sorting math / layer ordering** and bounded **JSON exports** suffice.

**Related:** [`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md) (generic bridge) · [`close-dev-loop`](../close-dev-loop/SKILL.md) (before/after **`debug_context_bundle`**) · [`bridge-environment-preflight`](../bridge-environment-preflight/SKILL.md) (Step 0).

## Prerequisites

| Requirement | Notes |
|---|---|
| `DATABASE_URL` or `config/postgres-dev.json` | Same registry as other bridge tools |
| Migration `0008_agent_bridge_job.sql` | `npm run db:migrate` |
| Unity Editor on `REPO_ROOT` | `AgentBridgeCommandRunner` dequeue |
| territory-ia MCP | **`unity_export_*`** and **`spec_section`** |

## Tool recipe (territory-ia) — execution order

1. **Preflight** — `npm run db:bridge-preflight` (or [`bridge-environment-preflight`](../bridge-environment-preflight/SKILL.md)). Exit 0 before enqueue.
2. **Baseline export (sorting)** — `unity_export_sorting_debug` with fixed **`seed_cell`** (e.g. `"12,8"`) and optional **`timeout_ms`** (40000 initial). Store full JSON text (sorting debug artifact / registry paths as returned).
3. **Spec authority** — `spec_section` with key **`geo`**, target **§7** (sorting formula / layer rules). Extract the normative terms (height, row, relative order) you will compare against.
4. **Cross-check cells** — `unity_export_cell_chunk` with **`origin_x`**, **`origin_y`**, **`chunk_width`**, **`chunk_height`** covering the same neighborhood as the seed (bounded 8×8 or per repro). Confirm cell ids / heights align with sorting debug rows.
5. **Change** — Edit C# / prefab / sorting only as needed. After **C#** changes, run **`npm run unity:compile-check`** (or `get_compilation_status` via bridge) before re-export.
6. **Post-fix export** — Repeat steps 2–4 with the **same** **`seed_cell`** and chunk bounds.
7. **Diff** — Compare before/after sorting debug JSON (stable keys only): ordering of paired objects, `sortingOrder` / layer fields if present, mismatches against §7 vocabulary from step 3.
8. **Verdict** — Pass only if export deltas match intended §7 fix; else iterate (cap iterations per your issue **`MAX_ITERATIONS`**).

## Comparison checklist (before / after)

Use the same **`seed_cell`** string for both runs. Record artifact paths or paste stable excerpts (no secrets).

| Check | Before | After |
|-------|--------|-------|
| `unity_export_sorting_debug` completed | | |
| `spec_section` **geo** §7 excerpt reviewed | | |
| `unity_export_cell_chunk` bounds cover repro area | | |
| Sorting-related keys improved vs §7 terms | | |
| No new `failed` / bridge error string | | |

## Parameter reference (MCP sugar)

| Tool | Role |
|---|---|
| `unity_export_sorting_debug` | **`kind`:** **`export_sorting_debug`** — optional **`seed_cell`** `"x,y"`, **`timeout_ms`**, **`agent_id`**. |
| `unity_export_cell_chunk` | **`kind`:** **`export_cell_chunk`** — **`origin_x`**, **`origin_y`**, **`chunk_width`**, **`chunk_height`**, optional **`timeout_ms`**, **`agent_id`**. |

Prefer raw **`unity_bridge_command`** if you need a different **`kind`**, manual **`unity_bridge_get`** polling, or custom **`params`** not covered above ([`docs/mcp-ia-server.md`](../../../docs/mcp-ia-server.md)).

## Seed prompt

```markdown
Run debug-sorting-order (`ia/skills/debug-sorting-order/SKILL.md`) for repro at seed_cell {SEED_CELL}.
Preflight → unity_export_sorting_debug → spec_section geo §7 → unity_export_cell_chunk → fix → repeat; same seed for before/after diff.
```
