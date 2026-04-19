#!/usr/bin/env tsx
/**
 * migrate-master-plans.ts
 *
 * Transform master plans from 4-level (Step > Stage > Phase > Task) schema to
 * 2-level (Stage > Task) schema.
 *
 * Input:  ia/state/pre-refactor-snapshot/{plan-path}  (read-only snapshot)
 * Output: {plan-path} at repo root (atomic overwrite via tempfile + rename)
 * State:  ia/state/lifecycle-refactor-migration.json  (M2 per-file status)
 *
 * Transform rules:
 *   - `## Steps` header → `## Stages`
 *   - Each (Step N + Stage N.M) pair → new `### Stage {seq} — {StepName} / {StageName}`
 *   - Step-level content blocks (Status, Objectives, Exit, Art, Relevant surfaces) → dropped
 *   - `**Phases:**` section → bullets extracted; merged into parent Stage **Exit:** block; section dropped
 *   - `##### Phase N` h5 headings → exit bullets extracted + merged; entire h5 section dropped
 *   - Task table `Phase` column → dropped
 *   - Task ids `T{N}.{M}.{k}` → `T{stageSeq}.{k}` (stageSeq monotonic across flattened plan, starting at 1)
 *   - Task `Issue` + `Status` cells → preserved verbatim
 *
 * CLI:
 *   --only <basename>   Process only the named file (e.g. blip-master-plan.md)
 *   --dry-run           Emit to stdout; no disk writes, no JSON mutation
 *   --help              Print usage and exit 0
 *
 * Idempotence: M2 entries with status "done" are skipped.
 * Crash-safety: each plan + JSON write goes through tempfile + fs.renameSync.
 */

import * as fs from 'fs';
import * as path from 'path';

// ---------------------------------------------------------------------------
// Constants
// ---------------------------------------------------------------------------

const REPO_ROOT = path.resolve(__dirname, '..', '..');
const MIGRATION_JSON = path.join(REPO_ROOT, 'ia', 'state', 'lifecycle-refactor-migration.json');
const SNAPSHOT_ROOT = path.join(REPO_ROOT, 'ia', 'state', 'pre-refactor-snapshot');

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

interface MigEntry {
  path: string;
  status: 'pending' | 'done';
}

interface MigJson {
  files: {
    M2: MigEntry[];
    [k: string]: unknown;
  };
  [k: string]: unknown;
}

interface StageBlock {
  stepName: string;
  stageName: string;
  bodyLines: string[]; // all lines after the stage heading, up to (not including) next heading
}

interface ParsedPlan {
  preamble: string[];      // everything before `## Steps` (inclusive, with ## Steps renamed)
  stagesHeader: string[];  // tracking legend block after ## Steps, before first ### Step
  stages: StageBlock[];
}

// ---------------------------------------------------------------------------
// CLI
// ---------------------------------------------------------------------------

interface CliArgs {
  only?: string;
  dryRun: boolean;
  help: boolean;
}

function parseArgs(argv: string[]): CliArgs {
  const args = argv.slice(2);
  const result: CliArgs = { dryRun: false, help: false };
  for (let i = 0; i < args.length; i++) {
    const a = args[i]!;
    if (a === '--help' || a === '-h') {
      result.help = true;
    } else if (a === '--dry-run') {
      result.dryRun = true;
    } else if (a === '--only') {
      result.only = args[++i];
    } else if (a.startsWith('--only=')) {
      result.only = a.slice('--only='.length);
    } else {
      process.stderr.write(`Unknown flag: ${a}\n`);
      process.exit(1);
    }
  }
  return result;
}

