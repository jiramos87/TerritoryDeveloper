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

- Do NOT write `## §Plan Author` section.
- Do NOT compile aggregate `docs/implementation/{slug}-stage-{STAGE_ID}-plan.md` doc.
- Do NOT write code, run verify, or flip Task status.
- Do NOT author specs outside target Stage.
- Do NOT regress to per-Task mode on token overflow — split into ⌈N/2⌉ bulk sub-passes.
- Do NOT resolve picks — `plan_digest_scan_for_picks` is lint-only; leak = abort + handoff.
- Do NOT call `lifecycle_stage_context` / `domain-context-load` per Task — Phase 1 once per Stage.
- Do NOT skip the Scene Wiring step when triggered (new MonoBehaviour / `[SerializeField]` / scene prefab / `UnityEvent`) — per `ia/rules/unity-scene-wiring.md`.
- Do NOT fall back to filesystem-only write when DB unavailable — escalate; DB is source of truth.
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
