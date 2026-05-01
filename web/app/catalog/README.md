# Catalog Authoring Console — Quickstart

Dev onboarding for the `web/app/catalog/**` authoring console (DEC-A16 / TECH-1614). Cold-start contributor → console reachable at `localhost:4000/catalog/dashboard` in 5 minutes using only this README.

Architecture lives in [`ia/specs/catalog-architecture.md`](../../../ia/specs/catalog-architecture.md). This file is dev-onboarding only — do not duplicate spec content.

## 1. Cold-start checklist

```bash
# 1. Install monorepo deps from repo root
npm install

# 2. Bootstrap blob root (creates var/blobs/ + .gitignore rules)
bash tools/scripts/bootstrap-blob-root.sh

# 3. Run DB migrations (SQL-authority — see web/README.md §Catalog DTOs vs SQL migrations)
npm run db:migrate

# 4. Local env (web/.env.local — gitignored)
cp web/.env.example web/.env.local
# Then fill DATABASE_URL=postgres://...  (see web/README.md §DATABASE_URL env contract)

# 5. Boot dev server
cd web && npm run dev   # http://localhost:4000

# 6. Open the console
open http://localhost:4000/catalog/dashboard
```

## 2. Dev login (magic-link path)

Console routes are gated by `proxy.ts` capability check (DEC-A33). Two login paths in dev:

### 2.1 Magic-link (production path)

Wire NextAuth EmailProvider via `.env.local`:

```bash
NEXTAUTH_URL=http://localhost:4000
NEXTAUTH_SECRET=<openssl rand -hex 32>
EMAIL_SERVER=smtp://user:pass@host:587   # any dev SMTP — Mailpit, Mailtrap, etc.
```

Magic-link route handler is **not yet wired** at `app/api/auth/[...nextauth]/route.ts` (DEC-A1 deferred — see post-MVP). Use the dev-cookie fallback below until it lands.

### 2.2 Dev-cookie fallback (current default)

Set in `web/.env.local`:

```bash
NEXT_PUBLIC_AUTH_DEV_FALLBACK=1
```

Then plant a cookie keyed to a row in `users`:

```bash
# Pick an existing user id from the users table:
psql "$DATABASE_URL" -c "select id, email, role from users order by created_at limit 5"

# Set the cookie (browser devtools → Application → Cookies → http://localhost:4000):
# Name: dev_user_id   Value: <users.id from query above>
```

`proxy.ts` resolves the cookie → checks role capabilities (`web/lib/auth/capabilities.ts`) → either passes through or returns the DEC-A48 forbidden envelope.

Never set `NEXT_PUBLIC_AUTH_DEV_FALLBACK=1` in production. Bypass is dev-only.

## 3. Route map

Layout: `web/app/catalog/layout.tsx` mounts `CatalogSidebar` (persistent left nav) + `SearchBar`. All `/catalog/*` routes render inside the layout main pane.

### 3.1 Entry routes

| Route | Purpose |
| --- | --- |
| `/catalog/dashboard` | 4-widget health overview — unresolved refs, lint failures, publish queue, snapshot freshness. |
| `/catalog/entities` | Cross-kind entity browser — filter by kind, status, retired. |
| `/catalog/render-runs` | `render_run` browser — sprite-gen invocations + variant blobs. |
| `/catalog/snapshots` | `snapshot` browser — published catalog snapshots Unity loader consumes. |
| `/catalog/audit-log` | Audit trail — capability-gated mutations, retired entities. |
| `/catalog/settings` | Console settings (capability matrix display, env diagnostic). |

### 3.2 Per-kind authoring routes

Each `kind` slug under `web/app/catalog/{kind}/` has a list page + `[slug]` detail page. Spine columns from `0021_catalog_spine.sql`; per-kind detail tables from `0029` onward.

| Route | Kind | Detail table |
| --- | --- | --- |
| `/catalog/sprites` | `sprite` | `sprite_detail` |
| `/catalog/assets` | `asset` | `asset_detail` |
| `/catalog/buttons` | `button` | `button_detail` |
| `/catalog/panels` | `panel` | `panel_detail` + `panel_child` |
| `/catalog/audio` | `audio` | `audio_detail` |
| `/catalog/pools` | `pool` | `pool_member` (+ `pool_member_conditions`) |
| `/catalog/tokens` | `token` | `token_detail` |
| `/catalog/archetypes` | `archetype` | `archetype_authoring` |

Detail pages: `/catalog/{kind}/[slug]` mounts the kind-specific edit form (e.g. `web/components/catalog/AssetEditForm.tsx`). Generic dispatcher: `/catalog/[kind]/[id]` for ad-hoc cross-kind navigation.

### 3.3 API routes

Mirror the page routes under `web/app/api/catalog/{kind}/route.ts` + `.../[slug]/route.ts`. Capability requirements declared via `routeMeta` exports — `proxy.ts` reads `web/lib/auth/route-meta-map.ts` (generated index) to gate `GET` / `POST` / `PUT` / `DELETE`.

## 4. How to add a new kind

End-to-end walkthrough — drizzle-style SQL → API route → catalog page wiring. Replace `{kind}` with your slug (e.g. `effect`, `decal`).

### 4.1 SQL migration

Create `db/migrations/00NN_{kind}_detail.sql` (next number from `db/migrations/` ordering):

