/**
 * Grid asset catalog → snapshot (Stage 2.1 — TECH-662+).
 * TECH-663: stdout-only snapshot. TECH-664: default file write.
 */
import { buildCatalogSnapshot } from "@/lib/catalog/build-catalog-snapshot";
import { loadCatalogForExport } from "@/lib/catalog/load-catalog-for-export";
import { stableJsonStringify } from "@/lib/catalog/stable-json-stringify";

function parse(
  argv: string[],
): { includeDrafts: boolean; help: boolean } {
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
    console.log(
      `usage: npx tsx web/scripts/catalog-export-cli.ts [--include-drafts]\n` +
        `  Emits versioned JSON snapshot to stdout. Default filter: published only.`,
    );
    process.exit(0);
  }
  const loaded = await loadCatalogForExport({ includeDrafts });
  const snapshot = buildCatalogSnapshot(loaded, { includeDrafts });
  process.stdout.write(stableJsonStringify(snapshot));
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
