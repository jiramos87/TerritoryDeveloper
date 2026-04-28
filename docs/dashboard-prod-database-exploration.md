# Dashboard Production Database — Exploration

> **Purpose:** Define the long-term, definitive approach for making `/dashboard` and `/api/ia/*` fully functional on the deployed Vercel app, where today they fail because no `DATABASE_URL` is wired and the local DB is on `localhost:5434`. Goal is a clean, repeatable, **single-source-of-truth** model where prod content tracks `main` automatically — not a one-shot snapshot.
>
> **Related plans and docs:**
>
> - `docs/web-platform-exploration.md` — Decision Log 2026-04-16 locks **Neon free (Launch tier)** as the Postgres provider; W7 locks roll-own JWT + sessions (`jose` + `@node-rs/argon2`).
> - `web/lib/db/client.ts` — lazy `postgres` singleton; throws on first query when `DATABASE_URL` unset (build-safe).
> - `web/README.md` — `DATABASE_URL` env contract; "no Vercel env wiring required at MVP (localhost-only critical path)" — this exploration retires that line.
> - `db/migrations/0001..0027_*.sql` — authoritative schema (pure SQL).
> - `tools/postgres-ia/` — migration runner, snapshot tooling, glossary seed.
> - `tools/scripts/materialize-backlog.sh` — IA-files → DB materializer (BACKLOG, master plans, stages, tasks).
> - `tools/scripts/vercel-deploy.sh` — current prod deploy path (`npm run deploy:web`).
> - `docs/db-boundaries.md` — browser ↔ DB ↔ MCP boundary rule.

---

## 0. Today vs target — matrix

| Layer | Today | Target |
|-------|-------|--------|
| **DB host** | `localhost:5434/territory_ia_dev` (dev only) | **Neon free** primary branch = prod; preview branches per PR (immediate teardown on close) |
| **Local image** | Homebrew (version drift possible per-developer) | **Docker compose w/ `postgres:16-alpine`** pinned; matches Neon major version + extensions |
| **Schema** | `npm run db:migrate` against local | CI-gated: **Atlas** schema-diff comment on PR, auto-apply on merge to `main` |
| **Content** | Lives in local DB; populated by materializers + manual edits | **Derived from `main`** — CI runs new `npm run db:sync-prod-content` wrapper against prod after each merge |
| **Driver** | `postgres` library local | **Same `postgres` library** in prod via Neon **TCP pooler URL** (`?pgbouncer=true`) — single driver everywhere preserves parity |
| **Vercel env** | `DATABASE_URL` unset → routes throw at runtime | `DATABASE_URL` (pooled, aliased) + `DATABASE_URL_UNPOOLED` (migrations only) — same names local + prod |
| **Auth** | Dev fallback (`NEXT_PUBLIC_AUTH_DEV_FALLBACK=1`); `DASHBOARD_AUTH_SKIP=1` locally | **Deferred to follow-on master plan**; `/dashboard` + `/api/ia/*` gated by Vercel deployment protection during rollout window |
| **Prod creds** | N/A | **CI-only after Stage 1 bootstrap**; humans get read-only role for diagnostics |
| **Observability** | None in prod | `/api/healthz` (`SELECT 1`) + Neon query insights + Vercel function logs |
| **Parity gates** | None | (a) Atlas schema-diff on PR, (b) nightly snapshot round-trip smoke, (c) materializer idempotency test, (d) version+extension assertion at connect |
| **Rollback** | N/A | Neon point-in-time restore (24h on free) + migration `down` parity audit |

---

## 1. Problem

1. **Prod dashboard is non-functional.** `web/lib/db/client.ts` throws on first query because Vercel has no `DATABASE_URL`. All DB-backed routes (`/dashboard`, `/api/ia/master-plans`, `/api/ia/stages/*`, catalog admin) 500 in prod.
2. **No content sync model exists.** Local DB is the only place where IA content (master plans, stages, tasks, glossary, backlog) lives in DB form. No pipeline pushes it anywhere. A one-shot snapshot would age within hours of the next merge.
3. **Single source of truth is ambiguous.** IA files in `ia/` are git-tracked and authoritative; DB is a derived projection via materializers. But today only the local DB is populated, so prod cannot be "just rebuilt from `main`" without explicit infra.
4. **Migration governance is informal.** `db:migrate` runs locally, by hand. No CI gate validates that a new migration applies cleanly, no diff vs prod schema is produced, no rollback rehearsal.
5. **Auth in prod is unfinished.** Schema row `0026_auth_users_capabilities` exists; W7 spec locks the auth stack; no route handlers wired; no `NEXTAUTH_SECRET` / email server in Vercel env.
6. **No serverless connection-pooling.** Vercel functions are stateless; opening a fresh `postgres` socket per cold start exhausts Neon's connection cap quickly without PgBouncer/HTTP driver.
7. **No preview-branch DB story.** PR previews on Vercel hit production DB by default → risk of write side-effects from preview tests / explorations.

---

## 2. Scope

### 2.1 In scope