function printHelp(): void {
  process.stdout.write(
    [
      'migrate-master-plans.ts — Transform Step/Stage/Phase/Task master plans to Stage/Task schema.',
      '',
      'Usage:',
      '  npx tsx tools/scripts/migrate-master-plans.ts [options]',
      '',
      'Options:',
      '  --only <basename>   Process only the named file (e.g. blip-master-plan.md)',
      '  --dry-run           Emit transformed content to stdout; no disk writes, no JSON mutation',
      '  --help              Print this help message and exit',
      '',
      'Default:',
      '  Iterate all M2 entries with status "pending" in ia/state/lifecycle-refactor-migration.json.',
      '  Entries with status "done" are skipped (idempotent).',
      '',
      'Exit codes:',
      '  0  Success (or --help)',
      '  1  Error (I/O failure, parse error, unknown flag)',
      '',
    ].join('\n'),
  );
}

// ---------------------------------------------------------------------------
// Atomic I/O
// ---------------------------------------------------------------------------

function atomicWrite(dest: string, content: string): void {
  const dir = path.dirname(dest);
  const tmp = path.join(dir, `.tmp-${path.basename(dest)}-${process.pid}`);
  fs.writeFileSync(tmp, content, 'utf8');
  fs.renameSync(tmp, dest);
}

function atomicWriteJson(dest: string, data: unknown): void {
  atomicWrite(dest, JSON.stringify(data, null, 2) + '\n');
}

// ---------------------------------------------------------------------------
// Heading patterns
// ---------------------------------------------------------------------------

const STEPS_SECTION_RE = /^## Steps$/;
const STEP_HEADING_RE = /^### Step \d+ — (.+)$/;
const STAGE_HEADING_RE = /^#### Stage \d+\.\d+ — (.+)$/;
const PHASE_H5_HEADING_RE = /^##### Phase \d+/;

// ---------------------------------------------------------------------------
// Parse: split plan into preamble + stagesHeader + []StageBlock
// ---------------------------------------------------------------------------

/**
 * Parse old-format master plan into structured parts.
 *
 * State machine:
 *   PREAMBLE      → before `## Steps` line
 *   STAGES_HEADER → between `## Steps` and first `### Step`
 *   STEP_BODY     → inside a `### Step` block, before first `#### Stage`
 *   STAGE_BODY    → inside a `#### Stage` block
 */
function parsePlan(content: string): ParsedPlan {
  const lines = content.split('\n');

  const preamble: string[] = [];
  const stagesHeader: string[] = [];
  const stages: StageBlock[] = [];

  type State = 'PREAMBLE' | 'STAGES_HEADER' | 'STEP_BODY' | 'STAGE_BODY';
  let state: State = 'PREAMBLE';

  let currentStepName = '';
  let currentStageName = '';
  let currentStageBody: string[] = [];

  function closeStage(): void {
    if (currentStageName) {
      stages.push({
        stepName: currentStepName,
        stageName: currentStageName,
        bodyLines: currentStageBody,
      });
      currentStageName = '';
      currentStageBody = [];
    }
  }

  for (const line of lines) {
    // ---- PREAMBLE ----
    if (state === 'PREAMBLE') {
      if (STEPS_SECTION_RE.test(line)) {
        preamble.push('## Stages'); // rename
        state = 'STAGES_HEADER';
      } else {
        preamble.push(line);
      }
      continue;
    }

    // ---- STAGES_HEADER ----
    if (state === 'STAGES_HEADER') {
      if (STEP_HEADING_RE.test(line)) {
        const m = line.match(STEP_HEADING_RE)!;
        currentStepName = m[1]!;
        state = 'STEP_BODY';
        // Do not emit step heading line
      } else {
        stagesHeader.push(line);
      }
      continue;
    }

    // ---- STEP_BODY ----
    if (state === 'STEP_BODY') {
      if (STEP_HEADING_RE.test(line)) {
        // New step: update name, stay in STEP_BODY, drop line
        closeStage();
        const m = line.match(STEP_HEADING_RE)!;
        currentStepName = m[1]!;
      } else if (STAGE_HEADING_RE.test(line)) {
        // First stage of this step: open a stage
        closeStage();
        const m = line.match(STAGE_HEADING_RE)!;
        currentStageName = m[1]!;
        currentStageBody = [];
        state = 'STAGE_BODY';
      }
      // All other lines in STEP_BODY: DROP (step-level content)
      continue;
    }

    // ---- STAGE_BODY ----
    if (state === 'STAGE_BODY') {
      if (STEP_HEADING_RE.test(line)) {
        // New step: close current stage, go to STEP_BODY
        closeStage();
        const m = line.match(STEP_HEADING_RE)!;
        currentStepName = m[1]!;
        state = 'STEP_BODY';
      } else if (STAGE_HEADING_RE.test(line)) {
        // New stage: close current, start new
        closeStage();
        const m = line.match(STAGE_HEADING_RE)!;
        currentStageName = m[1]!;
        currentStageBody = [];
        // Stay in STAGE_BODY
      } else {
        currentStageBody.push(line);
      }
      continue;
    }
  }

  closeStage();

  return { preamble, stagesHeader, stages };
}

