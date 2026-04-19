/**
 * plan-loader.ts — loadAllPlans() for the web workspace.
 *
 * Two data sources:
 *   - Filesystem (dev, local build): globs ia/projects/*master-plan*.md from repo root.
 *   - GitHub raw (Vercel runtime): fetches same files via Contents API + raw URLs with
 *     Next.js ISR cache (REVALIDATE_SECONDS). Selected when process.env.VERCEL === '1'.
 *
 * Wrapper-only invariant: parseMasterPlan is NOT modified by this file.
 * Empty ia/projects/ (no master-plan files) returns [] — diverges intentionally from CLI
 * (exits non-zero); RSC callers prefer graceful empty.
 * Requires Node 20+.
 */

import fs from 'fs/promises';
import path from 'path';
import { parseMasterPlan } from './plan-parser';
import type { PlanData } from './plan-loader-types';

const GH_OWNER = 'jiramos87';
const GH_REPO  = 'TerritoryDeveloper';
const REVALIDATE_SECONDS = 300; // 5 min ISR window for dashboard freshness vs rate-limit

/** Branch to read on Vercel. Prefers the current deployed branch (preview or prod). */
function githubRef(): string {
  return process.env.VERCEL_GIT_COMMIT_REF ?? 'main';
}

/**
 * Resolve repo root from process.cwd().
 * Next.js build runs with cwd = web/; validate:web may run from repo root.
 * Mirrors resolveContentPath existence-check idiom from web/lib/mdx/loader.ts.
 */
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
  const iaDir = path.join(cwd, 'ia', 'projects');
  try {
    await fs.access(iaDir);
    return cwd;
  } catch {
    return null;
  }
}

async function loadAllPlansFromFilesystem(): Promise<PlanData[]> {
  const repoRoot = await resolveRepoRoot();
  if (!repoRoot) return [];
  const plansDir = path.join(repoRoot, 'ia', 'projects');

  let allFiles: string[];
  try {
    allFiles = await fs.readdir(plansDir);
  } catch {
    return [];
  }

  const planFiles = allFiles
    .filter(f => f.includes('master-plan') && f.endsWith('.md'))
    .sort();

  if (planFiles.length === 0) return [];

  return Promise.all(
    planFiles.map(async f => {
      const content = await fs.readFile(path.join(plansDir, f), 'utf8');
      return parseMasterPlan(content, f);
    })
  );
}

type GhDirEntry = { name: string; type: string; download_url: string | null };

async function loadAllPlansFromGitHub(): Promise<PlanData[]> {
  const ref = githubRef();
  const listUrl = `https://api.github.com/repos/${GH_OWNER}/${GH_REPO}/contents/ia/projects?ref=${encodeURIComponent(ref)}`;

  const ghHeaders: Record<string, string> = { Accept: 'application/vnd.github+json' };
  if (process.env.GITHUB_TOKEN) ghHeaders['Authorization'] = `Bearer ${process.env.GITHUB_TOKEN}`;

  let entries: GhDirEntry[];
  try {
    const res = await fetch(listUrl, {
      headers: ghHeaders,
      next: { revalidate: REVALIDATE_SECONDS },
    });
    if (!res.ok) {
      console.warn(`[plan-loader] GitHub list failed ${res.status} for ${listUrl}`);
      return [];
    }
    entries = (await res.json()) as GhDirEntry[];
  } catch (err) {
    console.warn(`[plan-loader] GitHub list fetch threw:`, err);
    return [];
  }

  const planEntries = entries
    .filter(e => e.type === 'file' && e.name.includes('master-plan') && e.name.endsWith('.md') && e.download_url)
    .sort((a, b) => a.name.localeCompare(b.name));

  if (planEntries.length === 0) return [];

  return Promise.all(
    planEntries.map(async e => {
      const res = await fetch(e.download_url!, { headers: ghHeaders, next: { revalidate: REVALIDATE_SECONDS } });
      const content = await res.text();
      return parseMasterPlan(content, e.name);
    })
  );
}

/**
 * Load and parse all master-plan Markdown files from ia/projects/.
 * Data source chosen by runtime: Vercel → GitHub raw; otherwise → filesystem.
 */
export async function loadAllPlans(): Promise<PlanData[]> {
  if (process.env.VERCEL === '1') {
    return loadAllPlansFromGitHub();
  }
  return loadAllPlansFromFilesystem();
}
