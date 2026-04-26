#!/usr/bin/env tsx
/**
 * validate-blob-roots.ts — TECH-1436.
 *
 * Stage 3.1 gate (asset-pipeline). Walks every `gen://` URI stored in
 * `sprite_detail.source_uri` (and `audio_detail.source_uri` when that table
 * exists) and asserts each resolves to a real file on disk via
 * `BlobResolver` (TECH-1435 / DEC-A25 swap point).
 *
 * Read-only. Never mutates DB state.
 *
 * Inputs: DATABASE_URL or `config/postgres-dev.json` (resolveDatabaseUrl).
 *         BLOB_ROOT env var honored by `BlobResolver`; defaults to
 *         repo-root `var/blobs/`.
 *
 * Test stub: env `BLOB_ROOTS_FAKE_ROWS` (JSON) bypasses Postgres for unit
 * tests. Shape:
 *   {
 *     "sprite": [{ "entity_id": 1, "slug": "foo", "source_uri": "gen://r/0" }],
 *     "audio":  [{ "entity_id": 9, "slug": "bar", "source_uri": "gen://r/1" }] | null
 *   }
 * `audio: null` simulates the table being absent (Stage 9 not yet shipped).
 *
 * Exit codes:
 *   0  every `gen://` URI resolves on disk (or zero rows present).
 *   1  one or more URIs fail to resolve, OR DB unreachable / preflight fail.
 */

import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import * as fs from "node:fs";
import {
  BlobResolver,
  MalformedBlobUriError,
  UnsupportedSchemeError,
} from "../../../web/lib/blob-resolver";
import { resolveDatabaseUrl } from "../../postgres-ia/resolve-database-url.mjs";

// `pg` lives under `tools/postgres-ia/node_modules/`; resolve it at runtime
// from the sibling validator dir so this script stays runnable from
// `tools/scripts/validate/`. Only loaded on the live-DB code path —
// `BLOB_ROOTS_FAKE_ROWS` short-circuits before this fires.
type PgClient = {
  connect: () => Promise<void>;
  end: () => Promise<void>;
  query: (q: string, p?: unknown[]) => Promise<{ rows: unknown[] }>;
};
async function loadPgClient(connectionString: string): Promise<PgClient> {
  const pgModulePath = resolve(
    fileURLToPath(import.meta.url),
    "..",
    "..",
    "..",
    "postgres-ia",
    "node_modules",
    "pg",
    "lib",
    "index.js",
  );
  const mod = (await import(pgModulePath)) as {
    default?: { Client: new (cfg: { connectionString: string }) => PgClient };
    Client?: new (cfg: { connectionString: string }) => PgClient;
  };
  const Ctor = mod.Client ?? mod.default?.Client;
  if (!Ctor) {
    throw new Error("pg module did not expose a Client constructor");
  }
  return new Ctor({ connectionString });
}

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, "..", "..", "..");

interface Row {
  entity_id: number | string;
  slug: string;
  source_uri: string;
}

interface FakeRows {
  sprite?: Row[];
  audio?: Row[] | null;
}

interface Miss {
  kind: "sprite" | "audio";
  entity_id: number | string;
  slug: string;
  source_uri: string;
  resolved_path: string;
  reason: string;
}

function redact(url: string): string {
  try {
    const u = new URL(url);
    if (u.password) u.password = "***";
    return u.toString();
  } catch {
    return url;
  }
}

async function tableExists(client: PgClient, name: string): Promise<boolean> {
  const { rows } = await client.query(
    `SELECT 1 FROM information_schema.tables
      WHERE table_schema = 'public' AND table_name = $1`,
    [name],
  );
  return rows.length > 0;
}

