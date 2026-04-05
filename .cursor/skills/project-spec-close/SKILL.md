---
name: project-spec-close
description: >
  Use when closing a BACKLOG issue that used a temporary .cursor/projects/{ISSUE_ID}.md: migrate lessons
  to glossary/specs/ARCHITECTURE/rules/docs/MCP; delete the project spec; move the row to BACKLOG-ARCHIVE
  immediately; strip the closed issue id from all durable docs and code. Triggers: "close project spec",
  "complete issue", "closure", "migrate lessons and delete spec", "project spec closeout",
  "finish FEAT-xx / BUG-xx spec".
---

# Project spec close (verified issue / spec closure)

This skill **does not** call MCP tools itself. In an **Agent** chat with **territory-ia** enabled, follow the **Tool recipe (territory-ia)** below in order.

**Related:** [`project-new`](../project-new/SKILL.md), [`project-spec-kickoff`](../project-spec-kickoff/SKILL.md), [`project-spec-implement`](../project-spec-implement/SKILL.md), `npm run validate:dead-project-specs` ([`tools/validate-dead-project-spec-paths.mjs`](../../../tools/validate-dead-project-spec-paths.mjs)), MCP **`project_spec_closeout_digest`**, **`project_spec_journal_persist`**, **`spec_sections`** + root **`closeout:*`** ([`docs/mcp-ia-server.md`](../../../docs/mcp-ia-server.md)). **Conventions:** [`.cursor/skills/README.md`](../README.md). **IA policy:** [`.cursor/rules/terminology-consistency.mdc`](../../../.cursor/rules/terminology-consistency.mdc), [`.cursor/projects/PROJECT-SPEC-STRUCTURE.md`](../../projects/PROJECT-SPEC-STRUCTURE.md).

## Relationship to kickoff / implement

- After [`project-spec-implement`](../project-spec-implement/SKILL.md) finishes implementation, use **this** skill to **close** the loop: persist IA → delete spec → validate → **archive row** → **purge closed id from durable surfaces** (same session once the user confirms verification).

## Normative closeout order

**Do not delete** `.cursor/projects/{ISSUE_ID}.md` until **all** applicable **IA persistence** edits below are merged (or explicitly recorded as N/A with a one-line reason in chat).

**There is no “Completed” section in `BACKLOG.md`.** Completed work exists **only** in [`BACKLOG-ARCHIVE.md`](../../../BACKLOG-ARCHIVE.md).

## IA persistence checklist

For each closure, walk this list; tick **N/A** only when the closed issue truly did not touch that surface:

| # | Durable target | What to migrate from the project spec |
|---|----------------|--------------------------------------|
| G1 | [`.cursor/specs/glossary.md`](../../../.cursor/specs/glossary.md) | New or changed **domain** terms; **definitions** from resolved **Open Questions** / **Summary**; ensure **Spec** column points at authoritative **reference spec** sections. **Do not** embed backlog issue ids in new or edited rows ([`terminology-consistency.mdc`](../../../.cursor/rules/terminology-consistency.mdc)). |
| R1 | [`.cursor/specs/*.md`](../../../.cursor/specs/) (**reference specs**) | **Normative** behavior, invariants, or vocabulary that shipped. Follow [**REFERENCE-SPEC-STRUCTURE.md**](../../../.cursor/specs/REFERENCE-SPEC-STRUCTURE.md). |
| A1 | [`ARCHITECTURE.md`](../../../ARCHITECTURE.md) | New/changed **managers**, layers, or dependency facts. **No** backlog id citations. |
| U1 | [`.cursor/rules/*.mdc`](../../../.cursor/rules/) | New guardrails or edits. Exception: [`terminology-consistency.mdc`](../../../.cursor/rules/terminology-consistency.mdc) may mention id *pattern* for BACKLOG files only. |
| D1 | [`docs/`](../../../docs/) | Charters, how-tos; **no** closed-issue id strings unless the file is explicitly archive-oriented. |
| M1 | **MCP** docs + server | If tools changed: [`tools/mcp-ia-server/src/index.ts`](../../../tools/mcp-ia-server/src/index.ts), [`docs/mcp-ia-server.md`](../../../docs/mcp-ia-server.md), [`tools/mcp-ia-server/README.md`](../../../tools/mcp-ia-server/README.md). |
| I1 | **Generated IA indexes** | If **G1** or **R1** changed bodies that feed indexes, run `npm run generate:ia-indexes` from repo root; ensure **`generate:ia-indexes -- --check`** passes where **CI** expects it. |
| J1 | **Postgres** **`ia_project_spec_journal`** ([**glossary** **IA project spec journal**](../../../.cursor/specs/glossary.md)) | **Verbose** **Decision Log** + **Lessons learned** only — use MCP **`project_spec_journal_persist`** (same `issue_id` / `spec_path` as **`project_spec_closeout_digest`**, optional `git_sha`) **after** normative **G1–I1** edits, **before** deleting the project spec. **CLI:** `npm run db:persist-project-journal` from repo root. **Skip** with one chat line when neither **`DATABASE_URL`** nor [`config/postgres-dev.json`](../../../config/postgres-dev.json) yields a URL (or **CI** skips the file fallback without **`DATABASE_URL`**). **On `db_error`:** do **not** delete `.cursor/projects/{ISSUE_ID}.md` until **DB** is healthy or the user **explicitly waives** journal capture. |

