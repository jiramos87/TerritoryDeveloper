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

/**
 * Pick an emoji icon for the plan based on its title.
 * @param {string} title
 * @returns {string}
 */
function planIcon(title) {
  const t = title.toLowerCase();
  if (t.includes('blip') || t.includes('audio')) return '🎵';
  if (t.includes('multi-scale') || t.includes('simulation')) return '🗺️';
  if (t.includes('sprite') || t.includes('art')) return '🎨';
  return '📋';
}

/**
 * Build relative link from docs/ to ia/projects/{filename}.
 * @param {string} filename  e.g. "blip-master-plan.md"
 * @returns {string}
 */
function planDocLink(filename) {
  if (!filename) return '';
  return `../ia/projects/${filename}`;
}

// ─── Per-plan card computations ───────────────────────────────────────────

/**
 * Compute global step-based progress percentage for a plan.
 * Final steps count as 1.0; In Progress steps count partial (task-based); Draft = 0.
 * This avoids the misleading 100% that appears when all *decomposed* tasks are done
 * but most steps are still Draft/undecomposed.
 * @param {import('./parse.mjs').PlanData} plan
 * @returns {number} 0–100
 */
function computeGlobalStepPct(plan) {
  if (plan.steps.length === 0) return 0;
  const total = plan.steps.length;
  let weightDone = 0;
  for (const step of plan.steps) {
    if (step.status === 'Final') {
      weightDone += 1;
    } else if (step.status === 'In Progress') {
      const stepTasks = step.stages.flatMap(s => s.tasks);
      if (stepTasks.length > 0) {
        const done = stepTasks.filter(t => t.status === 'Done (archived)' || t.status === 'Done').length;
        weightDone += done / stepTasks.length;
      }
      // undecomposed In Progress step: 0 credit
    }
    // Draft: 0
  }
  return Math.round((weightDone / total) * 100);
}

/**
 * Compute task-level stats for a single step (active step progress).
 * @param {import('./parse.mjs').Step} step
 * @returns {{ done: number, total: number, pct: number }}
 */
function computeStepTaskStats(step) {
  const tasks = step.stages.flatMap(s => s.tasks);
  const done = tasks.filter(t => t.status === 'Done (archived)' || t.status === 'Done').length;
  const pct = tasks.length > 0 ? Math.round((done / tasks.length) * 100) : 0;
  return { done, total: tasks.length, pct };
}

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
    if (allStepTasks.length === 0) return step;
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
 * Render a segmented progress bar (done/draft/inprogress/pending segments).
 * @param {number} pct
 * @param {{ done: number, total: number, breakdown: Record<string,number> }} stats
 * @returns {string}
 */
function renderSegmentedBar(pct, stats) {
  const { total, breakdown } = stats;
  if (total === 0) return `<div class="seg-bar"><div class="seg-empty"></div></div>`;

  const donePct = Math.round((breakdown['Done (archived)'] / total) * 100);
  const inprogPct = Math.round((breakdown['In Progress'] / total) * 100);
  const reviewPct = Math.round((breakdown['In Review'] / total) * 100);
  const draftPct = Math.round((breakdown['Draft'] / total) * 100);
  const pendingPct = Math.max(0, 100 - donePct - inprogPct - reviewPct - draftPct);

  const segs = [
    donePct > 0 ? `<div class="seg seg-done" style="width:${donePct}%" title="${breakdown['Done (archived)']} Done"></div>` : '',
    inprogPct > 0 ? `<div class="seg seg-inprogress" style="width:${inprogPct}%" title="${breakdown['In Progress']} In Progress"></div>` : '',
    reviewPct > 0 ? `<div class="seg seg-inreview" style="width:${reviewPct}%" title="${breakdown['In Review']} In Review"></div>` : '',
    draftPct > 0 ? `<div class="seg seg-draft" style="width:${draftPct}%" title="${breakdown['Draft']} Draft"></div>` : '',
    pendingPct > 0 ? `<div class="seg seg-pending" style="width:${pendingPct}%" title="${breakdown['_pending_']} Pending"></div>` : '',
  ].filter(Boolean);

  return `<div class="seg-bar">${segs.join('')}</div>`;
}

/**
 * Render the stat chip row.
 * @param {Record<string,number>} breakdown
 * @param {number} total
 * @returns {string}
 */
