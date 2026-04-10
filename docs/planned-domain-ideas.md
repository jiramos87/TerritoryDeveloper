# Planned domain ideas (product backlog)

> **Purpose:** Register **future** game and tooling directions in one place. **Not** authoritative rules: **`BACKLOG.md`** issue text and, when implemented, **`ia/specs/*.md`** define behavior. This doc supports design discussion and **IA** search; glossary pointers live under [Planned terminology](../ia/specs/glossary.md#planned-domain-terms).

**Last updated:** 2026-04-10 (backlog hygiene: open **`BACKLOG.md`** + [`BACKLOG-ARCHIVE.md`](../BACKLOG-ARCHIVE.md) **Recent archive** for completed prerequisites)

## Relationship to the glossary

- **Reference specs** are unchanged until a prioritized **BACKLOG** feature ships.
- **`glossary.md`** includes a **non-authoritative** table of **planned** terms so agents can use consistent **English** labels when discussing **multipolar growth**, **water volume**, **geography authoring**, and **Compute-lib program** follow-ups (open rows on [`BACKLOG.md`](../BACKLOG.md); charter trace [`BACKLOG-ARCHIVE.md`](../BACKLOG-ARCHIVE.md)).

## 1. Geography authoring and parameter dashboard

**Idea:** A dedicated **geography authoring** experience (in-game wizard, **Editor** tool, or both) for **city** / **territory** **maps**, built on **isometric** terrain rules already in **`isometric-geography-system.md`**.

**Parameters (illustrative):** **map** dimensions, share of **water** vs land, **forest** coverage, **height** / hilliness, presence and mix of **sea**, **rivers**, **lakes** (**water body kind** proportions), and related knobs to be detailed in product design.

**Reuse:** The same structured parameters should feed **geography initialization**, future **player** tools (**terraform**, **basin** carving, **elevations**), **forest** placement, **water bodies** in **depressions**, and **AUTO** modes that depend on map template data.

**Technical alignment:** **JSON interchange program** + [`docs/postgres-interchange-patterns.md`](postgres-interchange-patterns.md) + **glossary**; JSON schemas, **Geography initialization** params, **CI** validation; **Postgres** dev surfaces ([`docs/postgres-ia-dev-setup.md`](postgres-ia-dev-setup.md)); **glossary** **Compute-lib program** for shared **compute** and **MCP** surfaces. No spec change required until this theme is scheduled for implementation on [`BACKLOG.md`](../BACKLOG.md).

## 2. Multipolar growth, desirability poles, and connurbation

**Idea:** Move from a single **urban centroid** and global **urban growth rings** toward **multiple** **urban poles** (e.g. industrial or service **desirability** hotspots). Each pole influences nearby **cells** so **AUTO** **zoning** and **street** *pressure* favor those regions; **committed** **roads** still use the **road preparation family** and **pathfinding cost model** (**geo** §10), not a substitute “desirability cost.”

**Rings:** Each pole can own a **ring** or distance field (exact math TBD); the city should still read as one **regional map** with coherent patterns.

**Connurbation:** Long-term rule set so two mature urban areas on one **map** can coexist under shared regional logic (definition TBD).

**Coordination:** Single-centroid **ring** tuning on [`BACKLOG.md`](../BACKLOG.md) remains relevant until **multipolar** data replaces it; **UrbanGrowthRingMath** (C#) is intended to make the transition safer.

## 3. Water volume, surface height, and adjacency fill

**Idea:** **Not** realistic 3D **fluid** dynamics. Target a **water volume budget** per **water body** (or connected volume): when the **basin** grows (e.g. new **depression** connected to **open water**), **surface height (S)** may **decrease** to conserve volume; shrinking capacity could raise **S**. Visuals: reposition **water** prefabs at the new **S** (exposing or submerging **terrain** / **islands**); **S** step changes are not animated as continuous simulation. Optional **isometric** directional **fill** **animation** for player feedback.

**Gameplay hook (planned):** Player **terraform** that digs a **dry** **cell** **Moore**-adjacent to **open water** allows the **hole** to **fill** from that body (full rules TBD).

**Related backlog:** **sea** / **shore band**, sources & drainage, **water body** tools — see [`BACKLOG.md`](../BACKLOG.md). Authoritative **water** and **geo** sections will be amended when this theme is implemented.

## 4. Program and tooling cross-links

| Item | Role |
|------|------|
| **Compute-lib program** | Charter (archived): **compute** libraries + **territory-ia** tools — [`BACKLOG-ARCHIVE.md`](../BACKLOG-ARCHIVE.md); ongoing **C#** utilities + research rows on [`BACKLOG.md`](../BACKLOG.md) **§ Compute-lib program** |
| **Pure C# compute helpers** | e.g. **ring** math multipolar-ready, optional **basin** / volume stubs for water-volume work |
| **Geography authoring / multipolar / water volume** | Product directions **without** `ia/projects/` specs until prioritized on [`BACKLOG.md`](../BACKLOG.md) |

## 5. When to promote to reference specs

When implementation starts on a prioritized **BACKLOG** feature:

1. Update the **authoritative** spec (**simulation-system** for **§Rings** / poles, **water-terrain-system** + **isometric-geography-system** for volume/**S**, **managers-reference** / **ARCHITECTURE** for authoring pipeline).
2. Move or merge the matching row from **glossary** “Planned terminology” into the main tables with a real **Spec** column.
3. Trim this doc or mark the section **completed** with a link to the spec change.
