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

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). No confirmation gate — all ops execute without human confirmation.

No MCP calls from skill body. Follow **Tool recipe** below in order.

Past friction / bug log → [`CHANGELOG.md`](CHANGELOG.md) (read on demand, not auto-loaded).

**Umbrella close** — once per spec, final stage. Per-stage closes → [`project-stage-close`](../project-stage-close/SKILL.md).

**Orchestrator guard:** closes project specs only. Refuse `*master-plan*`, `step-*-*.md`, `stage-*-*.md`. See [`orchestrator-vs-spec.md`](../../rules/orchestrator-vs-spec.md).

Do not delete `ia/projects/{ISSUE_ID}.md` until all IA persistence edits merged (or N/A with reason). No "Completed" section in BACKLOG — completed work only in [`BACKLOG-ARCHIVE.md`](../../../BACKLOG-ARCHIVE.md).

## Refactor fast path (`--refactor` flag)

Child tasks under `ia/projects/lifecycle-refactor-master-plan.md` (or any orchestrator whose durable IA is mid-rewrite) may invoke `--refactor` to skip steps deferred to the M8 batch:

- **Skip J1** (journal persist) — DB noise during refactor; capture in commit history.
- **Skip step 10 id purge** — refactor ids confined to feature branch; batched purge at M8 sign-off.
- **Skip multi-issue umbrella sync for siblings** — lifecycle-refactor children only roll under lifecycle-refactor master plan; no cross-orchestrator sweep.

Still runs: lessons migrate (when applicable), pre/post `validate:dead-project-specs`, archive yaml, BACKLOG row removal, `materialize-backlog`, spec delete, stage-close cascade + header-sync on the owning master plan. Default (no flag) runs all steps.

## IA persistence checklist

Walk per closure; N/A only when issue truly did not touch that surface:

| # | Target | Migrate |
|---|--------|---------|
| G1 | [`glossary.md`](../../../ia/specs/glossary.md) | New/changed domain terms; definitions from resolved Open Questions/Summary; Spec column → authoritative reference spec. No backlog ids. |
| R1 | [`ia/specs/*.md`](../../../ia/specs/) | Normative behavior, invariants, vocabulary that shipped. |
| A1 | [`ARCHITECTURE.md`](../../../ARCHITECTURE.md) | New/changed managers, layers, dependency facts. No backlog ids. |
| U1 | [`ia/rules/*.md`](../../../ia/rules/) | New guardrails or edits. |
| D1 | [`docs/`](../../../docs/) | Charters, how-tos; no closed-issue ids. |
| M1 | MCP docs + server | If tools changed: `index.ts`, `mcp-ia-server.md`, `README.md`. |
| I1 | Generated IA indexes | If G1/R1 changed bodies → `npm run generate:ia-indexes`; `--check` must pass for CI. |
| J1 | Postgres `ia_project_spec_journal` | Decision Log + Lessons via `project_spec_journal_persist` after G1–I1, before spec deletion. CLI: `npm run db:persist-project-journal`. Skip on no `DATABASE_URL`/`config/postgres-dev.json`. On `db_error`: do not delete spec until DB healthy or user waives. **Skipped when `--refactor`.** |

**Conflict rule:** Lesson/Decision contradicts reference spec → patch spec (or glossary + spec) in same batch or file follow-up BACKLOG item.

## Id purge (mandatory — skipped when `--refactor`)

After archiving, search repo for closed issue id — remove/rewrite every hit **except**: new `[x]` block in [`BACKLOG-ARCHIVE.md`](../../../BACKLOG-ARCHIVE.md) + existing archive history for that id, and open BACKLOG rows still tracking other work.

**Targets:** `ia/specs/glossary.md`, `ia/specs/*.md`, `ia/rules/*.md` (except terminology rule pattern line), `ia/skills/**/*.md`, `docs/**`, `projects/**`, `ARCHITECTURE.md`, `tools/**` docstrings, `Assets/**` comments. Rename committed files containing id when practical.

## Tool recipe (territory-ia) — closure session

Run in order. N/A → state why in chat.

