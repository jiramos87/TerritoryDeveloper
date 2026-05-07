#!/usr/bin/env node
/**
 * validate-red-stage-proof-anchor.mjs — §Red-Stage Proof anchor drift gate.
 *
 * TECH-22668 / game-ui-catalog-bake Stage 9.14
 *
 * Rule: For each active task spec body containing §Red-Stage Proof with an anchor
 * of the form `{file_path}::{method_name}`, parse the referenced test file, find the
 * [Test]-attributed method body, and assert that every surface keyword extracted from
 * the anchor prose appears in the method body (or a helper method called at depth 1).
 *
 * Surface keywords = PascalCase identifiers (≥5 chars) AND quoted strings in the
 * anchor section prose.
 *
 * Drift → exit 1 with structured stderr lines:
 *   [anchor-drift] task={id} anchor={file}::{method} missing_keywords=[X, Y]
 * Anchor file missing → exit 1:
 *   [anchor-missing] task={id} anchor={file}::{method} reason=file_not_found
 * Spec without §Red-Stage Proof → skip silently.
 *
 * Exit codes:
 *   0 = all anchors pass (or no anchors found)
 *   1 = ≥1 anchor failed
 *   2 = config / DB error
 *
 * Wired into validate:all:readonly after validate:catalog-panel-coverage.
 */

