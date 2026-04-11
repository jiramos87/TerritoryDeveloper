# AI Agent Guide — Territory Developer

> **TL;DR.** Use **`territory-ia`** MCP first (`backlog_issue` → `router_for_task` → `glossary_*` → `spec_section`). Ship work through the project-spec lifecycle (create → kickoff → implement → validate → close). Emit a **Verification** block per [`docs/agent-led-verification-policy.md`](docs/agent-led-verification-policy.md) at completion. Hard guardrails: [`ia/rules/invariants.md`](ia/rules/invariants.md). Native host surface (Claude Code hooks, slash commands, subagents): [`CLAUDE.md`](CLAUDE.md).

## 1. Before you start

1. Read the `/// <summary>` on the C# class you are about to modify.
2. If you have an issue id, call **`backlog_issue`** (territory-ia). Otherwise read [`BACKLOG.md`](BACKLOG.md).
3. Use [`ia/rules/agent-router.md`](ia/rules/agent-router.md) to find the right specs for the task.
4. **MCP first**: in MCP-enabled hosts, treat **`territory-ia`** as the default retrieval path. Suggested order: `backlog_issue` → `router_for_task` → `glossary_discover` / `glossary_lookup` (English only — translate from the conversation) → `spec_outline` / `spec_section` / `spec_sections` → `invariants_summary` / `list_rules` / `rule_content`. For closing a project spec: `project_spec_closeout_digest` after `backlog_issue`. Reference: [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md).
5. **Stale content:** the territory-ia MCP server caches per-process. After large edits to a doc, prefer a fresh `read_file` on that path or restart the MCP server.

## 2. Lifecycle skills (open the matching `SKILL.md`)

| Stage | Skill | Triggers |
|---|---|---|
| **Create** | [`project-new`](ia/skills/project-new/SKILL.md) | New `BACKLOG.md` row + `ia/projects/{ISSUE_ID}-{slug}.md` from a prompt |
| **Refine** | [`project-spec-kickoff`](ia/skills/project-spec-kickoff/SKILL.md) | Review or enrich a project spec before code |
| **Implement** | [`project-spec-implement`](ia/skills/project-spec-implement/SKILL.md) | Execute a spec's Implementation Plan |
| **Validate (Node)** | [`project-implementation-validation`](ia/skills/project-implementation-validation/SKILL.md) | Post-implementation Node checks aligned with CI |
| **Verify (Unity)** | [`agent-test-mode-verify`](ia/skills/agent-test-mode-verify/SKILL.md) + [`ide-bridge-evidence`](ia/skills/ide-bridge-evidence/SKILL.md) + [`close-dev-loop`](ia/skills/close-dev-loop/SKILL.md) | Path A batch / Path B IDE bridge evidence |
| **Bridge preflight** | [`bridge-environment-preflight`](ia/skills/bridge-environment-preflight/SKILL.md) | Before any `unity_bridge_command` in a session |
| **Close stage** | [`project-stage-close`](ia/skills/project-stage-close/SKILL.md) | End of each non-final stage of a multi-stage spec |
| **Close issue** | [`project-spec-close`](ia/skills/project-spec-close/SKILL.md) | Migrate lessons → durable IA, archive backlog row, delete spec |
| **UI rows** | [`ui-hud-row-theme`](ia/skills/ui-hud-row-theme/SKILL.md) | HUD/menu rows with `UiTheme` and the UI design spec |

Skill index + conventions: [`ia/skills/README.md`](ia/skills/README.md).

## 3. Verification policy (canonical)

The verification policy is canonicalized at [`docs/agent-led-verification-policy.md`](docs/agent-led-verification-policy.md) and surfaced as the always-on rule [`ia/rules/agent-verification-directives.md`](ia/rules/agent-verification-directives.md). Read the policy doc for the **Verification block** format (Node / IA, Unity compile, Path A batch, Path B bridge), the bridge timeout (40 s initial, escalation, 120 s ceiling), and the Path A project-lock release. Do **not** restate the policy here, in skills, or in rules.

## 4. Documentation hierarchy

