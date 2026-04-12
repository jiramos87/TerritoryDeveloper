# Territory Developer — IA System Review, Entity Model Analysis, and Extension Ideas

> Exploratory document — 2026-04-07. Based on code-as-truth-source analysis of the complete Information Architecture system, game entity model, and backlog.

---

## 1. Is the IA system well-documented for agents starting on an issue?

### What exists and works well

An agent beginning work has a **layered onboarding path** that scales from light to deep:

1. **`AGENTS.md`** — workflow policies, documentation hierarchy, MCP tool order, backlog conventions, pre-commit checklist. This is the single entry point.
2. **`ia/rules/` (always-apply)** — `agent-router.md` routes tasks to specs; `invariants.md` lists 12 hard constraints; `mcp-ia-default.md` enforces MCP-first retrieval; `terminology-consistency.md` and `project-overview.md` provide vocabulary and project identity.
3. **MCP tools** — `backlog_issue` → `router_for_task` → `glossary_discover/lookup` → `spec_section` gives an agent precisely the context slice it needs without reading whole files.
4. **Skills** — each `SKILL.md` includes a Tool Recipe with ordered MCP calls, trigger conditions, and explicit policies.
5. **`ARCHITECTURE.md`** — system layers, dependency map, data flows, init order.
6. **`docs/mcp-ia-server.md`** — MCP overview with tool descriptions and recipes.

### Assessment: Does a new agent have what it needs?

**Yes, substantially.** The system is already one of the most complete agent IA systems I've seen. An agent with MCP tools enabled gets routed to the right spec slices and glossary terms before writing code. The `always-apply` rules act as guardrails that load automatically.

### What's missing: the "system about the system"

There is **no single document that describes the IA system itself as a coherent design**. The information exists but is scattered across:

- `AGENTS.md` (documentation hierarchy, workflow)
- `docs/mcp-ia-server.md` (MCP tools)
- `docs/mcp-markdown-ia-pattern.md` (reusable pattern)
- `ia/skills/README.md` (skill conventions)
- `ARCHITECTURE.md` § Agent information architecture and MCP
- `ia/specs/REFERENCE-SPEC-STRUCTURE.md` (spec authoring)
- `ia/projects/PROJECT-SPEC-STRUCTURE.md` (project spec lifecycle)

**Recommendation:** A single `docs/information-architecture-overview.md` (~200 lines) that describes:

1. The design philosophy (hierarchical docs + semantic model + MCP slicing + skill workflows + Postgres persistence)
2. A diagram of how the layers connect (rules → specs → glossary → MCP → skills → project specs → backlog)
3. The lifecycle: how knowledge flows from issue creation → spec → implementation → closure → durable IA migration
4. The consistency mechanisms (terminology rules, invariant enforcement, `validate:*` scripts)
5. How to extend the system (add a spec, add a tool, add a skill, add a glossary term)

This would be the document an agent reads to understand *why* the system exists, not just *how to use it*. It would also benefit a human contributor onboarding to the project.

### Suggested improvements to existing documentation

| Area | Current gap | Proposed improvement |
|------|-------------|---------------------|
| **`AGENTS.md`** | Dense, long paragraph blocks (especially §3 Context from IA) | Restructure as a bulleted quick-start + detailed sections; add a "5-minute agent onboarding" summary at the top |
| **`docs/mcp-ia-server.md`** | Tool descriptions exist but no visual flow diagram | Add a sequence diagram: issue → MCP calls → code → verify → close |
| **Glossary** | Good coverage but no category index | Add a table of contents by category (Terrain, Water, Simulation, etc.) at the top of `glossary.md` |
| **Skills README** | Lists skills but doesn't show when to chain them | Add a "Skill lifecycle map" showing project-new → kickoff → implement → validation → close-dev-loop → close |
| **ARCHITECTURE.md** | Dependency map is text-only | Consider a Mermaid diagram for the manager dependency graph |

---

## 2. Extension and improvement ideas for the IA system

### 2.1 Derivable extensions (building on what exists)

