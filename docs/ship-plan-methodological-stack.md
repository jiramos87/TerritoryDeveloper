# Ship-plan methodological stack

> **Status:** authoritative reference for the ship-plan lifecycle protocol.
> **Audience:** agents authoring / executing master plans, skills, lifecycle scripts.
> **Caveman-tech voice** per `ia/rules/agent-output-caveman.md`.

Pillars below = mandatory unless skill body explicitly opts out. Each pillar carries enforcement surface (validator, hook, schema gate, MCP slice) so drift dies at CI, not in review.

---

## 1. TDD red/green

- Red-Stage Proof anchor — `{file}::{method}` declared in Task spec **before** impl; binds test to surface.
- Capture/finalize MCP pair — `red_stage_proof_capture` (failing baseline) → `red_stage_proof_finalize` (pass after impl).
- Anchor drift gate — `validate:red-stage-proof-anchor` checks method body keywords match anchor prose; blocks false-pass (test name aligns, body asserts wrong surface).
- Mandatory per-task — every Task spec ships a proof anchor, not optional.
- Stage-level red proof for player-visible delta; per-task red proof for algorithmic / branching cores.

## 2. Prototype-first

- Stage 1.0 tracer slice — mandatory vertical slice end-to-end before depth.
- §Visibility Delta — every Stage 2+ declares what's newly visible (player / agent surface).
- Seam validation early — wrong seam caught at slice, not after layer-by-layer build.
- Methodology rule-bound — `prototype-first-methodology.md` enforced via `validate:plan-prototype-first`.

## 3. Async cron processing

- `cron_*_enqueue` MCP family — agent fires + returns immediately; no blocking IO in inference path.
- Background drainer — launchd cron-server pulls queue, executes audit / journal / glossary / anchor jobs.
- Sync→async hard-cut — 5 legacy sync tools removed; equivalents only via cron.
- Stale-sweep + dashboard — visibility into stuck jobs, manual replay path.
- Decoupling — agent token budget protected from slow side-effects (audit, indexing, backlinks).

## 4. Data model — DB-as-source-of-truth

- Postgres holds specs / plans / tasks / catalogs; no `.md` mirror for live content.
- Single-write `master_plan_bundle_apply(jsonb)` — entire plan authored in one MCP call.
- Idempotent mutations — re-run resumes from DB state, no rollback gymnastics.
- File-system surfaces (`ia/projects/*.md`, `BACKLOG*.md`) regenerated from DB via `materialize-backlog.sh` + index regen.

## 5. Authoring discipline

- Glossary-driven terminology — pre-fetch `glossary_discover` + `glossary_lookup` once per plan; prevents synonym drift.
- Inline drift lint — synchronous check at digest write; relocates summary row to `master_plan_change_log` (kind `drift_lint_summary`).
- Surface routing — `router_for_task` + `invariants_summary` pre-load before authoring.
- Plan-digest contract — `§Goal` + `§Red-Stage Proof` + `§Work Items`, ~30 lines per task; anchor-resolved at write.

## 6. Execution shape

- Pair pattern (head/tail) — Opus planner + Sonnet applier; reasoning vs literal-apply, cost-tuned.
- Stage-atomic batching — one Stage = one inference = one commit. Failure ⇒ `ia_stages.status='partial'`; resume re-enters at first non-done task via DB query.
- Token-budget caps with fallback — `input_token_budget` frontmatter on caller-agent skills; over-cap auto-degrades to legacy two-pass (`/ship-stage-main-session`).
- Phase-checkpoint journal — `journal_append(payload_kind=phase_checkpoint)` mid-run; resume reader skips done phase_ids on chained child plans.

## 7. Verification

- Closed-loop chain — preflight → `validate:all` → compile → bridge preflight → smoke wired as one skill (`/verify-loop`).
- Schema-gate before mutate — `plan_apply_validate`, `validate:backlog-yaml`, `validate:fast --diff-paths`.
- Diff-scoped — validators only paths touched by stage commits (cumulative diff at stage close).
- Bounded fix→verify iteration — `MAX_ITERATIONS=2` per task; over budget = escalate.

## 8. Concurrency / parallelism

- Section claims (row-based, mig 0049/0052) — parallel agents on one umbrella plan; heartbeat sweep releases stale rows.
- Worktree isolation — risky composite skills (`/ship-cycle`, `/ship-plan`) in isolated git worktree.
- Per-domain lockfiles — `flock` only on dedicated lockfiles (`.id-counter.lock`, `.closeout.lock`, `.materialize-backlog.lock`, `.runtime-state.lock`); read-only validators skip.

## 9. Observability

- Audit trail (3-way) — `cron_audit_log` + `journal_append` + `master_plan_change_log`.
- Skill iteration log + `/skill-train` — friction retrospect → improvement proposals.
- Runtime state slice — `runtime_state` MCP exposes last `verify:local`, `bridge-preflight`, queued scenario.
- Phase checkpoints — visible in `ia_ship_stage_journal`; queryable for resume + audit.

---

## Cross-cutting

- **MCP-first.** Prefer `mcp__territory-ia__*` slice over full-file read. Order: `backlog_issue` → `router_for_task` → `glossary_*` → `spec_*` → `invariants_summary` / `rule_content`.
- **Cache-first preamble.** Tier 1 stable block reused across subagents (`ia/skills/_preamble/stable-block.md`); 5-min TTL discipline; F5 invalidation cascade fixed.
- **Caveman compression.** Agent prose max density per `ia/rules/agent-output-caveman.md`. Human-product register only for chat surface.
- **Trunk-based.** Single commit per stage, no merge gymnastics. PR optional; main branch is the integration line.

---

## Enforcement matrix

| Pillar | Validator / hook | MCP surface | Failure mode |
|---|---|---|---|
| TDD red/green | `validate:red-stage-proof-anchor` | `red_stage_proof_capture` / `_finalize` / `_get` / `_list` / `_mine` | CI red on anchor drift |
| Prototype-first | `validate:plan-prototype-first` | `master_plan_bundle_apply` schema | CI red on missing tracer / visibility delta |
| Async cron | `validate:cron-jobs-stale` | `cron_*_enqueue` family | Stale-sweep auto-fail row |
| DB-as-truth | `validate:dead-project-specs` | `master_plan_bundle_apply` / `task_batch_insert` | CI red on orphan `.md` |
| Authoring discipline | `plan_digest_lint` / glossary backlink check | `glossary_discover` / `_lookup` | Drift lint row in change_log |
| Execution shape | `validate:skill-drift` (`input_token_budget`) | `task_status_flip_batch` / `stage_closeout_apply` | Skill-drift CI fail |
| Verification | `validate:all` + `validate:fast` | `verify_classify` / `unity_compile` | Pass B verify-loop fails |
| Concurrency | `claims_sweep` cron | `section_claim` / `claim_heartbeat` | Stale row reclaim |
| Observability | dashboard + `journal_append` schema | `journal_append` / `cron_audit_log_enqueue` | Audit-row gap |

---

## Versioning

- Methodology version pinned to ship-protocol master plan (`ship-protocol`, `ship-protocol-v2`, …).
- New pillar added → ship as a Stage in current `ship-protocol-v{N+1}` chained child plan.
- Existing pillar deprecated → mark in this doc + open issue + retire validator only after one full sweep cycle.
