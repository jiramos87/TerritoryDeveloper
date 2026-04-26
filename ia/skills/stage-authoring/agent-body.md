# Mission

Run [`ia/skills/stage-authoring/SKILL.md`](../../ia/skills/stage-authoring/SKILL.md) end-to-end on Stage `{STAGE_ID}` of slug `{SLUG}`. Single-skill DB-backed stage-scoped bulk authoring. 9 phases (Sequential-dispatch guardrail → Load shared Stage MCP bundle → Read filed Task spec stubs → Token-split guardrail → Bulk author §Plan Digest direct → Self-lint via plan_digest_lint → Mechanicalization preflight → Per-task task_spec_section_write to DB → Hand-off). One Opus pass authors §Plan Digest direct; persists per-Task body via DB MCP (no aggregate doc).

# Recipe

1. **Parse args** — 1st = `SLUG` (bare master-plan slug, e.g. `blip`); 2nd = `STAGE_ID` (e.g. `5` or `Stage 5` or `7.2`); optional flag `--task {ISSUE_ID}` = single-spec re-author (bulk pass of N=1).
2. **Phase 0 — Sequential-dispatch guardrail** — Stage-scoped bulk N→1 dispatches Tasks sequentially within one Opus pass. NEVER spawn concurrent Opus invocations.
3. **Phase 1 — Load shared Stage MCP bundle** — Single `mcp__territory-ia__lifecycle_stage_context({slug, stage_id})` call. Fallback to `domain-context-load` subskill when composite unavailable. Do NOT re-run per Task.
4. **Phase 2 — Read filed Task spec stubs** — For each filed Task row (Status ∈ {Draft, In Review, In Progress}, non-`_pending_` Issue): read body via `task_spec_body({task_id})`. DB is sole source of truth. Verify §1 / §2.1 / §7 + §Plan Digest sentinel.
5. **Phase 3 — Token-split guardrail** — Sum input tokens vs Opus ≈180k threshold. Under → single bulk pass. Over → ⌈N/2⌉ sub-passes; shared context replayed per sub-pass. NEVER regress to per-Task mode.
6. **Phase 4 — Bulk author §Plan Digest (relaxed shape, direct)** — Single Opus call returns map `{ISSUE_ID → §Plan Digest body}`. Each body in RELAXED shape: §Goal / §Acceptance / §Pending Decisions / §Implementer Latitude / §Work Items / §Test Blueprint / §Invariants & Gate. §Work Items = flat list of `{repo-relative-path}: {1-line intent}` rows — NO verbatim before/after code. §Invariants & Gate = ONE block per digest carrying `invariant_touchpoints`, `validator_gate`, `escalation_enum`, **Gate:** + **STOP:**. Scene Wiring (when triggered per `ia/rules/unity-scene-wiring.md`) appears as a single §Work Items row prefixed `(Scene Wiring)`. Same pass runs canonical-term fold sub-check 4.5a (glossary alignment in digest body only) + 4.5b (retired-surface tombstone scan in digest body only — load `ia/skills/_retired/`, `.claude/agents/_retired/`, `.claude/commands/_retired/` once per Stage). 4.5c (template-section allowlist) + 4.5d (cross-ref task-id resolver) + 4.5e (stage-level summary) DROPPED — DB writes its own template; cross-refs resolve at implement time.
7. **Phase 5 — Self-lint via plan_digest_lint** — Per-Task call `plan_digest_lint({content})`. PASS=true → continue. PASS=false → revise + re-run once (cap=1). Second failure → halt.
8. **Phase 6 — Mechanicalization preflight** — Per-Task call `mechanicalization_preflight_lint({artifact_path: "db:{ISSUE_ID}", artifact_kind: "plan_digest"})`. PASS → prepend `mechanicalization_score` header. FAIL → halt unless advisory hatch (`failing_fields == ["picks"]` AND lint PASS AND no missing paths) → prepend advisory header + continue.
9. **Phase 7 — Per-task task_spec_section_write to DB** — For each Task: `task_spec_section_write({task_id, section: "§Plan Digest", body})`. DB sole persistence — no filesystem mirror. `db_unavailable` → escalate.
10. **Phase 8 — Hand-off** — Emit caveman summary (per-Task §Plan Digest counts + fold counters + lint/preflight verdicts + DB write counts + `drift_warnings` flag). Run `npm run validate:all`. Idempotent on re-entry.

# Hard boundaries

- Do NOT write `## §Plan Author` section.
- Do NOT compile aggregate `docs/implementation/{slug}-stage-{STAGE_ID}-plan.md` doc.
- Do NOT write code, run verify, or flip Task status.
- Do NOT author specs outside target Stage.
- Do NOT regress to per-Task mode on token overflow — split into ⌈N/2⌉ bulk sub-passes.
- Do NOT resolve picks — `plan_digest_scan_for_picks` is lint-only; leak = abort + handoff.
- Do NOT call `lifecycle_stage_context` / `domain-context-load` per Task — Phase 1 once per Stage.
- Do NOT skip the Scene Wiring step when triggered (new MonoBehaviour / `[SerializeField]` / scene prefab / `UnityEvent`) — per `ia/rules/unity-scene-wiring.md`.
- Do NOT write task spec bodies to filesystem — DB only via `task_spec_section_write`.
- Do NOT fall back to filesystem-only write when DB unavailable — escalate; DB is source of truth.
- Do NOT edit `ia/specs/glossary.md` — propose candidates in §Open Questions only.
- Do NOT touch `.claude/settings.json` `permissions.defaultMode` or `mcp__territory-ia__*` wildcard.
- Do NOT `rm -rf` or delete any existing file.
- Do NOT commit — user decides.

# Escalation shape

`{escalation: true, phase: N, reason: "...", task_id?: "...", failing_fields?: [...], stderr?: "..."}` — returned to dispatcher. See SKILL.md §Escalation rules for full trigger list (task spec missing, token-split overflow, plan_digest_lint critical twice, mechanicalization preflight FAIL outside advisory hatch, task_spec_section_write task_not_found / section_anchor_ambiguous / db_unavailable, validate:all non-zero).

# Output

Single caveman block returned to dispatcher (or user when standalone). Shape:

```
stage-authoring done. STAGE_ID={STAGE_ID} AUTHORED={N} SKIPPED={K} (split: {sub_pass_count} sub-pass(es))
Per-Task:
  {ISSUE_ID_1}: §Plan Digest written ({n_work_items} work items, {n_decisions} pending decisions, {n_latitude} latitude rows, {n_acceptance} acceptance, {n_tests} test rows); fold: {n_term_replacements}/{n_retired_refs_replaced}; lint=PASS; preflight=PASS.
  {ISSUE_ID_2}: ...
  ...
drift_warnings: {true|false}
DB writes: {N} task_spec_section_write OK; {K} unchanged.
next=stage-authoring-chain-continue
```

On escalation: JSON `{escalation: true, phase, reason, ...}` payload.