- Neon connection topology — pooled vs unpooled URLs, driver choice (`postgres` vs `@neondatabase/serverless` HTTP).
- Migration CI pipeline — dry-run on PR, auto-apply on merge, schema-diff artifact.
- Content sync pipeline — what runs, when, against which Neon branch.
- Vercel env wiring — which vars, which environments (production / preview / development), rotation policy.
- Branch-per-PR DB story — Neon branch creation + teardown lifecycle.
- Auth wiring in prod — secret management, email provider, session storage table.
- Health + observability surface.
- Rollback + disaster recovery.

### 2.2 Out of scope

- Schema redesign (migrations stay as authored).
- Replacing the `postgres` library at call sites (driver swap considered as topology decision only).
- Web frontend / dashboard UI (orthogonal).
- Unity bridge DB usage (separate concern; bridge runs locally).
- Multi-region replication (free tier irrelevant; revisit at paid tier).

### 2.3 Non-goals

- Mirror local DB as-is to prod (would carry dev fixtures + dirty state).
- Allow prod writes from non-CI sources for IA content.
- Replace IA files in `ia/` as the source of truth.

---

## 3. Terminology (working definitions — TBD markers flag open questions)

| Term | Working definition | TBD |
|------|--------------------|-----|
| **Source of truth** | The IA files under `ia/` (specs, master plans, glossary, backlog yaml). DB is a derived projection. | — |
| **Materializer** | Script that reads IA files and inserts/upserts into DB. Existing: `materialize-backlog.sh`, master-plan inserts (`tools/postgres-ia/`). | Q5 — single orchestrator script vs N independent scripts? |
| **Neon branch** | Git-like DB branch (free tier supports N branches with copy-on-write). Used as primary (prod), preview-per-PR, optional staging. | Q3 — naming convention + auto-teardown trigger. |
| **Pooled URL** | Connection string going through Neon's PgBouncer pooler. Required for serverless functions. Format: `?pgbouncer=true`. | — |
| **Unpooled URL** | Direct connection. Required for migrations + advisory locks. | — |
| **HTTP driver** | `@neondatabase/serverless` — Fetch-based, no TCP socket. Survives Vercel cold start without leak. | Q1 — swap from `postgres` library or keep both? |
| **Capability** | Auth permission row (`auth_users_capabilities` table from migration 0026). Drives dashboard route access. | Q7 — bootstrap mechanism for first admin user. |
| **Schema diff** | CI artifact comparing prod DB schema vs PR's migration set, posted as PR comment. | Q4 — tool: `migra` vs `pgsync` vs custom. |

---

## 4. Options compared (resolved)

> All four sub-decisions resolved during round-2/3/4 polling. Sections retained for design audit trail.

### 4.1 Connection driver — single driver via pooler **(LOCKED)**

**Decision:** keep the existing `postgres` library everywhere — local **and** prod. In prod, point at Neon's PgBouncer pooler URL (`?pgbouncer=true`).

| Aspect | `postgres` lib via pooler **(picked)** | `@neondatabase/serverless` HTTP (rejected) |
|--------|---------------------------------------|---------------------------------------------|
| Cold-start cost | ~300ms TCP+TLS first hit | ~80ms HTTP |
| Connection leak risk | None — pooler multiplexes | None — stateless HTTP |
| Local dev parity | **Identical to prod** | Needs local fetch-shim proxy or env branch |
| Prepared statement semantics | Same everywhere | Slightly different on HTTP path |
| Migration runner | Reuses same driver via unpooled URL | Migrations need separate `pg` client anyway |

**Why parity wins over cold-start perf:** dashboard is not latency-critical; subtle SQL-semantic divergence between two drivers would undermine the parity goal that drove this exploration. W7 names `@neondatabase/serverless` as a candidate but does not lock it; this exploration chooses TCP pooler instead.

### 4.2 Migration governance — manual vs CI-gated

| Aspect | Manual (today) | CI-gated (target) |
|--------|----------------|-------------------|
| Apply trigger | Developer runs `npm run db:migrate` | GitHub Action on merge to `main` |
| Pre-merge validation | None | Dry-run against ephemeral Neon branch; schema-diff comment on PR |
| Rollback rehearsal | None | Optional `down` migration test against branch before applying to prod |
| Drift risk | High (anyone can apply ad-hoc) | Low (only CI service account has prod creds) |
| Failure mode | Partial apply leaves prod in unknown state | Action fails, prod unchanged, alert raised |

**Recommendation:** CI-gated, single workflow `db-migrate.yml`. Service account uses unpooled URL stored as GitHub Actions secret. Dry-run uses ephemeral Neon branch named `pr-{number}`.

### 4.3 Content sync — snapshot once vs CI-driven materialization

| Aspect | One-shot snapshot | CI re-materialize on merge |
|--------|-------------------|-----------------------------|
| Setup cost | ~1 hour | ~1 day |
| Stays current | No (manual re-run) | Yes (auto on every merge to `main`) |
| Source of truth alignment | Drift accumulates | Prod = `main` always |
| Includes dev fixtures | Yes (risk) | No (clean projection from IA files) |
| Failure isolation | One bad snapshot = manual fix | Bad merge fails CI, prod unchanged |
| Local DB role | Prod twin | Dev sandbox only |

