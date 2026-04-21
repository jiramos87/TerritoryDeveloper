# Lifecycle Refactor — Stage 8 Dry-Run Findings

> **Date:** 2026-04-19
> **Branch:** `feature/lifecycle-collapse-cognitive-split`
> **Scope:** Analysis of observed friction while running Stage 8 entry (`/stage-file → /author → /plan-review`) on `ia/projects/lifecycle-refactor-master-plan.md`.
> **Status:** Findings-only. Freeze active (M8 pending). Fixes land post-M8 sign-off.
> **Upstream analysis:** `/plan-review` necessity review (same session) — genuine-niche-value case confirmed; Sonnet-downgrade still viable.

---

## Dispatch prompt — for fresh post-M8 agent session

> **User request (verbatim):** "append your prompt at the beggining of the findings doc"
>
> **Gate:** Do NOT run these fixes until lifecycle-refactor umbrella closeout (Stage 9) complete + M8 sign-off lifts freeze (CLAUDE.md §5). Stage 9 closeout happens on branch `feature/lifecycle-collapse-cognitive-split` before merge.

### Paste into fresh session after freeze lifts

```
Read docs/lifecycle-refactor-stage-8-dry-run-findings.md end-to-end. Execute the high-priority Fix Table rows in order:

  Row 1 — Strengthen plan-author Phase 4 canonical-term fold.
    - Load retired-surface tombstones (ia/skills/_retired/**, ia/commands/_retired/**, ia/agents/_retired/**) into Phase 4 scan.
    - Add template-section allowlist check (compare §-headers against ia/templates/project-spec-template.md).
    - Add cross-ref task-id resolver (verify all TECH-XXX / T-X.Y.Z refs resolve in current master plan + BACKLOG).
    - Patch ia/skills/plan-author/SKILL.md Phase 4.
    - Success = re-run against TECH-485..488 task bodies produces zero drift tuples for the 5 classes that slipped through (retired /enrich ref, §Closeout Plan shape, stale T4.1.3 ref, cross-ref yaml errors, retired-surface mention).

  Row 2 — Hard rule: N≥2 filed tasks → /ship-stage (never /ship).
    - Patch ia/skills/stage-file-apply/SKILL.md + ia/skills/project-new-apply/SKILL.md tail suggestions.
    - Patch subagent bodies stage-file-applier, project-new-applier.
    - Cross-reference `docs/agent-lifecycle.md` post-filing handoffs (rule already exists, implementation lags).
    - Success = stage-file-apply final output on multi-task stage suggests exactly `/ship-stage {MASTER_PLAN_PATH} {STAGE_ID}`.

  Row 3 — Auto-chain boundary decision (F1 resolution).
    - Decide: chain /stage-file → /author → /plan-review as single dispatch, OR stop at /stage-file tail and let user invoke each step.
    - Document decision in ia/rules/agent-lifecycle.md + CLAUDE.md §3 surface map.
    - If chain: patch stage-file-applier to auto-invoke plan-author + plan-reviewer; if stop: remove auto-chain to plan-author from current stage-file-applier.
    - Success = consistent UX across Stage 8 re-run (fresh test on unrelated master plan).

  Row 7 — Log Stage 8 dry-run findings via release-rollout-skill-bug-log helper.
    - For each finding F1..F12 (F8 retracted), call helper with SKILL_NAME=plan-author|plan-review|stage-file-apply|ship-stage as applicable.
    - TRACKER_SPEC = ia/projects/lifecycle-refactor-rollout-tracker.md (create if absent — single-row umbrella tracker for this refactor).
    - ROW_SLUG = m8-retrospective.
    - Dual-write pattern: per-skill Changelog + tracker Skill Iteration Log.

  Deferred rows 4, 5, 6, 9, 10, 11 = read-only; skim but do not execute this pass.

Do NOT run /ship-stage Stage 9 or /closeout against this branch — that already shipped. Re-run T8.1b external-plan sample (Row 9) only after Rows 1..3 + 7 land and verify chain.

Respect all invariants in ia/rules/invariants.md + terminology in ia/specs/glossary.md. Caveman output default per ia/rules/agent-output-caveman.md. After Rows 1..3 + 7 land, run npm run validate:all + /verify-loop --tooling-only, then commit + push.
```