#### A. **Semantic search across all IA surfaces** (evolution of TECH-18)

Currently `glossary_discover` does keyword ranking over glossary rows, and `spec_section` does fuzzy heading match. A natural next step:

- **Unified vector/FTS index** in Postgres covering: glossary terms, spec section bodies, invariants, rule descriptions, backlog issue Notes, and journal entries.
- **Single `ia_search(query, scope?)` MCP tool** that returns ranked results across all sources with source attribution.
- **Why now:** The Postgres infrastructure exists (`ia_project_spec_journal` already has `body_tsv` GIN index). Extending this pattern to spec sections and glossary is incremental.

#### B. **Agent context budget optimizer**

An MCP tool that, given an issue_id and a token budget (e.g., 4000 tokens), returns the optimal set of spec_section slices, glossary terms, and invariants — ranked by relevance to the issue's Files and Notes. This is the core of TECH-48 but framed as a budget-aware composite.

#### C. **Skill composition / chaining engine**

Skills today are static Markdown recipes. A lightweight engine could:

- Parse SKILL.md trigger conditions
- Given a task description, recommend the skill chain (e.g., "implement FEAT-43" → project-spec-kickoff → project-spec-implement → close-dev-loop)
- Pre-populate the MCP tool call sequence with issue-specific parameters
- **Implementation:** An MCP tool `suggest_skill_chain(task_description)` that reads all SKILL.md files and matches triggers.

#### D. **IA drift detection**

Extend `generate:ia-indexes --check` to also detect:

- Glossary terms not referenced in any spec
- Spec sections not referenced in agent-router
- Skills referencing tools that don't exist in the MCP server
- Dead glossary→spec cross-references

A `npm run validate:ia-consistency` that catches drift before it confuses agents.

#### E. **Project spec journal analytics**

The `ia_project_spec_journal` table stores Decision Log and Lessons Learned. Currently it's searched ad-hoc. Adding:

- **Pattern detection:** Which invariants are most frequently cited in Decision Logs? Which specs get the most lessons?
- **Recurring issues:** Keyword co-occurrence analysis across journal entries to surface systemic problems.
- An MCP tool `journal_insights(domain?)` that returns top patterns.

### 2.2 Out-of-the-box ideas

#### F. **Agent memory across sessions (persistent agent context)**

Today, each agent session starts fresh and must re-discover context via MCP. What if the system tracked:

- Which MCP tools an agent called for a given issue
- Which spec sections were most useful
- Which invariants were violated and caught
- Which glossary terms the agent needed to look up

A `agent_session_log` table that captures the "agent's journey" per issue, enabling:

- **Recommendation:** "Last time an agent worked on road issues, it needed geo §10, §13, roads-system §Validation, and invariant #10"
- **Efficiency metrics:** "Average MCP calls per issue type" to optimize the skill recipes

#### G. **Bidirectional IA: agents teaching the system**

Currently, knowledge flows from docs → agent. The reverse flow (agent → docs) only happens during project-spec-close. What if agents could:

- **Propose glossary additions** when they encounter an undefined term during implementation
- **Flag spec ambiguity** when `spec_section` doesn't answer the question they asked
- **Suggest invariant additions** when they discover a constraint empirically

A `ia_suggestion` Postgres table with lifecycle (proposed → reviewed → accepted/rejected), surfaced via MCP tool `suggest_ia_improvement(kind, content)`.

#### H. **Visual IA map (interactive)**

A web-based visualization (could be a simple HTML page served from `tools/`) showing:

- Specs as nodes, glossary terms as edges connecting them
- Skills as workflow arrows
- Backlog issues colored by priority, positioned near the specs they touch
- Real-time: which parts of the IA were accessed in the last N agent sessions

This would help the human developer see the "shape" of the project's knowledge.

#### I. **Spec versioning with semantic diff**

When a reference spec changes, track not just the git diff but the *semantic* delta:

- Which sections were added/removed/modified?
- Which glossary terms were affected?
- Which invariants now apply differently?

Store in Postgres alongside the journal. Enables: "What changed in the geography spec since the last time an agent worked on water issues?"

