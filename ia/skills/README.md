# Cursor Agent Skills — Territory Developer

Project-local **Cursor Skills** live here. Each skill is a folder with a **`SKILL.md`** file (Markdown + optional YAML frontmatter). Skills **orchestrate** workflows; **canonical facts** stay in `ia/specs/`, `BACKLOG.md`, and **territory-ia** MCP slices.

**Lifecycle overview (exploration → close):** [`docs/agent-lifecycle.md`](../../docs/agent-lifecycle.md) is the canonical flow + handoff matrix + decision tree. Fetch on demand (not force-loaded). The index table below is alphabetical by skill; the lifecycle doc gives the ordered surface.

**Conventions** (folder naming, thin-skill rules, **`glossary_discover`** array requirement, **Tool recipe** pattern) are defined in this README. For the **study** write-up, see [`docs/cursor-agents-skills-mcp-study.md`](../../docs/cursor-agents-skills-mcp-study.md).

**MCP improvements** for richer discovery from project-spec prose: see [`BACKLOG.md`](../../BACKLOG.md) (**Agent** / **MCP** rows).

## Lessons learned (from shipped kickoff work)

- **`router_for_task`:** Pass **`domain`** strings that match **`ia/rules/agent-router.md`** “Task domain” row labels (e.g. `Save / load`, `Road logic, placement, bridges`). Ad-hoc phrases often return **`no_matching_domain`** — use the router table vocabulary.
- **`router_for_task`** **`files`:** You may pass **`files`** (repo-relative paths) with or instead of **`domain`**; the server merges path heuristics (**glossary** **territory-ia spec-pipeline layer B**).
- **`backlog_issue`** **`depends_on_status`:** Each cited **Depends on** id returns **`open`** / **`completed`** / **`not_in_backlog`**, **`soft_only`**, **`satisfied`** — use it in **kickoff** / **implement** / **close** / **project-new** recipes (**glossary** **territory-ia spec-pipeline layer B**).

## Conventions

| Rule | Detail |
|------|--------|
| **Folder name** | `kebab-case`, one folder per skill (e.g. `stage-authoring`). |
| **Entry file** | `SKILL.md` at `ia/skills/{skill-name}/SKILL.md`. |
| **Frontmatter** | Include at least **`name`** and **`description`**. The **`description`** should state **when** the skill applies (triggers) so the IDE can surface it. |
| **Thin body** | Do **not** paste large chunks of **roads-system**, **isometric-geography-system**, or **water-terrain-system**. Point to **`spec_section`** / **`router_for_task`** via **territory-ia** instead. |
| **Glossary tools** | **`glossary_discover`** / **`glossary_lookup`** arguments must be **English** (translate from chat if needed). **`glossary_discover`** requires **`keywords` as a JSON array**, not a single string. |
| **Tool recipes** | For MCP-heavy skills, include a **numbered** “Tool recipe (territory-ia)” section so agents run tools in a **defined order**. |

## Index