---

## Session timeline observed

| Step | Command | Model | Tokens | Wall |
|------|---------|-------|--------|------|
| 1 | `/stage-file ia/projects/lifecycle-refactor-master-plan.md Stage 8` → stage-file-planner | Opus | 77.0k | 1m 40s |
| 2 | → stage-file-applier | Sonnet | 66.5k | 3m 32s |
| 3 | auto-chain → plan-author | Opus | 98.5k | 3m 30s |
| 4 | new CLI session — `/plan-review ia/projects/lifecycle-refactor-master-plan.md Stage 8` → plan-reviewer | Opus | 66.7k | 2m 14s |
| 5 | → plan-fix-applier | Sonnet | 36.2k | 1m 57s |
| **Total** | | | **~345k** | **~13 min** |

Filed: TECH-485, TECH-486, TECH-487, TECH-488.

Plan-review verdict: Fix — 5 tuples.

---

## Findings

### F1 — Auto-chain inconsistency after `/stage-file`

`/stage-file` auto-chained through `/author` then stopped. User opened a fresh CLI session to run `/plan-review`. Canonical flow per `ia/rules/agent-lifecycle.md`:

```
/stage-file → /author → /plan-review → [per-Task loop]
```

Current behaviour = half-chained. Either chain all the way through plan-review, or stop at `stage-file-apply` and let each subsequent command be invoked explicitly.

**Impact:** inconsistent UX; user cannot predict where the chain stops; extra context-setup cost when re-entering in a fresh CLI.

### F2 — Wrong next-step suggestion (recurrent)

Both sessions suggested `/ship TECH-485` (single-issue). Stage 8 has 4 filed tasks → correct default = `/ship-stage ia/projects/lifecycle-refactor-master-plan.md 8`.

User-memory entries already flag this class:

- `docs/agent-lifecycle.md` — after `/stage-file` or `/project-new`, suggest `/ship-stage` or `/ship`, never `/kickoff` alone.
- `.claude/commands/ship.md` (Next-handoff resolver) — after `/ship`, find next task via master-plan scan, not numeric adjacency.

Gap: neither memory hard-rules the **N≥2 → `/ship-stage`** branch. Subagent exit hand-offs don't check filed-task count on the owning Stage before emitting suggestion.

**Impact:** user has to catch the wrong suggestion every multi-task Stage. Silent miss = single-issue flow runs on Stage-scope work → per-Task Path B thrash, duplicate closeout attempts.

### F3 — `/plan-review` earned its keep, but flagged drift that plan-author should own

5 tuples applied:

1. retired `/enrich` → `/author --task {ISSUE_ID}` (plan-author copied tombstoned surface name into spec body).
2. `§Closeout Plan` → `§Stage Closeout Plan` (project-spec template shape drift).
3. stale `T4.1.3` → `T8.3` (pre-Step/Stage-collapse numbering leaked through).
4. cross-ref fix in `ia/backlog/TECH-485.yaml`.
5. cross-ref fix in `ia/backlog/TECH-488.yaml`.

All 5 = canonical-term / cross-ref drifts — the exact class `plan-author` Phase 4 ("canonical-term fold") is supposed to prevent.

Phase 4 gaps inferred:

- Does not load retired-surface tombstone list (e.g. `/enrich`, `project-spec-kickoff`, `project-stage-close`).
- Does not verify referenced section anchors against current `ia/templates/project-spec-template.md`.
- Does not resolve cross-ref Task ids against the current master plan (stale `T{step}.{stage}.{task}` format leaks through post-collapse).

### F4 — Refactor-in-flight sampling bias

3 of 5 tuples = lifecycle-refactor churn artifacts (retired commands, renumbered Stages, renamed sections). Steady-state `plan-review` yield on non-refactor work unknown. Caution against over-generalising "plan-review caught real bugs" → "plan-review load-bearing at steady state".

Re-measure `plan-review` yield on 2–3 post-M8 Stage entries before locking its role.