### 2.3 Perspective shift: from document retrieval to knowledge graph

The current system is fundamentally a **document retrieval** architecture: agents ask for slices of Markdown files. The next evolutionary step is a **knowledge graph** where:

- **Entities** are: managers, cells, water bodies, zones, buildings, prefabs, invariants, glossary terms, spec sections
- **Relationships** are: "depends on", "modifies", "validates", "persists", "affects demand of"
- **Queries** become: "What are all the things that happen when I change a cell's height?" → HeightMap sync invariant + shore band recalc + cliff face update + sorting order + water body membership check + terraform undo...

This is the long-term vision behind TECH-18's `dependency_chain(term)` tool. The Postgres tables already support it. The key insight is that **the glossary is already a proto-knowledge graph** (term → spec → category), and the agent-router is a **task→knowledge mapping**. Connecting them into a queryable graph would be transformative.

---

## 3. Entity model analysis and database extension for gameplay

### 3.1 Current runtime entity model

Based on code analysis of the actual classes:

```
GameSaveData (root save object)
├── List<CellData>           ← per-cell: position, height, zone, building, road, forest, water, desirability
├── WaterMapData             ← water bodies: id, surfaceHeight, classification, cell membership
├── CityStatsData            ← aggregate: population, money, happiness, 9 zone counts, resources, forest %
├── GrowthBudgetData         ← simulation: % allocation for daily growth
├── RegionalMap              ← neighbors: border cities, interstate connectivity
└── InGameTime               ← calendar: current date

Runtime-only (lost on save/load):
├── StatisticsManager        ← 8 trend queues (30-value rolling windows): population, unemployment, jobs, 3x demand, income, happiness
├── DemandManager            ← 3 demand levels + building deltas per cycle
├── EconomyManager           ← tax rates + projected income (rates are in CityStatsData, projections are computed)
└── Zone categories          ← derived from zone type on Awake
```

### 3.2 Gaps between runtime state and what could be queried/analyzed

| Gap | Current state | What's missing |
|-----|--------------|----------------|
| **Time-series history** | StatisticsManager keeps only 30 values in memory, not persisted | No historical population, demand, or economic trends across save/load |
| **Building-level identity** | Cells store building type/size/population but no unique building ID | Can't track individual building lifecycle (constructed → upgraded → demolished) |
| **Road network topology** | 4 booleans per cell (hasRoadAtLeft/Top/Right/Bottom) | No graph structure: can't query "path from A to B" or "connected road segments" without runtime pathfinding |
| **Service coverage** | desirability is a float per cell, computed once at geography init | No dynamic service radius (fire, police, education, health) or coverage maps |
| **Citizen model** | population is an integer per cell | No individual agents, no commute simulation, no demographic breakdown |
| **Transaction history** | money changes happen via AddMoney/RemoveMoney | No financial ledger: can't answer "where did the city spend money last month?" |
| **Zone evolution** | Zone type is set/replaced atomically | No upgrade history: can't answer "when did this cell go from light to heavy residential?" |

### 3.3 Proposed entity model extension for database-backed gameplay

#### Phase 1: Time-series persistence (aligns with FEAT-51 dashboard)

**New table: `city_metrics_history`**

```sql
CREATE TABLE city_metrics_history (
    id            bigserial PRIMARY KEY,
    game_session  text NOT NULL,          -- save name or session id
    game_date     date NOT NULL,          -- in-game date
    metric_kind   text NOT NULL,          -- 'population', 'money', 'happiness', 'demand_r', 'demand_c', 'demand_i', 'employment', 'forest_coverage'
    value         double precision NOT NULL,
    recorded_at   timestamptz DEFAULT now()
);
CREATE INDEX ON city_metrics_history (game_session, metric_kind, game_date);
```

**Integration point:** SimulationManager.ProcessSimulationTick() already calls each system in order. A `MetricsRecorder` service (new helper, not a MonoBehaviour manager) snapshots key values after each tick and writes to the DB via the same Postgres bridge pattern used for `agent_bridge_job`.

