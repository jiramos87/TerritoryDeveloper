# catalog-export (Stage 2.1)

Implementation lives under `web/lib/catalog/` and `web/scripts/catalog-export-cli.ts` (Next.js `web` package keeps `@/` resolution for DB + DTOs).

From repo root:

```bash
DATABASE_URL=… npm run catalog:export
```

Default: `published` rows only. Pass `--include-drafts` to the script via `npm run catalog:export -- --include-drafts`.
