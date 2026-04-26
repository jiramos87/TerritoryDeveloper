---
name: stage-authoring
purpose: >-
  DB-backed single-skill stage-authoring: one Opus bulk pass writes §Plan Digest direct per task via
  task_spec_section_write MCP. No aggregate doc.
audience: agent
loaded_by: "skill:stage-authoring"
slices_via: none
description: >-
  DB-backed single-skill stage-authoring. One Opus bulk pass authors §Plan Digest direct per filed
  Task spec stub of one Stage (RELAXED shape: §Goal / §Acceptance / §Pending Decisions /
  §Implementer Latitude / §Work Items / §Test Blueprint / §Invariants & Gate — intent over
  verbatim code). Stub → digest direct, no intermediate surface. Persists each per-Task §Plan
  Digest body to DB via `task_spec_section_write` MCP. Glossary alignment + retired-surface scan
  narrowed to §Plan Digest body only. Self-lints via `plan_digest_lint` (cap=1 retry). No
  aggregate doc compile.
  Triggers: "/stage-authoring {SLUG} {STAGE_ID}", "stage authoring", "stage-scoped digest",
  "author stage tasks". Argument order (explicit): SLUG first, STAGE_ID second.
phases:
  - Sequential-dispatch guardrail
  - Load shared Stage MCP bundle
  - Read filed Task spec stubs
  - Token-split guardrail
  - Bulk author §Plan Digest (relaxed shape, direct)
  - Self-lint via plan_digest_lint
  - Per-task task_spec_section_write to DB
  - Hand-off
triggers:
  - /stage-authoring {SLUG} {STAGE_ID}
  - stage authoring
  - stage-scoped digest
  - author stage tasks
argument_hint: {slug} Stage {X.Y} [--task {ISSUE_ID}] [--force-model {model}]
model: opus
reasoning_effort: high
tools_role: pair-head
tools_extra:
  - mcp__territory-ia__lifecycle_stage_context
  - mcp__territory-ia__task_spec_body
  - mcp__territory-ia__task_spec_section
  - mcp__territory-ia__task_spec_section_write
  - mcp__territory-ia__task_state
  - mcp__territory-ia__master_plan_render
  - mcp__territory-ia__stage_render
  - mcp__territory-ia__invariant_preflight
  - mcp__territory-ia__plan_digest_verify_paths
  - mcp__territory-ia__plan_digest_scan_for_picks
  - mcp__territory-ia__plan_digest_lint
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
  - BACKLOG row text + spec stub prose (Notes + acceptance caveman; row structure verbatim per agent-output-caveman-authoring)
hard_boundaries:
  - "Do NOT write `## §Plan Author` section."
  - Do NOT compile aggregate `docs/implementation/{slug}-stage-{STAGE_ID}-plan.md` doc.
  - Do NOT write code, run verify, or flip Task status.
  - Do NOT author specs outside target Stage.
  - Do NOT regress to per-Task mode on token overflow — split into ⌈N/2⌉ bulk sub-passes.
  - Do NOT resolve picks — `plan_digest_scan_for_picks` is lint-only; leak = abort + handoff.
  - Do NOT call `lifecycle_stage_context` / `domain-context-load` per Task — Phase 1 once per Stage.
  - Do NOT skip the Scene Wiring step when triggered (new MonoBehaviour / `[SerializeField]` / scene prefab / `UnityEvent`) — per `ia/rules/unity-scene-wiring.md`.
caller_agent: stage-authoring
---

# Stage-authoring skill — DB-backed single-skill

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** Single-skill stage-scoped spec-body authoring. Reads filed Task spec stubs of one Stage; writes §Plan Digest **direct** in one Opus bulk pass; persists per-Task body to DB via `task_spec_section_write` MCP. No aggregate doc compile.

**Contract:** [`ia/rules/plan-digest-contract.md`](../../rules/plan-digest-contract.md) — 9-point rubric, enforced by `plan_digest_lint`.

**Upstream:** `stage-file` (writes N filed spec stubs + DB rows). **Downstream:** `/ship-stage` (N≥2) or `/ship` (N=1). `stage-file` dispatcher calls this skill inline.

---

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `SLUG` | 1st arg | Bare master-plan slug (e.g. `blip`). DB-first via `master_plan_render` / `stage_render`. |
| `STAGE_ID` | 2nd arg | e.g. `5` or `Stage 5` or `7.2`. |
| `--task {ISSUE_ID}` | optional | Single-spec re-author escape hatch (bulk pass of N=1). |