**Gameplay value:**
- FEAT-51 (game data dashboard) gets a real data source for charts
- Agents can query "show me population growth over the last 50 ticks" via MCP
- Save/load preserves history

#### Phase 2: Event sourcing for financial transactions

**New table: `city_events`**

```sql
CREATE TABLE city_events (
    id            bigserial PRIMARY KEY,
    game_session  text NOT NULL,
    game_date     date NOT NULL,
    event_kind    text NOT NULL,          -- 'tax_income', 'road_expense', 'building_expense', 'service_expense', 'demolition_refund'
    amount        double precision,
    details       jsonb,                  -- { zone_type: "residential", cell_x: 5, cell_y: 10 }
    recorded_at   timestamptz DEFAULT now()
);
```

**Integration point:** EconomyManager.SpendMoney() and AddMoney() are the chokepoints for all financial flows. Wrapping them to emit events is non-invasive.

**Gameplay value:**
- Supports a clear audit trail for **monthly maintenance** and other treasury movements (see **glossary** **Monthly maintenance**)
- Dashboard shows income vs expenses breakdown
- Agents can diagnose "why is the city going bankrupt?" by querying recent events

#### Phase 3: Grid snapshot diffing for simulation analysis

**New table: `grid_snapshots`**

```sql
CREATE TABLE grid_snapshots (
    id            bigserial PRIMARY KEY,
    game_session  text NOT NULL,
    game_date     date NOT NULL,
    snapshot_kind text NOT NULL,          -- 'full', 'delta'
    data          jsonb NOT NULL,         -- compressed CellData array or delta
    cell_count    int NOT NULL,
    recorded_at   timestamptz DEFAULT now()
);
```

**Integration point:** Periodic snapshots (every N ticks or on significant events) captured by the same MetricsRecorder service.

**Gameplay value:**
- BUG-52 (AUTO zoning gaps) becomes diagnosable: diff snapshots to see which cells never get zoned
- FEAT-43 (growth ring tuning) gets empirical data: how does development actually spread over time?
- Agent debugging: `debug_context_bundle` could include a "last 5 snapshots for this Moore neighborhood"

#### Phase 4: Building identity and lifecycle

**New approach:** Add a `buildingId` (int) field to Cell/CellData and a `buildings` table:

```sql
CREATE TABLE buildings (
    id              bigserial PRIMARY KEY,
    game_session    text NOT NULL,
    building_id     int NOT NULL,          -- matches Cell.buildingId
    cell_x          int NOT NULL,
    cell_y          int NOT NULL,
    zone_type       text NOT NULL,
    density_tier    text NOT NULL,         -- light/medium/heavy
    constructed_at  date NOT NULL,         -- in-game date
    upgraded_at     date,
    demolished_at   date,
    population      int DEFAULT 0,
    UNIQUE(game_session, building_id)
);
```

**Gameplay value:**
- FEAT-08 (zone density evolution) needs to know building age and upgrade history
- Property value calculations can factor in building age
- Dashboard shows "buildings constructed this month" and "average building age per district"

### 3.4 Architecture for game-database integration

The current Postgres bridge (`agent_bridge_job`) is designed for agent/debug use. For gameplay persistence, a different pattern is needed:

**Option A: Write-through from Unity (recommended for Phase 1-2)**

```
SimulationManager.ProcessSimulationTick()
  → MetricsRecorder.RecordTick()     [new helper class]
    → batch INSERT via Node.js bridge (same pattern as agent-bridge-complete.mjs)
    → fire-and-forget: game doesn't wait for DB write
    → fallback: buffer in memory if DB unavailable
```

**Option B: Event bus with async drain (recommended for Phase 3-4)**

```
EconomyManager.SpendMoney()
  → GameEventBus.Emit(new MoneyEvent(...))    [new lightweight event system]
    → in-memory ring buffer (fixed size, e.g., 10k events)
    → async drain to Postgres every N seconds or on save
    → MCP tool reads from Postgres
```

