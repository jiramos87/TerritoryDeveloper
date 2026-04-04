---
name: project-spec-close
description: >
  Use when closing a BACKLOG issue that used a temporary .cursor/projects/{ISSUE_ID}.md: migrate lessons
  and decisions to glossary, reference specs, ARCHITECTURE.md, rules, and docs; delete the project spec;
  validate dead spec paths; then move BACKLOG to Completed after user confirms. Triggers: "close project spec",
  "complete issue", "TECH-xx closure", "migrate lessons and delete spec", "project spec closeout",
  "finish FEAT-xx / BUG-xx spec".
---

# Project spec close (verified issue / spec closure)

This skill **does not** call MCP tools itself. In an **Agent** chat with **territory-ia** enabled, follow the **Tool recipe (territory-ia)** below in order.

**Related:** **[`project-new`](../project-new/SKILL.md)** (create issue + spec stub from prompt — **TECH-56** completed), **[`project-spec-kickoff`](../project-spec-kickoff/SKILL.md)** (before code), **[`project-spec-implement`](../project-spec-implement/SKILL.md)** (shipping phases), **TECH-50** completed — `npm run validate:dead-project-specs` ([`tools/validate-dead-project-spec-paths.mjs`](../../../tools/validate-dead-project-spec-paths.mjs)), **TECH-58** — MCP **`project_spec_closeout_digest`** / **`spec_sections`** + root **`closeout:*`** scripts ([`docs/mcp-ia-server.md`](../../../docs/mcp-ia-server.md)). **Conventions:** [`.cursor/skills/README.md`](../README.md). **IA policy:** [`.cursor/rules/terminology-consistency.mdc`](../../../.cursor/rules/terminology-consistency.mdc), [`.cursor/projects/PROJECT-SPEC-STRUCTURE.md`](../../projects/PROJECT-SPEC-STRUCTURE.md) — **Closeout checklist** / **Lessons learned** (**TECH-50** / **TECH-51** / **TECH-52** closures). Shipped **TECH-51** (completed — [`BACKLOG.md`](../../../BACKLOG.md) **§ Completed**); **glossary** — **project-spec-close**, **project-implementation-validation** (**TECH-52** completed), **project-new** (**TECH-56** completed).

## Relationship to kickoff / implement

- After **[`project-spec-implement`](../project-spec-implement/SKILL.md)** finishes implementation, use **this** skill to **close** the loop: persist IA → delete spec → validate → **BACKLOG** **Completed** (user-confirmed).

## Normative closeout order

**Do not delete** `.cursor/projects/{ISSUE_ID}.md` until **all** applicable **IA persistence** edits below are merged (or explicitly recorded as N/A with a one-line reason in chat). **Do not** move the issue to **BACKLOG** **Completed** until **after** the spec file is deleted and **`validate:dead-project-specs`** is clean (or advisory mode is explicitly justified per **PROJECT-SPEC-STRUCTURE** — **Lessons learned (TECH-50 closure)**).

## IA persistence checklist

For each closure, walk this list; tick **N/A** only when the closed issue truly did not touch that surface:

| # | Durable target | What to migrate from the project spec |
|---|----------------|--------------------------------------|
| G1 | [`.cursor/specs/glossary.md`](../../../.cursor/specs/glossary.md) | New or changed **domain** terms; **definitions** from resolved **Open Questions** / **Summary**; ensure **Spec** column points at authoritative **reference spec** sections. |
| R1 | [`.cursor/specs/*.md`](../../../.cursor/specs/) (**reference specs**) | **Normative** behavior, invariants, or vocabulary that shipped; patch the **authoritative** file (e.g. **isometric-geography-system.md** for shared geo/road/water). Follow [**REFERENCE-SPEC-STRUCTURE.md**](../../../.cursor/specs/REFERENCE-SPEC-STRUCTURE.md). |
| A1 | [`ARCHITECTURE.md`](../../../ARCHITECTURE.md) | New/changed **managers**, layers, or dependency facts if the issue altered system structure or doc topology. |
| U1 | [`.cursor/rules/*.mdc`](../../../.cursor/rules/) | New guardrails or edits to **alwaysApply** / router rules. Use **`list_rules`** / **`rule_content`** when unsure. |
| D1 | [`docs/`](../../../docs/) | Charters, how-tos, study docs, any **project** doc that must outlive the spec. |
| M1 | **MCP** docs + server | If tools were added/renamed: [`tools/mcp-ia-server/src/index.ts`](../../../tools/mcp-ia-server/src/index.ts), [`docs/mcp-ia-server.md`](../../../docs/mcp-ia-server.md), [`tools/mcp-ia-server/README.md`](../../../tools/mcp-ia-server/README.md) — **snake_case** per **terminology-consistency**. |
| I1 | **Generated IA indexes** | If **G1** or **R1** changed bodies that feed indexes, run `npm run generate:ia-indexes` from repo root (see root `package.json`) and ensure **`generate:ia-indexes -- --check`** passes where **CI** expects it. |

