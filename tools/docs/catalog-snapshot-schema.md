# catalog snapshot JSON (Stage 2.1)

Hand-written DTOs in `web/lib/catalog/build-catalog-snapshot.ts` and `web/types/api/catalog-*.ts`. The export CLI writes a single JSON object with this top-level shape:

- `schemaVersion` (integer) — bump when a breaking change would confuse Unity; keep in lockstep with `CATALOG_SNAPSHOT_SCHEMA_VERSION` in the builder.
- `generatedAt` (ISO-8601 UTC string)
- `includeDrafts` (boolean) — `true` when the CLI was run with `--include-drafts`
- `assets`, `sprites`, `bindings`, `economy` — arrays; row shapes match 0011/0012 migrations
- `importHygiene` — sorted by `texturePath`; PPU and pivot for allowlisted `TextureImporter` policy (see `docs/grid-asset-visual-registry-exploration.md` §6)

**Stable bytes:** the CLI runs `stableJsonStringify` (sorted object keys, 2-space indent, trailing newline) so the same DB state yields the same file bytes (except `generatedAt`, which is rewritten each run).

**CI drift (`npm run catalog:export:check`):** compares stable JSON of everything except `generatedAt` so scheduled exports do not spuriously fail checks.