async function loadRows(): Promise<{
  sprite: Row[];
  audio: Row[] | null;
  source: string;
}> {
  const fake = process.env.BLOB_ROOTS_FAKE_ROWS;
  if (fake) {
    let parsed: FakeRows;
    try {
      parsed = JSON.parse(fake);
    } catch (err) {
      throw new Error(
        `BLOB_ROOTS_FAKE_ROWS is not valid JSON: ${(err as Error).message}`,
      );
    }
    return {
      sprite: parsed.sprite ?? [],
      audio: parsed.audio === undefined ? [] : parsed.audio,
      source: "fake",
    };
  }

  const url = resolveDatabaseUrl(REPO_ROOT);
  if (!url) {
    throw new Error("no DATABASE_URL or config/postgres-dev.json available");
  }
  const client = await loadPgClient(url);
  await client.connect();
  try {
    if (!(await tableExists(client, "sprite_detail"))) {
      throw new Error(
        "sprite_detail table missing — run db:migrate before validate:blob-roots",
      );
    }
    const sprite = (
      await client.query(
        `SELECT entity_id, source_uri,
                COALESCE(
                  (SELECT slug FROM catalog_entity ce WHERE ce.id = sd.entity_id),
                  ''
                ) AS slug
           FROM sprite_detail sd
          WHERE source_uri LIKE 'gen://%'
          ORDER BY entity_id`,
      )
    ).rows as unknown as Row[];

    let audio: Row[] | null = null;
    if (await tableExists(client, "audio_detail")) {
      audio = (
        await client.query(
          `SELECT entity_id, source_uri,
                  COALESCE(
                    (SELECT slug FROM catalog_entity ce WHERE ce.id = ad.entity_id),
                    ''
                  ) AS slug
             FROM audio_detail ad
            WHERE source_uri LIKE 'gen://%'
            ORDER BY entity_id`,
        )
      ).rows as Row[];
    }
    return { sprite, audio, source: redact(url) };
  } finally {
    await client.end();
  }
}

async function checkRows(
  kind: "sprite" | "audio",
  rows: Row[],
  resolver: BlobResolver,
  misses: Miss[],
): Promise<void> {
  for (const row of rows) {
    let resolved: string;
    try {
      resolved = resolver.resolve(row.source_uri);
    } catch (err) {
      const reason =
        err instanceof UnsupportedSchemeError
          ? "unsupported scheme"
          : err instanceof MalformedBlobUriError
            ? "malformed gen:// URI"
            : `resolve error: ${(err as Error).message}`;
      misses.push({
        kind,
        entity_id: row.entity_id,
        slug: row.slug,
        source_uri: row.source_uri,
        resolved_path: "",
        reason,
      });
      continue;
    }
    try {
      await fs.promises.access(resolved, fs.constants.R_OK);
    } catch {
      misses.push({
        kind,
        entity_id: row.entity_id,
        slug: row.slug,
        source_uri: row.source_uri,
        resolved_path: resolved,
        reason: "blob file missing on disk",
      });
    }
  }
}

async function main(): Promise<void> {
  let rowsBundle: { sprite: Row[]; audio: Row[] | null; source: string };
  try {
    rowsBundle = await loadRows();
  } catch (err) {
    console.error(`[validate:blob-roots] ${(err as Error).message}`);
    process.exit(1);
  }

  if (rowsBundle.audio === null) {
    console.error(
      "[validate:blob-roots] audio_detail not yet present (Stage 9) — skipping audio scan",
    );
  }

  const resolver = new BlobResolver();
  const misses: Miss[] = [];
  await checkRows("sprite", rowsBundle.sprite, resolver, misses);
  if (rowsBundle.audio !== null) {
    await checkRows("audio", rowsBundle.audio, resolver, misses);
  }

  if (misses.length > 0) {
    console.error(
      `[validate:blob-roots] FAIL — ${misses.length} unresolved gen:// URI(s) (db=${rowsBundle.source}):`,
    );
    for (const m of misses) {
      console.error(JSON.stringify(m));
    }
    process.exit(1);
  }

  const audioCount =
    rowsBundle.audio === null ? "skipped" : String(rowsBundle.audio.length);
  console.log(
    `[validate:blob-roots] OK — sprite=${rowsBundle.sprite.length} audio=${audioCount} (db=${rowsBundle.source})`,
  );
}

main().catch((err) => {
  console.error("[validate:blob-roots] error:", err);
  process.exit(1);
});
