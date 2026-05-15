# stage-file Recipe Smoke Report — recipe-runner-phase-e Stage 2.1

**Task:** TECH-6967
**Run date:** 2026-04-30
**Recipe:** `tools/recipes/stage-file.yaml`
**Target:** Stage 2.2 (`recipe-runner-phase-e`)
**Run ID:** `76f27685ea8b5d71`

---

## §Smoke inputs

```json
{
  "slug": "recipe-runner-phase-e",
  "stage_id": "2.2",
  "issue_prefix": "TECH",
  "target_section": "## IA evolution lane"
}
```

---

## §Execution summary

| metric | value |
|---|---|
| Recipe exit | 0 (ok) |
| Steps executed | 19 (17 ok, 2 skipped) |
| `filed_count` | 3 |
| `target_section` | `## IA evolution lane` |
| `materialize_status` | 0 |

**Skipped steps (expected):**
- `surface_path_verify` — Stage 2.2 has no `relevant_surfaces` set → `when` falsy → warn-only gate correctly skips.
- `progress` — `when: ${materialize.code}` with `materialize.code = 0`; `coerceTruthy("0")` = false → gate skips. Non-blocking; dashboard regeneration is best-effort.

---

## §Tasks filed

| task_id | title | status |
|---|---|---|
| TECH-6968 | Recipe-parity audit + body trim for stage-authoring | pending |
| TECH-6969 | Live-dispatch seam.author-plan-digest | pending |
| TECH-6970 | Token-drop measurement contribution to C2 gate | pending |

All three rows confirmed in `ia_tasks` via DB query post-run.

---

## §Audit log (ia_recipe_runs)

All 19 step rows status = `ok` or `skipped` — zero `error` rows for run_id `76f27685ea8b5d71`.

---

## §Manifest parity

`ia/state/backlog-sections.json` — TECH-6968/6969/6970 appended to `## IA evolution lane` section.
`checklist_line` field contains bare title (no `- [ ] **{ID}**` prefix) — expected: recipe receives `${row.title}` from `pending_q`; post-recipe `task_raw_markdown_write` step (subagent) populates full formatted row.

---

## §BACKLOG.md parity

Tasks with `raw_markdown = NULL` produce zero output lines in `materialize-backlog-from-db.mjs` (trailing blank strip collapses empty body). Tasks will appear with full format after post-recipe `task_raw_markdown_write` subagent step. Expected behavior per recipe design comment: "raw_markdown: null; follow-up stage-authoring populates §Plan Digest".

---

## §Change-log

`ia_master_plan_change_log` row written:
- `kind`: `stage_status_flip`
- `body`: `Stage 2.2: Draft → In Progress (3 tasks filed)`

---

## §Validator

`validate:master-plan-status` exit 0 — 0 drift rows.

---

## §Pre-existing bugs fixed (scope: TECH-6967)

| file | bug | fix |
|---|---|---|
| `tools/scripts/recipe-engine/stage-file/cardinality-gate.sh` | `$(cat <<SQL ... SQL)` heredoc with `${slug//\'/\'\'}` — bash parser unmatched `'` at line 44 | Replaced heredoc with pre-sanitized variable + direct psql query string |
| `tools/scripts/recipe-engine/stage-file/sizing-gate.sh` | Same heredoc pattern at line 43 | Same fix |

Both bugs were pre-existing (recipe never reached `cardinality` gate before this stage: no prior smoke target had `status=pending` tasks reaching this gate).

---

## §Verdict

Recipe dispatch end-to-end: **PASS**. All 3 pending Stage 2.2 tasks filed, change-log written, manifest appended, validators green.
