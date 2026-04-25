---
status: active
last_updated: 2026-04-21
---

# Agent lifecycle — canonical flow

Single canonical map for the `.claude/agents/` + `.claude/commands/` + `ia/skills/` surface. Names one entry point per lifecycle seam, defines the handoff each seam owes the next, and points at the authoritative rule / policy for every decision.

Thin anchor (always-loaded): [`ia/rules/agent-lifecycle.md`](../ia/rules/agent-lifecycle.md). Verification policy (canonical): [`docs/agent-led-verification-policy.md`](agent-led-verification-policy.md). Project hierarchy: [`ia/rules/project-hierarchy.md`](../ia/rules/project-hierarchy.md). Orchestrator vs project spec: [`ia/rules/orchestrator-vs-spec.md`](../ia/rules/orchestrator-vs-spec.md). Plan-Apply pair contract: [`ia/rules/plan-apply-pair-contract.md`](../ia/rules/plan-apply-pair-contract.md).

---

## 1. End-to-end flow (rev 4 — DB-primary refactor + skill-files-audit retirement scrub)

Not every issue visits every seam. Small one-shot fixes skip exploration + orchestration and enter at `/project-new`. Larger multi-step programs start at `/design-explore`.

```
exploration         orchestration         /stage-file chain                           /ship-stage chain
───────────         ─────────────         ────────────────                            ─────────────────
/design-explore     /master-plan-new      ┌──────────────────────────────┐            ┌─────────────────────────────────┐
  docs/{slug}.md ──→  ia/projects/     ──→│ stage-file (merged DB-backed)│  handoff   │ Pass A per-Task:                │
  + ## Design         {slug}-master     ──→│ → stage-authoring (1×N)      │ ─────────→ │  /implement + compile gate      │
  Expansion block     -plan.md             │ → plan-reviewer-mechanical   │            │  + task_status_flip(implemented)│
                      (permanent)          │ → plan-reviewer-semantic     │            │ Pass B per-Stage:               │
                                           │    (→ plan-applier Mode      │            │  /verify-loop (A+B cumulative)  │
                                           │     plan-fix on critical,    │            │  + /code-review                 │
                                           │     cap=1)                   │            │  + inline closeout              │
                                           │ → STOP                       │            │    (stage_closeout_apply MCP)   │
                                           └──────────────────────────────┘            │  + single stage commit          │
                                                                                       └─────────────────────────────────┘
```

`/ship-stage` is idempotent on `task_state` DB query — re-invocation on partially-done stages resumes from the first non-implemented Task. All chained seams (`/implement`, `/verify-loop`, `/code-review`) remain valid as standalone surfaces for ad-hoc / recovery use.

Single-task path (standalone issue, no master plan, N=1):

```
/project-new ──→ /author --task ──→ /ship {ISSUE_ID}
   (project-new-planner       (stage-authoring     (plan → implement → verify → close)
    → project-new-applier)     Stage 1×1; writes
                               §Plan Digest direct)
```

`/ship` chains the per-Task seams plan + implement + verify + close in one invocation for one issue id — a single-task analogue of `/ship-stage`. Closeout fires inline via `stage_closeout_apply` MCP (no separate `/closeout` seam). Standalone single-issue specs are archived by `/ship` itself or batched later by an owning Stage's `/ship-stage` Pass B.

Stage-scoped batching: `stage-authoring` fires ONCE per Stage (bulk Stage 1×N — single Opus pass over shared MCP bundle, writes §Plan Digest direct). Per-Task seams inside `/ship-stage` = `/implement`, then per-Stage `/verify-loop` + `/code-review` + inline closeout. Retired surfaces (do NOT invoke; tombstones live under `ia/skills/_retired/` + `.claude/agents/_retired/` + `.claude/commands/_retired/`): `spec-enrich`, `spec-kickoff` / `project-spec-kickoff` / `/kickoff`, `plan-author`, `plan-digest`, `plan-reviewer` (split into mechanical + semantic), `project-stage-close`, per-Task `project-spec-close`, `stage-closeout-plan`, `plan-applier` Mode stage-closeout, `opus-auditor` (dropped from `/ship-stage` Pass B per `3ac2d6e`), `/audit`, `/closeout`. All folded into `stage-authoring` (plan-author + plan-digest + spec-kickoff merge) or `/ship-stage` Pass B inline closeout (closeout chain merge).

