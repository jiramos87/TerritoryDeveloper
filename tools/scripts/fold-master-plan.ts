#!/usr/bin/env tsx
/**
 * fold-master-plan.ts
 *
 * Fold a flat `ia/projects/{slug}-master-plan.md` into the per-stage folder
 * shape required by Step 9 of the DB-primary refactor:
 *
 *   ia/projects/{slug}/
 *     index.md                              ← global preamble + stage index + trailer
 *     stage-{X.Y}-{short-name}.md           ← one file per `### Stage X.Y — name` block
 *
 * Source is removed via `git rm` (unless `--no-source-remove`).
 *
 * CLI:
 *   --plan <basename|slug>     Process a single plan (e.g. `blip` or `blip-master-plan.md`).
 *   --all                      Process every `*-master-plan.md` under `ia/projects/`.
 *   --dry-run                  Print summary; do not write files; do not run git.
 *   --no-source-remove         Skip `git rm` of the source plan (leaves both copies on disk).
 *   --help                     Print usage and exit 0.
 *
 * Idempotence: if `ia/projects/{slug}/` already exists, the plan is skipped.
 */

import * as fs from 'fs';
import * as path from 'path';
import { execFileSync } from 'child_process';

// ---------------------------------------------------------------------------
// Constants
// ---------------------------------------------------------------------------

const REPO_ROOT = path.resolve(__dirname, '..', '..');
const PROJECTS_DIR = path.join(REPO_ROOT, 'ia', 'projects');
const MASTER_PLAN_SUFFIX = '-master-plan.md';

// ---------------------------------------------------------------------------
// CLI
// ---------------------------------------------------------------------------

interface CliArgs {
  plan?: string;
  all: boolean;
  dryRun: boolean;
  noSourceRemove: boolean;
  help: boolean;
}

function parseArgs(argv: string[]): CliArgs {
  const args = argv.slice(2);
  const result: CliArgs = { all: false, dryRun: false, noSourceRemove: false, help: false };
  for (let i = 0; i < args.length; i++) {
    const a = args[i]!;
    if (a === '--help' || a === '-h') result.help = true;
    else if (a === '--all') result.all = true;
    else if (a === '--dry-run') result.dryRun = true;
    else if (a === '--no-source-remove') result.noSourceRemove = true;
    else if (a === '--plan') result.plan = args[++i];
    else if (a.startsWith('--plan=')) result.plan = a.slice('--plan='.length);
    else {
      process.stderr.write(`Unknown flag: ${a}\n`);
      process.exit(1);
    }
  }
  return result;
}

function printHelp(): void {
  process.stdout.write(
    [
      'fold-master-plan.ts — Fold a flat master plan into the per-stage folder shape.',
      '',
      'Usage:',
      '  npx tsx tools/scripts/fold-master-plan.ts --plan <basename|slug> [--dry-run] [--no-source-remove]',
      '  npx tsx tools/scripts/fold-master-plan.ts --all                  [--dry-run] [--no-source-remove]',
      '',
      'Options:',
      '  --plan <name>          Process one plan. Accepts `blip`, `blip-master-plan`, or full filename.',
      '  --all                  Process every *-master-plan.md under ia/projects/.',
      '  --dry-run              Print summary only; no disk writes; no git ops.',
      '  --no-source-remove     Leave the source `*-master-plan.md` on disk (default: `git rm`).',
      '  --help                 Print this help and exit.',
      '',
      'Output layout:',
      '  ia/projects/{slug}/index.md',
      '  ia/projects/{slug}/stage-{X.Y}-{short-name}.md   (one per `### Stage X.Y — name`)',
      '',
      'Idempotence:',
      '  Plans whose target folder already exists are skipped silently.',
      '',
    ].join('\n'),
  );
}

// ---------------------------------------------------------------------------
// Plan resolution
// ---------------------------------------------------------------------------