function renderStatChips(breakdown, total) {
  const chips = [];
  if (breakdown['Done (archived)'] > 0)
    chips.push(`<span class="chip chip-done">✅ ${breakdown['Done (archived)']} Done</span>`);
  if (breakdown['In Progress'] > 0)
    chips.push(`<span class="chip chip-inprogress">🔄 ${breakdown['In Progress']} Active</span>`);
  if (breakdown['In Review'] > 0)
    chips.push(`<span class="chip chip-inreview">👁 ${breakdown['In Review']} Review</span>`);
  if (breakdown['Draft'] > 0)
    chips.push(`<span class="chip chip-draft">📝 ${breakdown['Draft']} Draft</span>`);
  if (breakdown['_pending_'] > 0)
    chips.push(`<span class="chip chip-pending">⏳ ${breakdown['_pending_']} Pending</span>`);
  chips.push(`<span class="chip chip-total">📋 ${total} Total</span>`);
  return chips.join('');
}

/**
 * Render phase checklist as compact pills.
 * @param {import('./parse.mjs').PhaseEntry[]} phases
 * @returns {string}
 */
function renderPhasePills(phases) {
  if (!phases.length) return '<span class="phase-pill phase-muted">No phases</span>';
  return phases.map(p => {
    const icon = p.checked ? '✅' : '☐';
    const cls = p.checked ? 'phase-pill phase-done' : 'phase-pill phase-pending';
    // Shorten label: strip "Phase N — " prefix if present, keep rest concise
    const label = p.label.replace(/^Phase\s+\d+\s+[—-]\s+/, '');
    const short = label.length > 48 ? label.slice(0, 45) + '…' : label;
    return `<span class="${cls}" title="${esc(p.label)}">${icon} ${esc(short)}</span>`;
  }).join('');
}

/**
 * Render sibling-coordination warnings as a collapsible details block.
 * @param {string[]} warnings
 * @returns {string}
 */
function renderSiblingWarnings(warnings) {
  if (!warnings.length) return '';
  const count = warnings.filter(w =>
    w.includes('master-plan.md') && !w.includes('Parallel-work')
  ).length;
  const items = warnings.map(w => `<li>${esc(w)}</li>`);
  return `
  <details class="sibling-details">
    <summary>🔗 Sibling coordination <span class="sibling-count">${count} orchestrators in flight</span></summary>
    <ul>${items.join('')}</ul>
  </details>`;
}

/**
 * Render an overall mini-bar for the header (thin, compact).
 * @param {number} pct
 * @returns {string}
 */
function renderMiniBar(pct) {
  return `<div class="mini-bar-wrap"><div class="mini-bar-fill" style="width:${pct}%"></div></div>`;
}

/**
 * Render a single plan card.
 * @param {import('./parse.mjs').PlanData} plan
 * @returns {string}
 */
