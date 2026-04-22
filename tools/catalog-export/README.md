# catalog-export (Stage 2.1)

Implementation lives under `web/lib/catalog/` and `web/scripts/catalog-export-cli.ts` (Next.js `web` package keeps `@/` resolution for DB + DTOs).

From repo root:

```bash
DATABASE_URL=… npm run catalog:export
```

Writes `Assets/StreamingAssets/catalog/grid-asset-catalog-snapshot.json` by default (override with `--out` or `--stdout`).

Default filter: `published` only. `npm run catalog:export -- --include-drafts` for draft+published.
