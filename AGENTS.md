# AI Agent Guide — Territory Developer

## Before You Start

1. Check the `/// <summary>` on the class you are about to modify
2. Use `.cursor/rules/agent-router.mdc` to find the right specs for your task
3. **Context from IA (Cursor agents):** In **Agent** chats with tools enabled, treat **territory-ia** MCP as the **default** way to load specs and rules unless a tool call truly cannot run. **Do not** open whole spec files with `read_file` when a slice suffices. Suggested order: **`backlog_issue`** when you have an issue id (`BUG-37`, `FEAT-44`, …) → `list_specs` (if keys unknown) → `router_for_task` for domain → `glossary_discover` / `glossary_lookup` → `spec_outline` / `spec_section` (or `invariants_summary`, `list_rules` / `rule_content` as needed). For **`glossary_discover`** and **`glossary_lookup`**, arguments must be **English** (the glossary is English-only): if the developer writes in another language, **translate** their concepts into English domain terms before calling. If MCP is disabled in the host, fall back to `.cursor/rules/agent-router.mdc` and targeted `read_file`. Stale content: MCP caches parses per server process—after large edits to a doc, prefer a fresh `read_file` on that path or restart the MCP server. Reference: [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md).
4. **If asked to work on an issue:** use **`backlog_issue`** for that id when MCP is available; otherwise read `BACKLOG.md` (and see `BACKLOG.md` for priority and workflow).

System invariants and guardrails are in `.cursor/rules/invariants.mdc` (always loaded).
Task-to-spec routing is in `.cursor/rules/agent-router.mdc` (always loaded).
Full dependency map is in `ARCHITECTURE.md`.

## Documentation hierarchy

```
.cursor/rules/        → Guardrails (auto-loaded by Cursor, light)
.cursor/specs/        → Deep reference (read on demand per task)
ARCHITECTURE.md       → System layers, dependency map
AGENTS.md             → This file: workflow, policies, checklist
BACKLOG.md            → Issue tracking (read only when relevant)
docs/mcp-ia-server.md → territory-ia MCP (default retrieval path in Agent when enabled)
docs/mcp-markdown-ia-pattern.md → Reusable pattern: Markdown IA + Node MCP tools (any domain)
```

### `.cursor/specs/` inventory

These Markdown files are **reference specs** (per [glossary.md](.cursor/specs/glossary.md) — **Reference spec**): permanent deep reference for domain behavior and vocabulary. Authoring layout and checklist: [REFERENCE-SPEC-STRUCTURE.md](.cursor/specs/REFERENCE-SPEC-STRUCTURE.md).

| File | Scope |
|------|-------|
| `isometric-geography-system.md` | Canonical: terrain, water, cliffs, shores, sorting, terraform, roads, rivers, pathfinding |
| `ui-design-system.md` | UI foundations, components, patterns |
| `roads-system.md` | Road placement pipeline, validation, resolver, bridge rules, land slope stroke policy |
| `simulation-system.md` | Simulation tick order, AUTO pipeline, growth |
| `persistence-system.md` | Save/load pipeline, visual restore |
| `water-terrain-system.md` | Height model, water bodies, cliffs, shores, cascades |
| `managers-reference.md` | All managers and helper services: responsibilities, dependencies |
| `glossary.md` | Domain term definitions |
| `unity-development-context.md` | **Unity** patterns for this repo: **MonoBehaviour** lifecycle, **Inspector** / **`SerializeField`**, **`FindObjectOfType`** policy, **Script Execution Order**, 2D sorting vs **Sorting order** (pointer to geography §7), **Editor** agent diagnostics exports (§10) |
| `REFERENCE-SPEC-STRUCTURE.md` | Meta: conventions for writing and extending **reference specs** in this folder (terminology, MCP, new-file checklist) |

Do not add bug write-ups, agent prompts, or one-off specs under `.cursor/specs/`. Use `BACKLOG.md` while work is open; delete temporary markdown after completion.

### `.cursor/projects/` policy

Project-specific specs for features or complex bugs **in active development** live under `.cursor/projects/`. These are **temporary** — deleted after verified completion.

| Aspect | Rule |
|--------|------|
| Template | `.cursor/templates/project-spec-template.md` |
| Structure | `.cursor/projects/PROJECT-SPEC-STRUCTURE.md` (section order, requirements vs implementation) |
| Naming | `{ISSUE_ID}.md` (e.g. `FEAT-44.md`, `BUG-45.md`) |
| Lifecycle | Create → refine → implement → verify → close |
| On completion | Migrate lessons learned to canonical docs before deleting |

**Requirements vs implementation:** When authoring or extending a project spec, separate **product / game-logic** content (what the player and simulation rules do—using [`.cursor/specs/glossary.md`](.cursor/specs/glossary.md) terms) from **implementation** content (files, classes, algorithms). The **implementing agent** chooses code-level solutions **unless** a chosen approach would **change** the game behavior defined in the spec; in that case, record the conflict in the spec **Decision Log** or ask the product owner before proceeding.

