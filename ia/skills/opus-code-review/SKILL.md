---
name: opus-code-review
purpose: >-
  Per-Stage code review (Stage diff vs §Plan Digest acceptance + invariants + glossary). Three
  verdict branches: PASS / minor / critical. Critical fixes applied inline by reviewer (Edit /
  Write); no §Code Fix Plan tuples written.
audience: agent
loaded_by: "skill:opus-code-review"
slices_via: invariants_summary, glossary_lookup, glossary_discover
description: >-
  Reviewer skill. Runs once per Stage in `/ship-stage` Pass B (after verify-loop on cumulative
  Stage diff). Reads Stage diff vs combined §Plan Digest acceptance for all Tasks of the Stage,
  invariants subset, glossary anchors. Three verdict branches: (a) PASS → write §Code Review
  verdict to each Task spec via `task_spec_section_write`, no further action; (b) minor →
  fix-in-place inline (Edit / Write) OR open deferred issue, then write §Code Review; (c)
  critical → apply fixes inline (Edit / Write), re-enter verify-loop ONCE; second critical →
  halt with `STAGE_CODE_REVIEW_CRITICAL_TWICE`. Triggers: "/code-review {STAGE_ID}", "stage code
  review", "post-verify code review".
phases:
  - Load Stage diff + bundle
  - Read §Plan Digest per Task
  - Verdict branch
  - Inline fix (critical / fix-in-place minor)
  - Persist §Code Review per Task
  - Hand-off
triggers:
  - /code-review
  - stage code review
  - post-verify code review
  - opus code review
model: inherit
tools_role: custom
tools_extra: []
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
hard_boundaries: []
---

# Opus-code-review skill (per-Stage)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). **Self-review on your own branch** — [`agent-code-review-self.md`](../../rules/agent-code-review-self.md).

**Role:** Per-Stage reviewer in `/ship-stage` Pass B. Runs after `verify-loop` succeeds on the cumulative Stage diff. Reads diff vs combined acceptance from §Plan Digest of every Task in the Stage; emits one of three verdicts. Critical fixes applied **inline** via direct Edit / Write — `§Code Fix Plan` tuples are NOT written. Caller re-enters verify-loop + code-review at most once on critical.

DB is sole source of truth for task spec sections. Reads via `task_spec_section`; writes via `task_spec_section_write`. No filesystem spec read/write.

---

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `SLUG` | caller | Master plan slug (e.g. `blip`). |
| `STAGE_ID` | caller | Stage id within plan (e.g. `5.4`). |
| `STAGE_DIFF_ANCHOR` | caller | Commit SHA = `{FIRST_TASK_COMMIT_PARENT}` for Stage. Diff range: `git diff {STAGE_DIFF_ANCHOR}..HEAD` (Stage closeout commits do NOT yet exist at Pass B time). |
| `STAGE_MCP_BUNDLE` | caller | Pre-loaded `domain-context-load` payload from `ship-stage` Phase 2. REQUIRED — do NOT re-query. |
| `RE_ENTRY_COUNT` | caller | 0 on first pass; 1 on post-fix re-entry. ≥2 not allowed (caller halts). |

---

## Phase 1 — Load Stage diff + bundle

1. Resolve Stage tasks: `mcp__territory-ia__stage_bundle({slug: "{SLUG}", stage_id: "{STAGE_ID}"})` → returns `{stage, tasks}` with all Task ids + status.
2. Capture Stage diff: `git diff {STAGE_DIFF_ANCHOR}..HEAD` — full file list + content. If `STAGE_DIFF_ANCHOR` absent, derive from `git log --oneline` and the known Pass A Task commit messages (caller normally provides anchor).
3. Validate `STAGE_MCP_BUNDLE` present. Missing → halt with `STOPPED — STAGE_MCP_BUNDLE required`.
4. Load invariants subset relevant to changed files via `STAGE_MCP_BUNDLE.invariants` (no re-query).

---

## Phase 2 — Read §Plan Digest per Task

For each Task `{TASK_ID}` in `stage_bundle.tasks` (status ∈ {`implemented`, `verified`}):

1. `mcp__territory-ia__task_spec_section({task_id: "{TASK_ID}", section: "§Plan Digest"})` → relaxed shape (§Goal / §Acceptance / §Pending Decisions / §Implementer Latitude / §Work Items / §Test Blueprint / §Invariants & Gate) OR legacy shape (§Goal / §Acceptance / §Test Blueprint / §Examples / §Mechanical Steps). Detect by sub-heading presence. Literal `§` prefix required (see [`plan-digest-contract.md` §Section heading literal](../../rules/plan-digest-contract.md)).
2. Aggregate `{TASK_ID → §Plan Digest}` map. Combined acceptance = union of all per-Task §Acceptance rows.

Single bundle covers all N Tasks. Context overhead = O(1) per Stage via `STAGE_MCP_BUNDLE`.

---

## Phase 3 — Verdict branch

Run review against checks below over the full Stage diff. Collect findings by severity.

