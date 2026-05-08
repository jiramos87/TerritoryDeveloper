# Compaction-Loop Mitigation — Infrastructure Approaches

> **Status (2026-05-08):** Tier A4 shipped (TECH-846). Tier A1+A3+C4 bundle filed (TECH-847), deferred to atomization mid-sweep. Tier A2 dropped (harness gap). Tier B3 in flight as `large-file-atomization-refactor` master plan (Stages 1–5 shipped).

## Problem

Claude Code auto-compaction summarizes prior turns when context fills. Summary itself can be huge (the hud-bar audit summary = ~9KB dense prose: file:line citations, verbatim quotes, full bug catalog inline, multi-file recaps). Once summary alone consumes 15–20% of fresh context, second compaction triggers near-immediately = compaction loop. Agent burns turns re-summarizing instead of working.

## Root causes (territory-developer specific)

1. **Mega-files**: `UiBakeHandler.cs` (1361 lines), `Archetype.cs` (1159 lines) — full reads dominate context. Compaction must restate findings inline because "Read X" markers don't preserve agent's analysis.
2. **No structured working memory**: agent's bug catalog lives only in chat prose. Compaction must inline it verbatim or lose it.
3. **No checkpoint discipline**: long investigations (audit, design-explore) accumulate findings turn-by-turn with no append-only sink. Compaction = only persistence path.
4. **csharp_class_summary underused**: tool exists, agents still default to full `Read` of 1000+ line files.
5. **No pre-compact warning surface**: agent has no signal "context at 80%, persist state now". Harness silently triggers compact when limit hit.
6. **Skill chains write IA but not session state**: `journal_append` / `project_spec_journal_persist` exist but aren't part of investigation skill bodies.

## Approaches (ranked by leverage × implementation cost)

### Tier A — high leverage, low cost

| # | Approach | Surface | Mechanism | Status |
|---|---|---|---|---|
| A1 | **`scratchpad_ledger` MCP tool** | `tools/mcp-ia-server/src/index.ts` + new `ia_scratchpad_ledger` table (mig 0111) | Append-only structured rows `(session_id, slug, kind, payload_json)` for active investigations. Kinds: `file_read`, `decision`, `bug_found`, `pending_task`. Skill `audit-mode` requires every finding → `scratchpad_ledger_append`. Compaction summary = pointer `scratchpad_ledger_get("hud-bar-audit")` instead of inline catalog. Cuts 9KB → ~300 bytes. | **TECH-847 filed** — deferred to atomization mid-sweep (~Stage 10) |
| A2 | ~~Pre-compact warning hook~~ | ~~`.claude/settings.json` PreToolUse + new `tools/scripts/claude-hooks/context-pressure.sh`~~ | ~~Hook reads token-count signal~~ | **DROPPED** — `transcript_token_count` not exposed to Claude Code hooks. Substituted with C4. |
| A3 | **`audit-mode` skill** | `ia/skills/audit-mode/SKILL.md` | Wraps "find all bugs in X" pattern. Phases: enumerate target files → per-file read+ledger-append → final doc render from ledger. Doc rendered from DB rows = deterministic size. Replaces ad-hoc audit prose. | **TECH-847 filed** — bundled with A1 + C4 |
| A4 | **Force `csharp_class_summary` for big files** | `tools/scripts/claude-hooks/big-file-read-warn.sh` PreToolUse | Hook intercepts `Read` on `Assets/**/*.cs` >800 lines; suggests `csharp_class_summary` first. Optional `BIG_FILE_READ_OK=1` escape. | **SHIPPED — TECH-846** (commit `2277a5f3`). Restart Claude Code to load Read matcher. |

### Tier B — medium leverage, medium cost

| # | Approach | Surface | Mechanism | Status |
|---|---|---|---|---|
| B1 | **`context_checkpoint` MCP tool** | New table `ia_context_checkpoints` | Agent calls explicitly before risky multi-file reads. Stores `{slug, summary_md, refs[]}` rows. Compaction summary cites checkpoint id; harness can re-inject checkpoint markdown verbatim post-compact. | Deferred — revisit only if A-tier ROI insufficient |
| B2 | **Force-subagent dispatch for bulk reads** | New skill `bulk-read-broker` | If task needs >3 files >500 lines: skill dispatches subagent (`Explore` / `general-purpose`), main session sees only return summary. Same pattern as `release-rollout` already uses. | Deferred — partial overlap with existing subagent dispatch |
| B3 | **Mega-file split refactor** | Strategy γ atomization sweep — split `TerrainManager.cs`, `RoadManager.cs`, `WaterMap.cs`, `GridManager.cs`, `UiBakeHandler.cs` etc. into Domains/{X}/ folder shape with facade interface + per-concern services. | Direct: smaller files = smaller reads = smaller compactions. Side benefit: easier maintenance. | **IN FLIGHT** — `large-file-atomization-refactor` master plan, Stages 1–5 shipped (Terrain/Roads/Water/GridManager). ~25–30 stages total. |
| B4 | **`session_state` MCP slice** | Extend `runtime_state` tool | Per-session JSON store keyed by session_id. Agent saves working hypothesis + open threads. Compaction includes only the key. | Deferred — partial overlap with A1 ledger; revisit post-A-tier |