import { readFileSync, existsSync } from "node:fs";
import { resolve, dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
export const REPO_ROOT = resolve(__dirname, "../..");

// ── Anchor extraction ─────────────────────────────────────────────────────────

/**
 * Extract §Red-Stage Proof section from a task body string.
 * Returns the prose between the §Red-Stage Proof heading and the next ##/### heading,
 * or null if section not found.
 * @param {string} body
 * @returns {string | null}
 */
export function extractRedStageProofSection(body) {
  if (!body) return null;
  // Match `### §Red-Stage Proof` heading (any prefix level ##/###).
  const sectionRx = /^#{2,4}\s+§Red-Stage Proof\s*$/m;
  const m = sectionRx.exec(body);
  if (!m) return null;
  const start = m.index + m[0].length;
  // Find next heading of same or higher level.
  const nextHeading = /^#{2,4}\s+/m;
  const rest = body.slice(start);
  const next = nextHeading.exec(rest);
  const section = next ? rest.slice(0, next.index) : rest;
  return section.trim();
}

/**
 * Parse anchors from §Red-Stage Proof section prose.
 * Anchor pattern: `\`?{file_path}::{method_name}\`?` on an Anchor line.
 * Returns array of { filePath, methodName, prose } objects.
 * @param {string} section
 * @returns {Array<{filePath: string, methodName: string, prose: string}>}
 */
export function parseAnchors(section) {
  if (!section) return [];
  // Split into per-anchor blocks by looking for "Anchor" labels.
  const anchorHeaderRx = /\*\*Anchor[^:]*:\*\*[^\n]*`([^`]+)::([A-Za-z_][A-Za-z0-9_]*)`/g;
  const results = [];
  let match;
  const positions = [];
  while ((match = anchorHeaderRx.exec(section)) !== null) {
    positions.push({
      index: match.index,
      headerEnd: match.index + match[0].length,
      filePath: match[1],
      methodName: match[2],
    });
  }
  for (let i = 0; i < positions.length; i++) {
    const end = i + 1 < positions.length ? positions[i + 1].index : section.length;
    // Prose = everything AFTER the anchor header line (skip anchor declaration itself).
    // This prevents keywords being extracted from the file/method name in the anchor line.
    const prose = section.slice(positions[i].headerEnd, end).trim();
    results.push({ filePath: positions[i].filePath, methodName: positions[i].methodName, prose });
  }
  return results;
}

/**
 * Extract surface keywords from anchor prose.
 * Keywords = PascalCase identifiers (≥5 chars) AND quoted strings.
 * @param {string} prose
 * @returns {string[]}
 */
export function extractSurfaceKeywords(prose) {
  const keywords = new Set();
  // PascalCase identifiers ≥5 chars (e.g. CatalogPrefabRef, AutoZoningManager).
  const pascalRx = /\b([A-Z][a-zA-Z0-9_]{4,})\b/g;
  let m;
  while ((m = pascalRx.exec(prose)) !== null) {
    keywords.add(m[1]);
  }
  // Quoted strings (single or double).
  const quotedRx = /(?:'([^']+)'|"([^"]+)")/g;
  while ((m = quotedRx.exec(prose)) !== null) {
    const token = m[1] || m[2];
    if (token && token.length >= 2) keywords.add(token);
  }
  return [...keywords];
}

/**
 * Extract [Test]-attributed method body from C# source.
 * Performs balanced-brace scan starting at the method declaration after [Test].
 * Includes one-level helper calls (names matching /\bMethodName\b/ called within).
 * @param {string} source - Full file source.
 * @param {string} methodName
 * @returns {string | null} - Method body text (including calls), or null if not found.
 */
export function extractTestMethodBody(source, methodName) {
  // Find [Test] or [UnityTest] attribute preceding the method.
  // Scan for `[Test]` or `[UnityTest]` followed by method declaration.
  const testAttrRx = /\[(Unity)?Test\]/g;
  let attrMatch;
  while ((attrMatch = testAttrRx.exec(source)) !== null) {
    const afterAttr = source.slice(attrMatch.index);
    // Find method declaration: accessibility + return type + methodName + `(`
    const methodDeclRx = new RegExp(
      `(?:public|private|protected|internal|static|async|override|virtual|sealed|\\s)+[A-Za-z<>\\[\\]]+\\s+${escapeRegex(methodName)}\\s*\\(`,
    );
    const declMatch = methodDeclRx.exec(afterAttr);
    if (!declMatch) continue;
    // Find opening brace of method body.
    const afterDecl = afterAttr.slice(declMatch.index + declMatch[0].length);
    const braceStart = afterDecl.indexOf("{");
    if (braceStart === -1) continue;
    // Balanced-brace scan.
    let depth = 1;
    let i = braceStart + 1;
    const bodyText = afterDecl.slice(braceStart);
    while (i < bodyText.length && depth > 0) {
      if (bodyText[i] === "{") depth++;
      else if (bodyText[i] === "}") depth--;
      i++;
    }
    return bodyText.slice(0, i);
  }
  return null;
}

/**
 * Collect one-level helper method bodies called from primaryBody.
 * Matches identifiers that look like method calls: `Name(`
 * Finds their bodies in source.
 * @param {string} primaryBody
 * @param {string} source
 * @returns {string}
 */
export function collectHelperBodies(primaryBody, source) {
  const callRx = /\b([A-Z][A-Za-z0-9_]+)\s*\(/g;
  const helperNames = new Set();
  let m;
  while ((m = callRx.exec(primaryBody)) !== null) {
    helperNames.add(m[1]);
  }
  let combined = primaryBody;
  for (const name of helperNames) {
    // Only collect if there's a [Test] or non-test method with this name.
    const helperBody = extractNonTestMethodBody(source, name);
    if (helperBody) combined += "\n" + helperBody;
  }
  return combined;
}

/**
 * Extract any (non-[Test]) method body by name.
 * @param {string} source
 * @param {string} methodName
 * @returns {string | null}
 */
export function extractNonTestMethodBody(source, methodName) {
  const methodDeclRx = new RegExp(
    `(?:public|private|protected|internal|static|async|override|virtual|sealed|\\s)+[A-Za-z<>\\[\\],\\s]+\\s+${escapeRegex(methodName)}\\s*\\(`,
    "g",
  );
  let m;
  while ((m = methodDeclRx.exec(source)) !== null) {
    const afterDecl = source.slice(m.index + m[0].length);
    const braceStart = afterDecl.indexOf("{");
    if (braceStart === -1) continue;
    let depth = 1;
    let i = braceStart + 1;
    const bodyText = afterDecl.slice(braceStart);
    while (i < bodyText.length && depth > 0) {
      if (bodyText[i] === "{") depth++;
      else if (bodyText[i] === "}") depth--;
      i++;
    }
    return bodyText.slice(0, i);
  }
  return null;
}

function escapeRegex(str) {
  return str.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

// ── Validation logic (pure, injectable) ──────────────────────────────────────

/**
 * Validate a single anchor against the filesystem.
 * @param {{taskId: string, filePath: string, methodName: string, prose: string}} anchor
 * @param {string} repoRoot
 * @returns {{ok: boolean, errorKind?: string, missingKeywords?: string[]}}
 */
export function validateAnchor(anchor, repoRoot) {
  // Only validate C# test files — [Test]/[UnityTest] extraction is C#-specific.
  if (!anchor.filePath.endsWith(".cs")) {
    return { ok: true }; // non-C# anchors skipped silently
  }
  const absPath = resolve(repoRoot, anchor.filePath);
  if (!existsSync(absPath)) {
    return { ok: false, errorKind: "file_not_found" };
  }
  const source = readFileSync(absPath, "utf8");
  const methodBody = extractTestMethodBody(source, anchor.methodName);
  if (!methodBody) {
    return { ok: false, errorKind: "method_not_found" };
  }
  const combined = collectHelperBodies(methodBody, source);
  const keywords = extractSurfaceKeywords(anchor.prose);
  const missing = keywords.filter((kw) => !combined.includes(kw));
  if (missing.length > 0) {
    return { ok: false, errorKind: "drift", missingKeywords: missing };
  }
  return { ok: true };
}

/**
 * Validate all anchors across a list of task records.
 * @param {Array<{taskId: string, bodyMd: string}>} tasks
 * @param {string} repoRoot
 * @returns {{errors: string[], driftCount: number}}
 */
export function validateAllAnchors(tasks, repoRoot) {
  const errors = [];
  let driftCount = 0;
  for (const { taskId, bodyMd } of tasks) {
    const section = extractRedStageProofSection(bodyMd);
    if (!section) continue; // skip — no §Red-Stage Proof
    const anchors = parseAnchors(section);
    for (const anchor of anchors) {
      const result = validateAnchor({ ...anchor, taskId }, repoRoot);
      if (!result.ok) {
        driftCount++;
        if (result.errorKind === "drift") {
          errors.push(
            `[anchor-drift] task=${taskId} anchor=${anchor.filePath}::${anchor.methodName} missing_keywords=[${result.missingKeywords.join(", ")}]`,
          );
        } else {
          errors.push(
            `[anchor-missing] task=${taskId} anchor=${anchor.filePath}::${anchor.methodName} reason=${result.errorKind}`,
          );
        }
      }
    }
  }
  return { errors, driftCount };
}

// ── DB query ──────────────────────────────────────────────────────────────────

function loadEnv() {
  const envPath = resolve(REPO_ROOT, ".env");
  if (existsSync(envPath)) {
    const lines = readFileSync(envPath, "utf8").split("\n");
    for (const line of lines) {
      const m = line.match(/^([A-Z_][A-Z0-9_]*)=(.*)$/);
      if (m && !process.env[m[1]]) {
        process.env[m[1]] = m[2].trim();
      }
    }
  }
}

async function queryActiveTasks(databaseUrl) {
  const pgRequire = createRequire(join(REPO_ROOT, "tools/postgres-ia/package.json"));
  const pg = pgRequire("pg");
  const client = new pg.Client({ connectionString: databaseUrl });
  await client.connect();
  try {
    // ia_tasks.body is the task spec body (Plan Digest included).
    const result = await client.query(`
      SELECT t.task_id, t.body AS body_md
      FROM ia_tasks t
      WHERE t.status NOT IN ('archived', 'done')
        AND t.body IS NOT NULL
        AND t.body ILIKE '%§Red-Stage Proof%'
    `);
    return result.rows.map((r) => ({ taskId: r.task_id, bodyMd: r.body_md }));
  } finally {
    await client.end();
  }
}

// ── Main ──────────────────────────────────────────────────────────────────────

async function main() {
  loadEnv();
  const databaseUrl = process.env.DATABASE_URL;
  if (!databaseUrl) {
    console.error("[red-stage-proof-anchor] ERROR: DATABASE_URL not set");
    process.exit(2);
  }

  let tasks;
  try {
    tasks = await queryActiveTasks(databaseUrl);
  } catch (err) {
    console.error(`[red-stage-proof-anchor] DB query failed: ${err.message}`);
    process.exit(2);
  }

  if (tasks.length === 0) {
    console.log("[red-stage-proof-anchor] OK: no active tasks with §Red-Stage Proof anchors.");
    process.exit(0);
  }

  const { errors, driftCount } = validateAllAnchors(tasks, REPO_ROOT);

  if (errors.length > 0) {
    for (const err of errors) {
      console.error(err);
    }
    console.error(
      `\n[red-stage-proof-anchor] FAIL: ${driftCount} anchor(s) drifted. ` +
        "Fix test files so method bodies reference all surface keywords from §Red-Stage Proof prose.",
    );
    process.exit(1);
  }

  console.log(
    `[red-stage-proof-anchor] OK: ${tasks.length} task(s) checked, all anchors aligned.`,
  );
  process.exit(0);
}

main().catch((err) => {
  console.error(`[red-stage-proof-anchor] Unhandled error: ${err.message}`);
  process.exit(2);
});
