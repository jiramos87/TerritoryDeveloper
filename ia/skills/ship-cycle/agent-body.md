# Mission

Run `ia/skills/ship-cycle/SKILL.md` end-to-end on Stage `{STAGE_ID}` of `{SLUG}`. Stage-atomic batch ship-cycle: one Sonnet 4.6 inference emits ALL tasks of one Stage with boundary markers `<!-- TASK:{ISSUE_ID} START/END -->`. Per-task `unity:compile-check` gate + `task_status_flip(implemented)` after batch. Pass B (verify-loop + closeout + commit) handed to `/ship-stage` resume gate. Token budget hard cap 80k input — over cap = fallback ship-stage two-pass.

# Phase sequence (matches SKILL frontmatter `phases:`)

1. Phase 0 — Parse `{SLUG} {STAGE_ID}`; `stage_bundle(slug, stage_id)`; idle exit if stage done.
2. Phase 1 — Token-budget preflight: sum bundle + per-task §Plan Digest bytes; over 80k → STOPPED + fallback handoff.
3. Phase 2 — Single inference body emits all tasks with boundary markers.
4. Phase 3 — Per task: `unity:compile-check` (when Assets/**/*.cs touched) → `task_status_flip(implemented)` → `journal_append`.
5. Phase 4 — Hand off `Next: /ship-stage {SLUG} Stage {STAGE_ID}` for Pass B (verify-loop + closeout + single stage commit).

# Boundary marker contract

```
<!-- TASK:TECH-XXXXX START -->
... implementation body (code edits, file creations) ...
<!-- TASK:TECH-XXXXX END -->
```

HTML comments — invisible in rendered markdown, greppable by code-review / validators. Order = `tasks[]` order. Unbalanced markers → `STOPPED at {ISSUE_ID} — boundary_marker_unbalanced`.

# Hard boundaries

- Do NOT bypass token-budget preflight — over cap → fallback ship-stage two-pass.
- Do NOT commit per task — Pass B owns single stage commit.
- Do NOT skip `unity:compile-check` per task on Assets/**/*.cs touched.
- Do NOT cross stage boundary — strictly one Stage per invocation.
- Do NOT flip status outside `pending → implemented` in Pass-A-equivalent.
- Do NOT run `verify-loop` here — handed to ship-stage Pass B.
- Do NOT write task spec bodies to filesystem — DB sole source of truth.

# Escalation shape

```json
{"escalation": true, "phase": <int>, "reason": "token_budget_exceeded | boundary_marker_unbalanced | compile_check_failed | task_status_flip_failed", "task_id": "<opt>", "stderr": "<opt>"}
```

# Output

Caveman summary: `ship-cycle done. STAGE_ID={S} BATCH_SIZE={N} IMPLEMENTED={K} SKIPPED={M}` + per-task rows + token usage + `Next:` handoff.
