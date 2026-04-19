/**
 * plan-parser.ts — Master-plan Markdown parser (2-level Stage → Task).
 *
 * Pure function: master-plan *.md bytes → typed PlanData object.
 * No wall clock, no git, no filesystem I/O — caller provides raw Markdown string.
 *
 * Post lifecycle-refactor Stage 6: Step + Phase layers dropped. Master plans
 * now emit `### Stage N — {title}` directly under `## {section}` headings.
 */

import type {
  PlanData,
  TaskRow,
  TaskStatus,
  Stage,
  HierarchyStatus,
  PlanMetrics,
  StageChartBar,
  StageTaskCounts,
} from './plan-loader-types';

const TASK_STATUS_CANON: Record<string, TaskStatus> = {
  '_pending_': '_pending_',
  'Draft': 'Draft',
  'In Review': 'In Review',
  'In Progress': 'In Progress',
  'Done (archived)': 'Done (archived)',
  'Done': 'Done (archived)',
};

function normalizeTaskStatus(raw: string): TaskStatus {
  const trimmed = raw.trim();
  return TASK_STATUS_CANON[trimmed] ?? (trimmed as TaskStatus);
}

function parseStatusLine(line: string): { status: string; detail: string } | null {
  const m = line.match(/\*?\*?Status:\*?\*?\s+(.+)/);
  if (!m) return null;
  const raw = m[1].trim();
  const dashIdx = raw.indexOf(' — ');
  if (dashIdx !== -1) {
    return { status: raw.slice(0, dashIdx).trim(), detail: raw.slice(dashIdx + 3).trim() };
  }
  const dashIdx2 = raw.indexOf(' - ');
  if (dashIdx2 !== -1) {
    return { status: raw.slice(0, dashIdx2).trim(), detail: raw.slice(dashIdx2 + 3).trim() };
  }
  return { status: raw, detail: '' };
}

function parseTableRow(line: string): string[] | null {
  const trimmed = line.trim();
  if (!trimmed.startsWith('|')) return null;
  return trimmed.split('|').slice(1, -1).map(c => c.trim());
}

function isSeparatorRow(cells: string[]): boolean {
  return cells.every(c => /^[-: ]+$/.test(c));
}

function parseTaskTable(lines: string[]): TaskRow[] {
  const tasks: TaskRow[] = [];
  let headerParsed = false;
  let colMap: Record<string, number> | null = null;

  for (const line of lines) {
    const cells = parseTableRow(line);
    if (!cells) continue;
    if (isSeparatorRow(cells)) continue;

    if (!headerParsed) {
      colMap = {};
      cells.forEach((c, i) => {
        const key = c.toLowerCase().replace(/[^a-z]/g, '');
        colMap![key] = i;
      });
      headerParsed = true;
      continue;
    }

    if (!colMap) continue;
    if (cells.length < 3) continue;

    const taskIdx = colMap['task'] ?? 0;
    const nameIdx = colMap['name'] ?? -1;
    const issueIdx = colMap['issue'] ?? (nameIdx >= 0 ? 2 : 1);
    const statusIdx = colMap['status'] ?? (nameIdx >= 0 ? 3 : 2);
    const intentIdx = colMap['intent'] ?? (nameIdx >= 0 ? 4 : 3);

    const id = cells[taskIdx] ?? '';
    const name = nameIdx >= 0 ? (cells[nameIdx] ?? '') : '';
    const issueRaw = (cells[issueIdx] ?? '').replace(/\*\*/g, '').trim();
    const statusRaw = cells[statusIdx] ?? '';
    const intent = cells[intentIdx] ?? '';

    if (!id || id.toLowerCase() === 'task') continue;

    tasks.push({
      id: id.replace(/\*\*/g, '').trim(),
      ...(name ? { name: name.trim() } : {}),
      issue: issueRaw,
      status: normalizeTaskStatus(statusRaw),
      intent,
    });
  }

  return tasks;
}

function extractObjective(lines: string[]): string | undefined {
  for (const line of lines) {
    const m = line.match(/\*\*Objectives?:\*\*\s+(.+)/);
    if (m) return m[1].trim();
  }
  return undefined;
}

function extractSiblingWarnings(lines: string[]): string[] {
  const warnings: string[] = [];
  for (const line of lines) {
    const trimmed = line.trim();
    if (!trimmed.startsWith('>')) continue;
    const content = trimmed.replace(/^>\s*/, '');
    if (
      content.includes('master-plan') ||
      content.includes('Parallel-work rule') ||
      content.includes('Sibling orchestrator') ||
      content.includes('sibling orchestrator')
    ) {
      warnings.push(content);
    }
  }
  return warnings;
}