| Skill folder | Purpose | Trigger | Needs | Produces | Cleanup |
|---|---|---|---|---|---|
| [`design-explore/`](design-explore/SKILL.md) | Fuzzy survey → defined, reviewed design | Exploration doc exists; design decision not settled | `docs/{slug}.md` (any state) | `## Design Expansion` block persisted in same doc | none |
| [`master-plan-new/`](master-plan-new/SKILL.md) | Author orchestrator from Design Expansion | `## Design Expansion` block persisted in exploration doc | `docs/{slug}.md` with `## Design Expansion` block | `ia/projects/{slug}-master-plan.md` (permanent) | none |
| [`master-plan-extend/`](master-plan-extend/SKILL.md) | Append new Steps to existing orchestrator from exploration / extensions doc | Existing master plan + new source doc with expansion or deferred Steps | `ia/projects/{slug}-master-plan.md` + exploration / extensions doc | New `### Step {START}..{END}` blocks appended (fully decomposed) + header metadata synced | none |
| [`release-rollout/`](release-rollout/SKILL.md) | Drive one tracker row through next lifecycle cell (a)–(g) | Umbrella + sibling rollout tracker exist; row has non-`✓` column | `ia/projects/{umbrella-slug}-master-plan.md` + `ia/projects/{umbrella-slug}-rollout-tracker.md` + row slug | Tracker cell flipped + ticket + Change log row + next-row recommendation | none |
| [`release-rollout-enumerate/`](release-rollout-enumerate/SKILL.md) | Seed rollout tracker from umbrella bucket table | Umbrella master-plan exists; no tracker yet | `ia/projects/{umbrella-slug}-master-plan.md` | `ia/projects/{umbrella-slug}-rollout-tracker.md` with pre-filled matrix + disagreements appendix | none |
| [`release-rollout-track/`](release-rollout-track/SKILL.md) | Mechanical tracker cell flip + ticket + Change log | Downstream subagent returned success | Tracker path + row slug + target col + marker + ticket | Cell flip + Change log row | none |
| [`release-rollout-skill-bug-log/`](release-rollout-skill-bug-log/SKILL.md) | Dual-write skill bug entry to per-skill Changelog + tracker aggregator | Lifecycle skill misbehaved mid-rollout | Skill slug + bug summary + detail + fix status | Per-skill `## Changelog` entry + tracker `## Skill Iteration Log` row | none |
| [`stage-decompose/`](stage-decompose/SKILL.md) | Expand skeleton step in master plan in-place | Step in master plan underfilled after scope/pivot | Master plan with skeleton step | Step expanded to stages → phases → tasks in-place | none |
| [`stage-file/`](stage-file/SKILL.md) | Bulk-file `_pending_` tasks of one orchestrator stage | Stage decomposed; tasks `_pending_`, ready to file | Master plan with fully decomposed stage | BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs (one per task) | none |
| [`stage-authoring/`](stage-authoring/SKILL.md) | Opus bulk: §Plan Digest direct across N Task specs in one pass + DB persist | `/stage-authoring` after `stage-file` | N filed Task spec stubs | §Plan Digest per Task; lint + mechanicalization preflight | none |
| [`project-new/`](project-new/SKILL.md) | Single BACKLOG row + spec stub | One issue needed outside bulk stage filing | User prompt / task description | BACKLOG row + `ia/projects/{ISSUE_ID}.md` stub | none |
| [`opus-audit/`](opus-audit/SKILL.md) | Stage-scoped bulk audit: one Opus pass writes all N §Audit paragraphs | All Tasks in Stage reach post-verify Green | `ia/projects/{ISSUE_ID}.md` ×N with §Findings; shared Stage MCP bundle | N `§Audit` paragraphs written; feeds `/ship-stage` Pass B closeout | none |
| [`opus-code-review/`](opus-code-review/SKILL.md) | Per-Task Opus pair-head: diff vs spec + invariants → PASS / minor / critical | Task implement + verify-loop complete | `ia/projects/{ISSUE_ID}.md` §Implementation + §Findings; diff | §Code Review section written; §Code Fix Plan on critical verdict | none |
| [`plan-applier/`](plan-applier/SKILL.md) | Unified Sonnet pair-tail Mode plan-fix — applies §Plan Fix tuples; gates per `plan-apply-pair-contract.md` | Pair-head wrote tuples (`plan-review`) | Target §Plan section + paths in tuples | Tuples applied; per-mode validation gate | none |

