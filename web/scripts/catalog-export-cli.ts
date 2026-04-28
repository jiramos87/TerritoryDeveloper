/**
 * Catalog snapshot CLI — thin wrapper around `exportSnapshot` (TECH-2673, Stage 13.1).
 *
 * Writes the v2 per-kind JSON files + manifest under
 * `Assets/StreamingAssets/catalog/` via the canonical pipeline. Replaces the
 * legacy v1 single-file CLI; the old `--check` drift-compare mode is dropped
 * because v2 emits multiple files + a manifest hash that supersedes content
 * comparison (see `catalog_snapshot.hash` row).
 *
 * Usage:
 *   npx tsx web/scripts/catalog-export-cli.ts --author <uuid> [--include-drafts]
 *
 * @see ia/projects/asset-pipeline/stage-13.1 — TECH-2673 §Plan Digest
 */
import { exportSnapshot } from "@/lib/snapshot/export";

type ParsedArgs = {
  authorUserId: string | null;
  includeDrafts: boolean;
  help: boolean;
};

function parse(argv: string[]): ParsedArgs {
  let authorUserId: string | null = null;
  let includeDrafts = false;
  let help = false;
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === "--include-drafts") includeDrafts = true;
    else if (a === "--help" || a === "-h") help = true;
    else if (a === "--author" && argv[i + 1]) {
      authorUserId = argv[++i] as string;
    }
  }
  return { authorUserId, includeDrafts, help };
}

async function main(): Promise<void> {
  const { authorUserId, includeDrafts, help } = parse(process.argv.slice(2));
  if (help) {
    console.log(
      `usage: npx tsx web/scripts/catalog-export-cli.ts --author <uuid> [options]\n` +
        `  writes per-kind JSON files + manifest.json under Assets/StreamingAssets/catalog/\n` +
        `  --author <uuid>    required: users.id of the export author (FK target)\n` +
        `  --include-drafts   include draft entity_version rows in addition to published\n`,
    );
    process.exit(0);
  }
  if (!authorUserId) {
    console.error(
      "catalog:export: --author <uuid> is required (users.id of export author).",
    );
    process.exit(2);
  }

  const result = await exportSnapshot(authorUserId, { includeDrafts });
  console.error(
    `Wrote ${result.manifestPath} (snapshotId=${result.snapshotId} hash=${result.hash}).`,
  );
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
