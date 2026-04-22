/**
 * Grid asset catalog → snapshot (Stage 2.1).
 * Default: write `Assets/StreamingAssets/catalog/grid-asset-catalog-snapshot.json`.
 * `--check` for CI drift: TECH-666.
 */
import { mkdirSync, writeFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

import { buildCatalogSnapshot } from "@/lib/catalog/build-catalog-snapshot";
import { loadCatalogForExport } from "@/lib/catalog/load-catalog-for-export";
import { stableJsonStringify } from "@/lib/catalog/stable-json-stringify";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..");

/** Default Unity raw JSON path (StreamingAssets). @see TECH-664 */
export const DEFAULT_SNAPSHOT_RELPATH =
  "Assets/StreamingAssets/catalog/grid-asset-catalog-snapshot.json";

function parse(argv: string[]): {
  includeDrafts: boolean;
  help: boolean;
  stdout: boolean;
  outOverride: string | null;
} {
  let includeDrafts = false;
  let help = false;
  let stdout = false;
  let outOverride: string | null = null;
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === "--include-drafts") includeDrafts = true;
    if (a === "--help" || a === "-h") help = true;
    if (a === "--stdout") stdout = true;
    if (a === "--out" && argv[i + 1]) outOverride = argv[++i] as string;
  }
  return { includeDrafts, help, stdout, outOverride };
}

function resolveOutPath(override: string | null): string {
  if (!override) return path.join(REPO_ROOT, DEFAULT_SNAPSHOT_RELPATH);
  return path.isAbsolute(override) ? override : path.join(REPO_ROOT, override);
}

async function main(): Promise<void> {
  const { includeDrafts, help, stdout, outOverride } = parse(
    process.argv.slice(2),
  );
  if (help) {
    console.log(
      `usage: npx tsx web/scripts/catalog-export-cli.ts [options]\n` +
        `  (default) writes ${DEFAULT_SNAPSHOT_RELPATH}\n` +
        `  --stdout          print JSON only\n` +
        `  --include-drafts  include draft rows\n` +
        `  --out <path>      repo-relative or absolute output file\n`,
    );
    process.exit(0);
  }

  const loaded = await loadCatalogForExport({ includeDrafts });
  const snapshot = buildCatalogSnapshot(loaded, { includeDrafts });
  const text = stableJsonStringify(snapshot);
  const bytes = Buffer.from(text, "utf8");
  const out = resolveOutPath(outOverride);

  if (stdout) {
    process.stdout.write(text);
    return;
  }

  mkdirSync(path.dirname(out), { recursive: true });
  writeFileSync(out, bytes);
  // eslint-disable-next-line no-console
  console.error(`Wrote ${out} (${bytes.length} bytes)`);
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
