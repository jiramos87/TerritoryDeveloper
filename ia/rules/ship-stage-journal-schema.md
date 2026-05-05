# ia_ship_stage_journal — payload schema

Documents JSONB `payload` shapes per `payload_kind` value on `ia_ship_stage_journal`.
Additive only — no CHECK constraint on `payload_kind` column. Unknown kinds pass through.

---

## kind: `phase_checkpoint`

Written by ship-cycle Phase 3 (per task) and ship-plan Phases 6 + 7. Captures
decisions resolved + what to drop from ctx on resume.

```json
{
  "phase_id":            "ship-cycle.3.TECH-12345",
  "decisions_resolved":  ["TECH-12345:implemented", "TECH-12345:compile_check_pass"],
  "pending_decisions":   [],
  "next_phase":          "ship-cycle.3.TECH-12346",
  "ctx_drop_hint":       ["task_spec_body:TECH-12345", "compile_log:TECH-12345"]
}
```

| Field | Type | Notes |
|---|---|---|
| `phase_id` | string | Unique per-task/per-phase id. Pattern: `{skill}.{phase_seq}.{task_id or label}`. |
| `decisions_resolved` | string[] | Stable ids for decisions completed at this checkpoint. |
| `pending_decisions` | string[] | Decisions still open; resume reader must NOT skip this phase if non-empty. |
| `next_phase` | string | Hint for resume reader — what phase fires next. |
| `ctx_drop_hint` | string[] | Context keys caller may drop after checkpoint write. Not enforced — advisory. |

**Resume reader contract:**

- Query: `SELECT payload FROM ia_ship_stage_journal WHERE slug=$1 AND payload_kind='phase_checkpoint' ORDER BY recorded_at`
- Derive `resolved_phases = { row.payload.phase_id for each row }`
- Skip any phase whose `phase_id` is in `resolved_phases`
- Gate: only read when `target_version > 1 AND parent_plan_slug IS NOT NULL`. New plans skip the read.

**Lifecycle:** journal rows persist across sessions for the same `(slug, stage_id)`. Caller MUST clear
after `version_close` (ship-final closeout). Do NOT clear between Pass A + Pass B of the same session.

---

## kind: `drift_lint_summary` — on `ia_master_plan_change_log`

Written by ship-plan Phase 7 immediately after `master_plan_bundle_apply` succeeds.
Stored in `ia_master_plan_change_log`, NOT in `ia_ship_stage_journal`.

```json
{
  "anchor_failures":       [{"task_key": "TECH-12345", "ref": "tracer-test:Assets/Scripts/X.cs::DoFoo", "retried": 1, "resolved": true}],
  "glossary_warnings":     [{"task_key": "TECH-12345", "term": "wet-run", "canonical": "wet run", "replaced": true}],
  "retired_replacements":  [{"task_key": "TECH-12346", "from": "§Mechanical Steps", "to": "§Work Items", "count": 4}],
  "n_retried":    1,
  "n_resolved":   152,
  "n_unresolved": 0
}
```

| Field | Type | Notes |
|---|---|---|
| `anchor_failures` | object[] | Per-task anchor resolution failures; `resolved` = fixed after retry. |
| `glossary_warnings` | object[] | Per-task term mismatches; `replaced` = inline fix applied. |
| `retired_replacements` | object[] | Per-task retired-surface substitutions. |
| `n_retried` | int | Total anchors that needed retry. |
| `n_resolved` | int | Total items resolved (anchor + glossary + retired combined). |
| `n_unresolved` | int | Items that remain unresolved. Non-zero → ship-plan halts. |

**Write order guard (Review Note 5):** `master_plan_bundle_apply` MUST succeed before this row is written.
Version row must exist in `ia_master_plans` before `ia_master_plan_change_log` write — enforced by FK.

**Resume reader contract:**

- Phase 7 author prompt receives: `drift_lint_summary_id={row_id} ({n_resolved} resolved, {n_unresolved} unresolved)` — 1-line ref only.
- Phase 1 resume hook (when `parent_plan_slug` non-null): `SELECT * FROM ia_master_plan_change_log WHERE slug=$1 AND kind='drift_lint_summary' ORDER BY recorded_at DESC LIMIT 1` to skip already-corrected drift.
- `n_unresolved > 0` → ship-plan halts with `STOPPED — anchor_unresolved: drift_lint_summary_id={row_id}`.