**Recommendation:** CI-driven re-materialization. Workflow `db-content-sync.yml` runs after `db-migrate.yml` on merge to `main`. Sequence: `materialize-backlog.sh` → master-plan inserts → glossary seed → stage-body sync → task-spec sync. All idempotent (upsert semantics).

### 4.4 Preview-branch DB — shared prod vs Neon branch per PR

| Aspect | Shared prod | Neon branch per PR |
|--------|-------------|---------------------|
| Free tier compatibility | Yes | Yes (Neon free supports 10 branches, retains 24h) |
| Risk to prod data | High (preview writes leak in) | None (isolated) |
| Test surface | Read-only effectively | Full read+write |
| Setup cost | Zero | ~2 hours (Vercel preview hook + Neon API call) |
| Teardown | N/A | Auto on PR close (workflow `pr-cleanup.yml`) |

**Recommendation:** Neon branch per PR, named `preview-pr-{number}`. Vercel preview deployment env injects `DATABASE_URL` pointing at that branch. Prod immune.

### 4.5 Auth wiring — defer vs ship with this rollout

| Aspect | Defer | Ship with this rollout |
|--------|-------|------------------------|
| Dashboard accessible in prod | Vercel password protection only | Real per-user auth |
| Capability row (`0026`) used | Schema-only | Wired |
| Setup cost | Zero | ~1 day (route handlers + email provider account) |
| First-admin bootstrap | N/A | Manual seed via CLI or capability-grant migration |
| Backward compat | Dev fallback stays | Dev fallback stays (gated by `NEXT_PUBLIC_AUTH_DEV_FALLBACK`) |

**Recommendation:** **defer auth to a follow-on stage.** Ship rollout with Vercel deployment protection (password) on `/dashboard` + `/api/ia/*`. Auth is a meaningful surface area — better as its own master plan stage with proper review. Capability table is ready to use when that stage lands.

---

## 5. Proposed architecture

```
┌─────────────────────────────────────────────────────────────────┐
│  GitHub repo (main = source of truth)                           │
│  ├─ ia/**                  (specs, glossary, backlog yaml)      │
│  ├─ db/migrations/*.sql    (schema)                             │
│  └─ tools/postgres-ia/**   (materializers)                      │
└──────────────────┬──────────────────────────────────────────────┘
                   │ merge to main
                   ▼
┌─────────────────────────────────────────────────────────────────┐
│  GitHub Actions                                                 │
│  ├─ db-migrate.yml         (apply pending migrations)           │
│  └─ db-content-sync.yml    (re-materialize IA → DB)             │
└──────────────────┬──────────────────────────────────────────────┘
                   │ uses unpooled URL
                   ▼
┌─────────────────────────────────────────────────────────────────┐
│  Neon — primary branch (prod)                                   │
│  ├─ schema = sum of db/migrations/*                             │
│  └─ content = projection of ia/** at HEAD of main               │
└──────────────────┬──────────────────────────────────────────────┘
                   │ pooled URL via integration
                   ▼
┌─────────────────────────────────────────────────────────────────┐
│  Vercel functions — web/ (production deployment)                │
│  ├─ DATABASE_URL          (pooled, HTTP driver)                 │
│  └─ DATABASE_URL_UNPOOLED (rare; admin scripts)                 │
└─────────────────────────────────────────────────────────────────┘

       ┌──────────────────────────────┐
       │  PR opened                   │
       │  ├─ Neon API: branch from    │
       │  │  primary → preview-pr-N   │
       │  └─ Vercel preview env       │
       │     DATABASE_URL = branch    │
       └──────────────────────────────┘
```

---

## 6. Rollout stages (proposal — to be filed as a master plan)

Stage cardinality respects the 2–6 task rule; each task ~1–2 days.

### Stage 1 — Neon provisioning + local Docker pinning + manual bootstrap

1. Provision Neon project (PG 16) via Vercel integration; auto-injects `POSTGRES_URL` family env vars.
2. Alias / rename in Vercel UI to `DATABASE_URL` (pooled) + `DATABASE_URL_UNPOOLED` (direct) — same names as local.
3. Add `docker-compose.dev.yml` pinning `postgres:16-alpine` + required extensions (`pg_trgm`, others surfaced by audit). Update `db:setup-local` to wrap compose.
4. Manual one-shot bootstrap from a developer machine: `db:migrate` against unpooled prod URL → `db:sync-prod-content` (delivered in Stage 4 as a thin wrapper; Stage 1 ships a stub usable for first seed) → smoke `/api/healthz`.
5. Rotate creds: after bootstrap, prod URL moves to GitHub Actions secrets only; humans get read-only role.

### Stage 2 — Driver wiring + connect-time parity assertion