---

## Phase 0 — Sequential-dispatch guardrail

> **Guardrail:** Stage-scoped bulk N→1 dispatches Tasks sequentially within one Opus pass. Never spawn concurrent Opus invocations. One Task authored → next Task authored — no parallel fan-out.

Applies even when sub-passes are active (Phase 3 token-split path). Each sub-pass is a sequential slice; sub-passes themselves are sequential.

---

## Phase 1 — Load shared Stage MCP bundle

Call **once** at Stage open:

```
mcp__territory-ia__lifecycle_stage_context({
  slug: "{SLUG}",
  stage_id: "{STAGE_ID}"
})
```

Returns `{stage_header, task_spec_bodies, glossary_anchors, invariants, pair_contract_slice}`. Store as **shared context block** — reused across all Tasks in Stage.

If composite unavailable → fall back to [`domain-context-load`](../domain-context-load/SKILL.md) subskill:

- `keywords` = English tokens from Stage Objectives + Exit + filed Task intents (translate if non-English).
- `brownfield_flag = false` for Stages touching existing subsystems.
- `tooling_only_flag = true` for doc/IA-only Stages.
- `context_label = "stage-authoring Stage {STAGE_ID}"`.

**Do NOT re-run per Task.** `cache_block` = Tier 2 per-Stage ephemeral bundle.

---

## Phase 2 — Read filed Task spec stubs

1. `SLUG` already provided as 1st arg. Use `lifecycle_stage_context` (Phase 1) `stage_header` payload OR call `mcp__territory-ia__stage_render({ slug, stage_id })` to fetch Stage block. Parse Task-table rows with Status ∈ {Draft, In Review, In Progress} AND filed `{ISSUE_ID}` (non-`_pending_` Issue column). Master plan body lives in DB.
2. For each Task: read body via `mcp__territory-ia__task_spec_body({ task_id: "{ISSUE_ID}" })`. DB is sole source of truth — no filesystem fallback.
3. Verify each spec carries §1 Summary + §2.1 Goals + §7 Implementation Plan + `## §Plan Digest _pending — populated by /stage-authoring_` sentinel (or §Plan Digest already populated → idempotent skip per Phase 8.3). Idempotent skip applies to BOTH legacy (`§Mechanical Steps`) AND relaxed (`§Work Items`) sub-headings — do NOT re-author either shape.
4. Collect into `task_specs[] = [{task_id, body, source: "db"}]`.

Missing spec body in DB → abort with `STOPPED — task spec body missing for {ISSUE_ID}`. Re-route caller to `/stage-file` to file the stub first.

---

## Phase 3 — Token-split guardrail

Count total input tokens: Stage header + N spec stubs + MCP bundle + invariants snippet. Opus threshold ≈ 180k input tokens (leave headroom for output).

- Under threshold → proceed to Phase 4 with single bulk pass (N Tasks).
- Over threshold → split into ⌈N/2⌉ bulk sub-passes. Each sub-pass covers ⌈N/2⌉ Tasks; shared context (Stage header + MCP bundle + glossary table) replayed per sub-pass.
- **Never** regress to per-Task mode — per-Task authoring defeats the bulk intent.

Emit split decision in hand-off summary.

---

## Phase 4 — Bulk author §Plan Digest (relaxed shape, direct)

Single Opus call returns a map `{ISSUE_ID → §Plan Digest body}`. For each Task spec:

### 4.1 Compose §Plan Digest body

Shape mirrors [`ia/templates/plan-digest-section.md`](../../templates/plan-digest-section.md). Required sub-sections in order:

```markdown
## §Plan Digest

### §Goal

<!-- 1–2 sentences — task outcome in product / domain terms. Glossary-aligned. -->

### §Acceptance

- [ ] {observable behavior 1}
- [ ] {observable behavior 2}

### §Pending Decisions

- {decision name}: {choice chosen} — rationale: {why}
- {path or symbol name}: {resolved value}

### §Implementer Latitude

- {area}: implementer chooses freely (constraint: {invariant id or §Acceptance row})

### §Work Items

**Edits:** (intent only — implementer locates anchors against current HEAD)

- `{repo-relative-path}`: {1-line what + why}
- (Scene Wiring) `Assets/Scenes/{scene}.unity`: wire `{ComponentName}` per `ia/rules/unity-scene-wiring.md` — only when triggers fire.

### §Test Blueprint

- {test_name}: assert {behavior in glossary terms}

### §Invariants & Gate

invariant_touchpoints:
  - id: {invariant_id}
    expected: pass | unchanged | none

validator_gate: {npm run validate:all | npm run unity:compile-check | …}

escalation_enum: STOP-on-anchor-mismatch | STOP-on-acceptance-unmet | STOP-on-invariant-regression | STOP-on-validator-fail

**Gate:**
```bash
{single command line}
```

**STOP:** anchor_hint mismatch OR §Acceptance row unmet OR invariant regression OR validator_gate non-zero → escalate to caller; do NOT silently adapt.
```

