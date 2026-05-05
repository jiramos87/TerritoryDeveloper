// SKILL.md frontmatter parser + canonical schema validator. Zero deps.
// Matches existing tools/scripts/*.mjs hand-rolled YAML convention.

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
export const REPO_ROOT = path.resolve(__dirname, "..", "..", "..");

// ---------------------------------------------------------------------------
// Canonical SKILL.md frontmatter shape
// ---------------------------------------------------------------------------

export interface SkillFrontmatter {
  name: string;
  purpose: string;
  audience: "agent" | "human" | "both";
  loaded_by: string;
  slices_via: string;
  description: string;
  phases: string[];

  triggers: string[];
  argument_hint?: string;
  model?: "opus" | "sonnet" | "haiku" | "inherit";
  reasoning_effort?: "low" | "medium" | "high";
  input_token_budget?: number;
  pre_split_threshold?: number;
  tools_role: string;
  tools_extra: string[];
  caveman_exceptions: string[];
  hard_boundaries: string[];
  caller_agent?: string;
}

export const DEFAULT_CAVEMAN_EXCEPTIONS = [
  "code",
  "commits",
  "security/auth",
  "verbatim error/tool output",
  "structured MCP payloads",
];

// ---------------------------------------------------------------------------
// Raw frontmatter parser (block scalar + sequence support, no anchors)
// ---------------------------------------------------------------------------

export interface RawFrontmatter {
  raw: Record<string, unknown>;
  body: string;
}

export function splitFrontmatter(text: string): { fmBlock: string; body: string } {
  const match = text.match(/^---\n([\s\S]*?)\n---\n?/);
  if (!match) {
    throw new Error("Missing frontmatter block");
  }
  const body = text.slice(match[0].length);
  return { fmBlock: match[1], body };
}

// Minimal YAML parser — supports:
//   key: scalar
//   key: "quoted scalar"
//   key: >-
//     folded multi-line
//     value
//   key:
//     - list item
//     - "quoted item"
//   nothing else (no nested maps, no anchors, no flow style)
export function parseRawFrontmatter(fmBlock: string): Record<string, unknown> {
  const lines = fmBlock.split("\n");
  const result: Record<string, unknown> = {};
  let i = 0;

  while (i < lines.length) {
    const line = lines[i];
    if (line.trim() === "" || line.trim().startsWith("#")) {
      i += 1;
      continue;
    }

    const keyMatch = line.match(/^([A-Za-z_][A-Za-z0-9_-]*):\s*(.*)$/);
    if (!keyMatch) {
      i += 1;
      continue;
    }

    const key = keyMatch[1];
    const rest = keyMatch[2];

    // Block scalar (folded > / literal |, with optional - chomp)
    if (/^[>|][-+]?\s*$/.test(rest)) {
      const folded = rest.startsWith(">");
      const buf: string[] = [];
      i += 1;
      while (i < lines.length) {
        const next = lines[i];
        if (next.startsWith("  ")) {
          buf.push(next.slice(2));
          i += 1;
          continue;
        }
        if (next.trim() === "") {
          buf.push("");
          i += 1;
          continue;
        }
        break;
      }
      // Trim trailing empty lines
      while (buf.length > 0 && buf[buf.length - 1] === "") buf.pop();
      result[key] = folded ? buf.join(" ").replace(/\s+/g, " ").trim() : buf.join("\n");
      continue;
    }

    // Sequence (list of items on indented lines starting with `- `)
    if (rest === "") {
      const items: string[] = [];
      i += 1;
      while (i < lines.length) {
        const next = lines[i];
        const itemMatch = next.match(/^\s+-\s+(.+)$/);
        if (!itemMatch) break;
        items.push(unquote(itemMatch[1]));
        i += 1;
      }
      result[key] = items;
      continue;
    }

    // Inline flow-style array: key: [a, b, c]
    if (rest.startsWith("[") && rest.endsWith("]")) {
      const inner = rest.slice(1, -1).trim();
      if (inner === "") {
        result[key] = [];
      } else {
        result[key] = inner.split(",").map((s) => unquote(s.trim()));
      }
      i += 1;
      continue;
    }

    // Inline scalar
    result[key] = unquote(rest);
    i += 1;
  }

  return result;
}

function unquote(value: string): string {
  const v = value.trim();
  if ((v.startsWith('"') && v.endsWith('"')) || (v.startsWith("'") && v.endsWith("'"))) {
    return v.slice(1, -1);
  }
  return v;
}

// ---------------------------------------------------------------------------
// Validation
// ---------------------------------------------------------------------------

const VALID_TOOL_ROLES = new Set([
  "standalone-pipeline",
  "stage-pipeline",
  "pair-head",
  "pair-tail",
  "planner",
  "implementer",
  "validator",
  "lifecycle-helper",
  "custom",
]);

const VALID_AUDIENCES = new Set(["agent", "human", "both"]);
const VALID_MODELS = new Set(["opus", "sonnet", "haiku", "inherit"]);
const VALID_REASONING = new Set(["low", "medium", "high"]);