**Key constraint:** The game must remain playable without Postgres. All database writes are optional enrichment. The `db_unconfigured` graceful degradation pattern from the IA system applies here too.

---

## 4. Backlog impact analysis and new ideas

### 4.1 Highest-impact existing issues

Prioritized by "impact on the player's experience of a living, responsive city":

| Rank | Issue | Why high impact |
|------|-------|-----------------|
| 1 | ~~**Monthly maintenance**~~ (shipped) | Recurring **street** and **utility building** upkeep creates economic tension; see **glossary** **Monthly maintenance**. |
| 2 | ~~Dynamic happiness~~ (shipped) | Multi-factor happiness (employment, taxes, services, pollution) is the core loop of city-builders. Shipped with pollution model. |
| 3 | ~~**Tax→demand feedback**~~ (shipped) | **Tax** rates feed **happiness** and **per-sector demand**; same-day refresh after daily **happiness** — **managers-reference** **Demand (R / C / I)**. |
| 4 | **FEAT-43** (Growth ring tuning) | The AUTO simulation is the "life" of the city. Gradual center→edge gradient makes cities look organic vs artificial. |
| 5 | **BUG-52** (AUTO zoning gaps) | Visible artifacts that break immersion. Gap cells between roads and zones look like bugs to the player. |
| 6 | **FEAT-08** (Zone density evolution) | Buildings upgrading over time is the hallmark visual feedback of city growth in the genre. |
| 7 | **BUG-14** (FindObjectOfType in Update) | Performance tax on every frame. Fixing this improves the baseline experience for all players. |
| 8 | **TECH-01** (GridManager decomposition) | Not player-facing directly, but unblocks faster development of everything else. 2070-line hub class slows every feature. |
| 9 | **FEAT-51** (Game data dashboard) | Gives observability. Players love watching their city grow in charts. Also helps developers tune simulation parameters. |

### 4.2 Proposed multi-issue lanes (new)

#### Lane: "Economic Depth" (4-5 issues, sequential)

**Goal:** Transform the economy from "money goes up forever" to a genuine city-builder economic simulation.

```
Dynamic happiness (shipped: employment, taxes, services, pollution)
    → Monthly maintenance (shipped: streets, power plants — glossary)
      → Tax→demand feedback (shipped — **managers-reference** **Demand (R / C / I)**)
        → FEAT-09 (trade/production/salaries — deep economy)
```

**Rationale:** Each issue builds on the previous. After this lane, the player has genuine economic tension: balancing income vs expenses, managing happiness through services, and seeing the city respond to tax policy.

#### Lane: "Living City" (3-4 issues, partially parallel)

**Goal:** Make the AUTO simulation produce cities that look and feel organic.

```
FEAT-43 (growth ring tuning: center→edge gradient)
  ├→ BUG-52 (fix AUTO zoning gaps — can be parallel)
  └→ FEAT-08 (zone density evolution: buildings upgrade over time)
       → FEAT-47 (multipolar urban centroids — long-term)
```

**Rationale:** Growth rings control the macro shape, gap fixing controls the micro details, density evolution adds visual progression. Together, a city that "grows up" from a village.

#### Lane: "Player Agency & Feedback" (3 issues, sequential)

**Goal:** Close the loop between player actions and visible consequences.

```
BUG-48 (minimap auto-refresh — quick win)
  → FEAT-51 (game data dashboard with charts)
    → FEAT-42 (minimap height/relief layer — polish)
```

**Rationale:** The player needs to *see* the city changing. Live minimap + dashboard + terrain visualization give continuous feedback without opening menus.

### 4.3 New issue ideas

#### FEAT-XX — **City services coverage model** (fire, police, education, health)

**Type:** feature (new system)
**Files:** New `ServiceCoverageManager.cs`, `CityStats.cs`, `DemandManager.cs`, per-service radius logic
**Notes:** Each service building (fire station, police station, school, hospital) has a coverage radius computed from the road network. Cells within coverage get happiness/desirability bonuses; cells outside suffer penalties. Coverage gaps create visible "danger zones" on the minimap. Prerequisite for FEAT-11, FEAT-12, FEAT-13 but can ship as a generic framework first.

