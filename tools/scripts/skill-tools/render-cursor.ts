// Render .cursor/rules/cursor-skill-{slug}.mdc from canonical SKILL.md frontmatter.
// Source of truth for cursor wrapper output; legacy generate-cursor-skill-wrappers.mjs retired.

import { type SkillFrontmatter, collapseDescription } from "./frontmatter.js";

const FALLBACK_CALLER_AGENT_MAP = new Map<string, string>([
  ["master-plan-new", "master-plan-new"],
  ["stage-file", "stage-file"],
  ["project-new", "project-new"],
  ["stage-closeout-plan", "closeout"],
  ["plan-applier", "closeout"],
  ["ship", "ship"],
  ["ship-stage", "ship-stage"],
  ["release-rollout", "release-rollout"],
  ["release-rollout-track", "release-rollout-track"],
  ["project-spec-kickoff", "spec-kickoff"],
]);

function safeDescription(input: string): string {
  return input.replaceAll('"', '\\"').replace(/\s+/g, " ").trim();
}

export function renderCursor(fm: SkillFrontmatter): string {
  const description = safeDescription(collapseDescription(fm.description));
  const callerAgent = fm.caller_agent ?? FALLBACK_CALLER_AGENT_MAP.get(fm.name);
  const callerLine = callerAgent
    ? `When this skill invokes MCP mutation tools, pass \`caller_agent: "${callerAgent}"\`.`
    : "When this skill invokes MCP tools, follow its Tool recipe order exactly.";

  return `---
description: "${description}"
alwaysApply: false
---
Read and follow: @ia/skills/${fm.name}/SKILL.md

${callerLine}
If this skill is lifecycle-heavy, check: @.cursor/rules/cursor-lifecycle-adapters.mdc
`;
}
