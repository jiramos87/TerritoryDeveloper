#!/usr/bin/env node
/**
 * validate-panel-blueprint-harness.mjs — Node-shell behavioral validator.
 *
 * Implements the same logic as AgentBridgeCommandRunner.RunValidatePanelBlueprint
 * but runs as a standalone Node script so tests can invoke it without a live Unity Editor.
 *
 * Usage:
 *   node validate-panel-blueprint-harness.mjs --panel-id <slug|id> [--panels-file <path>]
 *
 * Returns JSON to stdout:
 *   { ok: bool, panel_id: string, kindsChecked: number, missing: [{path, required}] }
 *
 * Exits 0 when ok=true, 1 when ok=false (missing keys found or panel not found).
 *
 * TECH-27543 (bake-pipeline-hardening Stage 5) — closes audit finding C4.
 */

import { readFileSync, existsSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const REPO_ROOT = resolve(__dirname, "..", "..");

// ── Arg parse ────────────────────────────────────────────────────────────────

function parseArgs(argv) {
  const args = {};
  for (let i = 2; i < argv.length; i++) {
    if (argv[i] === "--panel-id" && argv[i + 1]) {
      args.panelId = argv[++i];
    } else if (argv[i] === "--panels-file" && argv[i + 1]) {
      args.panelsFile = argv[++i];
    }
  }
  return args;
}

// ── YAML parser for panel-schema.yaml ────────────────────────────────────────
// Minimal: extracts kinds[].kind + kinds[].required_keys[] without a YAML dep.

function parsePanelSchema(text) {
  const requiredByKind = {};
  let currentKind = null;
  let inRequiredKeys = false;

  for (const rawLine of text.split("\n")) {
    const line = rawLine.trimEnd();
    if (!line.trim() || line.trim().startsWith("#")) continue;

    // Match `  - kind: <slug>` (top-level kinds list item)
    const kindMatch = line.match(/^\s+-\s+kind:\s*(.+)$/);
    if (kindMatch) {
      currentKind = kindMatch[1].trim().replace(/^['"]|['"]$/g, "");
      requiredByKind[currentKind] = [];
      inRequiredKeys = false;
      continue;
    }

    // Match `    required_keys:` heading
    if (currentKind && line.match(/^\s+required_keys\s*:/)) {
      inRequiredKeys = true;
      continue;
    }

    // Match `      - <key>` under required_keys
    if (inRequiredKeys && currentKind) {
      const keyMatch = line.match(/^\s+-\s+(.+)$/);
      if (keyMatch) {
        const key = keyMatch[1].trim().replace(/^['"]|['"]$/g, "");
        if (key) requiredByKind[currentKind].push(key);
        continue;
      }
      // Any non-indented or differently-indented line resets required_keys context.
      if (line.match(/^\S/) || line.match(/^\s{0,3}\S/)) {
        inRequiredKeys = false;
      }
    }
  }
  return requiredByKind;
}

// ── Main ──────────────────────────────────────────────────────────────────────

function main() {
  const args = parseArgs(process.argv);

  if (!args.panelId) {
    process.stderr.write("validate-panel-blueprint-harness: --panel-id is required\n");
    process.exit(2);
  }

  // Load panel-schema.yaml.
  const schemaPath = resolve(REPO_ROOT, "tools", "blueprints", "panel-schema.yaml");
  if (!existsSync(schemaPath)) {
    process.stderr.write(`validate-panel-blueprint-harness: panel-schema.yaml not found at ${schemaPath}\n`);
    process.exit(2);
  }
  const requiredByKind = parsePanelSchema(readFileSync(schemaPath, "utf8"));

  // Load panels.json (fixture override or canonical).
  const panelsPath = args.panelsFile
    ? resolve(args.panelsFile)
    : resolve(REPO_ROOT, "Assets", "UI", "Snapshots", "panels.json");

  if (!existsSync(panelsPath)) {
    process.stderr.write(`validate-panel-blueprint-harness: panels.json not found at ${panelsPath}\n`);
    process.exit(2);
  }

  let panels;
  try {
    panels = JSON.parse(readFileSync(panelsPath, "utf8"));
  } catch (err) {
    process.stderr.write(`validate-panel-blueprint-harness: panels.json parse failed: ${err.message}\n`);
    process.exit(2);
  }

  // Find panel by id or slug.
  const panelId = args.panelId;
  const items = panels.items ?? panels;
  const panel = Array.isArray(items)
    ? items.find((p) => p.id === panelId || p.slug === panelId)
    : null;

  if (!panel) {
    const result = {
      ok: false,
      panel_id: panelId,
      kindsChecked: 0,
      missing: [{ path: "panel", required: `panel with id/slug '${panelId}' not found` }],
    };
    process.stdout.write(JSON.stringify(result) + "\n");
    process.exit(1);
  }

  // Validate per-child required keys.
  const children = panel.children ?? [];
  const missing = [];
  const kindsChecked = new Set();

  for (let ci = 0; ci < children.length; ci++) {
    const child = children[ci];
    const kind = child.kind ?? child.params_json?.kind ?? null;
    if (!kind) continue;

    kindsChecked.add(kind);
    const required = requiredByKind[kind];
    if (!required || required.length === 0) continue;

    const params = child.params_json ?? {};
    for (const key of required) {
      const val = params[key];
      if (val === undefined || val === null || val === "") {
        missing.push({
          path: `items[?].children[${ci}].params_json.${key}`,
          required: key,
          kind,
        });
      }
    }
  }

  const result = {
    ok: missing.length === 0,
    panel_id: panelId,
    kindsChecked: kindsChecked.size,
    missing,
  };

  process.stdout.write(JSON.stringify(result) + "\n");
  process.exit(result.ok ? 0 : 1);
}

main();
