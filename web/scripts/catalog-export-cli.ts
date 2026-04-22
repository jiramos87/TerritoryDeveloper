/**
 * Grid asset catalog load CLI (Stage 2.1 — TECH-662).
 * Snapshot serialization (TECH-663+) lands in follow-up commits.
 *
 * **Env:** `DATABASE_URL` required.
 */
import { loadCatalogForExport } from "@/lib/catalog/load-catalog-for-export";

function parse(argv: string[]): { includeDrafts: boolean; help: boolean } {
  let includeDrafts = false;
  let help = false;
  for (const a of argv) {
    if (a === "--include-drafts") includeDrafts = true;
    if (a === "--help" || a === "-h") help = true;
  }
  return { includeDrafts, help };
}

async function main(): Promise<void> {
  const { includeDrafts, help } = parse(process.argv.slice(2));
  if (help) {
    console.log(`usage: npx tsx web/scripts/catalog-export-cli.ts [--include-drafts]`);
    process.exit(0);
  }
  const loaded = await loadCatalogForExport({ includeDrafts });
  // eslint-disable-next-line no-console -- JSON contract for piping / inspection
  console.log(JSON.stringify(loaded, null, 2));
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
