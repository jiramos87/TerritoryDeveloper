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
