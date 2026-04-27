# Mission

Detect drift between open master-plan Stage `arch_surfaces` declarations and `arch_changelog` entries that landed after each Stage was filed. Args: `SLUG?` (optional master-plan slug). Output: drift report + AskUserQuestion polling per affected Stage + `master_plan_change_log_append` rows. Plan markdown never rewritten.

# Recipe

Follow `ia/skills/arch-drift-scan/SKILL.md` end-to-end. Phase sequence (gated):

1. **Load plan(s)** — `mcp__territory-ia__master_plan_state({ slug })` per slug. `SLUG` unset → caller passes slug or runs per-plan; do NOT enumerate via filesystem. Plan missing → STOP, report `master_plan_not_found: {slug}`.
2. **Call arch_drift_scan** — `mcp__territory-ia__arch_drift_scan({ slug })`. Returns array of `{ stage_id, drifted_surfaces, changelog_kind, question }`. Empty → emit "no drift" line + skip Phases 3–5.
3. **Render drift report** — Markdown table: `| Stage | Drifted surfaces | Kind | Suggested resolution |`. Print before polling.
4. **AskUserQuestion polling per Stage** — ONE `AskUserQuestion` per affected Stage. Stem describes what drifted (decision / surface) + which Stage. Options fixed (3): `acknowledge` (Stage stays as-is) | `reword` (Stage objective / exit needs re-authoring) | `re-plan` (Stage tasks need split / reorder / new tasks). Stem + option labels use product/domain wording per `ia/rules/agent-human-polling.md` — NO tool / api / db jargon. Wait for answer; do NOT batch.
5. **Append change-log per resolution** — per resolved Stage: `mcp__territory-ia__master_plan_change_log_append({ slug, kind: "arch_drift_scan", actor: "arch-drift-scan", body: "Stage {stage_id} drift: {drifted_surfaces} | resolution: {acknowledge|reword|re-plan}" })`.

# Tool recipe

1. `mcp__territory-ia__master_plan_state` — plan title + stage list.
2. `mcp__territory-ia__arch_drift_scan` — affected stages + suggested questions.
3. `mcp__territory-ia__arch_decision_get` / `arch_decision_list` — decision details for richer stems (optional).
4. `mcp__territory-ia__arch_changelog_since` — narrative context (optional).
5. `mcp__territory-ia__master_plan_change_log_append` — one append per resolved Stage.

# Hard boundaries

- Do NOT auto-rewrite plan markdown — change-log only.
- Do NOT skip AskUserQuestion polling on any affected Stage.
- Do NOT batch multiple Stages into one AskUserQuestion call.
- Do NOT append change-log before AskUserQuestion returns.
- Do NOT load whole plan markdown when `master_plan_state` slice suffices.
- Do NOT commit — user decides.
- IF AskUserQuestion times out / user cancels → STOP, do NOT append for that Stage; carry pending Stages forward.
- IF `master_plan_change_log_append` rejects → STOP, surface error code.

# Output

Single concise caveman message per scanned plan:

1. plan slug + title.
2. drift count (affected stages).
3. resolutions tally (acknowledge / reword / re-plan).
4. change-log row count appended.
5. next step — `claude-personal "/master-plan-extend {SLUG}"` when any `reword` / `re-plan` resolved; else "no follow-up".
