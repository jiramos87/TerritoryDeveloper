// Render .claude/agents/{slug}.md from canonical SKILL.md frontmatter.
// Body override pattern: ia/skills/{slug}/agent-body.md when present.

import fs from "node:fs";
import path from "node:path";
import { type SkillFrontmatter, REPO_ROOT, collapseDescription } from "./frontmatter.js";
import { resolveTools } from "./tool-roles.js";

const BODY_OVERRIDE_MARKER = "<!-- skill-tools:body-override -->";

export function renderAgent(fm: SkillFrontmatter): string {
  const description = collapseDescription(fm.description);
  const tools = resolveTools(fm.tools_role, fm.tools_extra);
  if (tools.length === 0) {
    throw new Error(`render-agent: empty tools list for ${fm.name} (tools_role=${fm.tools_role})`);
  }
  const exceptions = fm.caveman_exceptions.join(", ");
  const phaseLines = fm.phases.map((p, idx) => `${idx + 1}. ${p}`).join("\n");
  const boundaryLines =
    fm.hard_boundaries.length > 0
      ? fm.hard_boundaries.map((b) => `- ${b}`).join("\n")
      : "- See SKILL.md §Hard boundaries.";

  const frontmatterLines = [
    "---",
    `name: ${fm.name}`,
    `description: ${description}`,
    `tools: ${tools.join(", ")}`,
    `model: ${fm.model ?? "sonnet"}`,
  ];
  if (fm.reasoning_effort) {
    frontmatterLines.push(`reasoning_effort: ${fm.reasoning_effort}`);
  }
  frontmatterLines.push("---", "");

  const header = [
    ...frontmatterLines,
    "## Stable prefix (Tier 1 cache)",
    "",
    "> `cache_control: {\"type\":\"ephemeral\",\"ttl\":\"1h\"}` — per `docs/prompt-caching-mechanics.md` §3 Tier 1.",
    "",
    "@ia/skills/_preamble/stable-block.md",
    "",
    `Follow \`caveman:caveman\` for all responses. Standard exceptions: ${exceptions}. Anchor: \`ia/rules/agent-output-caveman.md\`.`,
    "",
    "@.claude/agents/_preamble/agent-boot.md",
    "",
  ].join("\n");

  const overridePath = path.join(REPO_ROOT, "ia", "skills", fm.name, "agent-body.md");
  if (fs.existsSync(overridePath)) {
    const overrideText = fs.readFileSync(overridePath, "utf8").trimEnd();
    return `${header}${BODY_OVERRIDE_MARKER}\n\n${overrideText}\n`;
  }

  const defaultBody = [
    "# Mission",
    "",
    `Run [\`ia/skills/${fm.name}/SKILL.md\`](../../ia/skills/${fm.name}/SKILL.md) end-to-end for \`$ARGUMENTS\`. ${collapseDescription(fm.purpose)}`,
    "",
    "# Recipe",
    "",
    `Follow \`ia/skills/${fm.name}/SKILL.md\` end-to-end. Phase sequence:`,
    "",
    phaseLines,
    "",
    "# Hard boundaries",
    "",
    boundaryLines,
    "",
    `See [\`ia/skills/${fm.name}/SKILL.md\`](../../ia/skills/${fm.name}/SKILL.md) §Hard boundaries for full constraints.`,
    "",
  ].join("\n");

  return `${header}${defaultBody}`;
}

export const AGENT_BODY_OVERRIDE_MARKER = BODY_OVERRIDE_MARKER;