```
docs/information-architecture-overview.md → IA philosophy, layers, lifecycle, extension guide
docs/agent-led-verification-policy.md     → canonical Verification policy
docs/mcp-ia-server.md                     → territory-ia MCP tool catalog + recipes
ia/rules/                                 → guardrails (always-loaded; light)
ia/skills/                                → workflow recipes (orchestration, not facts)
ia/specs/                                 → deep reference (read on demand per task)
ia/projects/                              → temporary project specs (deleted on close)
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
| [`managers-reference.md`](ia/specs/managers-reference.md) | All managers and helper services: responsibilities, dependencies |
| [`glossary.md`](ia/specs/glossary.md) | Domain term definitions (English only) |
| [`unity-development-context.md`](ia/specs/unity-development-context.md) | Unity patterns: MonoBehaviour lifecycle, Inspector / `SerializeField`, `FindObjectOfType` policy, Script Execution Order |
| [`REFERENCE-SPEC-STRUCTURE.md`](ia/specs/REFERENCE-SPEC-STRUCTURE.md) | Meta: how to author and extend reference specs |

Do **not** add bug write-ups, agent prompts, or one-off specs under `ia/specs/`. Use `BACKLOG.md` while work is open; delete temporary markdown after completion.

## 5. `ia/projects/` policy

Project-specific specs for features or complex bugs **in active development** live under `ia/projects/`. They are **temporary** — deleted after verified completion.

| Aspect | Rule |
|--------|------|
| Template | [`ia/templates/project-spec-template.md`](ia/templates/project-spec-template.md) |
| Structure | [`ia/projects/PROJECT-SPEC-STRUCTURE.md`](ia/projects/PROJECT-SPEC-STRUCTURE.md) |
| Naming | `{ISSUE_ID}-{description}.md` (e.g. `BUG-37-zone-cleanup.md`); legacy bare `{ISSUE_ID}.md` still accepted by validators / journal tools |
| Lifecycle | Create → refine → implement → verify → close |
| On completion | Migrate lessons learned to canonical docs **before** deleting (`project-spec-close`) |
| Dead-path check | `npm run validate:dead-project-specs` (advisory: `--advisory` or `CI_DEAD_SPEC_ADVISORY=1`) |
| Frontmatter | Four-field IA header (`purpose`, `audience`, `loaded_by`, `slices_via`); validator: `npm run validate:frontmatter` |

**Requirements vs implementation.** Separate **product / game-logic** content (player + simulation rules — using [`ia/specs/glossary.md`](ia/specs/glossary.md) terms) from **implementation** content (files, classes, algorithms). The implementing agent picks code-level solutions **unless** doing so would change spec-defined game behavior; in that case, record the conflict in the spec **Decision Log** or ask the product owner.

**Open Questions.** Every collaborative project spec SHOULD include `## Open Questions (resolve before / during implementation)`. Phrase questions in canonical domain vocabulary; target definitions and intended game logic only — implementation choices belong under **Implementation Plan** or **Implementation investigation notes**.

**Multi-stage specs.** Large rewrites declare top-level **stages** with internal **phases**, executed by one fresh agent per stage, with [`project-stage-close`](ia/skills/project-stage-close/SKILL.md) closing each non-final stage and the umbrella `project-spec-close` closing the last one.

### Project docs outside `ia/specs/`

Charters and discovery for cross-cutting programs live under `docs/`. Umbrella programs:

- **JSON interchange program** — glossary row + [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md), [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md), [`projects/json-use-cases-brainstorm.md`](projects/json-use-cases-brainstorm.md); charter trace in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md).
- **Compute-lib program** — glossary **Compute-lib program**; charter trace in [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md); ongoing work in [`BACKLOG.md`](BACKLOG.md) **§ Compute-lib program**.

Durable IA does **not** embed backlog issue ids — see [`ia/rules/terminology-consistency.md`](ia/rules/terminology-consistency.md).

## 6. Terminology and information consistency

