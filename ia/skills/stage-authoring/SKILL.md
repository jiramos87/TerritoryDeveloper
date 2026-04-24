---
purpose: "DB-backed single-skill stage-authoring: merges retired plan-author + plan-digest into one Opus bulk pass that writes §Plan Digest direct (no §Plan Author intermediate) per task via task_spec_section_write MCP. No aggregate doc."
audience: agent
loaded_by: skill:stage-authoring
slices_via: none
name: stage-authoring
description: >
  DB-backed single-skill stage-authoring — replaces legacy plan-author (Opus
  bulk §Plan Author writer) + plan-digest (Opus bulk mechanizer) pair (retired
  2026-04-24 Step 7 of `docs/ia-dev-db-refactor-implementation.md`). One Opus
  bulk pass authors §Plan Digest direct per filed Task spec stub of one Stage
  (rich format: Goal / Acceptance / Test Blueprint / Examples / sequential
  Mechanical Steps with Edits + Gate + STOP + MCP hints + optional Scene
  Wiring step). NO §Plan Author intermediate per design B6 / C7 (stub →
  digest direct). Persists each per-Task §Plan Digest body to DB via
  `task_spec_section_write` MCP. Absorbs canonical-term fold (glossary +
  retired-surface tombstone + template-section allowlist + cross-ref task-id
  resolver) into the same bulk pass. Self-lints via `plan_digest_lint`
  (cap=1 retry). Mechanicalization preflight via
  `mechanicalization_preflight_lint`. NO aggregate
  `docs/implementation/{slug}-stage-{STAGE_ID}-plan.md` doc per design D8
  (drops entirely).
  Triggers: "/stage-authoring {ORCHESTRATOR_SPEC} {STAGE_ID}",
  "stage authoring", "merged plan-author + plan-digest", "stage-scoped digest",
  "author + digest in one pass", "/author {ORCHESTRATOR_SPEC} {STAGE_ID}"
  (legacy alias).
  Argument order (explicit): ORCHESTRATOR_SPEC first, STAGE_ID second.
model: opus
phases:
  - "Sequential-dispatch guardrail"
  - "Load shared Stage MCP bundle"
  - "Read filed Task spec stubs"
  - "Token-split guardrail"
  - "Bulk author §Plan Digest (direct, no §Plan Author)"
  - "Self-lint via plan_digest_lint"
  - "Mechanicalization preflight"
  - "Per-task task_spec_section_write to DB"
  - "Hand-off"
---

# Stage-authoring skill — DB-backed single-skill (merged plan-author + plan-digest)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** Single-skill stage-scoped spec-body authoring. Reads filed Task spec stubs of one Stage; writes §Plan Digest **direct** in one Opus bulk pass (no §Plan Author intermediate); persists per-Task body to DB via `task_spec_section_write` MCP. Replaces retired plan-author + plan-digest pair (B2). Drops aggregate `docs/implementation/` compile (D8).

**Step 7 of `docs/ia-dev-db-refactor-implementation.md`** — this skill replaces retired [`plan-author/SKILL.md`](../_retired/plan-author/SKILL.md) + [`plan-digest/SKILL.md`](../_retired/plan-digest/SKILL.md). Design decisions B2 (merge) + B6 (drop §Plan Author surface) + C7 (digest direct, no aggregate) + C8 (stage-file tail calls inline) + D8 (aggregate doc retires) per [`docs/master-plan-foldering-refactor-design.md`](../../../docs/master-plan-foldering-refactor-design.md) §3.4.

**Contract:** [`ia/rules/plan-digest-contract.md`](../../rules/plan-digest-contract.md) — 9-point rubric, enforced by `plan_digest_lint`.

**Upstream:** `stage-file` (writes N filed spec stubs + DB rows). **Downstream:** `/ship-stage` (N≥2) or `/ship` (N=1). `stage-file` dispatcher calls this skill inline per C8.

---

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `ORCHESTRATOR_SPEC` | 1st arg | Repo-relative path to `ia/projects/{master-plan}.md`. |
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
  master_plan_path: "{ORCHESTRATOR_SPEC}",
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

