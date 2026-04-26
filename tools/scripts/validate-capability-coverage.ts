/**
 * validate-capability-coverage.ts
 *
 * TECH-1352 — DEC-A33 capability matrix drift gate.
 *
 * Walks every `web/app/api/catalog/**\/route.ts`, extracts `routeMeta` exports
 * keyed by HTTP verb, and asserts each declared `requires` capability id
 * exists in the `capability` table. Wired into `validate:all`.
 *
 * Exit codes:
 *   0  all routes have a valid `routeMeta.requires` mapping (or no handlers)
 *   1  one or more handlers missing `routeMeta.requires` or referencing an
 *      unknown capability id
 *   2  internal error (DB unreachable, AST parse failure, etc.)
 *
 * Usage:
 *   npx tsx tools/scripts/validate-capability-coverage.ts          # green tree
 *   npx tsx tools/scripts/validate-capability-coverage.ts <DIR>    # fixture dir
 *
 * The optional second arg lets unit tests point at fixture handlers under
 * `tools/scripts/__fixtures__/capability-coverage/`. Real-tree run uses the
 * default glob.
 *
 * The DB query can be stubbed via env `CAPABILITY_COVERAGE_FAKE_IDS` (comma-
 * separated id list) — used by the unit test to avoid spinning up a Postgres
 * connection.
 */

import path from "node:path";
import { fileURLToPath } from "node:url";
import { glob } from "glob";
import {
  Project,
  ObjectLiteralExpression,
  PropertyAssignment,
  StringLiteral,
  SyntaxKind,
} from "ts-morph";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..");
const VERBS = ["GET", "POST", "PATCH", "PUT", "DELETE"] as const;
type Verb = (typeof VERBS)[number];

type Finding = {
  file: string;
  verb: Verb;
  reason: "missing routeMeta.requires" | "unknown capability id";
  capability?: string;
};

async function loadCapabilityIds(): Promise<Set<string>> {
  const fake = process.env.CAPABILITY_COVERAGE_FAKE_IDS;
  if (fake) {
    return new Set(fake.split(",").map((s) => s.trim()).filter(Boolean));
  }
  // Reuse the resolver `db:migrate` uses — picks up DATABASE_URL,
  // `config/postgres-dev.json`, or the local default — so the validator runs
  // unattended in `validate:all` without extra env wiring.
  const { resolveDatabaseUrl } = await import(
    "../postgres-ia/resolve-database-url.mjs"
  );
  const dbUrl = resolveDatabaseUrl(REPO_ROOT) as string | null;
  if (!dbUrl) {
    throw new Error(
      "DATABASE_URL not resolvable — set DATABASE_URL or add config/postgres-dev.json. " +
        "Set CAPABILITY_COVERAGE_FAKE_IDS in tests to bypass.",
    );
  }
  const postgres = (await import("postgres")).default;
  const sql = postgres(dbUrl, { max: 1 });
  try {
    const rows = (await sql`select capability_id from capability`) as unknown as Array<{
      capability_id: string;
    }>;
    return new Set(rows.map((r) => r.capability_id));
  } finally {
    await sql.end({ timeout: 1 });
  }
}

function unquote(text: string): string {
  return text.replace(/^['"`]|['"`]$/g, "");
}

/**
 * Returns map of verb -> requires-id, OR `null` for "handler exists but has
 * no `routeMeta.requires`". Verbs not present in the file are omitted from
 * the map (no false positives).
 */
function extractRouteMeta(filePath: string): Map<Verb, string | null> {
  const project = new Project({ skipAddingFilesFromTsConfig: true });
  const source = project.addSourceFileAtPath(filePath);
  const out = new Map<Verb, string | null>();
  // Discover handler exports (function or const) per verb.
  for (const verb of VERBS) {
    const fn = source.getFunction(verb);
    const v = source.getVariableDeclaration(verb);
    if (fn || v) out.set(verb, null);
  }
  if (out.size === 0) return out;
  // Look for the `routeMeta` named export. Tolerate both `export const` and
  // a free `const routeMeta = ...` form.
  const meta = source.getVariableDeclaration("routeMeta");
  if (!meta) return out;
  // `as const` wraps the literal in an `AsExpression`; pierce it.
  const init = meta.getInitializer();
  if (!init) return out;
  const literal: ObjectLiteralExpression | undefined =
    init instanceof ObjectLiteralExpression
      ? init
      : init.getFirstDescendantByKind(SyntaxKind.ObjectLiteralExpression);
  if (!literal) return out;
  for (const prop of literal.getProperties()) {
    if (!(prop instanceof PropertyAssignment)) continue;
    const verbName = unquote(prop.getName()) as Verb;
    if (!VERBS.includes(verbName)) continue;
    const inner = prop.getInitializerIfKind(SyntaxKind.ObjectLiteralExpression);
    if (!inner) continue;
    const requiresProp = inner.getProperty("requires");
    if (!(requiresProp instanceof PropertyAssignment)) continue;
    const value = requiresProp.getInitializer();
    const text = value instanceof StringLiteral ? value.getLiteralText() : null;
    if (text != null) out.set(verbName, text);
  }
  return out;
}

async function main(): Promise<number> {
  const findings: Finding[] = [];
  const fixtureDir = process.argv[2];
  let files: string[];
  let capabilityIds: Set<string>;
  try {
    capabilityIds = await loadCapabilityIds();
  } catch (e) {
    console.error(`[capability-coverage] internal: ${(e as Error).message}`);
    return 2;
  }
  if (fixtureDir) {
    const abs = path.resolve(fixtureDir);
    files = await glob("**/*.ts", { cwd: abs, absolute: true });
  } else {
    files = await glob("web/app/api/catalog/**/route.ts", {
      cwd: REPO_ROOT,
      absolute: true,
    });
  }
  for (const file of files) {
    let handlers: Map<Verb, string | null>;
    try {
      handlers = extractRouteMeta(file);
    } catch (e) {
      console.error(
        `[capability-coverage] AST parse failed for ${path.relative(REPO_ROOT, file)}: ${(e as Error).message}`,
      );
      return 2;
    }
    for (const [verb, requires] of handlers) {
      if (requires === null) {
        findings.push({ file, verb, reason: "missing routeMeta.requires" });
      } else if (!capabilityIds.has(requires)) {
        findings.push({
          file,
          verb,
          reason: "unknown capability id",
          capability: requires,
        });
      }
    }
  }
  if (findings.length > 0) {
    for (const f of findings) {
      const rel = path.relative(REPO_ROOT, f.file);
      const tail = f.capability ? `: ${f.capability}` : "";
      console.error(`[capability-coverage] ${rel} ${f.verb} — ${f.reason}${tail}`);
    }
    return 1;
  }
  console.log(
    `[capability-coverage] OK — ${files.length} route file(s) validated against ${capabilityIds.size} capability id(s).`,
  );
  return 0;
}

main()
  .then((code) => process.exit(code))
  .catch((e) => {
    console.error(`[capability-coverage] internal: ${(e as Error).message}`);
    process.exit(2);
  });
