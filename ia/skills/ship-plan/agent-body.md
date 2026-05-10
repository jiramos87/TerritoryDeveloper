# Mission

Run `ia/skills/ship-plan/SKILL.md` end-to-end for plan slug `{SLUG}`. DB-backed bulk plan-author skill that replaces `stage-file` + `stage-authoring`. Recipe `ship-plan-phase-a` owns mechanical preflight (parse + schema + shared MCP context + task_bundle batch + spec_sections batch + drift-lint stash); agent owns digest composition only; recipe `ship-plan-phase-c` owns post-bundle fan-out.

# Phase sequence

1. Phase A — `npm run recipe:run -- ship-plan-phase-a --input slug={SLUG}`. Recipe steps: parse handoff yaml → `validate-handoff-schema.mjs` → prefetch `router_for_task` + `glossary_discover` + `invariants_summary` + `list_rules` (parallel) → `task_bundle_batch` → `spec_sections` (batch anchor expansion for ALL anchors across ALL tasks; replaces per-anchor sequential calls) → `plan_digest_drift_lint` (NEW MCP) → `cron_drift_lint_findings_enqueue(status='staged')` (two-phase commit; flips to `queued` post bundle_apply success via SQL fn `promote_drift_lint_staged`). Outputs `{shared_context, task_batch, anchor_bundle, drift_findings_job_id}`.
2. Phase A.1 — Blueprint loader (when handoff yaml `task_kind=ui-from-db`): MCP `catalog_panel_get` + dependency closure resolve. Branch only fires when blueprint task type detected.
3. Phase B — Compose 3-section §Plan Digest per task (Opus-owned). Sections: §Goal + §Red-Stage Proof + §Work Items, ~30 lines each. Anchor refs already pre-expanded by Phase A step 5 — digest carries fenced embeds with provenance comment, zero `@anchor` literals.
4. Phase B.5 — Token-split guardrail: when input >180k → split into ⌈N/2⌉ sub-passes; bundle dispatch (Phase C) still runs ONCE.
5. Phase C — Single `mcp__territory-ia__master_plan_bundle_apply({ bundle })` Postgres tx. Bundle jsonb shape per mig 0136: `{plan, stages[] (with optional `red_stage_proof_block` 4-field jsonb — server renders body), tasks[] (with `digest_body`)}`. SQL fn renders `body` server-side from `red_stage_proof_block` when present; uses pre-rendered `body` when provided; defaults to `design_only` / `not_applicable` skip-clause when both absent. Inside tx: SQL fn calls `promote_drift_lint_staged(plan_slug, version)` flipping stash row staged→queued. Returns `{plan_id, version, drift_lint_promoted: true}`.
6. Phase D — `npm run recipe:run -- ship-plan-phase-c --input slug={SLUG}`. Async fan-out (fire-and-forget; failures = warning): `cron_glossary_backlinks_enqueue` + `cron_anchor_reindex_enqueue` + `cron_audit_log_enqueue(audit_kind=plan_filed)`.
7. Phase E — Hand-off: `/ship-cycle {SLUG} Stage {first_stage}`.

# Hard boundaries

- Do NOT call `task_insert` / `stage_insert` / `master_plan_insert` / `task_spec_section_write` per row — single `master_plan_bundle_apply` only.
- Do NOT regress to per-Task authoring on token overflow — split into sub-passes; bundle still dispatches once.
- Do NOT skip drift lint — Phase A recipe step 6 enqueues findings as `staged`; bundle_apply atomically promotes to `queued`; cron drainer writes to `ia_master_plan_change_log`.
- Do NOT call `lifecycle_stage_context` / `domain-context-load` per Task — Phase A recipe step 3 once per plan.
- Do NOT compose stage `body` in agent prompt — mig 0136 SQL fn renders server-side from `red_stage_proof_block` jsonb. Pass jsonb verbatim from handoff yaml.
- Do NOT call `spec_section` per anchor — Phase A recipe step 5 batches via `spec_sections` MCP.
- Do NOT write code, run verify, or flip Task status — handoff to `/ship-cycle`.
- Do NOT write task spec bodies to filesystem — bundle apply persists to DB only.
- Do NOT fall back to filesystem on DB unavailable — escalate; DB is source of truth.
- Do NOT edit `ia/specs/glossary.md` — propose candidates in handoff `notes:` field only.
- Do NOT touch `.claude/settings.json` `permissions.defaultMode` or `mcp__territory-ia__*` wildcard.
- Do NOT skip Scene Wiring row in §Work Items when triggered (new MonoBehaviour / `[SerializeField]` / scene prefab / `UnityEvent`) per `ia/rules/unity-scene-wiring.md`.

# Escalation shape

`{escalation: true, phase: "A"|"B"|"C"|"D", reason: "schema_validation_failed | anchor_resolution_failed | drift_lint_persistent_fail | bundle_apply_constraint_violation | ...", task_key?: "...", failing_field?: "...", stderr?: "..."}` — returned to dispatcher. See SKILL.md §Escalation rules for full trigger list.

# Output

Caveman summary: `ship-plan done. SLUG={S} VERSION={V} STAGES={n} TASKS={n} DRIFT_PROMOTED={true|false}` + per-stage red_stage_proof anchor + per-task §Plan Digest counts + DB writes + next=ship-cycle Stage 1.0. Escalation: JSON `{escalation:true,phase,reason,...}`.

# Changelog

- `cityscene-mainmenu-panel-rollout 2.0` — main-menu shipped with 7 visible defects (sibling Quit-confirm, inline back-button, branding `--`, full-width buttons, missing rounded body, lost blip sounds). §Plan Digest body never named `layout_template` / zone routing / design-spec line range. **Lesson:** when handoff yaml `notes:` references `docs/ui-element-definitions.md`, Phase B §Goal must cite the design line span (e.g. `lines 1188-1322`) + enumerate `child_kind`s + zones; §Red-Stage Proof must anchor a screenshot or `prefab_inspect`-diff test that fails until the spec lines render verbatim. Drift lint (Phase A step 6) should reject digests citing `ui-element-definitions.md` without an explicit line range or zone list.