function resolvePlanFile(name: string): string {
  let basename = name.endsWith('.md') ? name : `${name}.md`;
  if (!basename.endsWith(MASTER_PLAN_SUFFIX)) {
    basename = basename.replace(/\.md$/, '') + MASTER_PLAN_SUFFIX;
  }
  return path.join(PROJECTS_DIR, basename);
}

function listAllPlanFiles(): string[] {
  const entries = fs.readdirSync(PROJECTS_DIR);
  return entries
    .filter((f) => f.endsWith(MASTER_PLAN_SUFFIX))
    .sort()
    .map((f) => path.join(PROJECTS_DIR, f));
}

// ---------------------------------------------------------------------------
// Parser
// ---------------------------------------------------------------------------

const STAGES_HEADING_RE = /^## Stages\s*$/;
const STAGE_HEADING_RE = /^### Stage (\d+(?:\.\d+)?) — (.+?)\s*$/;
const STEP_DIVIDER_RE = /^## Step \d+\b/;
const TOP_HEADING_RE = /^## (?!Stages\s*$)(?!Step \d+\b).+/;

interface StageBlock {
  stageId: string;       // "1" or "1.2"
  rawLabel: string;      // raw text after the em-dash
  shortName: string;     // last segment after final `/`, trimmed
  status: string;        // best-effort `**Status:** ...` line content; empty if absent
  lines: string[];       // FULL block including the `### Stage` header line
}

interface ParsedPlan {
  preamble: string[];     // everything before `## Stages` (NOT including that heading)
  legend: string[];       // lines between `## Stages` heading (exclusive) and first `### Stage`
  stages: StageBlock[];   // ordered by source position
  trailer: string[];      // everything from the first non-`## Step` `## ` heading after stages, to EOF
  hasStagesHeading: boolean;
}

function extractStatusLine(bodyLines: string[]): string {
  for (const l of bodyLines) {
    const m = l.match(/^\*\*Status:\*\*\s*(.+?)\s*$/);
    if (m) return m[1]!;
  }
  return '';
}

function shortNameFromLabel(label: string): string {
  const segments = label.split('/').map((s) => s.trim()).filter(Boolean);
  return segments[segments.length - 1] || label;
}

function parsePlan(content: string): ParsedPlan {
  const lines = content.split('\n');
  const preamble: string[] = [];
  const legend: string[] = [];
  const stages: StageBlock[] = [];
  const trailer: string[] = [];

  type State = 'PREAMBLE' | 'LEGEND' | 'STAGE_BODY' | 'TRAILER';
  let state: State = 'PREAMBLE';
  let hasStagesHeading = false;

  let currentStage: StageBlock | null = null;

  function closeStage(): void {
    if (currentStage) {
      stages.push(currentStage);
      currentStage = null;
    }
  }

  for (const line of lines) {
    if (state === 'PREAMBLE') {
      if (STAGES_HEADING_RE.test(line)) {
        hasStagesHeading = true;
        state = 'LEGEND';
      } else {
        preamble.push(line);
      }
      continue;
    }

    if (state === 'LEGEND') {
      if (STAGE_HEADING_RE.test(line)) {
        const m = line.match(STAGE_HEADING_RE)!;
        currentStage = {
          stageId: m[1]!,
          rawLabel: m[2]!,
          shortName: shortNameFromLabel(m[2]!),
          status: '',
          lines: [line],
        };
        state = 'STAGE_BODY';
        continue;
      }
      if (STEP_DIVIDER_RE.test(line)) {
        // Drop blip-style `## Step N — ...` separators; they have no semantic load
        // post-fold (stage labels already carry the Step / Stage path).
        continue;
      }
      if (TOP_HEADING_RE.test(line)) {
        // Trailer started before any stage was authored (rare); jump to TRAILER.
        state = 'TRAILER';
        trailer.push(line);
        continue;
      }
      legend.push(line);
      continue;
    }

    if (state === 'STAGE_BODY') {
      if (STAGE_HEADING_RE.test(line)) {
        closeStage();
        const m = line.match(STAGE_HEADING_RE)!;
        currentStage = {
          stageId: m[1]!,
          rawLabel: m[2]!,
          shortName: shortNameFromLabel(m[2]!),
          status: '',
          lines: [line],
        };
        continue;
      }
      if (STEP_DIVIDER_RE.test(line)) {
        // Drop separator without affecting stage scope.
        continue;
      }
      if (TOP_HEADING_RE.test(line)) {
        closeStage();
        state = 'TRAILER';
        trailer.push(line);
        continue;
      }
      currentStage!.lines.push(line);
      continue;
    }

    // TRAILER: collect until EOF
    trailer.push(line);
  }

  closeStage();

  // Backfill status from each stage's body
  for (const s of stages) {
    s.status = extractStatusLine(s.lines.slice(1));
  }

  return { preamble, legend, stages, trailer, hasStagesHeading };
}

