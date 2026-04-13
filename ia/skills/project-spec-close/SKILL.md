---
purpose: "Use when closing a BACKLOG issue that used a temporary ia/projects/{ISSUE_ID}.md: migrate lessons to glossary/specs/ARCHITECTURE/rules/docs/MCP; delete the project spec; move the row to BACKLOG-ARCHIVE immediately;…"
audience: agent
loaded_by: skill:project-spec-close
slices_via: none
name: project-spec-close
description: >
  Use when closing a BACKLOG issue that used a temporary ia/projects/{ISSUE_ID}.md: migrate lessons
  to glossary/specs/ARCHITECTURE/rules/docs/MCP; delete the project spec; move the row to BACKLOG-ARCHIVE
  immediately; strip the closed issue id from all durable docs and code. Triggers: "close project spec",
  "complete issue", "closure", "migrate lessons and delete spec", "project spec closeout",
  "finish FEAT-xx / BUG-xx spec".
---

# Project spec close (verified issue / spec closure)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). **Additional exception:** confirmation prompts before destructive operations stay normal English (spec deletion, BACKLOG row removal, archive append, id purge require explicit human confirmation).

No MCP calls from skill body. Follow **Tool recipe** below in order.

**Related:** [`project-new`](../project-new/SKILL.md) · [`project-spec-kickoff`](../project-spec-kickoff/SKILL.md) · [`project-spec-implement`](../project-spec-implement/SKILL.md) · [`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md) (optional Play evidence) · `npm run validate:dead-project-specs` · MCP `project_spec_closeout_digest` / `project_spec_journal_persist` / `spec_sections` ([`docs/mcp-ia-server.md`](../../../docs/mcp-ia-server.md)). **Policy:** [`terminology-consistency.md`](../../../ia/rules/terminology-consistency.md), [`PROJECT-SPEC-STRUCTURE.md`](../../projects/PROJECT-SPEC-STRUCTURE.md).

After implement → this skill closes loop: persist IA → delete spec → validate → archive row → purge id.

**Umbrella close** — once per spec, final stage. Per-stage closes → [`project-stage-close`](../project-stage-close/SKILL.md).

**Orchestrator guard:** closes project specs only. Refuse `*master-plan*`, `step-*-*.md`, `stage-*-*.md`. See [`orchestrator-vs-spec.md`](../../rules/orchestrator-vs-spec.md).

Do not delete `ia/projects/{ISSUE_ID}.md` until all IA persistence edits merged (or N/A with reason). No “Completed” section in BACKLOG — completed work only in [`BACKLOG-ARCHIVE.md`](../../../BACKLOG-ARCHIVE.md).

## IA persistence checklist

Walk list per closure; N/A only when issue truly did not touch that surface:

| # | Target | Migrate |
|---|--------|---------|
| G1 | [`glossary.md`](../../../ia/specs/glossary.md) | New/changed domain terms; definitions from resolved Open Questions/Summary; Spec column → authoritative reference spec. No backlog ids. |
| R1 | [`ia/specs/*.md`](../../../ia/specs/) | Normative behavior, invariants, vocabulary that shipped. Follow [`REFERENCE-SPEC-STRUCTURE.md`](../../../ia/specs/REFERENCE-SPEC-STRUCTURE.md). |
| A1 | [`ARCHITECTURE.md`](../../../ARCHITECTURE.md) | New/changed managers, layers, dependency facts. No backlog ids. |
| U1 | [`ia/rules/*.md`](../../../ia/rules/) | New guardrails or edits. |
| D1 | [`docs/`](../../../docs/) | Charters, how-tos; no closed-issue ids. |
| M1 | MCP docs + server | If tools changed: `index.ts`, `mcp-ia-server.md`, `README.md`. |
| I1 | Generated IA indexes | If G1/R1 changed bodies → `npm run generate:ia-indexes`; `--check` must pass for CI. |
| J1 | Postgres `ia_project_spec_journal` | Decision Log + Lessons via `project_spec_journal_persist` after G1–I1, before spec deletion. CLI: `npm run db:persist-project-journal`. Skip when no `DATABASE_URL`/`config/postgres-dev.json`. On `db_error`: do not delete spec until DB healthy or user waives. |

**Conflict rule:** Lesson/Decision contradicts reference spec → patch spec (or glossary + spec) in same batch or file follow-up BACKLOG item.

## Id purge (mandatory)

After archiving, search repo for closed issue id — remove/rewrite every hit **except**: new `[x]` block in [`BACKLOG-ARCHIVE.md`](../../../BACKLOG-ARCHIVE.md) + existing archive history for that id, and open BACKLOG rows still tracking other work.

**Targets:** `ia/specs/glossary.md`, `ia/specs/*.md`, `ia/rules/*.md` (except terminology rule pattern line), `ia/skills/**/*.md`, `docs/**`, `projects/**`, `ARCHITECTURE.md`, `tools/**` docstrings, `Assets/**` comments. Rename committed files containing id when practical.

## Tool recipe (territory-ia) — closure session

Run in order. N/A → state why in chat.

1. **User confirmation** — Implementation verified? (Single gate for rest of closeout.)
2. **`backlog_issue`** — refresh Files, Notes, Depends on, Acceptance, `depends_on_status`. Hard dep unsatisfied → resolve or user override.
3. **`project_spec_closeout_digest`** — structured extract. Unavailable → `read_file` fallback.
4. **IA persistence** — Apply G1–I1 via `router_for_task` + `spec_section`/`spec_sections` + `glossary_discover`/`glossary_lookup` (English). `list_rules`/`rule_content` for rules edits.
4b. **`project_spec_journal_persist`** — When DB URL resolves, persist Decision Log + Lessons (J1). Otherwise one-line skip.
4c. **Optional** — [`project-implementation-validation`](../project-implementation-validation/SKILL.md) after step 4 when I1 applies.
5. **`invariants_summary`** — When closure touches runtime C#, scene behavior, or guardrail docs.
6. **Multi-issue** — Patch umbrella/sibling `ia/projects/*.md`. **Mandatory for umbrella/master-plan orchestrators** (`*master-plan*.md`, `step-*-*.md`, `stage-*-*.md`): tick matching Phase checkbox(es), flip task-table Status column `Draft` → `Done` for the closing issue, update top-of-file `> **Status:**` pointer to next in-progress task. Optional: `npm run closeout:dependents -- --issue {ISSUE_ID}`.
7. **Delete** `ia/projects/{ISSUE_ID}.md` — only after J1 succeeded/waived/skipped.
8. **Cascade** — `npm run validate:dead-project-specs`; fix hits or advisory with reason.
9. **BACKLOG + archive** — Remove row from BACKLOG. Append `[x]` row with date to BACKLOG-ARCHIVE; `Spec:` → removed-after-closure pattern; Notes cite where content migrated.
10. **Id purge** — Per section above for `{ISSUE_ID}`.
11. **I1** — If glossary/spec bodies changed, `npm run generate:ia-indexes` + `--check`.

## Multi-issue

When spec references umbrella program or sibling `ia/projects/*.md`:
- Load umbrella/sibling specs (`read_file`/`backlog_issue` for related ids).
- Update Implementation Plan, Acceptance, Decision Log, Depends on for accuracy.
- **Umbrella/master-plan sync (mandatory):** tick Phase checkboxes the closed issue completed, set task-table Status `Draft`/`In Progress` → `Done`, refresh top-of-file `> **Status:**` pointer (`... / {NEXT_ISSUE} ({CLOSED_ISSUE} done)`).
- Do **before** deleting closed child spec.

## Manual fallback (no local Node)

No `npm` → search repo for `ia/projects/{ISSUE_ID}.md`; fix markdown links + BACKLOG `Spec:` lines; prefer CI IA tools workflow.

## Branching

`router_for_task` with agent-router domain labels → `spec_section`/`spec_sections`. No full `ia/specs/*.md` unless unavoidable.
- **Roads/bridges/wet run** → roads-system + isometric-geography-system.
- **Water/HeightMap/shore** → water-terrain-system + geo sections.
- **Save/load/DTO** → persistence-system; no on-disk Save data changes unless issue required.

## Efficiency shortcuts

- `project_spec_closeout_digest` — replaces ad-hoc parsing (step 3).
- `spec_sections` — batch slice fetch (step 4).
- `npm run closeout:worksheet -- --issue {ISSUE_ID}` — printable worksheet (`--json` for raw).
- `npm run closeout:dependents -- --issue {ISSUE_ID}` — citation scan (step 6).
- `npm run closeout:verify` — `validate:dead-project-specs` + `generate:ia-indexes --check` (after I1).

## Seed prompt (parameterize)

Replace `{SPEC_PATH}` and `{ISSUE_ID}` (and optional umbrella id in **Multi-issue** notes).

```markdown
Close @{SPEC_PATH} (issue **{ISSUE_ID}**) following **project-spec-close**’s **IA persistence checklist**, **Tool recipe**, and **Id purge** in order.
**Before** deleting the project spec: migrate content into [glossary](../../../ia/specs/glossary.md), [`ia/specs/`](../../../ia/specs/), [`ARCHITECTURE.md`](../../../ARCHITECTURE.md), [`ia/rules/`](../../../ia/rules/), [`docs/`](../../../docs/), and **MCP** docs if tools changed — per [terminology-consistency](../../../ia/rules/terminology-consistency.md) (no backlog ids in durable IA).
Reconcile umbrella/sibling `ia/projects/*.md` if applicable. **Then** delete the project spec, run `npm run validate:dead-project-specs`, **remove the row from BACKLOG.md**, **append to BACKLOG-ARCHIVE.md**, and **strip `{ISSUE_ID}`** from the rest of the repo (except open BACKLOG rows and archive).
Use **territory-ia**: `backlog_issue` → `project_spec_closeout_digest` → `router_for_task` / `spec_section` / `spec_sections` / `glossary_*` / `list_rules` as needed → **`project_spec_journal_persist`** when **`DATABASE_URL`** is set → `invariants_summary` if runtime or guardrails touched. Optional: `npm run closeout:dependents -- --issue {ISSUE_ID}` before umbrella/sibling edits.
```