1. Read `ORCHESTRATOR_SPEC`. Locate `### Stage {STAGE_ID}` (H3 canonical). Collect Task rows with Status ∈ {Draft, In Review, In Progress} AND filed `{ISSUE_ID}` (non-`_pending_` Issue column).
2. For each Task: prefer DB read via `mcp__territory-ia__task_spec_body({ task_id: "{ISSUE_ID}" })`. Fallback to `Read ia/projects/{ISSUE_ID}.md` when DB body empty (pre-Step-9 transitional — bodies may live on filesystem).
3. Verify each spec carries §1 Summary + §2.1 Goals + §7 Implementation Plan + `## §Plan Digest _pending — populated by /stage-authoring_` sentinel (or §Plan Digest already populated → idempotent skip per Phase 8.3).
4. Collect into `task_specs[] = [{task_id, body, source: "db"|"fs"}]`.

Missing spec stub → abort with `STOPPED — task spec missing for {ISSUE_ID}`. Re-route caller to `/stage-file` to file the stub first.

---

## Phase 3 — Token-split guardrail

Count total input tokens: Stage header + N spec stubs + MCP bundle + invariants snippet. Opus threshold ≈ 180k input tokens (leave headroom for output).

- Under threshold → proceed to Phase 4 with single bulk pass (N Tasks).
- Over threshold → split into ⌈N/2⌉ bulk sub-passes. Each sub-pass covers ⌈N/2⌉ Tasks; shared context (Stage header + MCP bundle + glossary table) replayed per sub-pass.
- **Never** regress to per-Task mode — per-Task authoring defeats the bulk intent (R10 regression bar carried from retired plan-author).

Emit split decision in hand-off summary.

---

## Phase 4 — Bulk author §Plan Digest (direct, no §Plan Author)

Single Opus call returns a map `{ISSUE_ID → §Plan Digest body}`. For each Task spec:

### 4.1 Compose §Plan Digest body

Shape mirrors [`ia/templates/plan-digest-section.md`](../../templates/plan-digest-section.md). Required sub-sections in order:

```markdown
## §Plan Digest

### §Goal

<!-- 1–2 sentences — task outcome in product / domain terms. Glossary-aligned. -->

### §Acceptance

<!-- Checkbox list — refined per-Task acceptance. Narrower than Stage Exit. -->

- [ ] …

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| {name} | {inputs} | {expected} | {node \| unity-batch \| bridge \| manual} |

### §Examples

<!-- Concrete inputs/outputs + edge cases. Tables or code blocks. -->

### §Mechanical Steps

#### Step 1 — {name}

**Goal:** …

**Edits:**
- `{repo-relative-path}` — **before**:
  ```
  …
  ```
  **after**:
  ```
  …
  ```

**Gate:**
```bash
…
```

**STOP:** …

**MCP hints:** `plan_digest_resolve_anchor`, `{other}`

**invariant_touchpoints:**
  - id: {invariant_id}
    gate: {MCP call or grep pattern}
    expected: pass | unchanged | none

**validator_gate:** {npm run validate:all | npm run unity:compile-check | …}
```

### 4.2 Authoring rules (absorbed from retired plan-author Phase 3)

- **§Goal:** product/domain phrasing per Task intent + Stage Objectives. Glossary terms only (no ad-hoc synonyms).
- **§Acceptance:** narrower than Stage Exit. Checkbox per concrete deliverable. Derived from §1 Summary + §2.1 Goals + §7 Implementation Plan stub.
- **§Test Blueprint:** structured tuple table consumed by `/implement` + `/verify-loop`. One row per test. Harness column constrained to {`node`, `unity-batch`, `bridge`, `manual`}.
- **§Examples:** edge cases + legacy shapes + canonical inputs. Tables or code blocks.
- **§Mechanical Steps:** sequential checklist of Edit tuples — author in execution order.

### 4.3 Mechanical step rules (absorbed from retired plan-digest Phase 2)

For each Edit tuple:

1. Translate authoring narrative into `(operation, target_path, before_string, after_string, invariant_touchpoints, validator_gate)`. Use `plan_digest_verify_paths` to confirm every target exists; use `plan_digest_resolve_anchor` to confirm every `before_string` is unique.
2. Required tuple fields:
   ```yaml
   invariant_touchpoints:
     - id: string
       gate: string   # MCP call or grep pattern
       expected: "pass" | "unchanged" | "none"
   validator_gate: string   # npm run validate:all | npm run unity:compile-check | ...
   ```
   If step has no runtime impact, `invariant_touchpoints: none (utility)` replaces the array.
