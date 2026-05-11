# Backlog — Territory Developer

> Single source of truth for project issues. Reference via `@BACKLOG.md` in agent conversation. Closed work → [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md). Use **`mcp__territory-ia__backlog_issue`** for slice access.
>
> **Lane order (highest first):** § Compute-lib program → § Agent ↔ Unity & MCP context lane → § IA evolution lane → § UI-as-code program → § Economic depth lane → § Gameplay & simulation lane → § Multi-scale simulation lane → § Blip audio program → § Sprite gen lane → § Web platform lane → § High / § Medium / § Code Health / § Low. **Gameplay blockers** in § High Priority stay **interrupt** work — stop play / corrupt saves.
>
> **Closed program charters** (trace in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) + glossary): **Spec-pipeline** (territory-ia spec-pipeline program; exploration [`projects/spec-pipeline-exploration.md`](projects/spec-pipeline-exploration.md)) · **UI-as-code program** umbrella (UI-as-code program; **`ui-design-system.md`** Codebase inventory (uGUI)) · **TECH-39 computational MCP suite** (Computational MCP tools (TECH-39)).
>
> **Active programs:** **§ Compute-lib program** (TECH-38 + TECH-32 / TECH-35 research) · **§ IA evolution lane** TECH-77–TECH-83 + TECH-552 (FTS, skill chaining, agent memory, bidirectional IA, knowledge graph, gameplay entity model, sim parameter tuning, Unity Agent Bridge — [`docs/ia-system-review-and-extensions.md`](docs/ia-system-review-and-extensions.md); bridge program [`docs/unity-ide-agent-bridge-analysis.md`](docs/unity-ide-agent-bridge-analysis.md)) · **§ UI-as-code program** open FEAT-51 · **§ Economic depth lane** FEAT-52 → FEAT-53 → FEAT-09 (economy, services, districts; monthly maintenance, tax→demand feedback, happiness + pollution shipped) · **§ Gameplay & simulation lane** player-facing AUTO / density.

---

## Compute-lib program

