#!/usr/bin/env node
/**
 * validate-catalog-naming.mjs — lint catalog_entity.slug against {purpose}-{kind} convention.
 *
 * Reads DATABASE_URL from env / .env file.
 * Queries `catalog_entity` table; applies slug regex + kind-suffix checks.
 * Lint mode (default): emits offender table, exits 0.
 * Hard-fail mode (--hard-fail flag): exits 1 when offenders found.
 *
 * TECH-17996 (game-ui-catalog-bake Stage 9.11).
 */

import { readFileSync, existsSync } from "node:fs";
import { resolve, dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const REPO_ROOT = resolve(__dirname, "../..");

// Hard-fail is the default. Pass --lint to run in warning-only mode (no CI fail).
// TECH-17998: flipped from lint-default to hard-fail-default post Stage 9.11-T3 migration.
const HARD_FAIL = !process.argv.includes("--lint");

// ---------------------------------------------------------------------------
// Slug convention
// ---------------------------------------------------------------------------

/** Regex: ^[a-z][a-z0-9]+(-[a-z0-9]+)*$ */
const SLUG_RE = /^[a-z][a-z0-9]+(-[a-z0-9]+)*$/;

/** Allowed kind suffixes (last hyphen-segment of slug). */
const ALLOWED_KIND_SUFFIXES = new Set([
  "button",
  "display",
  "readout",
  "picker",
  "panel",
  "icon",
]);

/**
 * Catalog entity kinds that require the {purpose}-{kind} suffix convention.
 * Archetype / pool / audio slugs follow their own naming rules.
 */
const CHECKED_KINDS = new Set(["button", "panel", "sprite", "token", "asset"]);

/**
 * Validate one slug. Returns array of violation strings (empty = valid).
 * Exported for unit testing.
 * @param {string} slug
 * @param {string} [catalogKind] — when provided, skips kind-suffix check for exempt kinds
 */
export function validateSlug(slug, catalogKind) {
  const violations = [];

  if (!SLUG_RE.test(slug)) {
    if (/\(\d+\)/.test(slug)) {
      violations.push("trailing (N) segment forbidden");
    } else if (/[A-Z]/.test(slug)) {
      violations.push("uppercase letter forbidden");
    } else if (/[_]/.test(slug)) {
      violations.push("underscore forbidden — use hyphen");
    } else if (/^\d/.test(slug)) {
      violations.push("leading digit forbidden");
    } else {
      violations.push(`fails slug regex ^[a-z][a-z0-9]+(-[a-z0-9]+)*$`);
    }
    return violations;
  }

  // Regex passes — check kind suffix
  const segments = slug.split("-");
  const lastSeg = segments[segments.length - 1];

  // Check all-numeric trailing segment (e.g. base-72, row-2) — always forbidden
  if (segments.length >= 2 && /^\d+$/.test(lastSeg)) {
    violations.push("all-numeric trailing segment forbidden (ambiguous ordinal)");
    return violations;
  }

  // Kind-suffix check only applies to game-UI entity kinds
  const skipKindSuffixCheck = catalogKind && !CHECKED_KINDS.has(catalogKind);
  if (!skipKindSuffixCheck && !ALLOWED_KIND_SUFFIXES.has(lastSeg)) {
    violations.push(
      `missing allowed kind suffix; last segment '${lastSeg}' not in {${[...ALLOWED_KIND_SUFFIXES].join(",")}}`
    );
  }

  // Slug must have >=2 segments (purpose + kind)
  if (segments.length < 2) {
    violations.push("missing purpose prefix — need at least {purpose}-{kind}");
  }

  return violations;
}

/**
 * Suggest a rename for a slug heuristically.
 * Not guaranteed correct — human review required.
 */
function suggestRename(slug, kind) {
  // Normalise underscores → hyphens
  let candidate = slug.replace(/_/g, "-").toLowerCase();
  // Strip trailing (N) like "button (5)"
  candidate = candidate.replace(/\s*\(\d+\)\s*$/, "");
  // Strip trailing digits-only segments
  candidate = candidate.replace(/-\d+$/, "");
  // If last segment not allowed kind suffix, append kind from catalog_entity.kind
  const segments = candidate.split("-");
  const last = segments[segments.length - 1];
  if (!ALLOWED_KIND_SUFFIXES.has(last) && kind && ALLOWED_KIND_SUFFIXES.has(kind)) {
    candidate = `${candidate}-${kind}`;
  }
  return candidate;
}

// ---------------------------------------------------------------------------
// DB helpers
// ---------------------------------------------------------------------------

function loadEnv() {
  const envPath = resolve(REPO_ROOT, ".env");
  if (existsSync(envPath)) {
    const lines = readFileSync(envPath, "utf8").split("\n");
    for (const line of lines) {
      const m = line.match(/^([A-Z_]+)=(.*)$/);
      if (m && !process.env[m[1]]) {
        process.env[m[1]] = m[2].trim();
      }
    }
  }
}

async function queryRows(databaseUrl) {
  // Use createRequire pointed at postgres-ia package.json — same pg resolution as other scripts.
  const pgRequire = createRequire(join(REPO_ROOT, "tools/postgres-ia/package.json"));
  const pg = pgRequire("pg");
  const client = new pg.Client({ connectionString: databaseUrl });
  await client.connect();
  try {
    const res = await client.query("SELECT id, slug, kind FROM catalog_entity ORDER BY slug");
    return res.rows;
  } finally {
    await client.end();
  }
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

async function main() {
  loadEnv();
  const databaseUrl = process.env.DATABASE_URL;
  if (!databaseUrl) {
    console.error("[catalog-naming] ERROR: DATABASE_URL not set");
    process.exit(2);
  }

  let rows;
  try {
    rows = await queryRows(databaseUrl);
  } catch (err) {
    console.error(`[catalog-naming] DB query failed: ${err.message}`);
    process.exit(2);
  }

  const offenders = [];
  for (const row of rows) {
    const violations = validateSlug(row.slug, row.kind);
    if (violations.length > 0) {
      offenders.push({
        id: row.id,
        slug: row.slug,
        kind: row.kind,
        violations,
        suggestion: suggestRename(row.slug, row.kind),
      });
    }
  }

  if (offenders.length === 0) {
    console.log("[catalog-naming] All slugs pass convention. OK.");
    process.exit(0);
  }

  // Emit offender table
  console.log(`[catalog-naming] ${offenders.length} rows fail convention:\n`);
  console.log(
    `  ${"slug".padEnd(30)} ${"kind".padEnd(12)} ${"suggestion".padEnd(30)} violation`
  );
  console.log(`  ${"-".repeat(90)}`);
  for (const o of offenders) {
    console.log(
      `  ✗ ${o.slug.padEnd(28)} ${o.kind.padEnd(12)} ${o.suggestion.padEnd(30)} ${o.violations.join("; ")}`
    );
  }

  if (HARD_FAIL) {
    console.error(
      `\n[catalog-naming] FAIL: ${offenders.length} slug(s) violate {purpose}-{kind} convention. Run migration or pass --lint for warning-only mode.`
    );
    process.exit(1);
  } else {
    console.log(
      `\n[catalog-naming] ${offenders.length} rows fail convention; --lint mode (no CI fail)`
    );
    process.exit(0);
  }
}

main().catch((err) => {
  console.error("[catalog-naming] unexpected error:", err);
  process.exit(2);
});
