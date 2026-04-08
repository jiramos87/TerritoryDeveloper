# AI Agent Guide — Territory Developer

## Before You Start

1. Check the `/// <summary>` on the class you are about to modify
2. Use `.cursor/rules/agent-router.mdc` to find the right specs for your task
3. **Context from IA (Cursor agents):** In **Agent** chats with tools enabled, treat **territory-ia** MCP as the **default** way to load specs and rules unless a tool call truly cannot run. **Do not** open whole spec files with `read_file` when a slice suffices. Suggested order: **`backlog_issue`** when you have an issue id (`BUG-37`, `FEAT-44`, …) → `list_specs` (if keys unknown) → `router_for_task` for domain → `glossary_discover` / `glossary_lookup` → `spec_outline` / `spec_section` / `spec_sections` (or `invariants_summary`, `list_rules` / `rule_content` as needed). For **project-spec-close** on `.cursor/projects/{ISSUE_ID}.md`, use **`project_spec_closeout_digest`** after **`backlog_issue`** (see that skill). For **`glossary_discover`** and **`glossary_lookup`**, arguments must be **English** (the glossary is English-only): if the developer writes in another language, **translate** their concepts into English domain terms before calling. If MCP is disabled in the host, fall back to `.cursor/rules/agent-router.mdc` and targeted `read_file`. Stale content: MCP caches parses per server process—after large edits to a doc, prefer a fresh `read_file` on that path or restart the MCP server. Reference: [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md).
4. **If asked to work on an issue:** use **`backlog_issue`** for that id when MCP is available; otherwise read `BACKLOG.md` (and see `BACKLOG.md` for priority and workflow).
5. **Project specs:** Stubs use [`.cursor/templates/project-spec-template.md`](.cursor/templates/project-spec-template.md), including **`## 7b. Test Contracts`** (tooling checks — [`.cursor/projects/PROJECT-SPEC-STRUCTURE.md`](.cursor/projects/PROJECT-SPEC-STRUCTURE.md) list item **7b**). When **creating** a new **BACKLOG** issue and **`.cursor/projects/{ISSUE_ID}.md`** from a user prompt, use [`.cursor/skills/project-new/SKILL.md`](.cursor/skills/project-new/SKILL.md). When **reviewing or enriching** `.cursor/projects/{ISSUE_ID}.md` before code, use [`.cursor/skills/project-spec-kickoff/SKILL.md`](.cursor/skills/project-spec-kickoff/SKILL.md) (or the paste template at [`.cursor/templates/project-spec-review-prompt.md`](.cursor/templates/project-spec-review-prompt.md)). When **executing** the spec’s **Implementation Plan**, use [`.cursor/skills/project-spec-implement/SKILL.md`](.cursor/skills/project-spec-implement/SKILL.md). After **MCP** / **schema** / **IA index**–touching work, use [`.cursor/skills/project-implementation-validation/SKILL.md`](.cursor/skills/project-implementation-validation/SKILL.md) for **CI**-aligned **Node** checks (optional but recommended); on a configured dev machine, **`npm run verify:local`** runs **`validate:all`** plus the **Unity** / **Postgres** chain ([`ARCHITECTURE.md`](ARCHITECTURE.md) **Local verification**). When **closing** the issue after verified work (migrate IA, delete the temporary spec, **move the row to** [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md), **purge the id from durable docs**), use [`.cursor/skills/project-spec-close/SKILL.md`](.cursor/skills/project-spec-close/SKILL.md). **Kickoff**, **implement**, and **close** define ordered **territory-ia** recipes where applicable (`backlog_issue` → `project_spec_closeout_digest` when closing a project spec → `invariants_summary` when applicable → `router_for_task` → `spec_section` / `spec_sections` → `glossary_*` → …); **`project-new`** uses a **create-first** tool order (see that skill). Skill index and authoring rules: [`.cursor/skills/README.md`](.cursor/skills/README.md).
6. **IDE agent bridge — Play Mode smoke:** **Before** the first bridge call in a session, run **`npm run db:bridge-preflight`** (exit codes 0–4; bounded repair policy in [`.cursor/skills/bridge-environment-preflight/SKILL.md`](.cursor/skills/bridge-environment-preflight/SKILL.md)). When **`unity_bridge_command`** is available, **Postgres** resolves, and **Unity Editor** is open on the repository root, **run the Play Mode sequence yourself** for bridge or close-loop verification: **`get_play_mode_status`** → **`enter_play_mode`** → **`get_play_mode_status`** → **`exit_play_mode`**. Prefer that over asking the human to click **Play**/**Stop**. Optional **`capture_screenshot`** (**`include_ui: true`**) for **Game view** visibility. After **C#** edits, use **`unity_compile`** or **`unity_bridge_command`** with **`kind`:** **`get_compilation_status`** for a compile snapshot before re-entering **Play Mode**; when no **Editor** holds the project lock, root **`npm run unity:compile-check`** (**`UNITY_EDITOR_PATH`**) is available. For **local post-implementation** checks (dev machine, **not** CI), run **`npm run verify:local`** from the repository root (**`validate:all`** including **`territory-compute-lib`** build, then batch compile, **`db:migrate`**, **`db:bridge-preflight`**, **macOS** Editor save/quit + relaunch + **`db:bridge-playmode-smoke`**). **`npm run verify:post-implementation`** is an alias. See [`ARCHITECTURE.md`](ARCHITECTURE.md) (**Local verification**) and [`.cursor/skills/project-implementation-validation/SKILL.md`](.cursor/skills/project-implementation-validation/SKILL.md). Full before/after **debug_context_bundle** loop: [`.cursor/skills/close-dev-loop/SKILL.md`](.cursor/skills/close-dev-loop/SKILL.md). See [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) (**Implementation and operations**) and [`.cursor/skills/ide-bridge-evidence/SKILL.md`](.cursor/skills/ide-bridge-evidence/SKILL.md).

System invariants and guardrails are in `.cursor/rules/invariants.mdc` (always loaded).
Task-to-spec routing is in `.cursor/rules/agent-router.mdc` (always loaded).
Full dependency map is in `ARCHITECTURE.md`.

## Documentation hierarchy

```
docs/information-architecture-overview.md → IA system overview: philosophy, layers, lifecycle, extension guide
.cursor/rules/        → Guardrails (auto-loaded by Cursor, light)
.cursor/skills/       → Cursor Agent Skills (thin workflows; see README — **project-new**, **project-spec-kickoff**, **project-spec-implement**, **project-implementation-validation**, **ide-bridge-evidence**, **bridge-environment-preflight**, **close-dev-loop**, **project-spec-close**)
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
| Dead path check | Before/after deleting a project spec, run `npm run validate:dead-project-specs` (repo root) so durable docs do not keep links to `.cursor/projects/{ISSUE_ID}.md`. Use **`BACKLOG.md`** / **`BACKLOG-ARCHIVE.md`** by issue id for the durable trace. See **PROJECT-SPEC-STRUCTURE** — **Closeout checklist**. Advisory-only: `node tools/validate-dead-project-spec-paths.mjs --advisory` or `CI_DEAD_SPEC_ADVISORY=1`. |

**Requirements vs implementation:** When authoring or extending a project spec, separate **product / game-logic** content (what the player and simulation rules do—using [`.cursor/specs/glossary.md`](.cursor/specs/glossary.md) terms) from **implementation** content (files, classes, algorithms). The **implementing agent** chooses code-level solutions **unless** a chosen approach would **change** the game behavior defined in the spec; in that case, record the conflict in the spec **Decision Log** or ask the product owner before proceeding.

**`## Open Questions` section:** Every collaborative project spec SHOULD include `## Open Questions (resolve before / during implementation)`. Questions there MUST be phrased in **canonical domain vocabulary** (glossary + linked specs) and MUST target **definitions and intended game logic only**—not specific APIs, class names, or implementation mechanics. Technical investigation and coding strategy belong under **Implementation plan**, **Implementation investigation notes**, or the agent’s own workflow—not under Open Questions.

### Project docs outside `.cursor/specs/`

Charters and discovery for cross-cutting programs live under `docs/` as listed in `ARCHITECTURE.md`. The **territory-ia** MCP is documented in [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) and [`tools/mcp-ia-server/README.md`](tools/mcp-ia-server/README.md).

**Umbrella programs (charters):** **JSON interchange program** — **glossary** row + [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md), [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md), [`ARCHITECTURE.md`](ARCHITECTURE.md), [`projects/json-use-cases-brainstorm.md`](projects/json-use-cases-brainstorm.md); **Postgres** dev surfaces (**Dev repro bundle**, **Editor export registry**, **Program extension mapping (E1–E3)**) in the same docs; **charter trace** [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md). **Compute-lib program** — **glossary** **Compute-lib program**; **charter trace** [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md); ongoing work [`BACKLOG.md`](BACKLOG.md) **§ Compute-lib program**. **Durable IA** does **not** embed backlog issue ids — see [`.cursor/rules/terminology-consistency.mdc`](.cursor/rules/terminology-consistency.mdc).

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

1. Prefer **`backlog_issue`** (territory-ia) for the issue id when MCP is enabled; otherwise read `BACKLOG.md`. If the issue sits under **§ Compute-lib program** (see **glossary** **Compute-lib program**), read that **BACKLOG** section and any **`.cursor/projects/{ISSUE_ID}.md`** on the row. For **JSON** interchange scope, use **glossary** **JSON interchange program** and [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md). For **Postgres** / **E1**–**E3** extension work, use that doc **Program extension mapping** plus [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md) (**Dev repro bundle**, **Editor export registry**). For **compute-lib** tooling, use **glossary** **territory-compute-lib**, **Computational MCP tools**, **C# compute utilities**. Then open the **child** spec or backlog row.
2. Read the files listed in the issue's "Files" field
3. Plan mode: analyze and propose a plan
4. Agent mode: implement, then move issue to "In progress"

### After implementing

Keep the issue **"In progress"** until the user confirms verification.

If the work used a temporary **project spec** (`.cursor/projects/{ISSUE_ID}.md`) and you are **closing** out: follow [`.cursor/skills/project-spec-close/SKILL.md`](.cursor/skills/project-spec-close/SKILL.md) — **persist** lessons to **glossary**, **reference specs**, **`ARCHITECTURE.md`**, **`.cursor/rules/`**, and **`docs/`** (and **MCP** docs if tools changed) **before** deleting the spec; run `npm run validate:dead-project-specs`; **remove** the row from [`BACKLOG.md`](BACKLOG.md); **append** **`[x]`** to [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md); **purge** the closed issue id from durable IA and code per that skill. **Glossary** defines **project-spec-close** and **project-implementation-validation**.

### Next issue and AI agent prompts

When the user asks which is the next issue, respond with it and **ask if they want an AI agent prompt** — a prompt for another agent to analyze, evaluate, and propose a development plan.

**Format:** Respond with Markdown; put full prompt inside a fenced code block tagged `markdown`. Prompt body in English unless user requests otherwise.

### Adding new issues

- **Issue id (per prefix):** Use the **next** number for the chosen prefix (`BUG-`, `FEAT-`, `TECH-`, `ART-`, `AUDIO-`). Scan **[`BACKLOG.md`](BACKLOG.md)** and **[`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md)** for the **highest** existing number with that prefix, then assign **max + 1**. **Do not reuse** an id that already appears in **either** file — archived rows keep that id for traceability, and **`backlog_issue`** resolves **`BACKLOG.md`** first, then **`BACKLOG-ARCHIVE.md`**.
- Include: Type, Files, Notes, Depends on (if applicable)
- Prefer `BACKLOG.md` + `.cursor/specs/` for durable rules

### Completing issues

Only when the user confirms verification. **Remove** the row from [`BACKLOG.md`](BACKLOG.md) and **append** **`[x]`** (with date) to [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) — there is **no** completed section in **BACKLOG.md**. Strip the closed issue id from **glossary**, **reference specs**, **rules**, **skills**, `docs/`, and code comments (**project-spec-close**). **`backlog_issue` MCP** resolves ids from **`BACKLOG.md`** first, then **`BACKLOG-ARCHIVE.md`**.

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
- [ ] If you changed links or **`Spec:`** lines for `.cursor/projects/*.md`, run `npm run validate:dead-project-specs` (repo root)
- [ ] If you changed **`tools/mcp-ia-server`**, **`docs/schemas`**, **`.cursor/specs`** bodies that feed **IA indexes**, or **`glossary.md`**, follow [`.cursor/skills/project-implementation-validation/SKILL.md`](.cursor/skills/project-implementation-validation/SKILL.md) (or run **`npm run validate:all`** / the full local chain **`npm run verify:local`** when Postgres + Unity bridge apply — see [`ARCHITECTURE.md`](ARCHITECTURE.md) **Local verification** and [`.github/workflows/ia-tools.yml`](.github/workflows/ia-tools.yml))
