### Stage 12 — Skill patches + plan consumers / Seed skills (`project-new`, `stage-file`)

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Teach `project-new` + `stage-file` to write full schema-v2 yaml at seed time. Both skills already reserve ids + write yaml; now they populate `parent_plan` + `task_key` + optional locator fields (`step`, `stage`, `phase`, `router_domain`, `surfaces`, `mcp_slices`, `skill_hints`) from plan task-row context. `stage-file` also runs `parent_plan_validate` on the freshly-seeded records (advisory).

**Exit:**

- `ia/skills/stage-file/SKILL.md` — seed step documents full v2 field population. `surfaces` pulled from the plan's `**Relevant surfaces (load when step opens):**` block + the task row's Intent column path refs. `mcp_slices` + `skill_hints` pulled from plan notes when present.
- `ia/skills/project-new/SKILL.md` — single-issue path requires `parent_plan` + `task_key` inputs; skill documents the fallback when neither plan nor task_key known (single-issue outside-plan path → both fields empty; validator advisory ignores).
- Both skills reference `backlog_record_validate` pre-write + `parent_plan_validate` post-write (advisory).
- Phase 1 — `stage-file` body patches.
- Phase 2 — `project-new` body patches.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T12.1 | Patch `stage-file` body — full v2 seed | _pending_ | _pending_ | Edit `ia/skills/stage-file/SKILL.md` seed-yaml step — document v2 field population: `parent_plan` = orchestrator path arg; `task_key` from task row id column; `step` / `stage` / `phase` derived from `task_key` parser; `router_domain` from MCP `router_for_task` first match; `surfaces` from plan's Relevant-surfaces block + task Intent path refs; `mcp_slices` + `skill_hints` from plan notes when present. Caveman prose. |
| T12.2 | Wire `parent_plan_validate` advisory into stage-file | _pending_ | _pending_ | Add to `ia/skills/stage-file/SKILL.md` a post-write step: call `parent_plan_validate` (MCP) in advisory mode after all yaml writes + before `materialize-backlog.sh`. Warn on drift count; do NOT block (strict flip lives in Step 6). |
| T12.3 | Patch `project-new` body — single-issue v2 seed | _pending_ | _pending_ | Edit `ia/skills/project-new/SKILL.md` — require `parent_plan` + `task_key` inputs when caller passes plan context; allow both empty for single-issue outside-plan flows. Document derivation rules + fallback. Bash fallback kept for MCP-unavailable case. |
| T12.4 | Update `project-new` input interview | _pending_ | _pending_ | Edit `ia/skills/project-new/SKILL.md` interview step — add `parent_plan?` + `task_key?` to the structured-input block; skill prompts when missing + plan context detected via `--plan` arg. Document in slash-command dispatcher (`.claude/commands/project-new.md`) if input schema exposed there. |