// ---------------------------------------------------------------------------
// Slugify
// ---------------------------------------------------------------------------

function slugify(label: string, maxLen = 60): string {
  const ascii = label
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .replace(/-{2,}/g, '-');
  return ascii.length <= maxLen ? ascii : ascii.slice(0, maxLen).replace(/-+$/, '');
}

// ---------------------------------------------------------------------------
// Render
// ---------------------------------------------------------------------------

function trimTrailingBlankLines(lines: string[]): string[] {
  let end = lines.length;
  while (end > 0 && lines[end - 1]!.trim() === '') end--;
  return lines.slice(0, end);
}

function renderIndex(parsed: ParsedPlan, stageFiles: Array<{ stage: StageBlock; filename: string }>): string {
  const out: string[] = [];

  out.push(...trimTrailingBlankLines(parsed.preamble));

  if (parsed.hasStagesHeading) {
    out.push('');
    out.push('## Stages');
    const legend = trimTrailingBlankLines(parsed.legend);
    if (legend.length) {
      out.push('');
      out.push(...legend);
    }
  }

  if (stageFiles.length) {
    out.push('');
    out.push('### Stage index');
    out.push('');
    for (const { stage, filename } of stageFiles) {
      const status = stage.status ? ` — _${stage.status}_` : '';
      out.push(`- [Stage ${stage.stageId} — ${stage.rawLabel}](${filename})${status}`);
    }
  }

  const trailer = trimTrailingBlankLines(parsed.trailer);
  if (trailer.length) {
    out.push('');
    out.push(...trailer);
  }

  return out.join('\n').replace(/\n{3,}/g, '\n\n').trimEnd() + '\n';
}

function renderStage(stage: StageBlock): string {
  const body = trimTrailingBlankLines(stage.lines);
  return body.join('\n').trimEnd() + '\n';
}

// ---------------------------------------------------------------------------
// Filesystem + git
// ---------------------------------------------------------------------------

function ensureDir(dir: string, dryRun: boolean): void {
  if (dryRun) return;
  fs.mkdirSync(dir, { recursive: true });
}

function writeFileAtomic(dest: string, content: string, dryRun: boolean): void {
  if (dryRun) return;
  const tmp = `${dest}.tmp-${process.pid}`;
  fs.writeFileSync(tmp, content, 'utf8');
  fs.renameSync(tmp, dest);
}

function gitRm(file: string, dryRun: boolean): void {
  if (dryRun) return;
  execFileSync('git', ['rm', '-q', file], { cwd: REPO_ROOT, stdio: 'inherit' });
}

// ---------------------------------------------------------------------------
// Per-plan driver
// ---------------------------------------------------------------------------

interface FoldResult {
  slug: string;
  source: string;
  targetDir: string;
  stageCount: number;
  stageFiles: string[];
  skipped?: string;
}

