---
purpose: "Two-pass chain: Pass 1 = per-Task implement + unity:compile-check fast-fail gate; Pass 2 = Stage-end bulk verify-loop (full Path A+B cumulative delta) + code-review (Stage diff) + audit + closeout. Step 1.6 resume gate skips Pass 1 when feat(id)/fix(id) commits already on branch. Approach B stateful chain subagent."
audience: agent
loaded_by: skill:ship-stage
slices_via: backlog_issue, router_for_task, spec_section, spec_sections, glossary_discover, glossary_lookup, invariants_summary
name: ship-stage
description: >
  Opus orchestrator. Drives every non-Done filed task row of one Stage X.Y through a
  two-pass chain. Pass 1 (per-Task): implement + unity:compile-check fast-fail gate +
  atomic Task-level commit. Pass 2 (Stage-end bulk): verify-loop full Path A+B on
  cumulative delta + code-review Stage diff (shared amortized context) + audit + closeout.
  Closeout (stage-closeout-planner → plan-applier Mode stage-closeout) is mandatory whenever
  upstream Pass 2 gates pass — do not emit SHIP_STAGE PASSED or defer /closeout to a follow-up
  session when verify + code-review + audit succeed. Step 1.6 resume: git scan for feat(id)/fix(id)
  skips finished Tasks; all satisfied → Pass 2 only. Opt-out --no-resume; --per-task-verify forces no resume.
  --per-task-verify flag preserves pre-TECH-519 N× per-Task verify-loop + code-review shape.
  MCP context loaded once via domain-context-load subskill; cached payload passed
  to per-task inner dispatches. Emits SHIP_STAGE {STAGE_ID}: PASSED or STOPPED.
  Triggers: "/ship-stage", "ship stage", "chain stage tasks", "ship all stage tasks".
model: inherit
phases:
  - "Parse stage"
  - "Stage tail detect (PASS2_ONLY)"
  - "Context load"
  - "Plan-author readiness gate"
  - "Resume gate"
  - "Pass 1 per-Task"
  - "Pass 2 Stage-end"
  - "Chain digest"
  - "Next-stage resolver"
---

# Ship-stage — chain dispatcher skill

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Canonical master-plan shape:** [`ia/projects/MASTER-PLAN-STRUCTURE.md`](../../projects/MASTER-PLAN-STRUCTURE.md) — authoritative. H3 `### Stage N.M` heading; 5-col Task table `| Task | Name | Issue | Status | Intent |` (Phase column retired); 2-level hierarchy Stage > Task (Step layer retired); Stage subsections `#### §Stage File Plan` / `#### §Plan Fix` / `#### §Stage Audit` / `#### §Stage Closeout Plan`.

**Related:** [`ship.md`](../../../.claude/commands/ship.md) (single-task chain — readiness → implement → verify-loop → code-review (fix loop cap=1) → audit; **no closeout** — Stage-scoped only) · [`verify-loop`](../verify-loop/SKILL.md) (`--skip-path-b` flag) · [`domain-context-load`](../domain-context-load/SKILL.md) (MCP cache subskill) · Stage-scoped closeout pair: [`stage-closeout-plan`](../stage-closeout-plan/SKILL.md) → [`plan-applier`](../plan-applier/SKILL.md) Mode stage-closeout (absorbs retired `project-stage-close` + `project-spec-close` per T7.14 / M6 collapse). Scene wiring: [`ia/rules/unity-scene-wiring.md`](../../rules/unity-scene-wiring.md) — Pass 1 Step 2.2 preflight + Pass 2 Step 3.2 code-review gate.

**Verification policy:** [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md).

## Normative — closeout is part of `PASSED`

When Pass 2 upstream gates succeed for this Stage (**verify-loop** `verdict: pass` when Step 3.1 runs; **code-review** not critical; **audit** completes), the chain **must** run **Step 3.5 Closeout** in the **same** `/ship-stage` invocation. Do **not**:

- Emit `SHIP_STAGE {STAGE_ID}: PASSED` after verify (or audit) only.
- Tell the operator to run standalone `/closeout` later unless a gate **failed** or human escalation applies.

`SHIP_STAGE {STAGE_ID}: PASSED` is valid **only** after `plan-applier` Mode **stage-closeout** completes successfully (per Step 3.5 gate). If closeout fails validators or tuple application → `STOPPED at closeout` (see Exit lines), not PASSED.

**Tooling-only / `--tooling-only` verify-loop:** still run Step 3.5 — archive/delete tuples apply to ia/backlog + `ia/projects/{ISSUE_ID}.md` the same way; no exemption from closeout on green path.

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `MASTER_PLAN_PATH` | User prompt | Repo-relative path to `*-master-plan.md` (e.g. `ia/projects/citystats-overhaul-master-plan.md`). |
| `STAGE_ID` | User prompt | Stage identifier as it appears in the master plan header (e.g. `Stage 1.1`). |
| `--per-task-verify` | Optional flag | **Rollback / legacy flag.** When set: Pass 2 verify-loop + code-review are SKIPPED; Pass 1 is promoted to full `verify-loop --skip-path-b` + `code-review` per Task (pre-TECH-519 shape). Audit + closeout remain Stage-scoped N=1 regardless. Use as safety valve for Stages too large for bulk Pass 2 review (e.g. N≥5, wide surface). **Also disables Step 1.6 resume skip** (unsafe — every Task needs fresh Pass 1 verify semantics). |
| `--no-resume` | Optional flag | When set: **disable Step 1.6** — every pending Task runs full Pass 1 (legacy behavior). Use for squash-only histories, forensic replay, or when Pass 1 commits used non-canonical messages. |

**Dispatch-shape agnostic:** identical behavior whether this skill is invoked as a Task-dispatched subagent (fresh context) or inline by an orchestrator (inherited context). Do not introduce subagent-only assumptions.

---

## Stage MCP bundle contract

Stage opener calls [`domain-context-load`](../domain-context-load/SKILL.md) once; returned payload `{glossary_anchors, router_domains, spec_sections, invariants}` kept in Stage scope. All Sonnet pair-tail invocations within the Stage read from that payload — no re-query of `glossary_discover`, `glossary_lookup`, `router_for_task`, `spec_sections`, or `invariants_summary` inside a Stage. The 5-tool recipe (`glossary_discover → glossary_lookup → router_for_task → spec_sections → invariants_summary`) is encapsulated entirely in `domain-context-load`; callers never inline it.

---

## Step 0 — Parse stage task table

**Algorithm (narrow regex, fails loud on schema drift):**

