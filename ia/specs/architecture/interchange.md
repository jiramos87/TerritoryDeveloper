# Interchange

## Agent information architecture and MCP

Authoritative **agent-facing** content lives in `ia/specs/`, `ia/rules/`, [`AGENTS.md`](../../../AGENTS.md), and the root `ARCHITECTURE.md` index. [`ia/rules/agent-router.md`](../../rules/agent-router.md) maps tasks to specs. For a holistic overview of the IA system — philosophy, layers, knowledge lifecycle, extension checklists, and **autoreference** of the stack — see [`docs/information-architecture-overview.md`](../../../docs/information-architecture-overview.md). **Agent-led verification** (Unity batch + IDE bridge, **Verification** block in agent messages): [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md).

The **territory-ia** MCP server ([`tools/mcp-ia-server/`](../../../tools/mcp-ia-server/), configured in [`.mcp.json`](../../../.mcp.json)) exposes that corpus through tools (`backlog_issue` for [`BACKLOG.md`](../../../BACKLOG.md) by issue id, plus `list_specs`, `spec_outline`, `spec_section`, `spec_sections`, `project_spec_closeout_digest`, `project_spec_journal_persist` / `project_spec_journal_search` / `project_spec_journal_get` / `project_spec_journal_update` when a dev DB URL resolves — `glossary_lookup`, `glossary_discover`, `router_for_task`, `invariants_summary`, `list_rules`, `rule_content`, `isometric_world_to_grid`) so agents can fetch slices without reading whole files. **Computational** math for **`isometric_world_to_grid`** lives in [`tools/compute-lib/`](../../../tools/compute-lib/) (**npm** **`territory-compute-lib`**); gameplay **grid** authority remains **C#**. It does not change Unity runtime architecture. Overview: [`docs/mcp-ia-server.md`](../../../docs/mcp-ia-server.md). A **domain-agnostic** description of the same file-backed IA + MCP pattern (reusable in other repos) is in [`docs/mcp-markdown-ia-pattern.md`](../../../docs/mcp-markdown-ia-pattern.md). **Integrated tooling and verification task order** (scripts, CI, MCP, Unity exports): [`docs/agent-tooling-verification-priority-tasks.md`](../../../docs/agent-tooling-verification-priority-tasks.md).

## JSON interchange (completed program)

JSON Schema + **CI** **`validate:fixtures`**, **Geography initialization** / Editor tooling payloads, **Postgres interchange patterns** (**B1**/**B3**/**P5**) in [`docs/postgres-interchange-patterns.md`](../../../docs/postgres-interchange-patterns.md). **Postgres** dev surfaces: **`db/migrations/`**, [`docs/postgres-ia-dev-setup.md`](../../../docs/postgres-ia-dev-setup.md), **`tools/postgres-ia/`**, [`config/postgres-dev.json`](../../../config/postgres-dev.json). **Charter trace:** [`BACKLOG-ARCHIVE.md`](../../../BACKLOG-ARCHIVE.md). Generated indexes are **supplementary** to Markdown and MCP; they do not replace **`list_specs`** / **`spec_section`** as authoritative sources.

## Local verification (post-implementation)

| Command | Role |
|---------|------|
| **`npm run verify:local`** | **Canonical** dev-machine chain: **`validate:all`** (dead project-spec paths, **`npm run compute-lib:build`**, **`test:ia`**, **`validate:fixtures`**, **`generate:ia-indexes --check`**) then [`tools/scripts/post-implementation-verify.sh`](../../../tools/scripts/post-implementation-verify.sh) with **`--skip-node-checks`** (**`unity:compile-check`**, **`db:migrate`**, **`db:bridge-preflight`**, **macOS** Editor save/quit + relaunch + **`db:bridge-playmode-smoke`**; **non-macOS** stops after **`db:bridge-preflight`** — see script). Implemented by [`tools/scripts/verify-local.sh`](../../../tools/scripts/verify-local.sh). Optional seed: **`npm run verify:local -- "x,y"`**. |
| **`npm run verify:post-implementation`** | Alias for **`verify:local`**. |
| **`npm run validate:all`** | **IA tools** subset only (no Unity / Postgres bridge). **`compute-lib:build`** matches **CI** ordering before **`test:ia`** ([`.github/workflows/ia-tools.yml`](../../../.github/workflows/ia-tools.yml)). |
| **`npm run unity:testmode-batch`** | **Agent test mode batch** (glossary): headless **Editor** load smoke on **committed scenarios** — **`tools/scripts/unity-testmode-batch.sh`**, **`AgentTestModeBatchRunner.Run`**, report under **`tools/reports/`** (optional **`--golden-path`** / integer **CityStats** assert, exit **8** on mismatch). Not the **Postgres** **IDE agent bridge** queue. Matrix and flags: [`tools/fixtures/scenarios/README.md`](../../../tools/fixtures/scenarios/README.md). |
| **`npm run unity:build-scenario-from-descriptor`** | **Scenario descriptor** batch (glossary **scenario_descriptor_v1**): headless **Editor** applies a committed **`scenario_descriptor_v1`** JSON then writes **`GameSaveData`** — **`tools/scripts/unity-build-scenario-from-descriptor.sh`**, **`ScenarioDescriptorBatchBuilder.Run`**. See [`tools/fixtures/scenarios/BUILDER.md`](../../../tools/fixtures/scenarios/BUILDER.md). |

**Agent test-mode verification (Cursor skill):** gate, **Path A** (**Agent test mode batch**) vs **Path B** (**IDE agent bridge**), **`validate:all`** / compile gates, bounded iterate, handoff for human **normal-game** **QA** — [`ia/skills/agent-test-mode-verify/SKILL.md`](../../skills/agent-test-mode-verify/SKILL.md).

**Not for CI.** Workflow notes: [`docs/mcp-ia-server.md`](../../../docs/mcp-ia-server.md), [`ia/skills/project-implementation-validation/SKILL.md`](../../skills/project-implementation-validation/SKILL.md).

## Interchange artifacts registry

| Artifact slug | Path | Schema version | Producer | Consumer |
|---|---|---|---|---|
| `feature-flags-snapshot` | `tools/interchange/feature-flags-snapshot.json` | 1 | MCP tool `feature_flags_snapshot_write` / web build step | `FeatureFlags.HydrateFromJson` (Unity runtime); bridge `flag_flip` re-triggers hydration |

### `feature-flags-snapshot` schema (v1)

```json
{
  "artifact": "feature-flags-snapshot",
  "schema_version": 1,
  "generated_at": "<ISO-8601 UTC>",
  "flags": [
    { "slug": "<kebab-slug>", "enabled": true, "default_value": false }
  ]
}
```

`flags[]` — one entry per `ia_feature_flags` row; `default_value` = fallback when Unity cannot read the file at boot.

## MCP tool catalog (Stage 1.3)

_pending_ — Stage 1.3 populates rows for `arch_decision_get`, `arch_decision_list`, `arch_surface_resolve`, `arch_drift_scan`, `arch_changelog_since` once tools land under `tools/mcp-ia-server/src/tools/arch.ts`.
