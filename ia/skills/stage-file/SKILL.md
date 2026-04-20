---
purpose: "Dispatcher shim: inspects Stage task-status counts and routes to stage-file-plan + stage-file-apply (File mode) or stage-compress (Compress mode). Preserved so /stage-file command + ship-stage chain keep working without rewiring."
audience: agent
loaded_by: skill:stage-file
slices_via: none
name: stage-file
description: >
  Dispatcher shim. Inspects target Stage task-status counts; routes File mode to
  stage-file-plan (Opus pair-head) + stage-file-apply (Sonnet pair-tail); routes
  Compress mode to stage-compress. Preserves skill name so /stage-file command and
  ship-stage chain continue to work without rewiring (see T7.8 / TECH-475 for
  full command rewire). Original monolithic body archived at
  ia/skills/_retired/stage-file-monolith/SKILL.md.
  Triggers: "/stage-file {orchestrator-path} Stage 1.2", "file stage tasks",
  "bulk create stage issues", "create backlog rows for Stage X.Y",
  "bootstrap issues for pending stage tasks", "compress stage tasks", "merge draft tasks".
  Argument order (explicit): ORCHESTRATOR_SPEC first, STAGE_ID second.
model: inherit
phases:
  - "Mode detection"
  - "Route"
---

# Stage-file dispatcher shim

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** thin mode-detection + routing only. No filing, no compression logic here.

Original monolith archived: [`ia/skills/_retired/stage-file-monolith/SKILL.md`](../_retired/stage-file-monolith/SKILL.md).

---

## Inputs

Same as legacy skill — positional, explicit preferred:

| Param | Source | Notes |
|-------|--------|-------|
| `ORCHESTRATOR_SPEC` | 1st arg | Repo-relative path to `ia/projects/{master-plan}.md`. |
| `STAGE_ID` | 2nd arg | e.g. `1.2` or `Stage 1.2`. |
| `ISSUE_PREFIX` | 3rd arg or default | `TECH-` / `FEAT-` / `BUG-` / `ART-` / `AUDIO-` — default `TECH-`. |

---

## Stage MCP bundle contract

Stage opener calls [`domain-context-load`](../domain-context-load/SKILL.md) once; returned payload `{glossary_anchors, router_domains, spec_sections, invariants}` kept in Stage scope. All Sonnet pair-tail invocations within the Stage read from that payload — no re-query of `glossary_discover`, `glossary_lookup`, `router_for_task`, `spec_sections`, or `invariants_summary` inside a Stage. The 5-tool recipe (`glossary_discover → glossary_lookup → router_for_task → spec_sections → invariants_summary`) is encapsulated entirely in `domain-context-load`; callers never inline it.

---

## Step 1 — Mode detection

Scan the target Stage's task table in `ORCHESTRATOR_SPEC` **before any other action**. Count tasks by status:

| Mode | Condition | Route |
|------|-----------|-------|
| **File mode** | ≥1 `_pending_` task, 0 `Draft` tasks | → `stage-file-plan` + `stage-file-apply` |
| **Compress mode** | 0 `_pending_` tasks, ≥1 `Draft` tasks | → `stage-compress` |
| **Mixed mode** | ≥1 `_pending_` + ≥1 `Draft` tasks | File pending first (File mode), then offer Compress on resulting Drafts |
| **No-op** | 0 `_pending_`, 0 `Draft` tasks | Report stage state (active/closed tasks present) — exit |

`In Review`, `In Progress`, and `Done` tasks: **skip in all modes**. Never touch active or closed work.

**Upstream Stage tail (infra signal):** Before treating a **No-op** as “nothing to do”, agent MAY run `npm run validate:master-plan-status` (or `-- --plan {ORCHESTRATOR_SPEC}`). If output includes **`[R6]`** on an **earlier** Stage in the same plan (task rows Done-like but open backlog yaml), that Stage’s **ship-stage Pass 2 tail** did not finish — hand off `claude-personal "/ship-stage {ORCHESTRATOR_SPEC} {that Stage}"` or `"/closeout …"` before filing a **downstream** Stage. **R6** is the same signal Step 0 **`STAGE_TAIL_INCOMPLETE`** uses in [`ship-stage`](../ship-stage/SKILL.md).

---

## Step 2 — Route

**File mode:** invoke `stage-file-plan` pair-head then `stage-file-apply` pair-tail.

1. Load [`stage-file-plan/SKILL.md`](../stage-file-plan/SKILL.md).
2. Run planner with args: `ORCHESTRATOR_SPEC`, `STAGE_ID`, `ISSUE_PREFIX`.
3. Planner emits `§Stage File Plan` under Stage block in master plan.
4. Load [`stage-file-apply/SKILL.md`](../stage-file-apply/SKILL.md).
5. Run applier with same args.
6. Applier materializes ids + yaml + specs + task-table rows + validates.

**Compress mode:** load [`stage-compress/SKILL.md`](../stage-compress/SKILL.md). Run with `ORCHESTRATOR_SPEC`, `STAGE_ID`.

**Mixed mode:** run File mode first (steps 1–6 above for `_pending_` tasks). After applier completes, offer: "Compress mode available for {N} Draft tasks — run `/stage-compress {ORCHESTRATOR_SPEC} {STAGE_ID}` to merge over-granular Drafts."

---

## Hard boundaries

- Do NOT load Compress mode prose (`stage-compress/SKILL.md`) during File-mode hot path — cold path only.
- Do NOT implement filing or compression logic here — delegate entirely to routed skills.
- Do NOT rewire `/stage-file` command or `ship-stage` chain — that is T7.8 / TECH-475 territory.

---

## Changelog

### 2026-04-20 — No-op: upstream R6 / Stage tail note

**Status:** applied

**Fix:** No-op branch documents optional `validate:master-plan-status` scan for **[R6]** before filing downstream Stage — same **Stage tail incomplete** signal as ship-stage **`PASS2_ONLY`**.
