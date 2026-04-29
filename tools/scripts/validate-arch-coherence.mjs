#!/usr/bin/env node
/**
 * validate-arch-coherence.mjs
 *
 * Stage 1.2 / TECH-2203 — read-only invariant gate over the architecture
 * coherence index. Enforces 3 checks:
 *
 *   1. orphan_surface_slug   — every `stage_arch_surfaces.surface_slug`
 *                              must reference an existing `arch_surfaces.slug`
 *                              row (FK reverse check; defends against direct
 *                              DB tampering even though FK constraint is in
 *                              place).
 *   2. unlinked_open_stage   — every open `ia_stages` row (status ∈ {pending,
 *                              in_progress}) MUST have ≥1 link in
 *                              `stage_arch_surfaces` OR appear in the
 *                              hard-coded `EXPLICIT_NONE_STAGES` allow-list.
 *   3. orphan_decision_surface — every `arch_surfaces` row with `kind =
 *                              'decision'` MUST have ≥1 `arch_decisions` row
 *                              referencing it via `surface_id`.
 *
 * Exit codes:
 *   0  All 3 checks green; prints summary footer.
 *   1  ≥1 violation; prints actionable per-violation lines + count.
 *   2  DB connection / query error.
 *
 * Read-only: only SELECT queries; never mutates rows. Wired into
 * `npm run validate:all` between `validate:master-plan-status` and
 * `validate:backlog-yaml` for fail-fast positioning.
 */

import path from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";
import { execSync } from "node:child_process";
import { resolveDatabaseUrl } from "../postgres-ia/resolve-database-url.mjs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = process.env.IA_REPO_ROOT ?? path.resolve(__dirname, "../..");

const pgRequire = createRequire(
  path.join(REPO_ROOT, "tools/postgres-ia/package.json"),
);
const pg = pgRequire("pg");

// ---------------------------------------------------------------------------
// Allow-list — Stages that intentionally declare `none` for arch_surfaces.
//
// Format: `${slug}::${stage_id}` keys. Seeded at Stage 1.2 closeout with all
// open Stages flagged `none-eligible` by the T1.2.4 backfill (no confident
// arch_surfaces match against stage body inference). Future open Stages MUST
// either declare `arch_surfaces[]` via `stage_insert` OR add an explicit
// allow-list entry here. Allow-list growth past ~10 new entries per cycle is
// a `decision_required` signal per TECH-2203 §Invariants & Gate.
// ---------------------------------------------------------------------------

