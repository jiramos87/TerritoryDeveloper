// Render .claude/commands/{slug}.md from canonical SKILL.md frontmatter.
// Body override: if `ia/skills/{slug}/command-body.md` exists, append after
// the auto-generated header block; otherwise emit a default dispatch tail.

import fs from "node:fs";
import path from "node:path";
import { type SkillFrontmatter, REPO_ROOT, collapseDescription } from "./frontmatter.js";

const BODY_OVERRIDE_MARKER = "<!-- skill-tools:body-override -->";

export function renderCommand(fm: SkillFrontmatter): string {
  const description = collapseDescription(fm.description);
  const argHint = fm.argument_hint ?? "";
  const exceptions = fm.caveman_exceptions.join(", ");
  const triggerLines = fm.triggers.map((t) => `- ${t}`).join("\n");
  const purposeLine = collapseDescription(fm.purpose);

  const header = [
    "---",
    `description: ${description}`,
    `argument-hint: "${argHint}"`,
    "---",
    "",
    `# /${fm.name} — ${purposeLine}`,
    "",
    `Drive \`$ARGUMENTS\` via the [\`${fm.name}\`](../agents/${fm.name}.md) subagent.`,
    "",
    `Follow \`caveman:caveman\` for all output. Standard exceptions: ${exceptions}. Anchor: \`ia/rules/agent-output-caveman.md\`.`,
    "",
    "## Triggers",
    "",
    triggerLines,
    "",
  ].join("\n");

  const overridePath = path.join(REPO_ROOT, "ia", "skills", fm.name, "command-body.md");
  if (fs.existsSync(overridePath)) {
    const overrideText = fs.readFileSync(overridePath, "utf8").trimEnd();
    return `${header}${BODY_OVERRIDE_MARKER}\n\n${overrideText}\n`;
  }

  const defaultTail = [
    "## Dispatch",
    "",
    `Single Agent invocation with \`subagent_type: "${fm.name}"\` carrying \`$ARGUMENTS\` verbatim.`,
    "",
    "## Hard boundaries",
    "",
    `See [\`ia/skills/${fm.name}/SKILL.md\`](../../ia/skills/${fm.name}/SKILL.md) §Hard boundaries.`,
    "",
  ].join("\n");

  return `${header}${defaultTail}`;
}

export const COMMAND_BODY_OVERRIDE_MARKER = BODY_OVERRIDE_MARKER;