1. Confirm `web/lib/db/client.ts` reads `DATABASE_URL` and uses the **existing `postgres` library** unchanged.
2. Add boot-time assertion: `SELECT version()` matches `>= 16.x`; required extensions present. Fast-fail with clear error.
3. Update `web/README.md` connection-pattern section to reflect single-driver-via-pooler model.
4. Verify all `web/lib/**` + `web/app/api/**` call sites work against pooled URL (no transaction-scoped advisory locks etc. that PgBouncer transaction mode breaks).

### Stage 3 — CI migration workflow + Atlas schema diff

1. `.github/workflows/db-migrate.yml` — on merge to `main`, apply pending migrations to prod via service-account unpooled URL.
2. PR-side dry-run — Neon branch from primary, apply PR's migrations, run **Atlas** schema diff vs primary, post comment on PR.
3. Service account creds stored as `NEON_API_KEY` + `DATABASE_URL_PROD_UNPOOLED` GitHub secrets.
4. Failure mode — workflow fails; prod schema unchanged; alert raised.

### Stage 4 — Content-sync wrapper + CI workflow + idempotency test

1. New `npm run db:sync-prod-content` wrapper script (under `tools/postgres-ia/`) orchestrates: `materialize-backlog.sh` → master-plan inserts → glossary seed → stage-body sync → task-spec sync.
2. `.github/workflows/db-content-sync.yml` — runs after `db-migrate` on merge to `main`; invokes the wrapper.
3. **Idempotency CI test:** run wrapper twice against same input; assert zero DB diffs on second run.
4. Audit pass on each materializer for `INSERT ... ON CONFLICT` + deterministic ordering; fix any non-deterministic surfaces surfaced.

### Stage 5 — Preview-branch DB lifecycle

1. `.github/workflows/preview-db-create.yml` — on PR open, Neon API branch from primary → `preview-pr-{number}`.
2. Inject branch URL into Vercel preview env via Vercel API.
3. `.github/workflows/preview-db-cleanup.yml` — **immediate teardown on PR close/merge**; deletes branch.
4. Document branch quota (10 free) + immediate-teardown policy in `web/README.md`.

### Stage 6 — Health + observability + parity round-trip

1. `web/app/api/healthz/route.ts` — `SELECT 1` + version + commit SHA + extension list.
2. `web/app/api/readyz/route.ts` — schema-version check (`SELECT MAX(version) FROM schema_migrations`).
3. **Nightly parity round-trip workflow** — dump prod → restore to throwaway Neon branch → run `validate:all` → tear down branch.
4. Document SLO + alert routing in `docs/operations.md` (new).

### Stage 7 — Rollback + DR rehearsal

1. Document Neon point-in-time restore procedure (24h window on free tier).
2. Migration `down` parity audit — every `up` has a tested `down` for the last N migrations (or explicit "irreversible" marker).
3. Quarterly DR drill — restore prod to staging branch, verify dashboard renders.
4. Runbook in `docs/operations.md`.

---

## 7. Resolved decisions (poll log 2026-04-26)

> All decisions locked through 4 polling rounds. Master plan can seed directly off this section.

| # | Question | Resolution | Round |
|---|----------|-----------|-------|
| D1 | Free-tier host | **Neon free** (parity-equal to AWS RDS but permanent + native branching + Vercel-native) | R2 |
| D2 | Local Postgres pinning | **Docker compose `postgres:16-alpine`** | R1 |
| D3 | Driver strategy | **Single `postgres` library** via Neon TCP pooler URL | R2 |
| D4 | Postgres major version | **PG 16** (Docker local + Neon project) | R3 |
| D5 | Schema-diff CI gate | **Yes — Atlas** PR comment | R1+R3 |
| D6 | Snapshot round-trip smoke | **Yes — nightly job** | R2 |
| D7 | Materializer idempotency test | **Yes — CI assert zero diffs on second run** | R2 |
| D8 | Connect-time version+extension assertion | **Yes — `SELECT version()` + extension check** | R1 |
| D9 | Content-sync orchestration | **Single `npm run db:sync-prod-content` wrapper** | R3 |
| D10 | Stage 1 bootstrap | **Manual one-shot wrapper run** (humans), then CI takes over | R3 |
| D11 | Vercel env var naming | **Alias to `DATABASE_URL` + `DATABASE_URL_UNPOOLED`** (parity with local names) | R4 |
| D12 | Preview branch teardown | **Immediate on PR close/merge** | R4 |
| D13 | Auth wiring | **Defer** to follow-on master plan; Vercel deployment protection during rollout | R4 |
| D14 | Prod creds policy | **CI-only after bootstrap**; humans read-only for diagnostics | R4 |

---

## 8. Risks + mitigations

