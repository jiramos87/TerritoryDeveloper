#!/usr/bin/env tsx
/**
 * gc-catalog.ts — Stage 18.1 / TECH-8604
 *
 * Two-mode catalog GC sweep:
 *
 *   --mode retired  Hard-delete catalog_entity rows where retired_at is older
 *                   than RETIRED_AGE_DAYS (default 30). Emits per-kind counts.
 *
 *   --mode orphan   Walk BLOB_ROOT (default var/blobs/), reconstruct each
 *                   blob's gen://{run_id}/{variant} URI, diff against
 *                   sprite_detail.source_uri set. Logs JSON
 *                   {date, count, paths[]} to data/state/orphan-blobs/. No
 *                   delete (manual review gate).
 *
 *   default         Run both modes.
 *
 * Inputs: DATABASE_URL or config/postgres-dev.json (resolveDatabaseUrl).
 * Exit codes: 0 ok, 1 DB unreachable or unrecoverable error.
 */

import { mkdirSync, readdirSync, statSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { Client } from "pg";
// @ts-expect-error — sibling .mjs without typings is intentional.
import { resolveDatabaseUrl } from "./resolve-database-url.mjs";

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, "../..");
const ORPHAN_DIR = join(REPO_ROOT, "data/state/orphan-blobs");
const DEFAULT_BLOB_ROOT = join(REPO_ROOT, "var/blobs");
const RETIRED_AGE_DAYS = Number.parseInt(
  process.env.RETIRED_AGE_DAYS ?? "30",
  10,
);

type Mode = "retired" | "orphan" | "both";

interface CliArgs {
  mode: Mode;
  date: string;
  blobRoot: string;
  dryRun: boolean;
}

function parseArgs(argv: string[]): CliArgs {
  let mode: Mode = "both";
  let date = new Date().toISOString().slice(0, 10);
  let blobRoot = process.env.BLOB_ROOT ?? DEFAULT_BLOB_ROOT;
  let dryRun = false;

  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === "--mode") {
      const v = argv[++i];
      if (v !== "retired" && v !== "orphan" && v !== "both") {
        die(`invalid --mode: ${v}`);
      }
      mode = v;
    } else if (a?.startsWith("--mode=")) {
      const v = a.slice("--mode=".length);
      if (v !== "retired" && v !== "orphan" && v !== "both") {
        die(`invalid --mode: ${v}`);
      }
      mode = v;
    } else if (a === "--date") {
      date = argv[++i] ?? date;
    } else if (a?.startsWith("--date=")) {
      date = a.slice("--date=".length);
    } else if (a === "--blob-root") {
      blobRoot = argv[++i] ?? blobRoot;
    } else if (a?.startsWith("--blob-root=")) {
      blobRoot = a.slice("--blob-root=".length);
    } else if (a === "--dry-run") {
      dryRun = true;
    } else if (a === "-h" || a === "--help") {
      printHelp();
      process.exit(0);
    } else if (a) {
      die(`unknown flag: ${a}`);
    }
  }

  if (!/^\d{4}-\d{2}-\d{2}$/.test(date)) {
    die(`invalid --date: ${date}`);
  }
  return { mode, date, blobRoot, dryRun };
}

function printHelp(): void {
  console.log(
    `gc-catalog.ts [--mode retired|orphan|both] [--date YYYY-MM-DD] [--blob-root PATH] [--dry-run]`,
  );
}

function die(msg: string): never {
  console.error(`gc-catalog: error: ${msg}`);
  process.exit(1);
}

async function sweepRetired(
  client: Client,
  dryRun: boolean,
): Promise<Record<string, number>> {
  const previewSql = `
    SELECT kind, count(*)::int AS n
    FROM catalog_entity
    WHERE retired_at IS NOT NULL
      AND retired_at < now() - ($1::int || ' days')::interval
    GROUP BY kind
    ORDER BY kind
  `;
  const preview = await client.query<{ kind: string; n: number }>(previewSql, [
    RETIRED_AGE_DAYS,
  ]);
  const counts: Record<string, number> = {};
  for (const row of preview.rows) counts[row.kind] = row.n;

  if (!dryRun && preview.rows.length > 0) {
    const deleteSql = `
      DELETE FROM catalog_entity
      WHERE retired_at IS NOT NULL
        AND retired_at < now() - ($1::int || ' days')::interval
    `;
    await client.query(deleteSql, [RETIRED_AGE_DAYS]);
  }
  return counts;
}

interface OrphanResult {
  date: string;
  count: number;
  paths: string[];
}

function walkBlobs(blobRoot: string): string[] {
  const out: string[] = [];
  let runIds: string[];
  try {
    runIds = readdirSync(blobRoot);
  } catch {
    return out;
  }
  for (const runId of runIds) {
    if (runId.startsWith(".")) continue;
    const runDir = join(blobRoot, runId);
    let runStat;
    try {
      runStat = statSync(runDir);
    } catch {
      continue;
    }
    if (!runStat.isDirectory()) continue;
    let variants: string[];
    try {
      variants = readdirSync(runDir);
    } catch {
      continue;
    }
    for (const fname of variants) {
      const m = /^(\d+)\.png$/.exec(fname);
      if (!m) continue;
      out.push(`gen://${runId}/${m[1]}`);
    }
  }
  return out;
}

async function sweepOrphans(
  client: Client,
  blobRoot: string,
  date: string,
): Promise<OrphanResult> {
  const fsUris = walkBlobs(blobRoot);
  const dbResult = await client.query<{ source_uri: string }>(
    `SELECT source_uri FROM sprite_detail WHERE source_uri LIKE 'gen://%'`,
  );
  const referenced = new Set<string>();
  for (const row of dbResult.rows) referenced.add(row.source_uri);
  const orphans = fsUris.filter((uri) => !referenced.has(uri)).sort();

  mkdirSync(ORPHAN_DIR, { recursive: true });
  const out: OrphanResult = { date, count: orphans.length, paths: orphans };
  const outPath = join(ORPHAN_DIR, `${date}.json`);
  writeFileSync(outPath, `${JSON.stringify(out, null, 2)}\n`, "utf8");
  return out;
}

async function main(): Promise<void> {
  const args = parseArgs(process.argv.slice(2));
  const databaseUrl = resolveDatabaseUrl(REPO_ROOT);
  if (!databaseUrl) die("DATABASE_URL not resolvable (CI without env)");

  const client = new Client({ connectionString: databaseUrl });
  try {
    await client.connect();
  } catch (err) {
    die(`DB connect failed: ${(err as Error).message}`);
  }

  try {
    const ts = new Date().toISOString();
    if (args.mode === "retired" || args.mode === "both") {
      const counts = await sweepRetired(client, args.dryRun);
      const total = Object.values(counts).reduce((a, b) => a + b, 0);
      console.log(
        `${ts} gc-catalog retired ok deleted=${total} ${
          args.dryRun ? "[dry-run] " : ""
        }per_kind=${JSON.stringify(counts)} threshold_days=${RETIRED_AGE_DAYS}`,
      );
    }
    if (args.mode === "orphan" || args.mode === "both") {
      const result = await sweepOrphans(client, args.blobRoot, args.date);
      console.log(
        `${ts} gc-catalog orphan ok count=${result.count} dest=${join(
          ORPHAN_DIR,
          `${args.date}.json`,
        )} blob_root=${args.blobRoot}`,
      );
    }
  } finally {
    await client.end();
  }
}

main().catch((err) => {
  console.error(`gc-catalog: fatal: ${(err as Error).stack ?? err}`);
  process.exit(1);
});