### F5 — Plan-review token cost ≈ 30% of Stage-entry pipeline

Plan-review = 66.7k Opus + 36.2k Sonnet ≈ 103k tokens (30% of 345k total). Most checks it ran = rule-book lookups (retired-term scan, section-name match, cross-ref id resolve), not synthesis. Opus overkill for mechanical checks.

Aligns with upstream necessity-analysis recommendation: **Sonnet-downgrade + conditional-gate on `plan-author` token-split**.

### F6 — Stage-entry requires 3 commands across 2 CLI sessions

User typed:

1. `/stage-file ia/projects/lifecycle-refactor-master-plan.md Stage 8` (auto-chains to /author)
2. `claude-personal "/plan-review ia/projects/lifecycle-refactor-master-plan.md Stage 8"` (fresh CLI)
3. `claude-personal "/ship-stage ia/projects/lifecycle-refactor-master-plan.md 8"` (after correction)

Candidate surface: `/stage-start {plan} {stage}` or `/ship-stage` front-end extension that covers entry (`stage-file → author → plan-review`) before handing off to the per-Task chain (`implement → verify-loop → code-review → audit → closeout`). Keeps human gates at author PASS + plan-review PASS.

### F7 — Self-referential dry-run scope diverged from T8.1 intent

T8.1 verbatim:

> Select a small pending Task from **any open master plan** (prefer _pending_, not In Progress); run `/plan-review` on its Stage → `/author --task {ISSUE_ID}` → `/implement` (no actual code ship; stop after plan-review + author).

Actually exercised:

- `/stage-file Stage 8` (filed TECH-485–488 into the lifecycle-refactor plan **itself**).
- Auto-chained `/author` on Stage 8.
- `/plan-review Stage 8`.

Dry-run target = Stage 8 of `lifecycle-refactor-master-plan.md` **itself** (self-referential — filing its own tasks). Stress-test coverage actually broader than T8.1 prescribed (5 subagent surfaces vs 3), but:

- No isolation from refactor-churn. F4 sampling bias amplified — the dry-run fixes refactor drift **as the refactor runs**.
- T8.1 intent explicitly wanted external, small, _pending_ scope in a different master plan. Not met.

Options:

1. Re-run T8.1 against an external plan (pick a _pending_ Task in another open master plan) for a clean yield sample.
2. Amend T8.1 intent to formally accept self-referential scope; document the bias in §Acceptance.
3. Fold current session into T8.1 as-is + add a follow-up task "T8.1b — external small-Task dry-run" to the orchestrator post-M8.

### F8 — Filed tasks show status `Done` immediately after filing

`ia/projects/lifecycle-refactor-master-plan.md` Stage 8 task table currently reads:

| Task | Issue | Status |
|------|-------|--------|
| T8.1 | TECH-485 | Done |
| T8.2 | TECH-486 | Done |
| T8.3 | TECH-487 | Done |
| T8.4 | TECH-488 | Done |

Expected per `ia/rules/orchestrator-vs-spec.md` R1/R2: filed rows start `Draft` → flip `In Review` on `/kickoff` → flip `In Progress` on `/implement` → flip `Done (archived)` on closeout.

Observed = all 4 `Done` with no implement / verify / close cycle run. Possible causes:

1. `plan-fix-applier` tuple mutated Status (pair-contract violation — applier must not flip lifecycle Status).
2. `stage-file-plan` planner seeded Status `Done` in tuples (planner bug — should seed `Draft`).
3. Manual edit outside the skill chain.

**Downstream blocker:** `/ship-stage` refuses to start on a Stage where all filed rows read `Done`. Dry-run cannot proceed past plan-review without resolving this.

**Triage step:** `git blame` Stage 8 task-table lines to pinpoint the Status-flip commit + responsible surface. Then either revert the flip (if bug) or document the flow (if intentional).

### F8 — RETRACTED (timing misread)