function renderPlanCard(plan) {
  const stats = computeStats(plan);
  const globalPct = computeGlobalStepPct(plan);
  const activeStep = findActiveStep(plan);
  const activeStage = activeStep ? findActiveStage(activeStep) : null;
  const stepStats = activeStep ? computeStepTaskStats(activeStep) : { done: 0, total: 0, pct: 0 };
  const activeTask = findActiveTask(plan);

  const icon = planIcon(plan.title);
  const docLink = planDocLink(plan.filename);

  // Step / stage / task labels (compact)
  const stepShort = activeStep
    ? `Step ${activeStep.id} · ${esc(activeStep.title.length > 30 ? activeStep.title.slice(0, 28) + '…' : activeStep.title)}`
    : '—';
  const stageShort = activeStage
    ? `Stage ${activeStage.id} · ${esc(activeStage.title.length > 30 ? activeStage.title.slice(0, 28) + '…' : activeStage.title)}`
    : '—';

  let taskShort;
  let taskTitle = '';
  const stepHasNoTasks = activeStep && activeStep.stages.flatMap(s => s.tasks).length === 0;
  if (!activeTask && stepHasNoTasks) {
    taskShort = '<em>Decomposition pending</em>';
  } else if (!activeTask) {
    taskShort = '<em>All tasks done</em>';
  } else {
    const displayName = activeTask.name && activeTask.name !== '_pending_'
      ? activeTask.name
      : (activeTask.intent.slice(0, 50) + (activeTask.intent.length > 50 ? '…' : ''));
    taskTitle = esc(activeTask.intent.slice(0, 200));
    taskShort = `<span class="task-id">${esc(activeTask.id)}</span> <span class="task-name">${esc(displayName)}</span>`;
  }

  const phaseChecklist = activeStage
    ? renderPhasePills(activeStage.phases)
    : '<span class="phase-pill phase-muted">No active stage</span>';

  const statusStr = plan.overallStatus + (plan.overallStatusDetail ? ` — ${plan.overallStatusDetail}` : '');
  const sClass = statusClass(plan.overallStatus);

  const statusEmoji = plan.overallStatus === 'Final' ? '✅' :
    plan.overallStatus === 'In Progress' ? '🔄' :
    plan.overallStatus === 'In Review' ? '👁' :
    plan.overallStatus === 'Draft' ? '📝' : '⏳';

  const globalPctColor = globalPct >= 80 ? '#40bf72' : globalPct >= 40 ? '#70a0e0' : '#c0b060';
  const stepPctColor = stepStats.pct >= 80 ? '#40bf72' : stepStats.pct >= 40 ? '#70a0e0' : '#c0b060';
  const stepPctLabel = activeStep ? `Step ${activeStep.id}: ${stepStats.pct}%` : '';

  return `
<div class="plan-card">
  <div class="plan-header">
    <span class="plan-icon">${icon}</span>
    <div class="plan-title-group">
      <h2 class="plan-title">${esc(plan.title)}</h2>
      ${docLink ? `<a class="plan-doc-link" href="${esc(docLink)}">📄 plan doc</a>` : ''}
    </div>
    <span class="plan-status ${sClass}">${statusEmoji} ${esc(statusStr)}</span>
    <div class="plan-pct-group">
      <div class="plan-pct" style="color:${globalPctColor}" title="Global plan progress (step-weighted)">${globalPct}%</div>
      ${stepPctLabel ? `<div class="plan-step-pct" style="color:${stepPctColor}" title="Active step task progress">${esc(stepPctLabel)}</div>` : ''}
    </div>
  </div>

  <div class="seg-row">
    ${renderSegmentedBar(stats.pct, stats)}
    <span class="seg-count">${stats.done} / ${stats.total}</span>
  </div>

  <div class="stat-chips">
    ${renderStatChips(stats.breakdown, stats.total)}
  </div>

  <div class="ctx-row">
    <span class="ctx-label">📍</span>
    <span class="ctx-step">${stepShort}</span>
    <span class="ctx-arrow">›</span>
    <span class="ctx-stage">${stageShort}</span>
    <span class="ctx-arrow">›</span>
    <span class="ctx-task" title="${taskTitle}">🎯 ${taskShort}</span>
  </div>

  <div class="phase-row">
    <span class="phase-row-label">Phases:</span>
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
  // Overall % = step-weighted average across all plans (same basis as plan cards)
  let totalStepWeight = 0;
  let totalStepDone = 0;
  for (const plan of plans) {
    totalStepWeight += plan.steps.length;
    // Accumulate weighted done from computeGlobalStepPct internals
    for (const step of plan.steps) {
      if (step.status === 'Final') {
        totalStepDone += 1;
      } else if (step.status === 'In Progress') {
        const stepTasks = step.stages.flatMap(s => s.tasks);
        if (stepTasks.length > 0) {
          const done = stepTasks.filter(t => t.status === 'Done (archived)' || t.status === 'Done').length;
          totalStepDone += done / stepTasks.length;
        }
      }
    }
  }
  const combinedPct = totalStepWeight > 0 ? Math.round((totalStepDone / totalStepWeight) * 100) : 0;

  // Task counts shown as secondary detail only
  let totalDone = 0;
  let totalTasks = 0;
  for (const plan of plans) {
    const stats = computeStats(plan);
    totalDone += stats.done;
    totalTasks += stats.total;
  }

  // Per-plan mini summary — use global step pct to match card display
  const planMinis = plans.map(plan => {
    const pct = computeGlobalStepPct(plan);
    const icon = planIcon(plan.title);
    const shortTitle = plan.title.replace(/\s*—\s*Master Plan.*$/, '').replace(/Isometric /, '');
    return `
    <div class="plan-mini">
      <span class="mini-icon">${icon}</span>
      <div class="mini-info">
        <span class="mini-title">${esc(shortTitle)}</span>
        ${renderMiniBar(pct)}
        <span class="mini-pct">${pct}%</span>
      </div>
    </div>`;
  }).join('');

  return `
<header class="overall-header">
  <div class="overall-top">
    <div class="overall-left">
      <h1>🏙️ Territory Developer</h1>
      <div class="overall-subtitle">Master Plan Progress</div>
    </div>
    <div class="overall-right">
      <div class="overall-pct">${combinedPct}%</div>
      <div class="overall-count">${totalDone} / ${totalTasks} tasks</div>
    </div>
  </div>
  <div class="overall-seg-row">
    <div class="overall-seg-bar">
      <div class="overall-seg-fill" style="width:${combinedPct}%"></div>
    </div>
  </div>
  <div class="plan-minis">
    ${planMinis}
  </div>
</header>`;
}

// ─── CSS ───────────────────────────────────────────────────────────────────

const CSS = `
*, *::before, *::after { box-sizing: border-box; }
body {
  font-family: system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif;
  background: #0a0a0f;
  color: #d0d0d8;
  margin: 0;
  padding: 1rem 2rem 3rem;
  line-height: 1.5;
}

/* ── Overall header ── */
.overall-header {
  background: linear-gradient(135deg, #0d1a2e 0%, #1a1a2e 50%, #1a0d2e 100%);
  border: 1px solid #2a2a5a;
  border-radius: 12px;
  padding: 1.5rem 1.75rem 1.25rem;
  margin-bottom: 1.5rem;
}
.overall-top {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  margin-bottom: 0.75rem;
}
.overall-left h1 {
  margin: 0 0 0.1rem;
  font-size: 1.5rem;
  color: #e8e8f8;
  letter-spacing: -0.01em;
}
.overall-subtitle {
  font-size: 0.8rem;
  color: #7070a0;
  text-transform: uppercase;
  letter-spacing: 0.08em;
}
.overall-right {
  text-align: right;
}
.overall-pct {
  font-size: 2.2rem;
  font-weight: 700;
  color: #a0e0b0;
  line-height: 1;
}
.overall-count {
  font-size: 0.78rem;
  color: #7070a0;
}
.overall-seg-bar {
  height: 6px;
  background: #1e1e3a;
  border-radius: 3px;
  overflow: hidden;
  margin-bottom: 1rem;
}
.overall-seg-fill {
  height: 100%;
  background: linear-gradient(90deg, #2d8a4e, #40bf72);
  border-radius: 3px;
}
.plan-minis {
  display: flex;
  gap: 1rem;
  flex-wrap: wrap;
}
.plan-mini {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  background: rgba(255,255,255,0.04);
  border: 1px solid rgba(255,255,255,0.08);
  border-radius: 8px;
  padding: 0.45rem 0.75rem;
  flex: 1;
  min-width: 160px;
}
.mini-icon { font-size: 1.2rem; }
.mini-info { flex: 1; min-width: 0; }
.mini-title {
  display: block;
  font-size: 0.76rem;
  color: #b0b0c8;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  margin-bottom: 0.2rem;
}
.mini-bar-wrap {
  height: 4px;
  background: #1e1e3a;
  border-radius: 2px;
  overflow: hidden;
  margin-bottom: 0.15rem;
}
.mini-bar-fill {
  height: 100%;
  background: linear-gradient(90deg, #2d8a4e, #40bf72);
  border-radius: 2px;
}
.mini-pct {
  font-size: 0.7rem;
  color: #70a080;
  font-weight: 600;
}

/* ── Plan cards ── */
.plan-card {
  background: #111118;
  border: 1px solid #222230;
  border-radius: 12px;
  padding: 1.25rem 1.5rem;
  margin-bottom: 1.25rem;
}

/* ── Plan header ── */
.plan-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 0.85rem;
  flex-wrap: wrap;
}
.plan-icon { font-size: 1.5rem; flex-shrink: 0; }
.plan-title-group {
  flex: 1;
  min-width: 0;
}
h2.plan-title {
  margin: 0;
  font-size: 1.0rem;
  color: #e8e8f8;
  letter-spacing: -0.01em;
}
.plan-doc-link {
  font-size: 0.72rem;
  color: #5080c0;
  text-decoration: none;
  opacity: 0.8;
}
.plan-doc-link:hover { opacity: 1; text-decoration: underline; }
.plan-status {
  font-size: 0.74rem;
  padding: 0.2rem 0.55rem;
  border-radius: 20px;
  white-space: nowrap;
  flex-shrink: 0;
}
.plan-pct-group {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  flex-shrink: 0;
  min-width: 4rem;
}
.plan-pct {
  font-size: 1.6rem;
  font-weight: 700;
  text-align: right;
  line-height: 1;
}
.plan-step-pct {
  font-size: 0.7rem;
  font-weight: 600;
  text-align: right;
  opacity: 0.75;
  margin-top: 0.15rem;
  white-space: nowrap;
}

/* ── Segmented bar ── */
.seg-row {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  margin-bottom: 0.7rem;
}
.seg-bar {
  flex: 1;
  height: 10px;
  background: #1e1e2e;
  border-radius: 5px;
  overflow: hidden;
  display: flex;
}
.seg {
  height: 100%;
  transition: width 0s;
}
.seg-done      { background: linear-gradient(90deg, #2d7a44, #40bf72); }
.seg-inprogress{ background: linear-gradient(90deg, #1a4a80, #60a0e0); }
.seg-inreview  { background: linear-gradient(90deg, #4a1a80, #b080e0); }
.seg-draft     { background: linear-gradient(90deg, #5a5a10, #c0b060); }
.seg-pending   { background: #2a2a3a; }
.seg-empty     { flex: 1; background: #1e1e2e; }
.seg-count {
  font-size: 0.78rem;
  color: #7070a0;
  white-space: nowrap;
}

/* ── Stat chips ── */
.stat-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem;
  margin-bottom: 0.85rem;
}
.chip {
  font-size: 0.72rem;
  padding: 0.15rem 0.5rem;
  border-radius: 20px;
  font-weight: 500;
  white-space: nowrap;
}
.chip-done      { background: #1a3a25; color: #72c28a; border: 1px solid #2d6a3a; }
.chip-inprogress{ background: #1a2a40; color: #70a0e0; border: 1px solid #2a4a70; }
.chip-inreview  { background: #2a1a40; color: #b080e0; border: 1px solid #4a2a70; }
.chip-draft     { background: #2a2a1a; color: #c0b060; border: 1px solid #5a5a2a; }
.chip-pending   { background: #1e1e2e; color: #6070a0; border: 1px solid #2a2a40; }
.chip-total     { background: #1a1a2a; color: #8080b0; border: 1px solid #2a2a4a; }

/* ── Context breadcrumb ── */
.ctx-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.35rem;
  font-size: 0.81rem;
  background: #0d0d18;
  border: 1px solid #1e1e30;
  border-radius: 8px;
  padding: 0.5rem 0.75rem;
  margin-bottom: 0.7rem;
}
.ctx-label { color: #5060a0; font-size: 0.85rem; flex-shrink: 0; }
.ctx-step  { color: #8080b0; }
.ctx-stage { color: #9090c0; }
.ctx-task  { color: #c0c8f0; font-weight: 500; }
.ctx-arrow { color: #3a3a60; font-size: 0.75rem; }
.task-id   { color: #6080c0; font-family: monospace; font-size: 0.78rem; }
.task-name { color: #d0d8f8; }

/* ── Phase pills ── */
.phase-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.35rem;
  font-size: 0.78rem;
  margin-bottom: 0.7rem;
}
.phase-row-label { color: #5060a0; font-size: 0.74rem; margin-right: 0.15rem; }
.phase-pill {
  padding: 0.15rem 0.55rem;
  border-radius: 20px;
  font-size: 0.72rem;
  white-space: nowrap;
  cursor: default;
}
.phase-done    { background: #1a3a25; color: #72c28a; border: 1px solid #2d6a3a; }
.phase-pending { background: #1a1a2a; color: #7080b0; border: 1px solid #2a2a40; }
.phase-muted   { background: transparent; color: #4a4a6a; border: 1px solid #2a2a40; font-style: italic; }

/* ── Sibling coordination ── */
.sibling-details {
  background: #0d0d0a;
  border: 1px solid #2a2a10;
  border-radius: 8px;
  padding: 0.5rem 0.85rem;
  font-size: 0.77rem;
  color: #a0a060;
  margin-top: 0.4rem;
}
.sibling-details summary {
  cursor: pointer;
  user-select: none;
  font-weight: 500;
  color: #b0b060;
  list-style: none;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}
.sibling-details summary::-webkit-details-marker { display: none; }
.sibling-count {
  font-size: 0.68rem;
  background: #2a2a10;
  color: #909040;
  padding: 0.1rem 0.4rem;
  border-radius: 10px;
  font-weight: 400;
}
.sibling-details ul {
  margin: 0.4rem 0 0 0.75rem;
  padding: 0;
  list-style: disc;
}
.sibling-details li { margin: 0.2rem 0; line-height: 1.4; }

/* ── Status badge colors ── */
.status-done      { background: #1a3a25; color: #72c28a; border: 1px solid #2d6a3a; }
.status-inprogress{ background: #1a2a40; color: #70a0e0; border: 1px solid #2a4a70; }
.status-inreview  { background: #2a1a40; color: #b080e0; border: 1px solid #4a2a70; }
.status-draft     { background: #2a2a1a; color: #c0b060; border: 1px solid #5a5a2a; }
.status-pending   { background: #1e1e2e; color: #6070a0; border: 1px solid #2a2a40; }
.status-other     { background: #1e1e2e; color: #8080a0; border: 1px solid #2a2a40; }
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
<footer style="font-size:0.72rem;color:#3a3a5a;margin-top:2rem;text-align:center;border-top:1px solid #1a1a2a;padding-top:1rem">
  Generated by <code>npm run progress</code> (tools/progress-tracker) — static snapshot, no live data.
</footer>
</body>
</html>
`;
}
