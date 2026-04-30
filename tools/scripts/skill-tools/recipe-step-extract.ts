/**
 * Shared helpers for recipe-step-drift detection.
 *
 * Extracts recipe step IDs, SKILL.md phase counts, and agent-body step refs
 * for recipified skills. Shared between validate-skill-recipe-drift.ts and
 * snapshot-baseline.ts.
 */

import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";
import yaml from "js-yaml";
import { REPO_ROOT, listSkillSlugs, splitFrontmatter, parseRawFrontmatter } from "./frontmatter.js";

export const BASELINES_DIR = path.join(REPO_ROOT, "ia", "state", "skill-recipe-baselines");

export interface RecipeStepBundle {
  recipe_step_ids: string[];
  skill_phase_ids: string[];
  agent_step_refs: string[];
  hash: string;
}

export interface BaselineRecord {
  slug: string;
  hash: string;
  captured_at: string;
  recipe_step_ids: string[];
  skill_phase_ids: string[];
  agent_step_refs: string[];
}

// ---------------------------------------------------------------------------
// Recipified slug detection
// ---------------------------------------------------------------------------

export function listRecipifiedSlugs(): string[] {
  const recipesDir = path.join(REPO_ROOT, "tools", "recipes");
  const recipeSlugs = new Set(
    fs
      .readdirSync(recipesDir)
      .filter((f) => f.endsWith(".yaml"))
      .map((f) => f.slice(0, -5)),
  );
  return listSkillSlugs().filter((slug) => recipeSlugs.has(slug));
}

// ---------------------------------------------------------------------------
// Extraction helpers
// ---------------------------------------------------------------------------

interface YamlStep {
  id: string;
  steps?: YamlStep[];
  then?: YamlStep[];
  else?: YamlStep[];
}

function collectStepIds(steps: YamlStep[]): string[] {
  const ids: string[] = [];
  for (const s of steps) {
    if (s.id) ids.push(s.id.toLowerCase().trim());
    if (s.steps) ids.push(...collectStepIds(s.steps));
    if (s.then) ids.push(...collectStepIds(s.then));
    if (s.else) ids.push(...collectStepIds(s.else));
  }
  return ids;
}

export function extractRecipeStepIds(slug: string): string[] {
  const recipePath = path.join(REPO_ROOT, "tools", "recipes", `${slug}.yaml`);
  const raw = yaml.load(fs.readFileSync(recipePath, "utf8")) as { steps?: YamlStep[] };
  if (!Array.isArray(raw?.steps)) return [];
  // Only top-level step IDs are canonical for drift detection.
  return raw.steps.map((s) => s.id.toLowerCase().trim());
}

export function extractSkillPhaseIds(slug: string): string[] {
  const skillPath = path.join(REPO_ROOT, "ia", "skills", slug, "SKILL.md");
  const text = fs.readFileSync(skillPath, "utf8");
  const { fmBlock } = splitFrontmatter(text);
  const raw = parseRawFrontmatter(fmBlock);
  const phases = raw["phases"];
  if (!Array.isArray(phases)) return [];
  return (phases as string[]).map((p) => p.toLowerCase().trim());
}

export function extractAgentStepRefs(slug: string, recipeStepIds: string[]): string[] {
  const agentPath = path.join(REPO_ROOT, ".claude", "agents", `${slug}.md`);
  if (!fs.existsSync(agentPath)) return [];
  const body = fs.readFileSync(agentPath, "utf8").toLowerCase();
  return recipeStepIds.filter((id) => body.includes(id));
}

// ---------------------------------------------------------------------------
// Hash
// ---------------------------------------------------------------------------

export function computeHash(
  recipe_step_ids: string[],
  skill_phase_ids: string[],
  agent_step_refs: string[],
): string {
  const canonical = JSON.stringify({
    r: [...recipe_step_ids].sort(),
    s: [...skill_phase_ids].sort(),
    a: [...agent_step_refs].sort(),
  });
  return crypto.createHash("sha256").update(canonical).digest("hex").slice(0, 16);
}

// ---------------------------------------------------------------------------
// Bundle
// ---------------------------------------------------------------------------

export function buildBundle(slug: string): RecipeStepBundle {
  const recipe_step_ids = extractRecipeStepIds(slug);
  const skill_phase_ids = extractSkillPhaseIds(slug);
  const agent_step_refs = extractAgentStepRefs(slug, recipe_step_ids);
  const hash = computeHash(recipe_step_ids, skill_phase_ids, agent_step_refs);
  return { recipe_step_ids, skill_phase_ids, agent_step_refs, hash };
}

// ---------------------------------------------------------------------------
// Baseline I/O
// ---------------------------------------------------------------------------

export function loadBaseline(slug: string): BaselineRecord | null {
  const p = path.join(BASELINES_DIR, `${slug}.json`);
  if (!fs.existsSync(p)) return null;
  return JSON.parse(fs.readFileSync(p, "utf8")) as BaselineRecord;
}

export function writeBaseline(slug: string, bundle: RecipeStepBundle): void {
  fs.mkdirSync(BASELINES_DIR, { recursive: true });
  const record: BaselineRecord = {
    slug,
    hash: bundle.hash,
    captured_at: new Date().toISOString(),
    recipe_step_ids: bundle.recipe_step_ids,
    skill_phase_ids: bundle.skill_phase_ids,
    agent_step_refs: bundle.agent_step_refs,
  };
  const content = JSON.stringify(record, Object.keys(record).sort(), 2) + "\n";
  fs.writeFileSync(path.join(BASELINES_DIR, `${slug}.json`), content);
}