**`## Open Questions` section:** Every collaborative project spec SHOULD include `## Open Questions (resolve before / during implementation)`. Questions there MUST be phrased in **canonical domain vocabulary** (glossary + linked specs) and MUST target **definitions and intended game logic only**—not specific APIs, class names, or implementation mechanics. Technical investigation and coding strategy belong under **Implementation plan**, **Implementation investigation notes**, or the agent’s own workflow—not under Open Questions.

### Project docs outside `.cursor/specs/`

Charters and discovery for cross-cutting programs live under `docs/` as listed in `ARCHITECTURE.md`. The **territory-ia** MCP is documented in [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) and [`tools/mcp-ia-server/README.md`](tools/mcp-ia-server/README.md).

**Umbrella backlog programs** (one charter spec + phased child issues): **[TECH-21](.cursor/projects/TECH-21.md)** — **JSON** schemas, validation, indexes, runtime DTOs (**TECH-40** → **TECH-41** → **TECH-42**); **[TECH-36](.cursor/projects/TECH-36.md)** — **computational** **compute-lib** + Unity extractions + MCP tools (**TECH-37** → **TECH-38** → **TECH-39**). For **`backlog_issue`**, child rows still have their own **Spec** paths—read the umbrella charter when scope spans multiple phases.

## Terminology and information consistency

Keep **one vocabulary** across code, specs, rules, backlog, tutorials, and MCP descriptions so agents and humans search and reason reliably.

| Source | Use for |
|--------|---------|
| [`.cursor/specs/glossary.md`](.cursor/specs/glossary.md) | Canonical **domain** terms; always check before naming features, bugs, or user-facing copy in docs. |
| Linked specs (e.g. geography, roads) | **Definitions** trump glossary if they differ (glossary defers to spec). |
| [`.cursor/rules/coding-conventions.mdc`](.cursor/rules/coding-conventions.mdc) | C# identifiers, XML docs, prefab naming for **new** assets. |
| [`BACKLOG.md`](BACKLOG.md) | Issue id prefixes per **Issue ID convention** below; write **Files**, **Notes**, and **Acceptance** using the same words as specs/glossary. |
| [`tools/mcp-ia-server/`](tools/mcp-ia-server/) | **Tool names** (`snake_case`) must match `registerTool` in code; keep [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) and the package README in sync. |

**New or changed concepts:** update the **glossary** and the **relevant spec** section—do not leave terminology only in backlog entries or informal docs.

Cursor loads **`.cursor/rules/terminology-consistency.mdc`** (`alwaysApply`) as a short reminder; this section is the full checklist.

## Backlog Workflow

`BACKLOG.md` is the single source of truth for project issues.

### Issue ID convention

`BUG-XX` bugs | `FEAT-XX` features | `TECH-XX` tech debt | `ART-XX` art | `AUDIO-XX` audio

### Working on an issue

1. Prefer **`backlog_issue`** (territory-ia) for the issue id when MCP is enabled; otherwise read `BACKLOG.md`. If the issue is a **child** of **TECH-21** or **TECH-36**, skim the **umbrella** spec (`.cursor/projects/TECH-21.md` or `TECH-36.md`) for program intent, then open the **child** spec (**TECH-40**–**TECH-42** or **TECH-37**–**TECH-39**).
2. Read the files listed in the issue's "Files" field
3. Plan mode: analyze and propose a plan
4. Agent mode: implement, then move issue to "In progress"

### After implementing

Keep the issue **"In progress"**. Only move to "Completed" when the user explicitly confirms.

### Next issue and AI agent prompts

When the user asks which is the next issue, respond with it and **ask if they want an AI agent prompt** — a prompt for another agent to analyze, evaluate, and propose a development plan.

**Format:** Respond with Markdown; put full prompt inside a fenced code block tagged `markdown`. Prompt body in English unless user requests otherwise.

### Adding new issues

- Use next available ID in the appropriate category
- Include: Type, Files, Notes, Depends on (if applicable)
- Prefer `BACKLOG.md` + `.cursor/specs/` for durable rules

### Completing issues

Only when user confirms verification. Mark `[x]`, move to **Completed (last 30 days)** with date. Items older than ~7 days → `BACKLOG-ARCHIVE.md` (use the **Recent archive** section for batches moved from Completed). **`backlog_issue` MCP** returns **open** issues from `BACKLOG.md` only — completed-only ids may live only in the archive.

### Priority order

1. In progress
2. High priority (critical bugs, core gameplay blockers)
3. Medium priority (important features, balance issues)
4. Code Health (technical debt, refactors)
5. Low priority (new systems, polish, content)

## Pre-commit Checklist

- [ ] Code compiles (Build in Unity)
- [ ] Class-level `/// <summary>` exists and is accurate
- [ ] New public methods have XML documentation
- [ ] Debug.Log messages and comments are in English
- [ ] If GridManager was touched, verify sorting order with different height levels
- [ ] If roads were modified, verify `InvalidateRoadCache()` is called where needed
- [ ] If a new manager was added, it follows the Inspector + FindObjectOfType pattern
- [ ] New prefabs follow `coding-conventions.mdc` naming (do not rename existing assets)
- [ ] Temporary `Debug.Log` diagnostics follow `coding-conventions.mdc` (remove or gate before merge)
- [ ] Wording for touched domains matches `glossary.md` / linked specs (and backlog text stays consistent if the issue was edited)