| Risk | Mitigation |
|------|------------|
| Neon free tier branch quota (10 branches) hit by busy PR week | Auto-teardown on PR close; weekly sweep job for stragglers |
| Migration auto-apply breaks prod | PR-side dry-run + schema diff catches most; rollback via Neon PITR |
| Materializer non-idempotent — duplicates on re-sync | Audit pass in Stage 4 task 3; add unit tests on materializer scripts |
| Cold-start TCP exhaustion before Stage 2 ships | Mitigate by ordering — Stage 2 ships before Stage 4 turns on auto-sync to prod |
| `DATABASE_URL` leak via Vercel build logs | Use Vercel sensitive env flag; never `console.log` env in build |
| Preview DB has stale content | Acceptable — preview is for code review, not content review; content lives on `main` |

---

## 9. Out-of-band notes

- `docs/asset-pipeline-post-mvp-extensions.md` adds a job-queue render-run table (`0027`); the migration is in tree already and will ride this rollout's first migrate.
- The existing local-only model has worked because all DB-using surfaces are agent skills running on dev machines. The web dashboard is the first **always-on, multi-user** consumer.
- This exploration intentionally does **not** propose moving content authoring to the DB. Authoring stays in IA files; DB stays a projection. Reversing that direction is a much larger redesign.

---

## 10. Next step

All decisions resolved (D1–D14, §7). Ready for `/design-explore docs/dashboard-prod-database-exploration.md` to expand into a `## Design Expansion` block (compare-approaches phase auto-skipped — locked decisions feed directly into architecture + subsystem-impact phases). Then `/master-plan-new` seeds the 7-stage orchestrator from §6.

---

## Design Expansion — Web Platform Alignment

> **Mode:** gap-analysis vs `docs/web-platform-exploration.md` (W7 Portal foundations + Decision Log 2026-04-16).
> Locked design (§0–§10 above) precedes this block. This section closes 9 gaps R1–R15 confirmed in scope; does NOT mutate prior sections or the resolved decisions table.

### Gap inventory

| Req | Source (W-section) | Current coverage in §0–§10 | Gap severity |
|-----|--------------------|----------------------------|--------------|
| R1 — Neon free tier as Postgres provider | W7.2 Decision Log 2026-04-16 | §0 row "DB host" + D1 confirm | Additive (already aligned; just affirm) |
| R2 — Env var naming parity local↔prod | W7 implied via `web/lib/db/client.ts` | §0 row "Vercel env" + D11 | Additive |
| R4 — Driver choice for serverless | W7.2 names `@neondatabase/serverless` HTTP | §0 row "Driver" + D3 picks `postgres` via TCP pooler | **BLOCKING** (override needed) |
| R8 — CI service-account creds for migrations | (none in W7 — new) | Stage 3 task 3 | Additive (concretize secret names) |
| R9 — PR preview branch lifecycle | (none in W7 — new) | Stage 5, D12 | Additive (lifecycle hooks) |
| R10 — Dashboard data path | W6.1 wraps `parse.mjs` | §1 problem 1 references DB-backed routes; routes already DB-backed | **BLOCKING** (supersession needed) |
| R11 — Auth deferral semantics | W7.1 locks `jose` + `@node-rs/argon2` | D13 defers; capability table 0026 ready | Additive (compat statement) |
| R14 — PgBouncer URL params (`?pgbouncer=true&connect_timeout=15`) | (none in W7 — new) | §0 row "Driver" + D3 | Additive (param wiring) |
| R15 — Docker compose pinning vs Neon major | (none in W7 — new) | §0 row "Local image" + D2 + D4 | Additive (extension parity) |

### Phase 3 — Component expansion per gap

#### Driver category

