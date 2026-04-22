# Architecture Audit — Implementation Handoff

**Date authored:** 2026-04-22
**Precursor artifacts:**
- Audit plan: `/tmp/grid-asset-db-architecture-decisions-2026-04-22.md` (volatile; read once at session start)
- Change list: `docs/architecture-audit-change-list-2026-04-22.md` (canonical; read fully before acting)
- Project memory: `~/.claude/shared/territory-developer/memory/project_architecture_locks.md` — 13 locked decisions
- Memory index: `~/.claude/shared/territory-developer/memory/MEMORY.md`

---

## Mission

Implement the change list against HEAD of the current working branch. The audit phase is complete; this handoff covers execution only — no new scope decisions, no new locks.

**You are picking up at:** zero edits landed. User ratified 7 picks total on 2026-04-22 (recorded in the change list header line 6), including Pick 6A (delete all auth surface) and Pick 7 (drop Drizzle entirely from web). **No open picks.** Project memory lock #3 and #5 were amended accordingly.

---

## Read-first (prime context before any edit)

1. `docs/architecture-audit-change-list-2026-04-22.md` — full change list, all 7 backlog candidates, sequencing graph.
2. `~/.claude/shared/territory-developer/memory/project_architecture_locks.md` — the 13 locks. Every edit must pass the "does this violate a lock?" filter.
3. `web/lib/db/client.ts` — the driver-swap target (current Neon HTTP client).
4. `web/lib/db/schema.ts` — the delete target.
5. `web/app/api/auth/{login,logout,register,session}/route.ts` — four 501 stubs to delete.
6. `ia/skills/release-rollout/SKILL.md` — 11 lines cited explicitly in the change list §`Per repo area` → SKILL sweep.
7. `Assets/Scripts/Managers/GameManagers/MetricsRecorder.cs` — already exists; read before reassessing multi-scale T5.3.

Do not re-derive the audit. If something in the change list seems wrong, flag it to the user before diverging.

---

## Pre-flight

No open user picks. The 2026-04-22 amendments ratified Pick 6A (delete all auth surface) and Pick 7 (drop Drizzle entirely from web). Read the change list header line 6 for the full ratified set before acting. Execute the work queue directly; only surface to user if you find a lock conflict or drift not already documented.

---

## Work queue

Execute in the order below unless the user reprioritizes. Each row maps 1:1 to the "Ready-to-file backlog rows" section of the change list (rows 1-7).

| # | Row | Gate |
|---|---|---|
| 1 | CLEANUP / Driver swap (`web/lib/db/client.ts`) | `npm run validate:web` green |
| 2 | CLEANUP / Drop Drizzle entirely (delete `schema.ts` + `drizzle.config.ts` + `web/drizzle/` + `drizzle-orm`/`drizzle-kit` deps) | `validate:web` green; zero runtime call sites confirmed; lockfile pruned |
| 3 | CLEANUP / Delete all auth surface (4 routes + parent dir + login page + middleware + `DASHBOARD_AUTH_SKIP` + robots `/auth` disallow) | `validate:web` green |
| 4 | DOCS / Create `docs/db-boundaries.md` | ≤1 page; cross-link locks #2, #3, #4, #5; add mention in `web/README.md` |
| 5 | SKILL / Inline-replicate release-rollout SKILL over 11 lines | `grep -n 'stage-file' ia/skills/release-rollout/SKILL.md` shows only new Plan-Apply agent references |
| 6 | DRIFT / web-platform header sync (plan lines 13-24, tracker row 4) | Header + tracker agree; lines 16, 17 softened per locks #7, #8 |
| 7 | AUDIT / MetricsRecorder vs TECH-82 Phase 1 | T5.3 intent rewritten to "verify + document + fill gaps" if applicable |

---

## Per-row implementation notes

**Row 1 (Driver swap).** Preserve the `getSql()` + `sql` Proxy exports verbatim so downstream callers compile unchanged. Pick `postgres` (postgres-js) for ergonomic parity with tagged templates, or `pg` + `Pool` — flag which you picked to the user. After the edit, smoke `web/README.md` examples still describe reality; update line ~201 / ~207 narration if you diverged from tagged-template shape. Update the Decision Log in `web-platform-master-plan.md` with an **appended** row (do not rewrite the historical Neon pick at line 858).

