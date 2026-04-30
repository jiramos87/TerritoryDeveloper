# Mission

Run `ia/skills/stage-authoring/SKILL.md` end-to-end for Stage `{STAGE_ID}` of `{SLUG}`. DB-backed bulk §Plan Digest authoring via recipe engine. Recipe YAML: `tools/recipes/stage-authoring.yaml`. 7 phases: sequential-dispatch guardrail → lifecycle_stage_context → task_spec_body reads → token-split → Opus bulk author (rubric-in-prompt) → task_spec_section_write per task → validate:master-plan-status.

# Recipe

Run via recipe engine. YAML: `tools/recipes/stage-authoring.yaml`. CLI: `npm run recipe:run -- stage-authoring -- slug {SLUG} stage_id {STAGE_ID}`.

# Hard boundaries

- Do NOT write `## §Plan Author` section.
- Do NOT compile aggregate `docs/implementation/{slug}-stage-{STAGE_ID}-plan.md` doc.
- Do NOT write code, run verify, or flip Task status.
- Do NOT author specs outside target Stage.
- Do NOT regress to per-Task mode on token overflow — split into ⌈N/2⌉ bulk sub-passes.
- Do NOT call `plan_digest_lint` MCP — rubric is enforced in-prompt only; no post-author lint or retry loop.
- Do NOT call `lifecycle_stage_context` / `domain-context-load` per Task — Phase 1 once per Stage.
- Do NOT skip the Scene Wiring step when triggered (new MonoBehaviour / `[SerializeField]` / scene prefab / `UnityEvent`) — per `ia/rules/unity-scene-wiring.md`.
- Do NOT write task spec bodies to filesystem — DB only via `task_spec_section_write`.
- Do NOT fall back to filesystem-only write when DB unavailable — escalate; DB is source of truth.
- Do NOT edit `ia/specs/glossary.md` — propose candidates in §Open Questions only.
- Do NOT touch `.claude/settings.json` `permissions.defaultMode` or `mcp__territory-ia__*` wildcard.

# Escalation shape

`{escalation: true, phase: N, reason: "...", task_id?: "...", failing_fields?: [...], stderr?: "..."}` — returned to dispatcher. See SKILL.md §Escalation rules for full trigger list.

# Output

Caveman summary: `stage-authoring done STAGE_ID={S} AUTHORED={N} SKIPPED={K}` + per-task §Plan Digest counts + fold + section_overrun + DB writes + drift_warnings. Full shape: see SKILL.md §Phase 6 Hand-off. Escalation: JSON `{escalation:true,phase,reason,...}`.
