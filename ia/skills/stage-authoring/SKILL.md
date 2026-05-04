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
  narrowed to §Plan Digest body only. Rubric injected into Opus authoring prompt as hard
  constraints (no post-author lint, no retry loop) + per-section soft byte caps emitted as
  warnings in handoff. No aggregate doc compile.
  Triggers: "/stage-authoring {SLUG} {STAGE_ID}", "stage authoring", "stage-scoped digest",
  "author stage tasks". Argument order (explicit): SLUG first, STAGE_ID second.
phases:
  - Sequential-dispatch guardrail
  - Load shared Stage MCP bundle
  - Read filed Task spec stubs
  - Token-split guardrail
  - Bulk author §Plan Digest (relaxed shape, direct, rubric-in-prompt)
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
  - Do NOT call `plan_digest_lint` MCP — rubric is enforced in-prompt only; no post-author lint or retry loop.
  - Do NOT call `lifecycle_stage_context` / `domain-context-load` per Task — Phase 1 once per Stage.
  - Do NOT skip the Scene Wiring step when triggered (new MonoBehaviour / `[SerializeField]` / scene prefab / `UnityEvent`) — per `ia/rules/unity-scene-wiring.md`.
caller_agent: stage-authoring
---

# Stage-authoring skill — DB-backed single-skill

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** Single-skill stage-scoped spec-body authoring. Reads filed Task spec stubs of one Stage; writes §Plan Digest **direct** in one Opus bulk pass; persists per-Task body to DB via `task_spec_section_write` MCP. No aggregate doc compile.

**Contract:** [`ia/rules/plan-digest-contract.md`](../../rules/plan-digest-contract.md) — 10-point rubric (9 contract + per-section soft caps). Rubric injected verbatim into the Phase 4 Opus authoring prompt as hard constraints. No post-author lint MCP call; no retry loop.

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
3. Verify each spec carries §1 Summary + §2.1 Goals + §7 Implementation Plan + `## §Plan Digest _pending — populated by /stage-authoring_` sentinel (or §Plan Digest already populated → idempotent skip per Phase 6.3). Idempotent skip applies to BOTH legacy (`§Mechanical Steps`) AND relaxed (`§Work Items`) sub-headings — do NOT re-author either shape.
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

## Phase 4 — Bulk author §Plan Digest (relaxed shape, direct, rubric-in-prompt)

Single Opus call returns a map `{ISSUE_ID → §Plan Digest body}`. Rubric injected verbatim into the authoring prompt as hard constraints — no post-author lint MCP call, no retry loop. For each Task spec:

**Phase 4.0 gate runs first** — §Red-Stage Proof confirm + lint blocks Plan Digest authoring on red lint. See Phase 4.0 below.

### Phase 4.0 — Confirm §Red-Stage Proof

Before authoring §Plan Digest, confirm the Stage's `**§Red-Stage Proof:**` block and bind `red_test_anchor` to a concrete test path + method.

**Steps:**
1. Read `red_test_anchor`, `target_kind`, `proof_artifact_id`, `proof_status` from the Stage block `**§Red-Stage Proof:**` field (master plan stage body — proof is Stage-scoped, not Task-scoped).
2. Resolve anchor via `tools/lib/red-stage-anchor-resolver.ts` `resolveAnchor()` semantics: parse `{anchor-kind}:{path}::{method}` from `red_test_anchor`.
3. **In-prompt lint:** assert the parsed `{method}` contains the canonical noun phrase for the Stage's `target_kind`:
   - `target_kind=tracer_verb` → method name MUST contain pascal-case noun phrase derived from §Tracer Slice `verb` (strip non-alphanumeric, pascal-case).
   - `target_kind=visibility_delta` → method name MUST contain pascal-case noun phrase derived from §Visibility Delta sentence head (split on space, leading capitalised tokens; fallback = first 3 words pascal-cased).
   - `target_kind=bug_repro` → method name MUST contain literal `BUG-NNNN` token (digits preserved).
4. **Lint failure** → emit `RED_STAGE_PROOF_LINT_FAIL` structured error naming the failing field + halt Stage authoring (no `task_spec_section_write` call for that Stage). Return `{escalation: true, phase: 4, reason: "red_stage_proof_lint_fail", stage_id, failing_field}`.
5. **Skip-clause:** `target_kind=design_only` AND `proof_artifact_id=n/a` AND `proof_status=not_applicable` triple-match → bypass anchor lint entirely; proceed to Phase 4.1.

