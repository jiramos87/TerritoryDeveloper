-- Architecture index (TECH-2018 / Stage 1.1).
--
-- Per DEC-A16 db-relations-four-tables: arch_surfaces, arch_decisions,
-- arch_changelog, stage_arch_surfaces. Source-of-truth split (DEC-A10) —
-- humans edit ia/specs/architecture/*.md; this DB indexes relations + serves
-- agent queries via Stage 1.3 MCP tools (arch_decision_get, arch_decision_list,
-- arch_surface_resolve, arch_drift_scan, arch_changelog_since).
--
-- Idempotent: CREATE TABLE IF NOT EXISTS + INSERT ... ON CONFLICT DO NOTHING.
-- Re-run produces zero schema/row diff.
--
-- Seeds:
--   arch_surfaces — 9 rows from sub-spec section anchors (≥8 per Stage 1.1 exit).
--   arch_decisions — 17 rows from ia/specs/architecture/decisions.md (TECH-2006).
--     surface_id FK populated where decision maps to a surface; NULL otherwise.
--   arch_changelog — empty (populated by Stage 1.4 hook).
--   stage_arch_surfaces — empty (populated by Stage 1.2 backfill).
--
-- Migration slot 0034 (0032/0033 taken at fix time). Stage 1.2 storage-shape
-- migration shifts to 0035.

BEGIN;

-- arch_surfaces: catalog of architectural surfaces (one row per major
-- sub-spec section anchor). kind ∈ {layer, flow, contract, decision}.
CREATE TABLE IF NOT EXISTS arch_surfaces (
  id              bigserial PRIMARY KEY,
  slug            text NOT NULL UNIQUE,
  kind            text NOT NULL CHECK (kind IN ('layer', 'flow', 'contract', 'decision')),
  spec_path       text NOT NULL,
  spec_section    text,
  last_edited_at  timestamptz,
  created_at      timestamptz NOT NULL DEFAULT now()
);

COMMENT ON TABLE arch_surfaces IS
  'Architectural surfaces (one row per ia/specs/architecture/*.md section anchor). Stage 1.1 / TECH-2018.';

CREATE INDEX IF NOT EXISTS arch_surfaces_kind_idx ON arch_surfaces (kind);

-- arch_decisions: decisions table mirroring ia/specs/architecture/decisions.md.
-- status ∈ {active, superseded}. surface_id nullable (trade-offs may not map
-- to a single surface). slug unique per DEC-A17 table-driven shape.
CREATE TABLE IF NOT EXISTS arch_decisions (
  id              bigserial PRIMARY KEY,
  slug            text NOT NULL UNIQUE,
  title           text NOT NULL,
  status          text NOT NULL DEFAULT 'active' CHECK (status IN ('active', 'superseded')),
  rationale       text NOT NULL,
  alternatives    text,
  superseded_by   text,
  surface_id      bigint REFERENCES arch_surfaces (id) ON DELETE SET NULL,
  created_at      timestamptz NOT NULL DEFAULT now()
);

COMMENT ON TABLE arch_decisions IS
  'Architectural decisions (mirrors ia/specs/architecture/decisions.md table rows). Stage 1.1 / TECH-2018.';

CREATE INDEX IF NOT EXISTS arch_decisions_status_idx ON arch_decisions (status);
CREATE INDEX IF NOT EXISTS arch_decisions_surface_idx ON arch_decisions (surface_id);

-- arch_changelog: append-only history of arch surface edits + decisions.
-- kind ∈ {edit, decide, supersede}. Populated by Stage 1.4 post-write hook.
CREATE TABLE IF NOT EXISTS arch_changelog (
  id              bigserial PRIMARY KEY,
  surface_slug    text,
  decision_slug   text,
  kind            text NOT NULL CHECK (kind IN ('edit', 'decide', 'supersede')),
  commit_sha      text,
  body            text,
  created_at      timestamptz NOT NULL DEFAULT now()
);

COMMENT ON TABLE arch_changelog IS
  'Append-only history of arch surface edits + decisions. Populated by Stage 1.4 hook. Stage 1.1 / TECH-2018.';

CREATE INDEX IF NOT EXISTS arch_changelog_surface_idx ON arch_changelog (surface_slug);
CREATE INDEX IF NOT EXISTS arch_changelog_decision_idx ON arch_changelog (decision_slug);
CREATE INDEX IF NOT EXISTS arch_changelog_created_idx ON arch_changelog (created_at DESC);

-- stage_arch_surfaces: link table joining ia_stages → arch_surfaces.
-- Per DEC-A12 plan-arch-link-stage-level. Populated by Stage 1.2 backfill.
CREATE TABLE IF NOT EXISTS stage_arch_surfaces (
  stage_id        text NOT NULL,
  surface_slug    text NOT NULL REFERENCES arch_surfaces (slug) ON DELETE CASCADE,
  created_at      timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (stage_id, surface_slug)
);

COMMENT ON TABLE stage_arch_surfaces IS
  'Link table: ia_stages → arch_surfaces (DEC-A12 stage-level arch_surfaces[]). Populated by Stage 1.2 backfill. Stage 1.1 / TECH-2018.';

-- Seed arch_surfaces (≥8 rows per Stage 1.1 exit; 9 rows below).
INSERT INTO arch_surfaces (slug, kind, spec_path, spec_section) VALUES
  ('layers/system-layers',          'layer',    'ia/specs/architecture/layers.md',       'System Layers'),
  ('layers/helper-services',        'layer',    'ia/specs/architecture/layers.md',       'Helper Services'),
  ('layers/full-dependency-map',    'layer',    'ia/specs/architecture/layers.md',       'Full Dependency Map'),
  ('data-flows/initialization',     'flow',     'ia/specs/architecture/data-flows.md',   'Initialization'),
  ('data-flows/simulation',         'flow',     'ia/specs/architecture/data-flows.md',   'Simulation (per tick)'),
  ('data-flows/persistence',        'flow',     'ia/specs/architecture/data-flows.md',   'Persistence'),
  ('interchange/agent-ia',          'contract', 'ia/specs/architecture/interchange.md',  'Agent information architecture and MCP'),
  ('interchange/json-interchange',  'contract', 'ia/specs/architecture/interchange.md',  'JSON interchange (completed program)'),
  ('decisions/all',                 'decision', 'ia/specs/architecture/decisions.md',    NULL)
ON CONFLICT (slug) DO NOTHING;

-- Seed arch_decisions (17 rows from ia/specs/architecture/decisions.md).
-- surface_id resolved via subquery on slug; NULL where no obvious match.
INSERT INTO arch_decisions (slug, title, status, rationale, alternatives, surface_id) VALUES
  ('DEC-A1',  'gridmanager-as-hub',
              'active',
              'Central coordinator for cell operations gives consistent access surface; large class size accepted as current trade-off (see DEC-A10).',
              'decompose into N services; ECS; ServiceLocator',
              (SELECT id FROM arch_surfaces WHERE slug = 'layers/full-dependency-map')),
  ('DEC-A2',  'findobjectoftype-pattern',
              'active',
              'Inspector wiring + null-check fallback in Awake/Start avoids DI framework overhead and matches Unity-native conventions.',
              'constructor DI; ServiceLocator; ECS',
              (SELECT id FROM arch_surfaces WHERE slug = 'layers/full-dependency-map')),
  ('DEC-A3',  'namespaces-territory-prefix',
              'active',
              'Most code under Territory.* (Core, Terrain, Roads, Zones, Forests, Buildings, Economy, UI, Geography, Timing, Utilities, Simulation, Persistence). Few legacy in global namespace.',
              'flat namespace; per-feature root',
              (SELECT id FROM arch_surfaces WHERE slug = 'layers/system-layers')),
  ('DEC-A4',  'spec-policy-agents-md',
              'active',
              'AGENTS.md carries spec policy; spec inventory under ia/specs/; agent routing via ia/rules/agent-router.md. Optional MCP via docs/mcp-ia-server.md; generic pattern via docs/mcp-markdown-ia-pattern.md.',
              'inline policy in CLAUDE.md; per-spec headers',
              (SELECT id FROM arch_surfaces WHERE slug = 'interchange/agent-ia')),
  ('DEC-A5',  'editor-agent-diagnostics-ia',
              'active',
              'Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs emits JSON/Markdown under tools/reports/ (gitignored outputs). Field vocabulary in ia/specs/unity-development-context.md §10; regressions tracked via BACKLOG.',
              'runtime telemetry; manual exports',
              (SELECT id FROM arch_surfaces WHERE slug = 'interchange/agent-ia')),
  ('DEC-A6',  'ide-agent-bridge-postgres',
              'active',
              'unity_bridge_command, unity_bridge_get, unity_compile enqueue work for AgentBridgeCommandRunner via agent_bridge_job. Bridge kind values include get_compilation_status. Play Mode smoke sequence reduces manual Play/Stop clicks.',
              'filesystem queue; named pipes; HTTP service',
              (SELECT id FROM arch_surfaces WHERE slug = 'interchange/agent-ia')),
  ('DEC-A7',  'high-coupling-accepted',
              'active',
              'Managers reference each other directly. Trade-off: simpler Inspector wiring vs harder unit isolation.',
              'event bus; mediator pattern',
              (SELECT id FROM arch_surfaces WHERE slug = 'layers/full-dependency-map')),
  ('DEC-A8',  'gridmanager-size-accepted',
              'active',
              'GridManager ~2070 lines; decomposition tracked in BACKLOG. Trade-off: hub clarity vs single-class growth.',
              'split per concern (cell ops / dispatch / queries)',
              (SELECT id FROM arch_surfaces WHERE slug = 'layers/full-dependency-map')),
  ('DEC-A9',  'no-event-system',
              'active',
              'Direct method calls between managers. Trade-off: explicit data flow vs subscriber decoupling.',
              'C# events; UniRx; SignalBus',
              (SELECT id FROM arch_surfaces WHERE slug = 'layers/full-dependency-map')),
  ('DEC-A10', 'source-of-truth-split',
              'active',
              'Doc-primary, DB-indexed: humans edit markdown, DB indexes relations. Keeps git-blame on architectural prose; SQL serves agent queries.',
              'DB-primary (markdown rendered from rows); doc-only (no DB); dual-write hard sync',
              (SELECT id FROM arch_surfaces WHERE slug = 'decisions/all')),
  ('DEC-A11', 'doc-home-architecture-subdir',
              'active',
              'ia/specs/architecture/{layers,data-flows,interchange,decisions}.md is first-class IA. Permanent domain under ia/specs/ per Invariant #12.',
              'root ARCHITECTURE.md; ia/projects/; docs/architecture/',
              (SELECT id FROM arch_surfaces WHERE slug = 'decisions/all')),
  ('DEC-A12', 'plan-arch-link-stage-level',
              'active',
              'Stage-level arch_surfaces[] field via stage_arch_surfaces link table. Joins on arch_surfaces.slug; clean SQL queries.',
              'task-level link; JSONB column on ia_stages; tag string array',
              (SELECT id FROM arch_surfaces WHERE slug = 'decisions/all')),
  ('DEC-A13', 'drift-trigger-on-write-plus-on-demand',
              'active',
              'Drift scan fires on arch decision write + on-demand /arch-drift-scan. NO pre-stage-file gate (avoids friction during planning).',
              'pre-stage-file gate; nightly cron only; manual-only',
              (SELECT id FROM arch_surfaces WHERE slug = 'decisions/all')),
  ('DEC-A14', 'drift-reaction-poll-never-rewrite',
              'active',
              'Drift report + relentless human polling per affected Stage. Never auto-rewrite plans — humans decide rationale.',
              'auto-rewrite Stage scope; silent log; abort planning until resolved',
              (SELECT id FROM arch_surfaces WHERE slug = 'decisions/all')),
  ('DEC-A15', 'arch-authoring-via-design-explore',
              'active',
              'New Architecture Decision phase inside /design-explore between Select Approach and Expand. Avoids new top-level command for arch authoring.',
              'new /arch-decide command; manual SQL inserts; spec-only edits',
              (SELECT id FROM arch_surfaces WHERE slug = 'decisions/all')),
  ('DEC-A16', 'db-relations-four-tables',
              'active',
              'All four tables: arch_surfaces, arch_decisions, arch_changelog, stage_arch_surfaces. Full coverage of surface inventory + decision rows + append-only history + plan↔arch links.',
              'drop changelog (append to plan change_log instead); merge surfaces+decisions; single JSONB blob',
              (SELECT id FROM arch_surfaces WHERE slug = 'decisions/all')),
  ('DEC-A17', 'decisions-doc-shape-table-driven',
              'active',
              'decisions.md = compact table-driven; NO ADR-style multi-paragraph records. Rationale ≤250 chars per row; alternatives ≤3 semicolon-separated.',
              'ADR per decision; YAML frontmatter; long-form prose',
              (SELECT id FROM arch_surfaces WHERE slug = 'decisions/all'))
ON CONFLICT (slug) DO NOTHING;

COMMIT;