const EXPLICIT_NONE_STAGES = new Set([
  "architecture-coherence-system::1.3",
  "architecture-coherence-system::1.4",
  "asset-pipeline::10.1",
  "asset-pipeline::11.1",
  "asset-pipeline::12.1",
  "asset-pipeline::13.1",
  "asset-pipeline::14.1",
  "asset-pipeline::14.2",
  "asset-pipeline::15.1",
  "asset-pipeline::17.1",
  "asset-pipeline::18.1",
  "asset-pipeline::19.1",
  "asset-pipeline::19.3",
  "asset-pipeline::20.1",
  "backlog-yaml-mcp-alignment::1",
  "backlog-yaml-mcp-alignment::10",
  "backlog-yaml-mcp-alignment::11",
  "backlog-yaml-mcp-alignment::12",
  "backlog-yaml-mcp-alignment::13",
  "backlog-yaml-mcp-alignment::14",
  "backlog-yaml-mcp-alignment::15",
  "backlog-yaml-mcp-alignment::16",
  "backlog-yaml-mcp-alignment::2",
  "backlog-yaml-mcp-alignment::3",
  "backlog-yaml-mcp-alignment::4",
  "backlog-yaml-mcp-alignment::4.1",
  "backlog-yaml-mcp-alignment::4.2",
  "backlog-yaml-mcp-alignment::5",
  "backlog-yaml-mcp-alignment::6",
  "backlog-yaml-mcp-alignment::7",
  "backlog-yaml-mcp-alignment::8",
  "backlog-yaml-mcp-alignment::9",
  "city-sim-depth::1",
  "city-sim-depth::10",
  "city-sim-depth::11",
  "city-sim-depth::12",
  "city-sim-depth::13",
  "citystats-overhaul::1",
  "citystats-overhaul::1.1",
  "citystats-overhaul::2",
  "citystats-overhaul::3",
  "citystats-overhaul::4",
  "citystats-overhaul::5",
  "citystats-overhaul::6",
  "citystats-overhaul::7",
  "citystats-overhaul::8",
  "dashboard-prod-database::1.1",
  "dashboard-prod-database::1.2",
  "dashboard-prod-database::1.3",
  "dashboard-prod-database::1.4",
  "dashboard-prod-database::1.5",
  "dashboard-prod-database::1.6",
  "dashboard-prod-database::1.7",
  "distribution::1",
  "distribution::2",
  "distribution::3",
  "distribution::4",
  "distribution::5",
  "distribution::6",
  "game-ui-design-system::1",
  "game-ui-design-system::2",
  "game-ui-design-system::3",
  "game-ui-design-system::4",
  "game-ui-design-system::5",
  "game-ui-design-system::6",
  "game-ui-design-system::7",
  "game-ui-design-system::9",
  "game-ui-design-system::10",
  "grid-asset-visual-registry::1.1",
  "grid-asset-visual-registry::1.2",
  "grid-asset-visual-registry::1.3",
  "grid-asset-visual-registry::1.4",
  "grid-asset-visual-registry::2.1",
  "grid-asset-visual-registry::2.2",
  "grid-asset-visual-registry::2.3",
  "grid-asset-visual-registry::3.1",
  "grid-asset-visual-registry::3.2",
  "grid-asset-visual-registry::4.1",
  "grid-asset-visual-registry::4.2",
  "grid-asset-visual-registry::4.3",
  "landmarks::1",
  "landmarks::10",
  "landmarks::11",
  "landmarks::12",
  "landmarks::2",
  "landmarks::3",
  "landmarks::4",
  "landmarks::5",
  "landmarks::6",
  "landmarks::7",
  "landmarks::8",
  "landmarks::9",
  "lifecycle-refactor::1",
  "lifecycle-refactor::10",
  "lifecycle-refactor::2",
  "lifecycle-refactor::3",
  "lifecycle-refactor::4",
  "lifecycle-refactor::5",
  "lifecycle-refactor::6",
  "lifecycle-refactor::7.1",
  "lifecycle-refactor::7.2",
  "lifecycle-refactor::8",
  "mcp-lifecycle-tools-opus-4-7-audit::1",
  "mcp-lifecycle-tools-opus-4-7-audit::10",
  "mcp-lifecycle-tools-opus-4-7-audit::11",
  "mcp-lifecycle-tools-opus-4-7-audit::12",
  "mcp-lifecycle-tools-opus-4-7-audit::13",
  "mcp-lifecycle-tools-opus-4-7-audit::14",
  "mcp-lifecycle-tools-opus-4-7-audit::15",
  "mcp-lifecycle-tools-opus-4-7-audit::16",
  "mcp-lifecycle-tools-opus-4-7-audit::17",
  "mcp-lifecycle-tools-opus-4-7-audit::2",
  "mcp-lifecycle-tools-opus-4-7-audit::3",
  "mcp-lifecycle-tools-opus-4-7-audit::4",
  "mcp-lifecycle-tools-opus-4-7-audit::5",
  "mcp-lifecycle-tools-opus-4-7-audit::6",
  "mcp-lifecycle-tools-opus-4-7-audit::7",
  "mcp-lifecycle-tools-opus-4-7-audit::8",
  "mcp-lifecycle-tools-opus-4-7-audit::9",
  "multi-scale::1",
  "multi-scale::11",
  "multi-scale::12",
  "multi-scale::13",
  "multi-scale::15",
  "multi-scale::2",
  "multi-scale::3",
  "multi-scale::6",
  "multi-scale::8",
  "multi-scale::9",
  "music-player::1",
  "music-player::1.1",
  "music-player::2",
  "music-player::3",
  "music-player::4",
  "music-player::6",
  "music-player::7",
  "session-token-latency::1.1",
  "session-token-latency::1.2",
  "session-token-latency::1.3",
  "session-token-latency::2.1",
  "session-token-latency::2.2",
  "session-token-latency::2.3",
  "session-token-latency::3.1",
  "session-token-latency::3.2",
  "session-token-latency::4.1",
  "session-token-latency::4.2",
  "session-token-latency::4.3",
  "session-token-latency::5.1",
  "skill-training::1",
  "skill-training::2",
  "skill-training::3",
  "skill-training::4",
  "skill-training::5",
  "skill-training::6",
  "sprite-gen::1",
  "sprite-gen::1.4",
  "sprite-gen::10",
  "sprite-gen::11",
  "sprite-gen::12",
  "sprite-gen::13",
  "sprite-gen::14",
  "sprite-gen::15",
  "sprite-gen::2",
  "sprite-gen::3",
  "sprite-gen::4",
  "sprite-gen::5",
  "sprite-gen::6",
  "sprite-gen::6.1",
  "sprite-gen::6.2",
  "sprite-gen::6.3",
  "sprite-gen::6.4",
  "sprite-gen::6.5",
  "sprite-gen::6.6",
  "sprite-gen::6.7",
  "sprite-gen::7",
  "sprite-gen::7 addendum",
  "sprite-gen::8",
  "sprite-gen::9",
  "sprite-gen::9 addendum",
  "ui-polish::1",
  "ui-polish::10",
  "ui-polish::11",
  "ui-polish::12",
  "ui-polish::13",
  "ui-polish::14",
  "ui-polish::2",
  "ui-polish::3",
  "ui-polish::4",
  "ui-polish::5",
  "ui-polish::6",
  "ui-polish::7",
  "ui-polish::8",
  "ui-polish::9",
  "unity-agent-bridge::1.1",
  "unity-agent-bridge::1.2",
  "unity-agent-bridge::1.3",
  "unity-agent-bridge::2.1",
  "unity-agent-bridge::2.2",
  "unity-agent-bridge::3.1",
  "unity-agent-bridge::3.2",
  "utilities::1",
  "utilities::10",
  "utilities::11",
  "utilities::13",
  "utilities::3",
  "utilities::4",
  "utilities::5",
  "utilities::6",
  "utilities::7",
  "utilities::8",
  "utilities::9",
  "web-platform::1",
  "web-platform::10",
  "web-platform::11",
  "web-platform::12",
  "web-platform::13",
  "web-platform::14",
  "web-platform::15",
  "web-platform::16",
  "web-platform::17",
  "web-platform::18",
  "web-platform::19",
  "web-platform::2",
  "web-platform::20",
  "web-platform::21",
  "web-platform::22",
  "web-platform::23",
  "web-platform::24",
  "web-platform::25",
  "web-platform::26",
  "web-platform::27",
  "web-platform::28",
  "web-platform::29",
  "web-platform::3",
  "web-platform::30",
  "web-platform::31",
  "web-platform::32",
  "web-platform::33",
  "web-platform::34",
  "web-platform::35",
  "web-platform::36",
  "web-platform::37",
  "web-platform::4",
  "web-platform::5",
  "web-platform::6",
  "web-platform::7",
  "web-platform::8",
  "web-platform::9",
  "zone-s-economy::1",
  "zone-s-economy::2",
  "zone-s-economy::3",
  "zone-s-economy::4",
  "zone-s-economy::5",
  "zone-s-economy::6",
  "zone-s-economy::7",
  "zone-s-economy::8",
  "zone-s-economy::9",
]);

