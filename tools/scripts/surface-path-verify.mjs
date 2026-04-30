#!/usr/bin/env node
/**
 * surface:path-verify — warn-only check that relevant_surfaces paths exist.
 *
 * Gate kind in stage-file recipe (Phase 2.2b). Non-blocking: always exit 0.
 * Warns on missing paths so ghost references are surfaced before spec stubs
 * inherit them.
 *
 * Args: --slug <slug> --stage_id <X.Y> --paths <path> [--paths <path> ...]
 */

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "../..");

const args = process.argv.slice(2);
const paths = [];
let slug = "";
let stageId = "";

for (let i = 0; i < args.length; i++) {
  if (args[i] === "--paths" && args[i + 1]) { paths.push(args[++i]); continue; }
  if (args[i] === "--slug" && args[i + 1]) { slug = args[++i]; continue; }
  if (args[i] === "--stage_id" && args[i + 1]) { stageId = args[++i]; continue; }
}

if (paths.length === 0) {
  console.log(`[surface:path-verify] no paths supplied (slug=${slug} stage=${stageId}) — skip`);
  process.exit(0);
}

let warns = 0;
for (const p of paths) {
  const abs = path.isAbsolute(p) ? p : path.join(REPO_ROOT, p);
  if (!fs.existsSync(abs)) {
    console.warn(`[surface:path-verify] WARN ghost path: ${p} (slug=${slug} stage=${stageId})`);
    warns++;
  }
}

if (warns === 0) {
  console.log(`[surface:path-verify] OK — ${paths.length} path(s) verified (slug=${slug} stage=${stageId})`);
} else {
  console.log(`[surface:path-verify] ${warns} ghost path(s) — warn-only, gate non-blocking`);
}

process.exit(0);
