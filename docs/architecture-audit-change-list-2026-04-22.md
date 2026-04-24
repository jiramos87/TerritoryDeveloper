# Architecture Audit — Implementation Plan (2026-04-22)

Sequential, pre-digested change list for a single-pass implementation by a composer-2-class agent. Run steps in order; do not skip. Each step has a gate command — STOP at the first red gate and surface to the user.

All paths below are **repo-relative to `/Users/javier/bacayo-studio/territory-developer/`**.

---

## Progression checklist

- [ ] **Step 1** — Driver swap in `web/lib/db/client.ts` (Neon HTTP → postgres-js)
- [ ] **Step 2** — Drop Drizzle from `web/` (delete `schema.ts` + `drizzle.config.ts` + `web/drizzle/`; remove `drizzle-orm` + `drizzle-kit` deps; regen lockfile)
- [ ] **Step 3** — Delete the auth surface (4 API routes + parent dir + login page + `web/proxy.ts` + `DASHBOARD_AUTH_SKIP` refs + robots `/auth` disallow + `.env.local.example` line + README sections)
- [ ] **Step 4** — Create `docs/db-boundaries.md` + cross-link from `web/README.md`
- [ ] **Step 5** — Inline-replicate `stage-file` dispatches in `ia/skills/release-rollout/SKILL.md` (5 targeted edits)
- [ ] **Step 6** — Reconcile web-platform status drift (`ia/projects/web-platform-master-plan.md` header lines + rollout tracker row 4)
- [ ] **Step 7** — Rewrite `multi-scale-master-plan.md` Stage 5 T5.3 intent (implementation already landed → verify+document)
- [ ] **Final gate** — `npm run validate:web`, `npm --prefix web run build`, sanity greps

---

## Step 1 — Driver swap in `web/lib/db/client.ts`

**Goal:** replace `@neondatabase/serverless` with `postgres` (postgres-js) while preserving the `getSql()` + `sql` tagged-template exports so downstream callers compile unchanged.