**Cross-link:** `ia/rules/tdd-red-green-methodology.md` — anchor grammar + enum tables for `target_kind`, `proof_status`, `red_test_anchor` format.

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
- **§Pending Decisions:** every non-trivial pick the implementer would otherwise face (helper choice, name, type, path, behavior pivot). EVERY ROW LOCKED — format `{decision name}: {choice chosen} — rationale: {why}`. Forbidden row shapes: question form (`?`, `which?`), `TBD`, `see spec X`, `defer to implementer`, `pick A or B`, `unresolved`, `open question`. The digester picks using domain signal (Stage Objectives + Exit + glossary + invariants + spec sections + sibling Tasks) and best-judgment defaults (least-surprising / minimal-mechanism / consistent-with-prior-Stage). Genuinely unsignalled pick AND unsafe to default → escalate via `escalation_enum: decision_required` in §Invariants & Gate + halt the authoring pass with `STOPPED — decision_required: {decision name}`. Do NOT leave the row in §Pending Decisions as an open question.
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

Per-Task counters retained for hand-off: `n_term_replacements` (digest body only), `n_retired_refs_replaced` (digest body only), `n_section_overrun` (digest body only). All other counters dropped.

### 4.6 Rubric injection (in-prompt hard constraints)

The Opus authoring prompt MUST embed the 10-point rubric verbatim as a hard-constraint preamble — NO post-author lint MCP call, NO retry loop. Rubric source: [`ia/rules/plan-digest-contract.md`](../../rules/plan-digest-contract.md).

**Rubric (inject verbatim into prompt preamble):**

```
HARD CONSTRAINTS — every Task §Plan Digest body MUST satisfy:

0. SECTION HEADING IS LITERAL §. The body's top heading line MUST be exactly:

       ## §Plan Digest

   The character before `Plan Digest` is the section-marker `§` (Unicode
   U+00A7 SECTION SIGN), NOT `S`, NOT `&sect;`, NOT omitted, NOT replaced
   with any look-alike. Sub-section headings MUST also carry the literal §:

       ### §Goal
       ### §Acceptance
       ### §Pending Decisions
       ### §Implementer Latitude
       ### §Work Items
       ### §Test Blueprint
       ### §Invariants & Gate

   Bare `## Plan Digest` / `### Goal` / etc. are REJECTED — `/ship-stage`
   readiness gate looks up the section by literal `§Plan Digest` and a
   missing § returns `section_not_found`. Same rule applies to the
   `task_spec_section_write` `section` arg: pass `"§Plan Digest"` (with §),
   never `"Plan Digest"`.

1. §Goal present — 1–2 sentences in product/domain terms; glossary-aligned.
2. §Acceptance present — every row = one observable behavior; checkbox shape.
3. §Pending Decisions present — EVERY row LOCKED with `{decision}: {choice} — rationale: {why}` shape. Forbidden: question form, `TBD`, `see spec`, `defer to implementer`, `pick A or B`, `unresolved`, `open`. Genuinely cannot pick AND unsafe to default → halt with `STOPPED — decision_required: {decision name}` + set `escalation_enum: decision_required`. Never leave a row as an open question.
4. §Implementer Latitude present — each row cites bounding constraint (invariant id or §Acceptance row); empty list OK.
5. §Work Items present — flat list of `{path}: {1-line intent}`; NO before/after code blocks; NO numbered steps.
6. §Test Blueprint present — test intents only (no inputs/expected/harness picks).
7. §Invariants & Gate present — ONE block per digest with invariant_touchpoints[] + validator_gate + escalation_enum + Gate command + STOP clause.
8. invariant_touchpoints[] non-empty OR `invariant_touchpoints: none (utility)` opt-out marker — required when Work Items touch `Assets/**/*.cs` or runtime files.
9. validator_gate present — single command line (e.g. `npm run validate:all`, `npm run unity:compile-check`).
10. Per-section soft byte caps (warn-only, do NOT abort):
    §Goal ≤400B · §Acceptance ≤1500B · §Pending Decisions ≤1500B ·
    §Implementer Latitude ≤800B · §Work Items ≤2000B · §Test Blueprint ≤1000B ·
    §Invariants & Gate ≤800B · total target ≈8KB.
    Overrun → emit `n_section_overrun` counter in handoff; do NOT abort.
