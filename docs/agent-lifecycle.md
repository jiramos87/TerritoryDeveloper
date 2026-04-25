---
status: active
last_updated: 2026-04-21
---

# Agent lifecycle — canonical flow

Single canonical map for the `.claude/agents/` + `.claude/commands/` + `ia/skills/` surface. Names one entry point per lifecycle seam, defines the handoff each seam owes the next, and points at the authoritative rule / policy for every decision.

Thin anchor (always-loaded): [`ia/rules/agent-lifecycle.md`](../ia/rules/agent-lifecycle.md). Verification policy (canonical): [`docs/agent-led-verification-policy.md`](agent-led-verification-policy.md). Project hierarchy: [`ia/rules/project-hierarchy.md`](../ia/rules/project-hierarchy.md). Orchestrator vs project spec: [`ia/rules/orchestrator-vs-spec.md`](../ia/rules/orchestrator-vs-spec.md). Plan-Apply pair contract: [`ia/rules/plan-apply-pair-contract.md`](../ia/rules/plan-apply-pair-contract.md).

---

## 1. End-to-end flow (rev 3 — M6 cognitive-split collapse)

Not every issue visits every seam. Small one-shot fixes skip exploration + orchestration and enter at `/project-new`. Larger multi-step programs start at `/design-explore`.

```
exploration         orchestration         /stage-file chain                           /ship-stage chain
───────────         ─────────────         ────────────────                            ─────────────────
/design-explore     /master-plan-new      ┌────────────────────────────┐              ┌──────────────────────────┐
  docs/{slug}.md ──→  ia/projects/     ──→│ stage-file-planner         │   handoff    │ Phase 1.5: §Plan Digest  │
  + ## Design         {slug}-master     ──→│ → stage-file-applier       │  ──────────→ │  readiness gate          │
  Expansion block     -plan.md             │ → plan-author (1×N)        │   (legacy:   │ Pass 1 per-Task:         │
                      (permanent)          │ → plan-digest (1×N)        │   JIT digest │  /implement + compile    │
                                           │ → plan-reviewer            │   if needed) │ Pass 2 Stage-end:        │
                                           │    (→ plan-fix-applier     │              │  /verify-loop (A+B)      │
                                           │     on critical, cap=1)    │              │  + /code-review          │
                                           │ → STOP                     │              │  + /audit                │
                                           └────────────────────────────┘              │  + /closeout             │
                                                                                       └──────────────────────────┘
```

`/ship-stage` readiness gate is idempotent on §Plan Digest (§Plan Author is ephemeral — `plan-digest` drops it after mechanizing; Q5 2026-04-22) — re-invocation on partially-done stages is safe. All chained seams (`/author`, `/plan-digest`, `/plan-review`, `/implement`, `/verify-loop`, `/code-review`, `/audit`, `/closeout`) remain valid as standalone surfaces for ad-hoc / recovery use.

Single-task path (standalone issue, no master plan, N=1):

```
/project-new ──→ /author --task ──→ /plan-digest --task ──→ /ship {ISSUE_ID}
   (project-new-planner       (plan-author        (plan-digest     (readiness → /implement → /verify-loop →
    → project-new-applier)     Stage 1×1)         Stage 1×1)       /code-review (fix loop cap=1) → /audit N=1)
```

`/ship` chains the per-Task seams implement + verify + code-review + audit in one invocation for one issue id — a single-task analogue of `/ship-stage`. Closeout is NOT part of `/ship` (retired per-Task `closeout` agent; `/closeout` is Stage-scoped only post-T7.14). Standalone single-issue specs remain open until a later Stage close batches them, or are archived manually.