1. Read `{MASTER_PLAN_PATH}`.
2. Locate stage header: scan for a heading line matching `#### {STAGE_ID}` (any number of leading `#` followed by a space, then `{STAGE_ID}`). Accept `## Stage X.Y`, `### Stage X.Y`, `#### Stage X.Y` to be header-depth agnostic. Regex: `/^#{2,6}\s+Stage\s+X\.Y\b/` where X.Y comes from `STAGE_ID`.
3. Collect lines between that heading and the next heading of equal or lower depth.
4. Locate task table: find a Markdown table with header row containing columns `Issue` and `Status` (case-insensitive, any column order). Regex: `/\|\s*Issue\s*\|/i` on the header row.
5. **Schema drift guard:** only `Issue` + `Status` are required columns. Canonical master-plan schema (per [`ia/projects/MASTER-PLAN-STRUCTURE.md`](../../projects/MASTER-PLAN-STRUCTURE.md)) is the 5-column shape `Task | Name | Issue | Status | Intent` — Phase column retired 2026-04-24 (lifecycle-refactor); any lingering `Phase` column is advisory legacy drift. If `Issue` OR `Status` column not found within the stage block → emit `SHIP_STAGE {STAGE_ID}: STOPPED at parser — schema mismatch` + diff showing required columns `[Issue, Status]` (canonical 5-col `[Task, Name, Issue, Status, Intent]`) vs found column headers. Stop.
6. Extract rows: for each data row, parse `Issue` column (must match `/\*\*?(TECH|BUG|FEAT|ART|AUDIO)-\d+\*\*?/` or bare id) and `Status` column.
7. Filter: keep rows where `Status` is NOT `Done` / `archived` / `skipped` (case-insensitive). These are the **pending implementation tasks** (drive Pass 1 — same as “pending tasks” below).
8. Collect **`STAGE_FILED_IDS`**: every issue id appearing in a row whose Issue column is not `_pending_` (table order, de-dupe by first occurrence). Used for Pass 2 scope + resume anchor when Pass 1 is empty.
9. **Stage tail signal (machine, not Task rows):** **`STAGE_TAIL_INCOMPLETE`** iff `STAGE_FILED_IDS` is non-empty **and** at least one id has an **open backlog record** `ia/backlog/{ISSUE_ID}.yaml` on disk. Matches `validate:master-plan-status` **R6** when task rows are all Done-like but closeout never archived yaml. Optional corroboration: `ia/projects/{ISSUE_ID}.md` still exists — expect true until closeout deletes specs.
10. **Branch (replaces naive idle-only exit):**
    - If **pending implementation tasks** non-empty → continue chain (Pass 1 may run).
    - **Else** if **`STAGE_TAIL_INCOMPLETE`** → set **`PASS2_ONLY=1`**. Emit:
      ```
      SHIP_STAGE tail: Stage tail incomplete — open backlog: {ISSUE_IDs} — resuming Pass 2 (verify-loop → code-review → audit → closeout)
      ```
      Do **not** emit idle exit. Continue **Step 1** → **Step 1.5** (readiness uses **`STAGE_FILED_IDS`**) → **Step 1.6** `PASS2_ONLY` branch → **Step 3** (skip Step 2 entirely). This fixes “all table rows Done but Pass 2 never ran” without manual `/closeout` guesswork.
    - **Else** (pending empty **and** tail complete) → emit `SHIP_STAGE {STAGE_ID}: all tasks already Done. No work needed.` + next-stage resolver (Step 5).
11. **Idempotence:** **`PASS2_ONLY`** re-entry is **Stage-level idempotent**: Pass 1 skipped; Pass 2 steps may re-run; `verify-loop` / `plan-applier` Mode stage-closeout should tolerate repeat when work already landed (validators gate). Distinct from **Task-level** idempotence (feat/fix commits). Future: disk **chain journal** may record substeps ([TECH-493](../../projects/TECH-493.md)); until then, **open backlog yaml** is the durable tail incomplete signal.

**Parser fixtures (verify at authoring, not runtime):**

- Canonical shape post-lifecycle-refactor 2026-04-24: `### Stage X.Y` (H3) + 5-col schema `Task | Name | Issue | Status | Intent`.
- Legacy fixtures (pre-refactor) carried `#### Stage` (H4) + 6-col `Task | Name | Phase | Issue | Status | Intent`. Parser accepts both during migration window; `Issue` + `Status` are the only required columns.

Parser accepts `##`–`######` to be forward-compatible; only `Issue` + `Status` columns are required, other columns ignored.

---

## Step 1 — Context load (once per chain)

Run [`domain-context-load`](../domain-context-load/SKILL.md) subskill once for the stage domain:

```
keywords: derive from master plan title + stage objectives (English)
tooling_only_flag: <auto-detect per heuristic below; default false>
context_label: "{MASTER_PLAN_PATH} {STAGE_ID}"
```

**`tooling_only_flag` auto-detect heuristic (pre-context-load):**

