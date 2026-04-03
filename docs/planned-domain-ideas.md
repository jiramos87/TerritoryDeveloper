# Planned domain ideas (product backlog)

> **Purpose:** Register **future** game and tooling directions in one place. **Not** authoritative rules: **`BACKLOG.md`** issue text and, when implemented, **`.cursor/specs/*.md`** define behavior. This doc supports design discussion and **IA** search; glossary pointers live under [Planned terminology](../.cursor/specs/glossary.md#planned-domain-terms).

**Last updated:** 2026-04-10 (backlog hygiene: open **`BACKLOG.md`** + [`BACKLOG-ARCHIVE.md`](../BACKLOG-ARCHIVE.md) **Recent archive** for completed prerequisites)

## Relationship to the glossary

- **Reference specs** are unchanged until a **FEAT-** is implemented.
- **`glossary.md`** includes a **non-authoritative** table of **planned** terms so agents can use consistent **English** labels when discussing **FEAT-46**–**FEAT-48** and the **TECH-36** program.

## 1. Geography authoring and parameter dashboard (**FEAT-46**)

**Idea:** A dedicated **geography authoring** experience (in-game wizard, **Editor** tool, or both) for **city** / **territory** **maps**, built on **isometric** terrain rules already in **`isometric-geography-system.md`**.

**Parameters (illustrative):** **map** dimensions, share of **water** vs land, **forest** coverage, **height** / hilliness, presence and mix of **sea**, **rivers**, **lakes** (**water body kind** proportions), and related knobs to be detailed in product design.

**Reuse:** The same structured parameters should feed **geography initialization**, future **player** tools (**terraform**, **basin** carving, **elevations**), **forest** placement, **water bodies** in **depressions**, and **AUTO** modes that depend on map template data.

**Technical alignment:** **TECH-21** program (**TECH-40**–**TECH-41** **§ Completed**, **TECH-44a** — JSON schemas, **Geography initialization** params, **CI** validation), **TECH-44** (**TECH-44b**/**c** — Postgres), **TECH-36** program (**TECH-37**–**TECH-39**) for shared **compute** and **MCP** surfaces; no spec change required until **FEAT-46** is scheduled for implementation.

## 2. Multipolar growth, desirability poles, and connurbation (**FEAT-47**)

**Idea:** Move from a single **urban centroid** and global **urban growth rings** toward **multiple** **urban poles** (e.g. industrial or service **desirability** hotspots). Each pole influences nearby **cells** so **AUTO** **zoning** and **street** *pressure* favor those regions; **committed** **roads** still use the **road preparation family** and **pathfinding cost model** (**geo** §10), not a substitute “desirability cost.”

**Rings:** Each pole can own a **ring** or distance field (exact math TBD); the city should still read as one **regional map** with coherent patterns.

**Connurbation:** Long-term rule set so two mature urban areas on one **map** can coexist under shared regional logic (definition TBD).

**Coordination:** **FEAT-43** (single-centroid **ring** tuning) remains relevant until **multipolar** data replaces it; **TECH-38** **UrbanGrowthRingMath** is intended to make the transition safer.

## 3. Water volume, surface height, and adjacency fill (**FEAT-48**)

**Idea:** **Not** realistic 3D **fluid** dynamics. Target a **water volume budget** per **water body** (or connected volume): when the **basin** grows (e.g. new **depression** connected to **open water**), **surface height (S)** may **decrease** to conserve volume; shrinking capacity could raise **S**. Visuals: reposition **water** prefabs at the new **S** (exposing or submerging **terrain** / **islands**); **S** step changes are not animated as continuous simulation. Optional **isometric** directional **fill** **animation** for player feedback.

**Gameplay hook (planned):** Player **terraform** that digs a **dry** **cell** **Moore**-adjacent to **open water** allows the **hole** to **fill** from that body (full rules TBD).

**Related backlog:** **FEAT-39** (**sea** / **shore band**), **FEAT-40** (sources & drainage), **FEAT-41** (**water body** tools). Authoritative **water** and **geo** sections will be amended when **FEAT-48** is implemented.

## 4. Program and tooling cross-links

| Item | Role |
|------|------|
| **TECH-36** | Umbrella program: **compute** libraries + **territory-ia** tools (**TECH-37**–**TECH-39**) |
| **TECH-38** | **Pure** **C#** helpers (e.g. **ring** math multipolar-ready, optional **basin** / volume stubs for **FEAT-48**) |
| **FEAT-46**–**FEAT-48** | Product issues **without** `.cursor/projects/` specs until prioritized |

## 5. When to promote to reference specs

When implementation starts on a **FEAT-**:

1. Update the **authoritative** spec (**simulation-system** for **§Rings** / poles, **water-terrain-system** + **isometric-geography-system** for volume/**S**, **managers-reference** / **ARCHITECTURE** for authoring pipeline).
2. Move or merge the matching row from **glossary** “Planned terminology” into the main tables with a real **Spec** column.
3. Trim this doc or mark the section **completed** with a link to the spec change.
