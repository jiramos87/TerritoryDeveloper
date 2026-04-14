/**
 * parse.test.mjs — Fixture tests for parse.mjs against in-flight master plans.
 *
 * Run: node --test tools/progress-tracker/tests/parse.test.mjs
 * (from repo root)
 */

import { readFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { test } from 'node:test';
import assert from 'node:assert/strict';

import { parseMasterPlan } from '../parse.mjs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = join(__dirname, '..', '..', '..');
const PLANS_DIR = join(REPO_ROOT, 'ia', 'projects');

function readPlan(name) {
  return readFileSync(join(PLANS_DIR, name), 'utf8');
}

// ─── multi-scale-master-plan ───────────────────────────────────────────────

test('multi-scale: parses title', () => {
  const plan = parseMasterPlan(readPlan('multi-scale-master-plan.md'), 'multi-scale-master-plan.md');
  assert.ok(plan.title.includes('Multi-Scale'), `Expected title to include 'Multi-Scale', got: ${plan.title}`);
});

test('multi-scale: parses overall status', () => {
  const plan = parseMasterPlan(readPlan('multi-scale-master-plan.md'), 'multi-scale-master-plan.md');
  assert.equal(plan.overallStatus, 'In Progress', `Expected 'In Progress', got: ${plan.overallStatus}`);
  assert.ok(plan.overallStatusDetail.length > 0, 'Expected non-empty status detail');
});

test('multi-scale: has sibling warnings', () => {
  const plan = parseMasterPlan(readPlan('multi-scale-master-plan.md'), 'multi-scale-master-plan.md');
  assert.ok(plan.siblingWarnings.length > 0, 'Expected sibling warnings');
  const text = plan.siblingWarnings.join(' ');
  assert.ok(
    text.includes('blip-master-plan') || text.includes('sprite-gen-master-plan'),
    'Expected blip or sprite-gen sibling reference'
  );
});

test('multi-scale: finds Step 1 with stages', () => {
  const plan = parseMasterPlan(readPlan('multi-scale-master-plan.md'), 'multi-scale-master-plan.md');
  assert.ok(plan.steps.length >= 1, 'Expected at least 1 step');
  const step1 = plan.steps.find(s => s.id === '1');
  assert.ok(step1, 'Expected Step 1');
  assert.ok(step1.stages.length >= 3, `Expected ≥3 stages in Step 1, got ${step1.stages.length}`);
});

test('multi-scale: stage 1.1 all tasks Done', () => {
  const plan = parseMasterPlan(readPlan('multi-scale-master-plan.md'), 'multi-scale-master-plan.md');
  const step1 = plan.steps.find(s => s.id === '1');
  const stage11 = step1.stages.find(s => s.id === '1.1');
  assert.ok(stage11, 'Expected Stage 1.1');
  assert.ok(stage11.tasks.length === 3, `Expected 3 tasks in Stage 1.1, got ${stage11.tasks.length}`);
  assert.ok(
    stage11.tasks.every(t => t.status === 'Done (archived)'),
    `Expected all Stage 1.1 tasks Done, got: ${stage11.tasks.map(t => t.status)}`
  );
});

test('multi-scale: stage 1.3 has In Progress task', () => {
  const plan = parseMasterPlan(readPlan('multi-scale-master-plan.md'), 'multi-scale-master-plan.md');
  const step1 = plan.steps.find(s => s.id === '1');
  const stage13 = step1.stages.find(s => s.id === '1.3');
  assert.ok(stage13, 'Expected Stage 1.3');
  const inProgress = stage13.tasks.filter(t => t.status === 'In Progress');
  assert.ok(inProgress.length >= 1, `Expected ≥1 In Progress task in Stage 1.3, got ${inProgress.length}`);
});

test('multi-scale: allTasks flat list populated', () => {
  const plan = parseMasterPlan(readPlan('multi-scale-master-plan.md'), 'multi-scale-master-plan.md');
  assert.ok(plan.allTasks.length > 0, 'Expected non-empty allTasks');
  // All tasks have required fields
  for (const task of plan.allTasks) {
    assert.ok(task.id, `Task missing id: ${JSON.stringify(task)}`);
    assert.ok(task.status, `Task missing status: ${JSON.stringify(task)}`);
  }
});

// ─── blip-master-plan ─────────────────────────────────────────────────────

test('blip: parses title', () => {
  const plan = parseMasterPlan(readPlan('blip-master-plan.md'), 'blip-master-plan.md');
  assert.ok(plan.title.toLowerCase().includes('blip'), `Expected title to include 'blip', got: ${plan.title}`);
});

test('blip: parses overall status', () => {
  const plan = parseMasterPlan(readPlan('blip-master-plan.md'), 'blip-master-plan.md');
  assert.ok(plan.overallStatus.length > 0, 'Expected non-empty overallStatus');
});

test('blip: has sibling warnings', () => {
  const plan = parseMasterPlan(readPlan('blip-master-plan.md'), 'blip-master-plan.md');
  assert.ok(plan.siblingWarnings.length > 0, 'Expected sibling warnings in blip plan');
});

test('blip: stage 1.1 tasks all Done', () => {
  const plan = parseMasterPlan(readPlan('blip-master-plan.md'), 'blip-master-plan.md');
  const step1 = plan.steps.find(s => s.id === '1');
  assert.ok(step1, 'Expected Step 1 in blip');
  const stage11 = step1.stages.find(s => s.id === '1.1');
  assert.ok(stage11, 'Expected Stage 1.1 in blip');
  assert.ok(stage11.tasks.length >= 4, `Expected ≥4 tasks in blip Stage 1.1, got ${stage11.tasks.length}`);
  const doneTasks = stage11.tasks.filter(t => t.status === 'Done (archived)');
  assert.ok(
    doneTasks.length === stage11.tasks.length,
    `Expected all Stage 1.1 tasks Done, got: ${stage11.tasks.map(t => t.status)}`
  );
});

// ─── sprite-gen-master-plan ───────────────────────────────────────────────

test('sprite-gen: parses title', () => {
  const plan = parseMasterPlan(readPlan('sprite-gen-master-plan.md'), 'sprite-gen-master-plan.md');
  assert.ok(
    plan.title.toLowerCase().includes('sprite') || plan.title.toLowerCase().includes('isometric'),
    `Expected title to include 'sprite' or 'isometric', got: ${plan.title}`
  );
});

test('sprite-gen: parses overall status as Draft', () => {
  const plan = parseMasterPlan(readPlan('sprite-gen-master-plan.md'), 'sprite-gen-master-plan.md');
  assert.equal(plan.overallStatus, 'Draft', `Expected 'Draft', got: ${plan.overallStatus}`);
});

test('sprite-gen: has steps with _pending_ tasks', () => {
  const plan = parseMasterPlan(readPlan('sprite-gen-master-plan.md'), 'sprite-gen-master-plan.md');
  assert.ok(plan.steps.length >= 1, 'Expected ≥1 step in sprite-gen');
  const pending = plan.allTasks.filter(t => t.status === '_pending_');
  assert.ok(pending.length > 0, `Expected _pending_ tasks in sprite-gen, got ${plan.allTasks.length} total tasks`);
});

test('sprite-gen: phase checklist entries found', () => {
  const plan = parseMasterPlan(readPlan('sprite-gen-master-plan.md'), 'sprite-gen-master-plan.md');
  const step1 = plan.steps.find(s => s.id === '1');
  assert.ok(step1, 'Expected Step 1 in sprite-gen');
  // Stage 1.1 has phase checklist
  const stage11 = step1.stages.find(s => s.id === '1.1');
  assert.ok(stage11, 'Expected Stage 1.1 in sprite-gen');
  assert.ok(stage11.phases.length >= 3, `Expected ≥3 phase entries, got ${stage11.phases.length}`);
  // None should be checked (all Draft)
  assert.ok(
    stage11.phases.every(p => !p.checked),
    'Expected all sprite-gen Stage 1.1 phases unchecked'
  );
});

// ─── Edge cases ────────────────────────────────────────────────────────────

test('parse: empty string returns empty plan', () => {
  const plan = parseMasterPlan('', 'empty.md');
  assert.equal(plan.title, 'empty.md');
  assert.equal(plan.overallStatus, '');
  assert.equal(plan.steps.length, 0);
  assert.equal(plan.allTasks.length, 0);
});

test('parse: task with "Done" (non-archived) normalized to Done (archived)', () => {
  const md = `# Test Plan

> **Status:** Draft

### Step 1 — First

**Status:** Draft

#### Stage 1.1 — First Stage

**Status:** Draft

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|------|-------|-------|--------|--------|
| T1.1.1 | 1 | TECH-1 | Done | Some task |
`;
  const plan = parseMasterPlan(md, 'test.md');
  assert.equal(plan.allTasks[0].status, 'Done (archived)');
});
