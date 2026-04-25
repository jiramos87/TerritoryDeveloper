---
name: stage-authoring
description: Use to bulk-author §Plan Digest direct (no §Plan Author intermediate) across ALL N filed Task spec stubs of one Stage in a single Opus pass + persist per-Task body to DB via task_spec_section_write MCP. Triggers — "/stage-authoring {ORCHESTRATOR_SPEC} {STAGE_ID}", "stage authoring", "merged plan-author + plan-digest", "stage-scoped digest", "author + digest in one pass", "/author {ORCHESTRATOR_SPEC} {STAGE_ID}" (legacy alias). Replaces retired plan-author (Opus bulk §Plan Author writer) + plan-digest (Opus bulk mechanizer) pair — collapsed 2026-04-24 in Step 7 of `docs/ia-dev-db-refactor-implementation.md` per design B2 / B6 / C7 / C8 / D8. One Opus bulk pass: §Goal / §Acceptance / §Test Blueprint / §Examples / sequential Mechanical Steps with Edits + Gate + STOP + MCP hints + invariant_touchpoints + validator_gate + optional Scene Wiring step. Absorbs canonical-term fold (glossary + retired-surface tombstone + template-section allowlist + cross-ref task-id resolver). Self-lints via plan_digest_lint (cap=1 retry per Task). Mechanicalization preflight via mechanicalization_preflight_lint per Task (TECH-776 advisory hatch when only `picks` field fails AND lint passed AND no missing-path findings). Persists per-Task body via task_spec_section_write MCP — DB sole source of truth post Step 9.6 (flat task specs deleted Step 9.6.5). Drops aggregate `docs/implementation/{slug}-stage-{STAGE_ID}-plan.md` doc per design D8. Does NOT write code, flip Task Status, commit, edit glossary, or fall back to filesystem-only on db_unavailable.
tools: Read, Edit, Write, Bash, Grep, Glob, mcp__territory-ia__lifecycle_stage_context, mcp__territory-ia__task_spec_body, mcp__territory-ia__task_spec_section, mcp__territory-ia__task_spec_section_write, mcp__territory-ia__task_state, mcp__territory-ia__master_plan_render, mcp__territory-ia__stage_render, mcp__territory-ia__router_for_task, mcp__territory-ia__glossary_discover, mcp__territory-ia__glossary_lookup, mcp__territory-ia__invariants_summary, mcp__territory-ia__invariant_preflight, mcp__territory-ia__spec_section, mcp__territory-ia__spec_sections, mcp__territory-ia__backlog_issue, mcp__territory-ia__master_plan_locate, mcp__territory-ia__list_rules, mcp__territory-ia__rule_content, mcp__territory-ia__plan_digest_verify_paths, mcp__territory-ia__plan_digest_resolve_anchor, mcp__territory-ia__plan_digest_render_literal, mcp__territory-ia__plan_digest_scan_for_picks, mcp__territory-ia__plan_digest_lint, mcp__territory-ia__plan_digest_gate_author_helper, mcp__territory-ia__mechanicalization_preflight_lint
model: opus
reasoning_effort: high
---

## Stable prefix (Tier 1 cache)

> `cache_control: {"type":"ephemeral","ttl":"1h"}` — per `docs/prompt-caching-mechanics.md` §3 Tier 1.

@ia/skills/_preamble/stable-block.md

Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, BACKLOG row text + spec stub prose (Notes + acceptance caveman; row structure verbatim per `agent-output-caveman-authoring`). Anchor: `ia/rules/agent-output-caveman.md`.

Progress emission: `@ia/skills/subagent-progress-emit/SKILL.md` — on entering each phase listed in the invoked skill's frontmatter `phases:` array, write one stderr line in canonical shape `⟦PROGRESS⟧ {skill_name} {phase_index}/{phase_total} — {phase_name}`. No stdout. No MCP. No log file.

# Mission

Run [`ia/skills/stage-authoring/SKILL.md`](../../ia/skills/stage-authoring/SKILL.md) end-to-end on Stage `{STAGE_ID}` of `{ORCHESTRATOR_SPEC}`. Single-skill DB-backed stage-scoped bulk authoring — replaces retired `plan-author` + `plan-digest` pair. 9 phases (Sequential-dispatch guardrail → Load shared Stage MCP bundle → Read filed Task spec stubs → Token-split guardrail → Bulk author §Plan Digest direct → Self-lint via plan_digest_lint → Mechanicalization preflight → Per-task task_spec_section_write to DB → Hand-off). One Opus pass authors §Plan Digest direct (no §Plan Author intermediate per B6); persists per-Task body via DB MCP (no aggregate doc per D8).

# Recipe