**Conflict rule:** If a **Lesson** or **Decision Log** entry **contradicts** a **reference spec**, **patch the spec** (or **glossary** + spec) in the same closure batch **or** file a **follow-up** **BACKLOG** item — do not leave silent drift.

## Id purge (mandatory for the closed issue)

After drafting the archived row, **search the repository** for the closed issue id (e.g. `FEAT-44`, `BUG-12`, `TECH-59`) and **remove or rewrite** every hit **except**:

- The new **`[x]`** block in [`BACKLOG-ARCHIVE.md`](../../../BACKLOG-ARCHIVE.md) for that closure (and any **existing** archive rows that already reference that id historically), and
- **Open** rows in [`BACKLOG.md`](../../../BACKLOG.md) that are **still** tracking other work (do not remove *their* ids).

**Targets for purge:** `.cursor/specs/glossary.md`, `.cursor/specs/*.md` reference specs, `.cursor/rules/*.mdc` (except the terminology rule’s generic pattern line), `.cursor/skills/**/*.md`, `docs/**`, `projects/**`, `ARCHITECTURE.md`, `tools/**` docstrings/comments, `Assets/**` comments, **renaming** committed files whose **names** contain the id when practical.

**Do not** strip the id from **other** issues’ open **BACKLOG** rows or from **unrelated** archive history unless you are explicitly closing that issue too.

## Tool recipe (territory-ia) — closure session

Run **in order** unless a step is **N/A** (state why in chat).

1. **User confirmation** — Implementation **verified**? (Single confirmation gates the rest of the closeout.)
2. **`backlog_issue`** with `issue_id` — refresh **Files**, **Notes**, **Depends on**, **Acceptance**, and **`depends_on_status`**. If any entry has **`satisfied`: false** and **`soft_only`** false, resolve or get explicit user override before spending effort on closeout.
3. **`project_spec_closeout_digest`** with `issue_id` **or** `spec_path` — structured extract. If the tool is unavailable, fall back to **`read_file`** on `.cursor/projects/{ISSUE_ID}.md`.
4. **IA persistence (edit durable docs)** — Apply checklist rows **G1–I1** using **`router_for_task`** + **`spec_section`** / **`spec_sections`** + **`glossary_discover`** / **`glossary_lookup`** (**English**). Use **`list_rules`** / **`rule_content`** when editing **`.mdc`**.
4b. **`project_spec_journal_persist`** — When a DB URL resolves (**`DATABASE_URL`** or [`config/postgres-dev.json`](../../../config/postgres-dev.json), not **CI**-skipped), persist **Decision Log** + **Lessons learned** to **`ia_project_spec_journal`** (see checklist **J1**). Otherwise note one-line skip.
4c. **Optional (`project-implementation-validation`)** — After step 4 (and **I1** when applicable), consider [`project-implementation-validation`](../project-implementation-validation/SKILL.md) before step 7.
5. **`invariants_summary`** — When closure touches **runtime** **C#**, scene behavior, or **invariants** / guardrail docs.
6. **Multi-issue** — Patch umbrella / sibling `.cursor/projects/*.md` for **honesty**. Optional: `npm run closeout:dependents -- --issue {ISSUE_ID}` from repo root, then verify manually.
7. **Delete** `.cursor/projects/{ISSUE_ID}.md` — only after **J1** succeeded or was **waived** / **skipped** (**db_unconfigured**).
8. **Cascade** — `npm run validate:dead-project-specs` from repo root **or** user relies on **CI**; fix hits **or** advisory mode only with explicit reason (**PROJECT-SPEC-STRUCTURE** closeout lessons).
9. **BACKLOG + archive (immediate)** — **Remove** the issue’s row from **[`BACKLOG.md`](../../../BACKLOG.md)**. **Append** a **`[x]`** row with date to **[`BACKLOG-ARCHIVE.md`](../../../BACKLOG-ARCHIVE.md)**; **`Spec:`** → removed-after-closure pattern; **Notes** cite where **glossary** / **reference spec** / **ARCHITECTURE** / rules / **docs** now hold migrated content.
10. **Id purge** — Run **Id purge** section above for `{ISSUE_ID}`.
11. **I1** — If glossary/spec bodies changed, `npm run generate:ia-indexes` and **`--check`** as required by **CI**.