Status = `Done` observed on all 4 rows was **post-`/ship-stage`** state, not a pre-existing Status-flip bug. Dry-run continuation was not blocked. The `/ship-stage` run dispatched against Draft rows (confirmed by agent banner `"Tasks: TECH-485, TECH-486, TECH-487, TECH-488 (all Draft). Dispatching chain."`) and legitimately flipped them to Done via closeout-apply. Fix row 8 no longer a blocker — retain `git blame` step only as forensic exercise if interested.

### F9 — `/ship-stage` ran clean end-to-end (positive signal)

Single `/ship-stage ia/projects/lifecycle-refactor-master-plan.md 8` invocation:

- 68 tool uses, ~103.1k tokens, 8m 37s wall.
- 4 tasks shipped (TECH-485–488) through author → implement → verify-loop → code-review → audit → closeout.
- Stage verify passed (`validate:all` + `unity:compile-check` + `db:bridge-preflight`).
- All yamls archived to `ia/backlog-archive/`; project specs deleted.
- M7 flipped `done` in `ia/state/lifecycle-refactor-migration.json`.
- No pair-contract escalations; no per-Task gate failures.

Signal: the rev-3 Stage-scoped chain works end-to-end. Validates the lifecycle-refactor M6 collapse (no Phase layer, Stage-scoped bulk pair shape for author/audit/closeout).

### F10 — Out-of-scope test-failure attribution worked correctly

Verify surfaced 10× `BlipGoldenFixtureTests` + 3× `TreasuryFloorClampServiceTests` failures. Agent correctly:

- Attributed Blip failures → `ia/projects/blip-master-plan.md` (not the lifecycle-refactor scope).
- Attributed Zone-S failures → `ia/projects/zone-s-economy-master-plan.md`.
- Escalated per T8.4 bounded-fix rule instead of attempting remediation.

Matches `ia/rules/agent-tooling-hints.md` (failure ownership / open BACKLOG check). No action.

### F11 — Migration-JSON polling via ad-hoc `python3 -c` is awkward

Agent ran 4 Bash calls to inspect `ia/state/lifecycle-refactor-migration.json` phases section:

1. `python3 -c "...print(json.dumps({k:v for k,v in d.items() if k.startswith('M') or k==..."` — empty output (filter predicate incomplete).
2. `python3 -c "...print(list(d.keys()))"` — key-enumeration probe.
3. `python3 -c "...print(json.dumps({k:v.get('done') if isinst..."` — null-heavy output (truthy key absent in some phase records).
4. `python3 -c "...print(json.dumps(d['phases'], indent=2))" | head -80` — finally yielded phases dump.

Pattern = trial-and-error. Low priority, but candidate for a typed surface:

- MCP tool `lifecycle_migration_status {phase?}` returning `{phase_id, status, flipped_at, notes}`.
- OR `jq '.phases | to_entries | map({phase: .key, status: .value.status})'` documented in `ia/skills/ship-stage/SKILL.md` §evidence gathering.

### F12 — Next-step argument syntax drift

Agent emitted:

```
claude-personal "/ship-stage ia/projects/lifecycle-refactor-master-plan.md Stage 9"
```

Original user invocation used bare numeric: `/ship-stage ia/projects/... 8`. Now suggestion uses `Stage 9` (word + number). `/ship-stage` subagent description = `{MASTER_PLAN_PATH} {STAGE_ID}` — `STAGE_ID` format spec ambiguous (`8` vs `8.1` vs `Stage 8` vs `Stage 8.1`).

Fix: lock `STAGE_ID` format in `.claude/agents/ship-stage.md` frontmatter description + reject ambiguous forms at argument parse. Also align all subagent suggestion prose to the locked format.

Confirmed Stage 9 exists at `ia/projects/lifecycle-refactor-master-plan.md:493` — `### Stage 9 — Validation + Merge / Sign-Off + Merge`. Suggestion semantically valid; only the syntax is drifted.

---

## Fixes