Ad-hoc lanes (invoked outside the main flow, not ordered):

- `/verify` — lightweight single-pass Verification block (no fix iteration). Use between phases when `/verify-loop` is overkill.
- `/testmode` — standalone test-mode batch / bridge hybrid loop. Called ad-hoc or composed by `/verify-loop`.

Umbrella-level driver (sits ABOVE the single-issue flow, dispatches INTO it):

- `/release-rollout {UMBRELLA_SPEC} {ROW_SLUG} [OPERATION]` — advances one row of an umbrella rollout tracker (e.g. `docs/full-game-mvp-rollout-tracker.md`) through the 7-column lifecycle (a) enumerate → (b) explore → (c) plan → (d) stage-present → (e) stage-decomposed → (f) task-filed → (g) align. Target column (f) (≥1 task filed) gates handoff to the single-issue flow. Dispatches to the same lifecycle commands above (`/design-explore`, `/master-plan-new`, `/master-plan-extend`, `/stage-decompose`, `/stage-file`) per target cell — never reimplements decomposition / filing logic. Tracker is seeded once by `release-rollout-enumerate` helper. Does NOT close issues (= `/closeout`).

Stage-scoped chain driver (handoff from `/stage-file` after `stage-authoring` + `plan-reviewer-mechanical` + `plan-reviewer-semantic` complete):

- `/ship-stage {MASTER_PLAN_PATH} {STAGE_ID}` — DB-backed two-pass orchestrator. Pass A = per-Task `/implement` + `unity:compile-check` fast-fail + `task_status_flip(implemented)` (no per-task commits — Pass A leaves a dirty worktree). Pass B = per-Stage `/verify-loop` on cumulative HEAD diff + `/code-review` (inline fix cap=1) + per-Task verified→done flips + inline closeout via `stage_closeout_apply` MCP (single call: shared migration tuples + N archive ops + N status flips + N id-purge ops) + single stage commit `feat({slug}-stage-X.Y)` + per-task `task_commit_record` + `stage_verification_flip`. Resume gate via `task_state` DB query (no git scan). Args: `{MASTER_PLAN_PATH} {STAGE_ID} [--no-resume]`.

---

## 2. Seam → surface matrix

