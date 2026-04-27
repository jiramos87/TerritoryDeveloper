#!/usr/bin/env node
/**
 * backfill-arch-surfaces.mjs
 *
 * One-shot driver that walks every open `ia_master_plans` row + each Stage,
 * infers `arch_surfaces[]` candidates from the Stage body via:
 *   - substring scan of `arch_surfaces.spec_path` mentions
 *   - substring scan of `arch_surfaces.slug` mentions
 *   - substring scan of `arch_surfaces.spec_section` mentions
 * then writes confident-single-match candidates into `stage_arch_surfaces`
 * (PK = (slug, stage_id, surface_slug)) and prints ambiguous / 0-candidate
 * Stages as a stdout markdown polling list.
 *
 * Idempotent: PK collision = silent skip on re-run; second run yields zero
 * inserted rows. NEVER inserts into `arch_surfaces` (Invariant #12 — link
 * existing rows only).
 *
 * Flags:
 *   --dry-run         Preview only; no DB writes (still prints summary +
 *                     polling list).
 *   --plan SLUG       Scope to one master plan; default = all open plans.
 *
 * Exit codes:
 *   0  Clean run.
 *   1  DB connection / query error.
 *   2  Invariant violation guard (e.g. unknown slug surfaced in candidate
 *      set).
 */

import { parseArgs } from "node:util";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";
import { resolveDatabaseUrl } from "../postgres-ia/resolve-database-url.mjs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = process.env.IA_REPO_ROOT ?? path.resolve(__dirname, "../..");

// Reuse the workspace-installed `pg` from tools/postgres-ia (no root install).
const pgRequire = createRequire(
  path.join(REPO_ROOT, "tools/postgres-ia/package.json"),
);
const pg = pgRequire("pg");

const { values: flags } = parseArgs({
  options: {
    "dry-run": { type: "boolean", default: false },
    plan: { type: "string" },
  },
  strict: false,
});

const DRY_RUN = flags["dry-run"];
const PLAN_SCOPE = flags.plan ?? null;

const conn = resolveDatabaseUrl(REPO_ROOT);
if (!conn) {
  console.error("DATABASE_URL not resolvable — aborting");
  process.exit(1);
}

const client = new pg.Client({ connectionString: conn });

try {
  await client.connect();
} catch (err) {
  console.error(`DB connect failed: ${err.message}`);
  process.exit(1);
}

// ---------------------------------------------------------------------------
// Load arch_surfaces inventory.
// ---------------------------------------------------------------------------

const surfacesRes = await client.query(
  `SELECT slug, kind, spec_path, spec_section FROM arch_surfaces ORDER BY slug`,
);
const surfaces = surfacesRes.rows;

if (surfaces.length === 0) {
  console.error("no rows in arch_surfaces — Stage 1.1 seed missing; aborting");
  await client.end();
  process.exit(1);
}

// ---------------------------------------------------------------------------
// Walk plans + stages.
// ---------------------------------------------------------------------------

const planQuery = PLAN_SCOPE
  ? `SELECT slug FROM ia_master_plans WHERE slug = $1 ORDER BY slug`
  : `SELECT slug FROM ia_master_plans ORDER BY slug`;
const planArgs = PLAN_SCOPE ? [PLAN_SCOPE] : [];
const plansRes = await client.query(planQuery, planArgs);

if (plansRes.rowCount === 0) {
  console.error(
    PLAN_SCOPE
      ? `no master plan found for slug=${PLAN_SCOPE}`
      : "no master plans in ia_master_plans",
  );
  await client.end();
  process.exit(1);
}

let stagesWalked = 0;
let confidentWritten = 0;
let ambiguousCount = 0;
let noneEligibleCount = 0;
const polling = [];