Stage-end batching: `/author`, `/audit`, `/closeout` all fire ONCE per Stage (bulk Stage 1×N — single Opus pass over shared MCP bundle). Per-Task seams = `/implement`, `/verify-loop`, `/code-review`. No `spec-enrich`, no `spec-kickoff`, no `project-stage-close`, no per-Task `project-spec-close` — all absorbed into the Stage-scoped bulk pair shape (T7.11 — `/author`; T7.12 — `subagent-progress-emit`; T7.13 — `stage-closeout-plan`; T7.14 — `stage-closeout-apply`). Tombstones live under `ia/skills/_retired/` + `.claude/agents/_retired/` + `.claude/commands/_retired/`. Post-F6 re-fold (2026-04-20) + plan-digest (2026-04-22): `/stage-file` now ALSO runs `plan-author` + `plan-digest` + `plan-reviewer` (→ `plan-fix-applier` on critical, re-entry cap=1) as final internal phases after the applier tail — 3-command stage entry collapses to 1 command (`/stage-file`). `/author` + `/plan-digest` + `/plan-review` remain valid as standalone surfaces for recovery + ad-hoc fixes.

Ad-hoc lanes (invoked outside the main flow, not ordered):

- `/verify` — lightweight single-pass Verification block (no fix iteration). Use between phases when `/verify-loop` is overkill.
- `/testmode` — standalone test-mode batch / bridge hybrid loop. Called ad-hoc or composed by `/verify-loop`.

Umbrella-level driver (sits ABOVE the single-issue flow, dispatches INTO it):

- `/release-rollout {UMBRELLA_SPEC} {ROW_SLUG} [OPERATION]` — advances one row of an umbrella rollout tracker (e.g. `docs/full-game-mvp-rollout-tracker.md`) through the 7-column lifecycle (a) enumerate → (b) explore → (c) plan → (d) stage-present → (e) stage-decomposed → (f) task-filed → (g) align. Target column (f) (≥1 task filed) gates handoff to the single-issue flow. Dispatches to the same lifecycle commands above (`/design-explore`, `/master-plan-new`, `/master-plan-extend`, `/stage-decompose`, `/stage-file`) per target cell — never reimplements decomposition / filing logic. Tracker is seeded once by `release-rollout-enumerate` helper. Does NOT close issues (= `/closeout`).

Stage-scoped chain driver (handoff from `/stage-file` after `plan-author` + `plan-digest` + `plan-review` complete):

- `/ship-stage {MASTER_PLAN_PATH} {STAGE_ID}` — chains all non-Done filed tasks of one Stage through §Plan Digest readiness gate (Phase 1.5; lazy-migration JIT if legacy §Plan Author only) → per-Task `/implement` + `unity:compile-check` (Pass 1) → Stage-end bulk `/verify-loop` (full Path A+B) + `/code-review` + `/audit` + `/closeout` (Pass 2). Emits chain-level stage digest. Per-task Path A mandatory; batched Path B once at stage end on cumulative delta. Chain stops on first gate failure (readiness / Pass 1 / Pass 2). `STAGE_CODE_REVIEW_CRITICAL_TWICE` re-entry cap = 1. Plan-author + plan-digest + plan-review do NOT run inside `/ship-stage` for the common case — they fold into `/stage-file` (exception: JIT `plan-digest` on legacy specs per Q13).

---

## 2. Seam → surface matrix

