#!/usr/bin/env -S npx tsx
/**
 * ia-db-import.ts — one-shot filesystem → DB import (Step 2).
 *
 * Reads:
 *   - ia/backlog/*.yaml  + ia/backlog-archive/*.yaml  → ia_tasks
 *   - ia/projects/*-master-plan.md                   → ia_master_plans + ia_stages
 *   - ia/projects/{ISSUE_ID}.md                      → ia_tasks.body (spec text)
 *
 * Writes into Postgres tables created in db/migrations/0015_ia_tasks_core.sql:
 *   ia_master_plans, ia_stages, ia_tasks, ia_task_deps.
 *
 * Idempotent (re-run replaces all rows under a single tx; filesystem untouched).
 *
 * Source of truth for decisions: docs/ia-dev-db-refactor-implementation.md §Step 2.
 */

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import pg from "pg";

import { loadAllYamlIssues } from "../src/parser/backlog-yaml-loader.js";
import type { ParsedBacklogIssue } from "../src/parser/backlog-parser.js";

// eslint-disable-next-line @typescript-eslint/no-require-imports
const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "../../..");

// ---------------------------------------------------------------------------
// DB URL resolution (mirrors tools/postgres-ia/resolve-database-url.mjs).
// ---------------------------------------------------------------------------

function resolveDatabaseUrl(): string {
  // Load root .env if present (dev path only — never in CI).
  const envFile = path.join(REPO_ROOT, ".env");
  if (fs.existsSync(envFile)) {
    const raw = fs.readFileSync(envFile, "utf8");
    for (const line of raw.split(/\r?\n/)) {
      const m = line.match(/^\s*([A-Z0-9_]+)\s*=\s*(.+?)\s*$/);
      if (m && !process.env[m[1]]) {
        process.env[m[1]] = m[2].replace(/^['"]|['"]$/g, "");
      }
    }
  }
  const env = process.env.DATABASE_URL?.trim();
  if (env) return env;

  const configPath = path.join(REPO_ROOT, "config/postgres-dev.json");
  if (fs.existsSync(configPath)) {
    const j = JSON.parse(fs.readFileSync(configPath, "utf8"));
    if (typeof j.database_url === "string" && j.database_url.trim()) {
      return j.database_url.trim();
    }
  }
  return "postgresql://postgres:postgres@localhost:5434/territory_ia_dev";
}

// ---------------------------------------------------------------------------
// Master-plan parser — extracts slug + title + stages from one md file.
// ---------------------------------------------------------------------------

interface ParsedMasterPlan {
  slug: string;
  title: string;
  sourcePath: string;
  stages: ParsedStage[];
}

interface ParsedStage {
  stage_id: string;
  title: string | null;
}

function parseMasterPlanFile(absPath: string): ParsedMasterPlan {
  const base = path.basename(absPath).replace(/-master-plan\.md$/, "");
  const relPath = path.relative(REPO_ROOT, absPath);

  const text = fs.readFileSync(absPath, "utf8");
  const lines = text.split(/\r?\n/);

  let title = base;
  for (const line of lines) {
    const m = line.match(/^#\s+(.+?)\s*$/);
    if (m) {
      title = m[1].trim();
      break;
    }
  }

  // Stage headers: `### Stage {id} — {title}` where {id} is free-form up to
  // the em-dash. Captures `1`, `1.1`, `6 addendum`, `9 addendum`, etc.
  const stages: ParsedStage[] = [];
  const seen = new Set<string>();
  const stageRx = /^###\s+Stage\s+(.+?)\s*(?:—|-)\s*(.+?)\s*$/;
  for (const line of lines) {
    const m = line.match(stageRx);
    if (!m) continue;
    const stage_id = m[1].trim();
    if (!stage_id || seen.has(stage_id)) continue;
    seen.add(stage_id);
    stages.push({ stage_id, title: m[2].trim() || null });
  }

  return { slug: base, title, sourcePath: relPath, stages };
}

function scanMasterPlans(): ParsedMasterPlan[] {
  const dir = path.join(REPO_ROOT, "ia/projects");
  const out: ParsedMasterPlan[] = [];
  for (const f of fs.readdirSync(dir)) {
    if (!f.endsWith("-master-plan.md")) continue;
    out.push(parseMasterPlanFile(path.join(dir, f)));
  }
  return out;
}

// ---------------------------------------------------------------------------
// Task body loader — reads ia/projects/{ISSUE_ID}.md when present.
// ---------------------------------------------------------------------------

function loadTaskBody(issueId: string): string {
  const p = path.join(REPO_ROOT, "ia/projects", `${issueId}.md`);
  if (!fs.existsSync(p)) return "";
  return fs.readFileSync(p, "utf8");
}

// ---------------------------------------------------------------------------
// Helpers — derive slug + stage from yaml record.
// ---------------------------------------------------------------------------

function deriveSlugFromParentPlan(
  parentPlan: string | null | undefined,
): string | null {
  if (!parentPlan) return null;
  const m = parentPlan.match(/ia\/projects\/(.+)-master-plan\.md/);
  return m ? m[1] : null;
}

/**
 * Parse spec frontmatter for `parent_plan:` when yaml record lacks it.
 * Returns the raw path string (unquoted) or null. Body has already been
 * loaded, so this is a cheap regex pass over the first ~500 chars.
 */
function deriveParentPlanFromBody(body: string): string | null {
  if (!body) return null;
  const head = body.slice(0, 2000);
  if (!head.startsWith("---")) return null;
  const fmEnd = head.indexOf("\n---", 4);
  if (fmEnd < 0) return null;
  const fm = head.slice(0, fmEnd);
  const m = fm.match(/^parent_plan:\s*["']?([^"'\n]+?)["']?\s*$/m);
  return m ? m[1]!.trim() : null;
}

/**
 * Derive a `stage_id` from a yaml `section:` string of the form
 * `"Stage 7 — ..."` or `"Stage 3.2 — ..."` or `"Stage 7 addendum — ..."`.
 * Returns the bare id (e.g. `"7"`, `"3.2"`, `"7 addendum"`) or null.
 */
function deriveStageFromSection(
  section: string | null | undefined,
): string | null {
  if (!section) return null;
  const m = section.match(/^Stage\s+([\w][\w.\s-]*?)\s+[—\-–]/);
  return m ? m[1]!.trim() : null;
}

function statusFromYaml(s: string): "pending" | "archived" {
  // Yaml `closed` → archived; everything else starts as pending.
  // Step 4 mutation tools manage the finer lifecycle states
  // (implemented / verified / done) going forward.
  return s === "closed" ? "archived" : "pending";
}

function prefixOfId(id: string): string {
  const m = id.match(/^([A-Z]+)-/);
  return m ? m[1] : "TECH";
}

function numericOfId(id: string): number {
  const m = id.match(/^[A-Z]+-(\d+)/);
  return m ? parseInt(m[1], 10) : 0;
}

// Parse yaml `depends_on_raw` + `related` into normalized id lists.
function parseIdList(raw: unknown): string[] {
  if (!raw) return [];
  if (Array.isArray(raw)) {
    return raw
      .map((x) => String(x).trim())
      .filter((x) => /^[A-Z]+-[0-9]+[a-z]?$/i.test(x));
  }
  if (typeof raw === "string") {
    return raw
      .split(/[,\s]+/)
      .map((x) => x.trim())
      .filter((x) => /^[A-Z]+-[0-9]+[a-z]?$/i.test(x));
  }
  return [];
}

// ---------------------------------------------------------------------------
// Main.
// ---------------------------------------------------------------------------

async function main() {
  const databaseUrl = resolveDatabaseUrl();
  const client = new pg.Client({ connectionString: databaseUrl });
  await client.connect();

  console.log(
    `ia-db-import: connecting ${databaseUrl.replace(/:[^:@]+@/, ":***@")}`,
  );

  const startedAt = Date.now();

  // --- load filesystem state ---------------------------------------------

  const { records: allIssues, parseErrorCount } = loadAllYamlIssues(
    REPO_ROOT,
    "all",
  );
  console.log(
    `ia-db-import: loaded ${allIssues.length} yaml issues (parse errors: ${parseErrorCount})`,
  );

  const plans = scanMasterPlans();
  console.log(`ia-db-import: scanned ${plans.length} master plans`);

  // --- derive stage set (union of master-plan headers + yaml references) -

  type StageKey = string; // `${slug}::${stage_id}`
  const stageMap = new Map<
    StageKey,
    {
      slug: string;
      stage_id: string;
      title: string | null;
      source_file_path: string | null;
    }
  >();

  for (const p of plans) {
    for (const s of p.stages) {
      const key = `${p.slug}::${s.stage_id}`;
      stageMap.set(key, {
        slug: p.slug,
        stage_id: s.stage_id,
        title: s.title,
        source_file_path: p.sourcePath,
      });
    }
  }

  for (const iss of allIssues) {
    const body = loadTaskBody(iss.issue_id);
    const slug =
      deriveSlugFromParentPlan(iss.parent_plan) ??
      deriveSlugFromParentPlan(deriveParentPlanFromBody(body));
    const stage_id = iss.stage ?? deriveStageFromSection(iss.backlog_section);
    if (!slug || !stage_id) continue;
    const key = `${slug}::${stage_id}`;
    if (!stageMap.has(key)) {
      // Yaml references a stage we didn't find in any master plan header.
      // Create a placeholder row so the FK can hold.
      const owningPlan = plans.find((p) => p.slug === slug);
      stageMap.set(key, {
        slug,
        stage_id,
        title: null,
        source_file_path: owningPlan?.sourcePath ?? null,
      });
    }
  }

  // Plans referenced only by yaml (slug with no -master-plan.md).
  const planSlugs = new Set(plans.map((p) => p.slug));
  const orphanSlugs = new Set<string>();
  for (const iss of allIssues) {
    const body = loadTaskBody(iss.issue_id);
    const slug =
      deriveSlugFromParentPlan(iss.parent_plan) ??
      deriveSlugFromParentPlan(deriveParentPlanFromBody(body));
    if (slug && !planSlugs.has(slug)) orphanSlugs.add(slug);
  }

  // --- build row lists ---------------------------------------------------

  const planRows = plans.map((p) => ({
    slug: p.slug,
    title: p.title,
    source_spec_path: p.sourcePath,
  }));
  for (const orphanSlug of orphanSlugs) {
    planRows.push({
      slug: orphanSlug,
      title: orphanSlug,
      source_spec_path: null as unknown as string, // allowed: column is nullable
    });
  }

  const stageRows = Array.from(stageMap.values());

  // Track max numeric id per prefix for sequence reconciliation.
  const maxIdByPrefix: Record<string, number> = {
    TECH: 0,
    FEAT: 0,
    BUG: 0,
    ART: 0,
    AUDIO: 0,
  };

  interface TaskRow {
    task_id: string;
    prefix: string;
    slug: string | null;
    stage_id: string | null;
    title: string;
    status: "pending" | "archived";
    priority: string | null;
    type: string | null;
    notes: string | null;
    body: string;
    completed_at: string | null;
    archived_at: string | null;
  }

  const taskRows: TaskRow[] = [];
  const depRows: Array<{
    task_id: string;
    depends_on_id: string;
    kind: "depends_on" | "related";
  }> = [];
  const depDropped: Array<{
    task_id: string;
    target_id: string;
    kind: string;
    reason: string;
  }> = [];
  const knownIds = new Set(allIssues.map((i) => i.issue_id));

  for (const iss of allIssues) {
    const prefix = prefixOfId(iss.issue_id);
    if (maxIdByPrefix[prefix] !== undefined) {
      maxIdByPrefix[prefix] = Math.max(
        maxIdByPrefix[prefix],
        numericOfId(iss.issue_id),
      );
    }

    const status = statusFromYaml(iss.status);
    const body = loadTaskBody(iss.issue_id);
    const slug =
      deriveSlugFromParentPlan(iss.parent_plan) ??
      deriveSlugFromParentPlan(deriveParentPlanFromBody(body));
    const stage_id = iss.stage ?? deriveStageFromSection(iss.backlog_section);
    // Only populate FK cols if the stage exists.
    const stageKey = slug && stage_id ? `${slug}::${stage_id}` : null;
    const hasStage = stageKey !== null && stageMap.has(stageKey);

    taskRows.push({
      task_id: iss.issue_id,
      prefix,
      slug: hasStage ? slug : null,
      stage_id: hasStage ? stage_id : null,
      title: iss.title,
      status,
      priority: iss.priority ?? null,
      type: iss.type ?? null,
      notes: iss.notes ?? null,
      body,
      completed_at: status === "archived" ? new Date().toISOString() : null,
      archived_at: status === "archived" ? new Date().toISOString() : null,
    });

    const dependsOn = parseIdList(iss.depends_on);
    const related = parseIdList(iss.related);
    for (const d of dependsOn) {
      if (d === iss.issue_id) continue;
      if (!knownIds.has(d)) {
        depDropped.push({
          task_id: iss.issue_id,
          target_id: d,
          kind: "depends_on",
          reason: "target id not present in open + archive yaml set",
        });
        continue;
      }
      depRows.push({
        task_id: iss.issue_id,
        depends_on_id: d,
        kind: "depends_on",
      });
    }
    for (const r of related) {
      if (r === iss.issue_id) continue;
      if (!knownIds.has(r)) {
        depDropped.push({
          task_id: iss.issue_id,
          target_id: r,
          kind: "related",
          reason: "target id not present in open + archive yaml set",
        });
        continue;
      }
      depRows.push({
        task_id: iss.issue_id,
        depends_on_id: r,
        kind: "related",
      });
    }
  }

  // Dedupe dep rows on (task_id, depends_on_id, kind).
  const dedupedDeps = Array.from(
    new Map(
      depRows.map((d) => [
        `${d.task_id}::${d.depends_on_id}::${d.kind}`,
        d,
      ]),
    ).values(),
  );

  // --- one transaction ---------------------------------------------------

  try {
    await client.query("BEGIN");

    // Wipe existing data in dependency-safe order.
    await client.query("SET CONSTRAINTS ALL DEFERRED");
    await client.query("DELETE FROM ia_task_deps");
    await client.query("DELETE FROM ia_task_spec_history");
    await client.query("DELETE FROM ia_task_commits");
    await client.query("DELETE FROM ia_fix_plan_tuples");
    await client.query("DELETE FROM ia_stage_verifications");
    await client.query("DELETE FROM ia_ship_stage_journal");
    await client.query("DELETE FROM ia_tasks");
    await client.query("DELETE FROM ia_stages");
    await client.query("DELETE FROM ia_master_plans");

    // Insert master plans.
    for (const p of planRows) {
      await client.query(
        `INSERT INTO ia_master_plans (slug, title, source_spec_path)
         VALUES ($1, $2, $3)`,
        [p.slug, p.title, p.source_spec_path],
      );
    }

    // Insert stages.
    for (const s of stageRows) {
      await client.query(
        `INSERT INTO ia_stages (slug, stage_id, title, source_file_path)
         VALUES ($1, $2, $3, $4)`,
        [s.slug, s.stage_id, s.title, s.source_file_path],
      );
    }

    // Insert tasks.
    for (const t of taskRows) {
      await client.query(
        `INSERT INTO ia_tasks
           (task_id, prefix, slug, stage_id, title, status, priority, type,
            notes, body, completed_at, archived_at)
         VALUES ($1, $2, $3, $4, $5, $6::task_status, $7, $8, $9, $10, $11, $12)`,
        [
          t.task_id,
          t.prefix,
          t.slug,
          t.stage_id,
          t.title,
          t.status,
          t.priority,
          t.type,
          t.notes,
          t.body,
          t.completed_at,
          t.archived_at,
        ],
      );
    }

    // Insert deps (post-tasks so FK holds).
    for (const d of dedupedDeps) {
      await client.query(
        `INSERT INTO ia_task_deps (task_id, depends_on_id, kind)
         VALUES ($1, $2, $3::ia_task_dep_kind)
         ON CONFLICT (task_id, depends_on_id, kind) DO NOTHING`,
        [d.task_id, d.depends_on_id, d.kind],
      );
    }

    // Reconcile sequences — advance to max(current, observed+1).
    for (const prefix of ["TECH", "FEAT", "BUG", "ART", "AUDIO"]) {
      const seq = `${prefix.toLowerCase()}_id_seq`;
      const observed = maxIdByPrefix[prefix];
      // setval('seq', max(current_last_value, observed), true) means
      // next nextval returns observed+1 (matches reserve-id.sh semantics).
      await client.query(
        `SELECT setval($1, GREATEST(
                            (SELECT last_value FROM ${seq}),
                            $2::bigint
                          ), true)`,
        [seq, Math.max(1, observed)],
      );
    }

    await client.query("COMMIT");
  } catch (e) {
    await client.query("ROLLBACK");
    throw e;
  }

  const elapsedMs = Date.now() - startedAt;

  // --- report ------------------------------------------------------------

  console.log(`ia-db-import: wrote ${planRows.length} master plans`);
  console.log(`ia-db-import: wrote ${stageRows.length} stages`);
  console.log(`ia-db-import: wrote ${taskRows.length} tasks`);
  console.log(`ia-db-import: wrote ${dedupedDeps.length} dep edges`);
  console.log(`ia-db-import: dropped ${depDropped.length} unresolved dep targets`);
  if (depDropped.length > 0 && process.env.IA_DB_IMPORT_VERBOSE === "1") {
    for (const d of depDropped) {
      console.log(
        `  drop ${d.task_id} ${d.kind} -> ${d.target_id} (${d.reason})`,
      );
    }
  }
  console.log(`ia-db-import: max ids observed  ${JSON.stringify(maxIdByPrefix)}`);
  console.log(`ia-db-import: elapsed ${(elapsedMs / 1000).toFixed(2)}s`);

  await client.end();
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