**Row 2 (Drizzle drop).** Confirm zero runtime call sites first: `grep -rn "from '@/lib/db/schema'" web/`, `grep -rn '"./schema"' web/lib/db/`, and `grep -rn 'drizzle' web/ --include='*.ts' --include='*.tsx'`. README doc-example references don't count as call sites. Delete `web/lib/db/schema.ts` + `drizzle.config.ts` + `web/drizzle/`. Remove `drizzle-orm` and `drizzle-kit` from `web/package.json` deps, then prune lockfile (`yarn install` or `npm install`). Grid-asset Stage 1.2 will re-author catalog types as hand-written DTOs in `web/types/api/catalog*.ts`, not Drizzle schema.

**Row 3 (auth surface delete).** Pick 6A ratified — delete the full auth surface: four `/api/auth/*` routes + parent `auth/` dir, `web/app/auth/login/page.tsx`, `web/middleware.ts` (or `web/proxy.ts` under Next.js 16 — check both paths), all `DASHBOARD_AUTH_SKIP` env knob references, and the `/auth` disallow in `web/app/robots.ts`.

**Row 4 (db-boundaries doc).** New file. One page. State: (a) browser code must not import `web/lib/db/*`; (b) route handlers and MCP server are the only authorized DB clients; (c) migration authority = `db/migrations/*.sql` pure SQL; (d) Drizzle is a query mirror, never authoritative. Cross-link locks.

**Row 5 (SKILL sweep).** User pick (1c) = inline replication. 11 lines listed in change list §SKILL — treat as one cohesive rewrite, not 11 patches. Replace every `stage-file` subagent dispatch with explicit two-subagent sequencing against the current `.claude/commands/stage-file.md` Plan-Apply pair (`stage-file-plan` → `plan-author` review → `stage-file-apply` → optional `plan-fix-applier` → STOP). Validate with the grep listed in the change list.

**Row 6 (drift sync).** Read `full-game-mvp-rollout-tracker.md:51` + `web-platform-master-plan.md:1-24` + Decision Log around line 836, 858. Reconcile to a single canonical state. Likely answer: Steps 1-4 Final, Step 5 Done-but-revisit, Step 6 Final — but verify against code. Flag to user if uncertain.

**Row 7 (MetricsRecorder audit).** Read `MetricsRecorder.cs` + `db/migrations/0009_city_metrics_history.sql` + grep `tools/mcp-ia-server/` for `city_metrics_query`. Then rewrite `multi-scale-master-plan.md` Stage 5 T5.3 intent. Cross-reference citystats Stage 3 T3.3 which plans to rewire `MetricsRecorder.BuildPayload` via `_facade.SnapshotForBridge(tick)` — sequencing note only, no edit here.

---

## Parallelization

Independent (safe to run concurrently or in any order): rows 1, 4, 5, 6, 7. Row 3 is independent of 1/2 but blocked on Pick 6.

Serial: row 2 depends on row 1 (both must land before grid-asset Stage 1.1 or citystats Stage 8 file). Do not begin grid-asset or citystats stage-file dispatch — those are downstream of this handoff and belong to separate work.

---

## Validation gates

- After each `web/` edit: `npm run validate:web` (or the repo's equivalent — check `package.json` scripts at session start).
- After SKILL edit: the grep listed above.
- After all rows land: final `npm run validate:all` (or equivalent) + `git diff --stat` sanity pass.

---

## Out of scope

- Filing BACKLOG rows into `ia/backlog.yaml` (user drives that).
- Running `/stage-file` for grid-asset or citystats (downstream of this handoff).
- Committing (per global no-auto-commit rule — user commits).
- Promoting `/tmp/grid-asset-db-architecture-decisions-2026-04-22.md` into the repo (user pick 4c = keep as tmp).
- Any edit that violates a lock in `project_architecture_locks.md`. If tempted, stop and flag.

---

## Handoff closure

When all 7 rows are green and validated, reply to the user with:
- Which rows landed, which are deferred, and why.
- Any lock violations caught and avoided.
- Any drift discovered beyond what the change list already flagged (append to `Changelog` section of the change list doc).
- The Pick-6 resolution if it was surfaced mid-session.
