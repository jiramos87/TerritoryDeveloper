#!/usr/bin/env tsx
/**
 * validate-mcp-catalog-coverage.ts — TECH-5123 DEC-A43 drift gate.
 *
 * Checks that every expected catalog entity MCP tool (catalog_{kind}_{op})
 * is registered in the MCP server. Also checks for orphan catalog_ tools
 * (registered but not in the expected convention set).
 *
 * Exit codes:
 *   0  all expected tools registered; no unexpected orphans
 *   1  missing or orphan tools found
 *
 * Usage:
 *   npx tsx tools/scripts/validate-mcp-catalog-coverage.ts
 *   npx tsx tools/scripts/validate-mcp-catalog-coverage.ts --json
 */

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..");

const JSON_FLAG = process.argv.includes("--json");

// ── Expected tool set (derived from convention) ───────────────────────────────

const CATALOG_KINDS = [
  "sprite", "asset", "button", "panel", "audio", "pool", "token", "archetype",
] as const;

const READ_OPS = ["list", "get", "get_version", "refs", "search"] as const;
const MUTATE_OPS = ["create", "update", "retire", "restore", "publish"] as const;

const EXPECTED_TOOLS: ReadonlySet<string> = new Set([
  ...CATALOG_KINDS.flatMap((kind) => [
    ...READ_OPS.map((op) => `catalog_${kind}_${op}`),
    ...MUTATE_OPS.map((op) => `catalog_${kind}_${op}`),
  ]),
  "catalog_bulk_action",
]);

// ── Known non-catalog-entity tools that match catalog_ prefix (not orphans) ──
const KNOWN_LEGACY_TOOLS = new Set([
  "catalog_get",
  "catalog_list",
  "catalog_upsert",
  "catalog_spawn_pool_list",
  "catalog_spawn_pool_get",
  "catalog_spawn_pool_upsert",
]);

// ── Extract registered tool names from MCP server source ─────────────────────

function extractKindsFromSource(src: string): string[] | null {
  // Match: const CATALOG_KINDS = ["sprite", ...] or as const
  const m = src.match(/CATALOG_KINDS\s*=\s*\[([^\]]+)\]/);
  if (!m) return null;
  const raw = m[1]!;
  return raw.match(/"([^"]+)"/g)?.map((s) => s.replace(/"/g, "")) ?? null;
}

function extractRegisteredCatalogTools(): Set<string> {
  const toolsDir = path.join(REPO_ROOT, "tools", "mcp-ia-server", "src", "tools");
  const registered = new Set<string>();

  const files = fs.readdirSync(toolsDir).filter((f) => f.endsWith(".ts"));

  // Collect kinds from the canonical source file first
  let kindsForExpansion: string[] | null = null;
  for (const file of files) {
    const src = fs.readFileSync(path.join(toolsDir, file), "utf8");
    const kinds = extractKindsFromSource(src);
    if (kinds && kinds.length > 0) {
      kindsForExpansion = kinds;
      break;
    }
  }

  for (const file of files) {
    const src = fs.readFileSync(path.join(toolsDir, file), "utf8");

    // Match literal string tool names: registerTool("catalog_xxx" or `catalog_xxx`
    const literalRe = /registerTool\s*\(\s*["']([^"']+)["']/g;
    let m: RegExpExecArray | null;
    while ((m = literalRe.exec(src)) !== null) {
      const name = m[1]!;
      if (name.startsWith("catalog_") && !name.includes("${")) {
        registered.add(name);
      }
    }

    // Match template literal tool names: `catalog_${kind}_op`
    const templateRe = /registerTool\s*\(\s*`(catalog_[^`]+)`/g;
    while ((m = templateRe.exec(src)) !== null) {
      const template = m[1]!;
      if (template.includes("${kind}") && kindsForExpansion) {
        for (const kind of kindsForExpansion) {
          registered.add(template.replace("${kind}", kind));
        }
      } else if (!template.includes("${")) {
        registered.add(template);
      }
    }
  }
  return registered;
}

// ── Main ─────────────────────────────────────────────────────────────────────

const registered = extractRegisteredCatalogTools();

const missing: string[] = [];
const orphan: string[] = [];

for (const expected of EXPECTED_TOOLS) {
  if (!registered.has(expected)) {
    missing.push(expected);
  }
}

for (const reg of registered) {
  if (!EXPECTED_TOOLS.has(reg) && !KNOWN_LEGACY_TOOLS.has(reg)) {
    orphan.push(reg);
  }
}

if (JSON_FLAG) {
  process.stdout.write(JSON.stringify({ missing, orphan }, null, 2) + "\n");
} else {
  if (missing.length === 0 && orphan.length === 0) {
    process.stdout.write(`catalog MCP coverage OK — ${EXPECTED_TOOLS.size} tools verified\n`);
  } else {
    if (missing.length > 0) {
      process.stdout.write("MISSING (route has no MCP tool):\n");
      for (const t of missing) {
        process.stdout.write(`  ${t} → status: missing\n`);
      }
    }
    if (orphan.length > 0) {
      process.stdout.write("ORPHAN (MCP tool has no REST route):\n");
      for (const t of orphan) {
        process.stdout.write(`  ${t} → status: orphan\n`);
      }
    }
  }
}

process.exit(missing.length > 0 || orphan.length > 0 ? 1 : 0);