export function parseMasterPlan(markdown: string, filename = ''): PlanData {
  const lines = markdown.split('\n');

  let title = filename;
  for (const line of lines) {
    if (line.startsWith('# ')) { title = line.slice(2).trim(); break; }
  }

  let overallStatus = '';
  let overallStatusDetail = '';
  for (const line of lines) {
    const trimmed = line.trim();
    if (trimmed.startsWith('>')) {
      const content = trimmed.replace(/^>\s*/, '');
      if (content.includes('**Status:**') || content.startsWith('Status:')) {
        const parsed = parseStatusLine(content);
        if (parsed) { overallStatus = parsed.status; overallStatusDetail = parsed.detail; break; }
      }
    }
  }

  const siblingWarnings = extractSiblingWarnings(lines);

  const stages: Stage[] = [];
  let currentStage: Stage | null = null;
  let stageLines: string[] = [];

  function finalizeStage(stage: Stage, accLines: string[]): void {
    stage.tasks = parseTaskTable(accLines);
    stage.objective = extractObjective(accLines);
  }

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];

    if (/^##\s+/.test(line)) {
      if (currentStage && stageLines.length) { finalizeStage(currentStage, stageLines); stages.push(currentStage); stageLines = []; }
      currentStage = null;
      continue;
    }

    const stageMatch = line.match(/^###\s+Stage\s+([0-9.]+)\s+—\s+(.+)/);
    if (stageMatch) {
      if (currentStage && stageLines.length) { finalizeStage(currentStage, stageLines); stages.push(currentStage); }
      stageLines = [];
      currentStage = {
        id: stageMatch[1],
        title: stageMatch[2].trim(),
        status: '' as HierarchyStatus,
        statusDetail: '',
        tasks: [],
      };
      continue;
    }

    if (currentStage) {
      stageLines.push(line);
      const trimmed = line.trim();
      if (trimmed.startsWith('**Status:**') || trimmed.match(/^\*?\*?Status:\*?\*?\s/)) {
        const parsed = parseStatusLine(trimmed);
        if (parsed && !currentStage.status) {
          currentStage.status = parsed.status as HierarchyStatus;
          currentStage.statusDetail = parsed.detail;
        }
      }
    }
  }

  if (currentStage && stageLines.length) { finalizeStage(currentStage, stageLines); stages.push(currentStage); }

  const allTasks: TaskRow[] = [];
  for (const stage of stages) {
    for (const task of stage.tasks) {
      allTasks.push(task);
    }
  }

  deriveHierarchyStatus(stages);

  return { title, filename, overallStatus, overallStatusDetail, siblingWarnings, stages, allTasks };
}

// Task state considered "terminal done" for derivation purposes.
export function isTaskDone(s: TaskStatus): boolean {
  return s === 'Done' || s === 'Done (archived)';
}

// Task state considered "active" (work in flight, not pending, not done).
export function isTaskActive(s: TaskStatus): boolean {
  return s === 'In Progress' || s === 'In Review';
}

// Task state considered "not yet filed / pending".
export function isTaskPending(s: TaskStatus): boolean {
  return s === '_pending_' || s === 'Draft';
}

/**
 * Pre-compute dashboard metrics for a single plan.
 *
 * Returns completion counts, StatBar label, per-stage chart breakdown, and
 * per-stage done/total counts. Call in server components / loaders; pass result
 * to render-only components as props.
 */
export function computePlanMetrics(plan: PlanData): PlanMetrics {
  const totalCount = plan.allTasks.length;
  const completedCount = plan.allTasks.filter(t => isTaskDone(t.status)).length;
  const statBarLabel = `${completedCount} / ${totalCount} done`;

  const chartData: StageChartBar[] = plan.stages.map(stage => ({
    label:      stage.title,
    pending:    stage.tasks.filter(t => isTaskPending(t.status)).length,
    inProgress: stage.tasks.filter(t => isTaskActive(t.status)).length,
    done:       stage.tasks.filter(t => isTaskDone(t.status)).length,
  }));

  const stageCounts: Record<string, StageTaskCounts> = {};
  for (const stage of plan.stages) {
    stageCounts[stage.id] = {
      done:  stage.tasks.filter(t => isTaskDone(t.status)).length,
      total: stage.tasks.length,
    };
  }

  return { completedCount, totalCount, statBarLabel, chartData, stageCounts };
}

/**
 * Overwrite stage `status` with a value derived from task completion.
 *
 * Hand-written Status lines in master-plan markdown drift — stages whose tasks
 * are all archived still read "Draft" or old free-form prose. Dashboard status
 * badges should reflect the actual task table, not stale prose.
 *
 * Stage rules (with tasks):
 *   - all tasks Done → 'Final'
 *   - any task active or any task Done mixed with pending → 'In Progress'
 *   - all tasks pending → 'Draft'
 *
 * Stage with no tasks (skeleton): keep parsed status.
 */
function deriveHierarchyStatus(stages: Stage[]): void {
  for (const stage of stages) {
    if (!stage.tasks.length) continue;
    const statuses = stage.tasks.map(t => t.status);
    const allDone = statuses.every(isTaskDone);
    const anyActive = statuses.some(isTaskActive);
    const anyDone = statuses.some(isTaskDone);
    const allPending = statuses.every(isTaskPending);
    if (allDone) {
      stage.status = 'Final';
    } else if (anyActive || (anyDone && !allDone)) {
      stage.status = 'In Progress';
    } else if (allPending) {
      stage.status = 'Draft';
    }
  }
}