Flip to `true` (skips `invariants_summary` — runtime-C# invariants irrelevant for tooling stages) when ANY of these hold:

- `MASTER_PLAN_PATH` matches `/mcp-lifecycle-tools|ia-infrastructure|tooling|bridge-environment|backlog-yaml-mcp/`.
- Master plan H1 contains bracket label `(IA Infrastructure)`, `(MCP)`, or `(Tooling)`.
- Stage block under `{STAGE_ID}` touches only `tools/mcp-ia-server/**`, `tools/scripts/**`, `ia/**`, `.claude/**`, `docs/**` (no `Assets/**/*.cs`).

Otherwise keep `false` (most runtime stages touch Unity C# and need invariants). Manual override via explicit prompt param still wins.

Store returned payload `{glossary_anchors, router_domains, spec_sections, invariants}` as `CHAIN_CONTEXT`. Pass to each per-task inner dispatch so kickoff / implementer / verify-loop don't re-query.

---

## Step 1.5 — §Plan Digest readiness gate (prerequisite; lazy-migration branch for legacy §Plan Author)

`/ship-stage` does NOT run `/author` or `/plan-review` internally — both fold into `/stage-file` dispatcher (F6 re-fold 2026-04-20). `/ship-stage` DOES auto-invoke `plan-digest` JIT for legacy specs whose `§Plan Author` is populated but `§Plan Digest` missing (lazy-migration branch per Q13 2026-04-22). Specs arriving at `/ship-stage` must carry populated `## §Plan Digest`; legacy Draft specs with `§Plan Author` only are upgraded on first re-entry.

**Readiness id list** (which specs to check):

| Situation | Ids to check |
|-----------|----------------|
| Pending implementation tasks non-empty | Issue ids from those rows only (legacy behavior). |
| **`PASS2_ONLY=1`** | All **`STAGE_FILED_IDS`** (specs usually still on disk until closeout). |
| Idle exit at Step 0 | Do not reach this gate. |

**Idempotent readiness check:** for each id in the readiness id list, read `ia/projects/{ISSUE_ID}*.md` and locate `## §Plan Digest`. Treat a spec as **digested** when ALL of these hold:

1. `## §Plan Digest` heading exists.
2. No line inside the block (until next `## ` heading at same/higher level) matches `_pending` case-insensitively.
3. Sub-headings `### §Goal`, `### §Acceptance`, `### §Mechanical Steps` exist with non-whitespace body content.

**If ALL specs digested:** continue to Step 2 (Pass 1) **or** Step 1.6 / 3 when **`PASS2_ONLY`**.

**If ANY spec missing §Plan Digest BUT populated §Plan Author:** auto-invoke `plan-digest` JIT (lazy-migration branch — Q13 2026-04-22). Emit ONE-TIME session warning: `LAZY_MIGRATION: §Plan Author → §Plan Digest upgrade on re-entry for {ISSUE_ID_LIST}`. Dispatch `plan-digest` as subagent on the Stage; wait for completion + lint PASS; re-run the readiness check.

**If ANY spec has neither §Plan Digest NOR §Plan Author:** stop chain. Emit:

```
SHIP_STAGE {STAGE_ID}: STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}
Next: claude-personal "/plan-digest {MASTER_PLAN_PATH} Stage {STAGE_ID}"
```

**Rationale:** `/stage-file` chain now ends with `plan-digest` + `plan-review` (plan-digest insertion 2026-04-22) — specs arrive with populated `§Plan Digest`. Legacy Draft specs (filed before plan-digest) carry `§Plan Author` only; lazy-migration auto-upgrades them on first `/ship-stage` entry. Branch retires when the last Draft is re-entered.

---

## Step 1.6 — Resume gate (partial Pass 1 / jump to Pass 2)

**Problem:** Re-invoking `/ship-stage` after Pass 1 commits landed but Pass 2 never ran MUST NOT re-dispatch `spec-implementer` for those Tasks. Partial Pass 1 (some Tasks committed, next not) MUST continue from the first Task without a Pass 1 anchor.

**Disable resume (legacy path):** If user prompt contains `--no-resume` → set `RESUME_DISABLED=1` and **skip § B below** (treat every pending Task as Pass 1 required). **If `--per-task-verify` is set → set `RESUME_DISABLED=1`** — resume skip is unsafe when Pass 2 verify is offloaded to per-Task loops.

**Exception — `PASS2_ONLY`:** When Step 0 set **`PASS2_ONLY=1`**, **always run § A** first, even if `RESUME_DISABLED=1`. There is no Pass 1 work to expand; `--no-resume` does not apply. If `--per-task-verify` is set with **`PASS2_ONLY`**, emit warning: **Stage tail completion requires bulk Step 3–3.5** — ignore `--per-task-verify` for Step 3 onward (closeout must run).

### § A — `PASS2_ONLY` (Stage tail incomplete, open backlog yaml)

Run **only when** Step 0 set **`PASS2_ONLY=1`** (all table rows Done-like, pending implementation empty, **`STAGE_TAIL_INCOMPLETE`**).

1. Let **`SCAN_IDS` = `STAGE_FILED_IDS`** (not `PENDING_ORDERED`, which is empty).
2. **Scan git (bounded):** same as § B §2.
3. For each id in **`SCAN_IDS`**, compute `pass1_present(id)` (same prefix rule as § B §3).
4. **Emit:**
   ```
   SHIP_STAGE resume: PASS2_ONLY — Pass 1 commit scan — satisfied: [{ids}] ; missing: [{ids or none}]
   ```
5. **Resolve `FIRST_TASK_COMMIT_PARENT`** using **`SCAN_IDS`** in place of `PENDING_ORDERED` in § B §5.
6. **Branch:**
   - **Any** `pass1_present` false → `SHIP_STAGE {STAGE_ID}: STOPPED — PASS2_ONLY: missing feat({ISSUE_ID}): / fix({ISSUE_ID}): on scan — repair git subjects or table`.
   - **All** true → emit `SHIP_STAGE resume: PASS2_ONLY — entering Pass 2` → **jump Step 3** (skip Step 2). **Stop Step 1.6** (do not run § B).

### § B — Standard resume (`RESUME_DISABLED=0` and not `PASS2_ONLY`)

**When `RESUME_DISABLED=1` and not `PASS2_ONLY`:** skip § A and § B → go directly to **Step 2** (full Pass 1 for every pending row).

**When `RESUME_DISABLED=0` and not `PASS2_ONLY`:**

1. Let `PENDING_ORDERED` = issue ids from Step 0 pending implementation rows (**table order**).

2. **Scan git (bounded):** `git log --first-parent -400 --format='%H %ct %s' HEAD` (repo root). If history shorter than 400, scan full reachable ancestry on first-parent chain.

3. **Pass 1 commit present:** For each `ISSUE_ID` in `PENDING_ORDERED`, `pass1_present(id)` = true iff **any** scanned subject line matches **exact** prefix (case-sensitive on id):
   - `feat({ISSUE_ID}):` OR `fix({ISSUE_ID}):`  
   Canonical ship-stage commit is `feat({ISSUE_ID}):` (Step 2.3); `fix({ISSUE_ID}):` allowed for hotfix follow-ups. **No** bare substring match on body — avoids false positives.

4. **Emit (always):**

   ```
   SHIP_STAGE resume: Pass 1 commit scan — satisfied: [{ids or none}] ; missing: [{ids or none}]
   ```

5. **Resolve `FIRST_TASK_COMMIT_PARENT` for Step 3.1** (used whenever ≥1 `pass1_present` is true after this run's classification — including full Pass 1 that adds new commits, recompute after Pass 1 ends; see Step 2 closing note):
   - Collect all commits from the scan whose subject matches `^(feat|fix)\(({ISSUE_ID})\):` for **any** `ISSUE_ID` in `PENDING_ORDERED`.
   - Let `OLDEST` = commit with **minimum** `%ct` among that set (tie-break: lexicographically smallest `%H`).
   - `FIRST_TASK_COMMIT_PARENT = OLDEST^` (first parent). If set is empty but all `pass1_present` true → **STOPPED — resume anchor missing** (inconsistent git vs table; human repair). If set empty and some missing → anchor filled after first new Pass 1 commit in this invocation (Step 2).

6. **Branch:**
   - **All `pass1_present` true** → **Skip Step 2 entirely.** Emit:

     ```
     SHIP_STAGE resume: all pending Tasks have Pass 1 commits on HEAD — entering Pass 2
     ```

     Jump to **Step 3**. Carry `FIRST_TASK_COMMIT_PARENT` from §5.

   - **Some true, some false** → **Partial Pass 1.** Enter Step 2 loop; for each Task, if `pass1_present(ISSUE_ID)` → **skip Steps 2.1–2.3** (and skip 2.4 — `--per-task-verify` already forced `RESUME_DISABLED`). Still run Step 2.5 journal with `pass1_resume_skipped: true`. Still run Step 2.6.

   - **All false** → **Full Pass 1** (unchanged). `FIRST_TASK_COMMIT_PARENT` assigned after first Task commit (parent of first new feat/fix line for this stage).

7. **Dirty worktree:** If `git status --porcelain` is non-empty, emit **warning** in resume summary — do not auto-stop; human owns merge/rebase state.

**Pass 2 sub-step granularity:** Step 3 remains **atomic** (3.1→3.5 in one invocation). If the operator manually ran verify-loop only, re-running Step 3.1 is redundant but correct; finer sub-step resume stays deferred to disk journal work ([TECH-493](../../projects/TECH-493.md) / Open Questions).

---

## Step 2 — Pass 1: per-Task loop (sequential, fail-fast)

**Entry:** If Step 1.6 jumped to Pass 2 (all `pass1_present`) → **do not enter Step 2**; Step 3 already next.

For each pending task row in order (index `i`, total `N`):

```
CURRENT_TASK = task_rows[i]
ISSUE_ID = CURRENT_TASK.issue_id
```

**Resume skip:** If Step 1.6 marked `pass1_present(ISSUE_ID)` → skip **Step 2.1 Implement**, **2.2 Compile gate**, **2.3 Commit**, **2.4** (n/a unless `--per-task-verify` with resume disabled). Jump to **Step 2.5** with `pass1_resume_skipped: true` in journal entry, then **2.6**.

### Step 2.1 — Implement

Dispatch `spec-implementer` subagent (Sonnet):

> Mission: Execute `ia/projects/{ISSUE_ID}*.md` §Plan Digest (§Mechanical Steps) end-to-end, step by step. Pre-loaded context: {CHAIN_CONTEXT}. §Plan Digest is the canonical plan — §Plan Author no longer present post-2026-04-22. End with `IMPLEMENT_DONE` or `IMPLEMENT_FAILED: {reason}`.

**Gate:** final output must contain `IMPLEMENT_DONE`. `IMPLEMENT_FAILED` → stop, emit STOPPED line + partial chain digest.

### Step 2.2 — Compile gate + scene-wiring preflight

Run `npm run unity:compile-check` (Path: repo root, ~15 s). Non-zero exit = compile failure.

**Scene-wiring preflight (additive, runs in same step):** if §Plan Digest for this Task carries a **Scene Wiring** mechanical step, confirm the Task diff includes an edit to `Assets/Scenes/*.unity` (or adds a prefab under `Assets/Prefabs/**`). Missing scene edit under a wiring trigger = Pass 1 failure:

```
STOPPED at {ISSUE_ID} — scene_wiring: §Plan Digest Scene Wiring step fired but no Assets/Scenes/*.unity edit in Task diff
```

Contract source: [`ia/rules/unity-scene-wiring.md`](../../rules/unity-scene-wiring.md). `spec-implementer` step 10 owns the wiring execution; this preflight is the ship-stage backstop.

**On failure:** emit:

```
STOPPED at {ISSUE_ID} — compile_gate: {reason}
```

Then emit partial chain digest (Step 4 shape with `tasks_stopped_at: "{ISSUE_ID}"`) listing:
- `tasks_completed`: issue ids of Tasks that passed Pass 1 before this Task.
- `uncommitted_tail`: this Task (implement done; commit NOT made — stop before commit on compile failure).
- `unstarted`: remaining Task ids after this Task.

Halt chain. `Next: claude-personal "/ship {ISSUE_ID}"` after user fixes compile error.

**On success:** continue.

### Step 2.3 — Atomic Task-level commit

Commit all changes for this Task as a single atomic commit. Message format:

```
feat({ISSUE_ID}): {short description from spec §1}

Pass 1 compile gate: passed
```

This commit is the bisection anchor for the Task.

### Step 2.4 — Per-Task verify-loop + code-review (--per-task-verify flag only)

**SKIP this step UNLESS `--per-task-verify` flag is set.** When flag set, run the legacy per-Task shape:

Dispatch `verify-loop` subagent (Sonnet) with `--skip-path-b` flag:

> Mission: Run verify-loop for {ISSUE_ID} with `--skip-path-b`. Path A compile gate runs; Path B skipped. JSON verdict `path_b: skipped_batched`. End with JSON Verification block where `verdict` is `pass`, `fail`, or `escalated`.

**Gate:** `verdict` must be `"pass"`. Failure → stop, emit STOPPED digest.

Then dispatch `opus-code-reviewer` subagent (Opus):

> Mission: Run opus-code-review for {ISSUE_ID}. STAGE_MCP_BUNDLE: {CHAIN_CONTEXT}. Emit verdict (PASS / minor / critical). On critical: write §Code Fix Plan; return `{verdict: "critical"}`.

Verdict `critical` → emit STOPPED digest (code-review gate); verdict `PASS` / `minor` → continue.

### Step 2.5 — Journal accumulation (Pass 1 entry)

After successful Step 2.2 (or Step 2.4 when flag set), append to `CHAIN_JOURNAL`:

```json
{
  "task_id": "{ISSUE_ID}",
  "pass1_compile_gate": "passed",
  "pass1_resume_skipped": false,
  "lessons": [],
  "decisions": [],
  "verify_iterations": 0
}
```

Set `pass1_resume_skipped: true` when Step 1.6 skipped Pass 1 for this Task (commit already on branch).

Lessons + decisions updated from closeout digest in Step 3.5 below.

### Step 2.6 — Re-read master plan

Re-read `{MASTER_PLAN_PATH}` to confirm task row status after commit. Continue to next task.

**After Step 2 loop completes (any mix of fresh + resume-skipped Tasks):** Recompute `FIRST_TASK_COMMIT_PARENT` if not already fixed: among all commits on `git log --first-parent -400` matching `^(feat|fix)\(({ISSUE_ID})\):` for any `ISSUE_ID` in `PENDING_ORDERED`, take **oldest** by `%ct` → parent = Step 3.1 anchor. Ensures partial resume + new Task commits share one cumulative diff base.

---

## Step 3 — Pass 2: Stage-end bulk (runs ONCE after all Tasks pass Pass 1)

**Order is fixed when each step applies:** 3.1 (verify) → 3.2 (code-review, unless skipped) → 3.4 (audit) → **3.5 (closeout)**. **Step 3.5 is not optional** after successful upstream steps — see **Normative — closeout is part of `PASSED`** above.

**SKIP Pass 2 verify-loop + code-review when `--per-task-verify` flag is set.** Jump directly to Step 3.4 (audit). **Still run Step 3.5 closeout** after Step 3.4 when audit completes.

### Step 3.1 — Verify-loop on cumulative Stage delta

Run `verify-loop` (full Path A+B, no `--skip-path-b`) on cumulative Stage delta:

**Cumulative delta anchor:** `git diff {FIRST_TASK_COMMIT_PARENT}..HEAD` — where `{FIRST_TASK_COMMIT_PARENT}` = the commit SHA immediately **before** the **oldest** Pass 1 Task commit for this Stage's pending ids (see Step 1.6 §5 and Step 2 closing recomputation). Equivalently: first parent of the earliest `feat({ISSUE_ID}):` / `fix({ISSUE_ID}):` commit among `PENDING_ORDERED` on the first-parent chain. **EXCLUDE Stage closeout commits** (closeout runs after Pass 2; closeout commits not yet on HEAD at this point — so the anchor is naturally correct).

> Mission: Run full verify-loop (Path A + Path B) on cumulative stage delta. Issue context: last closed {ISSUE_ID} (for backlog context). Changed areas = all files touched across all Pass 1 Task commits. End with JSON Verification block where `verdict` is `pass`, `fail`, or `escalated`.

**Gate:** `verdict` must be `"pass"`.

**STAGE_VERIFY_FAIL handling:** if Pass 2 verify-loop fails:
- All Tasks committed in Pass 1 — no rollback.
- Emit `SHIP_STAGE {STAGE_ID}: STAGE_VERIFY_FAIL` + chain digest with `stage_verify: failed` field + `escalation` object mirroring inner verify-loop `gap_reason` taxonomy (see `ia/skills/verify-loop/SKILL.md` § Step 7, `docs/agent-led-verification-policy.md` § Escalation taxonomy).
- `gap_reason` REQUIRED — pick `bridge_kind_missing` over `human_judgment_required` whenever a missing `unity_bridge_command` kind could close the loop.
- No automatic retry.

### Step 3.2 — Code-review on Stage-level diff

Dispatch `opus-code-reviewer` subagent (Opus) with Stage diff + shared context:

> Mission: Run opus-code-review on Stage-level diff (cumulative delta: same anchor as Step 3.1). STAGE_MCP_BUNDLE: {CHAIN_CONTEXT} — shared spec/invariant/glossary context cached from Phase 1 (do NOT re-query domain-context-load). All N §Plan Digest sections from task specs for `{MASTER_PLAN_PATH}` are the acceptance reference. Include scene-wiring check per [`ia/rules/unity-scene-wiring.md`](../../rules/unity-scene-wiring.md): any Task whose §Plan Digest carries **Scene Wiring** must have a matching `Assets/Scenes/*.unity` edit in the cumulative diff AND an evidence block in §8 Acceptance. Missing wiring under a fired trigger = critical verdict. Emit verdict (PASS / minor / critical). On critical: write §Code Fix Plan tuples targeting the appropriate spec files.

**Verdict PASS / minor:** continue to Step 3.4.

**Verdict critical (first time):**
1. Run `plan-applier` Mode code-fix Sonnet on `§Code Fix Plan` tuples.
2. Re-enter Step 3.1 verify-loop (one re-entry — cap = 1).
3. Run Step 3.2 code-review again.
4. Second critical verdict → exit `STAGE_CODE_REVIEW_CRITICAL_TWICE` + chain digest. Halt. Human review required.

### Step 3.3 — (Reserved)

No additional step between code-review and audit.

### Step 3.4 — Audit

Dispatch `opus-auditor` subagent (Opus) — Stage-scoped (unchanged):

> Mission: Run opus-audit Stage 1×N for Stage {STAGE_ID}. Issue ids: {all Task ids in Stage}. STAGE_MCP_BUNDLE: {CHAIN_CONTEXT}. Return audit report.

### Step 3.5 — Closeout (mandatory on green path)

Dispatch `stage-closeout-planner` → `plan-applier` Mode **stage-closeout** (Opus pair-head → Sonnet pair-tail) — Stage-scoped:

> Mission: Run Stage-scoped closeout for Stage {STAGE_ID} in {MASTER_PLAN_PATH}. All Task rows. Migrate lessons → delete specs → archive BACKLOG rows. Return full `project_spec_closeout_digest` JSON payload (including `lessons_migrated[]` and `decisions[]` per task) so chain journal can aggregate.

**Mandatory:** Execute whenever Step 3.1 (when not skipped) + Step 3.2/3.3 + Step 3.4 succeeded — **do not** end the chain with PASSED without this step. Destructive-op confirmation follows [`stage-closeout-plan`](../stage-closeout-plan/SKILL.md) / [`plan-applier`](../plan-applier/SKILL.md) Mode stage-closeout (human checkpoint if the pair-head requires it).

**Gate:** closeout digest JSON `validate_dead_specs_post.exit_code` == 0 (and per-mode validators per `plan-applier`). Failure → `SHIP_STAGE {STAGE_ID}: STOPPED at closeout — {reason}` + partial digest — **not** PASSED.

After closeout, update `CHAIN_JOURNAL` entries with lessons + decisions from closeout digest.

---

---

## Step 4 — Chain-level stage digest

Emit one chain-level stage digest at chain end (success or STAGE_VERIFY_FAIL). Distinct from per-spec `project-stage-close` which already fired inside each `spec-implementer`.

**Format:** mirrors `.claude/output-styles/closeout-digest.md` (JSON header + caveman summary) with additional `chain:` block.

```json
{
  "chain_stage_digest": true,
  "master_plan": "{MASTER_PLAN_PATH}",
  "stage_id": "{STAGE_ID}",
  "tasks_shipped": ["TECH-xxx", "TECH-yyy"],
  "tasks_stopped_at": null,
  "stage_verify": "passed|failed|skipped",
  "next_handoff": {
    "case": "filed|pending|skeleton|umbrella-done",
    "command": "/ship-stage|/stage-file|/stage-decompose|/closeout",
    "args": "ia/projects/{slug}-master-plan.md Stage X.Y",
    "shell": "claude-personal \"/ship-stage ia/projects/{slug}-master-plan.md Stage X.Y\""
  },
  "chain": {
    "tasks": [
      {
        "task_id": "TECH-xxx",
        "lessons": ["lesson1"],
        "decisions": ["decision1"],
        "verify_iterations": 0
      }
    ],
    "aggregate_lessons": ["..."],
    "aggregate_decisions": ["..."],
    "verify_iterations_total": 0
  }
}
```

`next_handoff.case` mirrors Step 5 resolver cases exactly — downstream drivers (`release-rollout`, dashboards) pick up the structured field without re-parsing caveman prose. On STOPPED / STAGE_VERIFY_FAIL, `next_handoff.case` is `"stopped"` or `"stage_verify_fail"` respectively and `command` / `args` reference the fix path (`/ship {ISSUE_ID}` or human-review directive).

Caveman summary follows JSON: tasks shipped, any stopped/failed, stage-level verify outcome, aggregate lesson count, next step.

---

## Step 5 — Next-stage resolver

Re-read `{MASTER_PLAN_PATH}` post-close. Scan for next stage after `{STAGE_ID}`:

**4 cases (in priority order):**

1. **Next filed stage** — next `### Stage X.Y` heading (H3 canonical; accept H4 legacy drift) where task table has ≥1 row with `Status != Done/archived/skipped` AND issue ids are real (not `_pending_`):
   → `Next: claude-personal "/ship-stage {MASTER_PLAN_PATH} Stage X.Y"`

2. **Next pending stage** — next `### Stage X.Y` heading where task table rows have `_pending_` issue ids (tasks not yet filed):
   → `Next: claude-personal "/stage-file {MASTER_PLAN_PATH} Stage X.Y"`

3. **Next skeleton stage** — next `### Stage X.Y` heading with no Task table populated (skeleton body per [`MASTER-PLAN-STRUCTURE.md`](../../projects/MASTER-PLAN-STRUCTURE.md)):
   → `Next: claude-personal "/stage-decompose {MASTER_PLAN_PATH} Stage X.Y"` (2-level hierarchy; Step layer retired 2026-04-24).

4. **Umbrella done** — no more stages in any state:
   → `Next: claude-personal "/closeout {UMBRELLA_ISSUE_ID}"` (if identifiable from master plan header) OR print `All stages done — umbrella close pending.`

---

## Exit lines

- **Success:** `SHIP_STAGE {STAGE_ID}: PASSED` + chain digest + `Next:` handoff — **only** after **Step 3.5 closeout** completed successfully (validators green). Never PASSED immediately after Step 3.1 / 3.4 alone.
- **Readiness gate fail:** `SHIP_STAGE {STAGE_ID}: STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}` + `Next: claude-personal "/plan-digest {MASTER_PLAN_PATH} Stage {STAGE_ID}"`.
- **Pass 1 compile failure:** `STOPPED at {ISSUE_ID} — compile_gate: {reason}` + partial chain digest (tasks-completed + uncommitted tail + unstarted list) + `Next: claude-personal "/ship {ISSUE_ID}"` after fix.
- **Pass 1 scene-wiring preflight failure:** `STOPPED at {ISSUE_ID} — scene_wiring: §Plan Digest Scene Wiring step fired but no Assets/Scenes/*.unity edit in Task diff` + partial chain digest + `Next: claude-personal "/ship {ISSUE_ID}"` after agent wires the scene per [`ia/rules/unity-scene-wiring.md`](../../rules/unity-scene-wiring.md).
- **Pass 1 implement failure (--per-task-verify only):** `SHIP_STAGE {STAGE_ID}: STOPPED at {ISSUE_ID} — implement: {reason}` + partial chain digest + `Next: claude-personal "/ship {ISSUE_ID}"` after fix.
- **Pass 2 verify failure:** `SHIP_STAGE {STAGE_ID}: STAGE_VERIFY_FAIL` + chain digest with `stage_verify: failed` + human review directive.
- **Pass 2 code-review critical twice:** `STAGE_CODE_REVIEW_CRITICAL_TWICE` + chain digest + human review required (structural issue).
- **Pass 2 closeout failure:** `SHIP_STAGE {STAGE_ID}: STOPPED at closeout — {reason}` + chain digest + `Next:` repair tuples or re-run `plan-applier` Mode stage-closeout after fix — do **not** emit PASSED.
- **Parser error:** `SHIP_STAGE {STAGE_ID}: STOPPED at parser — schema mismatch` + expected-vs-found column diff.
- **Resume anchor missing:** `SHIP_STAGE {STAGE_ID}: STOPPED — resume: pass1_present for all pending ids but no matching feat(id)/fix(id) commits on scan` + human repair (squash without id in subject → use `--no-resume` or amend message).

---

## Hard boundaries

- Sequential task dispatch only — tasks share files + invariants; no parallel.
- **Resume (Step 1.6)** is default-on when `--no-resume` absent and `--per-task-verify` absent. Do NOT re-implement Tasks that already have `feat({ISSUE_ID}):` / `fix({ISSUE_ID}):` on the scanned first-parent chain.
- Stop on first Pass 1 gate failure (compile or implement); do NOT continue to next task.
- Do NOT rollback committed Pass 1 Tasks on STAGE_VERIFY_FAIL or STAGE_CODE_REVIEW_CRITICAL_TWICE — commits already landed; emit digest + human directive only.
- `STAGE_CODE_REVIEW_CRITICAL` re-entry cap = 1 — second critical → `STAGE_CODE_REVIEW_CRITICAL_TWICE`; do NOT re-enter a third time.
- Pass 2 cumulative delta anchor: first Task-commit parent → Stage-end HEAD, EXCLUDING closeout commits.
- Stage-scoped closeout (`stage-closeout-plan` → `plan-applier` Mode stage-closeout) fires ONCE after Pass 2 audit (and skipped steps as applicable) when upstream gates pass — do NOT call per Task; do NOT skip; do NOT emit PASSED without it; do NOT defer to “run `/closeout` later” on green path.
- Chain-level stage digest is a NEW scope distinct from plan-applier Mode stage-closeout per-task digest aggregation.
- `domain-context-load` fires ONCE at chain start (Step 1); do NOT re-call per task.
- `plan-author` + `plan-review` do NOT run inside `/ship-stage` — both fold into `/stage-file` dispatcher. Step 1.5 is a readiness gate; non-digested AND non-authored → STOPPED + `/plan-digest` handoff. Do NOT dispatch `plan-author` or `plan-reviewer` from this skill. **Exception (lazy-migration, Q13 2026-04-22):** when a spec has populated `§Plan Author` but missing `§Plan Digest`, auto-invoke `plan-digest` JIT with a one-time session warning — this is the only subagent ship-stage dispatches outside Pass 1 / Pass 2.
- **Stage tail (`PASS2_ONLY`):** open `ia/backlog/{ISSUE_ID}.yaml` for any Stage filed id while table rows are Done ⇒ **not** idle exit; run Pass 2 through closeout. Aligns with `validate:master-plan-status` **R6**.
- Do NOT exceed `/ship` single-task dispatch shape for inner stages — each dispatches the canonical subagent.

---

## Placeholders

| Key | Default / meaning |
|-----|-------------------|
| `{MASTER_PLAN_PATH}` | Repo-relative path to master plan (e.g. `ia/projects/citystats-overhaul-master-plan.md`) |
| `{STAGE_ID}` | Stage identifier matching master plan header (e.g. `Stage 1.1`) |
| `{ISSUE_ID}` | Active task BACKLOG id (BUG-/FEAT-/TECH-/ART-/AUDIO-) |
| `{CHAIN_CONTEXT}` | `domain-context-load` payload `{glossary_anchors, router_domains, spec_sections, invariants}` |
| `{CHAIN_JOURNAL}` | In-process accumulator list of `{task_id, pass1_resume_skipped?, lessons[], decisions[], verify_iterations}` |
| `{FIRST_TASK_COMMIT_PARENT}` | SHA = `OLDEST_PASS1_COMMIT^` for pending ids (Step 1.6 / Step 2 tail); feeds Step 3.1 `git diff` anchor |

---

## Open Questions

- Crash-survivable `{CHAIN_JOURNAL}` (disk-persisted + resume on re-invocation) — tracked by [TECH-493](../../projects/TECH-493.md). Implementation deferred to that issue's implementer; this skill currently treats `{CHAIN_JOURNAL}` as in-process only.
- **Pass 2 sub-step resume** (verify-loop done, audit not) — not implemented; Step 3 re-runs 3.1–3.5 as a unit. Low cost for tooling stages; revisit if bridge-heavy Stages need mid-Pass-2 resume.

---

## Changelog

### 2026-04-24 — Canonical master-plan shape alignment (H3 Stage + 5-col table + Step layer retired)

**Status:** applied

**Symptom:** Step 0 §5 referenced 6-column superset `Task | Name | Phase | Issue | Status | Intent`; Step 5 case 3 suggested `/stage-decompose {MASTER_PLAN_PATH} Step N` — both retired by lifecycle-refactor 2026-04-24 (Phase column dropped; Step layer dropped; 2-level Stage > Task hierarchy).

**Fix:** Step 0 §5 schema guard now cites canonical 5-col `Task | Name | Issue | Status | Intent` per [`MASTER-PLAN-STRUCTURE.md`](../../projects/MASTER-PLAN-STRUCTURE.md); Phase column marked advisory-legacy. Step 5 case 3 emits `/stage-decompose {MASTER_PLAN_PATH} Stage X.Y` (Step layer retired). Parser fixtures updated to canonical shape; legacy H4 + 6-col noted as migration-window drift. Canonical-shape reference paragraph added at top. Parser remains forward-compatible (`##`–`######`, Issue + Status required only).

**Rollout row:** lifecycle-refactor-2026-04-24

---

### 2026-04-20 — Stage tail detect (`PASS2_ONLY`) + R6 alignment

**Status:** applied

**Symptom:** All task rows `Done` in master-plan table but Pass 2 (verify → audit → closeout) never ran; `/ship-stage` took idle exit; backlog yaml still open.

**Fix:** Step 0 **`STAGE_TAIL_INCOMPLETE`** (open `ia/backlog/{id}.yaml` for filed ids) + **`PASS2_ONLY`** branch → Step 1.6 § A → Pass 2 only. Validator **`validate:master-plan-status` R6** flags same drift. Glossary **Stage tail (open / incomplete)**. [`stage-file`](../stage-file/SKILL.md) No-op notes R6 before downstream filing.

---

### 2026-04-20 — Closeout mandatory on green Pass 2 path

**Status:** applied

**Symptom:** Operators or agents treated Stage closeout as optional after verify/audit passed, emitting PASSED or hand-waving to “run `/closeout` later.”

**Fix:** Added **Normative — closeout is part of `PASSED`**, tightened Step 3 / Step 3.5, Exit lines, and Hard boundaries: `SHIP_STAGE … PASSED` only after successful `plan-applier` Mode stage-closeout; new exit `STOPPED at closeout`. Tooling-only stages still close out in-band.

**Rollout row:** (session — ship-stage closeout normative)

---

### 2026-04-20 — Step 1.6 resume gate (partial Pass 1 + jump to Pass 2)

**Status:** applied

**Symptom:** Re-running `/ship-stage` after Pass 1 atomic commits but before Pass 2 re-dispatched `spec-implementer` for every Task still marked non-Done in the master plan table.

**Fix:** Step 1.6 scans `git log --first-parent -400` for `feat({ISSUE_ID}):` / `fix({ISSUE_ID}):` per pending Task; skips Pass 1 for satisfied ids; skips entire Step 2 when all satisfied; resolves `FIRST_TASK_COMMIT_PARENT` for cumulative Stage diff. Opt-out: `--no-resume`; forced off when `--per-task-verify` set.

**Rollout row:** (session follow-up — ship-stage resume)

---

### 2026-04-20 — Revert F6 fold in ship-stage; fold moved to /stage-file (F6 re-fold)

**Status:** applied

**Symptom:** F6 fold mis-placed `/author` + `/plan-review` inside `/ship-stage` chain. User intent: collapse Stage-entry friction ("3 commands across 2 CLI sessions") into ONE `/stage-file` command, not shift work into `/ship-stage`. Re-fold target: `/stage-file` dispatcher (stage-file-planner → stage-file-applier → plan-author → plan-reviewer → plan-fix-applier → STOP → handoff to `/ship-stage`).

**Fix:** Reverted Step 1.5 back to readiness gate (prerequisite §Plan Author populated check). Deleted Step 1.7 (plan-review pair dispatch). Removed exit lines `STOPPED at plan-author — {reason}` and `STOPPED at plan-review — STAGE_PLAN_REVIEW_CRITICAL_TWICE`. Restored exit line `STOPPED — prerequisite: §Plan Author not populated`. Removed `plan-author` + `plan-review` from phases array. Added hard boundary explicitly forbidding plan-author / plan-reviewer dispatch from this skill. Ship-stage stays self-contained for implement/verify/code-review/audit/closeout; author + plan-review live in `/stage-file`.

**Rollout row:** m8-retrospective

**Tracker aggregator:** [`ia/projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator`](../../projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator)

---

### 2026-04-20 — Plan-author readiness gate (reinstated after F6 re-fold)

**Status:** applied

**Symptom:** After `/stage-file`, specs retain `_pending_` §Plan Author stubs; `/ship-stage` jumped to implement with no structured stop; docs disagreed on whether ship-stage runs `/author`.

**Fix:** Step 1.5 readiness gate emits `STOPPED — prerequisite: §Plan Author not populated for {ISSUE_ID_LIST}` + `/author` handoff when any Task spec still `_pending_`. Initial F6 fold (same day) replaced this gate with an active plan-author dispatch; F6 re-fold (same day, see entry above) reverted that change and moved plan-author + plan-review into `/stage-file` dispatcher instead. Gate is now the canonical shape — safe to re-enter on partial-failure recovery.

---

### 2026-04-19 — Subagent bailed "no Task tool in nested context" — premature 50.7k token burn

**Status:** fixed (agent body patched)

**Symptom:**
`/ship-stage ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md Stage 17` launched as subagent, bailed after 3 tool uses + 50.7k tokens + 37s, reporting "Subagent blocked — no Task tool in nested context". Re-dispatch with explicit "inline execution" instruction succeeded (100+ tool uses). Stage 8 production run (F9 entry below) had already proven inline execution works. Inconsistent behavior = misread of `tools:` frontmatter intent.

**Root cause:**
`.claude/agents/ship-stage.md` `tools:` frontmatter intentionally omits `Agent`/`Task` (subagent cannot nest-dispatch). Skill body Steps 2.1–2.4 phrase work as "Dispatch `spec-kickoff` subagent" / "Dispatch `spec-implementer`" etc. Subagent read "Dispatch X" literally, found no Task tool, bailed. SKILL.md §40 "Dispatch-shape agnostic" directive not reinforced in agent body.

Secondary drift: agent body + skill Steps 2.1/2.4 + Hard boundaries still referenced retired surfaces `spec-kickoff` + per-spec `project-stage-close` (M6 collapse folded both into `/author` Stage 1×N + Stage-scoped `/closeout` pair).

**Fix:**
Added explicit "Execution model (CRITICAL)" section to `.claude/agents/ship-stage.md` stating: subagent runs ALL phase work inline using native tools; "Dispatch X subagent" phrasing in SKILL.md is shorthand for "execute the work that subagent would do"; do NOT bail on missing Task tool. Updated retired-surface refs (`spec-kickoff` → `/author`; `project-stage-close` → Stage-scoped `/closeout`). Added hard boundary: "Do NOT bail with 'no Task tool in nested context'."

Deeper rewrite of skill Steps 2.1–2.4 + Step 3 to canonical rev-3 lifecycle surfaces (author → plan-review → per-task implement/verify/code-review → audit → closeout) deferred — current shorthand-with-translation-directive unblocks the immediate 50.7k token regression.

**Rollout row:** m8-retrospective

**Tracker aggregator:** [`ia/projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator`](../../projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator)

---

### 2026-04-19 — Self-referential dry-run scope diverged from T8.1 intent (F7 finding)

**Status:** pending (deferred — T8.1b external-plan re-run row 9)

**Symptom:**
T8.1 verbatim asked for "small _pending_ Task from any open master plan". M8 dry-run actually exercised Stage 8 of `lifecycle-refactor-master-plan.md` itself (filed TECH-485..488 into the plan-under-refactor). Self-referential. Stress-test broader (5 surfaces vs 3) but no isolation from refactor-churn — F4 sampling bias amplified.

**Root cause:**
Process gap, not skill code bug — T8.1 dispatch did not enforce external-plan target.

**Fix:**
pending — re-run T8.1b against external open master plan with _pending_ Task for steady-state yield sample. Locks F4/F5 re-measurement.

**Rollout row:** m8-retrospective

**Tracker aggregator:** [`ia/projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator`](../../projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator)

---

### 2026-04-19 — Clean end-to-end Stage chain ship (F9 positive signal)

**Status:** observed (no fix required — validates rev-3 collapse)

**Symptom:**
Single `/ship-stage ia/projects/lifecycle-refactor-master-plan.md 8` invocation: 68 tool uses, ~103.1k tokens, 8m 37s wall. 4 tasks (TECH-485–488) shipped through author → implement → verify-loop → code-review → audit → closeout. Stage verify passed (`validate:all` + `unity:compile-check` + `db:bridge-preflight`). All yamls archived; project specs deleted. M7 flipped `done` in migration JSON. No pair-contract escalations.

**Root cause:**
Positive signal — rev-3 Stage-scoped chain works end-to-end. Validates lifecycle-refactor M6 collapse (Stage-scoped bulk pair shape for author/audit/closeout).

**Fix:**
none required.

**Rollout row:** m8-retrospective

**Tracker aggregator:** [`ia/projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator`](../../projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator)

---

### 2026-04-19 — Migration-JSON polling via ad-hoc python3 awkward (F11 finding)

**Status:** pending (deferred — Fix #11 optional)

**Symptom:**
M8 dry-run agent ran 4 trial-and-error `python3 -c "...json..."` Bash calls to inspect `ia/state/lifecycle-refactor-migration.json` phases section before yielding usable output.

**Root cause:**
No typed surface for migration-JSON status query. Agent fell back to ad-hoc python.

**Fix:**
pending — candidate MCP tool `lifecycle_migration_status {phase?}` returning `{phase_id, status, flipped_at, notes}` OR documented `jq '.phases | to_entries | map(...)'` pattern in `ship-stage` SKILL §evidence gathering. Low priority.

**Rollout row:** m8-retrospective

**Tracker aggregator:** [`ia/projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator`](../../projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator)

---

### 2026-04-19 — STAGE_ID argument syntax drift (F12 finding)

**Status:** pending (deferred — Fix #10)

**Symptom:**
Original user invocation: `/ship-stage ia/projects/... 8` (bare numeric). Agent suggestion drifted to `/ship-stage ia/projects/... Stage 9` (word + number). `/ship-stage` subagent description = `{MASTER_PLAN_PATH} {STAGE_ID}` — `STAGE_ID` format spec ambiguous (`8` vs `8.1` vs `Stage 8` vs `Stage 8.1`).

**Root cause:**
`STAGE_ID` accepted-format spec underdefined; subagent suggestion prose drifted across surfaces.

**Fix:**
pending — lock `STAGE_ID` format in `.claude/agents/ship-stage.md` frontmatter description (pick one canonical form; reject ambiguous); align all subagent suggestion prose. Low priority.

**Rollout row:** m8-retrospective

**Tracker aggregator:** [`ia/projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator`](../../projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator)

---