export function validateFrontmatter(raw: Record<string, unknown>): SkillFrontmatter {
  const errors: string[] = [];

  const requireString = (key: string): string => {
    const v = raw[key];
    if (typeof v !== "string" || v.length === 0) {
      errors.push(`${key}: required string`);
      return "";
    }
    return v;
  };

  const optionalString = (key: string): string | undefined => {
    const v = raw[key];
    if (v === undefined) return undefined;
    if (typeof v !== "string") {
      errors.push(`${key}: must be string`);
      return undefined;
    }
    return v;
  };

  const requireStringArray = (key: string, fallback: string[] = []): string[] => {
    const v = raw[key];
    if (v === undefined) return fallback;
    if (!Array.isArray(v) || !v.every((x) => typeof x === "string")) {
      errors.push(`${key}: must be string array`);
      return fallback;
    }
    return v as string[];
  };

  const name = requireString("name");
  if (name && !/^[a-z0-9-]+$/.test(name)) {
    errors.push(`name: must match /^[a-z0-9-]+$/, got "${name}"`);
  }

  // purpose / audience optional in MVP (warning surfaces in lint findings)
  const purpose = optionalString("purpose") ?? "";
  const audience = optionalString("audience") ?? "agent";
  if (audience && !VALID_AUDIENCES.has(audience)) {
    errors.push(`audience: must be agent|human|both`);
  }

  const description = requireString("description");
  if (description.length < 40) {
    errors.push(`description: too short (min 40 chars)`);
  }

  // phases optional in MVP — promoted to required at Phase 3 lockdown
  const phases = requireStringArray("phases", []);

  // New canonical fields — optional in MVP (warning), required in lockdown
  const triggers = requireStringArray("triggers", []);
  const tools_role = optionalString("tools_role") ?? "custom";
  if (!VALID_TOOL_ROLES.has(tools_role)) {
    errors.push(`tools_role: invalid value "${tools_role}"`);
  }
  const tools_extra = requireStringArray("tools_extra", []);
  const caveman_exceptions = requireStringArray("caveman_exceptions", DEFAULT_CAVEMAN_EXCEPTIONS);
  const hard_boundaries = requireStringArray("hard_boundaries", []);

  const model = optionalString("model");
  if (model && !VALID_MODELS.has(model)) {
    errors.push(`model: invalid "${model}"`);
  }
  const reasoning_effort = optionalString("reasoning_effort");
  if (reasoning_effort && !VALID_REASONING.has(reasoning_effort)) {
    errors.push(`reasoning_effort: invalid "${reasoning_effort}"`);
  }

  const optionalPositiveInt = (key: string): number | undefined => {
    const v = raw[key];
    if (v === undefined) return undefined;
    const n = Number(v);
    if (!Number.isInteger(n) || n <= 0) {
      errors.push(`${key}: must be positive integer`);
      return undefined;
    }
    return n;
  };

  const input_token_budget = optionalPositiveInt("input_token_budget");
  const pre_split_threshold = optionalPositiveInt("pre_split_threshold");

  if (errors.length > 0) {
    throw new Error(`Frontmatter validation failed:\n  - ${errors.join("\n  - ")}`);
  }

  return {
    name,
    purpose,
    audience: audience as SkillFrontmatter["audience"],
    loaded_by: optionalString("loaded_by") ?? "",
    slices_via: optionalString("slices_via") ?? "",
    description,
    phases,
    triggers,
    argument_hint: optionalString("argument_hint"),
    model: model as SkillFrontmatter["model"] | undefined,
    reasoning_effort: reasoning_effort as SkillFrontmatter["reasoning_effort"] | undefined,
    input_token_budget,
    pre_split_threshold,
    tools_role,
    tools_extra,
    caveman_exceptions,
    hard_boundaries,
    caller_agent: optionalString("caller_agent"),
  };
}

// ---------------------------------------------------------------------------
// File I/O
// ---------------------------------------------------------------------------

export function readSkillFrontmatter(slug: string): SkillFrontmatter {
  const skillPath = path.join(REPO_ROOT, "ia", "skills", slug, "SKILL.md");
  const text = fs.readFileSync(skillPath, "utf8");
  const { fmBlock } = splitFrontmatter(text);
  const raw = parseRawFrontmatter(fmBlock);
  return validateFrontmatter(raw);
}

export function listSkillSlugs(): string[] {
  const skillsDir = path.join(REPO_ROOT, "ia", "skills");
  return fs
    .readdirSync(skillsDir, { withFileTypes: true })
    .filter((e) => e.isDirectory() && !e.name.startsWith("_"))
    .map((e) => e.name)
    .filter((name) => fs.existsSync(path.join(skillsDir, name, "SKILL.md")))
    .sort();
}

export function collapseDescription(description: string): string {
  return description.replace(/\s+/g, " ").trim();
}