| Source | Use for |
|--------|---------|
| [`ia/specs/glossary.md`](ia/specs/glossary.md) | Canonical domain terms; check before naming features, bugs, or user-facing copy |
| Linked specs | Definitions trump glossary on conflict (glossary defers to spec) |
| [`ia/rules/coding-conventions.md`](ia/rules/coding-conventions.md) | C# identifiers, XML docs, prefab naming for new assets |
| [`BACKLOG.md`](BACKLOG.md) | Issue id prefixes; **Files** / **Notes** / **Acceptance** reuse spec/glossary vocabulary |
| [`tools/mcp-ia-server/`](tools/mcp-ia-server/) | Tool names (`snake_case`) match `registerTool` in code; keep [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) and the package README in sync |

New or changed concepts → update glossary **and** the relevant spec section. Don't leave terminology only in backlog or chat. Always-loaded rule body: [`ia/rules/terminology-consistency.md`](ia/rules/terminology-consistency.md).

## 7. Backlog workflow

[`BACKLOG.md`](BACKLOG.md) is the single source of truth for project issues. Id prefixes: `BUG-`, `FEAT-`, `TECH-`, `ART-`, `AUDIO-`. **`backlog_issue` MCP** resolves ids from `BACKLOG.md` first, then `BACKLOG-ARCHIVE.md`.

**Working on an issue.**

1. Prefer `backlog_issue` (territory-ia) for the id; otherwise read `BACKLOG.md`.
2. Read the files listed in the issue's **Files** field.
3. Plan mode: analyze and propose a plan.
4. Agent mode: implement, then move issue to **In progress**.

**After implementing.** Keep the issue **In progress** until the user confirms verification.

**Closing an issue with a project spec.** Follow [`project-spec-close`](ia/skills/project-spec-close/SKILL.md): persist lessons to glossary, reference specs, `ARCHITECTURE.md`, `ia/rules/`, `docs/` (and MCP docs if tools changed) **before** deleting the spec; run `npm run validate:dead-project-specs`; remove the row from `BACKLOG.md`; append `[x]` to `BACKLOG-ARCHIVE.md`; purge the closed id from durable IA and code.

**Adding new issues.**

- **Id (per prefix):** scan both `BACKLOG.md` and `BACKLOG-ARCHIVE.md` for the highest existing number with the chosen prefix; assign **max + 1**. Do **not** reuse ids — archived rows keep them for traceability.
- Include: Type, Files, Notes, Depends on (if applicable).
- Prefer `BACKLOG.md` + `ia/specs/` for durable rules.

**Priority order:** In progress → High priority → Medium priority → Code Health → Low priority.

**Next-issue prompts.** When the user asks which is next, respond with it and **ask if they want an AI agent prompt** — a prompt for another agent to analyze, evaluate, and propose a development plan. Format the prompt body inside a fenced ` ```markdown ` block, in English unless the user requests otherwise.

## 8. Pre-commit checklist

- [ ] Code compiles (Build in Unity or `npm run unity:compile-check`)
- [ ] Class-level `/// <summary>` exists and is accurate
- [ ] New public methods have XML documentation
- [ ] `Debug.Log` messages and comments are in English
- [ ] If `GridManager` was touched, verify sorting order with different height levels
- [ ] If roads were modified, `InvalidateRoadCache()` is called where needed
- [ ] If a new manager was added, it follows the Inspector + `FindObjectOfType` pattern
- [ ] New prefabs follow [`coding-conventions.md`](ia/rules/coding-conventions.md) naming (don't rename existing assets)
- [ ] Temporary `Debug.Log` diagnostics removed or gated per [`coding-conventions.md`](ia/rules/coding-conventions.md)
- [ ] Touched-domain wording matches `glossary.md` / linked specs
- [ ] If you changed links or `Spec:` lines for `ia/projects/*.md`: `npm run validate:dead-project-specs`
- [ ] If you changed `tools/mcp-ia-server`, `docs/schemas`, `ia/specs` bodies, or `glossary.md`: `npm run validate:all` (or `npm run verify:local` when Postgres + Unity bridge apply)
- [ ] Substantive implementation: include the **Verification** block per [`docs/agent-led-verification-policy.md`](docs/agent-led-verification-policy.md)