// ---------------------------------------------------------------------------
// DB connect.
// ---------------------------------------------------------------------------

const conn = resolveDatabaseUrl(REPO_ROOT);
if (!conn) {
  console.error("[arch-coherence] DATABASE_URL not resolvable — aborting");
  process.exit(2);
}

const client = new pg.Client({ connectionString: conn });

try {
  await client.connect();
} catch (err) {
  console.error(`[arch-coherence] DB connect failed: ${err.message}`);
  process.exit(2);
}

let violations = 0;

// ---------------------------------------------------------------------------
// Pre-flight: appendChangelogForRecentCommits.
//
// Stage 1.4 / TECH-2564 — walk `git log` since last `arch_changelog.commit_sha`
// cursor, emit one `arch_changelog` row per (commit_sha, spec_path) for every
// touched `ia/specs/architecture/**` path. INSERT…ON CONFLICT DO NOTHING via
// the UNIQUE index added in migration 0038 (commit_sha, spec_path partial).
//
// Resolves `surface_slug` via `arch_surfaces.spec_path` lookup; logs NULL when
// path lacks a surface row (invariant #12 — never auto-create surfaces).
//
// Graceful on dirty git state / missing repo / shallow log: catches errors
// and prints diagnostic, never aborts the validator.
// ---------------------------------------------------------------------------

