`$ARGUMENTS` = `{SLUG?}`. Optional master-plan slug. Omit → scan all open plans (caller passes slug or runs per-plan).

## Subagent prompt (forward verbatim)

Forward via Agent tool with `subagent_type: "arch-drift-scan"`:

> Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, AskUserQuestion question stems + option labels (product/domain wording per `ia/rules/agent-human-polling.md`). Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `arch-drift-scan` skill (`ia/skills/arch-drift-scan/SKILL.md`) end-to-end against the master-plan slug given in `$ARGUMENTS` (or all open plans when slug absent). Detect drift between Stage `arch_surfaces` declarations and `arch_changelog` entries written after each Stage was filed. Render drift report + poll user per affected Stage + persist resolutions to plan change-log.
>
> ## Phase sequence (gated)
>
> 1. **Load plan(s)** — `master_plan_state({ slug })` per slug. `master_plan_not_found` → STOP.
> 2. **Call arch_drift_scan** — per plan. Empty array → emit "no drift" line + skip Phases 3–5.
> 3. **Render drift report** — Markdown table: Stage | Drifted surfaces | Kind | Suggested resolution.
> 4. **AskUserQuestion polling per Stage** — ONE call per affected Stage. Stem + options use product/domain wording, NO tool/api jargon. Options fixed: `acknowledge` | `reword` | `re-plan`. Wait for answer; do NOT batch.
> 5. **Append change-log per resolution** — `master_plan_change_log_append({ slug, kind: "arch_drift_scan", actor: "arch-drift-scan", body: "Stage {stage_id} drift: {drifted_surfaces} | resolution: {x}" })`.
>
> ## Tool recipe
>
> 1. `mcp__territory-ia__master_plan_state` — plan title + stage list.
> 2. `mcp__territory-ia__arch_drift_scan` — affected stages + suggested questions.
> 3. `mcp__territory-ia__arch_decision_get` / `arch_decision_list` — decision details for richer stems (optional).
> 4. `mcp__territory-ia__arch_changelog_since` — narrative context (optional).
> 5. `mcp__territory-ia__master_plan_change_log_append` — one append per resolved Stage.
>
> ## Hard boundaries
>
> - Do NOT auto-rewrite plan markdown — change-log only.
> - Do NOT skip polling on any affected Stage.
> - Do NOT batch multiple Stages into one AskUserQuestion call.
> - Do NOT append change-log before AskUserQuestion returns.
> - Do NOT load whole plan markdown when `master_plan_state` slice suffices.
> - Do NOT commit — user decides.
>
> ## Output
>
> Single concise caveman message per scanned plan: plan slug + title; drift count; resolutions tally (acknowledge / reword / re-plan); change-log row count appended; next step (`claude-personal "/ship-plan --version-bump {SLUG}"` when any reword / re-plan resolved; else "no follow-up").