## Multi-issue (umbrella / siblings)

When **Depends on** or the project spec references an **umbrella** program (**glossary**: **JSON interchange program**, **Compute-lib program**, …) or **sibling** `.cursor/projects/*.md`:

- Load umbrella or sibling specs (`read_file` and/or **`backlog_issue`** for related ids).
- Update **Implementation Plan**, **Acceptance**, **Decision Log**, and **Depends on** so completed vs pending work is accurate.
- Do this **before** deleting the closed child spec.

## Manual fallback (no local Node)

If `npm run validate:dead-project-specs` cannot run: search the repo for the `.cursor/projects/{ISSUE_ID}.md` filename; fix **markdown** links and **BACKLOG** **`Spec:`** lines; prefer **CI** **IA tools** workflow when available.

## Branching (when editing reference specs)

Mirror **project-spec-kickoff** / **project-spec-implement**: **`router_for_task`** with **`.cursor/rules/agent-router.mdc`** domain labels → **`spec_section`** or **`spec_sections`** — do not load entire `.cursor/specs/*.md` files unless unavoidable.

- **Roads / bridges / wet run** → **roads-system** + **isometric-geography-system**.
- **Water / HeightMap / shore / river** → **water-terrain-system** + **geo** sections.
- **Save / load / DTO** → **persistence-system**; do **not** change on-disk **Save data** unless the issue required it.

## Efficiency

- **`project_spec_closeout_digest`** — one call replaces ad-hoc Markdown parsing for step 3.
- **`spec_sections`** — batch slice fetch for step 4.
- **`npm run closeout:worksheet -- --issue {ISSUE_ID}`** — printable Markdown worksheet (`--json` for raw digest).
- **`npm run closeout:dependents -- --issue {ISSUE_ID}`** — citation scan for step 6.
- **`npm run closeout:verify`** — after persistence when **I1** may apply: **`validate:dead-project-specs`** + **`generate:ia-indexes --check`**.

## Seed prompt (parameterize)

Replace `{SPEC_PATH}` and `{ISSUE_ID}` (and optional umbrella id in **Multi-issue** notes).

```markdown
Close @{SPEC_PATH} (issue **{ISSUE_ID}**) following **project-spec-close**’s **IA persistence checklist**, **Tool recipe**, and **Id purge** in order.
**Before** deleting the project spec: migrate content into [glossary](../../../.cursor/specs/glossary.md), [`.cursor/specs/`](../../../.cursor/specs/), [`ARCHITECTURE.md`](../../../ARCHITECTURE.md), [`.cursor/rules/`](../../../.cursor/rules/), [`docs/`](../../../docs/), and **MCP** docs if tools changed — per [terminology-consistency](../../../.cursor/rules/terminology-consistency.mdc) (no backlog ids in durable IA).
Reconcile umbrella/sibling `.cursor/projects/*.md` if applicable. **Then** delete the project spec, run `npm run validate:dead-project-specs`, **remove the row from BACKLOG.md**, **append to BACKLOG-ARCHIVE.md**, and **strip `{ISSUE_ID}`** from the rest of the repo (except open BACKLOG rows and archive).
Use **territory-ia**: `backlog_issue` → `project_spec_closeout_digest` → `router_for_task` / `spec_section` / `spec_sections` / `glossary_*` / `list_rules` as needed → **`project_spec_journal_persist`** when **`DATABASE_URL`** is set → `invariants_summary` if runtime or guardrails touched. Optional: `npm run closeout:dependents -- --issue {ISSUE_ID}` before umbrella/sibling edits.
```