| # | Lifecycle seam | Slash command | Subagent (`.claude/agents/`) | Skill (`ia/skills/`) | Model | Primary output | Hands off to |
|---|----------------|---------------|------------------------------|----------------------|-------|----------------|--------------|
| 1 | Explore | `/design-explore {DOC_PATH}` | `design-explore.md` | `design-explore/` | Opus | `docs/{slug}.md` with `## Design Expansion` persisted | `/master-plan-new` or `/project-new` |
| 2 | Orchestrate | `/master-plan-new {DOC_PATH} [SLUG] [SCOPE_BOUNDARY_DOC]` | `master-plan-new.md` | `master-plan-new/` | Opus | `ia/projects/{slug}-master-plan.md` orchestrator (permanent, NOT closeable) | `/stage-file {slug}-master-plan.md Stage 1.1` |
| 2a | Extend orchestrator | `/master-plan-extend {ORCHESTRATOR_SPEC} {SOURCE_DOC}` | `master-plan-extend.md` | `master-plan-extend/` | Opus | Appended `### Step {START}..{END}` blocks + header sync | `/stage-file {ORCHESTRATOR_SPEC} Stage {START}.1` |
| 2b | Decompose step | `/stage-decompose {PATH} Step {N}` | `stage-decompose.md` | `stage-decompose/` | Opus | One Step skeleton → stages → phases → tasks materialized | `/stage-file {PATH} Stage {N}.1` |
| 3 | Bulk-file stage (chain) | `/stage-file {PATH} {STAGE}` | `stage-file-planner.md` → `stage-file-applier.md` → `plan-author.md` → `plan-digest.md` → `plan-reviewer.md` (→ `plan-fix-applier.md` on critical) | `stage-file-plan/` → `stage-file-apply/` → `plan-author/` → `plan-digest/` → `plan-review/` (→ `plan-fix-apply/`) | Opus → Sonnet → Opus → Opus → Sonnet | N BACKLOG rows + N spec stubs + table flipped + `§Plan Author` (ephemeral) + `§Plan Digest` + drift scan PASS | chain stops post-plan-review — user runs `/ship-stage {PATH} Stage {STAGE}` (N≥2) OR `/ship {ISSUE_ID}` (N=1) |
| 4 | Single issue (pair seam #3, args-only) | `/project-new {intent} [--type ...]` | `project-new-planner.md` → `project-new-applier.md` | `project-new/` → `project-new-apply/` | Opus → Sonnet | One BACKLOG row + one `ia/projects/{ISSUE_ID}.md` stub | applier stops at tail — user runs `/author --task` → `/plan-digest --task` then `/ship` |
| 5 | Bulk author (Stage 1×N) | `/author {PATH} {STAGE}` or `/author --task {ISSUE_ID}` | `plan-author.md` | `plan-author/` | Opus | N `ia/projects/{ISSUE_ID}.md` `§Plan Author` sections + canonical-term fold | `/plan-digest` then `/plan-review` (N>1) or `/plan-digest` then `/implement` (N=1) |
| 5a | Plan digest (Stage 1×N) | `/plan-digest {PATH} {STAGE}` or `/plan-digest --task {ISSUE_ID}` | `plan-digest.md` | `plan-digest/` | Opus | `§Plan Digest` per spec (drops §Plan Author) + optional `docs/implementation/{slug}-stage-{id}-plan.md` | `/plan-review` (N>1) or `/implement` (N=1) when preceded by `/author` only |
| 6 | Plan review (pair seam #1) | `/plan-review {PATH} {STAGE}` | `plan-reviewer.md` → `plan-fix-applier.md` | `plan-review/` → `plan-fix-apply/` | Sonnet → Sonnet | Drift scan on §Plan Digest: PASS sentinel OR `§Plan Fix` tuples | per-Task `/implement {ISSUE_ID}` loop |
| 7 | Implement | `/implement {ISSUE_ID}` | `spec-implementer.md` | `project-spec-implement/` | Sonnet | Code changes + per-phase spec updates (Decision Log / Issues Found / Lessons) | `/verify-loop {ISSUE_ID}` (or `/verify` between phases) |
| 8 | Verify (closed-loop) | `/verify-loop {ISSUE_ID}` | `verify-loop.md` | `verify-loop/` | Sonnet | JSON Verification block + caveman summary; bounded fix iteration (`MAX_ITERATIONS=2`); writes `§Findings` | `/code-review {ISSUE_ID}` |
| 8a | Verify (single-pass) | `/verify` | `verifier.md` | *(composes `project-implementation-validation`, `agent-test-mode-verify`, `ide-bridge-evidence`)* | Sonnet | JSON Verification block (no fix iteration) | same handoff shape as `/verify-loop` |
| 8b | Test-mode ad-hoc | `/testmode {SCENARIO_ID}` | `test-mode-loop.md` | `agent-test-mode-verify/` | Sonnet | `tools/reports/agent-testmode-batch-*.json` | any verify seam |
| 9 | Code review (pair seam #4 head, per-Task) | `/code-review {ISSUE_ID}` | `opus-code-reviewer.md` → `code-fix-applier.md` | `opus-code-review/` → `code-fix-apply/` | Opus → Sonnet | Verdict PASS/minor → `§Code Review` mini-report; critical → `§Code Fix Plan` tuples applied + re-enter `/verify-loop` | next Stage Task `/code-review` or Stage `/audit` |
| 10 | Audit (Stage 1×N) | `/audit {PATH} {STAGE}` | `opus-auditor.md` | `opus-audit/` | Opus | N `§Audit` paragraphs (consistent voice; R11 §Findings gate enforced) | `/closeout {PATH} {STAGE}` |
| 11 | Close stage (pair seam #4 tail, Stage 1×N) | `/closeout {PATH} {STAGE}` | `stage-closeout-planner.md` → `stage-closeout-applier.md` | `stage-closeout-plan/` → `stage-closeout-apply/` | Opus → Sonnet | Shared migration ops deduped + N per-Task archive / delete / status-flip / id-purge / digest_emit ops; Stage header → Final; rolled up to Step / Plan per R3–R5 | next Stage (R2) or plan-level Final (R5) |
| C | Stage-scoped chain ship | `/ship-stage {PATH} {STAGE}` | `ship-stage.md` | `ship-stage/` | Opus | Phase 1.5 §Plan Digest readiness gate (+ optional JIT `plan-digest`) + Pass 1 `/implement` + compile gate per Task + Pass 2 Stage-end verify-loop (full Path A+B) + `/code-review` + `/audit` + `/closeout`; chain-level stage digest (`ia/skills/ship-stage/SKILL.md`) | next filed Stage or plan-level Final |
| C1 | Single-Task chain ship | `/ship {ISSUE_ID}` | *(no dedicated agent — dispatches canonical per-Task subagents)* `spec-implementer.md` → `verify-loop.md` → `opus-code-reviewer.md` (→ `plan-applier.md` Mode code-fix on critical, cap=1) → `opus-auditor.md` (N=1 degenerate) | *(no dedicated skill — composes)* `project-spec-implement/` → `verify-loop/` → `opus-code-review/` (→ `plan-applier/` Mode code-fix) → `opus-audit/` (N=1) | Opus orchestrator | Stage 1 §Plan Digest readiness gate + `/implement` + full `/verify-loop` (A+B) + `/code-review` + `/audit` for one `ISSUE_ID`; PASSED summary + next-handoff resolver (`/ship-stage` / `/ship` / `/stage-decompose`). No closeout — spec archived later by owning Stage's `/ship-stage` Step 3.5 or `/closeout`. | Standalone issue: terminal (commit + later Stage close); master-plan-owned: same next-handoff resolver as `/ship-stage` |
| U | Rollout umbrella | `/release-rollout {UMBRELLA_SPEC} {ROW_SLUG}` | `release-rollout.md` | `release-rollout/` (+ `-enumerate`, `-track`, `-skill-bug-log` helpers) | Opus | Tracker cell flipped + ticket + Change log row + next-row recommendation | Dispatches into seams 1 / 2 / 2a / 2b / 3 per target cell |
| R | Retrospective (skill training) | `/skill-train {SKILL_NAME}` | `skill-train.md` | `skill-train/` | Opus | `ia/skills/{SKILL_NAME}/train-proposal-{YYYY-MM-DD}.md` — unified-diff patch proposal | — (retrospective only — no auto-apply) |
| M | Meta / skill linearizer (preview composition) | `/unfold {TARGET_COMMAND} {TARGET_ARGS...} [--out PATH] [--depth N] [--format md\|yaml]` | `unfold.md` | `unfold/` | Sonnet | `ia/plans/{cmd-slug}-{arg-slug}-unfold.md` — decision-tree plan (explicit `on_success` / `on_failure` edges, literal arg substitution, runtime-only values as `${placeholder}`). Read-only — NO execution, NO source edits, NO commits | — (preview / audit artifact; user reviews + optionally runs `claude "follow {plan}"`) |
| P | Progress emit (preamble) | *(none)* | *(all agents, `@`-load)* | `subagent-progress-emit/` | — | `⟦PROGRESS⟧ {skill} {phase_i}/{phase_N} — {phase_name}` stderr lines | Host harness consumes for real-time progress UI |

Retired seams (tombstones only — do NOT invoke in new work): `spec-enrich` (folded into `plan-author`), `spec-kickoff` / `project-spec-kickoff` / `/kickoff` (folded into `plan-author`), `project-stage-close` (folded into Stage-scoped `/closeout` pair), per-Task `project-spec-close` / `closeout-opus` (folded into Stage `stage-closeout-plan` → `stage-closeout-apply`).

### 2a. Orchestrator Status flip owners (R1–R7)

Full enum + rules in `ia/rules/orchestrator-vs-spec.md`. Quick reference:

| Rule | Trigger | Owner | Flip |
|------|---------|-------|------|
| R1 | First task ever filed on plan | `stage-file-applier` post-loop | Plan top `Draft → In Progress — Step {N} / Stage {N.M}` |
| R2 | First task filed in a stage | `stage-file-applier` post-loop | Stage header `Draft/Planned → In Progress` |
| R3 | All tasks in stage archived | `stage-closeout-applier` Phase 5b | Stage `In Progress → Final` |
| R4 | All stages in step Final | `stage-closeout-applier` Phase 5b | Step `In Progress → Final` |
| R5 | All Steps Final | `stage-closeout-applier` Phase 5b | Plan top `In Progress → Final` |
| R6 | New Steps appended to Final plan | `master-plan-extend` Phase 7c | Plan top `Final → In Progress — Step {N_new} / Stage {N_new}.1` |
| R7 | Step decomposed from skeleton | `stage-decompose` Phase 4c | Step `Skeleton → Draft (tasks _pending_)` |
| R11 | §Findings gate (verify → audit) | `opus-auditor` Phase 0 | Block `/audit` dispatch if any Task §Findings empty |

---

## 3. Handoff contract

Every seam owes the next one a concrete artifact. Missing artifact = the next seam refuses to start.

| From | Owes | To | Refuses when missing |
|------|------|----|----------------------|
| `/design-explore` | `## Design Expansion` block persisted in `docs/{slug}.md` | `/master-plan-new` | Skill refuses authoring if expansion block absent |
| `/master-plan-new` | `ia/projects/{slug}-master-plan.md` with `_pending_` task seeds + cardinality gate (≥2 tasks/phase) cleared | `/stage-file` | Stage-file-planner refuses when tasks missing or cardinality unjustified |
| `/master-plan-extend` | Extended orchestrator with new `### Step {START}..{END}` blocks (fully decomposed) + header metadata synced | `/stage-file {ORCHESTRATOR_SPEC} Stage {START}.1` | Stage-file-planner refuses when new stage tasks missing |
| `/stage-decompose` | Target Step materialized: skeleton → stages → phases → tasks | `/stage-file {PATH} Stage {N}.1` | Stage-file-planner refuses if target Step Status still `Skeleton` |
| `/stage-file` | `ia/backlog/{id}.yaml` records + project spec stubs + `BACKLOG.md` regenerated + orchestrator table rows flipped + `§Plan Digest` per spec + `/plan-review` PASS sentinel | user `/ship-stage {PATH} {STAGE}` (N≥2 — not auto-chained) OR `/ship {ISSUE_ID}` (N=1) | Ship-stage refuses when `§Plan Digest` unpopulated (after lazy-migration if needed) |
| `/project-new` | `ia/backlog/{ISSUE_ID}.yaml` record + one template-seeded `ia/projects/{ISSUE_ID}.md` + `BACKLOG.md` regenerated + `validate:dead-project-specs` green | user `/author --task` then `/plan-digest --task` (not auto-chained) | Author refuses bare stub without §1 / §2 context |
| `/author` | Each spec §Plan Author populated (ephemeral) + canonical-term fold applied | `/plan-digest` | Plan-digest refuses when §Plan Author incomplete |
| `/plan-digest` | Each spec `§Plan Digest` (mechanical form) + drops §Plan Author; `plan_digest_lint` pass | `/plan-review` (N>1) or `/implement` (N=1) | Plan-review refuses when any §Plan Digest missing/invalid; implement refuses when digest still `_pending_` |
| `/plan-review` | Drift scan PASS sentinel OR `§Plan Fix` tuples applied to N specs; master-plan Stage block synced; glossary / invariants aligned | per-Task `/implement` loop | Implement refuses if drift verdict still `fix` + tuples unapplied |
| `/implement` | Phase code committed, compile clean, spec §6 / §9 / §10 appended per phase | `/verify-loop` | Verify-loop refuses when compile gate fails (Step 1) |
| `/verify-loop` | JSON Verification block with `verdict: pass` + `§Findings` non-empty in spec | `/code-review` | Code-review refuses when `§Findings` empty or verify verdict non-pass |
| `/code-review` | Per-Task `§Code Review` mini-report verdict PASS/minor (critical verdict triggers `/code-fix-apply` + re-enter `/verify-loop` → re-run `/code-review`) | next Stage Task `/code-review` OR Stage `/audit` | Audit Phase 0 refuses if any Task `§Code Review` verdict still critical + unresolved |
| `/audit` | N `§Audit` paragraphs written (R11 §Findings gate cleared) | `/closeout {PATH} {STAGE}` | Stage-closeout-planner refuses if any Task `§Audit` missing |
| `/closeout` | Shared migration ops deduped + N per-Task archive / delete / status-flip / id-purge / Stage digest aggregated; Stage / Step / Plan Status rolled up per R3–R5 | next Stage OR plan-level Final | — (terminal per Stage) |

Verification policy contract: [`docs/agent-led-verification-policy.md`](agent-led-verification-policy.md). Pair contract: [`ia/rules/plan-apply-pair-contract.md`](../ia/rules/plan-apply-pair-contract.md).

---

## 4. Decision tree — which command do I run right now?

```
Question                                                              → Command
────────                                                              ─────────
Fuzzy idea, no doc yet?                                               → none — write docs/{slug}.md yourself first
Exploration doc exists, needs to become a design?                     → /design-explore
Design persisted, multi-step work with step > stage > phase?          → /master-plan-new
Design persisted, single issue is enough?                             → /project-new
Orchestrator exists, new exploration / extensions doc adds Steps?     → /master-plan-extend
Orchestrator exists, a skeleton Step needs decomposition?             → /stage-decompose
Orchestrator exists, a stage is ready to materialize?                 → /stage-file
Stage filed (N≥2) + plan-author + plan-digest + plan-review complete, ship Stage? → /ship-stage   (readiness gate + implement + verify + code-review + audit + closeout)
Single task (N=1) authored, drive end-to-end for one ISSUE_ID?        → /ship {ISSUE_ID}   (readiness gate + implement + verify + code-review + audit; no closeout)
Stage filed ad-hoc (N=1) or recovery authoring?                       → /author --task then /plan-digest --task (standalone)
Authored spec needs drift scan standalone?                            → /plan-review (standalone recovery; chained inside /stage-file by default)
Spec fully authored, ready to ship code?                              → /implement
Phase just landed, want a quick sanity pass?                          → /verify
Phase / stage / spec done, need full closed-loop + fix iter?          → /verify-loop
Bridge / batch evidence needed in isolation?                          → /testmode
Task verify green, need post-verify code review?                      → /code-review
All Stage Tasks PASS/minor, ready for Stage-scoped §Audit bulk?       → /audit
Stage audited, ready to archive + flip Status rollups?                → /closeout  (Stage-scoped; per-Task flow retired)
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
| Writes §Findings? | No | Yes (gate for `/audit` R11) |
| Output style | `verification-report` (JSON + caveman) | Same shape + `fix_iterations` / `verdict` / `human_ask` fields |
| When | Between phases, pre-PR sanity check | Pre-`/code-review`, pre-Stage-close, pre-umbrella-close |
| Composes | `validate:all` + compile gate + Path A OR Path B | `bridge-environment-preflight` + `project-implementation-validation` + `agent-test-mode-verify` + `ide-bridge-evidence` + `close-dev-loop` |

Both defer to the single canonical policy [`docs/agent-led-verification-policy.md`](agent-led-verification-policy.md) for timeout escalation, Path A lock release, and Path B preflight. Neither agent restates the policy.

---

## 6. Close seam — Stage-scoped `/closeout` only (rev 3 — no per-Task close)

| Aspect | `/closeout {PATH} {STAGE}` (Stage-scoped, rev 3) |
|--------|--------------------------------------------------|
| Fires | Once per Stage after `/audit` completes + all Task rows Status = `Done` |
| Touches | N per-Task: `ia/backlog/{id}.yaml` → `ia/backlog-archive/{id}.yaml`; `ia/projects/{id}.md` deleted; master-plan Task row flipped `Done → Done (archived)`; id purged from durable docs/code. Shared: Stage / Step / Plan Status rolled up per R3–R5; `materialize-backlog.sh` + `validate:all` run once at end. |
| Deletes spec? | Yes — per-Task spec files deleted after lessons migration to canonical IA |
| Touches BACKLOG? | Yes — `materialize-backlog.sh` regenerates `BACKLOG.md` + `BACKLOG-ARCHIVE.md` after Stage-level mutation loop |
| Confirmation gate? | No (gate removed post-TECH-88; pair `Plan` lives in master plan `§Stage Closeout Plan` — human-reviewable before applier dispatched) |
| Per-Task `/closeout`? | **Retired.** Per-Task closeout surface removed in T7.14 — the per-Task digest is an MCP-internal response consumed by `stage-closeout-applier` during the Stage loop. |

Retired close seams (post-M6): `project-stage-close` (non-terminal stage close) + `project-spec-close` (per-Task spec close) — both folded into Stage-scoped `/closeout` pair. Legacy `/closeout {ISSUE_ID}` invocation shape (arg = single id) rejects with migration message.

---

## 7. Re-entry and partial completion

Each seam is idempotent against its own output: running `/author` twice on an already-authored Stage re-applies canonical-term fold idempotently; running `/verify-loop` twice on an already-green branch re-emits the Verification block without fix iteration; running `/closeout` twice on an already-closed Stage no-ops (planner detects all Task rows Status = `Done (archived)` and exits clean).

Resume rule: on returning to a paused issue, run `/verify` first to re-establish branch state, then pick up at the seam after the last green handoff artifact.

Never reuse retired ids. The monotonic-per-prefix rule ([`AGENTS.md` §7](../AGENTS.md)) holds across `ia/backlog/` + `ia/backlog-archive/` (and their generated views `BACKLOG.md` + `BACKLOG-ARCHIVE.md`). Id reservation: `bash tools/scripts/reserve-id.sh {PREFIX}` (single) or MCP `reserve_backlog_ids` (batch) — never scan markdown views for max id.

### Crashed-closeout recovery

Stage-scoped `/closeout` applier acquires `flock ia/state/.closeout.lock` before Stage mutation loop begins. If applier crashes mid-loop, re-run `/closeout {PATH} {STAGE}` — planner re-scans Stage Task table Status column + already-archived yaml records, rebuilds `§Stage Closeout Plan` dropping already-applied tuples, and applier resumes from first unapplied tuple. The id-counter lock (`ia/state/.id-counter.lock`) is unaffected — closeout uses a separate `ia/state/.closeout.lock` (invariant #13 preserved).

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