3. Render exact literals for code blocks via `plan_digest_render_literal` when the digest must quote a file literally.
4. For each step, ask `plan_digest_gate_author_helper({operation, file, before, after})` for the canonical gate command + expectation; embed verbatim.
5. Author STOP clause per step (what edit to re-open, or which upstream surface to escalate to).
6. Author Implementer MCP-tool hints per step (subset of `backlog_issue`, `glossary_lookup`, `invariant_preflight`, `plan_digest_resolve_anchor`, `unity_bridge_command`, etc.) — mechanical list, not narrative.

### 4.4 Scene Wiring step (mandatory when triggered)

Trigger detection (at author time): scan Task scope for
- (a) a new `class X : MonoBehaviour` under `Assets/Scripts/**/*.cs` that exposes `[SerializeField]` / `UnityEvent` / reads StreamingAssets, OR
- (b) a new `[SerializeField]` field on an existing scene object, OR
- (c) a new prefab expected at scene boot, OR
- (d) a new `UnityEvent` wired from the Inspector.

Zero triggers → omit Scene Wiring step entirely.

Any trigger fires → emit a dedicated **Scene Wiring** mechanical step in §Mechanical Steps. Shape per [`ia/rules/unity-scene-wiring.md`](../../rules/unity-scene-wiring.md):

- **Goal:** wire `{ComponentName}` into `Assets/Scenes/{SCENE}.unity` under `{parent_object}` with all `[SerializeField]` fields populated per spec.
- **Edits:** prefer `unity_bridge_command` kinds in sequence `open_scene → create_gameobject → set_gameobject_parent → attach_component → assign_serialized_field → save_scene`. Text-edit fallback only when bridge unavailable — include verbatim YAML before/after blocks for the `.unity` hunk.
- **Gate:** `npm run unity:compile-check` exits 0.
- **STOP:** scene file edit must appear in `git diff`; if absent after gate → re-open the Scene Wiring step, do NOT close the Task.
- **MCP hints:** `unity_bridge_command` (preferred), `find_gameobject` to confirm parent, `get_compilation_status` as compile gate when the Editor holds the project lock.
- **Evidence (required verbatim in `after:` literal or §Acceptance):** scene/parent/component/serialized_fields/unity_events/compile_check block per the scene-wiring rule.

Place the Scene Wiring step LAST in §Mechanical Steps (after all script + test edits, before closeout) so the gate runs against the final runtime surface.

### 4.5 Canonical-term fold + drift scan (absorbed from retired plan-author Phase 4)

Same Opus pass enforces canonical glossary terms + scans for retired surface refs. Four sub-checks (all four MUST run per Task; emit per-Task counts in hand-off summary).

#### 4.5a Glossary fold

For each Task body, enforce canonical glossary terms across §1 Summary, §4 Current State, §5 Proposed Design, §7 Implementation Plan AND the new §Plan Digest body.

Rules:
- Every domain term must match `ia/specs/glossary.md` spelling exactly.
- Ad-hoc synonyms → replace with canonical term inline.
- Term not in glossary → add to `§Open Questions` as candidate row (do NOT edit glossary from this skill).

Per-Task counter: `n_term_replacements`.

#### 4.5b Retired-surface tombstone scan

Load tombstone list from disk (one-shot per Stage):

```bash
ls -1 ia/skills/_retired/
ls -1 .claude/agents/_retired/
ls -1 .claude/commands/_retired/
```

Build retired-name set: skill basenames (e.g. `plan-author`, `plan-digest`, `project-spec-kickoff`, `project-spec-close`, `project-stage-close`, `project-new-plan`, `stage-file-monolith`), agent basenames (e.g. `spec-kickoff`, `closeout`, `project-new`, `stage-file-planner`, `stage-file-applier`), command basenames (e.g. `kickoff`).

Plus hard-coded retired slash refs: `/enrich`, `/kickoff`, `/plan-digest`, standalone `/author` when paired with `--mode digest` (any case).

For each Task body, scan §1 / §4 / §5 / §7 / §8 / §10 prose AND new §Plan Digest sub-sections for any retired surface name. Match must be replaced with the live successor:

| Retired | Live successor | Notes |
|---------|---------------|-------|
| `/enrich {id}` / `spec-enrich` | `/stage-authoring --task {ISSUE_ID}` | T7.11 fold |
| `/kickoff` / `spec-kickoff` / `project-spec-kickoff` | `/stage-authoring` (Stage 1×N) | M6 collapse |
| `/author {id}` / `plan-author` | `/stage-authoring` | Step 7 merge |
| `/plan-digest` / `plan-digest` | `/stage-authoring` | Step 7 merge |
| `project-spec-close` / `project-stage-close` | `/closeout` (folded into ship-stage Pass B per C10) | T7.14 / Step 8 |
| `stage-file-monolith` / `stage-file-planner` / `stage-file-applier` | `/stage-file` (DB-backed single-skill per Step 6) | Step 6 merge |
| `project-new-plan` | `/project-new` args-only pair | T7.10 fold |
| `docs/implementation/{slug}-stage-X.Y-plan.md` ref | drop ref entirely (aggregate doc retires per D8) | Step 7 |

