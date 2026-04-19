# Lifecycle Refactor — Rollout Tracker

> **Status:** Active — single-row retrospective tracker for the M0–M8 lifecycle refactor effort. Permanent artifact; NEVER closeable via `/closeout`. Hosts Skill Iteration Log aggregator for M8 dry-run findings (T8.1 / T8.1b / T8.4).
>
> **Scope:** Aggregates skill bugs / gaps surfaced during M0–M8 self-referential rollout dry-runs. Sibling to lifecycle-refactor master plan (already closed at M8 merge — `e0dc5db`). Not a multi-bucket umbrella; single retrospective row only.
>
> **Baseline SHA:** `e0dc5db` (chore(ia): close lifecycle-refactor Stage 9 — M8 merge complete, freeze lifted, Q9 telemetry tracker filed). All entries below post-baseline.
>
> **Read first if landing cold:**
> - [`docs/lifecycle-refactor-stage-8-dry-run-findings.md`](../../docs/lifecycle-refactor-stage-8-dry-run-findings.md) — full M8 findings doc (12 findings F1–F12, F8 retracted; Fix Table 11 rows).
> - [`ia/rules/agent-lifecycle.md`](../rules/agent-lifecycle.md) — canonical lifecycle flow + surface map.
> - [`ia/skills/release-rollout-skill-bug-log/SKILL.md`](../skills/release-rollout-skill-bug-log/SKILL.md) — dual-write helper.
>
> **Hard rules:**
> - Single-row tracker — NO multi-bucket matrix, NO (a)–(g) lifecycle columns. Lifecycle refactor M0–M8 already shipped; rows not applicable.
> - Skill Iteration Log aggregator below = ONLY active surface. Append-only.
> - Per-skill `## Changelog` in each `ia/skills/{name}/SKILL.md` = source of truth. Aggregator rows MUST link back.

---

## Row

| Row slug | Scope | Status | Anchor commit |
|----------|-------|--------|---------------|
| `m8-retrospective` | Stage 8 dry-run findings F1–F12 (F8 retracted) — skill bugs + process gaps surfaced during self-referential rollout | open (Fix Table rows landing in waves) | `e0dc5db` |

---

## Skill Iteration Log (aggregator)

Per-skill bug + fix detail lives in each `ia/skills/{name}/SKILL.md` §Changelog (per Q6 decision). Table below aggregates M8 retrospective entries only.

| Date | Skill | Rollout row | Bug / gap | Fix SHA | SKILL.md anchor |
|------|-------|-------------|-----------|---------|-----------------|
| 2026-04-19 | stage-file-apply | m8-retrospective | F1 — auto-chain inconsistency after `/stage-file` (half-chained through `/author` then stopped) | _pending_ | [`ia/skills/stage-file-apply/SKILL.md#changelog`](../skills/stage-file-apply/SKILL.md#changelog) |
| 2026-04-19 | stage-file-apply | m8-retrospective | F2 — wrong next-step suggestion (`/ship` instead of `/ship-stage` on multi-task Stage) | _pending_ | [`ia/skills/stage-file-apply/SKILL.md#changelog`](../skills/stage-file-apply/SKILL.md#changelog) |
| 2026-04-19 | project-new-apply | m8-retrospective | F2 sibling — N=1 path needed equivalent `/ship` chain dispatcher rule lock | _pending_ | [`ia/skills/project-new-apply/SKILL.md#changelog`](../skills/project-new-apply/SKILL.md#changelog) |
| 2026-04-19 | plan-author | m8-retrospective | F3 — Phase 4 fold missed 5 drift tuples (retired surface, template section, stale cross-ref, yaml errors) | _pending_ | [`ia/skills/plan-author/SKILL.md#changelog`](../skills/plan-author/SKILL.md#changelog) |
| 2026-04-19 | plan-review | m8-retrospective | F4 — refactor-in-flight sampling bias (3 of 5 tuples = lifecycle churn artifacts) | _pending_ | [`ia/skills/plan-review/SKILL.md#changelog`](../skills/plan-review/SKILL.md#changelog) |
| 2026-04-19 | plan-review | m8-retrospective | F5 — ≈30% of Stage-entry pipeline tokens; mechanical checks Opus-overkill | _pending_ | [`ia/skills/plan-review/SKILL.md#changelog`](../skills/plan-review/SKILL.md#changelog) |
| 2026-04-19 | stage-file-apply | m8-retrospective | F6 — Stage-entry friction: 3 commands across 2 CLI sessions; `/stage-start` candidate | _pending_ | [`ia/skills/stage-file-apply/SKILL.md#changelog`](../skills/stage-file-apply/SKILL.md#changelog) |
| 2026-04-19 | ship-stage | m8-retrospective | F7 — self-referential dry-run scope diverged from T8.1 external-plan intent | _pending_ | [`ia/skills/ship-stage/SKILL.md#changelog`](../skills/ship-stage/SKILL.md#changelog) |
| 2026-04-19 | ship-stage | m8-retrospective | F9 — clean end-to-end Stage chain ship (positive signal — validates rev-3 collapse) | _observed_ | [`ia/skills/ship-stage/SKILL.md#changelog`](../skills/ship-stage/SKILL.md#changelog) |
| 2026-04-19 | verify-loop | m8-retrospective | F10 — out-of-scope test-failure attribution worked correctly (positive signal) | _observed_ | [`ia/skills/verify-loop/SKILL.md#changelog`](../skills/verify-loop/SKILL.md#changelog) |
| 2026-04-19 | ship-stage | m8-retrospective | F11 — migration-JSON polling via ad-hoc python3 awkward; typed surface candidate | _pending_ | [`ia/skills/ship-stage/SKILL.md#changelog`](../skills/ship-stage/SKILL.md#changelog) |
| 2026-04-19 | ship-stage | m8-retrospective | F12 — STAGE_ID argument syntax drift (`8` vs `Stage 8` vs `8.1` ambiguous) | _pending_ | [`ia/skills/ship-stage/SKILL.md#changelog`](../skills/ship-stage/SKILL.md#changelog) |

Rollout agents append rows chronologically. Each entry MUST link to a per-skill `## Changelog` anchor.

---

## Change log

| Date | Note | Trigger |
|------|------|---------|
| 2026-04-19 | Initial scaffold — single-row m8-retrospective tracker created to host Stage 8 dry-run skill-bug aggregator (T8 Row 7). | M8 retrospective Fix Table Row 7 |
