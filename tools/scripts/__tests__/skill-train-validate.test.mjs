/**
 * skill-train-validate.test.mjs
 *
 * atomize_file_skill_frontmatter_validates:
 *   Asserts atomize-file SKILL.md frontmatter passes the skill-drift validator
 *   and contains required phases + sub-stage threshold table.
 */

import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { execSync } from "node:child_process";
import { readFileSync, existsSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const REPO_ROOT = resolve(__dirname, "../../..");

describe("atomize_file_skill_frontmatter_validates", () => {
  it("SKILL.md file exists with required frontmatter fields", () => {
    const skillPath = resolve(REPO_ROOT, "ia/skills/atomize-file/SKILL.md");
    assert.ok(existsSync(skillPath), `atomize-file SKILL.md not found at ${skillPath}`);

    const content = readFileSync(skillPath, "utf8");
    assert.match(content, /^---\n/, "SKILL.md must start with frontmatter delimiter");
    assert.match(content, /\nname:\s+atomize-file/, "Must have name: atomize-file");
    assert.match(content, /\npurpose:/, "Must have purpose field");
    assert.match(content, /\nphases:/, "Must have phases field");
    assert.match(content, /\ntriggers:/, "Must have triggers field");
    assert.match(content, /\nmodel:/, "Must have model field");
  });

  it("SKILL.md body contains sub-stage threshold table", () => {
    const skillPath = resolve(REPO_ROOT, "ia/skills/atomize-file/SKILL.md");
    const content = readFileSync(skillPath, "utf8");
    assert.match(content, /§Sub-stage decomposition threshold table/, "Must have sub-stage threshold table section");
    assert.match(content, /2500/, "Must reference 2500 LOC threshold");
    assert.match(content, /3500/, "Must reference 3500 LOC threshold");
  });

  it("generated agent stub exists (.claude/agents/atomize-file.md)", () => {
    const agentPath = resolve(REPO_ROOT, ".claude/agents/atomize-file.md");
    assert.ok(existsSync(agentPath), `Agent stub not found at ${agentPath} — run 'npm run skill:sync:all'`);
  });

  it("generated command stub exists (.claude/commands/atomize-file.md)", () => {
    const commandPath = resolve(REPO_ROOT, ".claude/commands/atomize-file.md");
    assert.ok(existsSync(commandPath), `Command stub not found at ${commandPath} — run 'npm run skill:sync:all'`);
  });

  it("skill-drift validator exits 0 (no drift)", () => {
    try {
      execSync("npm run validate:skill-drift", {
        cwd: REPO_ROOT,
        encoding: "utf8",
        stdio: ["pipe", "pipe", "pipe"],
      });
    } catch (e) {
      const out = (e.stdout ?? "") + (e.stderr ?? "");
      assert.fail(`validate:skill-drift failed:\n${out}`);
    }
  });
});
