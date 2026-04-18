/**
 * backlog-yaml-writer.mjs
 *
 * Pure writer helpers extracted from migrate-backlog-to-yaml.mjs so tests + other
 * tools can import buildYaml without executing the migrate script's top-level
 * BACKLOG.md parse + filesystem writes.
 *
 * Sole source of truth for the backlog yaml serializer shape. The migrate script
 * now imports buildYaml from here; test harnesses (TECH-366 round-trip) do the
 * same.
 */

// ---------------------------------------------------------------------------
// YAML serializer primitives
// ---------------------------------------------------------------------------

export function yamlLiteralBlock(value, indent = "  ") {
  if (!value || !value.trim()) return '""';
  const lines = value.split("\n");
  while (lines.length > 0 && !lines[lines.length - 1].trim()) lines.pop();
  return "|\n" + lines.map((l) => indent + l).join("\n");
}

export function yamlScalar(value) {
  if (value === null || value === undefined) return '""';
  const s = String(value);
  if (
    !s ||
    /^[:\[\]{},#&*!|>'"%@`]/.test(s) ||
    s.includes(": ") ||
    s.includes("\n") ||
    s.includes('"') ||
    s === "true" ||
    s === "false" ||
    s === "null" ||
    /^-?\d+(\.\d+)?$/.test(s)
  ) {
    return '"' + s.replace(/\\/g, "\\\\").replace(/"/g, '\\"').replace(/\n/g, "\\n") + '"';
  }
  return s;
}

export function yamlList(items, indent = "  ") {
  if (!items || items.length === 0) return "[]";
  return "\n" + items.map((item) => `${indent}- ${yamlScalar(item)}`).join("\n");
}

export function parseFilesField(filesStr) {
  if (!filesStr || !filesStr.trim()) return [];
  const paths = [];
  const backtickRe = /`([^`]+)`/g;
  let m;
  while ((m = backtickRe.exec(filesStr)) !== null) {
    const p = m[1].trim();
    if (p && p !== "…") paths.push(p);
  }
  return [...new Set(paths)];
}

export function parseDependsOn(dependsStr) {
  if (!dependsStr || !dependsStr.trim() || dependsStr.trim() === "none") return [];
  const ids = [];
  const re = /\b(BUG|FEAT|TECH|ART|AUDIO)-(\d+)([a-z]?)\b/gi;
  let m;
  while ((m = re.exec(dependsStr)) !== null) {
    ids.push(`${m[1].toUpperCase()}-${m[2]}${m[3] ? m[3].toLowerCase() : ""}`);
  }
  return [...new Set(ids)];
}

export function normalizePriority(section) {
  if (!section) return "medium";
  const s = section.toLowerCase();
  if (s.includes("high")) return "high";
  if (s.includes("medium")) return "medium";
  if (s.includes("low")) return "low";
  if (s.includes("completed")) return "closed";
  return "medium";
}

/**
 * Trim raw_markdown: stop at any ## top-level section header line.
 * Also trim trailing blank lines.
 */
export function trimRawMarkdown(raw) {
  if (!raw) return "";
  const lines = raw.split("\n");
  const out = [];
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    if (i === 0) { out.push(line); continue; }
    if (line === "") {
      let nextIndented = false;
      for (let j = i + 1; j < lines.length; j++) {
        if (lines[j] === "") continue;
        if (lines[j].startsWith("  ") || lines[j].startsWith("\t")) nextIndented = true;
        break;
      }
      if (nextIndented) out.push(line);
      else break;
      continue;
    }
    if (line.startsWith("  ") || line.startsWith("\t")) out.push(line);
    else break;
  }
  while (out.length > 0 && !out[out.length - 1].trim()) out.pop();
  return out.join("\n");
}

/**
 * Serialize a parsed backlog issue object to a yaml string.
 *
 * Schema v1 fields (always emitted): id, type, title, priority, status, section, spec, files,
 * notes, acceptance, depends_on, depends_on_raw, related, created, raw_markdown.
 *
 * Schema v2 locator fields (conditionally emitted — TECH-365):
 * - parent_plan + task_key: both required; partial-v2 = drop to v1.
 * - step, stage, phase, router_domain: optional scalars; emitted when non-null.
 *   step + phase bare integers.
 * - surfaces, mcp_slices, skill_hints: optional arrays; emitted when non-empty.
 * - Declaration order mirrors read path (yamlToIssue) for grep-symmetry.
 *
 * Note: `created` stamps the current run date — callers that need byte-identical
 * round-trip must normalize that line before compare.
 */
export function buildYaml(issue) {
  const files = parseFilesField(issue.files ?? "");
  const dependsOn = parseDependsOn(issue.depends_on ?? "");
  const priority = normalizePriority(issue.backlog_section);
  const typeStr = issue.type ?? "unknown";
  const specVal = issue.spec ? issue.spec.replace(/^`|`$/g, "").trim() : "";
  const createdDate = new Date().toISOString().slice(0, 10);

  const dependsOnRaw = (issue.depends_on ?? "").trim();

  const lines = [
    `id: ${issue.issue_id}`,
    `type: ${yamlScalar(typeStr)}`,
    `title: ${yamlScalar(issue.title)}`,
    `priority: ${priority}`,
    `status: ${issue.status === "completed" ? "closed" : "open"}`,
    `section: ${yamlScalar(issue.backlog_section)}`,
    `spec: ${specVal ? yamlScalar(specVal) : '""'}`,
    `files: ${yamlList(files)}`,
    `notes: ${issue.notes ? yamlLiteralBlock(issue.notes) : '""'}`,
    `acceptance: ${issue.acceptance ? yamlLiteralBlock(issue.acceptance) : '""'}`,
    `depends_on: ${yamlList(dependsOn)}`,
    `depends_on_raw: ${dependsOnRaw ? yamlScalar(dependsOnRaw) : '""'}`,
    `related: []`,
    `created: ${createdDate}`,
    `raw_markdown: ${yamlLiteralBlock(trimRawMarkdown(issue.raw_markdown || ""))}`,
  ];

  // --- schema-v2 locator fields ---
  if (issue.parent_plan != null && issue.task_key != null) {
    lines.push(`parent_plan: ${yamlScalar(issue.parent_plan)}`);
    lines.push(`task_key: ${yamlScalar(issue.task_key)}`);
  }
  if (issue.step != null) lines.push(`step: ${issue.step}`);
  if (issue.stage != null) lines.push(`stage: ${yamlScalar(issue.stage)}`);
  if (issue.phase != null) lines.push(`phase: ${issue.phase}`);
  if (issue.router_domain != null) lines.push(`router_domain: ${yamlScalar(issue.router_domain)}`);
  if (issue.surfaces && issue.surfaces.length > 0) lines.push(`surfaces: ${yamlList(issue.surfaces)}`);
  if (issue.mcp_slices && issue.mcp_slices.length > 0) lines.push(`mcp_slices: ${yamlList(issue.mcp_slices)}`);
  if (issue.skill_hints && issue.skill_hints.length > 0) lines.push(`skill_hints: ${yamlList(issue.skill_hints)}`);

  // Strip trailing space on key lines whose value is a newline-leading list
  // (avoids `files: \n  - path` vs fixture `files:\n  - path`).
  return lines.join("\n").replace(/ \n/g, "\n") + "\n";
}