```sql
-- Per-kind detail table — joined to catalog_entity.id 1:1.
create table {kind}_detail (
  entity_id uuid primary key references catalog_entity (id) on delete cascade,
  -- kind-specific columns here
  preview_blob_ref text,
  created_at timestamptz not null default now()
);

create index {kind}_detail_preview_blob_ref_idx on {kind}_detail (preview_blob_ref);
```

Spine row gets `kind = '{kind}'`; detail row carries kind-specific columns. Catalog architecture spec §Spine + Detail pattern.

Run `npm run db:migrate` to apply.

### 4.2 DTO + repo

Hand-written DTOs live under `web/types/api/{kind}.ts` (no Drizzle — see `web/README.md` §Catalog DTOs vs SQL migrations). Run `npm run validate:catalog-dto` after edits.

Repo helper: `web/lib/db/{kind}-repo.ts` — `list{Kind}s(filter)`, `create{Kind}(body, sql)`, `get{Kind}(slug, sql)` patterns.

### 4.3 API route

`web/app/api/catalog/{kind}/route.ts` (list + create) and `[slug]/route.ts` (read + update). Pattern from `web/app/api/catalog/sprites/route.ts`:

```ts
export const routeMeta = {
  GET:  { requires: "catalog.entity.create" },
  POST: { requires: "catalog.entity.create" },
} as const;

export async function GET(request: NextRequest) { /* ... */ }
export async function POST(request: NextRequest) { /* ... */ }
```

After adding `routeMeta`, regenerate `web/lib/auth/route-meta-map.ts` (auto-discovered from compiled output — boot dev server once). Verify with `web/tests/api/catalog/_capability-gate.spec.ts`.

### 4.4 Page wiring

- List page: `web/app/catalog/{kind}/page.tsx` — RSC, fetches via repo helper, mounts `EntityListDetailLayout` + kind-specific list component.
- Detail page: `web/app/catalog/{kind}/[slug]/page.tsx` — fetches one row, mounts kind-specific edit form.
- Sidebar entry: add to `web/components/catalog/CatalogSidebar.tsx` nav array.

### 4.5 Capability matrix

Add capability rows for the new kind to the `0026_auth_users_capabilities.sql`-derived capability table (or an additive migration). Roles + capabilities resolved via `web/lib/auth/capabilities.ts`.

### 4.6 Test coverage

- Smoke: `web/tests/api/catalog/{kind}/route.spec.ts` — round-trip GET/POST with capability fixture.
- Unit: `web/lib/db/{kind}-repo.test.ts` — query shape + error envelopes.
- E2E (optional): `web/tests/e2e/{kind}.spec.ts` — Playwright cold-clone smoke.

## 5. Glossary cross-refs

Console terms mapped to canonical glossary rows in [`ia/specs/glossary.md`](../../../ia/specs/glossary.md):

- **`render_run`** — sprite-gen invocation row (input archetype + variant blobs); `/catalog/render-runs` browser surface.
- **`archetype_version`** — versioned archetype YAML row; consumed by `render_run` invocation.
- **`blob_resolver`** — `gen://` URI → physical path lookup mediated by `BLOB_ROOT` env var (DEC-A25). Surface: `web/lib/blob-resolver.ts`.
- **`snapshot`** — published catalog export consumed by Unity loader; `/catalog/snapshots` browser surface.
- **`publish_lint_rule`** — L1 / L2 lint guards run pre-snapshot publish (see `0033_publish_lint_rule_audio_seed.sql` + `0039_publish_lint_rule_non_audio_seed.sql`).
- **`capability`** — proxy-level access gate keyed by user role (`0026_auth_users_capabilities.sql`).

## 6. Sprite-gen integration

Console `/catalog/render-runs` triggers sprite-gen FastAPI service (DEC-A3) — see [`tools/sprite-gen/README.md`](../../../tools/sprite-gen/README.md) §Run as service. Service emits variants under `BLOB_ROOT`; rows surface as `render_run` entries; promoted variants become `sprite` catalog entities.

Cold-start sprite-gen alongside the console:

```bash
# Terminal 1 — web console
cd web && npm run dev

# Terminal 2 — sprite-gen FastAPI
cd tools/sprite-gen && python -m src serve
```

Both share `BLOB_ROOT` (default: repo-local `var/blobs/`) — render output written by sprite-gen is read by `web/lib/blob-resolver.ts` for preview rendering.

## 7. Troubleshooting

| Symptom | Fix |
| --- | --- |
| `proxy.ts` returns 401 on every `/api/catalog/*` call | Set `NEXT_PUBLIC_AUTH_DEV_FALLBACK=1` + plant `dev_user_id` cookie (see §2.2). |
| 403 forbidden envelope on a route that should pass | User role lacks the route's `requires` capability — query `users.role` + `capabilities` table. |
| Blob preview 404s | `BLOB_ROOT` mismatch between sprite-gen + web. Both must read the same env var (or default). |
| Empty `/catalog/dashboard` widgets | DB migrations not applied; run `npm run db:migrate`. |

## 8. Links

- Repo `web/README.md` — workspace dev commands + design system + caveman boundary.
- DB-backed master plan slug `asset-pipeline` — orchestrator. Render via `mcp__territory-ia__master_plan_render({slug: "asset-pipeline"})`.
- Architecture spec: `ia/specs/catalog-architecture.md` (graduated from `docs/asset-pipeline-architecture.md` at Stage 20.1).
- Scene contract: [`docs/asset-pipeline-scene-contract.md`](../../../docs/asset-pipeline-scene-contract.md).