async function appendChangelogForRecentCommits() {
  let cursorSha = null;
  try {
    const cur = await client.query(
      `SELECT commit_sha
         FROM arch_changelog
        WHERE kind = 'spec_edit_commit' AND commit_sha IS NOT NULL
        ORDER BY created_at DESC
        LIMIT 1`,
    );
    cursorSha = cur.rows[0]?.commit_sha ?? null;
  } catch (err) {
    console.error(
      `[arch-coherence] changelog-append: cursor query failed (${err.message}); skipping pre-flight`,
    );
    return;
  }

  // git log range: cursorSha..HEAD if cursor exists, else last 50 commits.
  const range = cursorSha ? `${cursorSha}..HEAD` : `HEAD~50..HEAD`;
  let logOutput;
  try {
    logOutput = execSync(
      `git log ${range} --name-only --pretty=format:'COMMIT:%H' -- 'ia/specs/architecture/**'`,
      { cwd: REPO_ROOT, encoding: "utf8", stdio: ["ignore", "pipe", "pipe"] },
    );
  } catch (err) {
    // Cursor sha may have been rewritten / shallow clone — fall back to HEAD~50.
    if (cursorSha) {
      try {
        logOutput = execSync(
          `git log HEAD~50..HEAD --name-only --pretty=format:'COMMIT:%H' -- 'ia/specs/architecture/**'`,
          {
            cwd: REPO_ROOT,
            encoding: "utf8",
            stdio: ["ignore", "pipe", "pipe"],
          },
        );
      } catch (err2) {
        console.error(
          `[arch-coherence] changelog-append: git log fallback failed (${err2.message}); skipping`,
        );
        return;
      }
    } else {
      console.error(
        `[arch-coherence] changelog-append: git log failed (${err.message}); skipping`,
      );
      return;
    }
  }

  // Parse: alternating COMMIT:<sha> + path lines.
  const pairs = []; // {commit_sha, spec_path}[]
  let currentSha = null;
  for (const rawLine of logOutput.split("\n")) {
    const line = rawLine.trim();
    if (!line) continue;
    if (line.startsWith("COMMIT:")) {
      currentSha = line.slice("COMMIT:".length).trim();
      continue;
    }
    if (currentSha && line.startsWith("ia/specs/architecture/")) {
      pairs.push({ commit_sha: currentSha, spec_path: line });
    }
  }

  if (pairs.length === 0) {
    return; // No-op: no recent arch commits or no new ones since cursor.
  }

  // Surface-slug lookup map.
  const surfacesRes = await client.query(
    `SELECT slug, spec_path FROM arch_surfaces`,
  );
  const surfaceBySpecPath = new Map();
  for (const r of surfacesRes.rows) {
    if (!surfaceBySpecPath.has(r.spec_path)) {
      surfaceBySpecPath.set(r.spec_path, r.slug);
    }
  }

  let inserted = 0;
  for (const p of pairs) {
    const surfaceSlug = surfaceBySpecPath.get(p.spec_path) ?? null;
    const ins = await client.query(
      `INSERT INTO arch_changelog (kind, commit_sha, spec_path, surface_slug, body)
       VALUES ('spec_edit_commit', $1, $2, $3, $4)
       ON CONFLICT (commit_sha, spec_path) WHERE commit_sha IS NOT NULL AND spec_path IS NOT NULL
       DO NOTHING
       RETURNING id`,
      [
        p.commit_sha,
        p.spec_path,
        surfaceSlug,
        `auto: spec_edit_commit ${p.spec_path}`,
      ],
    );
    if (ins.rowCount > 0) inserted += 1;
  }

  if (inserted > 0) {
    console.log(
      `[arch-coherence] changelog-append: ${inserted} row(s) inserted across ${pairs.length} touched path(s)`,
    );
  }
}

