# AI Agent Guide — Territory Developer

> **TL;DR.** `territory-ia` MCP first (`backlog_issue` → `router_for_task` → `glossary_*` → `spec_section`). Ship via project-spec lifecycle (create → kickoff → implement → validate → close). Emit **Verification** block per [`docs/agent-led-verification-policy.md`](docs/agent-led-verification-policy.md) at completion. Hard guardrails: [`ia/rules/invariants.md`](ia/rules/invariants.md). Native host surface (Claude Code hooks, slash commands, subagents): [`CLAUDE.md`](CLAUDE.md).

## 1. Before you start

1. Read `/// <summary>` on C# class about to modify.
2. Have issue id → **`backlog_issue`** (territory-ia). Else [`BACKLOG.md`](BACKLOG.md).
3. [`ia/rules/agent-router.md`](ia/rules/agent-router.md) → right specs per task.
4. **MCP first**: in MCP-enabled hosts, `territory-ia` is default retrieval. Order: `backlog_issue` → `router_for_task` → `glossary_discover` / `glossary_lookup` (English only — translate from chat) → `spec_outline` / `spec_section` / `spec_sections` → `invariants_summary` / `list_rules` / `rule_content`. Closing project spec: `project_spec_closeout_digest` after `backlog_issue`. Ref: [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md).
5. **Stale content:** territory-ia MCP caches per-process. After large edits, prefer fresh `read_file` or restart MCP server.

## 2. Lifecycle skills (open matching `SKILL.md`)

| Stage | Skill | Triggers |
|---|---|---|
| **Create** | [`project-new`](ia/skills/project-new/SKILL.md) | New `BACKLOG.md` row + `ia/projects/{ISSUE_ID}-{slug}.md` from prompt |
| **Refine** | [`project-spec-kickoff`](ia/skills/project-spec-kickoff/SKILL.md) | Review / enrich project spec before code |
| **Implement** | [`project-spec-implement`](ia/skills/project-spec-implement/SKILL.md) | Execute spec Implementation Plan |
| **Validate (Node)** | [`project-implementation-validation`](ia/skills/project-implementation-validation/SKILL.md) | Post-implementation Node checks (CI-aligned) |
| **Verify (Unity)** | [`agent-test-mode-verify`](ia/skills/agent-test-mode-verify/SKILL.md) + [`ide-bridge-evidence`](ia/skills/ide-bridge-evidence/SKILL.md) + [`close-dev-loop`](ia/skills/close-dev-loop/SKILL.md) | Path A batch / Path B IDE bridge evidence |
| **Bridge preflight** | [`bridge-environment-preflight`](ia/skills/bridge-environment-preflight/SKILL.md) | Before any `unity_bridge_command` in session |
| **Close stage** | [`project-stage-close`](ia/skills/project-stage-close/SKILL.md) | End of each non-final stage of multi-stage spec |
| **Close issue** | [`project-spec-close`](ia/skills/project-spec-close/SKILL.md) | Migrate lessons → durable IA, archive row, delete spec |
| **UI rows** | [`ui-hud-row-theme`](ia/skills/ui-hud-row-theme/SKILL.md) | HUD/menu rows w/ `UiTheme` + UI design spec |

Skill index + conventions: [`ia/skills/README.md`](ia/skills/README.md).

## 3. Verification policy (canonical)

Canonicalized at [`docs/agent-led-verification-policy.md`](docs/agent-led-verification-policy.md); surfaced as always-on rule [`ia/rules/agent-verification-directives.md`](ia/rules/agent-verification-directives.md). Policy doc carries **Verification block** format (Node/IA, Unity compile, Path A batch, Path B bridge), bridge timeout (40 s initial, escalation, 120 s ceiling), Path A project-lock release. Do NOT restate here, in skills, or rules.

## 4. Documentation hierarchy

```
docs/information-architecture-overview.md → IA philosophy, layers, lifecycle, extension guide
docs/agent-led-verification-policy.md     → canonical Verification policy
docs/mcp-ia-server.md                     → territory-ia MCP tool catalog + recipes
ia/rules/                                 → guardrails (always-loaded; light)
ia/skills/                                → workflow recipes (orchestration, not facts)
ia/specs/                                 → deep reference (read on demand per task)
ia/projects/                              → temporary project specs (deleted on close); orchestrator docs (permanent)
ia/rules/project-hierarchy.md             → step/stage/phase/task execution hierarchy
ia/rules/orchestrator-vs-spec.md          → orchestrator vs project spec distinction
ia/templates/                             → project spec template + frontmatter schema
ARCHITECTURE.md                           → runtime layers, dependency map, Local verification
BACKLOG.md / BACKLOG-ARCHIVE.md           → issue tracking (root)
CLAUDE.md / AGENTS.md                     → host entry points (root)
MEMORY.md                                 → project-level architectural memory (root)
```

