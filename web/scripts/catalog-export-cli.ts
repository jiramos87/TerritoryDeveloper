/**
 * Grid asset catalog → snapshot (Stage 2.1).
 * Default: write `Assets/StreamingAssets/catalog/grid-asset-catalog-snapshot.json`.
 * `--check` compares on-disk JSON to a fresh export (see `snapshotForDriftCheck`).
 */
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

import {
  buildCatalogSnapshot,
  type CatalogSnapshotFile,
  snapshotForDriftCheck,
} from "@/lib/catalog/build-catalog-snapshot";
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
  check: boolean;
} {
  let includeDrafts = false;
  let help = false;
  let stdout = false;
  let outOverride: string | null = null;
  let check = false;
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === "--include-drafts") includeDrafts = true;
    if (a === "--help" || a === "-h") help = true;
    if (a === "--stdout") stdout = true;
    if (a === "--check") check = true;
    if (a === "--out" && argv[i + 1]) outOverride = argv[++i] as string;
  }
  return { includeDrafts, help, stdout, outOverride, check };
}

function resolveOutPath(override: string | null): string {
  if (!override) return path.join(REPO_ROOT, DEFAULT_SNAPSHOT_RELPATH);
  return path.isAbsolute(override) ? override : path.join(REPO_ROOT, override);
}

async function main(): Promise<void> {
  const { includeDrafts, help, stdout, outOverride, check } = parse(
    process.argv.slice(2),
  );
  if (help) {
    console.log(
      `usage: npx tsx web/scripts/catalog-export-cli.ts [options]\n` +
        `  (default) writes ${DEFAULT_SNAPSHOT_RELPATH}\n` +
        `  --stdout          print JSON only\n` +
        `  --include-drafts  include draft rows in export / check\n` +
        `  --out <path>      repo-relative or absolute output file\n` +
        `  --check           no write; fail if on-disk file drifts vs DB (omits generatedAt)\n`,
    );
    process.exit(0);
  }

  const out = resolveOutPath(outOverride);

  if (check) {
    const raw = readFileSync(out, "utf8");
    const disk = JSON.parse(raw) as CatalogSnapshotFile;
    const useDrafts = Boolean(disk.includeDrafts);
    const loaded = await loadCatalogForExport({ includeDrafts: useDrafts });
    const live = buildCatalogSnapshot(loaded, { includeDrafts: useDrafts });
    const a = snapshotForDriftCheck(disk);
    const b = snapshotForDriftCheck(live);
    if (a !== b) {
      // eslint-disable-next-line no-console
      console.error(
        `catalog:export --check: content drift (excluding generatedAt): ${out}`,
      );
      process.exit(1);
    }
    process.exit(0);
  }

  const loaded = await loadCatalogForExport({ includeDrafts });
  const snapshot = buildCatalogSnapshot(loaded, { includeDrafts });
  const text = stableJsonStringify(snapshot);
  const bytes = Buffer.from(text, "utf8");

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
