/**
 * backlog-record-schema.ts
 *
 * Pure shared lint core for backlog yaml records.
 * No filesystem access, no process.exit, no I/O.
 *
 * Callers (validate-backlog-yaml.mjs, MCP tool backlog_record_validate) pass
 * raw yaml body; this module returns { ok, errors, warnings }.
 * Error strings use "{rule}: {detail}" format — caller prepends file/id context.
 */

// ---------------------------------------------------------------------------
// Rule-id constants
// ---------------------------------------------------------------------------

export const E_MISSING_FIELD = "missing_required_field";
export const E_BAD_ID_FORMAT = "bad_id_format";
export const E_BAD_STATUS = "bad_status";
export const E_EMPTY_DEPENDS_ON_RAW = "empty_depends_on_raw";
export const E_BAD_TASK_KEY_FORMAT = "bad_task_key_format";
export const E_BAD_LOCATOR_ARRAY_TYPE = "bad_locator_array_type";
export const E_EMPTY_PARENT_PLAN = "empty_parent_plan";

// ---------------------------------------------------------------------------
// Minimal YAML scalar parser (schema-aware; matches emitter in
// tools/migrate-backlog-to-yaml.mjs and backlog-yaml-loader.ts)
// ---------------------------------------------------------------------------

function unquote(s: string): string {
  const t = s.trim();
  if (
    (t.startsWith('"') && t.endsWith('"')) ||
    (t.startsWith("'") && t.endsWith("'"))
  ) {
    return t
      .slice(1, -1)
      .replace(/\\n/g, "\n")
      .replace(/\\"/g, '"')
      .replace(/\\\\/g, "\\");
  }
  return t;
}

export interface ParsedYamlScalars {
  id?: string;
  type?: string;
  title?: string;
  status?: string;
  section?: string;
  priority?: string;
  spec?: string;
  notes?: string;
  acceptance?: string;
  depends_on?: string[];
  depends_on_raw?: string;
  related?: string[];
  created?: string;
  // Schema-v2 locator fields
  parent_plan?: string;
  task_key?: string;
  surfaces?: string[];
  mcp_slices?: string[];
  skill_hints?: string[];
  [key: string]: unknown;
}

/**
 * Minimal yaml key-value parser — same logic as validate-backlog-yaml.mjs
 * and backlog-yaml-loader.ts, consolidated here as the single canonical parser.
 */
export function parseYamlScalars(content: string): ParsedYamlScalars {
  const lines = content.split("\n");
  const obj: Record<string, unknown> = {};
  let i = 0;

  while (i < lines.length) {
    const line = lines[i]!;
    if (!line || line.startsWith("#")) { i++; continue; }

    const colonIdx = line.indexOf(": ");
    if (colonIdx < 0) {
      // Bare-colon key (block value follows)
      const bareColon = line.indexOf(":");
      if (bareColon >= 0 && bareColon === line.length - 1) {
        const key = line.slice(0, bareColon).trim();
        i++;
        if (i < lines.length && (lines[i] === "[]" || lines[i]!.startsWith("  - "))) {
          const items: string[] = [];
          while (i < lines.length && lines[i]!.startsWith("  - ")) {
            items.push(unquote(lines[i]!.slice(4)));
            i++;
          }
          obj[key] = items;
        } else {
          obj[key] = "";
        }
      } else {
        i++;
      }
      continue;
    }

    const key = line.slice(0, colonIdx).trim();
    const rawVal = line.slice(colonIdx + 2);

    if (rawVal === "|") {
      i++;
      const blockLines: string[] = [];
      while (i < lines.length && (lines[i]!.startsWith("  ") || lines[i] === "")) {
        blockLines.push(lines[i]!.startsWith("  ") ? lines[i]!.slice(2) : "");
        i++;
      }
      while (blockLines.length > 0 && !blockLines[blockLines.length - 1]) blockLines.pop();
      obj[key] = blockLines.join("\n");
      continue;
    }

    if (rawVal === "[]") {
      obj[key] = [];
    } else if (rawVal.trimStart() === "") {
      i++;
      if (i < lines.length && lines[i]!.trim().startsWith("- ")) {
        const items: string[] = [];
        while (i < lines.length && lines[i]!.trim().startsWith("- ")) {
          items.push(unquote(lines[i]!.trim().slice(2)));
          i++;
        }
        obj[key] = items;
      } else {
        obj[key] = "";
      }
      continue;
    } else {
      obj[key] = unquote(rawVal);
    }

    i++;
  }

  return obj as ParsedYamlScalars;
}

// ---------------------------------------------------------------------------
// Validation constants
// ---------------------------------------------------------------------------

const VALID_STATUS = new Set(["open", "closed"]);
const ID_RE = /^(TECH|FEAT|BUG|ART|AUDIO)-\d+[a-z]?$/;
const TASK_KEY_RE = /^T\d+\.\d+(\.\d+)?$/;
const LOCATOR_ARRAY_FIELDS = ["surfaces", "mcp_slices", "skill_hints"] as const;

/** Fields required in every record (open + closed). */
const REQUIRED_ALL = ["id", "type", "title", "status"] as const;
/** Fields required only in open records. */
const REQUIRED_OPEN_ONLY = ["section"] as const;

// ---------------------------------------------------------------------------
// Public API
// ---------------------------------------------------------------------------

export interface ValidateResult {
  ok: boolean;
  errors: string[];
  warnings: string[];
}

/**
 * Validate a single backlog yaml record body.
 *
 * Pure function — no filesystem access.
 * Error strings: "{rule}: {detail}" — caller prepends "{file}: " context.
 */
export function validateBacklogRecord(yamlBody: string): ValidateResult {
  const errors: string[] = [];
  const warnings: string[] = [];

  const s = parseYamlScalars(yamlBody);
  const isClosed = s.status === "closed";

  // 1. Required fields
  for (const field of REQUIRED_ALL) {
    if (!s[field]) {
      errors.push(`${E_MISSING_FIELD}: required field '${field}' is absent or empty`);
    }
  }
  if (!isClosed) {
    for (const field of REQUIRED_OPEN_ONLY) {
      if (!s[field]) {
        errors.push(`${E_MISSING_FIELD}: required field '${field}' is absent or empty (open record)`);
      }
    }
  }

  // 2. Status enum
  if (s.status && !VALID_STATUS.has(s.status)) {
    errors.push(`${E_BAD_STATUS}: invalid status '${s.status}' (expected: open, closed)`);
  }

  // 3. Id format
  if (s.id && !ID_RE.test(s.id)) {
    errors.push(`${E_BAD_ID_FORMAT}: id '${s.id}' does not match expected format (PREFIX-NUMBER)`);
  }

  // 4. depends_on non-empty → depends_on_raw must be non-empty
  const dependsOnArr = Array.isArray(s.depends_on) ? s.depends_on : [];
  if (dependsOnArr.length > 0 && !s.depends_on_raw) {
    errors.push(
      `${E_EMPTY_DEPENDS_ON_RAW}: depends_on has entries but depends_on_raw is absent or empty`,
    );
  }

  // 5. Schema-v2 locator checks (each gated on field presence — v1 back-compat)

  // 5a. task_key format
  if (s.task_key !== undefined && s.task_key !== null && s.task_key !== "") {
    if (typeof s.task_key !== "string" || !TASK_KEY_RE.test(s.task_key as string)) {
      errors.push(
        `${E_BAD_TASK_KEY_FORMAT}: task_key '${s.task_key}' does not match ^T\\d+\\.\\d+(\\.\\d+)?$`,
      );
    }
  }

  // 5b. locator array fields must be string[] when present
  for (const field of LOCATOR_ARRAY_FIELDS) {
    const val = s[field];
    if (val !== undefined && val !== null) {
      if (!Array.isArray(val)) {
        errors.push(
          `${E_BAD_LOCATOR_ARRAY_TYPE}: '${field}' must be a string array but got ${typeof val}`,
        );
      } else {
        const badIdx = (val as unknown[]).findIndex((el) => typeof el !== "string");
        if (badIdx >= 0) {
          errors.push(
            `${E_BAD_LOCATOR_ARRAY_TYPE}: '${field}[${badIdx}]' is not a string`,
          );
        }
      }
    }
  }

  // 5c. parent_plan non-empty when key present
  if ("parent_plan" in s && (s.parent_plan === "" || s.parent_plan === null || s.parent_plan === undefined)) {
    errors.push(
      `${E_EMPTY_PARENT_PLAN}: parent_plan key is present but value is empty`,
    );
  }

  return { ok: errors.length === 0, errors, warnings };
}
