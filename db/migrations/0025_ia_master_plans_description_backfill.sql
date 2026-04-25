-- 0025_ia_master_plans_description_backfill.sql
--
-- Backfill `description` for all 20 master plans authored before the column
-- existed (migration 0024 added it nullable). Each description distilled
-- case-by-case from the plan's preamble — short product overview + main
-- goals, ≤200 char soft target. Replaces preamble as primary dashboard
-- subtitle. Idempotent — only updates rows whose description IS NULL.

BEGIN;

UPDATE ia_master_plans SET description =
  'Align MCP `territory-ia` tool surface, type, validator, and skill docs with the per-issue yaml backlog refactor. Pure tooling — zero Unity / save-schema touches.'
  WHERE slug = 'backlog-yaml-mcp-alignment' AND description IS NULL;

UPDATE ia_master_plans SET description =
  'Procedural SFX synthesis subsystem — ten baked sounds from parameter-only patches, zero `.wav` / `.ogg` assets.'
  WHERE slug = 'blip' AND description IS NULL;

UPDATE ia_master_plans SET description =
  'Shared 12-signal simulation contract + district aggregation + 7 new sim sub-surfaces (pollution, crime, services, traffic, waste, construction, density) + overlays + HUD parity.'
  WHERE slug = 'city-sim-depth' AND description IS NULL;

UPDATE ia_master_plans SET description =
  'Replace `CityStats` god-class with typed read-model facade backed by columnar ring-buffer store + region/country rollup + new `web/app/stats` route.'
  WHERE slug = 'citystats-overhaul' AND description IS NULL;

UPDATE ia_master_plans SET description =
  'Unity build pipeline + unsigned mac/win installers + semver `BuildInfo` manifest + private `/download` web surface + in-game update notifier for 20–50 testers.'
  WHERE slug = 'distribution' AND description IS NULL;

UPDATE ia_master_plans SET description =
  'Umbrella orchestrator coordinating polished-ambitious MVP across 10+ buckets — tier lanes, cross-bucket deps, save-schema, stabilization, distribution gating.'
  WHERE slug = 'full-game-mvp' AND description IS NULL;

UPDATE ia_master_plans SET description =
  'Postgres-backed grid asset catalog (identity / sprites / economy / spawn pools) as source of truth — HTTP + MCP for agents, Unity boot snapshot consumed by `GridAssetCatalog`.'
  WHERE slug = 'grid-asset-visual-registry' AND description IS NULL;

UPDATE ia_master_plans SET description =
  'Landmarks v1 — tier-defining gifts on scale-tier transitions + intra-tier reward landmarks via bond-backed multi-month commission build, catalog-driven with sidecar JSON state.'
  WHERE slug = 'landmarks' AND description IS NULL;

UPDATE ia_master_plans SET description =
  'Big-bang collapse of Step/Stage/Phase/Task → Stage/Task hierarchy + Plan-Apply pair pattern (Opus heads / Sonnet tails) + Sonnet-ified spec enrichment + audit + code review.'
  WHERE slug = 'lifecycle-refactor' AND description IS NULL;

UPDATE ia_master_plans SET description =
  'Reshape `territory-ia` MCP surface from 4.6 sequential-call design to 4.7 composite-bundle + structured-envelope architecture across 32 tools.'
  WHERE slug = 'mcp-lifecycle-tools-opus-4-7-audit' AND description IS NULL;

UPDATE ia_master_plans SET description =
  'Min load-bearing work to prove the city ↔ region ↔ country game loop (dormant evolution + reconstruction).'
  WHERE slug = 'multi-scale' AND description IS NULL;

UPDATE ia_master_plans SET description =
  'Authored-jazz music subsystem — `.ogg` streaming, shuffle no-repeat-until-exhausted, `NowPlayingWidget`, Settings sliders, Credits screen, resume-by-track-id.'
  WHERE slug = 'music-player' AND description IS NULL;

UPDATE ia_master_plans SET description =
  'Token + latency remediation across MCP surface pruning, ambient context collapse, dispatch flattening, hook plane, repo hygiene, plus rev-4 larger bets.'
  WHERE slug = 'session-token-latency' AND description IS NULL;

UPDATE ia_master_plans SET description =
  'Two-skill split — JSON self-report at Phase-N-tail of 13 lifecycle skills + `skill-train` consumer subagent that synthesizes recurring friction into SKILL.md patch proposals.'
  WHERE slug = 'skill-training' AND description IS NULL;

UPDATE ia_master_plans SET description =
  'Python CLI + N-layer hybrid composer rendering isometric pixel art building sprites from YAML archetype specs — slope-aware foundations, palette mgmt, decoration, multi-footprint.'
  WHERE slug = 'sprite-gen' AND description IS NULL;

UPDATE ia_master_plans SET description =
  'Three concentric rings (token / primitive / juice) + flagship studio-rack polish on Main HUD + Toolbar + overlay toggles + CityStats handoff.'
  WHERE slug = 'ui-polish' AND description IS NULL;

UPDATE ia_master_plans SET description =
  'Tiered hardening + transport + optional depth on the shipped Postgres `agent_bridge_job` + `unity_bridge_command` / `unity_bridge_get` + `AgentBridgeCommandRunner`.'
  WHERE slug = 'unity-agent-bridge' AND description IS NULL;

UPDATE ia_master_plans SET description =
  'Water / power / sewage as country-pool-first resources with local contributor buildings, EMA soft warning → cliff-edge deficit, capacity-tier upgrades, landmarks plug-in.'
  WHERE slug = 'utilities' AND description IS NULL;

UPDATE ia_master_plans SET description =
  'Unified Next.js app at `web/` serving three audiences — public game site, live DevOps dashboard, future user portal — static-first hybrid on Vercel + Postgres.'
  WHERE slug = 'web-platform' AND description IS NULL;

UPDATE ia_master_plans SET description =
  'Zone S (state 4th zone channel, 7 sub-types) + economy depth — envelope budget allocator, floor-clamped treasury, single-bond-per-tier ledger, extended monthly-maintenance contract.'
  WHERE slug = 'zone-s-economy' AND description IS NULL;

COMMIT;