// ---------------------------------------------------------------------------
// Transform: stage body helpers
// ---------------------------------------------------------------------------

/** Split a markdown table row on `|`, trim cells. */
function splitRow(line: string): string[] {
  return line.split('|').slice(1, -1).map((c) => c.trim());
}

/** Join cells back into a markdown table row. */
function joinRow(cells: string[]): string {
  return '| ' + cells.join(' | ') + ' |';
}

/** Find "Phase" column index in table header cells (case-insensitive). Returns -1 if absent. */
function findPhaseColIdx(cells: string[]): number {
  return cells.findIndex((c) => c.toLowerCase() === 'phase');
}

/** Drop a column by index from a cells array. */
function dropCol(cells: string[], idx: number): string[] {
  return cells.filter((_, i) => i !== idx);
}

/**
 * Renumber a task id from old `T{N}.{M}.{k}` to new `T{stageSeq}.{k}`.
 * Returns original string unchanged if pattern does not match.
 */
function renumberTaskId(oldId: string, stageSeq: number): string {
  const m = oldId.match(/^T(\d+)\.(\d+)\.(\d+)$/);
  if (!m) return oldId;
  const k = parseInt(m[3]!, 10);
  return `T${stageSeq}.${k}`;
}

/**
 * Pass 1: Remove `**Phases:**` section and any `##### Phase` h5 sections.
 * Returns { lines: cleaned lines, phaseBullets: extracted phase description bullets }.
 */
function removePhasesSections(bodyLines: string[]): {
  lines: string[];
  phaseBullets: string[];
} {
  const phaseBullets: string[] = [];
  const out: string[] = [];
  let i = 0;

  while (i < bodyLines.length) {
    const line = bodyLines[i]!;

    // Detect `**Phases:**` section
    if (line.trim() === '**Phases:**') {
      i++; // skip header line
      // Skip optional blank line after header
      if (i < bodyLines.length && bodyLines[i]!.trim() === '') i++;
      // Collect phase bullet lines
      while (i < bodyLines.length) {
        const pl = bodyLines[i]!;
        if (/^- \[[ x]\] Phase \d+/.test(pl) || /^- Phase \d+/.test(pl)) {
          // Strip checkbox marker; keep "- Phase N — Description"
          const bullet = pl.replace(/^- \[[ x]\] /, '- ');
          phaseBullets.push(bullet);
          i++;
        } else {
          break;
        }
      }
      // Skip trailing blank line after Phases section (if present)
      if (i < bodyLines.length && bodyLines[i]!.trim() === '') i++;
      continue;
    }

    // Detect `##### Phase N` h5 heading → consume entire sub-section
    if (PHASE_H5_HEADING_RE.test(line)) {
      i++; // skip heading line
      // Scan until next h4/h3/h2/h5 heading or end
      while (i < bodyLines.length) {
        const pl = bodyLines[i]!;
        if (
          PHASE_H5_HEADING_RE.test(pl) ||
          STAGE_HEADING_RE.test(pl) ||
          STEP_HEADING_RE.test(pl) ||
          /^#{1,4} /.test(pl)
        ) {
          break;
        }
        // Collect exit bullets from phase h5 sections into phaseBullets
        if (/^\*\*Exit(?: criteria)?:\*\*$/.test(pl.trim())) {
          i++;
          while (i < bodyLines.length) {
            const eb = bodyLines[i]!;
            if (eb.startsWith('- ')) {
              phaseBullets.push(eb);
              i++;
            } else if (eb.trim() === '') {
              i++;
              break; // blank line ends exit block
            } else {
              break;
            }
          }
          continue;
        }
        i++;
      }
      continue;
    }

    out.push(line);
    i++;
  }

  return { lines: out, phaseBullets };
}

