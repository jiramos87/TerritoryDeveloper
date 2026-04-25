// Batch migrator: backfill canonical SKILL.md frontmatter + extract body partials
// from existing .claude/agents/{slug}.md + .claude/commands/{slug}.md.
//
// Idempotent: rerunning on a fully-migrated skill is a no-op (existing fields kept).
// Per skill, writes (when applicable):
//   - ia/skills/{slug}/SKILL.md            (frontmatter rewritten; body untouched)
//   - ia/skills/{slug}/agent-body.md       (when .claude/agents/{slug}.md exists)
//   - ia/skills/{slug}/command-body.md     (when .claude/commands/{slug}.md exists)
//
// Run downstream `npm run skill:sync:all` to regenerate the .claude/** wrappers.

import fs from "node:fs";
import path from "node:path";
import {
  REPO_ROOT,
  listSkillSlugs,
  splitFrontmatter,
  parseRawFrontmatter,
  DEFAULT_CAVEMAN_EXCEPTIONS,
} from "./frontmatter.js";
import { TOOL_ROLE_BASELINES } from "./tool-roles.js";

interface MigrationResult {
  slug: string;
  updated_skill: boolean;
  wrote_agent_body: boolean;
  wrote_command_body: boolean;
  notes: string[];
}

function readMaybe(p: string): string | null {
  return fs.existsSync(p) ? fs.readFileSync(p, "utf8") : null;
}