```

Author measures byte length per sub-section after composition. Overruns recorded as warnings; chain proceeds.

---

## Phase 5 — Per-task task_spec_section_write to DB

For each Task with §Plan Digest body finalized:

```
mcp__territory-ia__task_spec_section_write({
  task_id: "{ISSUE_ID}",
  section: "§Plan Digest",
  body: "{rendered §Plan Digest markdown}"
})
```

Returns `{ok: true, version, heading_normalized}` (history snapshot row written to `ia_task_spec_history`).

Errors:
- `task_not_found` → escalate (should not happen — Phase 2 verified spec presence).
- `section_anchor_ambiguous` → escalate; manual edit fallback.
- `db_unavailable` → escalate; do NOT fall back to filesystem write (DB is source of truth).

**No filesystem mirror** — DB write is sole persistence.

**Idempotency:** if `task_spec_section_write` returns `unchanged: true` (DB body matches new content) → record skip in hand-off counter.

### 5.1 Heading-drift self-check (mandatory per Task)

After every `task_spec_section_write` for `§Plan Digest`:

1. Inspect the result. If `heading_normalized: true` → MCP auto-corrected a § drift (rubric #0 violated during composition). Increment per-Task `n_heading_normalized` counter and surface in hand-off.
2. Read back via:

   ```
   mcp__territory-ia__task_spec_section({task_id, section: "§Plan Digest"})
   ```

   - `ok: true` → confirmed; the `/ship-stage` readiness gate will see the section.
   - `section_not_found` → DRIFT NOT RECOVERED. Escalate with `STOPPED — heading_drift: {task_id} §Plan Digest` and re-author once with the literal § character.

The read-back is cheap (single DB query per task) and guarantees the readiness gate of `/ship-stage` Phase 3 passes — preventing the failure mode where stage-authoring reports success but the next chain step finds `section_not_found` for all N tasks.

---

## Phase 6 — Hand-off

### 6.1 Emit hand-off summary

```
stage-authoring done. STAGE_ID={STAGE_ID} AUTHORED={N} SKIPPED={K} (split: {sub_pass_count} sub-pass(es))
Per-Task:
  {ISSUE_ID_1}: §Plan Digest written ({n_work_items} work items, {n_acceptance} acceptance rows, {n_decisions_locked} decisions LOCKED, {n_latitude} latitude rows, {n_tests} test intents); fold: {n_term_replacements}/{n_retired_refs_replaced}; section_overrun={n_section_overrun}; n_heading_normalized={n_heading_normalized}; n_unresolved_decisions=0 (must be 0 — non-zero → halt).
  {ISSUE_ID_2}: …
  …
drift_warnings: {true|false}
DB writes: {N} task_spec_section_write OK; {K} unchanged; {H} heading_normalized.
next=stage-authoring-chain-continue
```

### 6.2 Validate

Stage-authoring writes only `§Plan Digest` bodies via `task_spec_section_write` MCP (DB rows). Rubric is enforced in-prompt at Phase 4 — no post-author lint pass.

Run only the narrow gate that covers cross-Task / master-plan rollup drift:

```bash
npm run validate:master-plan-status
```

Non-zero exit → escalate.

Do NOT run `validate:all` here — chains 20 sub-validators (Jest `test:ia`, `compute-lib:build`, fixtures, web, mcp tooling, telemetry, runtime-state, cache-block, skill-drift, …) that touch surfaces stage-authoring did not modify. Heavy chain belongs in `/ship-stage` Pass B (post-implementation), not in plan-only DB writes.

### 6.3 Idempotency on re-entry

- §Plan Digest already populated (relaxed `§Work Items` OR legacy `§Mechanical Steps` sub-heading) AND DB body matches → record `skipped (already authored)`; no new `task_spec_section_write` call. Do NOT migrate legacy bodies to relaxed shape — leave as-is; downstream implementer reads both.
- §Plan Digest empty / sentinel `_pending — populated by /stage-authoring_` → fresh authoring pass per Phase 4 (writes relaxed shape).

### 6.4 Next-step

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
- Do NOT call `plan_digest_lint` MCP — rubric is enforced in-prompt only; no post-author lint or retry loop.
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
| `task_spec_section_write` `task_not_found` / `section_anchor_ambiguous` (Phase 5) | Escalate; manual edit fallback. |
| `task_spec_section_write` `db_unavailable` (Phase 5) | Escalate; do NOT silently fall back to filesystem. |
| Phase 6.2 `validate:master-plan-status` non-zero | Escalate post-loop; emit stderr. |
| Phase 4 `decision_required` (zero domain signal AND unsafe to default) | Halt with `STOPPED — decision_required: {decision name}`; set `escalation_enum: decision_required` in §Invariants & Gate; do NOT persist body for that Task. Surface decision name + Task id to caller. |

---

## Cross-references

- [`ia/rules/plan-digest-contract.md`](../../rules/plan-digest-contract.md) — 10-point rubric (9 contract + per-section soft caps) injected verbatim into Phase 4 Opus prompt.
- [`ia/rules/unity-scene-wiring.md`](../../rules/unity-scene-wiring.md) — Scene Wiring step trigger checklist.
- [`ia/templates/plan-digest-section.md`](../../templates/plan-digest-section.md) — §Plan Digest section template fragment.
- [`ia/skills/stage-file/SKILL.md`](../stage-file/SKILL.md) — upstream (writes N filed spec stubs + DB rows; tail calls this skill inline).
- [`ia/skills/ship-stage/SKILL.md`](../ship-stage/SKILL.md) — downstream (Pass A implement + Pass B verify + closeout).
- [`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md) — shared Stage MCP bundle recipe (fallback when `lifecycle_stage_context` unavailable).


---

## Changelog
