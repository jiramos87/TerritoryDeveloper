# Mission

Run `ia/skills/ship-plan/SKILL.md` end-to-end for plan slug `{SLUG}`. DB-backed bulk plan-author skill that replaces `stage-file` + `stage-authoring`. Read lean handoff YAML frontmatter from `docs/explorations/{SLUG}.md`, pre-fetch glossary + router + invariants once, compose 3-section §Plan Digest per task with inline anchor expansion, run drift lint, dispatch one `master_plan_bundle_apply` atomic Postgres tx.

# Phase sequence

1. Phase 1 — Parse handoff YAML frontmatter from `docs/explorations/{SLUG}.md`.
2. Phase 2 — Validate via `node tools/scripts/validate-handoff-schema.mjs docs/explorations/{SLUG}.md`.
3. Phase 3 — Pre-fetch shared MCP context once: `router_for_task` + `glossary_discover` + `invariants_summary` + `list_rules`. Cache as Tier 2 ephemeral block.
4. Phase 4 — Pre-load `task_bundle_batch` for all task_keys; cache as `TASK_BATCH`.
5. Phase 5 — Compose 3-section §Plan Digest per task (§Goal + §Red-Stage Proof + §Work Items, ~30 lines). Token-split into ⌈N/2⌉ sub-passes when >180k input tokens; bundle dispatch still runs once.
5.1. Phase 5.1 — Inline anchor expansion (per §Goal + §Work Items composition):
     - Detect `@{kind}:{slug}#{section}` refs (e.g. `@spec:architecture/layers#L1-L3`) + canonical glossary terms (case-sensitive against `glossary.md`).
     - Per unique ref: hit `ANCHOR_CACHE` (per-plan `Map<{kind}:{slug}#{section}, body>`); on miss call `spec_section({slug, section})` once + insert.
     - Replace ref token with fenced embed `<!-- @{kind}:{slug}#{section} -->\n```\n{body}\n```` (provenance comment + body block). Resulting digest carries zero `@anchor` literals.
     - Unresolvable ref → leave token literal + push to `DRIFT_WARNINGS[]` so Phase 6 lint catches it.
6. Phase 6 — Drift lint per task: anchor resolution + glossary alignment + retired-surface scan. 2-retry budget per failure mode; halt with structured escalation on persistent failure.
7. Phase 7 — Dispatch single `mcp__territory-ia__master_plan_bundle_apply({ bundle })` Postgres tx. Bundle shape: `{plan, stages[], tasks[]}` with `digest_body` per task. Constraint violation → re-author offending field; second failure escalates.
7.5. Phase 7.5 — Post-bundle async enqueues (fire-and-forget; failure = warning, not halt):
     - `mcp__territory-ia__cron_glossary_backlinks_enqueue({ slug, plan_id })` — cron drains within 5 min; upserts `ia_glossary_backlinks`.
     - `mcp__territory-ia__cron_anchor_reindex_enqueue({ paths: ["ia/specs/glossary.md"] })` — cron drains within 5 min; upserts `ia_spec_anchors`.
8. Phase 8 — Hand-off summary + next-step handoff (`/ship-cycle {SLUG} Stage {first_stage}`).

# Hard boundaries

- Do NOT call `task_insert` / `stage_insert` / `master_plan_insert` / `task_spec_section_write` per row — single `master_plan_bundle_apply` only.
- Do NOT regress to per-Task authoring on token overflow — split into sub-passes; bundle still dispatches once.
- Do NOT skip drift lint — anchor + glossary + retired-surface lint runs synchronously before bundle dispatch.
- Do NOT call `lifecycle_stage_context` / `domain-context-load` per Task — Phase 3 once per plan.
- Do NOT write code, run verify, or flip Task status — handoff to `/ship-cycle` (or legacy `/ship-stage`).
- Do NOT write task spec bodies to filesystem — bundle apply persists to DB only.
- Do NOT fall back to filesystem on DB unavailable — escalate; DB is source of truth.
- Do NOT edit `ia/specs/glossary.md` — propose candidates in handoff `notes:` field only.
- Do NOT touch `.claude/settings.json` `permissions.defaultMode` or `mcp__territory-ia__*` wildcard.
- Do NOT skip the Scene Wiring step when triggered (new MonoBehaviour / `[SerializeField]` / scene prefab / `UnityEvent`) — emit Scene Wiring row in §Work Items per `ia/rules/unity-scene-wiring.md`.

# Escalation shape

`{escalation: true, phase: N, reason: "...", task_key?: "...", failing_field?: "...", stderr?: "..."}` — returned to dispatcher. See SKILL.md §Escalation rules for full trigger list.

# Output

Caveman summary: `ship-plan done. SLUG={S} VERSION={V} STAGES={n} TASKS={n}` + per-stage red_stage_proof anchor + per-task §Plan Digest counts + drift_warnings + DB writes + next=ship-cycle Stage 1.0. Full shape: see SKILL.md §Phase 8 Hand-off. Escalation: JSON `{escalation:true,phase,reason,...}`.
