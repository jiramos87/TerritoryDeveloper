---
purpose: "Agent index — routes tasks to the right specs and rules"
audience: agent
loaded_by: always
slices_via: none
description: Agent index — routes tasks to the right specs and rules
alwaysApply: true
---

# Agent Router — What to Read by Task

territory-ia MCP available → follow **MCP — territory-ia** section below **before** bulk spec reads.

Read only task-relevant specs. Use table to navigate.

## Task → Spec routing

| Task domain | Spec to read | Key sections |
|---|---|---|
| Road logic, placement, bridges | `ia/specs/roads-system.md` + geography §9, §10, §13, **§14.5** | Validation surface, resolver rules, stroke/lip/wet run |
| Water, terrain, cliffs, shores | `ia/specs/water-terrain-system.md` + geography §2–§5, §11–§12 | Height model, layered visuals, lakes, rivers |
| Simulation, AUTO growth | `ia/specs/simulation-system.md` | Tick order, AUTO pipeline, **§Rings** (centroid + growth rings) |
| Save / load | `ia/specs/persistence-system.md` + geography §7.4, §11.5 | Load pipeline, visual restore |
| Manager responsibilities | `ia/specs/managers-reference.md` | Responsibilities, dependencies |
| Zones, buildings, RCI | `ia/specs/managers-reference.md` — **Zones & Buildings** | Zone lifecycle, pivot, density, undeveloped light zoning |
| Zone S, economy, budget envelope, bonds, treasury floor, state-service zoning, maintenance registry | `ia/specs/economy-system.md` | Overview, Zone S + sub-type registry, budget envelope, treasury clamp, bond ledger, maintenance contributors, save v3→v4 |
| Demand, desirability | `ia/specs/managers-reference.md` — **Demand (R/C/I)** | DemandManager, CityStats, growth pressure |
| Forests, regional map, utilities | `ia/specs/managers-reference.md` — **World features** | ForestManager, RegionalMapManager, resource buildings |
| In-game notifications | `ia/specs/managers-reference.md` — **Game notifications** | GameNotificationManager singleton |
| Slopes, sorting, geography | `ia/specs/isometric-geography-system.md` §1–§7 | Coordinates, heights, sorting formula |
| UI changes | `ia/specs/ui-design-system.md` | Foundations, components |
| Unity/MonoBehaviour/Inspector wiring, Script Execution Order, 2D renderer `sortingOrder`/layers (not isometric stacking), Editor Reports exports (Postgres registry + bridge) | `ia/specs/unity-development-context.md` | Full spec; defer Sorting order formula to geography §7; Editor agent diagnostics (menus, Postgres `editor_export_*`, `agent_bridge_job`) in §10 |
| Unity C# invariants (GridManager, HeightMap, roads, water, cliffs, MonoBehaviour lifecycle) | `ia/rules/unity-invariants.md` | Rules 1–11 + IF→THEN guardrails; fetch via MCP `rule_content unity-invariants` or `invariants_summary` (merges with universal `invariants.md`) |
| Unity scene wiring (new MonoBehaviour needs placement in `MainScene.unity`; new `[SerializeField]` needs Inspector assignment; new StreamingAssets consumer needs scene host) | `ia/rules/unity-scene-wiring.md` | Trigger list, target scene table, wiring checklist, evidence block; every lifecycle skill (plan-author, plan-digest, project-spec-implement, ship-stage, opus-code-review, verify-loop) enforces it |
| Coding standards | `ia/rules/coding-conventions.md` | XML docs, naming, prefabs, static-helper namespace |
| `tools/scripts`, smoke preflight, web deploy parity, IA test harness env | `ia/rules/agent-tooling-hints.md` | macOS shell, ts-node, `IA_COUNTER_*`, `git status` on `web/` |
| Branch author self-review (compact English review prose, IA scan order) | `ia/rules/agent-code-review-self.md` | Verdict → went well → suggestions; not caveman |
| XML doc style (C#) | `ia/rules/xml-doc-caveman.md` | Caveman Full style for `/// <summary>` / `<param>` / `<returns>` / `<exception>` / `<remarks>` |
| Domain terms | `ia/specs/glossary.md` | Quick definitions |
| Backlog / issues | `BACKLOG.md` (only if task involves issue) | — |
| Full dependency map | `ARCHITECTURE.md` | System layers, dep table |

## Canonical geography spec

`ia/specs/isometric-geography-system.md` = single source of truth for grid math, heights, slopes, water/shore/cliffs, sorting, terraform, roads, rivers, pathfinding. Other doc disagrees → geography wins.

## Quick reference for geography sections

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

## MCP — territory-ia (default when available)

Agent mode + territory-ia enabled → use these tools **by default** before bulk `read_file` on `ia/specs/*.md`: `backlog_issue` (when you have `BUG-`/`FEAT-`/`TECH-`/… id), `router_for_task`, `glossary_discover` (rough English keywords → canonical; translate from chat if human not English), `glossary_lookup` (known English term), `spec_outline`, `spec_section` (aliases: `geo`, `roads`, `unity`/`unityctx` → `unity-development-context`, …), `spec_sections` (batch slices), `project_spec_closeout_digest` (structured extract for `ia/projects/{ISSUE_ID}.md` closeout), `project_spec_journal_persist` / `project_spec_journal_search` / `project_spec_journal_get` / `project_spec_journal_update` (optional Postgres IA project spec journal when `DATABASE_URL` set — see [`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md)), `invariants_summary`, `list_rules`, `rule_content`, `unity_bridge_command` / `unity_bridge_get` / `unity_compile` (Postgres `agent_bridge_job` queue — `DATABASE_URL` + Unity on `REPO_ROOT`; `unity_compile` aliases `get_compilation_status`). Start with `list_specs` if keys unknown. Same routing as table above; MCP returns slices + structured errors (with fuzzy suggestions). Tool unavailable → use this doc + targeted reads.