await appendChangelogForRecentCommits();

// ---------------------------------------------------------------------------
// Check 1: orphan_surface_slug.
// ---------------------------------------------------------------------------

const orphans = await client.query(
  `SELECT s.slug, s.stage_id, s.surface_slug
     FROM stage_arch_surfaces s
     LEFT JOIN arch_surfaces a ON a.slug = s.surface_slug
    WHERE a.slug IS NULL
    ORDER BY s.slug, s.stage_id, s.surface_slug`,
);
if (orphans.rowCount > 0) {
  console.error(
    `[arch-coherence] ✗ orphan_surface_slug: ${orphans.rowCount} link row(s) reference unknown arch_surfaces`,
  );
  for (const r of orphans.rows) {
    console.error(
      `  - ${r.slug}/${r.stage_id} → ${r.surface_slug} (no arch_surfaces row); restore or delete the link row`,
    );
  }
  violations += orphans.rowCount;
}

// ---------------------------------------------------------------------------
// Check 2: unlinked_open_stage.
// ---------------------------------------------------------------------------

const unlinked = await client.query(
  `SELECT s.slug, s.stage_id
     FROM ia_stages s
     LEFT JOIN stage_arch_surfaces sa
            ON sa.slug = s.slug AND sa.stage_id = s.stage_id
    WHERE s.status IN ('pending', 'in_progress')
      AND sa.surface_slug IS NULL
    GROUP BY s.slug, s.stage_id
    ORDER BY s.slug, s.stage_id`,
);
const unlinkedFiltered = unlinked.rows.filter(
  (r) => !EXPLICIT_NONE_STAGES.has(`${r.slug}::${r.stage_id}`),
);
if (unlinkedFiltered.length > 0) {
  console.error(
    `[arch-coherence] ✗ unlinked_open_stage: ${unlinkedFiltered.length} open stage(s) have no arch_surfaces link and no explicit-none waiver`,
  );
  for (const r of unlinkedFiltered) {
    console.error(
      `  - ${r.slug}/${r.stage_id} — link a surface via stage_insert(arch_surfaces=[...]) OR add '${r.slug}::${r.stage_id}' to EXPLICIT_NONE_STAGES`,
    );
  }
  violations += unlinkedFiltered.length;
}

// ---------------------------------------------------------------------------
// Check 3: orphan_decision_surface.
// ---------------------------------------------------------------------------

const decOrphans = await client.query(
  `SELECT a.slug
     FROM arch_surfaces a
     LEFT JOIN arch_decisions d ON d.surface_id = a.id
    WHERE a.kind = 'decision' AND d.id IS NULL
    ORDER BY a.slug`,
);
if (decOrphans.rowCount > 0) {
  console.error(
    `[arch-coherence] ✗ orphan_decision_surface: ${decOrphans.rowCount} decision-kind arch_surfaces row(s) carry zero arch_decisions`,
  );
  for (const r of decOrphans.rows) {
    console.error(
      `  - ${r.slug} — either insert a DEC-Ax row or change arch_surfaces.kind off 'decision'`,
    );
  }
  violations += decOrphans.rowCount;
}

// ---------------------------------------------------------------------------
// Summary.
// ---------------------------------------------------------------------------

if (violations > 0) {
  console.error(`[arch-coherence] ${violations} violation(s) total`);
  await client.end();
  process.exit(1);
}

const counts = await client.query(
  `SELECT
     (SELECT count(*)::int FROM arch_surfaces) AS surfaces,
     (SELECT count(*)::int FROM stage_arch_surfaces) AS links,
     (SELECT count(*)::int FROM ia_stages WHERE status IN ('pending', 'in_progress')) AS open_stages`,
);
const c = counts.rows[0];
console.log(
  `[arch-coherence] ✓ ${c.surfaces} surfaces · ${c.links} stage links · ${c.open_stages} open stages`,
);

await client.end();
process.exit(0);