**R4 — `postgres` lib via TCP pooler (overrides W7's `@neondatabase/serverless` HTTP candidate)**
- Responsibility: single connection driver across local + prod; Neon TCP pooler URL with `?pgbouncer=true` for serverless safety.
- Data flow: `web/lib/db/client.ts` lazy singleton → `postgres()` → `process.env.DATABASE_URL` (pooled). Migrations use `DATABASE_URL_UNPOOLED`.
- Contract impact: import surface in `web/lib/db/client.ts` stays unchanged; no call-site refactor across `web/lib/**` or `web/app/api/**`.
- Non-scope: not changing call sites; not introducing dual-driver build matrix.

**R14 — PgBouncer URL params**
- Responsibility: ensure pooled URL carries `?pgbouncer=true&connect_timeout=15&statement_cache_size=0` to match transaction-pool mode constraints.
- Data flow: Vercel env → connection string parsed by `postgres` lib → driver config disables prepared-statement cache.
- Contract impact: Stage 2 boot-time assertion gains a "transaction-mode-safe" check (no advisory locks scoped to session).
- Non-scope: not touching unpooled URL params.

#### Data path category

**R10 — DB-backed dashboard supersession**
- Responsibility: confirm `/api/ia/master-plans`, `/api/ia/stages/*`, dashboard pages already read DB (not `parse.mjs`).
- Data flow: IA files → materializers → DB → `web/lib/db/client.ts` → RSC routes. `parse.mjs` retained only by legacy `tools/progress-tracker/render.mjs` for `docs/progress.html` snapshot.
- Contract impact: W6.1 ("plan-loader wraps `parse.mjs`") superseded by current DB read path; once prod DB live, `docs/progress.html` becomes optional artifact.
- Non-scope: not deleting `parse.mjs` or `progress.html` in this rollout (deprecation trigger TBD per W Review Notes).

#### Env category

**R1 — Neon free tier affirmation**
- Responsibility: confirm provider matches W7.2 lock; no provider-shop revisit.
- Contract impact: D1 echo of W7.2 — same Launch tier, primary branch = prod.
- Non-scope: paid-tier features (multi-region, larger compute).

**R2 — Env naming parity**
- Responsibility: same `DATABASE_URL` + `DATABASE_URL_UNPOOLED` names local + Vercel prod + GitHub Actions.
- Data flow: Vercel integration injects `POSTGRES_URL*` family → manually aliased in Vercel UI to `DATABASE_URL` family.
- Contract impact: `web/README.md` env contract row stays single-name.
- Non-scope: not introducing per-env prefix (`PROD_`, `PREVIEW_`).

#### CI category

**R8 — CI service-account creds**
- Responsibility: GitHub Actions secret holds `DATABASE_URL_PROD_UNPOOLED` + `NEON_API_KEY`; humans get separate read-only role post-bootstrap.
- Data flow: Stage 1 manual bootstrap → Stage 3 CI workflow uses secrets → unpooled URL for migrations + Atlas diff.
- Contract impact: D14 enforced — no human prod write creds after Stage 1 closes.
- Non-scope: not designing rotation cadence (that lives in Stage 7 runbook).

#### Preview category

**R9 — PR preview branch lifecycle**
- Responsibility: Neon branch per PR (`preview-pr-{number}`); Vercel preview env injected with branch URL; immediate teardown on PR close/merge.
- Data flow: PR open → `preview-db-create.yml` → Neon API branch → Vercel API env upsert → preview deploy reads branch DB. PR close → `preview-db-cleanup.yml` deletes branch.
- Contract impact: D12 immediate-teardown enforced; weekly sweep job catches stragglers (10-branch quota).
- Non-scope: not designing data seed for preview branches (copy-on-write from primary at branch time).

#### Auth category

**R11 — Auth deferral compatibility**
- Responsibility: D13 defers auth; W7.1 locked stack (`jose` + `@node-rs/argon2` + stateful `session` row, `SESSION_COOKIE_NAME=portal_session`, `SESSION_LIFETIME_DAYS=30`) stays the contract for the follow-on master plan.
- Data flow: rollout window — Vercel deployment protection (password) gates `/dashboard` + `/api/ia/*`. Auth follow-on stage wires `app/api/auth/*` route handlers + capability checks against `auth_users_capabilities` (migration 0026, already in tree).
- Contract impact: no regression vs W7 lock; no auth route handlers added in this rollout.
- Non-scope: route-handler implementation, first-admin bootstrap, email provider — all live in the auth follow-on master plan.

#### Local pinning category

**R15 — Docker compose pinning vs Neon major**
- Responsibility: `docker-compose.dev.yml` pins `postgres:16-alpine` matching Neon PG 16 major; load extensions Neon enables (`pg_trgm` confirmed; full list audited at Stage 1 task 3).
- Data flow: `db:setup-local` wraps `docker compose up -d postgres` → applies migrations against local instance.
- Contract impact: D2 + D4 enforced; D8 connect-time assertion validates parity at boot.
- Non-scope: not pinning patch version; not bundling Neon-specific extensions absent from upstream.

### Phase 4 — Architecture

Phase 4 skipped — no new runtime components introduced. All gaps are config / process / CI surface. Existing §5 ASCII diagram already covers component topology (GitHub Actions → Neon → Vercel functions). No Mermaid update required.

### Phase 5 — Subsystem impact

| Subsystem | Touched by gap | Dependency nature | Breaking vs additive | Mitigation |
|-----------|----------------|-------------------|----------------------|------------|
| `web/lib/db/client.ts` | R4, R14 | Driver + URL params | Additive (no API change) | Stage 2 boot-time assertion (D8) catches transaction-mode incompat |
| `tools/postgres-ia/` (migration runner, snapshot, glossary seed) | R8, R15 | Run target = prod via service account | Additive | Stage 3 secret naming + Stage 1 extension audit |
| `tools/scripts/materialize-backlog.sh` + master-plan inserts | R10 | Now run in CI on merge | Additive (idempotency mandated) | Stage 4 task 3 idempotency CI test (D7) |
| `web/app/api/ia/*` route handlers | R10 | Already DB-backed; just need `DATABASE_URL` set | None | Vercel env wiring in Stage 1 task 2 |
| `web/app/api/auth/*` (placeholder) | R11 | Deferred — no code yet | None at this rollout | Auth follow-on master plan owns wiring |
| Vercel env surface | R1, R2, R8 | New env vars in production + preview environments | Additive | D11 alias step in Stage 1 task 2 |
| `web/README.md` | R2 | Doc parity | Additive (replace "no Vercel env wiring required at MVP" line) | Stage 2 task 3 update |
| `docs/web-platform-exploration.md` | R4, R10 | Reference doc — must NOT mutate per hard boundary | None | Override log below points back at W7.2 + W6.1 |
| `.github/workflows/db-migrate.yml` (new) | R8 | New CI workflow | Additive | Stage 3 |
| `.github/workflows/db-content-sync.yml` (new) | R10 | New CI workflow | Additive | Stage 4 |
| `.github/workflows/preview-db-create.yml` + `preview-db-cleanup.yml` (new) | R9 | New CI workflows | Additive | Stage 5 |
| `docker-compose.dev.yml` (new) | R15 | Local dev pinning | Additive | Stage 1 task 3 |

Invariants flagged: 0 — no runtime C# / Unity code touched. `invariants_summary` skip justified (config/pipeline-only).

### Phase 6 — Implementation checklist

Mirrors §6 stage skeleton; closes the 9 gaps in-line.

**Stage 1 — Neon provisioning + local Docker pinning + manual bootstrap (R1, R2, R15)**
- [ ] Provision Neon project (PG 16) via Vercel integration → injects `POSTGRES_URL*` family
- [ ] Alias env in Vercel UI: `DATABASE_URL` (pooled) + `DATABASE_URL_UNPOOLED` (direct)
- [ ] Create `docker-compose.dev.yml` pinning `postgres:16-alpine` + extensions matching Neon (`pg_trgm` + audit list)
- [ ] Wrap `db:setup-local` to call compose
- [ ] Manual bootstrap: `db:migrate` against unpooled prod URL → `db:sync-prod-content` (Stage 4 wrapper) → `/api/healthz` smoke
- [ ] Move prod creds from human → CI-only; provision read-only diagnostic role for humans

**Stage 2 — Driver wiring + parity assertion (R4, R14)**
- [ ] Confirm `web/lib/db/client.ts` already uses `postgres()` lazy singleton (no driver swap)
- [ ] Append `?pgbouncer=true&connect_timeout=15&statement_cache_size=0` to pooled URL when constructing in Vercel
- [ ] Add boot-time assertion: `SELECT version() >= 16.x` + extension list check + transaction-mode safety probe
- [ ] Update `web/README.md` connection-pattern section + retire "no Vercel env wiring required at MVP" line
- [ ] Audit `web/lib/**` + `web/app/api/**` for session-scoped advisory locks (PgBouncer transaction mode breaks them)

**Stage 3 — CI migration workflow + Atlas diff (R8)**
- [ ] `.github/workflows/db-migrate.yml` — on merge to `main`, apply pending via service-account unpooled URL
- [ ] PR-side dry-run — Neon branch from primary, apply PR migrations, Atlas schema-diff comment
- [ ] GitHub secrets: `NEON_API_KEY`, `DATABASE_URL_PROD_UNPOOLED`
- [ ] Failure mode: workflow fails, prod schema unchanged, alert raised

**Stage 4 — Content-sync wrapper + CI workflow + idempotency test (R10)**
- [ ] New `npm run db:sync-prod-content` wrapper under `tools/postgres-ia/`
- [ ] Wrapper sequences: `materialize-backlog.sh` → master-plan inserts → glossary seed → stage-body sync → task-spec sync
- [ ] `.github/workflows/db-content-sync.yml` runs after `db-migrate` on merge to `main`
- [ ] CI idempotency test: run wrapper twice, assert zero diffs on second run
- [ ] Audit each materializer for `INSERT ... ON CONFLICT` + deterministic ordering

**Stage 5 — Preview-branch DB lifecycle (R9)**
- [ ] `.github/workflows/preview-db-create.yml` — on PR open, Neon API branch from primary → `preview-pr-{number}`
- [ ] Inject branch URL into Vercel preview env via Vercel API
- [ ] `.github/workflows/preview-db-cleanup.yml` — immediate teardown on PR close/merge
- [ ] Document branch quota (10) + immediate-teardown policy in `web/README.md`

**Stage 6 — Health + observability + parity round-trip**
- [ ] `web/app/api/healthz/route.ts` — `SELECT 1` + version + commit SHA + extension list
- [ ] `web/app/api/readyz/route.ts` — schema-version check
- [ ] Nightly parity round-trip workflow — dump prod → restore to throwaway branch → `validate:all` → tear down
- [ ] SLO + alert routing in `docs/operations.md`

**Stage 7 — Rollback + DR rehearsal**
- [ ] Document Neon PITR procedure (24h free tier window)
- [ ] Migration `down` parity audit
- [ ] Quarterly DR drill — restore prod → staging branch → verify dashboard renders
- [ ] Runbook in `docs/operations.md`

**Deferred / out of scope (R11)**
- Auth wiring deferred to follow-on master plan; W7.1 locked stack carries forward unchanged (`jose` + `@node-rs/argon2`, `SESSION_COOKIE_NAME=portal_session`, `SESSION_LIFETIME_DAYS=30`, stateful `session` row). Capability table (migration 0026) ships with Stage 3's first migrate but stays unwired until auth stage lands. Vercel deployment protection gates `/dashboard` + `/api/ia/*` during the rollout window.

### Phase 7 — Examples

**Example 1 — R4 driver migration (input → output → edge case)**

Input — current `web/lib/db/client.ts` import + connection (already in tree, no swap needed):
```ts
import postgres from 'postgres';
let _sql: ReturnType<typeof postgres> | null = null;
export function getSql() {
  if (!_sql) {
    if (!process.env.DATABASE_URL) throw new Error('DATABASE_URL unset');
    _sql = postgres(process.env.DATABASE_URL);
  }
  return _sql;
}
```

Output — Vercel prod env value (pooled, transaction-mode-safe):
```
DATABASE_URL=postgres://user:pass@ep-xyz-pooler.us-east-2.aws.neon.tech/neondb?pgbouncer=true&connect_timeout=15&statement_cache_size=0
DATABASE_URL_UNPOOLED=postgres://user:pass@ep-xyz.us-east-2.aws.neon.tech/neondb?connect_timeout=15
```

Edge case — cold-start latency regression (W7's rejected concern): first request after idle pays ~300ms TCP+TLS vs HTTP driver's ~80ms. Mitigation accepted per §4.1 rationale (parity > latency for non-latency-critical dashboard). Boot-time assertion catches the misconfig where someone points pooled URL at session-mode endpoint (advisory locks would silently drop).

**Example 2 — R10 supersession proof**

Existing route already DB-backed (no `parse.mjs` import):
```ts
// web/app/api/ia/master-plans/route.ts (current)
import { getSql } from '@/lib/db/client';
export async function GET() {
  const sql = getSql();
  const rows = await sql`SELECT slug, title, status FROM master_plan ORDER BY slug`;
  return Response.json({ plans: rows });
}
```

W6.1 plan-loader-wraps-parse.mjs path superseded by materializer pipeline writing to `master_plan` table. Once Stage 4 ships, `docs/progress.html` becomes optional snapshot artifact (deprecation trigger from W Review Notes — "dashboard uptime N weeks").

**Example 3 — R14 PgBouncer transaction-mode hazard (edge case)**

Failure mode — agent code uses session-scoped advisory lock against pooled URL:
```ts
await sql`SELECT pg_advisory_lock(${LOCK_ID})`;
// ... unrelated query lands on different physical conn via pooler
await sql`SELECT pg_advisory_unlock(${LOCK_ID})`; // silently fails — different session
```

Mitigation — Stage 2 audit pass + boot-time check; route advisory locks to unpooled client (rare; admin scripts only) or use transaction-scoped `pg_advisory_xact_lock`.

### Review Notes

Phase 8 subagent review run via Plan agent against this expansion. BLOCKING items resolved inline before persist. Verbatim NON-BLOCKING + SUGGESTIONS carried below.

**NON-BLOCKING:**
- Stage 1 task 3 should specify the extension-audit script path (e.g., a one-liner querying Neon's `pg_extension` after provisioning) so the local compose extension list is reproducible, not tribal knowledge.
- Stage 5 preview-branch URL injection into Vercel env via API needs an idempotency note — re-running on PR sync should upsert, not error.
- R11 auth deferral could mention the Vercel deployment protection password rotation cadence (one-line policy) so the gate isn't a long-lived shared secret.

**SUGGESTIONS:**
- Consider adding a Stage 0 / pre-flight check listing every `process.env.DATABASE_URL` reader across `web/` + `tools/` + `db/` — single audit doc reduces "did we miss a call site?" risk.
- Stage 4 idempotency CI test could double as a smoke for Stage 6 by hashing DB state before/after; reuses one Neon branch instead of two.
- Consider exporting the connect-time assertion (D8) as a shared helper (`web/lib/db/preflight.ts`) so the same probe runs in `/api/healthz`, boot, and the nightly round-trip workflow.

### Override log

- **R4 — W7.2 override.** W7 Decision Log 2026-04-16 named `@neondatabase/serverless` HTTP driver as candidate for cold-start optimization. This rollout overrides with `postgres` lib via Neon TCP pooler URL (D3) — parity priority across local + prod outweighs cold-start delta on a non-latency-critical dashboard. W7.2 lock not mutated; pointer recorded here only.
- **R10 — W6.1 supersession.** W6.1 ("plan-loader wraps `parse.mjs`") was the pre-DB plan; routes already moved to DB-backed reads (`/api/ia/master-plans`, `/api/ia/stages/*`). Once prod DB live (Stage 1 bootstrap), `parse.mjs` retained only by `tools/progress-tracker/render.mjs` for legacy `docs/progress.html` snapshot. W6 not mutated; supersession recorded here only.

### Expansion metadata

- **Date:** 2026-04-26
- **Model:** claude-opus-4-7
- **Mode:** gap-analysis vs `docs/web-platform-exploration.md`
- **Gaps closed:** 9 (R1, R2, R4, R8, R9, R10, R11, R14, R15)
- **Blocking items resolved:** 2 (R4 driver override, R10 dashboard data path supersession)
