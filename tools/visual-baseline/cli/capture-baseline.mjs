#!/usr/bin/env node
/**
 * capture-baseline.mjs — CLI entry for golden-image baseline capture.
 * Usage: node tools/visual-baseline/cli/capture-baseline.mjs [--panel <slug>]
 *
 * Captures rendered panel PNGs under tools/visual-baseline/golden/ and
 * writes sha256 checksums to .checksum-manifest.json.
 *
 * Stage 1 tracer: harness scaffolded here; rendering deferred to unity:visual-diff
 * (tools/scripts/unity-visual-diff.mjs) which invokes Unity batchmode to render.
 */

import { createHash } from "node:crypto";
import { readFileSync, writeFileSync, readdirSync, existsSync } from "node:fs";
import { join, dirname, basename } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const goldenDir = join(__dirname, "../golden");
const manifestPath = join(goldenDir, ".checksum-manifest.json");

function sha256(buf) {
  return createHash("sha256").update(buf).digest("hex");
}

function updateManifest(slug, pngPath) {
  const manifest = existsSync(manifestPath)
    ? JSON.parse(readFileSync(manifestPath, "utf8"))
    : { version: 1, entries: {} };
  const buf = readFileSync(pngPath);
  manifest.entries[slug] = sha256(buf);
  writeFileSync(manifestPath, JSON.stringify(manifest, null, 2) + "\n", "utf8");
  console.log(`[capture-baseline] Updated manifest: ${slug} → ${manifest.entries[slug].slice(0, 8)}...`);
}

function scanGoldens() {
  if (!existsSync(goldenDir)) {
    console.log("[capture-baseline] golden/ directory absent — no PNGs to scan.");
    return;
  }
  const pngs = readdirSync(goldenDir).filter((f) => f.endsWith(".png"));
  if (pngs.length === 0) {
    console.log("[capture-baseline] No golden PNGs found in golden/. Run unity:visual-diff --capture first.");
    return;
  }
  for (const png of pngs) {
    const slug = basename(png, ".png");
    updateManifest(slug, join(goldenDir, png));
  }
  console.log(`[capture-baseline] Scanned ${pngs.length} goldens.`);
}

const args = process.argv.slice(2);
const panelIdx = args.indexOf("--panel");
if (panelIdx >= 0 && args[panelIdx + 1]) {
  const slug = args[panelIdx + 1];
  const pngPath = join(goldenDir, `${slug}.png`);
  if (!existsSync(pngPath)) {
    console.error(`[capture-baseline] ERROR: golden PNG missing for panel "${slug}" at ${pngPath}`);
    process.exit(1);
  }
  updateManifest(slug, pngPath);
} else {
  scanGoldens();
}
