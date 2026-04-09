# Information Architecture — System Overview

> Single entry point for understanding the IA system as a coherent design.
> For day-to-day agent workflow, see [`AGENTS.md`](../AGENTS.md).
> For MCP tool catalog, see [`docs/mcp-ia-server.md`](mcp-ia-server.md).

## 0. Autoreference (where this document lives)

This file is the **canonical narrative** for the **Information Architecture** **stack** implemented in this repository. The stack **describes itself** through:

| Layer | Self-description |
|-------|------------------|
| **This overview** | Layer diagram (§2), lifecycle (§3), MCP map (§6) |
| **[`AGENTS.md`](../AGENTS.md)** | Agent workflow, checklist, links into rules and skills |
| **[`ARCHITECTURE.md`](../ARCHITECTURE.md)** | Runtime dependency map + **Local verification** |
| **[`.cursor/rules/`](../.cursor/rules/)** | Always-on and globs rules (guardrails, MCP defaults) |
| **[`.cursor/skills/`](../.cursor/skills/)** | Ordered recipes (implement, validate, bridge, test mode) |
| **[`tools/mcp-ia-server/`](../tools/mcp-ia-server/)** | **territory-ia** tool implementations |

**Agent-led verification** (Unity batch + IDE bridge reporting policy): [`docs/agent-led-verification-policy.md`](agent-led-verification-policy.md) and [`.cursor/rules/agent-verification-directives.mdc`](../.cursor/rules/agent-verification-directives.mdc).

---

## 1. Design philosophy

Territory Developer uses a **hierarchical, file-backed Information Architecture** to give AI agents and human contributors the right context at the right time, with minimal token cost.

Three principles:

1. **Slice, don't load.** Agents fetch spec *sections* and glossary *terms* via MCP tools, not whole files. A 778-line geography spec becomes a 50-line slice about shore bands.
2. **One vocabulary everywhere.** A single [glossary](../.cursor/specs/glossary.md) governs naming across code, specs, backlog, rules, skills, and MCP tools. Consistency makes search reliable and reduces ambiguity.
3. **Knowledge flows back.** Lessons learned during implementation migrate from temporary project specs into permanent reference specs, glossary, and rules before the project spec is deleted. Nothing is learned only once.

---

## 2. Layer diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                        BACKLOG.md                                   │
│  Issue tracking (BUG-, FEAT-, TECH-, ART-, AUDIO-)                  │
│  ← backlog_issue MCP tool                                           │
└──────────────┬──────────────────────────────────────────────────────┘
               │ creates / references
┌──────────────▼──────────────────────────────────────────────────────┐
│              .cursor/projects/{ISSUE_ID}.md                         │
│  Temporary project specs — goals, implementation plan, decisions    │
│  ← project_spec_closeout_digest, project_spec_journal_* MCP tools  │
└──────────────┬──────────────────────────────────────────────────────┘
               │ uses (via MCP slices)
┌──────────────▼──────────────────────────────────────────────────────┐
│              .cursor/specs/*.md  (permanent reference specs)        │
│  Domain behavior, canonical rules, stable §-numbered sections      │
│  ← spec_section, spec_sections, spec_outline, list_specs MCP tools │
├─────────────────────────────────────────────────────────────────────┤
│              .cursor/specs/glossary.md  (vocabulary hub)            │
│  Term → Definition → Spec → Category                               │
│  ← glossary_discover, glossary_lookup MCP tools                    │
└──────────────┬──────────────────────────────────────────────────────┘
               │ enforced by
┌──────────────▼──────────────────────────────────────────────────────┐
│              .cursor/rules/*.mdc  (always-apply guardrails)         │
│  invariants, agent-router, terminology-consistency, mcp-ia-default  │
│  ← invariants_summary, router_for_task, list_rules, rule_content   │
└──────────────┬──────────────────────────────────────────────────────┘
               │ orchestrated by
┌──────────────▼──────────────────────────────────────────────────────┐
│              .cursor/skills/*/SKILL.md  (agent workflows)           │
│  Ordered MCP tool recipes for each lifecycle stage                  │
│  project-new → kickoff → implement → validate → close-dev-loop →   │
│  close                                                              │
└──────────────┬──────────────────────────────────────────────────────┘
               │ served by
