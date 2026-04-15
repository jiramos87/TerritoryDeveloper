# Cursor Agent Skills — Territory Developer

Project-local **Cursor Skills** live here. Each skill is a folder with a **`SKILL.md`** file (Markdown + optional YAML frontmatter). Skills **orchestrate** workflows; **canonical facts** stay in `ia/specs/`, `BACKLOG.md`, and **territory-ia** MCP slices.

**Lifecycle overview (exploration → close):** [`docs/agent-lifecycle.md`](../../docs/agent-lifecycle.md) is the canonical flow + handoff matrix + decision tree. Always-loaded anchor: [`ia/rules/agent-lifecycle.md`](../rules/agent-lifecycle.md). The index table below is alphabetical by skill; the lifecycle doc gives the ordered surface.

**Conventions** (folder naming, thin-skill rules, **`glossary_discover`** array requirement, **Tool recipe** pattern) are defined in this README. For the **study** write-up, see [`docs/cursor-agents-skills-mcp-study.md`](../../docs/cursor-agents-skills-mcp-study.md).

**MCP improvements** for richer discovery from project-spec prose: see [`BACKLOG.md`](../../BACKLOG.md) (**Agent** / **MCP** rows).

## Lessons learned (from shipped kickoff work)

- **`router_for_task`:** Pass **`domain`** strings that match **`ia/rules/agent-router.md`** “Task domain” row labels (e.g. `Save / load`, `Road logic, placement, bridges`). Ad-hoc phrases often return **`no_matching_domain`** — use the router table vocabulary.
- **`router_for_task`** **`files`:** You may pass **`files`** (repo-relative paths) with or instead of **`domain`**; the server merges path heuristics (**glossary** **territory-ia spec-pipeline layer B**).
- **`backlog_issue`** **`depends_on_status`:** Each cited **Depends on** id returns **`open`** / **`completed`** / **`not_in_backlog`**, **`soft_only`**, **`satisfied`** — use it in **kickoff** / **implement** / **close** / **project-new** recipes (**glossary** **territory-ia spec-pipeline layer B**).

## Conventions

| Rule | Detail |
|------|--------|
| **Folder name** | `kebab-case`, one folder per skill (e.g. `project-spec-kickoff`). |
| **Entry file** | `SKILL.md` at `ia/skills/{skill-name}/SKILL.md`. |
| **Frontmatter** | Include at least **`name`** and **`description`**. The **`description`** should state **when** the skill applies (triggers) so the IDE can surface it. |
| **Thin body** | Do **not** paste large chunks of **roads-system**, **isometric-geography-system**, or **water-terrain-system**. Point to **`spec_section`** / **`router_for_task`** via **territory-ia** instead. |
| **Glossary tools** | **`glossary_discover`** / **`glossary_lookup`** arguments must be **English** (translate from chat if needed). **`glossary_discover`** requires **`keywords` as a JSON array**, not a single string. |
| **Tool recipes** | For MCP-heavy skills, include a **numbered** “Tool recipe (territory-ia)” section so agents run tools in a **defined order**. |

## Index