// Extract trigger array from a description string.
// Pattern hunted for: `Triggers — "a", "b", "c".`  OR  `Triggers: "a", "b"`
function extractTriggers(description: string): string[] {
  const re = /Triggers\s*[—:\-]\s*((?:"[^"]+"\s*,?\s*)+)/i;
  const match = description.match(re);
  if (!match) return [];
  const inner = match[1];
  const items = [...inner.matchAll(/"([^"]+)"/g)].map((m) => m[1].trim()).filter(Boolean);
  return items;
}

// Pull caveman_exceptions from existing agent body's "Standard exceptions:" line, if present.
// Splits on commas but respects nested parentheses + balanced quotes (treats them as atomic groups).
function extractCavemanExceptions(agentBody: string | null): string[] {
  if (!agentBody) return [...DEFAULT_CAVEMAN_EXCEPTIONS];
  // Match Standard exceptions: ... up to the first . that ends a sentence (not inside parens)
  const idx = agentBody.indexOf("Standard exceptions:");
  if (idx === -1) return [...DEFAULT_CAVEMAN_EXCEPTIONS];
  let i = idx + "Standard exceptions:".length;
  let depth = 0;
  let bt = false;
  let end = -1;
  while (i < agentBody.length) {
    const ch = agentBody[i];
    if (ch === "`") bt = !bt;
    else if (!bt && ch === "(") depth++;
    else if (!bt && ch === ")") depth--;
    else if (!bt && depth === 0 && (ch === "." || ch === "\n")) {
      end = i;
      break;
    }
    i++;
  }
  if (end === -1) end = agentBody.length;
  const segment = agentBody.slice(idx + "Standard exceptions:".length, end);

  const items: string[] = [];
  let buf = "";
  let d = 0;
  let inBt = false;
  for (const ch of segment) {
    if (ch === "`") inBt = !inBt;
    if (!inBt && ch === "(") d++;
    else if (!inBt && ch === ")") d--;
    if (!inBt && d === 0 && ch === ",") {
      items.push(buf);
      buf = "";
    } else {
      buf += ch;
    }
  }
  if (buf.trim()) items.push(buf);

  return items
    .map((s) => s.replace(/`/g, "").replace(/\*\*/g, "").replace(/\*/g, "").trim())
    .filter(Boolean);
}

// Pull hard_boundaries: parse # Hard boundaries section bullets if present.
function extractHardBoundaries(agentBody: string | null): string[] {
  if (!agentBody) return [];
  const re = /^#+\s+Hard boundaries\s*\n+([\s\S]*?)(?:\n#+\s|\n---|\n$)/m;
  const match = agentBody.match(re);
  if (!match) return [];
  const bullets = [...match[1].matchAll(/^[-*]\s+(.+?)$/gm)].map((m) => m[1].trim());
  // Trim each bullet to ≤120 chars; strip trailing markdown links / parentheticals
  return bullets
    .map((b) => b.replace(/\s+/g, " ").trim())
    .filter((b) => b.length > 0)
    .slice(0, 8);
}

// Parse tools list, model, reasoning_effort from existing agent file.
interface AgentMeta {
  tools: string[];
  model: string | null;
  reasoning_effort: string | null;
  body: string;
}

function parseAgent(agentText: string): AgentMeta {
  const { fmBlock, body } = splitFrontmatter(agentText);
  const lines = fmBlock.split("\n");
  let tools: string[] = [];
  let model: string | null = null;
  let reasoning_effort: string | null = null;

  for (const line of lines) {
    const m = line.match(/^([A-Za-z_][A-Za-z0-9_-]*):\s*(.*)$/);
    if (!m) continue;
    const [, key, rest] = m;
    if (key === "tools") {
      tools = rest
        .split(",")
        .map((t) => t.trim())
        .filter(Boolean);
    } else if (key === "model") {
      model = rest.trim();
    } else if (key === "reasoning_effort") {
      reasoning_effort = rest.trim();
    }
  }

  return { tools, model, reasoning_effort, body };
}

// Strip leading auto-header from agent body — find the agent-boot.md include line and
// take everything after it, trimming leading blanks.
function extractAgentBodyAfterBoot(rawAgentBody: string): string {
  const idx = rawAgentBody.indexOf("@.claude/agents/_preamble/agent-boot.md");
  if (idx === -1) {
    // Fallback: find first H1 (`# Mission` or similar)
    const h1 = rawAgentBody.match(/^#\s+/m);
    if (!h1 || h1.index === undefined) return rawAgentBody.trimStart();
    return rawAgentBody.slice(h1.index).trimStart();
  }
  // Skip past the import line
  const after = rawAgentBody.slice(idx);
  const newline = after.indexOf("\n");
  const tail = newline === -1 ? "" : after.slice(newline + 1);
  return tail.replace(/^\s*\n+/, "");
}

// Parse argument-hint from existing command frontmatter, plus body.
interface CommandMeta {
  argument_hint: string | null;
  body: string;
}

function parseCommand(commandText: string): CommandMeta {
  const { fmBlock, body } = splitFrontmatter(commandText);
  let argument_hint: string | null = null;
  for (const line of fmBlock.split("\n")) {
    const m = line.match(/^argument-hint:\s*"?([^"]*?)"?\s*$/);
    if (m) {
      argument_hint = m[1];
      break;
    }
  }
  return { argument_hint, body };
}

// Extract command body for body-override. Strategy:
//   - Take command body after frontmatter
//   - Strip a leading `# /{slug}` H1 if present (renderer emits its own)
//   - Strip leading "Drive `$ARGUMENTS` ... subagent." paragraph (renderer emits)
//   - Strip leading "## Triggers" + bullet list (renderer emits)
function extractCommandBody(rawCommandBody: string, slug: string): string {
  let body = rawCommandBody.trimStart();

  // Strip leading `# /slug ...` H1
  const h1Re = new RegExp(`^#\\s+/${slug}\\b[^\\n]*\\n+`);
  body = body.replace(h1Re, "");

  // Strip "Drive `$ARGUMENTS` ..." paragraph (one or two lines)
  body = body.replace(/^(Drive `\$ARGUMENTS`[^\n]*\n+)/, "");

  // Strip "Use `slug` subagent" intro paragraph
  const useRe = new RegExp(`^(Use \`${slug}\`[^\\n]*(\\n[^\\n]+)*\\n+)`);
  body = body.replace(useRe, "");

  // Strip leading caveman line if it parrots auto-line
  body = body.replace(/^(Follow `caveman:caveman`[^\n]*\n+)/, "");

  // Strip leading "## Triggers" section + bullet list
  body = body.replace(/^##\s+Triggers\s*\n+(?:[-*]\s+[^\n]+\n+)*\n*/, "");

  return body.trimStart();
}

// Compute best tools_role + tools_extra for an agent's tools list.
// Strategy: best-fit by Jaccard similarity (|baseline ∩ tools| / |baseline ∪ tools|).
// Tie-break: prefer larger overlap, then larger baseline. Lint surfaces missing-baseline gaps.
// Falls back to "custom" only when no role overlaps meaningfully (overlap < 2).
function inferToolsRole(agentTools: string[]): { tools_role: string; tools_extra: string[] } {
  const toolSet = new Set(agentTools);
  let bestRole = "custom";
  let bestScore = -1;
  let bestOverlap = 0;
  let bestBaselineSize = 0;

  for (const [role, baseline] of Object.entries(TOOL_ROLE_BASELINES)) {
    if (role === "custom") continue;
    if (baseline.length === 0) continue;
    const overlap = baseline.filter((t) => toolSet.has(t)).length;
    const union = baseline.length + agentTools.length - overlap;
    const jaccard = overlap / union;
    // Score with bonus for larger baseline overlap (more specific roles)
    const score = jaccard * 100 + overlap;
    if (score > bestScore) {
      bestScore = score;
      bestRole = role;
      bestOverlap = overlap;
      bestBaselineSize = baseline.length;
    }
  }

  // Reject when overlap ≤1 — too weak; prefer custom + full extras
  if (bestOverlap <= 1) {
    return { tools_role: "custom", tools_extra: [...agentTools] };
  }

  const baseline = TOOL_ROLE_BASELINES[bestRole] ?? [];
  const baselineSet = new Set(baseline);
  const extra = agentTools.filter((t) => !baselineSet.has(t));
  return { tools_role: bestRole, tools_extra: extra };
}

// Render canonical frontmatter block (string) preserving the original key order
// where possible while injecting new canonical fields if absent.
function renderFrontmatter(fields: Record<string, unknown>): string {
  const ORDER = [
    "name",
    "purpose",
    "audience",
    "loaded_by",
    "slices_via",
    "description",
    "phases",
    "triggers",
    "argument_hint",
    "model",
    "reasoning_effort",
    "tools_role",
    "tools_extra",
    "caveman_exceptions",
    "hard_boundaries",
    "caller_agent",
  ];

  const lines: string[] = [];
  const keys = ORDER.filter((k) => fields[k] !== undefined && fields[k] !== null);
  for (const key of keys) {
    const value = fields[key];
    if (typeof value === "string") {
      lines.push(formatScalar(key, value));
    } else if (Array.isArray(value)) {
      if (value.length === 0) {
        lines.push(`${key}: []`);
      } else {
        lines.push(`${key}:`);
        for (const item of value) {
          lines.push(`  - ${quoteIfNeeded(String(item))}`);
        }
      }
    }
  }
  return lines.join("\n");
}

function formatScalar(key: string, value: string): string {
  // Multi-line: use folded block scalar `>-`
  if (value.includes("\n") || value.length > 100) {
    const wrapped = wrapTextLines(value, 100).map((l) => `  ${l}`).join("\n");
    return `${key}: >-\n${wrapped}`;
  }
  // Quote if contains special chars or starts/ends with whitespace
  return `${key}: ${quoteIfNeeded(value)}`;
}

function quoteIfNeeded(value: string): string {
  // Always quote: contains :, #, [, ], {, }, ', ", or starts with - or special
  if (/^[-:?@`!|*&%>]/.test(value)) return `"${value.replace(/"/g, '\\"')}"`;
  if (/[:#]/.test(value) && !value.startsWith('"')) return `"${value.replace(/"/g, '\\"')}"`;
  return value;
}

function wrapTextLines(text: string, maxLen: number): string[] {
  const collapsed = text.replace(/\s+/g, " ").trim();
  const words = collapsed.split(" ");
  const lines: string[] = [];
  let current = "";
  for (const word of words) {
    if (current.length + word.length + 1 > maxLen) {
      lines.push(current);
      current = word;
    } else {
      current = current ? `${current} ${word}` : word;
    }
  }
  if (current) lines.push(current);
  return lines;
}

// Per-skill migrator
function migrateSkill(slug: string): MigrationResult {
  const result: MigrationResult = {
    slug,
    updated_skill: false,
    wrote_agent_body: false,
    wrote_command_body: false,
    notes: [],
  };

  const skillDir = path.join(REPO_ROOT, "ia", "skills", slug);
  const skillPath = path.join(skillDir, "SKILL.md");
  const agentPath = path.join(REPO_ROOT, ".claude", "agents", `${slug}.md`);
  const commandPath = path.join(REPO_ROOT, ".claude", "commands", `${slug}.md`);

  const skillText = fs.readFileSync(skillPath, "utf8");
  const { fmBlock, body: skillBody } = splitFrontmatter(skillText);
  const skillFm = parseRawFrontmatter(fmBlock);

  const agentText = readMaybe(agentPath);
  const commandText = readMaybe(commandPath);
  const hasAgent = !!agentText;
  const hasCommand = !!commandText;

  let agentMeta: AgentMeta | null = null;
  if (agentText) {
    agentMeta = parseAgent(agentText);
  }
  let commandMeta: CommandMeta | null = null;
  if (commandText) {
    commandMeta = parseCommand(commandText);
  }

  // ----- Build canonical frontmatter -----
  const fields: Record<string, unknown> = { ...skillFm };

  // Ensure required base fields exist (preserve existing values)
  fields.name = fields.name ?? slug;
  fields.audience = fields.audience ?? "agent";
  fields.loaded_by = fields.loaded_by ?? `skill:${slug}`;
  fields.slices_via = fields.slices_via ?? "none";

  if (!fields.description) {
    // Fall back to agent description
    if (agentText) {
      const m = agentText.match(/^description:\s*(.+(?:\n {2,}.+)*)/m);
      if (m) fields.description = m[1].replace(/\s+/g, " ").trim();
    }
  }
  if (!fields.purpose) {
    fields.purpose = collapseAndShorten(String(fields.description ?? ""), 200);
  }

  if (!Array.isArray(fields.phases)) {
    fields.phases = (fields.phases as string[] | undefined) ?? [];
  }

  // Triggers
  if (!Array.isArray(fields.triggers) || (fields.triggers as string[]).length === 0) {
    const fromDescription = extractTriggers(String(fields.description ?? ""));
    fields.triggers = fromDescription;
  }

  // argument_hint
  if (!fields.argument_hint && commandMeta?.argument_hint) {
    fields.argument_hint = commandMeta.argument_hint;
  }

  // model + reasoning_effort
  if (!fields.model && agentMeta?.model) {
    fields.model = agentMeta.model;
  } else if (!fields.model) {
    fields.model = "inherit";
  }
  if (!fields.reasoning_effort && agentMeta?.reasoning_effort) {
    fields.reasoning_effort = agentMeta.reasoning_effort;
  }

  // tools_role + tools_extra
  if (hasAgent && agentMeta) {
    if (!fields.tools_role || (fields.tools_role === "custom" && (fields.tools_extra as string[] | undefined ?? []).length === 0)) {
      const inferred = inferToolsRole(agentMeta.tools);
      fields.tools_role = inferred.tools_role;
      fields.tools_extra = inferred.tools_extra;
      result.notes.push(`tools_role=${inferred.tools_role} (extras=${inferred.tools_extra.length})`);
    }
  } else {
    fields.tools_role = fields.tools_role ?? "custom";
    fields.tools_extra = (fields.tools_extra as string[] | undefined) ?? [];
  }

  // caveman_exceptions + hard_boundaries
  const rawAgentBody = agentMeta?.body ?? null;
  if (!Array.isArray(fields.caveman_exceptions) || (fields.caveman_exceptions as string[]).length === 0) {
    fields.caveman_exceptions = extractCavemanExceptions(rawAgentBody);
  }
  if (!Array.isArray(fields.hard_boundaries) || (fields.hard_boundaries as string[]).length === 0) {
    fields.hard_boundaries = extractHardBoundaries(rawAgentBody);
  }

  // caller_agent — unset unless skill matches name (default for skills with own subagent)
  if (!fields.caller_agent && hasAgent) {
    fields.caller_agent = slug;
  }

  // ----- Write SKILL.md if frontmatter changed -----
  const renderedFm = renderFrontmatter(fields);
  const newSkillText = `---\n${renderedFm}\n---\n${skillBody}`;
  if (newSkillText !== skillText) {
    fs.writeFileSync(skillPath, newSkillText);
    result.updated_skill = true;
  }

  // ----- Write agent-body.md (if agent surface) -----
  if (rawAgentBody) {
    const bodyContent = extractAgentBodyAfterBoot(rawAgentBody);
    const bodyPath = path.join(skillDir, "agent-body.md");
    const existing = readMaybe(bodyPath);
    const newBody = bodyContent.endsWith("\n") ? bodyContent : `${bodyContent}\n`;
    if (existing !== newBody) {
      fs.writeFileSync(bodyPath, newBody);
      result.wrote_agent_body = true;
    }
  }

  // ----- Write command-body.md (if command surface) -----
  if (commandMeta) {
    const bodyContent = extractCommandBody(commandMeta.body, slug);
    if (bodyContent.trim().length > 0) {
      const bodyPath = path.join(skillDir, "command-body.md");
      const existing = readMaybe(bodyPath);
      const newBody = bodyContent.endsWith("\n") ? bodyContent : `${bodyContent}\n`;
      if (existing !== newBody) {
        fs.writeFileSync(bodyPath, newBody);
        result.wrote_command_body = true;
      }
    }
  }

  return result;
}

function collapseAndShorten(text: string, maxLen: number): string {
  const collapsed = text.replace(/\s+/g, " ").trim();
  if (collapsed.length <= maxLen) return collapsed;
  return collapsed.slice(0, maxLen - 1).replace(/\s+\S*$/, "") + "…";
}

// CLI entry
function main(): void {
  const args = process.argv.slice(2);
  const slugFilter = args.find((a) => !a.startsWith("--"));
  const slugs = slugFilter ? [slugFilter] : listSkillSlugs();

  const results: MigrationResult[] = [];
  for (const slug of slugs) {
    try {
      results.push(migrateSkill(slug));
    } catch (err) {
      console.error(`migrate ${slug}: ${(err as Error).message}`);
    }
  }

  const updated = results.filter((r) => r.updated_skill).length;
  const wroteAgent = results.filter((r) => r.wrote_agent_body).length;
  const wroteCmd = results.filter((r) => r.wrote_command_body).length;

  for (const r of results) {
    if (r.updated_skill || r.wrote_agent_body || r.wrote_command_body) {
      const tags = [
        r.updated_skill ? "skill" : "",
        r.wrote_agent_body ? "agent-body" : "",
        r.wrote_command_body ? "cmd-body" : "",
      ]
        .filter(Boolean)
        .join("+");
      console.log(`  ${r.slug.padEnd(40)} ${tags}${r.notes.length ? ` (${r.notes.join("; ")})` : ""}`);
    }
  }
  console.log(
    `migrate — ${slugs.length} skill(s); skill_updates=${updated} agent_bodies=${wroteAgent} command_bodies=${wroteCmd}`
  );
}

main();
