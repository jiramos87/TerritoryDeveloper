/**
 * render.test.mjs — Snapshot checks on generated HTML structure.
 *
 * Run: node --test tools/progress-tracker/tests/render.test.mjs
 * (from repo root)
 */

import { readFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { test } from 'node:test';
import assert from 'node:assert/strict';

import { parseMasterPlan } from '../parse.mjs';
import { renderHtml } from '../render.mjs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = join(__dirname, '..', '..', '..');
const PLANS_DIR = join(REPO_ROOT, 'ia', 'projects');

function readPlan(name) {
  return readFileSync(join(PLANS_DIR, name), 'utf8');
}

const PLAN_FILES = [
  'multi-scale-master-plan.md',
  'blip-master-plan.md',
  'sprite-gen-master-plan.md',
];

function buildAllPlans() {
  return PLAN_FILES.map(f => parseMasterPlan(readPlan(f), f));
}

test('renderHtml: returns a string', () => {
  const plans = buildAllPlans();
  const html = renderHtml(plans);
  assert.equal(typeof html, 'string');
  assert.ok(html.length > 0);
});

test('renderHtml: valid HTML5 doctype', () => {
  const plans = buildAllPlans();
  const html = renderHtml(plans);
  assert.ok(html.startsWith('<!DOCTYPE html>'), 'Expected HTML5 doctype at start');
});

test('renderHtml: has <head> with charset and inline CSS', () => {
  const plans = buildAllPlans();
  const html = renderHtml(plans);
  assert.ok(html.includes('<meta charset="utf-8">'), 'Missing charset meta');
  assert.ok(html.includes('<style>'), 'Missing inline <style>');
  assert.ok(!html.includes('<link'), 'Must not contain external <link> tags');
});

test('renderHtml: zero <script> tags (no JS runtime)', () => {
  const plans = buildAllPlans();
  const html = renderHtml(plans);
  assert.ok(!html.includes('<script'), 'Must not contain <script> tags');
});

test('renderHtml: zero external fetch URLs (no src/href with http/https)', () => {
  const plans = buildAllPlans();
  const html = renderHtml(plans);
  // Should not contain any external resource links
  const hasExternal = /href\s*=\s*"https?:\/\//i.test(html)
    || /src\s*=\s*"https?:\/\//i.test(html);
  assert.ok(!hasExternal, 'Must not contain external fetch URLs');
});

test('renderHtml: overall header present with progress bar', () => {
  const plans = buildAllPlans();
  const html = renderHtml(plans);
  assert.ok(html.includes('overall-header'), 'Missing overall-header section');
  assert.ok(html.includes('progress-bar-fill'), 'Missing progress bar fill element');
  assert.ok(html.includes('tasks Done across'), 'Missing combined task count text');
});

test('renderHtml: one plan-card per plan', () => {
  const plans = buildAllPlans();
  const html = renderHtml(plans);
  const cardMatches = html.match(/class="plan-card"/g) ?? [];
  assert.equal(cardMatches.length, plans.length, `Expected ${plans.length} plan-cards, got ${cardMatches.length}`);
});

test('renderHtml: plan titles appear in output', () => {
  const plans = buildAllPlans();
  const html = renderHtml(plans);
  for (const plan of plans) {
    // Title is HTML-escaped; just check key substring
    const keyWord = plan.title.split(' ')[0]; // e.g. "Multi-Scale", "Blip", "Isometric"
    assert.ok(html.includes(keyWord), `Expected plan title keyword "${keyWord}" in HTML`);
  }
});

test('renderHtml: green progress bar % appears for multi-scale (has Done tasks)', () => {
  const plans = buildAllPlans();
  const html = renderHtml(plans);
  // Multi-scale has Done tasks so pct should be > 0
  const multiScalePlan = plans.find(p => p.title.includes('Multi-Scale'));
  assert.ok(multiScalePlan, 'Expected multi-scale plan');
  const doneCount = multiScalePlan.allTasks.filter(t => t.status === 'Done (archived)').length;
  const total = multiScalePlan.allTasks.length;
  const pct = total > 0 ? Math.round((doneCount / total) * 100) : 0;
  assert.ok(pct > 0, `Expected non-zero pct for multi-scale, got ${pct}`);
  // The % should appear somewhere in the HTML
  assert.ok(html.includes(`${pct}%`), `Expected "${pct}%" in HTML`);
});

test('renderHtml: sibling coordination section visible', () => {
  const plans = buildAllPlans();
  const html = renderHtml(plans);
  assert.ok(html.includes('sibling-box') || html.includes('Sibling'), 'Expected sibling-box or Sibling text in HTML');
});

test('renderHtml: deterministic — two consecutive renders identical', () => {
  const plans1 = buildAllPlans();
  const plans2 = buildAllPlans();
  const html1 = renderHtml(plans1);
  const html2 = renderHtml(plans2);
  assert.equal(html1, html2, 'renderHtml must be deterministic: same input → same output bytes');
});

test('renderHtml: info table rows present (active step/stage/task)', () => {
  const plans = buildAllPlans();
  const html = renderHtml(plans);
  assert.ok(html.includes('Active step'), 'Missing Active step row');
  assert.ok(html.includes('Active stage'), 'Missing Active stage row');
  assert.ok(html.includes('Active task'), 'Missing Active task row');
});

test('renderHtml: phase checklist section present', () => {
  const plans = buildAllPlans();
  const html = renderHtml(plans);
  assert.ok(html.includes('Phase checklist'), 'Missing phase checklist section');
});

test('renderHtml: empty plans array returns valid HTML', () => {
  const html = renderHtml([]);
  assert.ok(html.startsWith('<!DOCTYPE html>'));
  assert.ok(html.includes('0 / 0 tasks Done'));
});
