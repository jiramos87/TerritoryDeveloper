/**
 * render.mjs — Deterministic HTML renderer for master-plan progress data.
 *
 * Pure function: parseMasterPlan output array → docs/progress.html string.
 * No wall clock, no git, no filesystem I/O — caller provides PlanData[].
 *
 * Output: self-contained HTML with inline CSS, zero JS, zero external fetches.
 * Same PlanData input → identical HTML bytes (deterministic).
 */

// ─── Status helpers ────────────────────────────────────────────────────────

const STATUS_ORDER = ['In Progress', 'In Review', 'Draft', '_pending_', 'Done (archived)'];

/**
 * @param {string} status
 * @returns {string} CSS class name
 */
function statusClass(status) {
  if (status === 'Done (archived)' || status === 'Done') return 'status-done';
  if (status === 'In Progress') return 'status-inprogress';
  if (status === 'In Review') return 'status-inreview';
  if (status === 'Draft') return 'status-draft';
  if (status === '_pending_') return 'status-pending';
  return 'status-other';
}

/**
 * Escape HTML special chars (deterministic — no locale-dependent behavior).
 * @param {string} text
 * @returns {string}
 */
function esc(text) {
  return String(text)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

// ─── Per-plan card computations ───────────────────────────────────────────

/**
 * Compute progress stats for a plan.
 * @param {import('./parse.mjs').PlanData} plan
 * @returns {{ done: number, total: number, pct: number, breakdown: Record<string,number> }}
 */
function computeStats(plan) {
  const breakdown = {
    'Done (archived)': 0,
    'In Progress': 0,
    'In Review': 0,
    'Draft': 0,
    '_pending_': 0,
    'other': 0,
  };

  let done = 0;
  let total = 0;

  for (const task of plan.allTasks) {
    total++;
    const s = task.status;
    if (s === 'Done (archived)' || s === 'Done') {
      done++;
      breakdown['Done (archived)']++;
    } else if (breakdown[s] !== undefined) {
      breakdown[s]++;
    } else {
      breakdown['other']++;
    }
  }

  const pct = total > 0 ? Math.round((done / total) * 100) : 0;
  return { done, total, pct, breakdown };
}

/**
 * Find the active task (first non-Done task in document order across all stages).
 * @param {import('./parse.mjs').PlanData} plan
 * @returns {import('./parse.mjs').TaskRow | null}
 */
function findActiveTask(plan) {
  for (const step of plan.steps) {
    for (const stage of step.stages) {
      for (const task of stage.tasks) {
        const s = task.status;
        if (s !== 'Done (archived)' && s !== 'Done') {
          return task;
        }
      }
    }
  }
  return null;
}

/**
 * Find the active step (first step not all Done).
 * @param {import('./parse.mjs').PlanData} plan
 * @returns {import('./parse.mjs').Step | null}
 */
function findActiveStep(plan) {
  for (const step of plan.steps) {
    const allStepTasks = step.stages.flatMap(s => s.tasks);
    if (allStepTasks.length === 0) return step; // no tasks yet = still pending
    const allDone = allStepTasks.every(t => t.status === 'Done (archived)' || t.status === 'Done');
    if (!allDone) return step;
  }
  return plan.steps[plan.steps.length - 1] ?? null;
}

/**
 * Find the active stage (first stage with non-Done tasks).
 * @param {import('./parse.mjs').Step} step
 * @returns {import('./parse.mjs').Stage | null}
 */
function findActiveStage(step) {
  for (const stage of step.stages) {
    if (stage.tasks.length === 0) return stage;
    const allDone = stage.tasks.every(t => t.status === 'Done (archived)' || t.status === 'Done');
    if (!allDone) return stage;
  }
  return step.stages[step.stages.length - 1] ?? null;
}

// ─── HTML fragment builders ───────────────────────────────────────────────

/**
 * Render the progress bar HTML fragment.
 * @param {number} pct
 * @returns {string}
 */
function renderProgressBar(pct) {
  return `
    <div class="progress-bar-wrap" title="${pct}% complete">
      <div class="progress-bar-fill" style="width:${pct}%"></div>
    </div>
    <span class="progress-label">${pct}%</span>`.trimStart();
}

/**
 * Render the task status breakdown as a compact inline list.
 * @param {Record<string,number>} breakdown
 * @param {number} total
 * @returns {string}
 */
function renderBreakdown(breakdown, total) {
  const labels = [
    ['Done (archived)', 'Done'],
    ['In Progress', 'In Progress'],
    ['In Review', 'In Review'],
    ['Draft', 'Draft'],
    ['_pending_', 'pending'],
  ];
  const parts = labels
    .filter(([key]) => breakdown[key] > 0)
    .map(([key, label]) => `<span class="badge ${statusClass(key)}">${label}: ${breakdown[key]}</span>`);
  return parts.join(' ') + ` <span class="badge status-other">Total: ${total}</span>`;
}

/**
 * Render phase checklist for a stage.
 * @param {import('./parse.mjs').PhaseEntry[]} phases
 * @returns {string}
 */
function renderPhaseChecklist(phases) {
  if (!phases.length) return '<em class="muted">No phase checklist</em>';
  const items = phases.map(p => {
    const checkGlyph = p.checked ? '&#x2705;' : '&#x25A1;';
    const cls = p.checked ? 'phase-done' : 'phase-pending';
    return `<li class="${cls}">${checkGlyph} ${esc(p.label)}</li>`;
  });
  return `<ul class="phase-list">${items.join('')}</ul>`;
}

/**
 * Render sibling-coordination warnings.
 * @param {string[]} warnings
 * @returns {string}
 */
function renderSiblingWarnings(warnings) {
  if (!warnings.length) return '';
  const items = warnings.map(w => `<li>${esc(w)}</li>`);
  return `
  <div class="sibling-box">
    <strong>Sibling / parallel-work coordination:</strong>
    <ul>${items.join('')}</ul>
  </div>`;
}

/**
 * Render a single plan card.
 * @param {import('./parse.mjs').PlanData} plan
 * @returns {string}
 */
function renderPlanCard(plan) {
  const stats = computeStats(plan);
  const activeStep = findActiveStep(plan);
  const activeStage = activeStep ? findActiveStage(activeStep) : null;
  const activeTask = findActiveTask(plan);

  const stepLabel = activeStep
    ? `Step ${activeStep.id} — ${esc(activeStep.title)}`
    : '<em>—</em>';
  const stageLabel = activeStage
    ? `Stage ${activeStage.id} — ${esc(activeStage.title)}`
    : '<em>—</em>';
  const taskLabel = activeTask
    ? `${esc(activeTask.id)}: ${esc(activeTask.intent.slice(0, 120))}${activeTask.intent.length > 120 ? '…' : ''}`
    : '<em>All tasks done</em>';

  // Phase checklist: use active stage's phases
  const phaseChecklist = activeStage
    ? renderPhaseChecklist(activeStage.phases)
    : '<em class="muted">No active stage</em>';

  const statusStr = plan.overallStatus + (plan.overallStatusDetail ? ` — ${plan.overallStatusDetail}` : '');

  return `
<div class="plan-card">
  <div class="plan-header">
    <h2 class="plan-title">${esc(plan.title)}</h2>
    <span class="plan-status ${statusClass(plan.overallStatus)}">${esc(statusStr)}</span>
  </div>

  <div class="progress-row">
    ${renderProgressBar(stats.pct)}
    <span class="task-counts">${stats.done} / ${stats.total} tasks Done</span>
  </div>

  <div class="breakdown-row">
    ${renderBreakdown(stats.breakdown, stats.total)}
  </div>

  <table class="info-table">
    <tr><th>Active step</th><td>${stepLabel}</td></tr>
    <tr><th>Active stage</th><td>${stageLabel}</td></tr>
    <tr><th>Active task</th><td>${taskLabel}</td></tr>
  </table>

  <div class="phase-section">
    <strong>Phase checklist (active stage):</strong>
    ${phaseChecklist}
  </div>

  ${renderSiblingWarnings(plan.siblingWarnings)}
</div>`;
}

/**
 * Render the overall header summary.
 * @param {import('./parse.mjs').PlanData[]} plans
 * @returns {string}
 */
function renderOverallHeader(plans) {
  let totalDone = 0;
  let totalTasks = 0;
  for (const plan of plans) {
    const stats = computeStats(plan);
    totalDone += stats.done;
    totalTasks += stats.total;
  }
  const combinedPct = totalTasks > 0 ? Math.round((totalDone / totalTasks) * 100) : 0;

  return `
<header class="overall-header">
  <h1>Territory Developer — Master Plan Progress</h1>
  <div class="overall-summary">
    <div class="progress-row">
      ${renderProgressBar(combinedPct)}
      <span class="task-counts">${totalDone} / ${totalTasks} tasks Done across ${plans.length} plan${plans.length !== 1 ? 's' : ''}</span>
    </div>
  </div>
</header>`;
}

// ─── CSS ───────────────────────────────────────────────────────────────────

const CSS = `
*, *::before, *::after { box-sizing: border-box; }
body {
  font-family: system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif;
  background: #0f0f0f;
  color: #d4d4d4;
  margin: 0;
  padding: 1rem 2rem 3rem;
  line-height: 1.5;
}
h1 { margin: 0 0 0.5rem; font-size: 1.4rem; color: #e8e8e8; }
h2.plan-title { margin: 0; font-size: 1.1rem; color: #e8e8e8; }
.overall-header {
  background: #1a1a2e;
  border: 1px solid #2a2a5a;
  border-radius: 8px;
  padding: 1.25rem 1.5rem;
  margin-bottom: 1.5rem;
}
.plan-card {
  background: #181818;
  border: 1px solid #2c2c2c;
  border-radius: 8px;
  padding: 1.25rem 1.5rem;
  margin-bottom: 1.25rem;
}
.plan-header {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 0.75rem;
  flex-wrap: wrap;
}
.plan-status {
  font-size: 0.78rem;
  padding: 0.2rem 0.55rem;
  border-radius: 4px;
  white-space: nowrap;
}
.progress-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 0.5rem;
  flex-wrap: wrap;
}
.progress-bar-wrap {
  flex: 1;
  min-width: 120px;
  max-width: 400px;
  height: 14px;
  background: #2a2a2a;
  border-radius: 7px;
  overflow: hidden;
}
.progress-bar-fill {
  height: 100%;
  background: linear-gradient(90deg, #2d8a4e, #40bf72);
  border-radius: 7px;
  transition: width 0s;
}
.progress-label {
  font-size: 0.88rem;
  font-weight: 600;
  color: #a0e0b0;
  min-width: 3rem;
}
.task-counts { font-size: 0.82rem; color: #888; }
.breakdown-row { margin-bottom: 0.85rem; }
.badge {
  display: inline-block;
  font-size: 0.74rem;
  padding: 0.15rem 0.45rem;
  border-radius: 4px;
  margin-right: 0.25rem;
  font-weight: 500;
}
.info-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.84rem;
  margin-bottom: 0.85rem;
}
.info-table th {
  text-align: left;
  color: #888;
  font-weight: 500;
  width: 130px;
  padding: 0.25rem 0.5rem 0.25rem 0;
  white-space: nowrap;
}
.info-table td { padding: 0.25rem 0; color: #ccc; }
.phase-section { margin-bottom: 0.85rem; font-size: 0.85rem; }
.phase-list {
  margin: 0.35rem 0 0 1rem;
  padding: 0;
  list-style: none;
}
.phase-list li { margin: 0.15rem 0; }
.phase-done { color: #72c28a; }
.phase-pending { color: #999; }
.muted { color: #666; font-style: italic; }
.sibling-box {
  background: #1a1a0a;
  border: 1px solid #4a4a00;
  border-radius: 5px;
  padding: 0.7rem 1rem;
  font-size: 0.8rem;
  color: #c0b870;
}
.sibling-box ul { margin: 0.3rem 0 0 1rem; padding: 0; }
.sibling-box li { margin: 0.2rem 0; }

/* Status colors */
.status-done      { background: #1a3a25; color: #72c28a; border: 1px solid #2d6a3a; }
.status-inprogress{ background: #1a2a40; color: #70a0e0; border: 1px solid #2a4a70; }
.status-inreview  { background: #2a1a40; color: #b080e0; border: 1px solid #4a2a70; }
.status-draft     { background: #2a2a1a; color: #c0b060; border: 1px solid #5a5a2a; }
.status-pending   { background: #222; color: #888; border: 1px solid #444; }
.status-other     { background: #222; color: #aaa; border: 1px solid #444; }
`.trim();

// ─── Main render entry point ───────────────────────────────────────────────

/**
 * Render all plans to a self-contained HTML string.
 *
 * @param {import('./parse.mjs').PlanData[]} plans  array from parseMasterPlan calls
 * @returns {string}  complete HTML document (deterministic bytes for same input)
 */
export function renderHtml(plans) {
  const cards = plans.map(renderPlanCard).join('\n');
  const header = renderOverallHeader(plans);

  return `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>Territory Developer — Progress</title>
<style>
${CSS}
</style>
</head>
<body>
${header}
<main>
${cards}
</main>
<footer style="font-size:0.75rem;color:#555;margin-top:2rem;text-align:center">
  Generated by <code>npm run progress</code> (tools/progress-tracker) — static snapshot, no live data.
</footer>
</body>
</html>
`;
}
