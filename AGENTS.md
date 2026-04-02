# AI Agent Guide — Territory Developer

## Before You Start

1. Check the `/// <summary>` on the class you are about to modify
2. Use `.cursor/rules/agent-router.mdc` to find the right specs for your task
3. **If asked to work on an issue:** read `BACKLOG.md` for context and priority

System invariants and guardrails are in `.cursor/rules/invariants.mdc` (always loaded).
Task-to-spec routing is in `.cursor/rules/agent-router.mdc` (always loaded).
Full dependency map is in `ARCHITECTURE.md`.

## Documentation hierarchy

```
.cursor/rules/   → Guardrails (auto-loaded by Cursor, light)
.cursor/specs/   → Deep reference (read on demand per task)
ARCHITECTURE.md  → System layers, dependency map
AGENTS.md        → This file: workflow, policies, checklist
BACKLOG.md       → Issue tracking (read only when relevant)
```

### `.cursor/specs/` inventory

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

Do not add bug write-ups, agent prompts, or one-off specs under `.cursor/specs/`. Use `BACKLOG.md` while work is open; delete temporary markdown after completion.

### `.cursor/projects/` policy

Project-specific specs for features or complex bugs **in active development** live under `.cursor/projects/`. These are **temporary** — deleted after verified completion.

| Aspect | Rule |
|--------|------|
| Template | `.cursor/templates/project-spec-template.md` |
| Naming | `{ISSUE_ID}.md` (e.g. `FEAT-44.md`, `BUG-45.md`) |
| Lifecycle | Create → refine → implement → verify → close |
| On completion | Migrate lessons learned to canonical docs before deleting |

### Project docs outside `.cursor/specs/`

Charters and discovery for cross-cutting programs live under `docs/` as listed in `ARCHITECTURE.md`.

## Backlog Workflow

`BACKLOG.md` is the single source of truth for project issues.

### Issue ID convention

`BUG-XX` bugs | `FEAT-XX` features | `TECH-XX` tech debt | `ART-XX` art | `AUDIO-XX` audio

### Working on an issue

1. Read `BACKLOG.md` to get the full issue context
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

Only when user confirms verification. Mark `[x]`, move to "Completed (last 30 days)" with date. Items older than ~7 days → `BACKLOG-ARCHIVE.md`.

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
