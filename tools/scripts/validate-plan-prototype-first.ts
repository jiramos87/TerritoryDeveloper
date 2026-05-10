#!/usr/bin/env npx tsx
/**
 * validate-plan-prototype-first.ts
 *
 * Stage 1.3 / TECH-10305 — read-only invariant gate enforcing the
 * prototype-first methodology on master plans created on or after the cutover
 * date (2026-05-03 per D3). Plans created before the cutover are
 * "grandfathered" and skipped (warn-only).
 *
 * Three checks against `ia_stages` rows of non-grandfathered plans:
 *
 *   1. tracer_slice_required — Stage 1.0 and Stage 1.1 MUST carry a
 *      `tracer_slice_block` jsonb with 5 non-empty string fields:
 *      name, verb, surface, evidence, gate (per D5).
 *      Missing block OR missing/empty field → hard violation.
 *
 *   2. visibility_delta_required — every Stage with `stage_id` NOT matching
 *      `1.x` MUST carry a non-empty `visibility_delta` text (per D6).
 *      Stages 1.x are exempt (Stage 1 = tracer slice; visibility delta starts
 *      at Stage 2).
 *
 *   3. visibility_delta_unique — per plan, the set of `visibility_delta`
 *      strings (across non-1.x stages) MUST be unique. Duplicates indicate
 *      two stages claiming the same player-visible surface, which violates
 *      the methodology's "every stage adds new visible surface" axiom.
 *
 * Stub-blocker scan extension (TECH-10306) lives in `scanStubBlockers` and
 * is invoked per non-grandfathered plan as a soft warn (printed to stdout;
 * does NOT contribute to exit code).
 *
 * Exit codes:
 *   0  All non-grandfathered plans green; prints summary footer + warn count.
 *   1  ≥1 hard violation; prints actionable per-violation lines.
 *   2  DB connection / query error.
 *
 * Wired into `npm run validate:all` after `validate:master-plan-status` and
 * before `validate:arch-coherence`.
 */

import path from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";
import * as fs from "node:fs";
import { resolveDatabaseUrl } from "../postgres-ia/resolve-database-url.mjs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = process.env.IA_REPO_ROOT ?? path.resolve(__dirname, "../..");

const pgRequire = createRequire(
  path.join(REPO_ROOT, "tools/postgres-ia/package.json"),
);
const pg = pgRequire("pg");

// ---------------------------------------------------------------------------
// Cutover date (D3): plans created before this date are grandfathered.
// ---------------------------------------------------------------------------

// Plan-scope opt-in — when set (ship-final cumulative-validate), restrict to
// stages owned by that slug. Cross-plan drift gated by validate:all in CI.
const SCOPE_SLUG = (process.env.VALIDATE_SCOPE_SLUG ?? "").trim();

const CUTOVER_ISO = "2026-05-03";

// ---------------------------------------------------------------------------
// Required tracer-slice fields (D5).
// ---------------------------------------------------------------------------

const TRACER_FIELDS = ["name", "verb", "surface", "evidence", "gate"] as const;

// ---------------------------------------------------------------------------
// Stub-blocker scan (TECH-10306): per-Stage verb-driven scan.
//
// For each Stage 1.0 / 1.1 with non-null `tracer_slice_block.verb`:
//   1. Extract PascalCase tokens from `verb` via TOKEN_REGEX (D4 lock).
//   2. For each token, walk SCOPE dirs; collect files whose contents reference
//      the token (substring match on identifier).
//   3. For each matched file, scan for stub markers. Emit per-violation hit
//      with file + line + marker text.
//
// Token resolves to zero files → silent skip with stdout warning (verb may
// reference yet-unwritten code per D6).
//
// Markers (D4 — substring match, case-sensitive):
//   - `throw new NotImplementedException`  (C# stub)
//   - `// TODO blocker`                    (universal blocker tag)
//
// Scope: Assets/**/*.cs + tools/**/*.{ts,mjs} + web/**/*.{ts,tsx}
// ---------------------------------------------------------------------------

