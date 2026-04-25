// Sync rendered command + agent + cursor mirror against canonical SKILL.md.
// Default = diff-only (exit 1 on drift). --apply writes files.

import fs from "node:fs";
import path from "node:path";
import { type SkillFrontmatter, REPO_ROOT, listSkillSlugs, readSkillFrontmatter } from "./frontmatter.js";
import { renderAgent } from "./render-agent.js";
import { renderCommand } from "./render-command.js";
import { renderCursor } from "./render-cursor.js";
import { resolveTools } from "./tool-roles.js";

export interface SyncOptions {
  apply: boolean;
  diff: boolean;
}

export interface SyncResult {
  slug: string;
  files: SyncFileResult[];
  driftCount: number;
}

export interface SyncFileResult {
  filePath: string;
  changed: boolean;
  reason: "wrote" | "unchanged" | "would-write" | "skipped-override";
  bytesBefore: number;
  bytesAfter: number;
}

export interface SyncRunResult {
  results: SyncResult[];
  totalDrift: number;
  filesWritten: number;
}

export function syncSlug(slug: string, options: SyncOptions): SyncResult {
  const fm = readSkillFrontmatter(slug);
  const files: SyncFileResult[] = [];

  // Surfaceless skill = empty tools list (custom + no extras). No agent / command / cursor wrappers.
  // Preserves single source of truth: skills opt out of surfaces by leaving tools_extra empty.
  const tools = resolveTools(fm.tools_role, fm.tools_extra);
  const hasSurface = tools.length > 0;

  const agentPath = path.join(REPO_ROOT, ".claude", "agents", `${fm.name}.md`);
  const commandPath = path.join(REPO_ROOT, ".claude", "commands", `${fm.name}.md`);
  const cursorPath = path.join(REPO_ROOT, ".cursor", "rules", `cursor-skill-${fm.name}.mdc`);

  const targets: Array<{
    filePath: string;
    expected: string;
  }> = [];

  if (hasSurface) {
    targets.push({ filePath: agentPath, expected: renderAgent(fm) });
    targets.push({ filePath: commandPath, expected: renderCommand(fm) });
  } else if (fs.existsSync(commandPath)) {
    // Surfaceless-with-command-file pattern (orchestrator/dispatcher skills): render command only.
    // Body override under ia/skills/{slug}/command-body.md carries the actual dispatch body.
    targets.push({ filePath: commandPath, expected: renderCommand(fm) });
  }
  // Cursor mirror always rendered when SKILL.md exists (mirrors agent or command body)
  if (hasSurface || fs.existsSync(cursorPath)) {
    targets.push({ filePath: cursorPath, expected: renderCursor(fm) });
  }

  let driftCount = 0;
  for (const { filePath, expected } of targets) {
    const exists = fs.existsSync(filePath);
    const actual = exists ? fs.readFileSync(filePath, "utf8") : "";
    const bytesBefore = actual.length;

    if (actual === expected) {
      files.push({
        filePath,
        changed: false,
        reason: "unchanged",
        bytesBefore,
        bytesAfter: expected.length,
      });
      continue;
    }

    driftCount += 1;

    if (options.apply) {
      fs.mkdirSync(path.dirname(filePath), { recursive: true });
      fs.writeFileSync(filePath, expected, "utf8");
      files.push({
        filePath,
        changed: true,
        reason: "wrote",
        bytesBefore,
        bytesAfter: expected.length,
      });
    } else {
      files.push({
        filePath,
        changed: false,
        reason: "would-write",
        bytesBefore,
        bytesAfter: expected.length,
      });
    }
  }

  return { slug, files, driftCount };
}

export function syncAll(options: SyncOptions): SyncRunResult {
  return syncSlugs(listSkillSlugs(), options);
}

export function syncSlugs(slugs: string[], options: SyncOptions): SyncRunResult {
  const results: SyncResult[] = [];
  let totalDrift = 0;
  let filesWritten = 0;
  for (const slug of slugs) {
    const r = syncSlug(slug, options);
    results.push(r);
    totalDrift += r.driftCount;
    filesWritten += r.files.filter((f) => f.changed).length;
  }
  return { results, totalDrift, filesWritten };
}

export function shortDiffSummary(result: SyncResult): string {
  const lines: string[] = [];
  for (const f of result.files) {
    const rel = path.relative(REPO_ROOT, f.filePath);
    if (f.reason === "unchanged") {
      lines.push(`  ok    ${rel}`);
    } else if (f.reason === "would-write") {
      lines.push(`  drift ${rel}  (${f.bytesBefore} → ${f.bytesAfter} bytes)`);
    } else if (f.reason === "wrote") {
      lines.push(`  wrote ${rel}  (${f.bytesBefore} → ${f.bytesAfter} bytes)`);
    } else {
      lines.push(`  skip  ${rel}`);
    }
  }
  return lines.join("\n");
}

export function getRenderedTargets(fm: SkillFrontmatter): Array<{
  filePath: string;
  expected: string;
}> {
  return [
    {
      filePath: path.join(REPO_ROOT, ".claude", "agents", `${fm.name}.md`),
      expected: renderAgent(fm),
    },
    {
      filePath: path.join(REPO_ROOT, ".claude", "commands", `${fm.name}.md`),
      expected: renderCommand(fm),
    },
    {
      filePath: path.join(REPO_ROOT, ".cursor", "rules", `cursor-skill-${fm.name}.mdc`),
      expected: renderCursor(fm),
    },
  ];
}