### `ia/specs/` inventory

| File | Scope |
|------|-------|
| [`isometric-geography-system.md`](ia/specs/isometric-geography-system.md) | Canonical: terrain, water, cliffs, shores, sorting, terraform, roads, rivers, pathfinding |
| [`ui-design-system.md`](ia/specs/ui-design-system.md) | UI foundations, components, patterns |
| [`roads-system.md`](ia/specs/roads-system.md) | Road placement pipeline, validation, resolver, bridge rules, land slope stroke policy |
| [`simulation-system.md`](ia/specs/simulation-system.md) | Simulation tick order, AUTO pipeline, growth |
| [`persistence-system.md`](ia/specs/persistence-system.md) | Save/load pipeline, visual restore |
| [`water-terrain-system.md`](ia/specs/water-terrain-system.md) | Height model, water bodies, cliffs, shores, cascades |
| [`managers-reference.md`](ia/specs/managers-reference.md) | Managers + helper services: responsibilities, deps |
| [`glossary.md`](ia/specs/glossary.md) | Domain term definitions (English only) |
| [`unity-development-context.md`](ia/specs/unity-development-context.md) | Unity patterns: MonoBehaviour lifecycle, Inspector / `SerializeField`, `FindObjectOfType`, Script Execution Order |
| [`REFERENCE-SPEC-STRUCTURE.md`](ia/specs/REFERENCE-SPEC-STRUCTURE.md) | Meta: author + extend reference specs |

Do NOT add bug write-ups, agent prompts, one-off specs under `ia/specs/`. Use `BACKLOG.md` while open; delete temp markdown after completion.

## 5. `ia/projects/` policy

Project-specific specs for features or complex bugs **in active development** → `ia/projects/`. **Temporary** — deleted after verified completion.

| Aspect | Rule |
|--------|------|
| Template | [`ia/templates/project-spec-template.md`](ia/templates/project-spec-template.md) |
| Structure | [`ia/projects/PROJECT-SPEC-STRUCTURE.md`](ia/projects/PROJECT-SPEC-STRUCTURE.md) |
| Naming | `{ISSUE_ID}-{description}.md` (e.g. `BUG-37-zone-cleanup.md`); legacy bare `{ISSUE_ID}.md` still accepted |
| Lifecycle | Create → refine → implement → verify → close |
| On completion | Migrate lessons learned to canonical docs **before** deleting (`project-spec-close`) |
| Dead-path check | `npm run validate:dead-project-specs` (advisory: `--advisory` or `CI_DEAD_SPEC_ADVISORY=1`) |
| Frontmatter | 4-field IA header (`purpose`, `audience`, `loaded_by`, `slices_via`); validator: `npm run validate:frontmatter` |

**Requirements vs implementation.** Separate **product / game-logic** content (player + simulation rules — [`ia/specs/glossary.md`](ia/specs/glossary.md) terms) from **implementation** (files, classes, algorithms). Implementing agent picks code-level solutions **unless** it changes spec-defined game behavior; then record conflict in **Decision Log** or ask product owner.

**Open Questions.** Every collaborative project spec SHOULD include `## Open Questions (resolve before / during implementation)`. Phrase in canonical domain vocabulary; target definitions + intended game logic only — implementation choices → **Implementation Plan** / **Implementation investigation notes**.

**Multi-stage specs.** Large rewrites declare top-level **stages** with internal **phases**, executed by one fresh agent per stage; [`project-stage-close`](ia/skills/project-stage-close/SKILL.md) closes each non-final stage; umbrella `project-spec-close` closes last.

### Project docs outside `ia/specs/`

Charters + discovery for cross-cutting programs → `docs/`. Umbrella programs:

- **JSON interchange program** — glossary row + [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md), [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md), [`projects/json-use-cases-brainstorm.md`](projects/json-use-cases-brainstorm.md); charter trace in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md).
- **Compute-lib program** — glossary **Compute-lib program**; charter in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md); ongoing work in [`BACKLOG.md`](BACKLOG.md) **§ Compute-lib program**.

