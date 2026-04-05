# Cursor Agent Skills — Territory Developer

Project-local **Cursor Skills** live here. Each skill is a folder with a **`SKILL.md`** file (Markdown + optional YAML frontmatter). Skills **orchestrate** workflows; **canonical facts** stay in `.cursor/specs/`, `BACKLOG.md`, and **territory-ia** MCP slices.

**Conventions** (folder naming, thin-skill rules, **`glossary_discover`** array requirement, **Tool recipe** pattern) are defined in this README. For the **study** write-up, see [`docs/cursor-agents-skills-mcp-study.md`](../../docs/cursor-agents-skills-mcp-study.md).

**MCP improvements** for richer discovery from project-spec prose: see [`BACKLOG.md`](../../BACKLOG.md) (**Agent** / **MCP** rows).

## Lessons learned (from shipped kickoff work)

- **`router_for_task`:** Pass **`domain`** strings that match **`.cursor/rules/agent-router.mdc`** “Task domain” row labels (e.g. `Save / load`, `Road logic, placement, bridges`). Ad-hoc phrases often return **`no_matching_domain`** — use the router table vocabulary.
- **`router_for_task`** **`files`:** You may pass **`files`** (repo-relative paths) with or instead of **`domain`**; the server merges path heuristics (**glossary** **territory-ia spec-pipeline layer B**).
- **`backlog_issue`** **`depends_on_status`:** Each cited **Depends on** id returns **`open`** / **`completed`** / **`not_in_backlog`**, **`soft_only`**, **`satisfied`** — use it in **kickoff** / **implement** / **close** / **project-new** recipes (**glossary** **territory-ia spec-pipeline layer B**).

## Conventions

| Rule | Detail |
|------|--------|
| **Folder name** | `kebab-case`, one folder per skill (e.g. `project-spec-kickoff`). |
| **Entry file** | `SKILL.md` at `.cursor/skills/{skill-name}/SKILL.md`. |
| **Frontmatter** | Include at least **`name`** and **`description`**. The **`description`** should state **when** the skill applies (triggers) so the IDE can surface it. |
| **Thin body** | Do **not** paste large chunks of **roads-system**, **isometric-geography-system**, or **water-terrain-system**. Point to **`spec_section`** / **`router_for_task`** via **territory-ia** instead. |
| **Glossary tools** | **`glossary_discover`** / **`glossary_lookup`** arguments must be **English** (translate from chat if needed). **`glossary_discover`** requires **`keywords` as a JSON array**, not a single string. |
| **Tool recipes** | For MCP-heavy skills, include a **numbered** “Tool recipe (territory-ia)” section so agents run tools in a **defined order**. |

## Index

| Skill folder | Purpose | Trace |
|--------------|---------|-------|
| [`project-new/`](project-new/SKILL.md) | Create a new **`BACKLOG.md`** row + **`.cursor/projects/{ISSUE_ID}.md`** stub from a user prompt (**territory-ia** + optional **`web_search`**) | [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md) |
| [`project-spec-kickoff/`](project-spec-kickoff/SKILL.md) | Review or enrich `.cursor/projects/{ISSUE_ID}.md` before implementation; ordered MCP context pull | *(shipped — archive)* |
| [`project-spec-implement/`](project-spec-implement/SKILL.md) | Execute a project spec’s **Implementation Plan** after the spec is ready; per-phase MCP slices + checklist | *(shipped — archive)* |
| [`project-implementation-validation/`](project-implementation-validation/SKILL.md) | After implementation: **Node** checks aligned with **IA tools** CI (dead spec paths, **MCP** tests, fixtures, **IA index** `--check`); optional **`verify`** | [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md) |
| [`project-spec-close/`](project-spec-close/SKILL.md) | Close an issue: persist IA → delete spec → `validate:dead-project-specs` → **remove** **`BACKLOG.md`** row → **append** **`BACKLOG-ARCHIVE.md`** → **id purge** | [`project-spec-close/SKILL.md`](project-spec-close/SKILL.md) |
| [`ui-hud-row-theme/`](ui-hud-row-theme/SKILL.md) | Add or adjust **HUD** / **menu** rows using **`UiTheme`** + **`ui-design-system.md`** | **glossary** **UI-as-code program**; **`ui-design-system.md`** **§5.2** |

**Planned / follow-up domain skills** (roads, terrain/water, new **MonoBehaviour** managers): see [`BACKLOG.md`](../../BACKLOG.md). **Spec pipeline program:** **glossary** **territory-ia spec-pipeline program**; charter [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md).

## Optional template

Copy-paste stub (no frontmatter): [`.cursor/templates/project-spec-review-prompt.md`](../templates/project-spec-review-prompt.md). **Kickoff** tool order is authoritative in **`project-spec-kickoff/SKILL.md`**; **implementation** order in **`project-spec-implement/SKILL.md`**; **post-implementation Node checks** in **`project-implementation-validation/SKILL.md`**; **closeout** order in **`project-spec-close/SKILL.md`**; **new issue + spec stub** workflow in **`project-new/SKILL.md`**.