for (const { slug } of plansRes.rows) {
  const stagesRes = await client.query(
    `SELECT stage_id, body, objective, exit_criteria
       FROM ia_stages
      WHERE slug = $1
      ORDER BY stage_id`,
    [slug],
  );

  for (const stage of stagesRes.rows) {
    stagesWalked += 1;
    const haystack = [stage.body ?? "", stage.objective ?? "", stage.exit_criteria ?? ""]
      .join("\n")
      .toLowerCase();

    const candidates = new Set();
    for (const s of surfaces) {
      const spec_path = (s.spec_path ?? "").toLowerCase();
      const spec_section = (s.spec_section ?? "").toLowerCase();
      const surfaceSlug = s.slug.toLowerCase();
      let hit = false;
      if (spec_path && haystack.includes(spec_path)) hit = true;
      if (!hit && surfaceSlug && haystack.includes(surfaceSlug)) hit = true;
      if (
        !hit &&
        spec_section &&
        spec_section.length >= 6 &&
        haystack.includes(spec_section)
      ) {
        hit = true;
      }
      if (hit) candidates.add(s.slug);
    }

    // Skip stages already linked (idempotent guard — also covered by PK).
    const existingRes = await client.query(
      `SELECT surface_slug FROM stage_arch_surfaces WHERE slug = $1 AND stage_id = $2`,
      [slug, stage.stage_id],
    );
    const alreadyLinked = new Set(existingRes.rows.map((r) => r.surface_slug));
    const newCandidates = [...candidates].filter((c) => !alreadyLinked.has(c));

    if (alreadyLinked.size > 0 && newCandidates.length === 0) {
      // Stage already resolved; skip silently.
      continue;
    }

    if (newCandidates.length === 1 && alreadyLinked.size === 0) {
      // Confident single-match — write directly.
      const surfaceSlug = newCandidates[0];
      if (!DRY_RUN) {
        await client.query(
          `INSERT INTO stage_arch_surfaces (slug, stage_id, surface_slug)
             VALUES ($1, $2, $3)
             ON CONFLICT (slug, stage_id, surface_slug) DO NOTHING`,
          [slug, stage.stage_id, surfaceSlug],
        );
      }
      confidentWritten += 1;
      console.log(
        `[${DRY_RUN ? "dry" : "wet"}] LINK ${slug} ${stage.stage_id} → ${surfaceSlug}`,
      );
      continue;
    }

    if (newCandidates.length >= 2) {
      ambiguousCount += 1;
      polling.push({
        slug,
        stage_id: stage.stage_id,
        kind: "ambiguous",
        candidates: newCandidates,
      });
      continue;
    }

    if (newCandidates.length === 0 && alreadyLinked.size === 0) {
      noneEligibleCount += 1;
      polling.push({
        slug,
        stage_id: stage.stage_id,
        kind: "none-eligible",
        candidates: [],
      });
    }
  }
}

// ---------------------------------------------------------------------------
// Invariant guard — confirm arch_surfaces row count unchanged (sanity check).
// ---------------------------------------------------------------------------

const postCountRes = await client.query(`SELECT count(*)::int AS n FROM arch_surfaces`);
const postCount = postCountRes.rows[0].n;
if (postCount !== surfaces.length) {
  console.error(
    `INVARIANT VIOLATION: arch_surfaces row count changed (${surfaces.length} → ${postCount}); script must NEVER mutate arch_surfaces`,
  );
  await client.end();
  process.exit(2);
}

// ---------------------------------------------------------------------------
// Polling list — stdout markdown.
// ---------------------------------------------------------------------------

if (polling.length > 0) {
  console.log("");
  console.log("## Polling list (manual resolution required)");
  console.log("");
  console.log("| Plan slug | Stage | Kind | Candidates |");
  console.log("|---|---|---|---|");
  for (const row of polling) {
    const cands = row.candidates.length > 0 ? row.candidates.join(", ") : "_none_";
    console.log(`| ${row.slug} | ${row.stage_id} | ${row.kind} | ${cands} |`);
  }
}

// ---------------------------------------------------------------------------
// Summary footer.
// ---------------------------------------------------------------------------

console.log("");
console.log(
  `summary: ${stagesWalked} stages walked, ${confidentWritten} confident links${DRY_RUN ? " (dry-run)" : " written"}, ${ambiguousCount} ambiguous polling, ${noneEligibleCount} explicit-none-eligible`,
);

await client.end();
process.exit(0);
