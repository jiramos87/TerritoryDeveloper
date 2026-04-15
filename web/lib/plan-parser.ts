/**
 * plan-parser.ts — Master-plan Markdown parser (TypeScript port of tools/progress-tracker/parse.mjs).
 *
 * Pure function: master-plan *.md bytes → typed PlanData object.
 * No wall clock, no git, no filesystem I/O — caller provides raw Markdown string.
 *
 * tools/progress-tracker/parse.mjs is the authoritative reference implementation.
 * Keep logic in sync; this file owns the TypeScript types.
 */

import type { PlanData, TaskRow, TaskStatus, PhaseEntry, Stage, Step } from './plan-loader-types';

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
    const phaseIdx = colMap['phase'] ?? (nameIdx >= 0 ? 2 : 1);
    const issueIdx = colMap['issue'] ?? (nameIdx >= 0 ? 3 : 2);
    const statusIdx = colMap['status'] ?? (nameIdx >= 0 ? 4 : 3);
    const intentIdx = colMap['intent'] ?? (nameIdx >= 0 ? 5 : 4);

    const id = cells[taskIdx] ?? '';
    const name = nameIdx >= 0 ? (cells[nameIdx] ?? '') : '';
    const phase = cells[phaseIdx] ?? '';
    const issueRaw = (cells[issueIdx] ?? '').replace(/\*\*/g, '').trim();
    const statusRaw = cells[statusIdx] ?? '';
    const intent = cells[intentIdx] ?? '';

    if (!id || id.toLowerCase() === 'task') continue;

    tasks.push({
      id: id.replace(/\*\*/g, '').trim(),
      ...(name ? { name: name.trim() } : {}),
      phase: phase.trim(),
      issue: issueRaw,
      status: normalizeTaskStatus(statusRaw),
      intent,
    });
  }

  return tasks;
}

function parsePhaseChecklist(lines: string[]): PhaseEntry[] {
  const phases: PhaseEntry[] = [];
  for (const line of lines) {
    const m = line.match(/^\s*-\s*\[([ xX])\]\s+(.+)/);
    if (!m) continue;
    phases.push({ checked: m[1].toLowerCase() === 'x', label: m[2].trim() });
  }
  return phases;
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

  const steps: Step[] = [];
  let currentStep: Step | null = null;
  let currentStage: Stage | null = null;
  let inStageSection = false;
  let stageLines: string[] = [];
  let stepLines: string[] = [];

  function finalizeStage(stage: Stage, accLines: string[]): void {
    stage.phases = parsePhaseChecklist(accLines);
    stage.tasks = parseTaskTable(accLines);
  }

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];

    if (/^##\s+/.test(line)) {
      if (currentStage && stageLines.length) { finalizeStage(currentStage, stageLines); stageLines = []; }
      if (currentStep) { steps.push(currentStep); currentStep = null; }
      currentStage = null;
      inStageSection = false;
      continue;
    }

    const stepMatch = line.match(/^###\s+Step\s+(\S+)\s+—\s+(.+)/);
    if (stepMatch) {
      if (currentStage && stageLines.length) { finalizeStage(currentStage, stageLines); stageLines = []; }
      if (currentStep) steps.push(currentStep);
      currentStep = {
        id: stepMatch[1].replace(/[^0-9]/g, '') || stepMatch[1],
        title: stepMatch[2].trim(),
        status: 'Draft',
        statusDetail: '',
        stages: [],
      };
      currentStage = null;
      stepLines = [];
      inStageSection = false;
      continue;
    }

    const stageMatch = line.match(/^####\s+Stage\s+([0-9.]+)\s+—\s+(.+)/);
    if (stageMatch) {
      if (currentStage && stageLines.length) finalizeStage(currentStage, stageLines);
      stageLines = [];
      currentStage = {
        id: stageMatch[1],
        title: stageMatch[2].trim(),
        status: 'Draft',
        statusDetail: '',
        phases: [],
        tasks: [],
      };
      if (currentStep) currentStep.stages.push(currentStage);
      inStageSection = true;
      continue;
    }

    if (currentStep && !inStageSection) {
      stepLines.push(line);
      const trimmed = line.trim();
      if (trimmed.startsWith('**Status:**') || trimmed.match(/^\*?\*?Status:\*?\*?\s/)) {
        const parsed = parseStatusLine(trimmed);
        if (parsed && !currentStep.status) {
          currentStep.status = parsed.status as Step['status'];
          currentStep.statusDetail = parsed.detail;
        }
      }
    }

    if (currentStage && inStageSection) {
      stageLines.push(line);
      const trimmed = line.trim();
      if (trimmed.startsWith('**Status:**') || trimmed.match(/^\*?\*?Status:\*?\*?\s/)) {
        const parsed = parseStatusLine(trimmed);
        if (parsed && !currentStage.status) {
          currentStage.status = parsed.status as Stage['status'];
          currentStage.statusDetail = parsed.detail;
        }
      }
    }
  }

  if (currentStage && stageLines.length) finalizeStage(currentStage, stageLines);
  if (currentStep) steps.push(currentStep);

  const allTasks: TaskRow[] = [];
  for (const step of steps) {
    for (const stage of step.stages) {
      for (const task of stage.tasks) {
        allTasks.push(task);
      }
    }
  }

  return { title, filename, overallStatus, overallStatusDetail, siblingWarnings, steps, allTasks };
}
