/**
 * parse.mjs — Master-plan Markdown parser.
 *
 * Pure function: master-plan *.md bytes → typed PlanData object.
 * No wall clock, no git, no filesystem I/O — caller provides raw Markdown string.
 *
 * Output schema (TypeScript-style for documentation):
 *
 * type TaskStatus =
 *   | '_pending_'
 *   | 'Draft'
 *   | 'In Review'
 *   | 'In Progress'
 *   | 'Done (archived)'
 *   | 'Done';        // short form found in some rows
 *
 * type HierarchyStatus =
 *   | 'Draft'
 *   | 'In Review'
 *   | 'In Progress'  // may have trailing " — {active child}" detail
 *   | 'Final';
 *
 * interface TaskRow {
 *   id: string;           // e.g. "T1.1.1"
 *   phase: string;        // e.g. "1"
 *   issue: string;        // e.g. "TECH-87" or "_pending_"
 *   status: TaskStatus;
 *   intent: string;
 * }
 *
 * interface PhaseEntry {
 *   checked: boolean;     // true = [x], false = [ ]
 *   label: string;        // phase label text
 * }
 *
 * interface Stage {
 *   id: string;           // e.g. "1.1"
 *   title: string;
 *   status: HierarchyStatus;
 *   statusDetail: string; // text after " — " in status line, if any
 *   phases: PhaseEntry[];
 *   tasks: TaskRow[];
 * }
 *
 * interface Step {
 *   id: string;           // e.g. "1"
 *   title: string;
 *   status: HierarchyStatus;
 *   statusDetail: string;
 *   stages: Stage[];
 * }
 *
 * interface PlanData {
 *   title: string;              // first # heading
 *   overallStatus: string;      // raw status line from opening blockquote
 *   overallStatusDetail: string;// text after " — " in overall status, if any
 *   siblingWarnings: string[];  // blockquote lines mentioning sibling orchestrators
 *   steps: Step[];
 *   allTasks: TaskRow[];        // flat list across all steps/stages (convenience)
 * }
 */

/**
 * STATUS_ENUM maps raw cell text to a canonical TaskStatus string.
 * The master plans use several variants.
 */
const TASK_STATUS_CANON = {
  '_pending_': '_pending_',
  'Draft': 'Draft',
  'In Review': 'In Review',
  'In Progress': 'In Progress',
  'Done (archived)': 'Done (archived)',
  'Done': 'Done (archived)',
};

/**
 * Normalize task status cell text to a canonical value.
 * @param {string} raw
 * @returns {string}
 */
function normalizeTaskStatus(raw) {
  const trimmed = raw.trim();
  return TASK_STATUS_CANON[trimmed] ?? trimmed;
}

/**
 * Parse the hierarchy status from a "**Status:** ..." line.
 * Returns { status, detail } where detail is the part after " — ", if any.
 * @param {string} line
 * @returns {{ status: string, detail: string } | null}
 */
function parseStatusLine(line) {
  // Match: **Status:** In Progress — Stage 1.3
  //    or: **Status:** Draft
  //    or: Status: In Progress — ...
  const m = line.match(/\*?\*?Status:\*?\*?\s+(.+)/);
  if (!m) return null;
  const raw = m[1].trim();
  const dashIdx = raw.indexOf(' — ');
  if (dashIdx !== -1) {
    return { status: raw.slice(0, dashIdx).trim(), detail: raw.slice(dashIdx + 3).trim() };
  }
  // also handle plain " - " separator (some lines)
  const dashIdx2 = raw.indexOf(' - ');
  if (dashIdx2 !== -1) {
    return { status: raw.slice(0, dashIdx2).trim(), detail: raw.slice(dashIdx2 + 3).trim() };
  }
  return { status: raw, detail: '' };
}

/**
 * Parse a Markdown table row into an array of trimmed cell strings.
 * Returns null if the line is not a table row.
 * @param {string} line
 * @returns {string[] | null}
 */