Per-Task counter: `n_retired_refs_replaced`.

#### 4.5c Template-section allowlist

Read `ia/templates/project-spec-template.md` once per Stage. Extract every `## ` and `### ` heading line — call this the **canonical-section-set**.

For each Task body, scan `## ` / `### ` headings. Any heading NOT in canonical-section-set = drift. Common drifts:

| Drifted heading | Canonical replacement |
|----------------|----------------------|
| `§Plan Author` (legacy intermediate) | `§Plan Digest` (direct, per B6) |
| `§Closeout Plan` (per-Task) | folded into ship-stage Pass B per C10 — drop section |
| `§Audit Plan` | `§Audit` |
| `§Review` / `§Code Review Plan` | `§Code Review` |

Do NOT delete unknown headings — emit warning in per-Task hand-off entry. If `## §Plan Author` block present → replace entirely with new `## §Plan Digest` body (Step 7 absorbs the surface; §Plan Author retires per B6).

Per-Task counter: `n_section_drift_fixed`.

#### 4.5d Cross-ref task-id resolver

For each Task body, scan all prose for two id classes:

1. **BACKLOG ids**: pattern `\b(BUG|FEAT|TECH|ART|AUDIO)-\d+\b`. Resolve via `mcp__territory-ia__task_state({ task_id })` (DB-backed; covers open + archived). Unresolved → add to per-Task warning list `unresolved_backlog_refs[]`.
2. **Task-key refs**: pattern `\bT\d+\.\d+(\.\d+)?\b` (e.g. `T8.3`). Resolve via owning master plan task-table (read once per Stage from `MASTER_PLAN_PATH`). Unresolved → emit drift entry + add comment `<!-- WARN: stale task-ref {T_REF} — verify against {MASTER_PLAN_PATH} -->` next to the offending line. Auto-rewrite ONLY when ref clearly maps to a single live task (Opus judgment).

Per-Task counters: `n_unresolved_backlog_refs`, `n_stale_task_refs`.

#### 4.5e Stage-level summary

Aggregate counters per Task:

```
{ISSUE_ID}:
  glossary_replacements: {n_term_replacements}
  retired_refs_replaced: {n_retired_refs_replaced}
  section_drift_fixed:   {n_section_drift_fixed}
  unresolved_backlog_refs: [{id}, ...]
  stale_task_refs:         [{T_REF}, ...]
```

Sub-pass exit gate: if `unresolved_backlog_refs` OR `stale_task_refs` non-empty for ANY Task → tag Stage hand-off summary with `drift_warnings: true`. (No downstream `/plan-review` per design — drift surfaces in hand-off summary only.)

---

## Phase 5 — Self-lint via plan_digest_lint

For each per-Task §Plan Digest body:

1. Call `mcp__territory-ia__plan_digest_lint({ content })`. `pass: true` → continue.
2. `pass: false` → revise failing tuples in-place; re-run lint once. Second failure → abort chain with `STOPPED — plan-digest lint critical twice for {ISSUE_ID}`; surface first 5 failures verbatim.

Retry cap = 1 per Task.

Extended `plan_digest_lint` rules:
- Every step touching `Assets/**/*.cs` or runtime files MUST carry non-empty `invariant_touchpoints[]` OR opt-out marker `invariant_touchpoints: none (utility)`. Missing → lint rule 10 failure.
- Every step MUST carry `validator_gate`. Missing → lint rule 11 failure.

NO aggregate stage doc lint pass — aggregate doc retired per D8.

---

## Phase 6 — Mechanicalization preflight

Run `mechanicalization-preflight` skill over each per-Task §Plan Digest body:

