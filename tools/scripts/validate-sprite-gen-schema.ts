/**
 * validate-sprite-gen-schema.ts
 *
 * TECH-1434 — Pydantic ↔ ui_hints field-parity gate for the sprite-gen
 * `params/` package. Invokes `python -m src.params.dump_schema`, loads
 * `tools/sprite-gen/src/params/ui_hints.json`, and asserts every Pydantic
 * field path appears as a key in the ui_hints sidecar (and vice versa).
 *
 * Exit codes:
 *   0  parity OK across both endpoints (render + promote)
 *   1  parity drift — one or more fields in schema-only or hints-only
 *   2  internal error (missing python, malformed JSON, etc.)
 *
 * Usage:
 *   npx tsx tools/scripts/validate-sprite-gen-schema.ts
 *
 * Wired into `npm run validate:all` after `validate:capability-coverage`.
 */

import { spawnSync } from "node:child_process";
import * as fs from "node:fs";
import * as path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..");
const TOOL_ROOT = path.join(REPO_ROOT, "tools", "sprite-gen");
const HINTS_PATH = path.join(TOOL_ROOT, "src", "params", "ui_hints.json");

type SchemaDump = {
  render: { properties?: Record<string, unknown> };
  promote: { properties?: Record<string, unknown> };
};

type Hints = Record<string, unknown> & { _groups_order?: unknown };

type Finding = {
  endpoint: "render" | "promote";
  field: string;
  reason: "missing in ui_hints" | "missing in schema";
};

function dumpSchemaJson(): SchemaDump {
  // Prefer `python3` first; fall back to `python` for envs that only have
  // the unversioned alias.
  const candidates = ["python3", "python"];
  let lastErr = "";
  for (const bin of candidates) {
    const result = spawnSync(bin, ["-m", "src.params.dump_schema"], {
      cwd: TOOL_ROOT,
      encoding: "utf-8",
    });
    if (result.error || result.status !== 0) {
      lastErr = String(result.stderr ?? result.error?.message ?? "");
      continue;
    }
    try {
      return JSON.parse(result.stdout) as SchemaDump;
    } catch (e) {
      throw new Error(`malformed JSON from ${bin}: ${(e as Error).message}`);
    }
  }
  throw new Error(`python schema dump failed: ${lastErr}`);
}

function loadHints(): Hints {
  const raw = fs.readFileSync(HINTS_PATH, "utf-8");
  return JSON.parse(raw) as Hints;
}

function compareEndpoint(
  endpoint: "render" | "promote",
  schemaProps: Set<string>,
  hintsKeys: Set<string>,
): Finding[] {
  const findings: Finding[] = [];
  for (const field of schemaProps) {
    if (!hintsKeys.has(field)) {
      findings.push({ endpoint, field, reason: "missing in ui_hints" });
    }
  }
  for (const field of hintsKeys) {
    if (!schemaProps.has(field)) {
      findings.push({ endpoint, field, reason: "missing in schema" });
    }
  }
  return findings;
}

function main(): number {
  let dump: SchemaDump;
  try {
    dump = dumpSchemaJson();
  } catch (e) {
    process.stderr.write(`[validate:sprite-gen-schema] ${(e as Error).message}\n`);
    return 2;
  }

  let hints: Hints;
  try {
    hints = loadHints();
  } catch (e) {
    process.stderr.write(`[validate:sprite-gen-schema] failed to load ui_hints: ${(e as Error).message}\n`);
    return 2;
  }

  const renderSchemaFields = new Set(Object.keys(dump.render.properties ?? {}));
  const promoteSchemaFields = new Set(Object.keys(dump.promote.properties ?? {}));

  const renderHints = (hints.render ?? {}) as Record<string, unknown>;
  const promoteHints = (hints.promote ?? {}) as Record<string, unknown>;
  const renderHintsFields = new Set(
    Object.keys(renderHints).filter((k) => !k.startsWith("_")),
  );
  const promoteHintsFields = new Set(
    Object.keys(promoteHints).filter((k) => !k.startsWith("_")),
  );

  const findings: Finding[] = [
    ...compareEndpoint("render", renderSchemaFields, renderHintsFields),
    ...compareEndpoint("promote", promoteSchemaFields, promoteHintsFields),
  ];

  if (findings.length === 0) {
    process.stdout.write("[validate:sprite-gen-schema] OK — render + promote field parity intact\n");
    return 0;
  }

  for (const f of findings) {
    process.stderr.write(
      `[validate:sprite-gen-schema] ${f.endpoint}.${f.field} — ${f.reason}\n`,
    );
  }
  return 1;
}

process.exit(main());
