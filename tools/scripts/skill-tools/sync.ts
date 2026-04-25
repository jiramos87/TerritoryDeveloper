// Sync rendered command + agent + cursor mirror against canonical SKILL.md.
// Default = diff-only (exit 1 on drift). --apply writes files.

import fs from "node:fs";
import path from "node:path";
import { type SkillFrontmatter, REPO_ROOT, listSkillSlugs, readSkillFrontmatter } from "./frontmatter.js";
import { renderAgent } from "./render-agent.js";
import { renderCommand } from "./render-command.js";
import { renderCursor } from "./render-cursor.js";

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

  const targets: Array<{
    filePath: string;
    expected: string;
  }> = [
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