**Conflict rule:** If a **Lesson** or **Decision Log** entry **contradicts** a **reference spec**, **patch the spec** (or **glossary** + spec) in the same closure batch **or** file a **follow-up** **BACKLOG** item — do not leave silent drift.

## Tool recipe (territory-ia) — closure session

Run **in order** unless a step is **N/A** (state why in chat).

1. **User confirmation** — Implementation **verified**? Do **not** edit **BACKLOG** **Completed** until the user confirms (chat guardrail; no tool).
2. **`backlog_issue`** with `issue_id` — refresh **Files**, **Notes**, **Depends on**, **Acceptance**, and **`depends_on_status`**. If any entry has **`satisfied`: false** and **`soft_only`** false, resolve or get explicit user override before spending effort on closeout.
3. **`project_spec_closeout_digest`** with `issue_id` **or** `spec_path` — structured extract (`sections`, `cited_issue_ids`, `suggested_english_keywords`, heuristic `checklist_hints`). Use this instead of a raw **`read_file`** for step-3 parsing; still **edit** durable docs yourself (no auto-merge). If the tool is unavailable, fall back to **`read_file`** on `.cursor/projects/{ISSUE_ID}.md`.
4. **IA persistence (edit durable docs)** — Apply the **IA persistence checklist** using **`router_for_task`** + **`spec_section`** or **`spec_sections`** (batch when several headings are needed) + **`glossary_discover`** / **`glossary_lookup`** (**English** keywords). When **`router_for_task`** **`domain`** strings fail, pass **`files`** built from digest / backlog **Files** paths (**glossary** **territory-ia spec-pipeline layer B (TECH-62)**). Use **`list_rules`** / **`rule_content`** when editing **`.mdc`**. If closure touches simulation / roads / water / **Save** / UI, pull slices before editing reference specs (see **Branching** below).
4b. **Optional (`project-implementation-validation` — TECH-52 completed)** — If the closed work touched **`tools/mcp-ia-server`**, **`docs/schemas`**, or **reference spec** / **glossary** bodies that feed **IA indexes**, consider **[`project-implementation-validation`](../project-implementation-validation/SKILL.md)** after step 4 (and after **I1** regen when applicable) and **before** step 8. This runs **CI**-aligned **Node** checks; it does **not** replace **IA persistence** or mandatory **`validate:dead-project-specs`** in step 8.
5. **`invariants_summary`** — When closure touches **runtime** **C#**, scene behavior, or **invariants** / guardrail docs.
6. **Multi-issue** — Patch umbrella / sibling `.cursor/projects/*.md` for **honesty** (**Depends on**, **Implementation Plan**, **Acceptance**). Optional: from repo root, `npm run closeout:dependents -- --issue {ISSUE_ID}` for a first-pass list of citations (then verify manually). See **Multi-issue (umbrella / siblings)** below.
7. **Delete** `.cursor/projects/{ISSUE_ID}.md`.
8. **Cascade** — `npm run validate:dead-project-specs` from repo root **or** user relies on **CI**; fix hits **or** advisory mode only with explicit reason (**PROJECT-SPEC-STRUCTURE** — **TECH-50** lessons).
9. **BACKLOG.md** — Move issue to **Completed (last 30 days)** with date; **`Spec:`** → removed-after-closure pattern; **Notes** cite where **glossary** / **reference spec** / **ARCHITECTURE** / rules / **docs** now hold migrated content. **Only after** user confirms the backlog move ([**AGENTS.md**](../../../AGENTS.md)).