┌──────────────▼──────────────────────────────────────────────────────┐
│              territory-ia MCP server                                │
│  tools/mcp-ia-server/ — 20+ tools over stdio transport             │
│  Registered in .mcp.json; Postgres optional (journal, bridge)      │
└─────────────────────────────────────────────────────────────────────┘
```

**Data flows:**
- **Down:** agents query MCP tools → tools read specs/glossary/rules/backlog from filesystem or Postgres
- **Up:** on project-spec-close, lessons migrate from temporary project specs into permanent specs, glossary, rules, and docs
- **Lateral:** skills define the order in which agents call MCP tools for a given lifecycle stage

---

## 3. Knowledge lifecycle

Every issue follows a lifecycle where knowledge is created, refined, used, and then migrated into durable IA:

```
1. CREATE          project-new skill
                   backlog_issue + glossary_discover + router_for_task + spec_section
                   → BACKLOG.md row + .cursor/projects/{ISSUE_ID}.md stub

2. REFINE          project-spec-kickoff skill
                   backlog_issue → invariants_summary → router_for_task → spec_section → glossary_*
                   → enriched project spec with clear Open Questions, Implementation Plan

3. IMPLEMENT       project-spec-implement skill
                   Per-phase loop: router_for_task → spec_section → glossary_* → code → compile gate
                   → code changes + updated spec Decision Log / Issues Found

4. VERIFY          agent-test-mode-verify / close-dev-loop / ide-bridge-evidence (dev machine)
                   Agent test mode batch + optional IDE agent bridge; see agent-led-verification-policy.md
                   → batch JSON, bridge evidence, compile gate

5. VALIDATE        project-implementation-validation skill
                   npm run validate:dead-project-specs, test:ia, validate:fixtures, generate:ia-indexes --check
                   → CI-aligned validation report

6. CLOSE           project-spec-close skill
                   project_spec_closeout_digest → persist lessons to glossary, specs, rules, docs
                   → project_spec_journal_persist (Postgres) → delete spec → archive backlog row
                   → purge closed issue id from durable surfaces
```

**Key invariant:** Temporary project specs are *always* deleted after closure. Any knowledge worth keeping is migrated to permanent IA surfaces first.

---

## 4. Semantic model

Three axes connect the entire IA:

| Axis | Source | Purpose |
|------|--------|---------|
| **Vocabulary** | [glossary.md](../.cursor/specs/glossary.md) | Canonical term definitions; one name per concept across all surfaces |
| **Task routing** | [agent-router.mdc](../.cursor/rules/agent-router.mdc) | Maps task domains to specs and sections; powers `router_for_task` MCP tool |
| **Invariants** | [invariants.mdc](../.cursor/rules/invariants.mdc) | 12 hard constraints + IF→THEN guardrails that must never be violated |

Agents use these three axes to navigate from a task description to the precise context they need:

```
Task: "fix road rendering at border"
  → router_for_task(domain: "Road logic") → roads-system, geo §13
  → glossary_discover(keywords: ["road", "border", "interstate"]) → "Map border", "Road stroke", "Street"
  → invariants_summary() → #10: road preparation family
  → spec_section(spec: "geo", section: "13") → exact rules