| [`project-spec-implement/`](project-spec-implement/SKILL.md) | Execute Implementation Plan phase by phase | Implementation Plan filled; spec ready to ship | Enriched `ia/projects/{ISSUE_ID}.md` with Implementation Plan | Code changes + §7 phase ticks in spec | none |
| [`project-implementation-validation/`](project-implementation-validation/SKILL.md) | Post-implementation validation chain | Code landed on branch; need post-impl checks | Code changes on branch | Verification block (JSON + caveman summary) | none |
| [`verify-loop/`](verify-loop/SKILL.md) | Closed-loop verification with bounded fix iteration | Post-implementation; single-pass `/verify` not sufficient | Code changes on branch + Postgres running + Editor available | JSON Verification block; up to `MAX_ITERATIONS` fix commits | none |
| [`bridge-environment-preflight/`](bridge-environment-preflight/SKILL.md) | Verify Postgres + `agent_bridge_job` table before bridge commands | Before any `unity_bridge_command` call | Postgres at configured port | Exit code 0–4 + repair steps | none |
| [`ide-bridge-evidence/`](ide-bridge-evidence/SKILL.md) | Capture Unity console logs / screenshot via bridge | Need Play Mode evidence (logs, visual state) | Preflight passed + Editor running on `REPO_ROOT` | Console logs and/or screenshot artifact | none |
| [`close-dev-loop/`](close-dev-loop/SKILL.md) | Before/after `debug_context_bundle` diff + compile gate | Debugging anomaly; need diff across a code change | Preflight passed + `debug_context_bundle` baseline exists | Verdict (pass/fail) + anomaly diff | none |
| [`debug-sorting-order/`](debug-sorting-order/SKILL.md) | Sorting debug via `unity_export_*` + `spec_section` **geo** §7 | Isometric draw order / sorting looks wrong; bounded JSON exports suffice | Postgres + Editor; seed cell + chunk bounds | Before/after export diff vs §7 | none |
| [`agent-test-mode-verify/`](agent-test-mode-verify/SKILL.md) | Batchmode test-mode scenario loop | Need Unity scenario validation without manual Play Mode | Unity project + Postgres bridge + scenario id | Reports under `tools/reports/` | none |
| [`ui-hud-row-theme/`](ui-hud-row-theme/SKILL.md) | Add/adjust HUD/menu rows via UiTheme | Adding or adjusting a HUD or menu UI row | `UiTheme` + `ui-design-system.md` accessible in scene | UI row implementation in scene/prefab | none |
| [`unfold/`](unfold/SKILL.md) | Meta-tool. Linearize a composite slash-command invocation into one self-contained decision-tree plan (explicit `on_success` / `on_failure` edges, literal arg substitution, runtime-only values as `${placeholder}`). Read-only — NO execution, NO source edits, NO commits. | Preview a risky composite run; diff skill behavior across edits; hand fresh agent a plan without the skill runtime | `TARGET_COMMAND` + args; reads `.claude/commands/*.md` → `.claude/agents/*.md` → `ia/skills/*/SKILL.md` | `ia/plans/{cmd-slug}-{arg-slug}-unfold.md` | none |

**Planned / follow-up domain skills** (roads, terrain/water, new **MonoBehaviour** managers): see [`BACKLOG.md`](../../BACKLOG.md). **Spec pipeline program:** **glossary** **territory-ia spec-pipeline program**; charter [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md).

## Optional template

Copy-paste stub (no frontmatter): [`ia/templates/project-spec-review-prompt.md`](../templates/project-spec-review-prompt.md). **§Plan Digest** (canonical executable plan) authored by **`stage-authoring/SKILL.md`** (Opus bulk pass writes §Plan Digest direct + DB persist); **implementation** in **`project-spec-implement/SKILL.md`**; **post-implementation Node checks** in **`project-implementation-validation/SKILL.md`**; **agent test-mode** batch/bridge loop in **`agent-test-mode-verify/SKILL.md`**; optional **Unity** log/screenshot bridge in **`ide-bridge-evidence/SKILL.md`**; **Close Dev Loop** before/after **`debug_context_bundle`** in **`close-dev-loop/SKILL.md`**; **Stage closeout** runs inline in **`/ship-stage`** Pass B via `stage_closeout_apply` MCP; **new issue + spec stub** in **`project-new/SKILL.md`**.