const STUB_MARKERS = [
  "throw new NotImplementedException",
  "// TODO blocker",
];

const TOKEN_REGEX = /\b[A-Z][a-zA-Z0-9]+(?:\.[a-zA-Z][a-zA-Z0-9]+)*\b/g;

const SCOPE_GLOBS: { dir: string; ext: RegExp }[] = [
  { dir: "Assets", ext: /\.cs$/ },
  { dir: "tools", ext: /\.(ts|mjs)$/ },
  { dir: "web", ext: /\.(ts|tsx)$/ },
];

function* walk(dir: string): Generator<string> {
  if (!fs.existsSync(dir)) return;
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  for (const e of entries) {
    if (e.name === "node_modules" || e.name.startsWith(".")) continue;
    const full = path.join(dir, e.name);
    if (e.isDirectory()) yield* walk(full);
    else if (e.isFile()) yield full;
  }
}

// Self-exclusion: validator's own source contains the marker strings verbatim.
const SELF_FILE = path.relative(REPO_ROOT, fileURLToPath(import.meta.url));

interface StubHit {
  file: string;
  line: number;
  marker: string;
  token: string;
  plan: string;
  stage: string;
}

// Cache scoped files + their bodies once per validator run.
let _scopedFilesCache: { rel: string; body: string }[] | null = null;
function loadScopedFiles(): { rel: string; body: string }[] {
  if (_scopedFilesCache) return _scopedFilesCache;
  const out: { rel: string; body: string }[] = [];
  for (const scope of SCOPE_GLOBS) {
    const root = path.join(REPO_ROOT, scope.dir);
    for (const file of walk(root)) {
      if (!scope.ext.test(file)) continue;
      const rel = path.relative(REPO_ROOT, file);
      if (rel === SELF_FILE) continue;
      try {
        out.push({ rel, body: fs.readFileSync(file, "utf8") });
      } catch {
        /* unreadable file — skip */
      }
    }
  }
  _scopedFilesCache = out;
  return out;
}

function extractTokens(verb: string): string[] {
  const matches = verb.match(TOKEN_REGEX) ?? [];
  return Array.from(new Set(matches));
}

function scanStubBlockers(
  verb: string,
  plan: string,
  stage: string,
): { hits: StubHit[]; orphanTokens: string[] } {
  const tokens = extractTokens(verb);
  const files = loadScopedFiles();
  const hits: StubHit[] = [];
  const orphanTokens: string[] = [];

  for (const token of tokens) {
    const matchedFiles = files.filter((f) => f.body.includes(token));
    if (matchedFiles.length === 0) {
      orphanTokens.push(token);
      continue;
    }
    for (const f of matchedFiles) {
      const lines = f.body.split("\n");
      for (let i = 0; i < lines.length; i += 1) {
        for (const marker of STUB_MARKERS) {
          if (lines[i].includes(marker)) {
            hits.push({
              file: f.rel,
              line: i + 1,
              marker,
              token,
              plan,
              stage,
            });
          }
        }
      }
    }
  }
  return { hits, orphanTokens };
}

// ---------------------------------------------------------------------------
// Types.
// ---------------------------------------------------------------------------

interface PlanRow {
  slug: string;
  created_at: Date;
}

interface StageRow {
  slug: string;
  stage_id: string;
  status: string;
  body: string | null;
  tracer_slice_block: Record<string, unknown> | null;
  visibility_delta: string | null;
}

function isOneXStage(stage_id: string): boolean {
  return /^1(\.|$)/.test(stage_id);
}

function validateTracerSliceBlock(
  block: Record<string, unknown> | null,
): string | null {
  if (block === null || typeof block !== "object") {
    return "tracer_slice_block missing or not an object";
  }
  for (const field of TRACER_FIELDS) {
    const v = (block as Record<string, unknown>)[field];
    if (typeof v !== "string" || v.trim() === "") {
      return `tracer_slice_block.${field} missing or empty`;
    }
  }
  return null;
}