**1a.** Add `postgres` as a dependency (and keep the edit in-tree so Step 2's `npm install` picks it up in the same lockfile regen). Edit `web/package.json` dependencies:

- **Remove** this line from `dependencies`:
  ```
  "@neondatabase/serverless": "^1.0.2",
  ```
- **Add** this line to `dependencies` (alphabetical order, before `d3`):
  ```
  "@next/mdx": "^16.2.3",
  "d3": "^7.9.0",
  "postgres": "^3.4.4",
  ```
  (Insert `"postgres": "^3.4.4",` between `"lucide-react"` and `"next"`.)

**1b.** Overwrite `web/lib/db/client.ts` with exactly this content:

```ts
// Lazy singleton — avoids build-time connection + repeat init.
import postgres, { type Sql } from 'postgres';

let _sql: Sql | null = null;

export function getSql(): Sql {
  if (_sql) return _sql;
  const url = process.env.DATABASE_URL;
  if (!url) throw new Error('DATABASE_URL not set — required for DB access.');
  _sql = postgres(url);
  return _sql;
}

// Re-export as `sql` for tagged-template ergonomics at call sites.
export const sql = new Proxy({} as Sql, {
  get: (_t, prop) => Reflect.get(getSql() as object, prop),
  apply: (_t, _thisArg, args) => (getSql() as unknown as (...a: unknown[]) => unknown)(...args),
});
```

**Gate (after Step 2 lockfile regen, not here):** deferred to Step 2's gate, since `typecheck` requires `postgres` to be actually installed.

---

## Step 2 — Drop Drizzle entirely from `web/`

**Goal:** remove Drizzle schema + config + generated output + deps. Confirm zero runtime call sites before deleting.

**2a.** Run these grep guards. All must return **zero** matches (README doc-example hits don't count — those are handled in Step 3 README edits):

```bash
grep -rn "from '@/lib/db/schema'" web/ --include='*.ts' --include='*.tsx'
grep -rn "from '\./schema'" web/lib/db/ --include='*.ts'
grep -rn "drizzle-orm" web/ --include='*.ts' --include='*.tsx'
grep -rn "drizzle-kit" web/ --include='*.ts' --include='*.tsx'
```

If any **code** match appears (ignore `web/lib/db/schema.ts` self-import and `web/drizzle.config.ts` self-import — both will be deleted next), STOP and surface.

**2b.** Delete these paths:

```bash
rm web/lib/db/schema.ts
rm web/drizzle.config.ts
rm -rf web/drizzle/
```

**2c.** Edit `web/package.json`:

- In `dependencies`: **remove** the line `"drizzle-orm": "^0.45.2",`.
- In `devDependencies`: **remove** the line `"drizzle-kit": "^0.31.10",`.
- In `scripts`: **remove** the line `"db:generate": "drizzle-kit generate",`. (Mind the trailing comma on the preceding line after removal.)

**2d.** Regenerate the lockfile + install `postgres` from Step 1:

```bash
cd web && npm install && cd ..
```

**Gate:**

```bash
npm run validate:web
```

Must exit 0 (lint + typecheck + vitest all green). If typecheck complains about `Sql` import, confirm `postgres` version `^3.4.4` installed (`npm ls postgres --prefix web`).

---

## Step 3 — Delete the auth surface

**Goal:** remove every trace of auth UI + middleware gating + env bypass knob. After this step the `/dashboard` route is openly accessible on localhost.

**3a.** Delete the four 501-stub route handlers + their parent dir:

```bash
rm web/app/api/auth/login/route.ts
rm web/app/api/auth/logout/route.ts
rm web/app/api/auth/register/route.ts
rm web/app/api/auth/session/route.ts
rmdir web/app/api/auth/login web/app/api/auth/logout web/app/api/auth/register web/app/api/auth/session
rmdir web/app/api/auth
```

**3b.** Delete the login page + its parent dirs:

```bash
rm web/app/auth/login/page.tsx
rmdir web/app/auth/login
rmdir web/app/auth
```

**3c.** Delete the middleware file:

```bash
rm web/proxy.ts
```

(Note: this repo is on Next.js 16 — the file is `web/proxy.ts`, not `web/middleware.ts`. No `web/middleware.ts` exists; do not attempt to delete it.)

**3d.** Edit `web/app/robots.ts` — remove `'/auth'` from the `disallow` array. The file becomes:

```ts
import type { MetadataRoute } from 'next';
import { getBaseUrl } from '@/lib/site/base-url';

export default function robots(): MetadataRoute.Robots {
  const base = getBaseUrl();
  return {
    rules: {
      userAgent: '*',
      allow: '/',
      disallow: ['/design', '/dashboard'],
    },
    sitemap: `${base}/sitemap.xml`,
  };
}
```

**3e.** Edit `web/.env.local.example` — **delete** the line:

```
DASHBOARD_AUTH_SKIP=
```

(File is committed; other lines stay untouched.)

**3f.** Best-effort: also delete the line `DASHBOARD_AUTH_SKIP=1` from `web/.env.local` if the file is present in the workspace (it is gitignored; may not exist in CI/composer-2 workspace — skip silently if the file is absent).

**3g.** Edit `web/README.md`:

- **In the `## Routes` table** (around line 75) — change the row:
  ```
  | `/dashboard` | Master-plan progress dashboard | gated (bypass via `DASHBOARD_AUTH_SKIP=1`) | RSC |
  ```
  to:
  ```
  | `/dashboard` | Master-plan progress dashboard | none (MVP — open on localhost) | RSC |
  ```
  Also change rows:
  ```
  | `/dashboard/releases` | Release picker | gated (TECH-358 matcher) | RSC |
  | `/dashboard/releases/:releaseId/progress` | Release progress tree | gated (TECH-358 matcher) | RSC + `PlanTree` Client island |
  ```
  to:
  ```
  | `/dashboard/releases` | Release picker | none (MVP — open on localhost) | RSC |
  | `/dashboard/releases/:releaseId/progress` | Release progress tree | none (MVP — open on localhost) | RSC + `PlanTree` Client island |
  ```
- **Delete** the sentence right after the routes table: `Auth gate for /dashboard* inherits from proxy matcher widened in TECH-358.`

- **In the `## Portal` section** (starting around line 186) — delete from the `## Portal` heading through the end of the `### Migration tooling` subsection, inclusive of the `---` rule and the `**Step 5 boundary:** ...` line that precedes `## Tokens`. In other words: remove the entire block between (exclusive) the preceding `## ...` heading and (exclusive) the next `## Tokens` heading.

  Replace that deleted block with this minimal replacement:

  ```markdown
  ## Portal

  App-infra surface — Postgres wired at Step 5 via `web/lib/db/client.ts` (lazy singleton, postgres-js driver). Architecture-only tier: no migrations run from web, no auth flow. Authoritative migrations live under `db/migrations/*.sql` (pure SQL). See [`docs/db-boundaries.md`](../docs/db-boundaries.md) for the browser ↔ DB ↔ MCP boundary rule.

  ### Connection pattern

  Lazy singleton via `getSql()` — connection not opened at import time; opened on first call. `sql` tagged-template Proxy delegates to `getSql()` on first use.

  ```ts
  import { sql } from '@/lib/db/client'

  // Lazy — no connection until this line executes:
  const rows = await sql`SELECT 1 AS ping`
  ```

  Build-time safety: `next build` succeeds without `DATABASE_URL` set — client module imports cleanly; error thrown only on first query at runtime.

  Source: `web/lib/db/client.ts`.

  ### DATABASE_URL env contract

  Required format: any standard Postgres connection string (`postgresql://user:pass@host:5432/db`). Local contributors create `web/.env.local` (gitignored) with `DATABASE_URL=...`. No Vercel env wiring required at MVP (localhost-only critical path).
  ```

- **Delete** the entire `## Local development auth bypass` section (around line 438) — from that heading through (exclusive of) the next `## Backend logic / frontend render boundary` heading.

**3h.** Confirm zero lingering references:

```bash
grep -rn "DASHBOARD_AUTH_SKIP\|portal_session\|/api/auth\|app/auth/login\|@neondatabase" web/ --include='*.ts' --include='*.tsx' --include='*.md' --include='*.json' --include='*.example'
```

Must return **zero** non-`.next/` matches. (Ignore any `web/.next/` cache hits; that directory is generated and regenerates on next build.)

**Gate:**

```bash
npm run validate:web
```

Must exit 0.

---

## Step 4 — Create `docs/db-boundaries.md`

**Goal:** short (≤1 page) house-rule doc fixing the browser ↔ DB ↔ MCP boundary.

**4a.** Create `docs/db-boundaries.md` with exactly this content:

```markdown
# DB boundaries (2026-04-22)

House rule for this repo. Short. No exceptions without an amendment here.

## Who talks to Postgres

| Caller | Allowed? | Via |
|---|---|---|
| Browser code (`web/app/**/page.tsx` Client Components, `web/components/**`) | NO | Must go through a route handler |
| Next.js route handlers (`web/app/api/**/route.ts`) | YES | `web/lib/db/client.ts` → `getSql()` or `sql` tagged template |
| React Server Components (`page.tsx` without `'use client'`) | YES | Same as above — they run server-side |
| MCP server (`tools/mcp-ia-server/**`) | YES | Its own pooled client; never imports from `web/` |
| Unity runtime (C#) | NO (direct) | Fire-and-forget Node bridge (`tools/postgres-ia/*.mjs`) |

## Migration authority

- **Source of truth:** pure `.sql` files under `db/migrations/`.
- Migration runner lives under `tools/postgres-ia/` (Node).
- **No ORM migrations.** Drizzle has been dropped from `web/` (2026-04-22). If you need a type for a query row, hand-write a DTO under `web/types/api/**` (or colocated beside the route handler) and optionally validate with zod at the route-handler boundary.

## DTO authoring pattern

- Route handlers under `web/app/api/**` define per-endpoint DTOs in `web/types/api/*.ts` (or a colocated `types.ts`).
- Browser code imports the **same** DTO types when typing `fetch()` responses — single source of truth per endpoint.
- MCP server follows the same pattern for its tool inputs/outputs.
- If internal row-type inference from the live DB becomes painful later, bolt on `pg-to-ts` codegen — **do not** re-introduce an ORM.

## Enforcement

- `web/lib/db/client.ts` is the only authorized DB client module for `web/`. Grep for `from '@/lib/db/client'` — every hit must be inside a server-only file (route handler or RSC).
- Breaking this boundary = breaking the MVP localhost-only hosting lock.
```

**4b.** Cross-link from `web/README.md`. In the `## Portal` block rewritten in Step 3g, the pointer to `docs/db-boundaries.md` is already in the first paragraph — no extra edit needed.

**Gate:**

```bash
test -f docs/db-boundaries.md && echo OK
grep -q "db-boundaries.md" web/README.md && echo OK
```

Both must print `OK`.

---

## Step 5 — Inline-replicate `stage-file` dispatches in `ia/skills/release-rollout/SKILL.md`

**Goal:** replace every place the SKILL dispatches a `stage-file` subagent (a retired wrapper) with explicit sequential dispatch of the real agent chain that `.claude/commands/stage-file.md` runs: `stage-file-planner` → `stage-file-applier` → `plan-author` → `plan-reviewer` (→ `plan-applier` Mode plan-fix on critical, cap=1).

**Do not touch** slash-command references (`/stage-file` as a user command is still valid). **Do not touch** the Changelog section. 5 targeted edits total.

**5a.** Edit **line 28** (the dispatch chain narration). Find:

```
`design-explore` → `master-plan-new` → `master-plan-extend` → `stage-decompose` → `stage-file-plan` + `stage-file-apply` → `project-new` → `project-spec-implement` → `/closeout` (Stage-scoped). Release-rollout sits ABOVE this chain — it does not replace it; it sequences multiple child chains under one umbrella.
```

Replace with:

```
`design-explore` → `master-plan-new` → `master-plan-extend` → `stage-decompose` → `stage-file-planner` → `stage-file-applier` → `plan-author` → `plan-reviewer` (→ `plan-applier` on critical, cap=1) → `project-new` → `project-spec-implement` → `/closeout` (Stage-scoped). Release-rollout sits ABOVE this chain — it does not replace it; it sequences multiple child chains under one umbrella.
```

**5b.** Edit **Phase 4 autonomous-chain sub-step** (currently around lines 92–96). Find the block:

```
**Autonomous chain (b) ✓ path:**
1. Call Agent `master-plan-new` subagent with exploration doc path.
2. Wait for success. Read authored plan to find first Stage (Stage 1.1 or equivalent).
3. Call Agent `stage-file` subagent for that Stage.
4. Wait for success → (f) ✓. Proceed to Phase 5.
```

Replace with:

```
**Autonomous chain (b) ✓ path:**
1. Call Agent `master-plan-new` subagent with exploration doc path.
2. Wait for success. Read authored plan to find first Stage (Stage 1.1 or equivalent).
3. Dispatch the `/stage-file` agent chain against that Stage — sequence these four (or five on critical) subagents in order, waiting for each to return before dispatching the next: (i) `stage-file-planner` → (ii) `stage-file-applier` → (iii) `plan-author` → (iv) `plan-reviewer`. If `plan-reviewer` returns PASS → proceed to Phase 5. If `plan-reviewer` returns critical → (v) dispatch `plan-applier` Mode plan-fix, then re-dispatch `plan-reviewer` once (cap=1). Second critical → abort chain + surface to user.
4. Wait for PASS → (f) ✓. Proceed to Phase 5.
```

**5c.** Edit the **dispatch matrix row (f)** (currently around line 113). Find:

```
| (f) | Agent `stage-file` subagent (Stage resolved from child plan first Stage) | No |
```

Replace with:

```
| (f) | Sequential chain: Agent `stage-file-planner` → Agent `stage-file-applier` → Agent `plan-author` → Agent `plan-reviewer` (→ Agent `plan-applier` on critical, cap=1). Stage resolved from child plan first Stage. | No |
```

**5d.** Edit the **chain narration template** (currently around line 121–123). Find:

```
{ROW_SLUG} → (f) ✓. chain: master-plan-new ({doc}) → stage-file ({Stage N.M}, {issue-ids}). Tier: {A|B|C|D|E}. Next-row recommendation below.
```

Replace with:

```
{ROW_SLUG} → (f) ✓. chain: master-plan-new ({doc}) → stage-file-planner → stage-file-applier → plan-author → plan-reviewer ({Stage N.M}, {issue-ids}). Tier: {A|B|C|D|E}. Next-row recommendation below.
```

**5e.** Edit the **Seed prompt Phase 4 narration** (currently around line 208). Find:

```
Phase 4: when (b) ✓ → autonomous chain (c)→(f) via Agent tool (master-plan-new → read first Stage → stage-file); human pause ONLY for incomplete (b), ⚠️, ❓, or subagent failure.
```

Replace with:

```
Phase 4: when (b) ✓ → autonomous chain (c)→(f) via Agent tool (master-plan-new → read first Stage → stage-file-planner → stage-file-applier → plan-author → plan-reviewer, with plan-applier on critical, cap=1); human pause ONLY for incomplete (b), ⚠️, ❓, or subagent failure.
```

**Gate:**

```bash
grep -n 'Agent .stage-file.[^-]' ia/skills/release-rollout/SKILL.md
```

Must return **zero** matches (the pattern catches `Agent 'stage-file'` or `Agent \`stage-file\`` followed by a non-hyphen — i.e. the old wrapper name, not the new hyphenated agent names).

```bash
grep -cE 'stage-file-planner|stage-file-applier|plan-author|plan-reviewer' ia/skills/release-rollout/SKILL.md
```

Must print a count ≥ 4. If either gate fails, re-open the 5 edits.

---

## Step 6 — Reconcile web-platform status drift

**Goal:** align the `web-platform-master-plan.md` header with reality after Steps 1–3 retired Step 5 portal outputs, and flip the rollout tracker row 4 cell (d) accordingly.

**6a.** Edit `ia/projects/web-platform-master-plan.md` **line 5**. Find:

```
> **Status:** MVP Done 2026-04-17 — Steps 1–6 all Final (Step 5 portal stages 5.1 + 5.2 + 5.3 all Done 2026-04-17; Step 6 all three stages Done 2026-04-17). Post-MVP extensions now tracked in companion doc `docs/web-platform-post-mvp-extensions.md` — ready for `/design-explore` poll-based expansion + `/master-plan-extend` Step 7+.
```

Replace with:

```
> **Status:** MVP Done 2026-04-17 — Steps 1–4 + 6 Final. Step 5 Done 2026-04-17 but architecture outputs (Drizzle schema, `/api/auth/*` stubs, auth middleware + login page, `DASHBOARD_AUTH_SKIP` bypass) retired 2026-04-22 per architecture audit (`docs/architecture-audit-change-list-2026-04-22.md`); Postgres driver swapped from `@neondatabase/serverless` to `postgres`-js. Post-MVP extensions now tracked in companion doc `docs/web-platform-post-mvp-extensions.md` — ready for `/design-explore` poll-based expansion + `/master-plan-extend` Step 7+.
```

**6b.** Edit `ia/projects/web-platform-master-plan.md` **line 16** (inside the Locked decisions block). Find:

```
> - Hosting: Vercel free tier. Build root `web/`. Deploy on push to `main`.
```

Replace with:

```
> - Hosting: Vercel free tier. Build root `web/`. Vercel preview deploys optional; MVP critical path is localhost build (2026-04-22 audit — localhost-only MVP lock).
```

**6c.** Edit `ia/projects/web-platform-master-plan.md` **line 17**. Find:

```
> - Auth (W7): roll-own JWT + sessions. No third-party auth provider.
```

Replace with:

```
> - Auth (W7): deferred entirely per 2026-04-22 audit — no `/api/auth/*` and no auth UI surface in MVP. If/when portal re-enters scope, roll-own JWT + sessions remains the locked preference (not re-decide); no third-party auth provider.
```

**6d.** Edit `docs/full-game-mvp-rollout-tracker.md` — row 4 (`web-platform`), column (d). Find the cell text:

```
✓ (Steps 1–4 Final; 5–6 Paused)
```

(in the row starting `| 4 | A | 9 | web-platform |` around line 51). Replace with:

```
✓ (Steps 1–4 + 6 Final; Step 5 Done 2026-04-17 with architecture outputs retired 2026-04-22 per audit — see `docs/architecture-audit-change-list-2026-04-22.md`)
```

**6e.** Append a Skill Iteration Log row to `docs/full-game-mvp-rollout-tracker.md` noting the 2026-04-22 audit retirement. Locate the `## Skill Iteration Log` section (or equivalent change log table near the bottom of the tracker) and append a single-line entry:

```
| 2026-04-22 | web-platform | Architecture audit: Neon driver swapped → postgres-js; Drizzle dropped; auth surface deleted. Row 4 (d) cell updated. | architecture audit |
```

If the log table column shape differs, match it — minimum field: date, row slug, one-line delta. If no log table exists, create one under a new `## Skill Iteration Log` heading at the end of the tracker with header row `| Date | Row | Delta | Author |` and this entry.

**Gate:**

```bash
grep -q "retired 2026-04-22" ia/projects/web-platform-master-plan.md && echo OK
grep -q "retired 2026-04-22" docs/full-game-mvp-rollout-tracker.md && echo OK
```

Both must print `OK`.

---

## Step 7 — Rewrite `multi-scale-master-plan.md` Stage 5 T5.3 intent

**Goal:** T5.3 (TECH-292) "MetricsRecorder Phase 1 integration" reads like new authoring work, but all three Phase-1 acceptance criteria are already landed. Rewrite the intent so the task scope is verify+document.

Evidence (already verified, do not re-verify in this step):
- `Assets/Scripts/Managers/GameManagers/MetricsRecorder.cs` exists and fires per-tick via `RecordAfterSimulationTick()`.
- `db/migrations/0009_city_metrics_history.sql` exists.
- `tools/mcp-ia-server/src/tools/city-metrics-query.ts` registers `city_metrics_query` tool.

**7a.** Edit `ia/projects/multi-scale-master-plan.md`, T5.3 row (currently around line 175). Find the cell text after `Draft |`:

```
`MetricsRecorder.cs` (new) fires fire-and-forget per `SimulationManager` tick; `db/migrations/` `city_metrics_history` schema + bridge scripts; `mcp__territory-ia__city_metrics_query` tool per `ia/projects/TECH-82.md` Phase 1 acceptance. Scope-slice of **TECH-82** — does NOT subsume TECH-82 Phases 2–4.
```

Replace with:

```
Verify + document TECH-82 Phase 1 integration (all three acceptance criteria already landed as of 2026-04-22 audit): (1) `Assets/Scripts/Managers/GameManagers/MetricsRecorder.cs` fires fire-and-forget per `SimulationManager` tick — present; (2) `db/migrations/0009_city_metrics_history.sql` applied — present; (3) `mcp__territory-ia__city_metrics_query` tool returns time-series — present at `tools/mcp-ia-server/src/tools/city-metrics-query.ts`. Task scope = verification pass + acceptance-criteria sign-off in closeout notes, not new authoring. Cross-ref: citystats Stage 3 T3.3 plans to rewire `MetricsRecorder.BuildPayload` via `CityStatsFacade.SnapshotForBridge(tick)` — sequencing note, no edit here. Scope-slice of **TECH-82** — does NOT subsume TECH-82 Phases 2–4.
```

**7b.** Also edit the Stage 5 **Objectives** line (currently around line 158). Find:

```
**Objectives:** City tick profiled; egregious non-BUG-55 allocators patched; `MetricsRecorder` Phase 1 integrated (game remains playable without Postgres); EditMode tick budget test establishes Step 3 parity baseline.
```

Replace with:

```
**Objectives:** City tick profiled; egregious non-BUG-55 allocators patched; `MetricsRecorder` Phase 1 integration verified (already landed 2026-04-22 — game remains playable without Postgres); EditMode tick budget test establishes Step 3 parity baseline.
```

**Gate:**

```bash
grep -q "already landed as of 2026-04-22" ia/projects/multi-scale-master-plan.md && echo OK
```

Must print `OK`.

---

## Final gate

Run these in order. Any non-zero exit → STOP + surface to user.

```bash
# From repo root:
npm run validate:web
npm --prefix web run build
```

Sanity greps — each must return **zero** matches (ignore `web/.next/` cache hits if present):

```bash
grep -rn "@neondatabase" web/ --include='*.ts' --include='*.tsx' --include='*.json'
grep -rn "drizzle-orm\|drizzle-kit\|drizzle.config" web/ --include='*.ts' --include='*.tsx' --include='*.json'
grep -rn "DASHBOARD_AUTH_SKIP\|portal_session" web/ --include='*.ts' --include='*.tsx' --include='*.md' --include='*.example'
grep -rn "from '@/lib/db/schema'" web/ --include='*.ts' --include='*.tsx'
grep -n "Agent .stage-file.[^-]" ia/skills/release-rollout/SKILL.md
```

Presence greps — each must return ≥ 1 match:

```bash
grep -n "import postgres" web/lib/db/client.ts
grep -n "db-boundaries.md" web/README.md
grep -n "retired 2026-04-22" ia/projects/web-platform-master-plan.md
grep -n "already landed as of 2026-04-22" ia/projects/multi-scale-master-plan.md
test -f docs/db-boundaries.md
```

Final `git diff --stat` sanity: expect changes under `web/lib/db/client.ts`, `web/package.json`, `web/package-lock.json`, deletions under `web/lib/db/schema.ts` + `web/drizzle.config.ts` + `web/drizzle/**` + `web/app/api/auth/**` + `web/app/auth/**` + `web/proxy.ts`, edits to `web/app/robots.ts` + `web/README.md` + `web/.env.local.example`, a new `docs/db-boundaries.md`, 5 line edits in `ia/skills/release-rollout/SKILL.md`, edits in `ia/projects/web-platform-master-plan.md` + `docs/full-game-mvp-rollout-tracker.md` + `ia/projects/multi-scale-master-plan.md`.

---

## Closure

When every checkbox is ticked and the Final gate is green:

- Do **not** commit — user commits manually.
- Do **not** file BACKLOG rows — user drives that.
- Do **not** dispatch `/stage-file` for grid-asset or citystats — those are downstream of this plan.
- Reply to the user with: (a) which steps landed, (b) gate command outputs (one line each), (c) any drift or surprise encountered (append a one-line `Changelog` row below).

---

## Changelog

| Date | Delta | Author |
|---|---|---|
| 2026-04-22 | Initial implementation plan — sequential, pre-digested, composer-2-ready. 7 steps + Final gate. | architecture audit |
| 2026-04-22 | Executed: all 7 steps landed; `web/package-lock.json` deleted (monorepo lockfile at repo root only); release-rollout matrix row (f) wording adjusted so `grep 'Agent .stage-file.[^-]'` stays empty (hyphenated planner/applier names); removed stale `web/.next/types/validator.ts` after route deletes. On this host: run `validate:web` under Node ≥20 from repo root; `next build` green under Node 20.18.1. | agent |
