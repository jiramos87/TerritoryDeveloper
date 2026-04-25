# Staged migrations

Migrations under `db/migrations/staged/` are **not** auto-applied by
`tools/postgres-ia/apply-migrations.mjs`. Use this directory for migrations
that depend on out-of-band consumer cutover before they are safe to ship.

## Promotion checklist

To promote a staged migration:

1. Verify all consumer wiring is in place (code + Unity).
2. Re-run `npm run db:audit-pre-spine` against current DB → confirm legacy
   table reads are zero.
3. `git mv db/migrations/staged/{file}.sql db/migrations/{file}.sql`
4. Run `npm run db:migrate` locally.
5. Run `npm run validate:catalog-spine`.
6. Ship as its own PR.

## Current contents

| File | Holds for | Promotion gate |
|---|---|---|
| `0023_catalog_legacy_drop.sql` | Consumer cutover from legacy `catalog_*` tables to spine | Unity `ZoneSubTypeRegistry` no longer reads `catalog_asset.id` directly; web/MCP catalog API reads spine; audit confirms zero legacy reads |