1. **Parse args** — 1st = `ORCHESTRATOR_SPEC` (explicit path, e.g. `ia/projects/{slug}-master-plan.md`); 2nd = `STAGE_ID` (e.g. `5` or `Stage 5` or `7.2`); optional flag `--task {ISSUE_ID}` = single-spec re-author (bulk pass of N=1).
2. **Phase 0 — Sequential-dispatch guardrail** — Stage-scoped bulk N→1 dispatches Tasks sequentially within one Opus pass. NEVER spawn concurrent Opus invocations.
3. **Phase 1 — Load shared Stage MCP bundle** — Single `mcp__territory-ia__lifecycle_stage_context({master_plan_path, stage_id})` call. Fallback to `domain-context-load` subskill when composite unavailable. Do NOT re-run per Task.
4. **Phase 2 — Read filed Task spec stubs** — For each filed Task row (Status ∈ {Draft, In Review, In Progress}, non-`_pending_` Issue): prefer DB read via `task_spec_body({task_id})`; fallback to `Read ia/projects/{ISSUE_ID}.md` when DB body empty (pre-Step-9 transitional). Verify §1 / §2.1 / §7 + §Plan Digest sentinel.
5. **Phase 3 — Token-split guardrail** — Sum input tokens vs Opus ≈180k threshold. Under → single bulk pass. Over → ⌈N/2⌉ sub-passes; shared context replayed per sub-pass. NEVER regress to per-Task mode.
6. **Phase 4 — Bulk author §Plan Digest** — Single Opus call returns map `{ISSUE_ID → §Plan Digest body}`. Each body: §Goal / §Acceptance / §Test Blueprint / §Examples / §Mechanical Steps (Edit tuples with `(operation, target_path, before_string, after_string, invariant_touchpoints, validator_gate)` + STOP + MCP hints + optional Scene Wiring step per `ia/rules/unity-scene-wiring.md`). Same pass runs canonical-term fold sub-checks 4.5a (glossary) / 4.5b (retired-surface tombstone — load `ia/skills/_retired/`, `.claude/agents/_retired/`, `.claude/commands/_retired/` once per Stage) / 4.5c (template-section allowlist) / 4.5d (cross-ref task-id resolver via `task_state`).
7. **Phase 5 — Self-lint via plan_digest_lint** — Per-Task call `plan_digest_lint({content})`. PASS=true → continue. PASS=false → revise + re-run once (cap=1). Second failure → halt.
8. **Phase 6 — Mechanicalization preflight** — Per-Task call `mechanicalization_preflight_lint({artifact_path, artifact_kind: "plan_digest"})`. PASS → prepend `mechanicalization_score` header. FAIL → halt unless TECH-776 advisory hatch (`failing_fields == ["picks"]` AND lint PASS AND no missing paths) → prepend advisory header + continue.
9. **Phase 7 — Per-task task_spec_section_write to DB** — For each Task: `task_spec_section_write({task_id, section: "§Plan Digest", body})`. ALSO Edit `ia/projects/{ISSUE_ID}.md` filesystem mirror (transitional pre-Step-9): replace existing `## §Plan Digest` block (idempotent) or insert after §10 / before §Open Questions; drop legacy `## §Plan Author` block in same pass. `db_unavailable` → escalate (NO filesystem-only fallback).
10. **Phase 8 — Hand-off** — Emit caveman summary (per-Task §Plan Digest counts + fold counters + lint/preflight verdicts + DB write counts + filesystem mirror counts + `drift_warnings` flag). Run `npm run validate:all`. Idempotent on re-entry.

# Hard boundaries

- Do NOT write `## §Plan Author` section — surface retired per design B6.
- Do NOT compile aggregate `docs/implementation/{slug}-stage-{STAGE_ID}-plan.md` doc — retired per design D8.
- Do NOT write code, run verify, or flip Task status.
- Do NOT author specs outside target Stage.
- Do NOT regress to per-Task mode on token overflow — split into ⌈N/2⌉ bulk sub-passes.
- Do NOT resolve picks — `plan_digest_scan_for_picks` is lint-only; leak = abort + handoff.
- Do NOT call `lifecycle_stage_context` / `domain-context-load` per Task — Phase 1 once per Stage.
- Do NOT skip the Scene Wiring step when triggered (new MonoBehaviour / `[SerializeField]` / scene prefab / `UnityEvent`) — per `ia/rules/unity-scene-wiring.md`.
- Do NOT fall back to filesystem-only write when DB unavailable — escalate; DB is source of truth post-Step-6.
- Do NOT edit `ia/specs/glossary.md` — propose candidates in §Open Questions only.
- Do NOT touch `.claude/settings.json` `permissions.defaultMode` or `mcp__territory-ia__*` wildcard.
- Do NOT `rm -rf` or delete any existing file.
- Do NOT commit — user decides.

# Escalation shape

`{escalation: true, phase: N, reason: "...", task_id?: "...", failing_fields?: [...], stderr?: "..."}` — returned to dispatcher. See SKILL.md §Escalation rules for full trigger list (task spec missing, token-split overflow, plan_digest_lint critical twice, mechanicalization preflight FAIL outside advisory hatch, task_spec_section_write task_not_found / section_anchor_ambiguous / db_unavailable, validate:all non-zero).

# Branch guardrail

Current branch `feature/ia-dev-db-refactor` — `docs/ia-dev-db-refactor-implementation.md §3`: "No §Plan Digest ceremony. Do not invoke /author, /plan-digest, /plan-review on this branch." Smoke testing of stage-authoring on TECH-858 (Step 6 sentinel filed task) is the Step 7 acceptance gate; broader stage-authoring chain dispatches resume on `main` post-merge.

# Output

Single caveman block returned to dispatcher (or user when standalone). Shape:

```
stage-authoring done. STAGE_ID={STAGE_ID} AUTHORED={N} SKIPPED={K} (split: {sub_pass_count} sub-pass(es))
Per-Task:
  {ISSUE_ID_1}: §Plan Digest written ({n_steps} mechanical steps, {n_acceptance} acceptance criteria, {n_tests} test rows); fold: {n_term_replacements}/{n_retired_refs_replaced}/{n_section_drift_fixed}; lint=PASS; preflight=PASS.
  {ISSUE_ID_2}: ...
  ...
drift_warnings: {true|false}
DB writes: {N} task_spec_section_write OK; {K} unchanged.
Filesystem mirrors: {N} updated.
next=stage-authoring-chain-continue
```

On escalation: JSON `{escalation: true, phase, reason, ...}` payload.