// ---------------------------------------------------------------------------
// Main.
// ---------------------------------------------------------------------------

// ---------------------------------------------------------------------------
// Test-injection shim (TECH-10308): bypass DB when env carries
// `PLAN_PROTOTYPE_FIRST_FAKE_ROWS` — JSON `{plans: PlanRow[], stages: StageRow[]}`.
// ---------------------------------------------------------------------------

interface FakeBundle {
  plans: PlanRow[];
  stages: StageRow[];
}

function readFakeBundle(): FakeBundle | null {
  const raw = process.env.PLAN_PROTOTYPE_FIRST_FAKE_ROWS;
  if (!raw) return null;
  try {
    return JSON.parse(raw) as FakeBundle;
  } catch (e) {
    console.error(
      `[plan-prototype-first] PLAN_PROTOTYPE_FIRST_FAKE_ROWS parse failed: ${
        e instanceof Error ? e.message : String(e)
      }`,
    );
    process.exit(2);
  }
}

async function main(): Promise<number> {
  const fake = readFakeBundle();

  let plansRows: PlanRow[];
  let stagesRows: StageRow[];
  let client: { end: () => Promise<unknown> } | null = null;

  if (fake) {
    plansRows = fake.plans;
    stagesRows = fake.stages;
  } else {
    const conn = resolveDatabaseUrl(REPO_ROOT);
    if (!conn) {
      console.error(
        "[plan-prototype-first] DATABASE_URL not resolvable — aborting",
      );
      return 2;
    }

    const pgClient = new pg.Client({ connectionString: conn });
    client = pgClient;

    try {
      await pgClient.connect();
    } catch (err) {
      const msg = err instanceof Error ? err.message : String(err);
      console.error(`[plan-prototype-first] DB connect failed: ${msg}`);
      return 2;
    }
  }

  let violations = 0;
  let warnCount = 0;

  try {
    if (!fake) {
      const pgClient = client as unknown as InstanceType<typeof pg.Client>;
      const plansRes = await pgClient.query<PlanRow>(
        `SELECT slug, created_at
           FROM ia_master_plans
          ORDER BY slug`,
      );
      plansRows = plansRes.rows;
    }

    const grandfathered: string[] = [];
    const activePlans: PlanRow[] = [];
    const cutover = new Date(CUTOVER_ISO);
    for (const row of plansRows!) {
      const created = new Date(row.created_at);
      if (created < cutover) {
        grandfathered.push(row.slug);
      } else {
        activePlans.push(row);
      }
    }

    if (activePlans.length === 0) {
      console.log(
        `[plan-prototype-first] ✓ no plans on/after cutover ${CUTOVER_ISO}; ${grandfathered.length} grandfathered (skipped)`,
      );
      return 0;
    }

    // Restrict to scoped slug when ship-final cumulative-validate sets VALIDATE_SCOPE_SLUG.
    const filteredPlans = SCOPE_SLUG ? activePlans.filter((p) => p.slug === SCOPE_SLUG) : activePlans;
    const activeSlugs = new Set(filteredPlans.map((p) => p.slug));
    if (!fake) {
      const pgClient = client as unknown as InstanceType<typeof pg.Client>;
      const stagesRes = await pgClient.query<StageRow>(
        `SELECT slug, stage_id, status, body, tracer_slice_block, visibility_delta
           FROM ia_stages
          WHERE slug = ANY($1::text[])
          ORDER BY slug, stage_id`,
        [Array.from(activeSlugs)],
      );
      stagesRows = stagesRes.rows;
    }

    const stagesByPlan = new Map<string, StageRow[]>();
    for (const s of stagesRows!) {
      if (!activeSlugs.has(s.slug)) continue;
      const arr = stagesByPlan.get(s.slug) ?? [];
      arr.push(s);
      stagesByPlan.set(s.slug, arr);
    }

    for (const plan of filteredPlans) {
      const stages = stagesByPlan.get(plan.slug) ?? [];
      if (stages.length === 0) continue;

      // Check 1: Stage 1.0 / 1.1 MUST carry tracer_slice_block.
      for (const s of stages) {
        if (s.stage_id === "1.0" || s.stage_id === "1.1") {
          if (s.body?.includes("target_kind: design_only")) continue;
          const err = validateTracerSliceBlock(s.tracer_slice_block);
          if (err) {
            console.error(
              `[plan-prototype-first] ✗ tracer_slice_required: ${plan.slug}/${s.stage_id} — ${err}`,
            );
            violations += 1;
          }
        }
      }

      // Check 2: every non-1.x Stage MUST carry visibility_delta.
      // Check 3: visibility_delta unique within plan.
      const deltaCounts = new Map<string, string[]>();
      for (const s of stages) {
        if (isOneXStage(s.stage_id)) continue;
        if (s.body?.includes("target_kind: design_only")) continue;
        const d = (s.visibility_delta ?? "").trim();
        if (d === "") {
          console.error(
            `[plan-prototype-first] ✗ visibility_delta_required: ${plan.slug}/${s.stage_id} — visibility_delta missing or empty (Stages 2+)`,
          );
          violations += 1;
          continue;
        }
        const arr = deltaCounts.get(d) ?? [];
        arr.push(s.stage_id);
        deltaCounts.set(d, arr);
      }
      for (const [delta, ids] of deltaCounts.entries()) {
        if (ids.length > 1) {
          console.error(
            `[plan-prototype-first] ✗ visibility_delta_unique: ${plan.slug} — ${ids.length} stages share visibility_delta="${delta}" (${ids.join(", ")})`,
          );
          violations += ids.length - 1;
        }
      }
    }

    // Stub-blocker scan (TECH-10306) — per Stage 1.0/1.1 with non-null verb.
    // Violation → hard error (exit nonzero). Orphan token → warn (exit 0).
    for (const plan of filteredPlans) {
      const stages = stagesByPlan.get(plan.slug) ?? [];
      for (const s of stages) {
        if (s.stage_id !== "1.0" && s.stage_id !== "1.1") continue;
        const block = s.tracer_slice_block;
        if (!block || typeof block !== "object") continue;
        const verb = (block as Record<string, unknown>).verb;
        if (typeof verb !== "string" || verb.trim() === "") continue;

        const { hits, orphanTokens } = scanStubBlockers(
          verb,
          plan.slug,
          s.stage_id,
        );

        for (const token of orphanTokens) {
          console.log(
            `[plan-prototype-first] ⚠ stub_blocker_scan: ${plan.slug}/${s.stage_id} — token "${token}" resolves to zero files (verb may reference future code)`,
          );
          warnCount += 1;
        }

        for (const h of hits) {
          console.error(
            `[plan-prototype-first] ✗ stub_blocker_scan: ${h.file}:${h.line} — "${h.marker}" (token=${h.token}, plan=${h.plan}/${h.stage})`,
          );
          violations += 1;
        }
      }
    }

    if (violations > 0) {
      console.error(
        `[plan-prototype-first] ${violations} violation(s) total across ${filteredPlans.length} active plan(s); ${grandfathered.length} grandfathered (skipped)`,
      );
      return 1;
    }

    console.log(
      `[plan-prototype-first] ✓ ${filteredPlans.length} active plan(s) checked · ${grandfathered.length} grandfathered (skipped) · ${warnCount} warn(s)`,
    );
    return 0;
  } finally {
    if (client) await client.end().catch(() => {});
  }
}

main().then(
  (code) => process.exit(code),
  (err) => {
    console.error(
      `[plan-prototype-first] uncaught: ${err instanceof Error ? err.stack ?? err.message : String(err)}`,
    );
    process.exit(2);
  },
);
