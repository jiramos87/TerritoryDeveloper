# AI Agent Guide — Territory Developer

## Before You Start

1. Read `ARCHITECTURE.md` for project structure, data flows, and dependency map
2. Read `.cursor/rules/` for coding conventions and manager responsibilities
3. Check the `/// <summary>` on the class you are about to modify
4. Read `BACKLOG.md` for current issues, priorities, and in-progress work

### Canonical geography specification

**`.cursor/specs/isometric-geography-system.md`** is the single canonical reference for grid coordinates, height model, slopes, water/shore/cliff layering, sorting, lakes/rivers, persistence, roads, terraform, and pathfinding. `ARCHITECTURE.md` summarizes init order and persistence; it defers mechanisms to the spec.

### `.cursor/specs/` policy

| File | Scope |
|------|--------|
| `isometric-geography-system.md` | Terrain, water, cliffs, shores, sorting, terraform, roads, rivers, pathfinding |
| `ui-design-system.md` | UI foundations, components, patterns |

Do not add bug write-ups, agent prompts, or one-off specs under `.cursor/specs/`. Use `BACKLOG.md` while work is open; delete temporary markdown after completion.

### `.cursor/projects/` policy

Project-specific specs for features or complex bugs **in active development** live under `.cursor/projects/`. These are **temporary** — they are deleted after the work is completed and verified.

| Aspect | Rule |
|--------|------|
| Template | `.cursor/templates/project-spec-template.md` — always use it as the starting point |
| Naming | `{ISSUE_ID}.md` (e.g. `FEAT-44.md`, `BUG-45.md`) |
| Lifecycle | Create when starting spec work → refine with agent → implement → verify → close |
| On completion | Migrate **lessons learned**, **new rules**, and **design decisions** to canonical docs (`AGENTS.md`, `.cursor/specs/`, `.cursor/rules/`) before deleting the project spec |

## Project docs outside `.cursor/specs/`

Charters and discovery for cross-cutting programs live under `docs/` as listed in `ARCHITECTURE.md`.

## Backlog: Next Issue and AI Agent Prompts

When the user asks which is the next issue, respond with it and **ask if they want an AI agent prompt** — a prompt for another agent to analyze, evaluate, and propose a development plan.

### Format when delivering an AI agent prompt

Respond with **Markdown** (not plain text) so it can be copied as a `.md` file:
- Put the full prompt inside a **fenced code block** with language tag `markdown`.
- Short prompts may omit the fence.
- Prompt body should be in **English** unless the user requests otherwise.

## Backlog: After Implementing a Plan

Keep the issue **"In progress"**. Only move to "Completed" when the user explicitly confirms verification.

## What to Read by Task Type

| Task | Where to start | Spec sections |
|------|---------------|---------------|
| Road logic | Road manager, grid manager, terrain manager | Geography spec §9, §10, §13 |
| Zoning logic | Zone manager, grid manager, demand manager | — |
| UI changes | UI manager + relevant controller | `.cursor/specs/ui-design-system.md` |
| UI / UX design system | `docs/ui-design-system-project.md` + context + spec | — |
| Simulation / AUTO growth | Simulation manager, auto builders, centroid service | — |
| Economy | Economy manager, city stats | — |
| Geography / slopes / heightmap | Geography spec (canonical) | §1–§5, §7–§8 |
| Terrain / heightmap | Terrain manager, heightmap, geography manager | §2–§4 |
| Water bodies | Water manager, water map, geography manager | §2, §4.2, §5.6–5.9, §7, §11–§12 |
| Forests | Forest manager, forest map, geography manager | — |
| Project spec (any issue) | `.cursor/templates/project-spec-template.md` + `BACKLOG.md` + relevant specs | — |
| New building type | Building interface, zone manager, grid manager | — |
| New prefab variants | `coding-conventions.mdc` (Prefabs section) | Geography spec §6.4 |
| Sorting / render bug | Grid sorting service, terrain manager | §7 |
| Interstate highways | Interstate manager, grid/terrain/road managers | §10, §13.5–§13.6 |
| Save / load | Save manager, grid data, water manager persistence | §7.4, §11.5 |
| GridManager decomposition | `BACKLOG.md` + grid manager + helper services | — |
| Demand / growth | Demand manager, growth manager, city stats | — |
| Statistics display | Statistics manager, city stats UI controller | — |
| Camera / viewport | Camera controller, grid manager | — |

## System Invariants (NEVER violate)

1. `HeightMap[x,y]` == `Cell.height` — always in sync; update both on every write (spec §2.4)
2. After road modification → call `InvalidateRoadCache()`
3. No `FindObjectOfType` in `Update` or per-frame loops — cache in `Awake`/`Start` only
4. No new singletons — use Inspector + `FindObjectOfType` pattern
5. No direct `gridArray`/`cellArray` access outside `GridManager` — use `GetCell(x, y)`
6. Do not add responsibilities to `GridManager` — extract to helper classes
7. Shore band: land Moore-adjacent to water must have `height ≤ min(S)` of neighbor water cells (spec §2.4.1)
8. Rivers: `H_bed` monotonically non-increasing toward exit (spec §12.4)
9. Cliff visible faces: south + east only — N/W not instantiated (spec §5.7)
10. Road placement: always through the **road preparation family** (`TryPrepareRoadPlacementPlan`, longest-valid-prefix, and locked deck-span prep when applicable) ending in `PathTerraformPlan` + Phase1 + `Apply` — never `ComputePathPlan` **alone** without that validation surface (spec §13.1)
11. `UrbanizationProposal`: NEVER re-enable — obsolete by design (TECH-13)
12. Do not add specs under `.cursor/specs/` for bugs or one-off work — use `BACKLOG.md` and `.cursor/projects/`

## Guardrails (IF → THEN)

- IF adding a manager reference → THEN use `[SerializeField] private` + `FindObjectOfType` fallback in `Awake`
- IF modifying roads → THEN call `InvalidateRoadCache()` after changes
- IF placing a road → THEN use the preparation family (`TryPrepareRoadPlacementPlan` / longest-prefix / locked deck-span branch), NOT `ComputePathPlan` alone as the sole placement gate
- IF touching `GridManager` → THEN extract new logic to a helper class, do not grow GridManager
- IF creating a new manager → THEN make it a MonoBehaviour scene component, never `new`
- IF modifying `HeightMap` → THEN also write `Cell.height` (and vice versa)
- IF placing or removing water → THEN call `RefreshShoreTerrainAfterWaterUpdate` afterward
- IF adding a new spec → THEN only under `.cursor/specs/` if it covers a permanent domain; use `.cursor/projects/` for issue-specific specs and `BACKLOG.md` for lightweight tracking
- IF closing a project spec → THEN migrate lessons learned, new rules, and design decisions to canonical docs before deleting the file from `.cursor/projects/`
- IF creating a project spec → THEN use `.cursor/templates/project-spec-template.md` as the starting point and name the file `{ISSUE_ID}.md` under `.cursor/projects/`

## Pre-commit Checklist

- [ ] Code compiles (Build in Unity)
- [ ] Class-level `/// <summary>` exists and is accurate
- [ ] New public methods have XML documentation
- [ ] Debug.Log messages and comments are in English
- [ ] If GridManager was touched, verify sorting order with different height levels
- [ ] If roads were modified, verify `InvalidateRoadCache()` is called where needed
- [ ] If a new manager was added, it follows the Inspector + FindObjectOfType pattern
- [ ] New prefabs follow `coding-conventions.mdc` naming (do not rename existing assets)
- [ ] Temporary `Debug.Log` diagnostics follow `coding-conventions.mdc` (remove or gate before merge)