function foldOne(srcPath: string, dryRun: boolean, removeSource: boolean): FoldResult {
  const basename = path.basename(srcPath);
  const slug = basename.replace(/-master-plan\.md$/, '');
  const targetDir = path.join(PROJECTS_DIR, slug);
  const targetRel = path.relative(REPO_ROOT, targetDir);

  if (!fs.existsSync(srcPath)) {
    return { slug, source: srcPath, targetDir, stageCount: 0, stageFiles: [], skipped: 'source-missing' };
  }
  if (fs.existsSync(targetDir)) {
    return { slug, source: srcPath, targetDir, stageCount: 0, stageFiles: [], skipped: 'target-folder-exists' };
  }

  const content = fs.readFileSync(srcPath, 'utf8');
  const parsed = parsePlan(content);

  // Build per-stage filename (collision-safe via stage id; short-name slug for readability)
  const seenNames = new Set<string>();
  const stageFiles: Array<{ stage: StageBlock; filename: string }> = [];
  for (const s of parsed.stages) {
    let nameSlug = slugify(s.shortName);
    if (!nameSlug) nameSlug = 'unnamed';
    let filename = `stage-${s.stageId}-${nameSlug}.md`;
    let n = 2;
    while (seenNames.has(filename)) {
      filename = `stage-${s.stageId}-${nameSlug}-${n}.md`;
      n++;
    }
    seenNames.add(filename);
    stageFiles.push({ stage: s, filename });
  }

  const indexBody = renderIndex(parsed, stageFiles);

  if (dryRun) {
    process.stdout.write(`[dry-run] ${slug}: ${parsed.stages.length} stages → ${targetRel}/\n`);
    process.stdout.write(`  index.md (${indexBody.length} chars)\n`);
    for (const { filename, stage } of stageFiles) {
      const body = renderStage(stage);
      process.stdout.write(`  ${filename} (Stage ${stage.stageId} — ${stage.shortName}; ${body.length} chars)\n`);
    }
    return {
      slug,
      source: srcPath,
      targetDir,
      stageCount: parsed.stages.length,
      stageFiles: stageFiles.map((s) => s.filename),
    };
  }

  ensureDir(targetDir, false);
  writeFileAtomic(path.join(targetDir, 'index.md'), indexBody, false);
  for (const { filename, stage } of stageFiles) {
    writeFileAtomic(path.join(targetDir, filename), renderStage(stage), false);
  }

  if (removeSource) {
    const srcRel = path.relative(REPO_ROOT, srcPath);
    gitRm(srcRel, false);
  }

  process.stderr.write(`[fold] ${slug}: ${parsed.stages.length} stages → ${targetRel}/\n`);

  return {
    slug,
    source: srcPath,
    targetDir,
    stageCount: parsed.stages.length,
    stageFiles: stageFiles.map((s) => s.filename),
  };
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

function main(): void {
  const args = parseArgs(process.argv);
  if (args.help) {
    printHelp();
    process.exit(0);
  }
  if (!args.plan && !args.all) {
    process.stderr.write('Either --plan <name> or --all is required.\n');
    printHelp();
    process.exit(1);
  }

  const planFiles = args.all ? listAllPlanFiles() : [resolvePlanFile(args.plan!)];
  if (planFiles.length === 0) {
    process.stderr.write('No master plan files matched.\n');
    process.exit(1);
  }

  const removeSource = !args.noSourceRemove;

  let totalPlans = 0;
  let totalStages = 0;
  let skipped: FoldResult[] = [];

  for (const f of planFiles) {
    const result = foldOne(f, args.dryRun, removeSource);
    if (result.skipped) {
      skipped.push(result);
      continue;
    }
    totalPlans++;
    totalStages += result.stageCount;
  }

  if (skipped.length) {
    process.stderr.write(`\nSkipped ${skipped.length}:\n`);
    for (const r of skipped) {
      process.stderr.write(`  ${r.slug} (${r.skipped})\n`);
    }
  }

  process.stderr.write(`\n${args.dryRun ? '[dry-run] ' : ''}Folded ${totalPlans} plan(s); ${totalStages} stage file(s) emitted.\n`);
}

main();