/**
 * Pass 2: Merge phase bullets into the Stage `**Exit:**` (or `**Exit criteria:**`) block.
 * Bullets are appended after the last exit bullet, before the next section header.
 * If no exit section found, bullets are not inserted (informational; dry-run visible).
 */
function mergeBulletsIntoExit(lines: string[], phaseBullets: string[]): string[] {
  if (phaseBullets.length === 0) return lines;

  const exitHeaderRe = /^\*\*Exit(?: criteria)?:\*\*$/;

  // Find the last bullet line within the exit section
  let exitInsertIdx = -1;
  let inExit = false;
  let lastBulletInExit = -1;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i]!;
    if (exitHeaderRe.test(line.trim())) {
      inExit = true;
      lastBulletInExit = i; // will advance
      continue;
    }
    if (inExit) {
      if (line.startsWith('- ')) {
        lastBulletInExit = i;
      } else if (line.trim() === '') {
        // blank line: might be within or ending exit block — keep scanning
      } else {
        // Non-bullet, non-blank: exit section ended
        inExit = false;
        exitInsertIdx = lastBulletInExit + 1;
      }
    }
  }
  // If still in exit at end of file
  if (inExit) {
    exitInsertIdx = lastBulletInExit + 1;
  }

  if (exitInsertIdx < 0) {
    // No exit section found — append at end (best effort)
    return [...lines, ...phaseBullets];
  }

  // Insert phase bullets at exitInsertIdx
  const result = [
    ...lines.slice(0, exitInsertIdx),
    ...phaseBullets,
    ...lines.slice(exitInsertIdx),
  ];
  return result;
}

/**
 * Pass 3: Drop `Phase` column from task table; renumber task ids.
 * Detects table by header row starting with `| Task |`.
 */
function processTaskTable(lines: string[], stageSeq: number): string[] {
  const out: string[] = [];
  let phaseColIdx = -1;
  let inTable = false;

  for (const line of lines) {
    if (!inTable) {
      // Detect task table header: starts with `| Task |` or `| Task\t`
      if (/^\| Task /.test(line)) {
        const cells = splitRow(line);
        phaseColIdx = findPhaseColIdx(cells);
        if (phaseColIdx >= 0) {
          out.push(joinRow(dropCol(cells, phaseColIdx)));
        } else {
          out.push(line); // no Phase column
        }
        inTable = true;
        continue;
      }
      out.push(line);
      continue;
    }

    // Inside table
    if (!line.startsWith('|')) {
      inTable = false;
      out.push(line);
      continue;
    }

    const cells = splitRow(line);

    // Separator row (all cells are dashes)
    const isSep = cells.every((c) => /^-+$/.test(c));
    if (isSep) {
      out.push(joinRow(phaseColIdx >= 0 ? dropCol(cells, phaseColIdx) : cells));
      continue;
    }

    // Data row: drop Phase column + renumber task id
    let newCells = phaseColIdx >= 0 ? dropCol(cells, phaseColIdx) : [...cells];
    if (newCells.length > 0) {
      newCells[0] = renumberTaskId(newCells[0]!, stageSeq);
    }
    out.push(joinRow(newCells));
  }

  return out;
}