| # | Fix | Target surface | Priority | Landing window |
|---|-----|----------------|----------|----------------|
| 1 | Strengthen `plan-author` Phase 4 canonical-term fold: load retired-surface tombstones, current template section allowlist, cross-ref task-id resolver against owning master plan | `ia/skills/plan-author/SKILL.md` | High | post-M8 |
| 2 | Hard rule in subagent exit hand-off: detect filed-task count on owning Stage; N≥2 → suggest `/ship-stage {plan} {stage}`; N=1 → `/ship {id}` | `.claude/agents/stage-file-applier.md`, `.claude/agents/plan-author.md`, `.claude/agents/plan-fix-applier.md` | High | post-M8 |
| 3 | Decide auto-chain boundary: either chain through `/plan-review` or stop at `stage-file-apply` — document the chosen contract in `agent-lifecycle.md` | `.claude/commands/stage-file.md` + `ia/rules/agent-lifecycle.md` + subagent hand-off prose | Medium | post-M8 |
| 4 | Sonnet-downgrade candidate for `plan-review` mechanical checks (retired-term scan, section-name match, cross-ref resolve); keep Opus only for cross-sub-pass coherence when plan-author split | `ia/skills/plan-review/SKILL.md`, `.claude/agents/plan-reviewer.md` | Medium | post-M8, post-yield-remeasurement |
| 5 | Conditional-gate `plan-review`: skip when `plan-author` stayed single-pass; auto-PASS sentinel under Stage block | `ia/skills/plan-review/SKILL.md` Phase 0 | Medium | post-M8 |
| 6 | Consider `/stage-start {plan} {stage}` orchestrator collapsing entry path into one command | new skill OR extend `/ship-stage` | Low | post-M8, scope discussion first |
| 7 | Log F1–F5 as per-skill Changelog entries via `release-rollout-skill-bug-log` (user-gate channel) | `ia/skills/{plan-author,plan-review,stage-file-plan,stage-file-apply}/SKILL.md` §Changelog | High | post-M8 |
| 8 | ~~`git blame` Stage 8 task-table lines~~ RETRACTED (F8 retraction). Optional forensic only — not blocking | — | ~~Blocker~~ N/A | retained as historical note |
| 9 | Re-run T8.1 against an external open master plan with a _pending_ Task for steady-state `plan-review` yield sample (F4/F5 re-measurement) | new follow-up Task `T8.1b` under Stage 8 OR T8.1 §Acceptance amendment | Medium | post-M8 |
| 10 | Lock `STAGE_ID` argument format for `/ship-stage` (pick one: bare `8` / dotted `8.1` / reject `Stage 8` word prefix); align all subagent exit-hand-off suggestion prose | `.claude/agents/ship-stage.md` + all subagents emitting next-step prompts | Low | post-M8 |
| 11 | Typed migration-JSON status surface — MCP tool `lifecycle_migration_status` OR documented `jq` pattern in `ship-stage` SKILL §evidence gathering | `tools/mcp-ia-server/` + `ia/skills/ship-stage/SKILL.md` | Low | post-M8, optional |

---

## Not-actions (deliberate non-fixes)

- **Don't retire `/plan-review` entirely.** Dry-run validated genuine yield on refactor-churn drift. Even steady-state, the cross-sub-pass coherence role when `plan-author` splits is load-bearing.
- **Don't inline the 5 fixes into per-subagent memory hacks.** Root cause = Phase 4 fold incomplete + hand-off suggester blind to filed-task count. Treat as skill-level bugs, not behavioural patches.
- **Don't patch during freeze.** M8 sign-off gates any non-refactor skill edits.

---

## Cross-references

- `ia/projects/lifecycle-refactor-master-plan.md` — umbrella; Stage 8 under active dry-run.
- `ia/rules/agent-lifecycle.md` — canonical ordered flow.
- `ia/rules/plan-apply-pair-contract.md` — seam #1 `plan-review → plan-fix-apply`.
- `ia/skills/plan-author/SKILL.md` §Phase 4 — canonical-term fold (F3 target).
- `ia/skills/plan-review/SKILL.md` — Sonnet-downgrade candidate (F5).
- `docs/lifecycle-opus-planner-sonnet-executor-exploration.md` §Motivating observations #2 — original plan-review rationale.
- Prior signal on F2: `docs/agent-lifecycle.md` (post-filing handoffs) + `.claude/commands/ship.md` next-handoff resolver.
