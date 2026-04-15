/**
 * plan-loader.ts — loadAllPlans() for the web workspace.
 *
 * Globs ia/projects/*master-plan*.md from repo root, reads each file,
 * delegates parsing to tools/progress-tracker/parse.mjs via dynamic ESM
 * import, and returns a typed PlanData[].
 *
 * Wrapper-only invariant: parse.mjs is NOT modified by this file.
 * Empty ia/projects/ (no master-plan files) returns [] — diverges
 * intentionally from CLI (exits non-zero); RSC callers prefer graceful empty.
 * Requires Node 20+ — dynamic ESM import() of parse.mjs relies on Node ≥ 20 stable ESM resolver.
 */

import fs from 'fs/promises';
import path from 'path';
import type { PlanData } from './plan-loader-types';

/**
 * Resolve repo root from process.cwd().
 * Next.js build runs with cwd = web/; validate:web may run from repo root.
 * Mirrors resolveContentPath existence-check idiom from web/lib/mdx/loader.ts.
 */
async function resolveRepoRoot(): Promise<string> {
  const cwd = process.cwd();
  if (path.basename(cwd) === 'web') {
    const candidate = path.resolve(cwd, '..');
    // Verify ia/projects/ exists at candidate to catch mis-parented repos.
    await fs.access(path.join(candidate, 'ia', 'projects'));
    return candidate;
  }
  // Assume cwd is already repo root; verify presence of ia/projects/.
  const iaDir = path.join(cwd, 'ia', 'projects');
  try {
    await fs.access(iaDir);
  } catch {
    throw new Error(
      `[plan-loader] Could not locate ia/projects/ from cwd="${cwd}". ` +
        `Run from repo root or web/ subdirectory.`
    );
  }
  return cwd;
}

/**
 * Load and parse all master-plan Markdown files from ia/projects/.
 *
 * Filter + sort mirrors tools/progress-tracker/index.mjs lines 39-42 verbatim:
 *   f.includes('master-plan') && f.endsWith('.md')
 *
 * Filename arg passed as basename (not absolute path) — matches index.mjs
 * line 53; PlanData consumers key off basename for sibling-warning matching.
 */
export async function loadAllPlans(): Promise<PlanData[]> {
  const repoRoot = await resolveRepoRoot();
  const plansDir = path.join(repoRoot, 'ia', 'projects');

  let allFiles: string[];
  try {
    allFiles = await fs.readdir(plansDir);
  } catch {
    // Directory unreadable — treat as empty rather than throw.
    return [];
  }

  const planFiles = allFiles
    .filter(f => f.includes('master-plan') && f.endsWith('.md'))
    .sort(); // deterministic order

  if (planFiles.length === 0) {
    return [];
  }

  // Dynamic ESM import — module cache dedupes repeat calls.
  const { parseMasterPlan } = await import(
    '../../tools/progress-tracker/parse.mjs'
  ) as { parseMasterPlan: (md: string, filename: string) => PlanData };

  return Promise.all(
    planFiles.map(async f => {
      const content = await fs.readFile(path.join(plansDir, f), 'utf8');
      return parseMasterPlan(content, f); // basename — NOT absolute path
    })
  );
}