### Tier C — exploratory / structural

| # | Approach | Surface | Mechanism | Status |
|---|---|---|---|---|
| C1 | **Auto-evict file reads from chat** | Hook PostToolUse on Read | After Read, if file >N lines, hook truncates the tool result to head/tail + footer "full content cached at `.claude/file-cache/{hash}.md`". Forces summary discipline. Risky: agents may need full content for next step. | Deferred — risky |
| C2 | **Dedicated investigation subagent** | New `.claude/agents/investigator.md` | Long-running audits delegated wholesale. Subagent owns bug catalog. Returns final doc + 200-token summary. Main session never sees raw findings. | Deferred — A3 `audit-mode` covers this lane |
| C3 | **Compaction-aware preamble** | `ia/skills/_preamble/stable-block.md` extension | Add tier "if you are summarizing for compaction → emit pointer-only shape, refer agent to ledger/checkpoint". Trains compaction LLM to produce smaller summaries. Effect uncertain — compaction prompt is harness-side. | Deferred — effect uncertain, revisit after A-tier ROI measured |
| C4 | **Output-style: `audit-summary`** | `.claude/output-styles/audit-summary.md` | Force structured table shape for audit output. Deterministic compression vs free-form prose. | **TECH-847 filed** — A2 substitute (table-shape forcing covers what pre-compact warn would have) |

## Decision log (2026-05-08)

User-locked decisions after live exploration:

1. **Ship A4 standalone first** as quick win during active atomization sweep — TECH-846, shipped commit `2277a5f3`.
2. **Bundle A1 + A3 + C4** (with C4 substituting for dropped A2) as one umbrella TECH — TECH-847 filed, deferred to atomization mid-sweep (~Stage 10).
3. **Drop A2** — `transcript_token_count` not exposed to Claude Code PreToolUse hook env. C4 (`audit-summary` output-style table-shape forcing) substitutes for the deterministic-compression slice A2 would have provided.
4. **Ledger rows carry both `session_id` and `slug` (nullable)** — enables cross-session handoff via slug-keyed re-entry while preserving session-scoped queries.
5. **Ledger TTL: durable** initially. Revisit if `ia_scratchpad_ledger` row count exceeds 100K.
6. **A2 revival path** — additive PreToolUse retrofit if Anthropic harness later exposes `transcript_token_count`. Track in TECH-847 changelog.

Net effect once TECH-847 ships: long investigations write findings to DB rows; compaction summary degenerates to "session_id=X, ledger=hud-bar-audit, 23 entries". Re-entry post-compact = `scratchpad_ledger_get` restores full catalog deterministically. Compaction loop broken because summary size becomes O(turn_count) not O(finding_size × turn_count).

## Sequencing relative to atomization sweep

1. **Stages 1–5 (shipped)**: Terrain / Roads / Water / GridManager domains atomized via Strategy γ. TECH-846 hook now nudges every subsequent stage toward `csharp_class_summary` first. Immediate token savings during the remaining ~20–25 stages.
2. **Stage ~10 (mid-sweep)**: start TECH-847 (A1 + A3 + C4 bundle). Ledger discipline absorbs remaining audit-style explorations between stages.
3. **Sweep completion**: revisit Tier B B1 (`context_checkpoint`) and B4 (`session_state`) only if A-tier insufficient.

## Open questions (resolved unless flagged)

- ~~Does Claude Code expose `transcript_token_count` to hooks?~~ **NO** — A2 dropped, C4 substituted.
- Compaction prompt — is it harness-controlled or model-default? If harness, C3 viable; if not, output-style (C4) is closest lever. **Status: open** — irrelevant to TECH-847 scope (C4 path forced).
- ~~Ledger TTL: per-session vs per-slug.~~ **Resolved: both nullable, durable retention.**

## Next

- TECH-846 — ship `2277a5f3` lands; user restart Claude Code so Read matcher activates.
- TECH-847 — pick up at atomization mid-sweep (~Stage 10). Run `/stage-authoring --task TECH-847` to populate §Plan Digest, then `/ship TECH-847`.
- Plan-of-record: `~/.claude-personal/plans/create-a-plan-for-splendid-fountain.md`.

## Appendix — repo size baseline (2026-05-08)

Survey to identify Tier B3 (mega-file split) candidates and estimate compaction-pressure surface area.

### Top 20 code files by LOC

