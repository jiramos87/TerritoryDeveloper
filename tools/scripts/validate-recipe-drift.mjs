#!/usr/bin/env node
/**
 * validate:recipe-drift — DEC-A19 Phase B drift gate.
 *
 * Walks tools/recipes/*.yaml and asserts:
 *   1. Each recipe parses as YAML.
 *   2. Each recipe validates against tools/recipe-engine/schema/recipe.schema.json.
 *   3. Recipe slug (`recipe:` field) matches file basename.
 *   4. Every `seam.{name}` step references an existing tools/seams/{name}/ dir
 *      with both input.schema.json + output.schema.json.
 *   5. seam steps carry no `retry:` (Q5 policy).
 *   6. Every `gate.{name}` references an npm script in root package.json.
 *
 * Phase C will additionally diff recipe ↔ skill registry (skill SKILL.md `recipe:`
 * frontmatter must match a tools/recipes/{name}.yaml).
 */

import { promises as fs, existsSync as nodeExistsSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import yaml from "js-yaml";
import Ajv from "../mcp-ia-server/node_modules/ajv/dist/ajv.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..");
const RECIPES_DIR = path.join(REPO_ROOT, "tools", "recipes");
const SEAMS_DIR = path.join(REPO_ROOT, "tools", "seams");
const RECIPE_SCHEMA = path.join(REPO_ROOT, "tools", "recipe-engine", "schema", "recipe.schema.json");
const ROOT_PKG = path.join(REPO_ROOT, "package.json");

async function main() {
  const failures = [];
  const schema = JSON.parse(await fs.readFile(RECIPE_SCHEMA, "utf8"));
  const ajv = new Ajv.default({ allErrors: true, strict: false });
  const validate = ajv.compile(schema);

  const pkg = JSON.parse(await fs.readFile(ROOT_PKG, "utf8"));
  const npmScripts = new Set(Object.keys(pkg.scripts ?? {}));

  let entries;
  try {
    entries = await fs.readdir(RECIPES_DIR);
  } catch (err) {
    if (err.code === "ENOENT") {
      console.log("[validate:recipe-drift] OK — tools/recipes/ absent (no recipes yet)");
      return;
    }
    throw err;
  }
  const yamlFiles = entries.filter((f) => f.endsWith(".yaml") || f.endsWith(".yml")).sort();
  if (yamlFiles.length === 0) {
    console.log("[validate:recipe-drift] OK — no recipes under tools/recipes/");
    return;
  }

  for (const file of yamlFiles) {
    const full = path.join(RECIPES_DIR, file);
    const rel = path.relative(REPO_ROOT, full);
    const expectSlug = path.basename(file).replace(/\.(yaml|yml)$/, "");
    let recipe;
    try {
      const raw = await fs.readFile(full, "utf8");
      recipe = yaml.load(raw);
    } catch (err) {
      failures.push(`[${rel}] YAML parse failed: ${err.message}`);
      continue;
    }
    if (!recipe || typeof recipe !== "object") {
      failures.push(`[${rel}] empty or non-object recipe`);
      continue;
    }
    if (!validate(recipe)) {
      failures.push(`[${rel}] schema validation failed:\n${formatErrors(validate.errors)}`);
      continue;
    }
    if (recipe.recipe !== expectSlug) {
      failures.push(`[${rel}] recipe slug '${recipe.recipe}' must equal file basename '${expectSlug}'`);
    }

    walkSteps(recipe.steps ?? [], (step, parents) => {
      if ("seam" in step) {
        const seamDir = path.join(SEAMS_DIR, step.seam);
        const inputSchema = path.join(seamDir, "input.schema.json");
        const outputSchema = path.join(seamDir, "output.schema.json");
        for (const p of [inputSchema, outputSchema]) {
          if (!existsSync(p)) {
            failures.push(`[${rel}] step '${parents.concat(step.id).join("/")}' references missing seam asset ${path.relative(REPO_ROOT, p)}`);
          }
        }
        if (step.retry) {
          failures.push(`[${rel}] step '${parents.concat(step.id).join("/")}' has retry on seam — rejected per Q5 escalate-to-human policy`);
        }
      }
      if ("gate" in step) {
        if (!npmScripts.has(step.gate)) {
          failures.push(`[${rel}] step '${parents.concat(step.id).join("/")}' references unknown npm script '${step.gate}'`);
        }
      }
    });
  }

  if (failures.length > 0) {
    console.error("[validate:recipe-drift] FAIL");
    for (const f of failures) console.error(f);
    console.error(`[validate:recipe-drift] ${failures.length} failure(s) across ${yamlFiles.length} recipe(s)`);
    process.exit(1);
  }
  console.log(`[validate:recipe-drift] OK — ${yamlFiles.length} recipe(s) validated`);
}

function existsSync(p) {
  return nodeExistsSync(p);
}

function walkSteps(steps, visit, parents = []) {
  for (const step of steps) {
    visit(step, parents);
    if ("flow" in step) {
      const childParents = parents.concat(step.id);
      if (step.flow === "seq" || step.flow === "parallel" || step.flow === "until") {
        walkSteps(step.steps ?? [], visit, childParents);
      } else if (step.flow === "when") {
        walkSteps(step.then ?? [], visit, childParents);
        walkSteps(step.else ?? [], visit, childParents);
      }
    }
  }
}

function formatErrors(errs) {
  if (!errs || errs.length === 0) return "  (no error detail)";
  return errs
    .map((e) => `    - ${e.instancePath || "/"} ${e.message}${e.params ? " " + JSON.stringify(e.params) : ""}`)
    .join("\n");
}

main().catch((err) => {
  console.error("[validate:recipe-drift] uncaught error:", err);
  process.exit(1);
});