### 4.2 Authoring rules

- **§Goal:** product/domain phrasing per Task intent + Stage Objectives. Glossary terms only.
- **§Acceptance:** sharp behavior contract. Each row = one observable behavior code-review + verify-loop will gate on. Derived from §1 Summary + §2.1 Goals + §7 Implementation Plan stub.
- **§Pending Decisions:** every non-trivial pick the implementer would otherwise face (helper choice, name, type, path, behavior pivot). Resolved here, not deferred.
- **§Implementer Latitude:** picks deferred to implementer. Each row MUST cite its bounding constraint (invariant id or §Acceptance row). Empty list = digest is fully prescriptive on design surface.
- **§Work Items:** flat list of file targets + 1-line intent. NO verbatim before/after code blocks. NO numbered steps. Implementer sequences.
- **§Test Blueprint:** test intents only. Implementer designs inputs / expected / picks harness from {`node`, `unity-batch`, `bridge`, `manual`}.
- **§Invariants & Gate:** ONE block per digest. Per-step variants forbidden — implementer applies all work items then runs the single gate.

### 4.3 Work-item authoring rules

For each row in §Work Items:

1. Confirm `{path}` exists on HEAD via `plan_digest_verify_paths` (creates exempted — flag with `(create)` prefix in the intent line).
2. Write a one-line intent: `{path}: {what changes + why}`. NO before/after code blocks. NO anchor literals. The implementer locates the exact byte position.
3. Do NOT prescribe operation type (edit/create/delete) unless ambiguous — implementer decides from intent + HEAD state. Use `(create)` / `(delete)` prefixes only when intent doesn't make it self-evident.
4. Do NOT call `plan_digest_render_literal` or `plan_digest_resolve_anchor` — those are legacy verbatim-rendering helpers, retired in the relaxed shape.
5. Glossary terms only — no ad-hoc synonyms.

### 4.4 Scene Wiring entry (mandatory when triggered)

Trigger detection (at author time): scan Task scope for
- (a) a new `class X : MonoBehaviour` under `Assets/Scripts/**/*.cs` that exposes `[SerializeField]` / `UnityEvent` / reads StreamingAssets, OR
- (b) a new `[SerializeField]` field on an existing scene object, OR
- (c) a new prefab expected at scene boot, OR
- (d) a new `UnityEvent` wired from the Inspector.

Zero triggers → omit Scene Wiring entry.

Any trigger fires → emit a SINGLE Scene Wiring row in §Work Items (last entry, after script + test work items):

```
- (Scene Wiring) `Assets/Scenes/{SCENE}.unity`: wire `{ComponentName}` under `{parent_object}` per ia/rules/unity-scene-wiring.md — populate [SerializeField] fields per §Pending Decisions; prefer unity_bridge_command sequence (open_scene → create_gameobject → set_gameobject_parent → attach_component → assign_serialized_field → save_scene); text-edit fallback when bridge unavailable.
```

Decisions about parent object, serialized field values, and prefab references go in §Pending Decisions (not in the Work Items row). Evidence requirements (scene/parent/component/serialized_fields/unity_events/compile_check) are inherited from `ia/rules/unity-scene-wiring.md` — implementer emits the evidence block; digester does not pre-render it.

`/ship-stage` Pass A scene-wiring preflight detects the `(Scene Wiring)` prefix in §Work Items (or the legacy `Scene Wiring` step in §Mechanical Steps for backwards compat) and gates the worktree diff for `Assets/Scenes/*.unity` edits.

### 4.5 Canonical-term scan (narrowed)

Same Opus pass enforces canonical glossary terms within the new §Plan Digest body ONLY. Do NOT scan §1 / §4 / §5 / §7 / §8 / §10 — those sections are upstream responsibility (`/stage-file` + spec stub author) and re-scanning them every digest pass is wasted work.

#### 4.5a Glossary alignment (digest body only)

