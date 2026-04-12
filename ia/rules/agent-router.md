---
purpose: "Agent index — routes tasks to the right specs and rules"
audience: agent
loaded_by: always
slices_via: none
description: Agent index — routes tasks to the right specs and rules
alwaysApply: true
---

# Agent Router — What to Read by Task

When **territory-ia** MCP tools are available, follow the **MCP — territory-ia** section below **before** loading entire spec files.

Read **only** the specs relevant to your task. Use this table to navigate.

## Task → Spec routing

| Task domain | Spec to read | Key sections |
|-------------|-------------|--------------|
| Road logic, placement, bridges | `ia/specs/roads-system.md` + geography spec §9, §10, §13, **§14.5** | Validation surface, resolver rules, stroke / lip / wet run |
| Water, terrain, cliffs, shores | `ia/specs/water-terrain-system.md` + geography spec §2–§5, §11–§12 | Height model, layered visuals, lakes, rivers |
| Simulation, AUTO growth | `ia/specs/simulation-system.md` | Tick order, AUTO pipeline, **§Rings** (centroid + growth rings) |
| Save / load | `ia/specs/persistence-system.md` + geography spec §7.4, §11.5 | Load pipeline, visual restore |
| Manager responsibilities | `ia/specs/managers-reference.md` | Responsibilities, dependencies |
| Zones, buildings, RCI | `ia/specs/managers-reference.md` — **Zones & Buildings** | Zone lifecycle, pivot, density, undeveloped light zoning |
| Demand, desirability | `ia/specs/managers-reference.md` — **Demand (R / C / I)** | DemandManager, CityStats, growth pressure |
| Forests, regional map, utilities | `ia/specs/managers-reference.md` — **World features** | ForestManager, RegionalMapManager, resource buildings |
| In-game notifications | `ia/specs/managers-reference.md` — **Game notifications** | GameNotificationManager singleton |
| Slopes, sorting, geography | `ia/specs/isometric-geography-system.md` §1–§7 | Coordinates, heights, sorting formula |
| UI changes | `ia/specs/ui-design-system.md` | Foundations, components |
| Unity / MonoBehaviour / Inspector wiring, Script Execution Order, 2D renderer `sortingOrder` / layers (not isometric stacking rules), Editor Reports exports (Postgres registry + bridge) | `ia/specs/unity-development-context.md` | Full spec; defer Sorting order formula to `isometric-geography-system.md` §7; Editor agent diagnostics (menus, **Postgres** **`editor_export_*`**, **`agent_bridge_job`**) in §10 |
| Coding standards | `ia/rules/coding-conventions.md` | XML docs, naming, prefabs |
| Domain terms | `ia/specs/glossary.md` | Quick definitions |
| Backlog / issues | `BACKLOG.md` (only if task involves an issue) | — |
| Full dependency map | `ARCHITECTURE.md` | System layers, dep table |

## Canonical geography spec

`ia/specs/isometric-geography-system.md` is the **single source of truth** for grid math, heights, slopes, water/shore/cliffs, sorting, terraform, roads, rivers, pathfinding. When another doc disagrees, the geography spec wins.

## Quick reference for geography spec sections

| Need to understand... | Read sections |
|---|---|
| Grid math, coordinates | §1 |
| Height model, water surface | §2 |
| Slope determination | §3–§4 |
| Shore/cliff/water layering | §5 |
| Prefab inventory | §6 |
| Sorting order | §7 |
| Terraform system | §8 |
| Road prefabs on terrain | §9 |
| Pathfinding costs | §10 |
| Water map, lakes, junctions | §11 |
| Procedural rivers | §12 |
| Road/interstate/bridge validation | §13 |
| Engineering notes, road/grid vocabulary (stroke, lip, grass, Chebyshev) | §14 (**§14.5**) |

## MCP — territory-ia (default when tools are available)

In **Agent** mode with the **territory-ia** server enabled, use these tools **by default** to build context—**before** bulk `read_file` on `ia/specs/*.md`: `backlog_issue` (when you have `BUG-`/`FEAT-`/`TECH-`/… id), `router_for_task`, `glossary_discover` (rough **English** keywords → canonical terms; translate from the chat if the human did not write in English), `glossary_lookup` (known **English** term), `spec_outline`, `spec_section` (aliases: `geo`, `roads`, `unity` / `unityctx` → `unity-development-context`, …), `spec_sections` (batch slices), `project_spec_closeout_digest` (structured extract for `ia/projects/{ISSUE_ID}.md` closeout prep), `project_spec_journal_persist` / `project_spec_journal_search` / `project_spec_journal_get` / `project_spec_journal_update` (optional **Postgres** **IA project spec journal** when `DATABASE_URL` is set — see [`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md)), `invariants_summary`, `list_rules`, `rule_content`, `unity_bridge_command` / `unity_bridge_get` / `unity_compile` (**Postgres** **`agent_bridge_job`** queue — **`DATABASE_URL`** + **Unity** on **REPO_ROOT**; **`unity_compile`** aliases **`get_compilation_status`**). Start with `list_specs` if you need keys. Same routing priorities as the table above; MCP returns slices and structured errors (including fuzzy suggestions). If a tool is unavailable, use this document and targeted file reads.