/**
 * Transform a single stage body (all lines after the stage heading).
 * Applies all three passes in order.
 */
function transformStageBody(bodyLines: string[], stageSeq: number): string[] {
  const { lines: p1, phaseBullets } = removePhasesSections(bodyLines);
  const p2 = mergeBulletsIntoExit(p1, phaseBullets);
  const p3 = processTaskTable(p2, stageSeq);
  return p3;
}

// ---------------------------------------------------------------------------
// Assemble transformed plan
// ---------------------------------------------------------------------------

function transformPlan(content: string): string {
  const parsed = parsePlan(content);

  // If no stages found (empty or already migrated) return content unchanged
  if (parsed.stages.length === 0) {
    return content;
  }

  const out: string[] = [];

  // Preamble (## Steps already renamed to ## Stages)
  out.push(...parsed.preamble);

  // Tracking legend block
  out.push(...parsed.stagesHeader);

  // Flattened stages
  for (let i = 0; i < parsed.stages.length; i++) {
    const stage = parsed.stages[i]!;
    const seq = i + 1;
    const heading = `### Stage ${seq} — ${stage.stepName} / ${stage.stageName}`;
    out.push(heading);
    out.push(...transformStageBody(stage.bodyLines, seq));
  }

  return out.join('\n');
}

// ---------------------------------------------------------------------------
// Migration JSON helpers
// ---------------------------------------------------------------------------

function loadMigrationJson(): MigJson {
  const raw = fs.readFileSync(MIGRATION_JSON, 'utf8');
  return JSON.parse(raw) as MigJson;
}

function flipFileDone(mig: MigJson, repoRelPath: string): void {
  const entry = mig.files.M2.find((e) => e.path === repoRelPath);
  if (!entry) {
    throw new Error(`Path not found in M2: ${repoRelPath}`);
  }
  entry.status = 'done';
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

  const mig = loadMigrationJson();
  let entries = mig.files.M2;

  // Filter to --only basename if requested
  if (args.only) {
    const basename = args.only;
    entries = entries.filter((e) => path.basename(e.path) === basename);
    if (entries.length === 0) {
      process.stderr.write(`No M2 entry found with basename: ${basename}\n`);
      process.exit(1);
    }
  }

  let processed = 0;
  let skipped = 0;

  for (const entry of entries) {
    // Idempotence: skip already-done files
    if (entry.status === 'done') {
      skipped++;
      if (args.dryRun) {
        process.stderr.write(`[skip] ${entry.path} (status: done)\n`);
      }
      continue;
    }

    const snapshotPath = path.join(SNAPSHOT_ROOT, entry.path);
    const outputPath = path.join(REPO_ROOT, entry.path);

    if (!fs.existsSync(snapshotPath)) {
      process.stderr.write(`Snapshot not found: ${snapshotPath}\n`);
      process.exit(1);
    }

    const content = fs.readFileSync(snapshotPath, 'utf8');
    const transformed = transformPlan(content);

    if (args.dryRun) {
      // Emit to stdout; no disk mutation; no JSON flip
      if (entries.length > 1) {
        process.stdout.write(`\n${'='.repeat(72)}\n=== ${entry.path}\n${'='.repeat(72)}\n\n`);
      }
      process.stdout.write(transformed);
      if (!transformed.endsWith('\n')) process.stdout.write('\n');
      processed++;
      continue;
    }

    // Atomic write: plan file
    atomicWrite(outputPath, transformed);

    // Atomic write: flip status in migration JSON immediately after plan write succeeds
    flipFileDone(mig, entry.path);
    atomicWriteJson(MIGRATION_JSON, mig);

    process.stderr.write(`[done] ${entry.path}\n`);
    processed++;
  }

  if (!args.dryRun) {
    process.stderr.write(`Processed: ${processed}, Skipped (done): ${skipped}\n`);
  }
}

main();