For each new §Plan Digest body, every domain term MUST match `ia/specs/glossary.md` spelling exactly. Ad-hoc synonyms → replace with canonical term inline. Term not in glossary → leave as-is + emit warning in hand-off (do NOT edit glossary from this skill, do NOT add §Open Questions row from this pass).

#### 4.5b Retired-surface scan (digest body only)

Hard-coded retired surface names that must NOT appear in the new §Plan Digest body:

| Retired | Live successor |
|---------|---------------|
| `/enrich` / `spec-enrich` | `/stage-authoring --task {ISSUE_ID}` |
| `/kickoff` / `spec-kickoff` | `/stage-authoring` (Stage 1×N) |
| `/author {id}` / `plan-author` | `/stage-authoring` |
| `/plan-digest` / `plan-digest` | `/stage-authoring` |
| `project-spec-close` / `project-stage-close` | folded into `/ship-stage` Pass B inline closeout |
| `stage-file-monolith` / `stage-file-planner` / `stage-file-applier` | `/stage-file` |
| `project-new-plan` | `/project-new` |
| `docs/implementation/{slug}-stage-X.Y-plan.md` ref | drop entirely |
| `§Mechanical Steps` (heading) | `§Work Items` (in newly-authored bodies; legacy bodies untouched) |

Match → replace inline. Per-Task counter: `n_retired_refs_replaced` (digest-body only).

The fuller fold (§1/§4/§5/§7/§8/§10 scan, template-section allowlist, cross-ref task-id resolver across the whole spec) is RETIRED from this skill — those sections are not authored here, so re-scanning is overhead. Spec-wide drift surfaces at code-review time on the Stage diff (`opus-code-reviewer` Phase 2/3); no separate plan-review pass.

Per-Task counters retained for hand-off: `n_term_replacements` (digest body only), `n_retired_refs_replaced` (digest body only). All other counters dropped.

---

## Phase 5 — Self-lint via plan_digest_lint

For each per-Task §Plan Digest body:

1. Call `mcp__territory-ia__plan_digest_lint({ content })`. `pass: true` → continue.
2. `pass: false` → revise failing tuples in-place; re-run lint once. Second failure → abort chain with `STOPPED — plan-digest lint critical twice for {ISSUE_ID}`; surface first 5 failures verbatim.

Retry cap = 1 per Task.

Extended `plan_digest_lint` rules:
- Every step touching `Assets/**/*.cs` or runtime files MUST carry non-empty `invariant_touchpoints[]` OR opt-out marker `invariant_touchpoints: none (utility)`. Missing → lint rule 10 failure.
- Every step MUST carry `validator_gate`. Missing → lint rule 11 failure.

No aggregate stage doc lint pass — no aggregate doc.

---

## Phase 6 — Per-task task_spec_section_write to DB

For each Task with §Plan Digest body finalized + lint PASS:

```
mcp__territory-ia__task_spec_section_write({
  task_id: "{ISSUE_ID}",
  section: "§Plan Digest",
  body: "{rendered §Plan Digest markdown}"
})
```

Returns `{ok: true, version}` (history snapshot row written to `ia_task_spec_history`).

Errors:
- `task_not_found` → escalate (should not happen — Phase 2 verified spec presence).
- `section_anchor_ambiguous` → escalate; manual edit fallback.
- `db_unavailable` → escalate; do NOT fall back to filesystem write (DB is source of truth).

**No filesystem mirror** — DB write is sole persistence.

**Idempotency:** if `task_spec_section_write` returns `unchanged: true` (DB body matches new content) → record skip in hand-off counter.

---

## Phase 7 — Hand-off

### 7.1 Emit hand-off summary

```
stage-authoring done. STAGE_ID={STAGE_ID} AUTHORED={N} SKIPPED={K} (split: {sub_pass_count} sub-pass(es))
Per-Task:
  {ISSUE_ID_1}: §Plan Digest written ({n_work_items} work items, {n_acceptance} acceptance rows, {n_decisions} pending decisions, {n_latitude} latitude rows, {n_tests} test intents); fold: {n_term_replacements}/{n_retired_refs_replaced}; lint=PASS.
  {ISSUE_ID_2}: …
  …
drift_warnings: {true|false}
DB writes: {N} task_spec_section_write OK; {K} unchanged.
next=stage-authoring-chain-continue
```

### 7.2 Validate