Durable IA does NOT embed backlog issue ids — see [`ia/rules/terminology-consistency.md`](ia/rules/terminology-consistency.md).

## 6. Terminology and information consistency

| Source | Use for |
|--------|---------|
| [`ia/specs/glossary.md`](ia/specs/glossary.md) | Canonical domain terms; check before naming features, bugs, user-facing copy |
| Linked specs | Definitions trump glossary on conflict (glossary defers to spec) |
| [`ia/rules/coding-conventions.md`](ia/rules/coding-conventions.md) | C# identifiers, XML docs, prefab naming for new assets |
| [`BACKLOG.md`](BACKLOG.md) | Issue id prefixes; **Files** / **Notes** / **Acceptance** reuse spec/glossary vocabulary |
| [`tools/mcp-ia-server/`](tools/mcp-ia-server/) | Tool names (`snake_case`) match `registerTool` in code; keep [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) + package README in sync |

New/changed concepts → update glossary **and** relevant spec section. No terminology-only-in-backlog-or-chat. Always-loaded rule body: [`ia/rules/terminology-consistency.md`](ia/rules/terminology-consistency.md).

## 7. Backlog workflow

[`BACKLOG.md`](BACKLOG.md) = single source of truth for project issues. Id prefixes: `BUG-`, `FEAT-`, `TECH-`, `ART-`, `AUDIO-`. **`backlog_issue` MCP** resolves ids from `BACKLOG.md` first, then `BACKLOG-ARCHIVE.md`.

**Working on issue.**

1. Prefer `backlog_issue` (territory-ia) for id; else `BACKLOG.md`.
2. Read files in issue **Files** field.
3. Plan mode: analyze + propose plan.
4. Agent mode: implement, move issue → **In progress**.

**After implementing.** Keep issue **In progress** until user confirms verification.

**Closing issue with project spec.** Follow [`project-spec-close`](ia/skills/project-spec-close/SKILL.md): persist lessons → glossary, reference specs, `ARCHITECTURE.md`, `ia/rules/`, `docs/` (+ MCP docs if tools changed) **before** deleting spec; `npm run validate:dead-project-specs`; remove row from `BACKLOG.md`; append `[x]` to `BACKLOG-ARCHIVE.md`; purge closed id from durable IA + code.

**Adding issues.**

- **Id (per prefix):** scan `BACKLOG.md` + `BACKLOG-ARCHIVE.md` for highest number w/ chosen prefix; assign **max + 1**. No reuse — archived rows keep ids for traceability.
- Include: Type, Files, Notes, Depends on (if applicable).
- **Caveman prose** in Notes / Acceptance per [`ia/rules/agent-output-caveman.md`](ia/rules/agent-output-caveman.md). Row structure + bolded glossary terms + id cross-refs + path links verbatim.
- Prefer `BACKLOG.md` + `ia/specs/` for durable rules.

**Priority order:** In progress → High priority → Medium priority → Code Health → Low priority.

**Next-issue prompts.** User asks which is next → respond with it + **ask if they want an AI agent prompt** (analysis + development plan). Format prompt body in fenced ` ```markdown ` block, English unless user requests otherwise.

## 8. Pre-commit checklist

- [ ] Code compiles (Unity Build or `npm run unity:compile-check`)
- [ ] Class-level `/// <summary>` exists + accurate
- [ ] New public methods have XML documentation
- [ ] `Debug.Log` + comments in English
- [ ] `GridManager` touched → verify sorting order w/ different height levels
- [ ] Roads modified → `InvalidateRoadCache()` called where needed
- [ ] New manager → Inspector + `FindObjectOfType` pattern
- [ ] New prefabs follow [`coding-conventions.md`](ia/rules/coding-conventions.md) naming (don't rename existing)
- [ ] Temporary `Debug.Log` removed/gated per [`coding-conventions.md`](ia/rules/coding-conventions.md)
- [ ] Touched-domain wording matches `glossary.md` / linked specs
- [ ] Changed links / `Spec:` lines for `ia/projects/*.md` → `npm run validate:dead-project-specs`
- [ ] Changed `tools/mcp-ia-server`, `docs/schemas`, `ia/specs` bodies, or `glossary.md` → `npm run validate:all` (or `npm run verify:local` when Postgres + Unity bridge apply)
- [ ] Substantive implementation → include **Verification** block per [`docs/agent-led-verification-policy.md`](docs/agent-led-verification-policy.md)
