#!/usr/bin/env node

import { promises as fs } from "node:fs";
import path from "node:path";

const repoRoot = process.cwd();
const skillsRoot = path.join(repoRoot, "ia", "skills");
const rulesRoot = path.join(repoRoot, ".cursor", "rules");

const callerAgentMap = new Map([
  ["master-plan-new", "master-plan-new"],
  ["stage-file", "stage-file"],
  ["project-new", "project-new"],
  ["stage-closeout-plan", "closeout"],
  ["plan-applier", "closeout"],
  ["ship-stage", "ship-stage"],
  ["release-rollout", "release-rollout"],
  ["release-rollout-track", "release-rollout-track"],
  ["project-spec-kickoff", "spec-kickoff"],
]);

const safeDescription = (input) =>
  input.replaceAll('"', '\\"').replace(/\s+/g, " ").trim();

function parseFrontmatter(fileText, filePath) {
  const match = fileText.match(/^---\n([\s\S]*?)\n---\n/);
  if (!match) {
    throw new Error(`Missing frontmatter in ${filePath}`);
  }

  const block = match[1];
  const nameMatch = block.match(/^name:\s*(.+)$/m);
  const descriptionMatch = block.match(/^description:\s*(.+)$/m);
  if (!nameMatch || !descriptionMatch) {
    throw new Error(`Missing name/description frontmatter in ${filePath}`);
  }

  let descriptionRaw = descriptionMatch[1].trim();
  if (descriptionRaw === ">" || descriptionRaw === "|" || descriptionRaw === ">-" || descriptionRaw === "|-") {
    const lines = block.split("\n");
    const startIndex = lines.findIndex((line) => line.startsWith("description:"));
    const folded = [];
    for (let i = startIndex + 1; i < lines.length; i += 1) {
      const line = lines[i];
      if (/^[A-Za-z0-9_-]+:\s*/.test(line)) break;
      if (line.startsWith("  ")) {
        folded.push(line.trim());
        continue;
      }
      if (line.trim() === "") {
        folded.push("");
        continue;
      }
      break;
    }
    descriptionRaw = folded.join(" ").replace(/\s+/g, " ").trim();
  }

  return {
    name: nameMatch[1].trim().replace(/^["']|["']$/g, ""),
    description: descriptionRaw.replace(/^["']|["']$/g, ""),
  };
}

function wrapperContent(skillName, description) {
  const callerAgent = callerAgentMap.get(skillName);
  const callerLine = callerAgent
    ? `When this skill invokes MCP mutation tools, pass \`caller_agent: "${callerAgent}"\`.`
    : "When this skill invokes MCP tools, follow its Tool recipe order exactly.";

  return `---
description: "${safeDescription(description)}"
alwaysApply: false
---
Read and follow: @ia/skills/${skillName}/SKILL.md

${callerLine}
If this skill is lifecycle-heavy, check: @.cursor/rules/cursor-lifecycle-adapters.mdc
`;
}

async function main() {
  await fs.mkdir(rulesRoot, { recursive: true });
  const entries = await fs.readdir(skillsRoot, { withFileTypes: true });
  const dirs = entries.filter((entry) => entry.isDirectory()).map((entry) => entry.name).sort();

  let written = 0;
  for (const dirName of dirs) {
    const skillPath = path.join(skillsRoot, dirName, "SKILL.md");
    try {
      const text = await fs.readFile(skillPath, "utf8");
      const { name, description } = parseFrontmatter(text, skillPath);
      const fileName = `cursor-skill-${name}.mdc`;
      const outPath = path.join(rulesRoot, fileName);
      await fs.writeFile(outPath, wrapperContent(name, description), "utf8");
      written += 1;
    } catch (error) {
      if (error.code === "ENOENT") {
        continue;
      }
      throw error;
    }
  }

  console.log(`Generated ${written} Cursor skill wrapper rules in .cursor/rules`);
}

main().catch((error) => {
  console.error(error.message);
  process.exit(1);
});