function parseTableRow(line) {
  const trimmed = line.trim();
  if (!trimmed.startsWith('|')) return null;
  // Split on '|', drop first and last empty segments
  const cells = trimmed.split('|').slice(1, -1).map(c => c.trim());
  return cells;
}

/**
 * Detect separator row (---|---|...)
 * @param {string[]} cells
 * @returns {boolean}
 */
function isSeparatorRow(cells) {
  return cells.every(c => /^[-: ]+$/.test(c));
}

/**
 * Parse task table rows from lines.
 * Expected columns: Task | Phase | Issue | Status | Intent
 * @param {string[]} lines   subset of lines (table body only)
 * @returns {TaskRow[]}
 */
function parseTaskTable(lines) {
  const tasks = [];
  let headerParsed = false;
  let colMap = null; // { task, phase, issue, status, intent } → column index

  for (const line of lines) {
    const cells = parseTableRow(line);
    if (!cells) continue;
    if (isSeparatorRow(cells)) continue;

    if (!headerParsed) {
      // This is the header row — determine column indices
      colMap = {};
      cells.forEach((c, i) => {
        const key = c.toLowerCase().replace(/[^a-z]/g, '');
        colMap[key] = i;
      });
      headerParsed = true;
      continue;
    }

    if (!colMap) continue;
    if (cells.length < 3) continue;

    // Use flexible column detection
    const taskIdx = colMap['task'] ?? 0;
    const phaseIdx = colMap['phase'] ?? 1;
    const issueIdx = colMap['issue'] ?? 2;
    const statusIdx = colMap['status'] ?? 3;
    const intentIdx = colMap['intent'] ?? 4;

    const id = cells[taskIdx] ?? '';
    const phase = cells[phaseIdx] ?? '';
    // Strip bold markers from issue cell: **TECH-87** → TECH-87
    const issueRaw = (cells[issueIdx] ?? '').replace(/\*\*/g, '').trim();
    const statusRaw = cells[statusIdx] ?? '';
    const intent = cells[intentIdx] ?? '';

    // Skip empty or header-like rows
    if (!id || id.toLowerCase() === 'task') continue;

    tasks.push({
      id: id.replace(/\*\*/g, '').trim(),
      phase: phase.trim(),
      issue: issueRaw,
      status: normalizeTaskStatus(statusRaw),
      intent: intent,
    });
  }

  return tasks;
}

/**
 * Parse phase checklist bullets from lines.
 * Lines matching "- [ ] Phase ..." or "- [x] Phase ..."
 * @param {string[]} lines
 * @returns {PhaseEntry[]}
 */
function parsePhaseChecklist(lines) {
  const phases = [];
  for (const line of lines) {
    const m = line.match(/^\s*-\s*\[([ xX])\]\s+(.+)/);
    if (!m) continue;
    phases.push({
      checked: m[1].toLowerCase() === 'x',
      label: m[2].trim(),
    });
  }
  return phases;
}

/**
 * Extract sibling-orchestrator blockquote warnings from the document.
 * These are blockquote lines (> ...) that mention sibling orchestrators
 * (typically containing "blip-master-plan", "sprite-gen-master-plan",
 * "multi-scale-master-plan", or "Parallel-work rule").
 * @param {string[]} lines  all document lines
 * @returns {string[]}
 */