1. Call `mcp__territory-ia__mechanicalization_preflight_lint({ artifact_path: "{ia/projects/{ISSUE_ID}.md|db:{ISSUE_ID}}", artifact_kind: "plan_digest" })`.
2. `pass: true` → prepend `mechanicalization_score` YAML header at top of §Plan Digest body per `ia/rules/mechanicalization-contract.md`.
3. `pass: false` → halt with `STOPPED — mechanicalization_score: {overall}; failing_fields: [...]` for {ISSUE_ID}; do NOT persist artifact.
4. **Advisory escape hatch (TECH-776 regex drift):** if `pass: false` AND `failing_fields == ["picks"]` AND Phase 5 `plan_digest_lint` was PASS AND no missing-path findings → prepend `mechanicalization_score: advisory_partial; failing_fields: [picks]; reason: preflight-regex-vs-rich-format-drift` header + continue. Tracks structural fix in TECH-776.

---

## Phase 7 — Per-task task_spec_section_write to DB

For each Task with §Plan Digest body finalized + lint PASS + preflight PASS:

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
- `db_unavailable` → escalate; do NOT fall back to filesystem write (DB is source of truth per Step 6+).

**Filesystem write (transitional):** while `ia/projects/{ISSUE_ID}.md` files still exist on disk pre-Step-9, ALSO Edit the file in-place to keep filesystem mirror coherent. Replace any existing `## §Plan Digest` block (idempotent on re-run) or insert after last line of `## 10. Lessons Learned` (before `## Open Questions`). Drop legacy `## §Plan Author` block in same write pass.

Post-Step-9 (filesystem specs DELETED): drop the filesystem mirror branch — DB write is the only persistence.

**Idempotency:** if `task_spec_section_write` returns `unchanged: true` (DB body matches new content) → skip filesystem mirror; record skip in hand-off counter.

---

## Phase 8 — Hand-off

### 8.1 Emit hand-off summary

```
stage-authoring done. STAGE_ID={STAGE_ID} AUTHORED={N} SKIPPED={K} (split: {sub_pass_count} sub-pass(es))
Per-Task:
  {ISSUE_ID_1}: §Plan Digest written ({n_steps} mechanical steps, {n_acceptance} acceptance criteria, {n_tests} test rows); fold: {n_term_replacements}/{n_retired_refs_replaced}/{n_section_drift_fixed}; lint=PASS; preflight=PASS.
  {ISSUE_ID_2}: …
  …
drift_warnings: {true|false}
DB writes: {N} task_spec_section_write OK; {K} unchanged.
Filesystem mirrors: {N} updated.
next=stage-authoring-chain-continue
```

### 8.2 Validate

```bash
npm run validate:all
```

Non-zero exit → escalate. (Replaces retired plan-author `validate:dead-project-specs` — Step 9 cleanup phase drops the dead-specs validator entirely; until then `validate:all` covers both surfaces.)

### 8.3 Idempotency on re-entry

- §Plan Digest already populated AND DB body matches AND lint PASS → record `skipped (already authored)`; no new `task_spec_section_write` call.
- §Plan Digest empty / sentinel `_pending — populated by /stage-authoring_` → fresh authoring pass per Phase 4.
- §Plan Digest populated but lint FAIL → re-author per Phase 4 (cap=1 per Task).

### 8.4 Next-step

Dispatcher (`/stage-file` per C8) receives this hand-off and continues to:

- **N≥2:** `Next: claude-personal "/ship-stage {ORCHESTRATOR_SPEC} Stage {STAGE_ID}"` (runs implement + verify + code-review + closeout per Step 8 ship-stage rewrite).
- **N=1:** `Next: claude-personal "/ship {ISSUE_ID}"`.

When invoked standalone (not via `/stage-file` chain): emit same handoff verbatim.

---

## Hard boundaries

- Do NOT write `## §Plan Author` section — surface retired per B6.
- Do NOT compile aggregate `docs/implementation/{slug}-stage-{STAGE_ID}-plan.md` — retired per D8.
- Do NOT write code, run verify, or flip Task status.
- Do NOT author specs outside target Stage.
- Do NOT regress to per-Task mode if tokens exceed threshold — split into ⌈N/2⌉ bulk sub-passes.
- Do NOT resolve picks — `plan_digest_scan_for_picks` is lint-only; leak = abort + handoff to operator.
- Do NOT call `domain-context-load` per Task — Phase 1 once per Stage.
- Do NOT skip the Scene Wiring step when triggered — wiring is a Stage deliverable per [`ia/rules/unity-scene-wiring.md`](../../rules/unity-scene-wiring.md); dropping the step lets Stages ship dead runtime paths (grid-asset-visual-registry 2.2 canonical incident).
- Do NOT fall back to filesystem-only write on DB unavailable — escalate; DB is source of truth post-Step-6.
- Do NOT commit — user decides.