```

---

## 5. Consistency mechanisms

| Mechanism | What it enforces | How |
|-----------|-----------------|-----|
| [terminology-consistency.mdc](../.cursor/rules/terminology-consistency.mdc) | Single vocabulary across code, specs, backlog, MCP | Always-apply Cursor rule |
| [invariants.mdc](../.cursor/rules/invariants.mdc) | 12 hard invariants + guardrails | Always-apply Cursor rule; `invariants_summary` MCP tool |
| [mcp-ia-default.mdc](../.cursor/rules/mcp-ia-default.mdc) | Agents use MCP tools before reading whole files | Always-apply Cursor rule |
| `npm run validate:dead-project-specs` | No dangling links to deleted project specs | CI script |
| `npm run test:ia` | MCP parsers and tools work correctly | CI test suite |
| `npm run validate:fixtures` | JSON Schema fixtures valid | CI validation |
| `npm run generate:ia-indexes -- --check` | IA indexes (spec-index.json, glossary-index.json) not stale | CI check |
| `npm run validate:all` | Dead project-spec paths, **`compute-lib:build`**, **`test:ia`**, **`validate:fixtures`**, **`generate:ia-indexes --check`** | CI umbrella (subset; **CI** also runs **`npm ci`** in packages) |
| `npm run verify:local` | **`validate:all`** then **`post-implementation-verify.sh --skip-node-checks`**: **`unity:compile-check`**, **`db:migrate`**, **`db:bridge-preflight`**, **macOS** Editor + **`db:bridge-playmode-smoke`**. **`verify:post-implementation`** = alias. [`tools/scripts/verify-local.sh`](../tools/scripts/verify-local.sh). Dev machine; **not** CI. | Local post-implementation |
| [coding-conventions.mdc](../.cursor/rules/coding-conventions.mdc) | C# naming, XML docs, prefab conventions | Globs-apply rule (`**/*.cs`) |

---

## 6. MCP tool ecosystem

The **territory-ia** MCP server ([tools/mcp-ia-server/](../tools/mcp-ia-server/), configured in [.mcp.json](../.mcp.json)) exposes 20+ tools over stdio transport:

| Category | Tools | Data source |
|----------|-------|-------------|
| **Backlog** | `backlog_issue` | BACKLOG.md / BACKLOG-ARCHIVE.md |
| **Specs** | `list_specs`, `spec_outline`, `spec_section`, `spec_sections` | .cursor/specs/, .cursor/rules/, AGENTS.md, ARCHITECTURE.md |
| **Glossary** | `glossary_discover`, `glossary_lookup` | glossary.md |
| **Routing/Rules** | `router_for_task`, `invariants_summary`, `list_rules`, `rule_content` | agent-router.mdc, invariants.mdc, .cursor/rules/*.mdc |
| **Project specs** | `project_spec_closeout_digest` | .cursor/projects/{ISSUE_ID}.md |
| **Journal** | `project_spec_journal_persist`, `_search`, `_get`, `_update` | Postgres `ia_project_spec_journal` |
| **Compute** | `grid_distance`, `growth_ring_classify`, `isometric_world_to_grid`, `pathfinding_cost_preview`, `geography_init_params_validate` | territory-compute-lib + Zod |
| **Unity bridge** | `unity_bridge_command`, `unity_bridge_get`, `unity_compile` | Postgres `agent_bridge_job` |

**Suggested call order:** `backlog_issue` → `router_for_task` → `glossary_discover`/`glossary_lookup` → `spec_section`/`spec_sections` → `invariants_summary` (when touching C# runtime).

Full tool documentation: [docs/mcp-ia-server.md](mcp-ia-server.md).

---

## 7. Skill system

Skills under [.cursor/skills/](../.cursor/skills/) define ordered MCP tool recipes for each lifecycle stage. They don't execute tools — they tell the agent which tools to call, in what order, with what parameters.

| Lifecycle stage | Skill | Core MCP recipe |
|-----------------|-------|-----------------|
| **Create** issue | [project-new](../.cursor/skills/project-new/SKILL.md) | glossary_discover → router_for_task → spec_section → backlog_issue |
| **Refine** spec | [project-spec-kickoff](../.cursor/skills/project-spec-kickoff/SKILL.md) | backlog_issue → invariants_summary → router_for_task → spec_section → glossary_* |
| **Implement** | [project-spec-implement](../.cursor/skills/project-spec-implement/SKILL.md) | Per-phase: router → spec_section → glossary → code → compile gate |
| **Validate** (Node + local bridge) | [project-implementation-validation](../.cursor/skills/project-implementation-validation/SKILL.md) | **`npm run validate:all`** (includes **`compute-lib:build`**); **`npm run verify:local`** (full dev chain; **`verify:post-implementation`** alias) |
| **Debug** (Play Mode) | [ide-bridge-evidence](../.cursor/skills/ide-bridge-evidence/SKILL.md) / [close-dev-loop](../.cursor/skills/close-dev-loop/SKILL.md) | unity_bridge_command (debug_context_bundle, compile gate, before/after diff) |
| **Preflight** (bridge) | [bridge-environment-preflight](../.cursor/skills/bridge-environment-preflight/SKILL.md) | Postgres + agent_bridge_job readiness check |
| **Close** issue | [project-spec-close](../.cursor/skills/project-spec-close/SKILL.md) | closeout_digest → persist IA → journal_persist → delete spec → archive |
| **UI row** | [ui-hud-row-theme](../.cursor/skills/ui-hud-row-theme/SKILL.md) | spec_section (ui-design-system §1, §3.0, §4.3, §5.2) |

Skill conventions: [.cursor/skills/README.md](../.cursor/skills/README.md).

---

## 8. Optional Postgres layer

When `DATABASE_URL` resolves (or `config/postgres-dev.json` exists), two tables add persistence:

| Table | Purpose |
|-------|---------|
| `ia_project_spec_journal` | Decision Log + Lessons Learned from closed project specs; FTS + keyword GIN index |
| `agent_bridge_job` | Job queue for Unity Editor bridge commands (play mode, screenshots, compile status) |

When Postgres is unavailable, all tools return `db_unconfigured` gracefully. The game and IA system remain fully functional without it.

Setup: [docs/postgres-ia-dev-setup.md](postgres-ia-dev-setup.md). Migrations: `db/migrations/`.

---

## 9. Extension guide

### Adding a reference spec

1. Create `.cursor/specs/{name}.md` with stable `##`-numbered sections
2. Add a row to the `.cursor/specs/` inventory table in [AGENTS.md](../AGENTS.md)
3. Optionally add a short alias in `tools/mcp-ia-server/src/config.ts` (`SPEC_KEY_ALIASES`)
4. Confirm `list_specs` returns the new spec
5. Run `npm run generate:ia-indexes` and commit the updated index files
6. Update [docs/mcp-ia-server.md](mcp-ia-server.md) if the alias is documented there
7. Run `npm run validate:all`

### Adding an MCP tool

1. Implement in `tools/mcp-ia-server/src/` (new file or extend existing)
2. Register with `server.tool(...)` in `tools/mcp-ia-server/src/index.ts`
3. Add input validation (Zod schema)
4. Add tests under `tools/mcp-ia-server/tests/`
5. Update [docs/mcp-ia-server.md](mcp-ia-server.md) (tool table + recipe if applicable)
6. Update [tools/mcp-ia-server/README.md](../tools/mcp-ia-server/README.md)
7. If introducing a new term, add a glossary row
8. Run `npm run verify` and `npm run test:ia`

### Adding a skill

1. Create `.cursor/skills/{skill-name}/SKILL.md` with YAML frontmatter (`name`, `description` with trigger phrases)
2. Include a numbered "Tool recipe (territory-ia)" section with ordered MCP calls
3. Keep the body thin — point to `spec_section`/`router_for_task` instead of pasting spec content
4. Add a row to [.cursor/skills/README.md](../.cursor/skills/README.md) index table
5. Optionally add a pointer in [AGENTS.md](../AGENTS.md) and [CLAUDE.md](../CLAUDE.md)
6. Follow conventions in [.cursor/skills/README.md](../.cursor/skills/README.md)

### Adding a glossary term

1. Add a row to the table in [.cursor/specs/glossary.md](../.cursor/specs/glossary.md): Term, Definition, Spec reference, Category
2. If the term has normative behavior, define or cite it in the relevant reference spec section
3. Run `npm run generate:ia-indexes` and commit `glossary-index.json`
4. Verify with `glossary_lookup` that the new term resolves

### Adding a rule

1. Create `.cursor/rules/{name}.mdc` with YAML frontmatter (`description`, `alwaysApply`, optional `globs`)
2. Keep it short — rules are always-loaded guardrails, not full specs
3. If it routes tasks to specs, consider adding rows to [agent-router.mdc](../.cursor/rules/agent-router.mdc) instead
4. Optionally update [AGENTS.md](../AGENTS.md) if the rule is significant

### Adding a Postgres table

1. Create migration in `db/migrations/` (next sequential number, `0009_...`)
2. Update [docs/postgres-ia-dev-setup.md](postgres-ia-dev-setup.md) with table description
3. If adding MCP tools that use the table, follow the "Adding an MCP tool" checklist above
4. Ensure graceful `db_unconfigured` fallback when Postgres is unavailable
5. Optionally update [docs/postgres-interchange-patterns.md](postgres-interchange-patterns.md) if the table follows B1/B3/P5 patterns

---

## 10. Document index

| Document | Purpose |
|----------|---------|
| [AGENTS.md](../AGENTS.md) | Agent workflow policies, documentation hierarchy, backlog conventions, pre-commit checklist |
| [ARCHITECTURE.md](../ARCHITECTURE.md) | System layers, dependency map, data flows, init order, architectural decisions |
| [BACKLOG.md](../BACKLOG.md) | Single source of truth for project issues |
| [BACKLOG-ARCHIVE.md](../BACKLOG-ARCHIVE.md) | Closed issues with date and trace |
| [CLAUDE.md](../CLAUDE.md) | Claude Code project instructions (MCP, skills, rules summary) |
| [.cursor/specs/glossary.md](../.cursor/specs/glossary.md) | Canonical domain term definitions |
| [.cursor/specs/REFERENCE-SPEC-STRUCTURE.md](../.cursor/specs/REFERENCE-SPEC-STRUCTURE.md) | How to author and extend reference specs |
| [.cursor/projects/PROJECT-SPEC-STRUCTURE.md](../.cursor/projects/PROJECT-SPEC-STRUCTURE.md) | How to author project specs; closure checklist |
| [.cursor/rules/agent-router.mdc](../.cursor/rules/agent-router.mdc) | Task → spec routing tables |
| [.cursor/rules/invariants.mdc](../.cursor/rules/invariants.mdc) | 12 system invariants + guardrails |
| [.cursor/rules/terminology-consistency.mdc](../.cursor/rules/terminology-consistency.mdc) | Vocabulary consistency rule |
| [.cursor/rules/mcp-ia-default.mdc](../.cursor/rules/mcp-ia-default.mdc) | MCP-first retrieval rule |
| [.cursor/skills/README.md](../.cursor/skills/README.md) | Skill index and conventions |
| [docs/mcp-ia-server.md](mcp-ia-server.md) | MCP tool catalog, recipes, operations |
| [docs/mcp-markdown-ia-pattern.md](mcp-markdown-ia-pattern.md) | Reusable domain-agnostic IA+MCP pattern |
| [docs/postgres-ia-dev-setup.md](postgres-ia-dev-setup.md) | Postgres dev setup, migrations, bridge queue |
| [docs/postgres-interchange-patterns.md](postgres-interchange-patterns.md) | JSON interchange patterns (B1/B3/P5) |
| [docs/ia-system-review-and-extensions.md](ia-system-review-and-extensions.md) | IA system review, entity model analysis, extension ideas |
| [.mcp.json](../.mcp.json) | MCP server configuration |
| [tools/mcp-ia-server/](../tools/mcp-ia-server/) | MCP server source code |
| [tools/compute-lib/](../tools/compute-lib/) | Computational math library (Node) |
| [tools/postgres-ia/](../tools/postgres-ia/) | Postgres bridge scripts |