function extractSiblingWarnings(lines) {
  const warnings = [];
  for (const line of lines) {
    const trimmed = line.trim();
    if (!trimmed.startsWith('>')) continue;
    const content = trimmed.replace(/^>\s*/, '');
    // Capture lines about sibling orchestrators or parallel-work rule
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

/**
 * Main parser entry point.
 *
 * @param {string} markdown  raw Markdown content of a master-plan file
 * @param {string} filename  basename of the file (used as fallback title)
 * @returns {PlanData}
 */
export function parseMasterPlan(markdown, filename = '') {
  const lines = markdown.split('\n');

  // --- Overall title (first # heading) ---
  let title = filename;
  for (const line of lines) {
    if (line.startsWith('# ')) {
      title = line.slice(2).trim();
      break;
    }
  }

  // --- Overall status (from opening blockquote, > **Status:** line) ---
  let overallStatus = '';
  let overallStatusDetail = '';
  for (const line of lines) {
    const trimmed = line.trim();
    if (trimmed.startsWith('>')) {
      const content = trimmed.replace(/^>\s*/, '');
      if (content.includes('**Status:**') || content.startsWith('Status:')) {
        const parsed = parseStatusLine(content);
        if (parsed) {
          overallStatus = parsed.status;
          overallStatusDetail = parsed.detail;
          break;
        }
      }
    }
  }

  // --- Sibling warnings ---
  const siblingWarnings = extractSiblingWarnings(lines);

  // --- Parse steps, stages, tasks ---
  // Strategy: scan lines for heading levels
  //   ### Step N — title
  //   #### Stage N.M — title
  // Within each stage: collect phase bullets and task table rows

  const steps = [];
  let currentStep = null;
  let currentStage = null;
  let inTaskTable = false;
  let inStageSection = false;
  let stageLines = [];
  let stepLines = [];

  // Helper: finalize a stage's phases + tasks from accumulated lines
  function finalizeStage(stage, accLines) {
    stage.phases = parsePhaseChecklist(accLines);
    stage.tasks = parseTaskTable(accLines);
  }

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];

    // Detect ### Step heading
    const stepMatch = line.match(/^###\s+Step\s+(\S+)\s+—\s+(.+)/);
    if (stepMatch) {
      // Close previous stage
      if (currentStage && stageLines.length) {
        finalizeStage(currentStage, stageLines);
        stageLines = [];
      }
      // Close previous step
      if (currentStep) {
        steps.push(currentStep);
      }
      currentStep = {
        id: stepMatch[1].replace(/[^0-9]/g, '') || stepMatch[1],
        title: stepMatch[2].trim(),
        status: '',
        statusDetail: '',
        stages: [],
      };
      currentStage = null;
      stepLines = [];
      inStageSection = false;
      continue;
    }

    // Detect #### Stage heading
    const stageMatch = line.match(/^####\s+Stage\s+([0-9.]+)\s+—\s+(.+)/);
    if (stageMatch) {
      // Close previous stage
      if (currentStage && stageLines.length) {
        finalizeStage(currentStage, stageLines);
      }
      stageLines = [];
      currentStage = {
        id: stageMatch[1],
        title: stageMatch[2].trim(),
        status: '',
        statusDetail: '',
        phases: [],
        tasks: [],
      };
      if (currentStep) {
        currentStep.stages.push(currentStage);
      }
      inStageSection = true;
      continue;
    }

    // Collect lines for current step (before any stage) to find step status
    if (currentStep && !inStageSection) {
      stepLines.push(line);
      // Parse step status
      const trimmed = line.trim();
      if (trimmed.startsWith('**Status:**') || trimmed.match(/^\*?\*?Status:\*?\*?\s/)) {
        const parsed = parseStatusLine(trimmed);
        if (parsed && !currentStep.status) {
          currentStep.status = parsed.status;
          currentStep.statusDetail = parsed.detail;
        }
      }
    }

    // Collect lines for current stage
    if (currentStage && inStageSection) {
      stageLines.push(line);
      // Parse stage status
      const trimmed = line.trim();
      if (trimmed.startsWith('**Status:**') || trimmed.match(/^\*?\*?Status:\*?\*?\s/)) {
        const parsed = parseStatusLine(trimmed);
        if (parsed && !currentStage.status) {
          currentStage.status = parsed.status;
          currentStage.statusDetail = parsed.detail;
        }
      }
    }
  }

  // Finalize last stage + step
  if (currentStage && stageLines.length) {
    finalizeStage(currentStage, stageLines);
  }
  if (currentStep) {
    steps.push(currentStep);
  }

  // Flat task list
  const allTasks = [];
  for (const step of steps) {
    for (const stage of step.stages) {
      for (const task of stage.tasks) {
        allTasks.push(task);
      }
    }
  }

  return {
    title,
    overallStatus,
    overallStatusDetail,
    siblingWarnings,
    steps,
    allTasks,
  };
}