Stage-authoring writes only `§Plan Digest` bodies via `task_spec_section_write` MCP (DB rows). Per-Task `plan_digest_lint` already runs in Phase 4 — that is the per-spec integrity gate.

Run only the narrow gate that covers cross-Task / master-plan rollup drift:

```bash
npm run validate:master-plan-status
```

Non-zero exit → escalate.

Do NOT run `validate:all` here — chains 20 sub-validators (Jest `test:ia`, `compute-lib:build`, fixtures, web, mcp tooling, telemetry, runtime-state, cache-block, skill-drift, …) that touch surfaces stage-authoring did not modify. Heavy chain belongs in `/ship-stage` Pass B (post-implementation), not in plan-only DB writes.

### 7.3 Idempotency on re-entry

- §Plan Digest already populated (relaxed `§Work Items` OR legacy `§Mechanical Steps` sub-heading) AND DB body matches AND lint PASS → record `skipped (already authored)`; no new `task_spec_section_write` call. Do NOT migrate legacy bodies to relaxed shape — leave as-is; downstream implementer reads both.
- §Plan Digest empty / sentinel `_pending — populated by /stage-authoring_` → fresh authoring pass per Phase 4 (writes relaxed shape).
- §Plan Digest populated but lint FAIL → re-author per Phase 4 (cap=1 per Task), writing relaxed shape.

### 7.4 Next-step

Dispatcher (`/stage-file`) receives this hand-off and continues to:

- **N≥2:** `Next: claude-personal "/ship-stage {SLUG} Stage {STAGE_ID}"` (runs implement + verify + code-review + closeout).
- **N=1:** `Next: claude-personal "/ship {ISSUE_ID}"`.

When invoked standalone (not via `/stage-file` chain): emit same handoff verbatim.

---

## Hard boundaries

- Do NOT write `## §Plan Author` section.
- Do NOT compile aggregate `docs/implementation/{slug}-stage-{STAGE_ID}-plan.md`.
- Do NOT write code, run verify, or flip Task status.
- Do NOT author specs outside target Stage.
- Do NOT regress to per-Task mode if tokens exceed threshold — split into ⌈N/2⌉ bulk sub-passes.
- Do NOT resolve picks — `plan_digest_scan_for_picks` is lint-only; leak = abort + handoff to operator.
- Do NOT call `domain-context-load` per Task — Phase 1 once per Stage.
- Do NOT skip the Scene Wiring step when triggered — wiring is a Stage deliverable per [`ia/rules/unity-scene-wiring.md`](../../rules/unity-scene-wiring.md); dropping the step lets Stages ship dead runtime paths.
- Do NOT fall back to filesystem-only write on DB unavailable — escalate; DB is source of truth.
- Do NOT commit — user decides.

---

## Escalation rules

| Trigger | Halt shape |
|---------|-----------|
| Task spec missing for filed `{ISSUE_ID}` (Phase 2) | `STOPPED — task spec missing for {ISSUE_ID}`; route caller to `/stage-file`. |
| Token-split sub-pass count > N (Phase 3) | Surface counter; user confirms split or aborts. |
| `plan_digest_lint` PASS=false twice for any Task (Phase 5) | `STOPPED — plan-digest lint critical twice for {ISSUE_ID}`; surface first 5 failures. |
| `task_spec_section_write` `task_not_found` / `section_anchor_ambiguous` (Phase 6) | Escalate; manual edit fallback. |
| `task_spec_section_write` `db_unavailable` (Phase 6) | Escalate; do NOT silently fall back to filesystem. |
| Phase 7.2 `validate:master-plan-status` non-zero | Escalate post-loop; emit stderr. |

---

## Cross-references

- [`ia/rules/plan-digest-contract.md`](../../rules/plan-digest-contract.md) — 9-point rubric enforced by `plan_digest_lint`.
- [`ia/rules/unity-scene-wiring.md`](../../rules/unity-scene-wiring.md) — Scene Wiring step trigger checklist.
- [`ia/templates/plan-digest-section.md`](../../templates/plan-digest-section.md) — §Plan Digest section template fragment.
- [`ia/skills/stage-file/SKILL.md`](../stage-file/SKILL.md) — upstream (writes N filed spec stubs + DB rows; tail calls this skill inline).
- [`ia/skills/ship-stage/SKILL.md`](../ship-stage/SKILL.md) — downstream (Pass A implement + Pass B verify + closeout).
- [`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md) — shared Stage MCP bundle recipe (fallback when `lifecycle_stage_context` unavailable).