**Why:** This is the missing piece between "place buildings" and "manage a city." Every successful city-builder has service coverage as a core mechanic.

#### FEAT-XX — **Zoning demand heatmap overlay**

**Type:** feature (UI/simulation)
**Files:** `MiniMapController.cs`, `DemandManager.cs`, `GridManager.cs`
**Notes:** Toggle a minimap/game overlay showing per-cell demand intensity for R/C/I. Color-coded: green (high demand) → yellow (balanced) → red (oversupplied). Updated each simulation tick. Helps the player decide where to zone without guessing.

**Why:** Makes demand *visible* rather than abstract. Players in city-builders make better decisions when they can see the data spatially.

#### FEAT-XX — **Natural disaster events** (floods, fires, earthquakes)

**Type:** feature (new system)
**Files:** New `DisasterManager.cs`, `WaterManager.cs`, `ForestManager.cs`, `CityStats.cs`
**Notes:** Periodic events that test the player's city design. Floods affect low-elevation cells near water bodies. Fires spread through dense zones without fire service coverage. Earthquakes damage buildings proportional to density. Events are probability-based per simulation tick with increasing frequency as the city grows.

**Why:** Creates dramatic moments and validates the player's infrastructure choices. Also drives adoption of service buildings (fire stations, hospitals).

#### TECH-XX — **Simulation replay/rewind**

**Type:** tooling/feature
**Files:** New `SimulationReplayService.cs`, grid snapshot persistence
**Notes:** Record simulation state at each tick (using the grid_snapshots approach from §3.3). Allow rewinding to any past state. Initially for debugging (paired with close-dev-loop), eventually a player feature ("undo last 10 ticks"). Uses the same Postgres infrastructure as the metrics persistence.

**Why:** Dual value: debugging tool for developers + undo feature for players. The grid snapshot infrastructure enables both.

#### FEAT-XX — **District/neighborhood system**

**Type:** feature (simulation/UI)
**Files:** New `DistrictManager.cs`, `GridManager.cs`, `CityStats.cs`, `UIManager.cs`
**Notes:** Allow the player to define named districts (contiguous cell regions). Each district tracks its own stats: population, happiness, demand, density, tax policy. Districts can have independent tax rates and service priorities. Enables "downtown vs suburbs" gameplay. Visual: distinct minimap coloring per district.

**Why:** Adds strategic depth. The player manages macro (city-wide) and micro (district) policies. Also makes FEAT-47 (multipolar growth) more meaningful: each urban pole naturally becomes a district.

#### TECH-XX — **Agent-driven simulation parameter tuning**

**Type:** tooling/agent enablement
**Files:** `tools/mcp-ia-server/src/tools/`, `SimulationManager.cs`, `AutoRoadBuilder.cs`, `AutoZoningManager.cs`
**Notes:** MCP tools to: (1) read current simulation parameters (growth budget %, demand decay rate, desirability multiplier, ring boundary fractions), (2) modify them at runtime via bridge command, (3) run N simulation ticks and measure outcomes (population growth rate, zone distribution, road density). Enables agents to A/B test parameter changes and recommend optimal values.

**Why:** The simulation has many magic numbers (TECH-03). Rather than hand-tuning, let agents run experiments. This is particularly valuable for FEAT-43 (growth ring tuning) and any future balance work.

---

## Summary

The IA system is remarkably complete and well-designed. The main opportunities are:

1. **Documentation:** A single "system overview" document explaining the IA philosophy and architecture
2. **IA evolution:** Semantic search, knowledge graph, agent memory, bidirectional learning
3. **Entity model:** Time-series persistence, event sourcing, building identity — all building on the existing Postgres infrastructure
4. **Gameplay impact:** The "Economic Depth" lane (**monthly maintenance** and **tax→demand** feedback shipped; happiness + pollution shipped) delivers strong player value per effort invested
5. **New systems:** Service coverage, demand heatmaps, districts, and agent-driven parameter tuning would each deepen the simulation significantly