| # | Lifecycle seam | Slash command | Subagent (`.claude/agents/`) | Skill (`ia/skills/`) | Model | Primary output | Hands off to |
|---|----------------|---------------|------------------------------|----------------------|-------|----------------|--------------|
| 1 | Explore | `/design-explore {DOC_PATH}` | `design-explore.md` | `design-explore/` | Opus | `docs/{slug}.md` with `## Design Expansion` persisted | `/master-plan-new` or `/project-new` |
| 2 | Orchestrate | `/master-plan-new {DOC_PATH} [SLUG] [SCOPE_BOUNDARY_DOC]` | `master-plan-new.md` | `master-plan-new/` | Opus | `ia/projects/{slug}-master-plan.md` orchestrator (permanent, NOT closeable) | `/stage-file {slug}-master-plan.md Stage 1.1` |
| 2a | Extend orchestrator | `/master-plan-extend {ORCHESTRATOR_SPEC} {SOURCE_DOC}` | `master-plan-extend.md` | `master-plan-extend/` | Opus | Appended `### Step {START}..{END}` blocks + header sync | `/stage-file {ORCHESTRATOR_SPEC} Stage {START}.1` |
| 2b | Decompose step | `/stage-decompose {PATH} Step {N}` | `stage-decompose.md` | `stage-decompose/` | Opus | One Step skeleton → stages → phases → tasks materialized | `/stage-file {PATH} Stage {N}.1` |
| 3 | Bulk-file stage (chain) | `/stage-file {PATH} {STAGE}` | `stage-file.md` → `stage-authoring.md` → `plan-reviewer-mechanical.md` → `plan-reviewer-semantic.md` (→ `plan-applier.md` Mode plan-fix on critical) | `stage-file/` (merged DB-backed single-skill — `-planner` + `-applier` retired Step 6) → `stage-authoring/` → `plan-review-mechanical/` → `plan-review-semantic/` (→ `plan-applier/` Mode plan-fix) | Opus → Opus → Sonnet → Opus → Sonnet | N `ia_tasks` rows + N body stubs + table flipped + `§Plan Digest` written direct to body via `task_spec_section_write` MCP + drift scan PASS | chain stops post-plan-review — user runs `/ship-stage {PATH} Stage {STAGE}` (N≥2) OR `/ship {ISSUE_ID}` (N=1) |
| 4 | Single issue (pair seam #3, args-only) | `/project-new {intent} [--type ...]` | `project-new-planner.md` → `project-new-applier.md` | `project-new/` → `project-new-apply/` | Opus → Sonnet | One `ia_tasks` row + one body stub | applier stops at tail — user runs `/author --task` then `/ship` |
| 5 | Stage authoring (Stage 1×N) | `/author {PATH} {STAGE}` or `/author --task {ISSUE_ID}` | `stage-authoring.md` | `stage-authoring/` | Opus | N `§Plan Digest` sections written direct to Task bodies via `task_spec_section_write` MCP + canonical-term fold | `/plan-review` (N>1) or `/implement` (N=1) |
| 6 | Plan review (pair seam #1) | `/plan-review {PATH} {STAGE}` | `plan-reviewer-mechanical.md` → `plan-reviewer-semantic.md` → `plan-applier.md` Mode plan-fix | `plan-review-mechanical/` → `plan-review-semantic/` → `plan-applier/` Mode plan-fix | Sonnet → Opus → Sonnet | Drift scan on §Plan Digest: PASS sentinel OR `§Plan Fix` tuples | per-Task `/implement {ISSUE_ID}` loop |
| 7 | Implement | `/implement {ISSUE_ID}` | `spec-implementer.md` | `spec-implementer/` | Sonnet | Code changes + per-phase Task body updates | `/verify-loop {ISSUE_ID}` (or `/verify` between phases) |
| 8 | Verify (closed-loop) | `/verify-loop {ISSUE_ID}` | `verify-loop.md` | `verify-loop/` | Sonnet | JSON Verification block + caveman summary; bounded fix iteration (`MAX_ITERATIONS=2`); writes `§Findings` | `/code-review {ISSUE_ID}` |
| 8a | Verify (single-pass) | `/verify` | `verifier.md` | *(composes `project-implementation-validation`, `agent-test-mode-verify`, `ide-bridge-evidence`)* | Sonnet | JSON Verification block (no fix iteration) | same handoff shape as `/verify-loop` |
| 8b | Test-mode ad-hoc | `/testmode {SCENARIO_ID}` | `test-mode-loop.md` | `agent-test-mode-verify/` | Sonnet | `tools/reports/agent-testmode-batch-*.json` | any verify seam |
| 9 | Code review (pair seam #3 head, per-Task) | `/code-review {ISSUE_ID}` | `opus-code-reviewer.md` → `plan-applier.md` Mode code-fix | `opus-code-review/` → `plan-applier/` Mode code-fix | Opus → Sonnet | Verdict PASS/minor → `§Code Review` mini-report; critical → `§Code Fix Plan` tuples applied + re-enter `/verify-loop` | next Stage Task `/code-review` or `/ship-stage` Pass B inline closeout |
| C | Stage-scoped chain ship | `/ship-stage {PATH} {STAGE}` | `ship-stage.md` | `ship-stage/` | Opus | DB-backed two-pass — Pass A per-Task `/implement` + compile gate + `task_status_flip(implemented)`; Pass B per-Stage `/verify-loop` (cumulative diff) + `/code-review` (inline fix cap=1) + per-Task verified→done flips + inline closeout (`stage_closeout_apply` MCP) + single stage commit + `task_commit_record` + `stage_verification_flip` | next filed Stage or plan-level Final |
| C1 | Single-Task chain ship | `/ship {ISSUE_ID}` | `ship.md` | `ship/` | Opus | Plan → implement → verify → close for one `ISSUE_ID`; PASSED summary + next-handoff resolver. Inline closeout via `stage_closeout_apply` MCP (no separate `/closeout` seam). | Standalone issue: terminal (committed + closed); master-plan-owned: next Task or Stage close |
| U | Rollout umbrella | `/release-rollout {UMBRELLA_SPEC} {ROW_SLUG}` | `release-rollout.md` | `release-rollout/` (+ `-enumerate`, `-track`, `-skill-bug-log` helpers) | Opus | Tracker cell flipped + ticket + Change log row + next-row recommendation | Dispatches into seams 1 / 2 / 2a / 2b / 3 per target cell |
| R | Retrospective (skill training) | `/skill-train {SKILL_NAME}` | `skill-train.md` | `skill-train/` | Opus | `ia/skills/{SKILL_NAME}/train-proposal-{YYYY-MM-DD}.md` — unified-diff patch proposal | — (retrospective only — no auto-apply) |
| M | Meta / skill linearizer (preview composition) | `/unfold {TARGET_COMMAND} {TARGET_ARGS...} [--out PATH] [--depth N] [--format md\|yaml]` | `unfold.md` | `unfold/` | Sonnet | `ia/plans/{cmd-slug}-{arg-slug}-unfold.md` — decision-tree plan (explicit `on_success` / `on_failure` edges, literal arg substitution, runtime-only values as `${placeholder}`). Read-only — NO execution, NO source edits, NO commits | — (preview / audit artifact; user reviews + optionally runs `claude "follow {plan}"`) |
| P | Progress emit (preamble) | *(none)* | *(all agents, `@`-load)* | `subagent-progress-emit/` | — | `⟦PROGRESS⟧ {skill} {phase_i}/{phase_N} — {phase_name}` stderr lines | Host harness consumes for real-time progress UI |

Retired seams (tombstones only — do NOT invoke in new work): `spec-enrich` / `spec-kickoff` / `project-spec-kickoff` / `/kickoff` / `plan-author` / `plan-digest` (all folded into `stage-authoring`), `plan-reviewer` (split into `plan-reviewer-mechanical` + `plan-reviewer-semantic`), `code-fix-applier` / `plan-fix-applier` (collapsed into `plan-applier` Modes code-fix / plan-fix per TECH-506), `project-stage-close` / per-Task `project-spec-close` / `closeout-opus` / `stage-closeout-plan` / `plan-applier` Mode stage-closeout / `/closeout` (all folded into `/ship-stage` Pass B inline closeout via `stage_closeout_apply` MCP), `opus-auditor` / `/audit` (dropped from Pass B per `3ac2d6e`).

### 2a. Orchestrator Status flip owners (R1–R6)

Full enum + rules in `ia/rules/orchestrator-vs-spec.md`. Quick reference:

| Rule | Trigger | Owner | Flip |
|------|---------|-------|------|
| R1 | First task ever filed on plan | `stage-file` applier pass post-loop | Plan top `Draft → In Progress — Stage {N.M}` |
| R2 | First task filed in a stage | `stage-file` applier pass post-loop | Stage header `Draft → In Progress` |
| R3 | All tasks in stage archived | `/ship-stage` Pass B inline closeout (`stage_closeout_apply` MCP) | Stage `In Progress → Final` |
| R5 | All Stages Final | `/ship-stage` Pass B inline closeout (`stage_closeout_apply` MCP) | Plan top `In Progress → Final` |
| R6 | New Stages appended to Final plan | `master-plan-extend` Phase 7c | Plan top `Final → In Progress — Stage {N.M_new}` |
| R7 | Stage decomposed from skeleton | `stage-decompose` Phase 4c | Stage `Skeleton → Draft (tasks _pending_)` |

---

## 3. Handoff contract

Every seam owes the next one a concrete artifact. Missing artifact = the next seam refuses to start.

| From | Owes | To | Refuses when missing |
|------|------|----|----------------------|
| `/design-explore` | `## Design Expansion` block persisted in `docs/{slug}.md` | `/master-plan-new` | Skill refuses authoring if expansion block absent |
| `/master-plan-new` | `ia/projects/{slug}-master-plan.md` with `_pending_` task seeds + cardinality gate (≥2 tasks/phase) cleared | `/stage-file` | Stage-file-planner refuses when tasks missing or cardinality unjustified |
| `/master-plan-extend` | Extended orchestrator with new `### Step {START}..{END}` blocks (fully decomposed) + header metadata synced | `/stage-file {ORCHESTRATOR_SPEC} Stage {START}.1` | Stage-file-planner refuses when new stage tasks missing |
| `/stage-decompose` | Target Step materialized: skeleton → stages → phases → tasks | `/stage-file {PATH} Stage {N}.1` | Stage-file-planner refuses if target Step Status still `Skeleton` |
| `/stage-file` | N `ia_tasks` rows + body stubs + orchestrator table rows flipped + `§Plan Digest` written direct per Task body via `task_spec_section_write` MCP + `/plan-review` PASS sentinel | user `/ship-stage {PATH} {STAGE}` (N≥2 — not auto-chained) OR `/ship {ISSUE_ID}` (N=1) | `/ship-stage` refuses when `§Plan Digest` unpopulated for any Task |
| `/project-new` | One `ia_tasks` row + body stub + `validate:dead-project-specs` green | user `/author --task` then `/ship` (not auto-chained) | `stage-authoring` refuses bare stub without §1 / §2 context |
| `/author` | Each Task body §Plan Digest populated (mechanical form, written direct via `task_spec_section_write` MCP) + canonical-term fold + `plan_digest_lint` pass | `/plan-review` (N>1) or `/implement` / `/ship` (N=1) | `/plan-review` refuses when any §Plan Digest missing/invalid; `/implement` refuses when digest still `_pending_` |
| `/plan-review` | Drift scan PASS sentinel OR `§Plan Fix` tuples applied to N Task bodies; master-plan Stage block synced; glossary / invariants aligned | per-Task `/implement` loop | `/implement` refuses if drift verdict still `fix` + tuples unapplied |
| `/implement` | Phase code applied, compile clean, Task body Decision Log / Issues Found / Lessons appended per phase | `/verify-loop` | `/verify-loop` refuses when compile gate fails (Step 1) |
| `/verify-loop` | JSON Verification block with `verdict: pass` + `§Findings` non-empty in Task body | `/code-review` | `/code-review` refuses when `§Findings` empty or verify verdict non-pass |
| `/code-review` | Per-Task `§Code Review` mini-report verdict PASS/minor (critical verdict triggers `plan-applier` Mode code-fix + re-enter `/verify-loop` → re-run `/code-review`) | next Stage Task `/code-review` OR `/ship-stage` Pass B inline closeout | Pass B inline closeout refuses if any Task `§Code Review` verdict still critical + unresolved |
| `/ship-stage` Pass B inline closeout (`stage_closeout_apply` MCP) | Single MCP call: shared migration ops deduped + N per-Task archive (`ia_tasks.archived_at`) / status-flip / id-purge ops; Stage / Plan Status rolled up per R3 / R5; `materialize-backlog.sh` + `validate:all` run once at end | next Stage OR plan-level Final | — (terminal per Stage) |

Verification policy contract: [`docs/agent-led-verification-policy.md`](agent-led-verification-policy.md). Pair contract: [`ia/rules/plan-apply-pair-contract.md`](../ia/rules/plan-apply-pair-contract.md).

---

## 4. Decision tree — which command do I run right now?

```
Question                                                              → Command
────────                                                              ─────────
Fuzzy idea, no doc yet?                                               → none — write docs/{slug}.md yourself first
Exploration doc exists, needs to become a design?                     → /design-explore
Design persisted, multi-step work with stage > task?                  → /master-plan-new
Design persisted, single issue is enough?                             → /project-new
Orchestrator exists, new exploration / extensions doc adds Stages?    → /master-plan-extend
Orchestrator exists, a skeleton Stage needs decomposition?            → /stage-decompose
Orchestrator exists, a stage is ready to materialize?                 → /stage-file
Stage filed (N≥2) + stage-authoring + plan-review complete, ship Stage? → /ship-stage  (Pass A implement + Pass B verify + code-review + inline closeout + commit)
Single task (N=1) authored, drive end-to-end for one ISSUE_ID?        → /ship {ISSUE_ID}  (plan → implement → verify → close inline)
Stage filed ad-hoc (N=1) or recovery authoring?                       → /author --task (standalone — writes §Plan Digest direct)
Authored spec needs drift scan standalone?                            → /plan-review (standalone recovery; chained inside /stage-file by default)
Spec fully authored, ready to ship code?                              → /implement
Phase just landed, want a quick sanity pass?                          → /verify
Phase / stage / spec done, need full closed-loop + fix iter?          → /verify-loop
Bridge / batch evidence needed in isolation?                          → /testmode
Task verify green, need post-verify code review?                      → /code-review
Multi-task Stage with ≥1 non-Done row, drive all end-to-end?          → /ship-stage
Umbrella master-plan with rollout tracker, advance one row?           → /release-rollout {UMBRELLA_SPEC} {ROW_SLUG}
Skill showing recurring friction, want retrospective patch proposal?  → /skill-train {SKILL_NAME}
```

---

## 5. Verification split — `/verify` vs `/verify-loop`

| Aspect | `/verify` | `/verify-loop` |
|--------|-----------|----------------|
| Scope | Single pass | Closed-loop (7 steps) |
| Code edits | None (read-only reporter) | Narrow: Step 6 fix iteration only |
| Fix iteration | — | Bounded `MAX_ITERATIONS` (default 2) |
| Writes §Findings? | No | Yes (Task body §Findings — feeds `/code-review`; legacy `/audit` R11 gate retired with opus-auditor per `3ac2d6e`) |
| Output style | `verification-report` (JSON + caveman) | Same shape + `fix_iterations` / `verdict` / `human_ask` fields |
| When | Between phases, pre-PR sanity check | Pre-`/code-review`, pre-Stage-close, pre-umbrella-close |
| Composes | `validate:all` + compile gate + Path A OR Path B | `bridge-environment-preflight` + `project-implementation-validation` + `agent-test-mode-verify` + `ide-bridge-evidence` + `close-dev-loop` |

Both defer to the single canonical policy [`docs/agent-led-verification-policy.md`](agent-led-verification-policy.md) for timeout escalation, Path A lock release, and Path B preflight. Neither agent restates the policy.

---

## 6. Close seam — `/ship-stage` Pass B inline closeout only (rev 4 — no separate `/closeout` command)

| Aspect | `/ship-stage` Pass B inline closeout (`stage_closeout_apply` MCP) |
|--------|------------------------------------------------------------------|
| Fires | Once per Stage inside Pass B, after `/verify-loop` + `/code-review` complete + per-Task verified→done flips |
| Touches | Single MCP call applies: N per-Task `ia_tasks.archived_at` set + spec filesystem-mirror deleted + master-plan Task row flipped `Done → Done (archived)` + id purged from durable docs/code. Shared: Stage / Plan Status rolled up per R3 / R5; `materialize-backlog.sh` + `validate:all` run once at end. |
| Deletes spec? | Yes — filesystem-mirror spec files deleted after lessons migration to canonical IA. DB row preserved with `archived_at` timestamp. |
| Touches BACKLOG? | Yes — `materialize-backlog.sh` regenerates legacy view (`BACKLOG.md` derived from DB post Step 9.6). |
| Confirmation gate? | No — Pass B is committed atomically per Stage (single stage commit `feat({slug}-stage-X.Y)` lands closeout + verified diff together). |
| Separate `/closeout` command? | **Retired.** `/closeout` slash command + `stage-closeout-plan` skill + `plan-applier` Mode stage-closeout retired — full closeout chain folded into Pass B inline call. |

Retired close seams (post DB-primary refactor): `project-stage-close` / per-Task `project-spec-close` / `closeout-opus` / `stage-closeout-plan` / `plan-applier` Mode stage-closeout / `/closeout` — all folded into `/ship-stage` Pass B inline closeout via `stage_closeout_apply` MCP. Legacy `/closeout {ISSUE_ID}` and `/closeout {PATH} {STAGE}` invocation shapes are removed.

---

## 7. Re-entry and partial completion

Each seam is idempotent against its own output: running `stage-authoring` twice on an already-authored Stage re-applies canonical-term fold idempotently; running `/verify-loop` twice on an already-green branch re-emits the Verification block without fix iteration; running `/ship-stage` twice on an already-closed Stage no-ops (resume gate detects all `ia_tasks` rows Status = `verified` + `archived_at` set and exits clean).

Resume rule: on returning to a paused issue, run `/verify` first to re-establish branch state, then pick up at the seam after the last green handoff artifact.

Never reuse retired ids. The monotonic-per-prefix rule ([`AGENTS.md` §7](../AGENTS.md)) holds across `ia_tasks` rows + `archived_at`-set rows (and their generated views `BACKLOG.md` + `BACKLOG-ARCHIVE.md`). Id reservation: `bash tools/scripts/reserve-id.sh {PREFIX}` (single) or MCP `reserve_backlog_ids` (batch) — never scan markdown views for max id.

### Crashed-ship-stage recovery

`/ship-stage` Pass A + Pass B are idempotent against the `ia_tasks` + `ia_stages` DB rows. If Pass A crashes mid-loop, re-run `/ship-stage {PATH} {STAGE}` — resume gate via `task_state` MCP query detects already-implemented tasks (`status = implemented`) and skips them, picking up at the first unimplemented task. If Pass B crashes mid-loop (verify-loop / code-review / inline closeout), the `stage_closeout_apply` MCP call is atomic against shared migration tuples + N archive ops + N status flips + N id-purge ops — re-run `/ship-stage` resumes via `stage_state` DB query (already-archived `ia_tasks.archived_at` rows skipped). No filesystem lockfile required — DB serializability handles concurrency. Invariant #13 (id-counter lock at `ia/state/.id-counter.lock`) preserved separately.

---

## 8. Adding a new agent / command / skill

New seam proposal → update:

1. This doc (§1 flow diagram + §2 matrix + §3 handoff row + §4 decision tree row).
2. [`AGENTS.md`](../AGENTS.md) §2 — one-line row pointing at this doc.
3. [`ia/skills/README.md`](../ia/skills/README.md) — skill index row if a new skill.
4. [`docs/information-architecture-overview.md`](information-architecture-overview.md) §3 + §7 — if the change affects the Knowledge lifecycle or Skill system tables.

Subagent authoring conventions (Opus vs Sonnet, `reasoning_effort`, caveman directive, forwarded `caveman:caveman` preamble, pair contract alignment): [`CLAUDE.md`](../CLAUDE.md) §3 + [`ia/rules/plan-apply-pair-contract.md`](../ia/rules/plan-apply-pair-contract.md). Slash command dispatcher shape: mirror an existing `.claude/commands/*.md` (verbatim subagent prompt forwarded via Agent tool with `subagent_type`).

---

## 9. Crosslinks

- [`ia/rules/project-hierarchy.md`](../ia/rules/project-hierarchy.md) — step > stage > phase > task semantics.
- [`ia/rules/orchestrator-vs-spec.md`](../ia/rules/orchestrator-vs-spec.md) — permanent vs temporary doc split.
- [`ia/rules/plan-apply-pair-contract.md`](../ia/rules/plan-apply-pair-contract.md) — Plan-Apply pair tuple contract + escalation rule.
- [`docs/agent-led-verification-policy.md`](agent-led-verification-policy.md) — Verification block canonical policy.
- [`CLAUDE.md`](../CLAUDE.md) — Claude Code host surface (hooks, slash commands, subagents, memory).
- [`AGENTS.md`](../AGENTS.md) — agent workflow + backlog / issue process.
- [`ia/skills/README.md`](../ia/skills/README.md) — skill index + conventions.

---

## 10. Claude Code host — Task tool + paste-ready handoffs

**Task updates (Agent tool + Claude Code Tasks):** when a dispatched subagent returns success, the **next** tool call should flip the owning task to **completed** (`TaskUpdate` / equivalent) **before** the user-facing summary. Long pipelines with multiple subagent returns should repeat at each success boundary so context compaction cannot leave work done but the task stuck `in_progress`.

**Handoff lines:** do not emit a “next command” with unresolved placeholders (`{slug}`, `{ISSUE_ID}`, `{path}`, …). Resolve args from the master plan / `BACKLOG.md` / tracker, then emit one paste-ready line. This repo’s slash-command examples often wrap as `claude-personal "/…"` for terminal launch — adjust the launcher prefix to match the developer’s install.