---

## Escalation rules

| Trigger | Halt shape |
|---------|-----------|
| Task spec missing for filed `{ISSUE_ID}` (Phase 2) | `STOPPED — task spec missing for {ISSUE_ID}`; route caller to `/stage-file`. |
| Token-split sub-pass count > N (Phase 3) | Surface counter; user confirms split or aborts. |
| `plan_digest_lint` PASS=false twice for any Task (Phase 5) | `STOPPED — plan-digest lint critical twice for {ISSUE_ID}`; surface first 5 failures. |
| `mechanicalization_preflight_lint` PASS=false (Phase 6) outside advisory hatch | `STOPPED — mechanicalization_score: {overall}; failing_fields: [...]` for {ISSUE_ID}. |
| `task_spec_section_write` `task_not_found` / `section_anchor_ambiguous` (Phase 7) | Escalate; manual edit fallback. |
| `task_spec_section_write` `db_unavailable` (Phase 7) | Escalate; do NOT silently fall back to filesystem. |
| Phase 8.2 `validate:all` non-zero | Escalate post-loop; emit stderr. |

---

## Cross-references

- [`ia/rules/plan-digest-contract.md`](../../rules/plan-digest-contract.md) — 9-point rubric enforced by `plan_digest_lint`.
- [`ia/rules/mechanicalization-contract.md`](../../rules/mechanicalization-contract.md) — preflight contract.
- [`ia/rules/unity-scene-wiring.md`](../../rules/unity-scene-wiring.md) — Scene Wiring step trigger checklist.
- [`ia/templates/plan-digest-section.md`](../../templates/plan-digest-section.md) — §Plan Digest section template fragment.
- [`ia/skills/stage-file/SKILL.md`](../stage-file/SKILL.md) — upstream (writes N filed spec stubs + DB rows; tail calls this skill inline per C8).
- [`ia/skills/ship-stage/SKILL.md`](../ship-stage/SKILL.md) — downstream (Pass A implement + Pass B verify + closeout per Step 8 rewrite).
- [`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md) — shared Stage MCP bundle recipe (fallback when `lifecycle_stage_context` unavailable).
- [`ia/skills/_retired/plan-author/SKILL.md`](../_retired/plan-author/SKILL.md) — pre-merge upstream half (archived).
- [`ia/skills/_retired/plan-digest/SKILL.md`](../_retired/plan-digest/SKILL.md) — pre-merge downstream half (archived).
- [`docs/master-plan-foldering-refactor-design.md`](../../../docs/master-plan-foldering-refactor-design.md) §3.4 — design decisions B2 / B6 / C7 / C8 / D8.

---

## Changelog

### 2026-04-24 — Step 7 merge: stage-authoring single-skill (retire plan-author + plan-digest pair)

**Status:** applied

**Symptom:** Pre-merge two-skill chain (`plan-author` Opus bulk → `plan-digest` Opus bulk) wrote `§Plan Author` intermediate then mechanized into `§Plan Digest`, with `§Plan Author` ephemeral + dropped. Two Opus passes per Stage. Aggregate doc compiled at `docs/implementation/{slug}-stage-{STAGE_ID}-plan.md` per pass. DB-primary refactor (Step 7 of `docs/ia-dev-db-refactor-implementation.md`) requires direct stub → digest write via `task_spec_section_write` MCP + drops aggregate doc per design D8.

**Fix:** Collapsed pair into single skill per design B2 + C7. One Opus bulk pass authors §Plan Digest direct (no §Plan Author intermediate per B6). Persists per-Task body to DB via `task_spec_section_write` MCP (Step 4 mutation surface). Absorbs retired plan-author Phase 4 canonical-term fold (4a glossary + 4b retired-surface tombstone + 4c template-section allowlist + 4d cross-ref resolver) into the same bulk pass. Inherits retired plan-digest mechanical-step rules (invariant_touchpoints + validator_gate + STOP + MCP hints + Scene Wiring step). Self-lints via `plan_digest_lint` (cap=1 retry per Task). Mechanicalization preflight via `mechanicalization_preflight_lint` per Task. Drops aggregate `docs/implementation/{slug}-stage-{STAGE_ID}-plan.md` doc per design D8. Pre-merge skill bodies archived at `ia/skills/_retired/plan-author/SKILL.md` + `ia/skills/_retired/plan-digest/SKILL.md`.

**Rollout row:** ia-dev-db-refactor Step 7
