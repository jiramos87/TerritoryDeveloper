/**
 * plan-parser-verify.ts — CLI diagnostic for master-plan parsing.
 *
 * Parses every ia/projects/*master-plan*.md via parseMasterPlan (same code
 * path /dashboard uses) and prints a per-plan/per-stage summary:
 *
 *   <plan filename>
 *     [<status>] Stage N — <title>      done/total    (statusDetail)
 *
 * Intended use: dashboard-vs-markdown mismatch debugging. Cheaper than
 * spawning a codebase exploration subagent — shows the raw parser output the
 * dashboard renders against.
 *
 * Run via `npm run plan-parser:verify` (from repo root or web/).
 * Exits 0 on success, 1 if no master plans found.
 */

import fs from 'fs/promises';
import path from 'path';
import { parseMasterPlan, computePlanMetrics } from './plan-parser';

/** Resolve repo root from cwd (mirrors plan-loader.ts). */
async function resolveRepoRoot(): Promise<string | null> {
  const cwd = process.cwd();
  if (path.basename(cwd) === 'web') {
    const candidate = path.resolve(cwd, '..');
    try {
      await fs.access(path.join(candidate, 'ia', 'projects'));
      return candidate;
    } catch {
      return null;
    }
  }
  try {
    await fs.access(path.join(cwd, 'ia', 'projects'));
    return cwd;
  } catch {
    return null;
  }
}

function pad(s: string, n: number): string {
  return s.length >= n ? s : s + ' '.repeat(n - s.length);
}

async function main(): Promise<number> {
  const repoRoot = await resolveRepoRoot();
  if (!repoRoot) {
    console.error('plan-parser-verify: could not locate ia/projects/ (run from repo root or web/)');
    return 1;
  }
  const plansDir = path.join(repoRoot, 'ia', 'projects');
  const entries = await fs.readdir(plansDir);
  const files = entries.filter((f) => f.includes('master-plan') && f.endsWith('.md')).sort();

  if (files.length === 0) {
    console.error(`plan-parser-verify: no master-plan files under ${plansDir}`);
    return 1;
  }

  for (const file of files) {
    const content = await fs.readFile(path.join(plansDir, file), 'utf8');
    const parsed = parseMasterPlan(content, file);
    const metrics = computePlanMetrics(parsed);

    const overall = parsed.overallStatus || '(no overall status)';
    console.log(`\n${file}  [${overall}]  ${metrics.statBarLabel}`);

    for (const stage of parsed.stages) {
      const counts = metrics.stageCounts[stage.id];
      const status = stage.status || '(derived-empty)';
      const detail = stage.statusDetail ? `  — ${stage.statusDetail}` : '';
      const countLabel = counts ? `${counts.done}/${counts.total}` : '—';
      console.log(
        `  [${pad(status, 12)}] Stage ${pad(stage.id, 4)} ${pad(countLabel, 7)}  ${stage.title}${detail}`,
      );
    }
  }

  return 0;
}

main().then(
  (code) => process.exit(code),
  (err) => {
    console.error(err);
    process.exit(1);
  },
);