| Skill folder | Purpose | Trace |
|--------------|---------|-------|
| [`design-explore/`](design-explore/SKILL.md) | Move an exploration doc from fuzzy survey to defined design: compare approaches → select → expand → architecture → subsystem impact → impl points → examples → subagent review → persist. Seeds a master plan or `project-new`. | `docs/` exploration docs |
| [`master-plan-new/`](master-plan-new/SKILL.md) | Author `ia/projects/{slug}-master-plan.md` orchestrator from an exploration doc's persisted `## Design Expansion`: decompose Implementation Points into step > stage > phase > task skeleton, tasks seeded `_pending_`, cardinality gate (≥2 tasks per phase). Hands off to `stage-file`. | `ia/rules/project-hierarchy.md`, `ia/rules/orchestrator-vs-spec.md` |
| [`project-new/`](project-new/SKILL.md) | Create a new **`BACKLOG.md`** row + **`ia/projects/{ISSUE_ID}.md`** stub from a user prompt (**territory-ia** + optional **`web_search`**) | [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md) |
| [`stage-decompose/`](stage-decompose/SKILL.md) | Expand a deferred skeleton step (Steps 2+) in an existing master plan into stages → phases → tasks in-place. MCP context from step Exit criteria + surfaces; cardinality + task-sizing gate; edits orchestrator doc only. Does NOT create BACKLOG rows. Bridges `master-plan-new` (skeleton author) → `stage-file` (task filer). | `ia/rules/project-hierarchy.md`, `ia/rules/orchestrator-vs-spec.md` |
| [`stage-file/`](stage-file/SKILL.md) | Bulk-file all `_pending_` tasks of one orchestrator stage as BACKLOG rows + project spec stubs. Each task delegates to **`project-new`**; shared stage context loaded once; phase/task cardinality enforced. | `ia/rules/project-hierarchy.md` |
| [`project-spec-kickoff/`](project-spec-kickoff/SKILL.md) | Review or enrich `ia/projects/{ISSUE_ID}.md` before implementation; ordered MCP context pull | *(shipped — archive)* |
| [`project-spec-implement/`](project-spec-implement/SKILL.md) | Execute a project spec’s **Implementation Plan** after the spec is ready; per-phase MCP slices + checklist | *(shipped — archive)* |
| [`project-implementation-validation/`](project-implementation-validation/SKILL.md) | After implementation: **`npm run validate:all`** (**compute-lib** build + dead spec paths, **MCP** tests, fixtures, **IA index** `--check`); **`npm run verify:local`** (canonical full dev chain + **macOS** bridge smoke; **`verify:post-implementation`** alias); optional **`verify`** | [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md) |
| [`ide-bridge-evidence/`](ide-bridge-evidence/SKILL.md) | Optional **Unity** **Play Mode** evidence via **`unity_bridge_command`** (**`get_console_logs`**, **`capture_screenshot`**, **`include_ui`**) — **Postgres** + Editor on **REPO_ROOT**; **N/A** in CI | **unity-development-context** §10, [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) |
| [`bridge-environment-preflight/`](bridge-environment-preflight/SKILL.md) | **Bridge preflight:** verify Postgres + **`agent_bridge_job`** before bridge commands; exit codes 0–4, bounded repair | [`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md), **glossary** **IDE agent bridge** |
| [`close-dev-loop/`](close-dev-loop/SKILL.md) | **Close Dev Loop:** before/after **`debug_context_bundle`**, compile gate (**`unity_compile`** / **`get_compilation_status`**, **`unity:compile-check`**, **`get_console_logs`**), diff **`bundle.anomalies`**, verdict | **glossary** **IDE agent bridge**, [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) |
| [`agent-test-mode-verify/`](agent-test-mode-verify/SKILL.md) | **Agent test-mode loop:** **batchmode** / queue file + **test mode** scenario, exports under **`tools/reports/`**, bounded iterate, handoff for human **QA** — **standalone** or after **`project-spec-implement`** | [`projects/TECH-31a3-agent-test-mode-verify-skill.md`](../../projects/TECH-31a3-agent-test-mode-verify-skill.md) (**TECH-31** stage **31a3**); [`BACKLOG.md`](../../BACKLOG.md) **TECH-31** |
| [`verify-loop/`](verify-loop/SKILL.md) | **Integrated closed-loop verification:** preflight → `validate:all` → compile gate → Path A test-mode batch and / or Path B bridge hybrid → Play Mode evidence → bounded fix→verify iteration → JSON Verification block + caveman handoff. Composes `bridge-environment-preflight`, `project-implementation-validation`, `agent-test-mode-verify`, `ide-bridge-evidence`, `close-dev-loop`. | [`docs/agent-led-verification-policy.md`](../../docs/agent-led-verification-policy.md) |
| [`project-spec-close/`](project-spec-close/SKILL.md) | **Umbrella close** at the end of a spec: persist IA → delete spec → `validate:dead-project-specs` → **remove** **`BACKLOG.md`** row → **append** **`BACKLOG-ARCHIVE.md`** → **id purge** | [`project-spec-close/SKILL.md`](project-spec-close/SKILL.md) |
| [`project-stage-close/`](project-stage-close/SKILL.md) | **Per-stage close** for multi-stage project specs: tick stage's §7 phases, update **Last updated**, append §6 Decision Log / §9 Issues Found / §10 Lessons Learned, optional Postgres journal, emit paste-ready **handoff** for next stage's fresh agent. Does **not** touch BACKLOG / archive / spec deletion (that is the umbrella `project-spec-close`). | *(shipped — archive)* |
| [`ui-hud-row-theme/`](ui-hud-row-theme/SKILL.md) | Add or adjust **HUD** / **menu** rows using **`UiTheme`** + **`ui-design-system.md`** | **`ui-design-system.md`** **§5.2** |

**Planned / follow-up domain skills** (roads, terrain/water, new **MonoBehaviour** managers): see [`BACKLOG.md`](../../BACKLOG.md). **Spec pipeline program:** **glossary** **territory-ia spec-pipeline program**; charter [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md).

## Optional template

Copy-paste stub (no frontmatter): [`ia/templates/project-spec-review-prompt.md`](../templates/project-spec-review-prompt.md). **Kickoff** tool order is authoritative in **`project-spec-kickoff/SKILL.md`**; **implementation** order in **`project-spec-implement/SKILL.md`**; **post-implementation Node checks** in **`project-implementation-validation/SKILL.md`**; **agent test-mode** batch/bridge loop in **`agent-test-mode-verify/SKILL.md`**; optional **Unity** log/screenshot bridge in **`ide-bridge-evidence/SKILL.md`**; **Close Dev Loop** before/after **`debug_context_bundle`** in **`close-dev-loop/SKILL.md`**; **closeout** order in **`project-spec-close/SKILL.md`**; **new issue + spec stub** workflow in **`project-new/SKILL.md`**.