| Check | Severity |
|-------|----------|
| Combined §Acceptance rows (all Tasks) met by Stage diff | critical if not met |
| Digest body fully applied — relaxed shape: every §Work Items row has a matching diff entry; legacy shape: every §Mechanical Steps step executed (no step silently skipped) | critical if row/step missing |
| Invariants respected in changed C# (or N/A if tooling-only) | critical if violated |
| Glossary terms spelled canonically in changed docs | minor |
| No adjacent refactors beyond Stage scope | minor |
| Cross-ref links resolve | minor |
| Frontmatter `phases:` present on new SKILL.md files | minor |
| No new singletons; no `FindObjectOfType` in per-frame (C# only) | critical if violated |
| **Scene wiring** — Task whose §Plan Digest carries a **Scene Wiring** entry (legacy: dedicated mechanical step; relaxed: §Work Items row prefixed `(Scene Wiring)`) OR whose diff fires any trigger in [`ia/rules/unity-scene-wiring.md`](../../rules/unity-scene-wiring.md) (new runtime MonoBehaviour / new `[SerializeField]` / new StreamingAssets consumer / new prefab at scene boot / new scene-level `UnityEvent`) has a matching `Assets/Scenes/*.unity` edit in the Stage diff AND the evidence block (scene/parent/component/serialized_fields/unity_events/compile_check) was emitted by `spec-implementer` | **critical if trigger fired but no `.unity` / prefab edit, or evidence block absent** |

**Determine verdict:**
- Zero findings → **PASS**.
- Only minor findings → **minor**.
- Any critical finding → **critical**.

---

## Phase 4 — Inline fix (critical / fix-in-place minor)

**PASS branch:** skip Phase 4. Proceed to Phase 5.

**Minor branch:** when finding is fix-in-place (typo, glossary mis-spelling, broken link, missing frontmatter `phases:`): apply directly via Edit / Write. When finding requires a new BACKLOG issue (deferred refactor, scope creep flagged): note id placeholder in §Code Review payload — do NOT file the issue here.

**Critical branch:** apply each critical fix inline via direct Edit / Write. Do NOT write `§Code Fix Plan` tuples. Do NOT spawn a pair-tail. Fix scope = minimal diff to clear the critical finding. After all fixes applied:

1. Run `mcp__territory-ia__unity_compile()` (or `npm run unity:compile-check`) when C# touched. Compile failure → halt with `STOPPED — code-review fix broke compile`.
2. Return verdict `critical_fixes_applied` to caller. Caller (`ship-stage` Pass B) re-enters verify-loop + code-review ONCE. On `RE_ENTRY_COUNT >= 1` and another critical → halt with `STAGE_CODE_REVIEW_CRITICAL_TWICE`; do NOT apply a second round of fixes.

---

## Phase 5 — Persist §Code Review per Task

Write verdict body to each Task in the Stage via `task_spec_section_write`. Body shape varies per branch:

**PASS body:**

```
Verdict: **PASS**

Reviewed: Stage diff {STAGE_DIFF_ANCHOR}..HEAD ({n_files} files, {n_lines} lines).
Acceptance: all {N} rows met across {M} Tasks.
Invariants: no violations.
Glossary: canonical.
```

**Minor body:**

```
Verdict: **minor**

Minor findings:
- {finding}: {fix-in-place | deferred — file new issue}
- ...

No critical fixes; verify-loop not re-entered.
```

**Critical body:**

```
Verdict: **critical (fixed inline)**

Critical findings (all resolved by inline Edit / Write):
- {finding}: {one-line resolution}
- ...

Compile gate: PASS post-fix. Caller re-entered verify-loop + code-review (RE_ENTRY_COUNT={n}).
```

Per-Task call: `mcp__territory-ia__task_spec_section_write({task_id: "{TASK_ID}", section: "Code Review", body: "<branch body>"})`. DB sole persistence — no filesystem write.

---

## Phase 6 — Hand-off

Return single caveman block to caller:

```
opus-code-review {STAGE_ID}: verdict={PASS|minor|critical_fixes_applied}
Tasks reviewed: {N} ({task_id_list})
Findings: {n_critical} critical, {n_minor} minor
Inline fixes applied: {n_files_touched} files, {n_lines_changed} lines (critical branch only)
DB writes: {N} task_spec_section_write OK
next={proceed|reenter_verify_loop|halt_critical_twice}
```

Caller branches on `next`:
- `proceed` (PASS / minor) → Pass B continues to per-task `verified→done` flips + closeout.
- `reenter_verify_loop` (critical_fixes_applied) → caller re-runs verify-loop + this skill once.
- `halt_critical_twice` → caller emits `STAGE_CODE_REVIEW_CRITICAL_TWICE`; human review.

---

## Hard boundaries

- Do NOT read or write task spec body from filesystem — DB only via `task_spec_section` / `task_spec_section_write`.
- Do NOT write `§Code Fix Plan` tuples — fixes are applied inline via Edit / Write per ship-stage contract.
- Do NOT spawn `plan-applier` — pair-tail Mode code-fix is retired in this flow.
- Do NOT re-query `domain-context-load` — `STAGE_MCP_BUNDLE` is required input.
- Do NOT exceed one re-entry — `RE_ENTRY_COUNT >= 2` halts with `STAGE_CODE_REVIEW_CRITICAL_TWICE`.
- Do NOT alter Pass A Task commits — Stage uses single stage-end commit.
- Do NOT commit — caller (`ship-stage` Pass B Step 8) emits the single stage commit covering all fixes.
- Do NOT flip `task_status` — caller owns `verified→done` flips after Pass B clean.

---

## Cross-references

- [`ia/rules/unity-scene-wiring.md`](../../rules/unity-scene-wiring.md) — scene-wiring trigger list + evidence block; feeds Phase 3 critical check.
- [`ia/skills/verify-loop/SKILL.md`](../verify-loop/SKILL.md) — runs immediately before this skill in Pass B.
- [`ia/skills/ship-stage/SKILL.md`](../ship-stage/SKILL.md) — caller; owns single stage commit + status flips + closeout.
- [`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md) — shared Stage MCP bundle recipe.
- Glossary term **Opus code review** (`ia/specs/glossary.md`).