0. **Disjoint-purge pre-flight** — before any destructive op: `rg --fixed-strings "{ISSUE_ID}" ia/ docs/ Assets/ ARCHITECTURE.md tools/ | grep -v "ia/backlog\|ia/backlog-archive\|ia/projects/{ISSUE_ID}"` → collect hit-file set. Acquire `flock ia/state/.closeout.lock` (separate from the id-counter lock). Under that lock: (a) read `ia/state/in-flight-closeouts.json` (create empty `[]` if absent); (b) drop entries with `started_at` older than 24 h (TTL purge); (c) check if any remaining entry's `hit_files` overlaps your hit-file set → STOP if overlap; (d) append `{ "issue_id": "{ISSUE_ID}", "started_at": "{ISO-8601-now}", "pid": <PID>, "hit_files": [...] }`; (e) rewrite file; (f) release lock. De-register on step 10 complete. Schema: `ia/state/in-flight-closeouts.schema.json`. **Skipped when `--refactor`** (no concurrent closeouts during refactor freeze).
1. **Verify precondition** — confirm implementation phases ticked in spec (read spec; stop if unticked phases found).
2. **`backlog_issue`** — refresh Files, Notes, Depends on, Acceptance, `depends_on_status`. Hard dep unsatisfied → resolve or user override.
3. **`project_spec_closeout_digest`** — structured extract. Unavailable → `read_file` fallback.
4. **IA persistence** — Apply G1–I1. Default: `router_for_task` + `spec_section` + `glossary_lookup` via MCP. Allowlist in closeout subagent is minimal (5 tools); fall back to `Read ia/specs/*.md` / `Read ia/rules/*.md` directly when a needed MCP slice tool is absent.
4b. **`project_spec_journal_persist`** — When DB URL resolves, persist Decision Log + Lessons (J1). Otherwise one-line skip. **Skipped when `--refactor`.**
4c. **Optional** — [`project-implementation-validation`](../project-implementation-validation/SKILL.md) after step 4 when I1 applies.
5. **`invariants_summary`** — When closure touches runtime C#, scene behavior, or guardrail docs. If tool not in allowlist, `Read ia/rules/invariants.md` directly.
6. **Multi-issue** — Patch umbrella/sibling `ia/projects/*.md`. **Mandatory for umbrella/master-plan orchestrators** (`*master-plan*.md`, `step-*-*.md`, `stage-*-*.md`): tick matching Phase checkbox(es), flip task-table Status column `Draft` → `Done` for the closing issue, update top-of-file `> **Status:**` pointer to next in-progress task. **When `--refactor`:** limited to the one owning orchestrator (lifecycle-refactor master plan) — no sibling-sweep.
6b. **Orchestrator stage-complete check** — After flipping task → `Done`, scan the parent stage's task table. If all tasks in that stage are now `Done` or `Done (archived)`, **automatically run `project-stage-close` inline on the orchestrator** before step 7.
6c. **Header-sync step/stage Status + Backlog state** — After task-row flip (and after any inline `project-stage-close` in 6b), rewrite every `**Status:**` paragraph and `**Backlog state (...):**` line under `### Step N — Title` (h3) and `#### Stage N.N — Title` (h4) headers in the touched master plan from task-table ground truth. Rules:
  - All task rows in block `Done (archived)` → `**Status:** Final`
  - Mix of `Done (archived)` + open → `**Status:** In Progress — {first-open-task-id}`
  - All `_pending_` → `**Status:** Draft (tasks _pending_ — not yet filed)`
  - `**Backlog state (Label):** k filed` where k = count of rows with a non-`_pending_` **Issue** cell.
  - Stage depth = `####` (h4); step depth = `###` (h3).
  - After all stage rewrites: if every sibling stage under a step reads `Final`, force the step `**Status:** Final`.
  - **R5 — Top-Status rollup to Final:** after all Step rewrites, check every `### Step N` block. If ALL Steps now read `**Status:** Final`, rewrite the plan top-of-file `> **Status:**` line to `Final`. If any Step is not Final, leave unchanged.
  - Rewrite idempotent — re-running on already-synced doc produces zero diff.
  - Helper: `tools/mcp-ia-server/src/parser/master-plan-header-sync.ts` exports `syncMasterPlanHeaders(markdown)`. Use when running Node-capable agent; otherwise apply the same logic inline via targeted `Edit` calls.
7. **Delete** `ia/projects/{ISSUE_ID}.md` — only after J1 succeeded/waived/skipped.
8. **Cascade** — `npm run validate:dead-project-specs`; fix hits or advisory with reason.
9. **BACKLOG + archive** — Write `ia/backlog-archive/{ISSUE_ID}.yaml` with `status: closed`, Notes citing where content migrated, `spec: ""` (removed-after-closure). **Then delete `ia/backlog/{ISSUE_ID}.yaml`** — use `rm ia/backlog/{ISSUE_ID}.yaml` and verify the file no longer exists before continuing (never leave both copies on disk). Also remove the issue entry from `ia/state/backlog-sections.json` if present. Run `node tools/scripts/materialize-backlog.mjs` (or `bash tools/scripts/materialize-backlog.sh` on Linux) to regenerate `BACKLOG.md` + `BACKLOG-ARCHIVE.md`. **Do NOT** edit `BACKLOG.md` or `BACKLOG-ARCHIVE.md` directly.
9b. **Regenerate progress dashboard** — `npm run progress` from repo root. Non-blocking — failure does NOT block close. Web dashboard auto-refreshes within ~5 min via ISR — no deploy needed.
10. **Id purge** — Per section above for `{ISSUE_ID}`. **Skipped when `--refactor`.**
11. **I1** — If glossary/spec bodies changed, `npm run generate:ia-indexes` + `--check`.

## Multi-issue

When spec references umbrella program or sibling `ia/projects/*.md`:
- Load umbrella/sibling specs (`read_file`/`backlog_issue` for related ids).
- Update Implementation Plan, Acceptance, Decision Log, Depends on for accuracy.
- **Umbrella/master-plan sync (mandatory):** tick Phase checkboxes the closed issue completed, set task-table Status `Draft`/`In Progress` → `Done`, refresh top-of-file `> **Status:**` pointer (`... / {NEXT_ISSUE} ({CLOSED_ISSUE} done)`).
- Do **before** deleting closed child spec.
- When `--refactor`: scope restricted to lifecycle-refactor master plan only.