| Lines | File |
|---|---|
| 4273 | `Assets/Scripts/Managers/GameManagers/TerrainManager.cs` |
| 3215 | `Assets/Scripts/Managers/GameManagers/RoadManager.cs` |
| 2325 | `Assets/Scripts/Managers/UnitManagers/WaterMap.cs` |
| 2319 | `Assets/Scripts/Managers/GameManagers/GridManager.cs` |
| 2285 | `tools/mcp-ia-server/src/ia-db/mutations.ts` |
| 1754 | `Assets/Scripts/Editor/AgentBridgeCommandRunner.cs` |
| 1520 | `tools/mcp-ia-server/dist/ia-db/mutations.js` (build artifact) |
| 1449 | `Assets/Scripts/Managers/GameManagers/ZoneManager.cs` |
| 1440 | `Assets/Scripts/Editor/Bridge/UiBakeHandler.cs` |
| 1386 | `Assets/Scripts/Editor/AgentBridgeCommandRunner.Mutations.cs` |
| 1318 | `Assets/Scripts/Managers/GameManagers/AutoRoadBuilder.cs` |
| 1284 | `Assets/Scripts/Managers/GameManagers/CityStats.cs` |
| 1267 | `tools/mcp-ia-server/src/tools/unity-bridge-command.ts` |
| 1221 | `Assets/Scripts/Editor/AgentTestModeBatchRunner.cs` |
| 1219 | `Assets/Scripts/Editor/AgentBridgeCommandRunner.Conformance.cs` |
| 1212 | `tools/sprite-gen/src/compose.py` |
| 1160 | `Assets/Scripts/Managers/GameManagers/GeographyManager.cs` |
| 1159 | `Assets/Scripts/Editor/Bridge/UiBakeHandler.Archetype.cs` |
| 1148 | `Assets/Scripts/Managers/GameManagers/InterstateManager.cs` |
| 1121 | `Assets/Scripts/Managers/GameManagers/RoadPrefabResolver.cs` |

### Top 20 doc files by LOC

| Lines | File | Status |
|---|---|---|
| 3016 | `BACKLOG-ARCHIVE.md` | generated |
| 2017 | `docs/ui-element-definitions.md` | live |
| 1646 | `docs/asset-pipeline-architecture.md` | live |
| 1201 | `docs/web-platform-post-mvp-extensions.md` | live |
| 1185 | `ia/state/pre-refactor-snapshot/ia/projects/web-platform-master-plan.md` | frozen |
| 1064 | `docs/full-game-mvp-exploration.md` | live |
| 1032 | `BACKLOG.md` | generated |
| 1013 | `docs/lifecycle-opus-planner-sonnet-executor-exploration.md` | live |
| 971 | `docs/region-scale-design.md` | live |
| 856 | `ia/state/pre-refactor-snapshot/ia/projects/blip-master-plan.md` | frozen |
| 851 | `docs/master-plan-foldering-refactor-design.md` | live |
| 849 | `docs/blip-procedural-sfx-exploration.md` | live |
| 816 | `docs/session-token-latency-audit-exploration.md` | live |
| 783 | `ia/specs/isometric-geography-system.md` | live |
| 734 | `docs/unity-ide-agent-bridge-analysis.md` | live |
| 725 | `docs/ia-dev-db-refactor-implementation.md` | live |
| 723 | `docs/prototype-first-methodology-design.md` | live |
| 720 | `ia/state/pre-refactor-snapshot/ia/projects/backlog-yaml-mcp-alignment-master-plan.md` | frozen |
| 715 | `docs/isometric-sprite-generator-exploration.md` | live |
| 695 | `ia/state/pre-refactor-snapshot/ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md` | frozen |

### Observations

- **Code clusters**: `GameManagers/*` dominates — `TerrainManager` (4.3K), `RoadManager` (3.2K), `WaterMap` + `GridManager` (~2.3K each). Bridge/bake layer also fat: `AgentBridgeCommandRunner` family (~4.4K across 3 files), `UiBakeHandler` family (~3.6K across 3 files).
- **Doc inflation**: 2 generated (`BACKLOG*.md`) + 5 frozen pre-refactor snapshots account for 5 of top 20. Live design docs >700 lines mostly under `docs/*-exploration.md` + `*-design.md`.
- **Compaction-pressure threshold**: any session reading 2+ of the >2K-line code files = guaranteed context strain. Single full Read of `TerrainManager.cs` ≈ 15–20% of compact-friendly window.
- **Tier B3 split candidates** (ranked by leverage): `TerrainManager`, `RoadManager`, `mutations.ts`, `WaterMap`, `GridManager`, `AgentBridgeCommandRunner`, `ZoneManager`, `UiBakeHandler`.
- **Tier A1 ledger reuse**: hud-bar audit catalog (~9KB) would compress to ~300 bytes via `scratchpad_ledger_get` pointer — same ratio applies to any of the >700-line exploration docs if authored ledger-first.