## Multi-issue (umbrella / siblings)

When **Depends on** or the project spec references an **umbrella** program (**TECH-21**, **TECH-36**, …) or **sibling** `.cursor/projects/*.md`:

- Load umbrella or sibling specs (`read_file` and/or **`backlog_issue`** for related ids).
- Update **Implementation Plan**, **Acceptance**, **Decision Log**, and **Depends on** so completed vs pending work is accurate.
- Do this **before** deleting the closed child spec.

## Manual fallback (no local Node)

If `npm run validate:dead-project-specs` cannot run: search the repo for the `.cursor/projects/{ISSUE_ID}.md` filename; fix **markdown** links and **BACKLOG** **`Spec:`** lines; prefer **CI** **IA tools** workflow when available.

## Branching (when editing reference specs)

Mirror **project-spec-kickoff** / **project-spec-implement**: **`router_for_task`** with **`.cursor/rules/agent-router.mdc`** domain labels → **`spec_section`** or **`spec_sections`** for needed slices — do not load entire `.cursor/specs/*.md` files unless unavoidable.

- **Roads / bridges / wet run** → **roads-system** + **isometric-geography-system**.
- **Water / HeightMap / shore / river** → **water-terrain-system** + **geo** sections.
- **Save / load / DTO** → **persistence-system**; do **not** change on-disk **Save data** unless the issue required it.

## Efficiency (TECH-58)

- **`project_spec_closeout_digest`** — one call replaces ad-hoc Markdown parsing for step 3.
- **`spec_sections`** — batch slice fetch for step 4 when multiple **`spec_section`** reads would follow.
- **`npm run closeout:worksheet -- --issue {ISSUE_ID}`** — printable Markdown worksheet (`--json` for raw digest).
- **`npm run closeout:dependents -- --issue {ISSUE_ID}`** — citation scan for step 6 (see script limitations).
- **`npm run closeout:verify`** — after persistence when **I1** may apply: runs **`validate:dead-project-specs`** + **`generate:ia-indexes --check`** locally; **CI** **ia-tools** remains authoritative.

## Extra scripts / MCP

Tracked closeout helpers ship under **TECH-58** (MCP tools above + root **`closeout:*`** scripts). For **missing project-spec path** detection, keep using **`npm run validate:dead-project-specs`** (**TECH-50**). For **BACKLOG id validation** inside project specs, **TECH-30** / shared **Node** helpers remain separate backlog work — do not invent ad-hoc scanners; file a **BACKLOG** row if a new check is needed.

## Seed prompt (parameterize)

Replace `{SPEC_PATH}` and `{ISSUE_ID}` (and optional umbrella id in **Multi-issue** notes).

```markdown
Close @{SPEC_PATH} (issue **{ISSUE_ID}**) following **project-spec-close**’s **IA persistence checklist** and **Tool recipe** in order.
**Before** deleting the project spec: migrate **Lessons Learned**, **Decision Log**, and resolved **Open Questions** into [glossary](../../../.cursor/specs/glossary.md), the relevant **reference spec** sections under [`.cursor/specs/`](../../../.cursor/specs/), [`ARCHITECTURE.md`](../../../ARCHITECTURE.md), [`.cursor/rules/`](../../../.cursor/rules/), [`docs/`](../../../docs/), and **MCP** docs if tools changed — per [terminology-consistency](../../../.cursor/rules/terminology-consistency.mdc).
Reconcile umbrella/sibling `.cursor/projects/*.md` if applicable. **Then** delete the project spec, run `npm run validate:dead-project-specs`, and only **then** prepare the **BACKLOG.md** **Completed** row (user must confirm the backlog move per **AGENTS.md**).
Use **territory-ia**: `backlog_issue` → `project_spec_closeout_digest` → `router_for_task` / `spec_section` / `spec_sections` / `glossary_*` / `list_rules` as needed → `invariants_summary` if runtime or guardrails touched. Optional: `npm run closeout:dependents -- --issue {ISSUE_ID}` before umbrella/sibling edits.
```