**Dependency order.** Pilot compute-lib + World ↔ Grid MCP shipped ([`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) + glossary `territory-compute-lib`). TECH-39 (computational MCP suite) closed (glossary `Computational MCP tools (TECH-39)`). **TECH-38** (C# pure modules + harnesses) extends `Utilities/Compute/` + `tools/reports/`. Research **TECH-32** + **TECH-35** marked `Depends on: none` but run after TECH-38 surfaces exist (compare vs UrbanGrowthRingMath / RNG notes).

## Agent ↔ Unity & MCP context lane

Ordered for **closed-loop agent ↔ Unity** — **Close Dev Loop** orchestration shipped (glossary **IDE agent bridge** — [`docs/unity-ide-agent-bridge-analysis.md`](docs/unity-ide-agent-bridge-analysis.md); Play Mode bridge **`kind`** values, **`debug_context_bundle`**, **`close-dev-loop`** Skill, **dev environment preflight** archived [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) **Recent archive**). Remaining lane order: **JSON / reports** plumbing → **MCP platform** → **agent workflow & CI helpers** → **research tooling**. (**§ Compute-lib program** above: **TECH-38** + **TECH-32**/**TECH-35**.) **Prerequisites for later items:** **TECH-15**, **TECH-16**, **TECH-31**, **TECH-35**, **TECH-30** (existing `ia/projects/*.md`); **TECH-38** + archived **TECH-39** (**§ Compute-lib program** / [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md)). **Spec-pipeline** charter: **glossary** **territory-ia spec-pipeline program** + archive.

## Backlog YAML ↔ MCP alignment program

Orchestrator: [`ia/projects/backlog-yaml-mcp-alignment-master-plan.md`](projects/backlog-yaml-mcp-alignment-master-plan.md) (permanent, never closeable — step > stage > phase > task per `ia/rules/project-hierarchy.md`). Aligns **MCP territory-ia** parser + tool surface + validator + skill docs w/ per-issue yaml backlog refactor. Step 1 = HIGH band (IP1–IP5). Stage 1.1 closed 2026-04-17 (TECH-295..TECH-301 all archived — type extension + loader field mapping + soft-dep marker preservation + `proposed_solution` decision + MCP payload surfacing + round-trip test). Stage 1.2 opened 2026-04-17 — 7 tasks filed below (TECH-323..TECH-329: shared lint core + `backlog_record_validate` + `reserve_backlog_ids` + concurrency test + `backlog_list` + filter tests). Step 2 = MEDIUM/LOW band (IP6–IP9); file rows when Stage 1.x → `In Progress`.

### Stage 1.1 — Types + yaml loader (IP1 + IP2)

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 1.2 — MCP tools batch 1 (IP3 + IP4 + IP5)

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 3.2 — Template frontmatter + backfill script

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

## MCP lifecycle tools — Opus 4.7 audit program

Orchestrator: [`ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md`](projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md) (permanent, never closeable — step > stage > phase > task per `ia/rules/project-hierarchy.md`). Reshapes 32-tool MCP surface from 4.6-era sequential-call shape to 4.7-era composite-bundle + structured-envelope architecture. Step 1 closed — Stage 1.1 + Stage 1.2 archived (glossary bulk-`terms`, structured `invariants_summary`, v0.6.0 release). Step 2 In Progress — Stage 2.1 archived (TECH-388..TECH-391: envelope + caller allowlist + unit tests). Stage 2.2 opened 2026-04-18 — 8 tasks filed below (TECH-398..TECH-405: wrap all 32 handlers in `wrapTool` by family — spec / rule+router / glossary / invariant / backlog / DB-coupled / bridge / Unity analysis). Step 2 exit ships as breaking release v1.0.0.

### Stage 2.1 — Envelope Infrastructure + Auth

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 2.2 — Rewrite 32 Tool Handlers

## DB lifecycle extensions program

Orchestrator: [`ia/projects/db-lifecycle-extensions/index.md`](projects/db-lifecycle-extensions/index.md) (permanent, never closeable — stage > task per `ia/specs/project-hierarchy.md`). Closes DB-backed lifecycle subsystem gaps surfaced post-pivot — 10 new MCP tools + 6 PG migrations + skill chain inline edits to remove documented workarounds. Sequenced Approach D (A→B→C) + 6 picked DB-arch leverage opportunities (#2, #4, #6, #7, #8, #10), under DEC-A18 lock. Stage 1 opened 2026-04-28 — 6 tasks filed below (TECH-2973..TECH-2978: `task_raw_markdown_write` MCP + 0041 migration / `master_plan_change_log` UNIQUE + 0042 / `stage_closeout_apply` PG-txn wrap + diagnose / `task_dep_register` Tarjan SCC / SKILL inline workaround removal / `arch_surfaces_backfill` MCP promotion). Stage 2 + Stage 3 file when Stage 1 → `In Progress`.

### Stage 1 — Mechanical fillers

## IA evolution lane

Evolve **Information Architecture** from doc retrieval → learning, bidirectional, graph-queryable platform. **TECH-77** (FTS) + **TECH-78** (skill chaining) independent. **TECH-79** (agent memory) + **TECH-80** (bidirectional IA) need Postgres tables (independent). **TECH-81** (knowledge graph) long-term — benefits from **TECH-77** index + **TECH-79** session data. **TECH-82** (gameplay entity model) bridges IA tooling + game data. **TECH-83** (sim param tuning) uses bridge + optional **TECH-82** metrics tables. **TECH-552** (Unity Agent Bridge program — MCP + Editor queue hardening / transport / depth tiers per [`docs/unity-ide-agent-bridge-analysis.md`](docs/unity-ide-agent-bridge-analysis.md) §10). **Context:** [`docs/ia-system-review-and-extensions.md`](docs/ia-system-review-and-extensions.md). **Overview:** [`docs/information-architecture-overview.md`](docs/information-architecture-overview.md).

## Prototype-first methodology rollout

**Master plan:** `prototype-first-methodology` — codify the meta-result already shipped in `docs/prototype-first-methodology-design.md` `## Design Expansion`: amend `design-explore` Phase 9 persist contract so future explorations emit `§Core Prototype + §Iteration Roadmap` subsections. **Stage 1.1** (self-application codified — DEC-A22 surface re-point + persist-contract v2 fixture + arch_changelog audit row).

## Architecture coherence program

**Master plan:** `architecture-coherence-system` — split `ARCHITECTURE.md` into `ia/specs/architecture/{layers,data-flows,interchange,decisions}.md` sub-specs, DB-index arch surfaces + decisions + changelog, ship `/arch-drift-scan` skill + `/design-explore` Architecture Decision phase + 4 MCP tools to keep planning aligned. **Stage 1.1** (doc split + migration 0032) + **Stage 1.2** (plan-arch backfill + Stage block schema) shipped. **Stage 1.3** in progress: 4 read-side MCP tools + drift-scan skill.

## UI-as-code program (exploration)

**Charter (§ Completed — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) **Recent archive**):** Reduce **manual Unity Editor** work for **HUD** / **menus** / **panels** / **toolbars** — make **UI** composable from **IDE** (Cursor) + **AI agents**. Shipped: **reference spec** (**`ui-design-system.md`**), **runtime** **`UiTheme`** + **`UIManager` partials** + prefab **v0**, **Editor** menus (**`unity-development-context.md`** **§10**), **Cursor Skills**, optional **territory-ia** affordances. **UI** spans **multiple scenes**; **UI** inventory export + spec prose **per scene**. **As-built baseline:** **`ui-design-system.md`** + committed [`docs/reports/ui-inventory-as-built-baseline.json`](docs/reports/ui-inventory-as-built-baseline.json). **Codebase inventory (uGUI):** **`ui-design-system.md`** **Related files**. **Ongoing:** refresh **inventory** + baseline JSON when hierarchies shift; optional **`ui_theme_tokens` MCP** — new **BACKLOG** row if product wants it.

## Economic depth lane

Transform economy from "money goes up forever" → genuine city-builder sim w/ tension, feedback loops, player-visible consequences. **Sequential dependency order:** dynamic happiness (done — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md)) → **monthly maintenance** (shipped — **glossary** **Monthly maintenance**) → **tax→demand feedback** (shipped — **managers-reference** **Demand (R / C / I)**; [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md)) → **FEAT-09** (trade/production — deep economy, moved from § Low Priority). **FEAT-52** (city services coverage) + **FEAT-53** (districts) extend spatial economic depth. **Context:** [`docs/ia-system-review-and-extensions.md`](docs/ia-system-review-and-extensions.md) §4.

## Gameplay & simulation lane

Player-facing **simulation**, **AUTO** growth, **urban growth rings** / **zone density** depth. **Economic** issues → **§ Economic depth lane** above. **§ High Priority** still holds map/render/save **interrupt** bugs.

## Multi-scale simulation lane

Orchestrator: [`ia/projects/multi-scale-master-plan.md`](projects/multi-scale-master-plan.md) (permanent, never closeable — step > stage > phase > task per `ia/rules/project-hierarchy.md`). Step 1 = parent-scale conceptual stubs (code + save surfaces only; no playable parent scales). Stage 1.1 = parent-scale identity fields — archived. Stage 1.2 = cell-type split — archived. Stage 1.3 = neighbor-city stub + interstate-border semantics — filed below. Step 2 = City MVP close. Stage 4 = bug stabilization — archived. Stage 5 = tick performance + metrics foundation — filed below (TECH-1471..TECH-1474).

### Stage 1.3 — Neighbor-city stub + interstate-border semantics

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 5 — City MVP close / Tick performance + metrics foundation

### Stage 6 — City MVP close / City readability dashboard

## Distribution program — Full-Game MVP Bucket 10

Orchestrator: [`ia/projects/distribution-master-plan.md`](projects/distribution-master-plan.md) (permanent, never closeable — step > stage > phase > task per `ia/rules/project-hierarchy.md`). Bucket 10 of full-game-mvp umbrella (Tier E — unsigned installer tier for curated 20–50 testers; signing / Linux / WebGL / patch deltas / Steam / public itch deferred). Exploration: [`docs/distribution-exploration.md`](docs/distribution-exploration.md) §Design Expansion. Step 1 = Unity build pipeline + versioning manifest. Step 2 = unsigned packaging + `/download` publication + in-game notifier. Stage 1.1 opened 2026-04-18 — 4 tasks filed below (TECH-347..TECH-350: BuildInfo SO type + asset + SemverCompare helper + distribution glossary rows).

### Stage 1.1 — BuildInfo SO + semver compare helper

## CityStats overhaul program

Orchestrator: [`ia/projects/citystats-overhaul-master-plan.md`](projects/citystats-overhaul-master-plan.md) (permanent, never closeable — step > stage > phase > task per `ia/rules/project-hierarchy.md`). Bucket 8 of full-game-mvp umbrella (Tier D — execution gated on downstream triggers; filing now for IA alignment). Replace `CityStats` god-class w/ typed read-model facade (`CityStatsFacade`) + columnar ring-buffer store (`ColumnarStatsStore`); migrate consumers; add region/country rollup facades; surface web stats route. **Stage 1.1 (Core types) closed 2026-04-21** — TECH-303, TECH-304 archived. Next: `/stage-file ia/projects/citystats-overhaul-master-plan.md` Stage 2 (CityStatsFacade + tick bracket; tasks still `_pending_`).

### Stage 1.1 — Core types (IStatsReadModel, StatKey, ColumnarStatsStore) — **closed**

## City-Sim Depth program

Orchestrator: [`ia/projects/city-sim-depth-master-plan.md`](projects/city-sim-depth-master-plan.md) (permanent, never closeable — step > stage > phase > task per `ia/rules/project-hierarchy.md`). Bucket 2 of full-game-mvp umbrella. Shared 12-signal simulation contract + district aggregation + `HappinessComposer` / `DesirabilityComposer` migration + 7 new simulation sub-surfaces + signal overlays + HUD/district panel parity. Step 1 = Signal Layer Foundation. Stage 1.1 opened 2026-04-17 — 4 tasks filed below (TECH-305..TECH-308: `SimulationSignal` enum + producer/consumer interfaces + `SignalField` + `SignalMetadataRegistry` SO + `SignalFieldRegistry` MonoBehaviour + `ia/specs/simulation-signals.md` reference spec).

### Stage 1.1 — Signal Contract Primitives

## Utilities program

Orchestrator: [`ia/projects/utilities-master-plan.md`](projects/utilities-master-plan.md) (permanent, never closeable — step > stage > phase > task per `ia/rules/project-hierarchy.md`). Bucket 4a of full-game-mvp umbrella. Country-pool-first water / power / sewage w/ local contributor buildings feeding per-scale pools (city / region / country). EMA soft warning → cliff-edge deficit (freeze + happiness decay + desirability decay). Stage 1.1 opened 2026-04-17 — 4 tasks filed below (TECH-331..TECH-334: `UtilityKind` / `ScaleTag` / `PoolStatus` enums + `PoolState` struct + `IUtilityContributor` / `IUtilityConsumer` interfaces + assembly compile-check).

### Stage 1.1 — Data contracts + enums

## Skill training program

Orchestrator: [`ia/projects/skill-training-master-plan.md`](projects/skill-training-master-plan.md) (permanent, never closeable — step > stage > phase > task per `ia/rules/project-hierarchy.md`). Approach A two-skill split — structured JSON self-report emitter at Phase-N-tail of 13 lifecycle skills + `skill-train` consumer subagent (Opus, on-demand) that synthesizes recurring friction into patch proposals for SKILL.md bodies, gated by user review. Exploration: [`docs/skill-training-exploration.md`](docs/skill-training-exploration.md) §Design Expansion. Stage 1.1 opened 2026-04-18 — 4 tasks filed below (TECH-367..TECH-370: glossary rows × 4 + agent-lifecycle surface-map row + CLAUDE.md §3 pointer + AGENTS.md retrospective paragraph). Satisfies invariant #12 — terminology lands before Stage 1.2 or Step 2 authors cross-refs.

### Stage 1.1 — Glossary + Docs Foundation

## Blip audio program

Orchestrator: [`ia/projects/blip-master-plan.md`](projects/blip-master-plan.md) (permanent, never closeable — step > stage > phase > task per `ia/rules/project-hierarchy.md`). Step 1 = DSP foundations + audio infra (all four stages archived). Step 2 in progress — Stage 2.1 archived. Stage 2.2 archived 2026-04-15 (TECH-169..TECH-174). Stage 2.3 closed 2026-04-15 (TECH-188..TECH-191 all archived). Stage 2.4 closed 2026-04-15 (TECH-196..TECH-199 all archived). Step 3 opened 2026-04-15 — Stage 3.1 closed 2026-04-15 (TECH-209..TECH-212 all archived). Stage 3.2 closed 2026-04-15 (TECH-215..TECH-218 all archived). Stage 3.3 closed 2026-04-16 (TECH-219..TECH-222 all archived). Stage 3.4 closed 2026-04-16 (TECH-227..TECH-230 archived). Step 4 opened 2026-04-16 — Stage 4.1 closed 2026-04-16 (TECH-235..TECH-238 all archived). Stage 4.2 closed 2026-04-16 (TECH-243..TECH-246 all archived — `BlipVolumeController` logic bodies + `SfxMutedKey` boot-time restore + glossary update). Step 5 = DSP kernel v2 (post-MVP FX chain + LFOs + biquad BP + param smoothing). Stage 5.1 opened 2026-04-16 — 5 tasks filed below (FX data model + memoryless cores: BitCrush / RingMod / SoftClip / DcBlocker; delay-line kinds stubbed to passthrough until Stage 5.2). Stage 5.2 opened 2026-04-16 — 6 tasks filed below (TECH-270..TECH-275: `BlipDelayPool` service + `Render` delay-buffer overload + `BlipBaker` lease-on-bake + comb / allpass / chorus / flanger kernels + NoAlloc chorus gate).

### Stage 3.1 — Patch authoring + catalog wiring

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 3.2 — UI + Eco + Sys call sites

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 3.3 — World lane call sites

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 3.4 — Golden fixtures + spec promotion + glossary

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_


### Stage 1.1 — Audio infrastructure + persistent bootstrap

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 1.2 — Patch data model

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 1.3 — Voice DSP kernel

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 1.4 — EditMode DSP tests

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 2.1 — Bake-to-clip pipeline

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 2.2 — Catalog + mixer router + cooldown registry + player pool

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 2.3 — BlipEngine facade + main-thread gate

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 2.4 — PlayMode smoke test

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 4.1 — Options panel UI (slider + mute toggle + controller stub)

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 4.2 — Settings controller + persistence + mute semantics

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 5.1 — FX data model + memoryless cores

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 5.2 — Delay-line FX + BlipDelayPool

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 5.3 — LFOs + routing matrix + param smoothing

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

## Music audio program

Orchestrator: [`ia/projects/music-player-master-plan.md`](projects/music-player-master-plan.md) (permanent, never closeable — step > stage > phase > task per `ia/rules/project-hierarchy.md`). Step 1 = audio infra + playlist pipeline. Stage 1.1 opened 2026-04-17 — 6 tasks filed below (TECH-316..TECH-321: Blip-Music mixer group + Master/Music param exposure + `MusicBootstrap` consts + Awake shape + prefab + MainMenu placement). Stages 1.2 / 2.x / 3.x remain in master plan; file rows when parent stage → `In Progress`.

### Stage 1.1 — Mixer extension + persistent bootstrap

## Sprite gen lane

Orchestrator: [`ia/projects/sprite-gen-master-plan.md`](projects/sprite-gen-master-plan.md) (permanent, never closeable — step > stage > phase > task per `ia/rules/project-hierarchy.md`). Step 1 = Geometry MVP. Stages 1.1–1.2 archived (TECH-123..TECH-128, TECH-147..TECH-152 in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md)). Stage 1.3 opened 2026-04-15 — 6 tasks filed below (K-means extractor + palette CLI + apply_ramp + compose wiring + palette tests + bootstrap residential JSON + Tier 1 `.gpl` round-trip). T1.3.3+T1.3.4 merged into TECH-155 (apply_ramp API + compose wiring — tight coupling, single commit unit); T1.3.7+T1.3.8+T1.3.9 merged into TECH-158 (GPL export + import + round-trip test — must land atomic for symmetry). Stage 1.4 opened 2026-04-15 — 9 tasks filed below (slopes.yaml + iso_stepped_foundation + compose auto-insert + slope regression tests + Unity meta writer + promote/reject CLI + Aseprite bin resolver + layered .aseprite emit + promote --edit round-trip). Steps 2–3 remain in master plan; file rows when parent stage → `In Progress`.

### Stage 1.1 — Scaffolding + Primitive Renderer (Layer 1)

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 1.2 — Composition + YAML Schema + CLI Skeleton (Layer 2)

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 1.3 — Palette System (Layer 3)

_(all tasks archived — see `BACKLOG-ARCHIVE.md`)_

### Stage 1.4 — Slope-Aware Foundation + Curation CLI (Layer 5)

## Web platform lane

Orchestrator: [`ia/projects/web-platform-master-plan.md`](projects/web-platform-master-plan.md) (permanent, never closeable — step > stage > phase > task per `ia/rules/project-hierarchy.md`). Step 1 = Scaffold + design system foundation. Stage 1.1 closed (see BACKLOG-ARCHIVE.md). Stage 1.2 closed 2026-04-14 — tokens + Tailwind wiring task + DataTable/BadgeChip + StatBar/FilterChips + HeatmapCell/AnnotatedMap + `/design` review route + README §Tokens all archived (see BACKLOG-ARCHIVE.md). Step 2 closed 2026-04-15 — Stage 2.1 (MDX pipeline + public pages + SEO — TECH-163…TECH-168), Stage 2.2 (wiki + glossary auto-index + search — TECH-184…TECH-187), Stage 2.3 (devlog + RSS + origin story — TECH-192…TECH-195) all archived. Step 3 Stage 3.1 closed 2026-04-15 — plan loader + typed schema (TECH-200…TECH-203 archived). Stage 3.2 closed 2026-04-15 — dashboard RSC + filters (T3.2.1 + T3.2.2 + T3.2.3 + T3.2.4 archived). Stage 3.3 closed 2026-04-15 — legacy handoff + E2E smoke + deprecation log (TECH-213 + TECH-214 archived). Step 4 Stage 4.1 closed 2026-04-16 — nav sidebar + icon system (TECH-223 + TECH-224 + TECH-225 + TECH-226 all archived). Stage 4.2 closed 2026-04-16 — UI primitives polish + dashboard percentages (TECH-231 + TECH-232 + TECH-233 + TECH-234 all archived 2026-04-16). Stage 4.3 closed 2026-04-16 — D3 PlanChart grouped-bar chart (TECH-239 + TECH-240 + TECH-241 + TECH-242 all archived 2026-04-16). Stage 4.4 closed 2026-04-16 — multi-select dashboard filtering (TECH-247 + TECH-248 + TECH-249 + TECH-250 all archived 2026-04-16). Stage 5.1 closed 2026-04-16 — Postgres provider + auth library selection. TECH-252 + TECH-253 + TECH-254 + TECH-255 all archived 2026-04-16 (Neon free + roll-own JWT + sessions + `web/lib/db/client.ts` lazy driver wiring + `web/README.md §Portal` contributor doc landed). Stage 5.2 opened 2026-04-16 — 4 tasks filed (drizzle schema + `db:generate` script + 4 stub auth route handlers; no migrations run, no real auth flow — architecture-only per orchestrator §Step 5). Stage 5.3 closed 2026-04-17 — Phase 0 (TECH-269), Phase 1 (TECH-265 + TECH-266), Phase 2 (TECH-267 + TECH-268) all archived; presence-only cookie check w/ `DASHBOARD_AUTH_SKIP=1` local-dev bypass, no signature verify — architecture-only per orchestrator §Step 5. Next.js 16 middleware → proxy rename absorbed during TECH-268 smoke (see `ia/projects/web-platform-master-plan.md` §Step 5 Status).

### Stage 5.2 — Auth API stubs + schema draft

### Stage 5.3 — Dashboard auth middleware migration

### Stage 6.1 — Playwright e2e harness: install + config + CI wiring

### Stage 6.2 — Baseline route coverage

### Stage 6.3 — Dashboard e2e (SSR filter flows)

### Stage 7.1 — Registry + pure shapers


### Grid asset visual registry — Step 1 Stage 1.2 (hand-written DTOs, no Drizzle)

Orchestrator: [`ia/projects/grid-asset-visual-registry-master-plan.md`](../ia/projects/grid-asset-visual-registry-master-plan.md). **SQL** in `db/migrations/` is authoritative; `web/types/api/catalog*.ts` hand DTOs per **architecture audit 2026-04-22**; depends on **TECH-612** (0011) / **TECH-615** (0012) migrations archived.


### Web platform — Stage 24 (CD bundle extraction + transcription pipeline)

## High Priority

<!-- zone-s-economy master plan — Stage 1.1 (orchestrator: `ia/projects/zone-s-economy-master-plan.md`; Bucket 3 of full-game MVP umbrella) -->

## Medium Priority
<!-- zone-s-economy master plan — Stage 1.1 (orchestrator: `ia/projects/zone-s-economy-master-plan.md`; Bucket 3 of full-game MVP umbrella) -->

## Code Health (technical debt)

*(Umbrella programs (**spec-pipeline**, **JSON**/**Postgres** interchange, **compute-lib**, **Cursor Skills**) and **Editor export registry** are archived under [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) with **glossary** pointers. **§ IA evolution lane** holds **TECH-77**–**TECH-83** + **TECH-552** (FTS, skill chaining, agent memory, bidirectional IA, knowledge graph, gameplay entity model, sim parameter tuning, Unity Agent Bridge). **§ Economic depth lane** holds **monthly maintenance** (shipped — **glossary**) → **tax→demand feedback** (shipped — **managers-reference** **Demand**) → **FEAT-52** → **FEAT-53** → **FEAT-09** (happiness + pollution shipped). **§ Gameplay & simulation lane** lists **BUG-52**, **FEAT-43**, **FEAT-08**. **§ Compute-lib program** above holds **TECH-38** + **TECH-32**/**TECH-35**; **TECH-39** **MCP** suite is archived.)*

## Low Priority

*(Program history: [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md). Open lanes: **§ Compute-lib program**, **§ Agent ↔ Unity & MCP context lane**, **§ IA evolution lane**, **§ Economic depth lane**, **§ Gameplay & simulation lane**, **§ Multi-scale simulation lane**, **§ Blip audio program**, **§ Sprite gen lane**, **§ Web platform lane**, then standard priority sections.)*

---

## How to Use This Backlog

1. **Work on issue:** Open chat in Cursor, reference `@BACKLOG.md`, request analysis / implementation by ID (e.g. "Analyze BUG-01, propose plan").
2. **Reprioritize:** Move row up/down within section, or change section.
3. **Add new issue:** Next available ID per category, place in correct priority section.
4. **Complete issue:** Remove row from **BACKLOG.md**; append **`[x]`** row w/ date to [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) only (**no** "Completed" section in **BACKLOG.md**). After closure, **strip citations** to that issue id from durable docs (glossary, reference specs, rules, skills, `docs/`, code comments) per **project-spec-close** — **BACKLOG.md** (open rows), **BACKLOG-ARCHIVE.md**, new archived row may still name id.
5. **In progress:** Move to "In progress" section when starting.
6. **Dependencies:** `Depends on: ID` when open issue waits on another. **Convention:** every ID in `Depends on:` must appear **above** the dependent in this file (earlier in same section / higher-priority section), **or** be **completed** in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) — then write `Depends on: none` + cite archived id in **Notes**. Check deps before starting.

### ID Convention
| Prefix | Category |
|--------|----------|
| `BUG-XX` | Bugs / broken functionality |
| `FEAT-XX` | Features / enhancements |
| `TECH-XX` | Technical debt, refactors, code health |
| `ART-XX` | Art assets, prefabs, sprites |
| `AUDIO-XX` | Audio assets / audio system |

### Issue Fields
- **Type:** fix, feature, refactor, art/assets, audio/feature, etc.
- **Files:** main files involved
- **Notes:** context, problem description, expected solution
- **Acceptance** (optional): concrete pass/fail criteria
- **Depends on** (optional): IDs that must complete first

### Section Order
1. **Compute-lib program** (**TECH-38** open; **TECH-39** archived; pilot **compute-lib** archived; related **TECH-32**, **TECH-35**; charter — [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md))
2. **Agent ↔ Unity & MCP context lane** (Unity exports, MCP, CI, perf harnesses, adjacent tooling)
3. In progress (active — insert above **High priority** when used)
4. High priority (critical bugs, core gameplay blockers)
5. Medium priority (important features, balance, improvements)
6. **Multi-scale simulation lane** (orchestrator [`ia/projects/multi-scale-master-plan.md`](projects/multi-scale-master-plan.md); file rows only when parent stage → `In Progress`)
7. **Blip audio program** (orchestrator [`ia/projects/blip-master-plan.md`](projects/blip-master-plan.md); file rows only when parent stage → `In Progress`)
8. **Sprite gen lane** (orchestrator [`ia/projects/sprite-gen-master-plan.md`](projects/sprite-gen-master-plan.md); file rows only when parent stage → `In Progress`)
9. **Web platform lane** (orchestrator [`ia/projects/web-platform-master-plan.md`](projects/web-platform-master-plan.md); file rows only when parent stage → `In Progress`)
9. Code Health (technical debt, refactors, performance)
9. Low priority (new systems, polish, content)
8. **Archive** — completed work lives only in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md)
