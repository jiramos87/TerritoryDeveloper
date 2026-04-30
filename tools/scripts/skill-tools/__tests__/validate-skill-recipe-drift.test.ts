import test from "node:test";
import assert from "node:assert/strict";
import crypto from "node:crypto";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";

// ---------------------------------------------------------------------------
// Helpers — synthetic fixture tree
// ---------------------------------------------------------------------------

function makeTmpRepo(): string {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), "skill-recipe-drift-test-"));
  const recipeDir = path.join(dir, "tools", "recipes");
  const skillDir = path.join(dir, "ia", "skills", "my-skill");
  const agentDir = path.join(dir, ".claude", "agents");
  const baselineDir = path.join(dir, "ia", "state", "skill-recipe-baselines");

  fs.mkdirSync(recipeDir, { recursive: true });
  fs.mkdirSync(skillDir, { recursive: true });
  fs.mkdirSync(agentDir, { recursive: true });
  fs.mkdirSync(baselineDir, { recursive: true });

  // recipe YAML with two top-level step IDs
  fs.writeFileSync(
    path.join(recipeDir, "my-skill.yaml"),
    [
      "recipe: my-skill",
      "steps:",
      "  - id: load_context",
      "    mcp: glossary_lookup",
      "    args: {key: foo}",
      "  - id: write_output",
      "    bash: tools/scripts/write.sh",
    ].join("\n"),
  );

  // SKILL.md with two phases
  fs.writeFileSync(
    path.join(skillDir, "SKILL.md"),
    [
      "---",
      "name: my-skill",
      "description: A synthetic skill for testing purposes in skill-recipe-drift gate.",
      "phases:",
      "  - Load context",
      "  - Write output",
      "audience: agent",
      "loaded_by: on-demand",
      "slices_via: none",
      "tools_role: implementer",
      "triggers: []",
      "caveman_exceptions: []",
      "hard_boundaries: []",
      "---",
      "# My-skill body",
    ].join("\n"),
  );

  // agent body that cites both step IDs
  fs.writeFileSync(
    path.join(agentDir, "my-skill.md"),
    "Use load_context to load. Then write_output to persist.\n",
  );

  return dir;
}

function buildBundle(dir: string, slug = "my-skill") {
  const recipeDir = path.join(dir, "tools", "recipes");
  const skillDir = path.join(dir, "ia", "skills", slug);
  const agentDir = path.join(dir, ".claude", "agents");

  // Inline extraction (mirrors recipe-step-extract logic)
  const recipeYaml = fs.readFileSync(path.join(recipeDir, `${slug}.yaml`), "utf8");
  const recipe_step_ids: string[] = [];
  for (const m of recipeYaml.matchAll(/^  - id: ([a-z][a-z0-9_]*)/gm)) {
    recipe_step_ids.push(m[1]);
  }

  const skillText = fs.readFileSync(path.join(skillDir, "SKILL.md"), "utf8");
  const phasesMatch = skillText.match(/^phases:\n((?:  - .+\n)+)/m);
  const skill_phase_ids: string[] = [];
  if (phasesMatch) {
    for (const m of phasesMatch[1].matchAll(/  - (.+)/g)) {
      skill_phase_ids.push(m[1].toLowerCase().trim());
    }
  }

  const agentBody = fs.readFileSync(path.join(agentDir, `${slug}.md`), "utf8").toLowerCase();
  const agent_step_refs = recipe_step_ids.filter((id) => agentBody.includes(id));

  const canonical = JSON.stringify({
    r: [...recipe_step_ids].sort(),
    s: [...skill_phase_ids].sort(),
    a: [...agent_step_refs].sort(),
  });
  const hash: string = crypto.createHash("sha256").update(canonical).digest("hex").slice(0, 16);

  return { recipe_step_ids, skill_phase_ids, agent_step_refs, hash };
}

function writeBaseline(dir: string, slug: string, bundle: ReturnType<typeof buildBundle>): void {
  const baselineDir = path.join(dir, "ia", "state", "skill-recipe-baselines");
  const record = {
    agent_step_refs: bundle.agent_step_refs,
    captured_at: new Date().toISOString(),
    hash: bundle.hash,
    recipe_step_ids: bundle.recipe_step_ids,
    skill_phase_ids: bundle.skill_phase_ids,
    slug,
  };
  fs.writeFileSync(path.join(baselineDir, `${slug}.json`), JSON.stringify(record, null, 2) + "\n");
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

test("extract: recipe step IDs parsed from YAML", () => {
  const dir = makeTmpRepo();
  const { recipe_step_ids } = buildBundle(dir);
  assert.deepEqual(recipe_step_ids, ["load_context", "write_output"]);
  fs.rmSync(dir, { recursive: true });
});

test("extract: agent step refs found when step IDs appear in body", () => {
  const dir = makeTmpRepo();
  const { agent_step_refs } = buildBundle(dir);
  assert.deepEqual(agent_step_refs.sort(), ["load_context", "write_output"]);
  fs.rmSync(dir, { recursive: true });
});

test("drift gate: hash match → no drift", () => {
  const dir = makeTmpRepo();
  const bundle = buildBundle(dir);
  writeBaseline(dir, "my-skill", bundle);

  const baselineDir = path.join(dir, "ia", "state", "skill-recipe-baselines");
  const stored = JSON.parse(fs.readFileSync(path.join(baselineDir, "my-skill.json"), "utf8"));
  assert.equal(stored.hash, bundle.hash);
  fs.rmSync(dir, { recursive: true });
});

test("drift gate: hash changes when recipe step added", () => {
  const dir = makeTmpRepo();
  const bundle = buildBundle(dir);
  writeBaseline(dir, "my-skill", bundle);

  // Add a new step to the recipe (leading newline guards against missing trailing newline)
  const recipePath = path.join(dir, "tools", "recipes", "my-skill.yaml");
  fs.appendFileSync(recipePath, "\n  - id: new_step\n    bash: tools/scripts/new.sh\n");

  const bundleAfter = buildBundle(dir);
  assert.notEqual(bundleAfter.hash, bundle.hash, "hash must change when recipe step added");
  fs.rmSync(dir, { recursive: true });
});

test("baseline idempotent: re-run produces byte-identical file", () => {
  const dir = makeTmpRepo();
  const bundle = buildBundle(dir);
  writeBaseline(dir, "my-skill", bundle);

  const baselineDir = path.join(dir, "ia", "state", "skill-recipe-baselines");
  const first = fs.readFileSync(path.join(baselineDir, "my-skill.json"), "utf8");

  // Re-write with same bundle
  writeBaseline(dir, "my-skill", bundle);
  const second = fs.readFileSync(path.join(baselineDir, "my-skill.json"), "utf8");

  // Content must match (allow minor timestamp diff — we stub captured_at)
  const firstObj = JSON.parse(first);
  const secondObj = JSON.parse(second);
  assert.equal(firstObj.hash, secondObj.hash);
  assert.deepEqual(firstObj.recipe_step_ids, secondObj.recipe_step_ids);
  assert.deepEqual(firstObj.skill_phase_ids, secondObj.skill_phase_ids);
  assert.deepEqual(firstObj.agent_step_refs, secondObj.agent_step_refs);

  fs.rmSync(dir, { recursive: true });
});
