import { describe, it, expect } from 'vitest';
import { parseMasterPlan, computePlanMetrics } from '../plan-parser';

// ---------------------------------------------------------------------------
// Fixture helpers — compose minimal master-plan markdown
// ---------------------------------------------------------------------------

function tableHeader(): string {
  return [
    '| Task | Name | Issue | Status | Intent |',
    '| --- | --- | --- | --- | --- |',
  ].join('\n');
}

function stageHeader(id: string, title: string, status = 'Draft'): string {
  return [
    `### Stage ${id} — ${title}`,
    '',
    `**Status:** ${status}`,
    '',
    '**Tasks:**',
    '',
  ].join('\n');
}

function plan(body: string, title = 'Fixture Plan'): string {
  return `# ${title}\n\n## Stages\n\n${body}\n`;
}

// ---------------------------------------------------------------------------
// Case 1 — Baseline: regular table parses as expected
// ---------------------------------------------------------------------------

describe('parseMasterPlan — baseline', () => {
  it('parses a single stage with 2 tasks', () => {
    const md = plan([
      stageHeader('1', 'First'),
      tableHeader(),
      '| T1.1 | First task | TECH-1 | Done | do thing |',
      '| T1.2 | Second task | TECH-2 | In Progress | do other |',
    ].join('\n'));

    const parsed = parseMasterPlan(md, 'fixture.md');
    expect(parsed.stages).toHaveLength(1);
    expect(parsed.stages[0].tasks).toHaveLength(2);
    expect(parsed.stages[0].tasks[0].id).toBe('T1.1');
    expect(parsed.stages[0].tasks[1].status).toBe('In Progress');
  });
});

// ---------------------------------------------------------------------------
// Case 2 — Fenced code-block containing `payload: |` markdown table rows
//
// Regression guard: a fenced yaml block embedding markdown table rows must NOT
// be parsed as real task rows. Before fence tracking was added to
// parseTaskTable, the rows inside `payload: |` bled into the task list and
// forced `deriveHierarchyStatus` to flip "Final" stages to "In Progress".
// ---------------------------------------------------------------------------

describe('parseMasterPlan — fenced code block immunity', () => {
  it('ignores table rows inside a fenced yaml payload', () => {
    const md = plan([
      stageHeader('8', 'Validation'),
      tableHeader(),
      '| T8.1 | Real task | TECH-485 | Done | intent one |',
      '| T8.2 | Real task | TECH-486 | Done | intent two |',
      '',
      '#### §Plan Fix',
      '',
      '```yaml',
      '- operation: replace_section',
      '  target_anchor: "task_key:T8.1"',
      '  payload: |',
      '    | T8.1 | Ghost row | **TECH-485** | Draft | should not count |',
      '```',
      '',
      '```yaml',
      '- operation: replace_section',
      '  target_anchor: "task_key:T8.2"',
      '  payload: |',
      '    | T8.2 | Ghost row | **TECH-486** | Draft | should not count either |',
      '```',
    ].join('\n'));

    const parsed = parseMasterPlan(md, 'fixture.md');
    const stage = parsed.stages[0];

    expect(stage.tasks).toHaveLength(2);
    expect(stage.tasks.map((t) => t.id)).toEqual(['T8.1', 'T8.2']);
    expect(stage.tasks.every((t) => t.status === 'Done (archived)')).toBe(true);
  });

  it('derives Final stage status when all real tasks Done and ghosts fenced out', () => {
    const md = plan([
      stageHeader('8', 'Validation', 'Draft'),
      tableHeader(),
      '| T8.1 | Real | TECH-485 | Done | a |',
      '| T8.2 | Real | TECH-486 | Done | b |',
      '| T8.3 | Real | TECH-487 | Done | c |',
      '| T8.4 | Real | TECH-488 | Done | d |',
      '',
      '#### §Plan Fix',
      '',
      '```yaml',
      '- op: x',
      '  payload: |',
      '    | T8.1 | ghost | **TECH-485** | Draft | x |',
      '    | T8.4 | ghost | **TECH-488** | Draft | x |',
      '```',
    ].join('\n'));

    const parsed = parseMasterPlan(md, 'fixture.md');
    const stage = parsed.stages[0];

    expect(stage.tasks).toHaveLength(4);
    expect(stage.status).toBe('Final');

    const metrics = computePlanMetrics(parsed);
    expect(metrics.stageCounts['8']).toEqual({ done: 4, total: 4 });
  });

  it('resumes table parsing after a fenced block closes', () => {
    const md = plan([
      stageHeader('1', 'Mixed'),
      tableHeader(),
      '| T1.1 | Pre-fence | TECH-1 | Done | a |',
      '',
      '```yaml',
      '- op: x',
      '  payload: |',
      '    | T1.999 | ghost | **TECH-X** | Draft | no |',
      '```',
      '',
      '| T1.2 | Post-fence | TECH-2 | Done | b |',
    ].join('\n'));

    const parsed = parseMasterPlan(md, 'fixture.md');
    const stage = parsed.stages[0];

    expect(stage.tasks.map((t) => t.id)).toEqual(['T1.1', 'T1.2']);
    expect(stage.tasks.every((t) => t.status === 'Done (archived)')).toBe(true);
  });
});

// ---------------------------------------------------------------------------
// Case 3 — deriveHierarchyStatus over-ride behavior
// ---------------------------------------------------------------------------

describe('parseMasterPlan — deriveHierarchyStatus', () => {
  it('flips parsed "Draft" to "Final" when all tasks Done', () => {
    const md = plan([
      stageHeader('2', 'All Done', 'Draft'),
      tableHeader(),
      '| T2.1 | a | TECH-1 | Done | a |',
      '| T2.2 | b | TECH-2 | Done (archived) | b |',
    ].join('\n'));

    const parsed = parseMasterPlan(md, 'fixture.md');
    expect(parsed.stages[0].status).toBe('Final');
  });

  it('sets "In Progress" when Done mixed with pending', () => {
    const md = plan([
      stageHeader('2', 'Mixed', 'Draft'),
      tableHeader(),
      '| T2.1 | a | TECH-1 | Done | a |',
      '| T2.2 | b | TECH-2 | Draft | b |',
    ].join('\n'));

    const parsed = parseMasterPlan(md, 'fixture.md');
    expect(parsed.stages[0].status).toBe('In Progress');
  });

  it('sets "Draft" when all tasks pending', () => {
    const md = plan([
      stageHeader('2', 'All Pending', 'In Progress'),
      tableHeader(),
      '| T2.1 | a | TECH-1 | _pending_ | a |',
      '| T2.2 | b | TECH-2 | Draft | b |',
    ].join('\n'));

    const parsed = parseMasterPlan(md, 'fixture.md');
    expect(parsed.stages[0].status).toBe('Draft');
  });

  it('leaves empty-stage parsed status untouched', () => {
    const md = plan([
      stageHeader('3', 'Skeleton', 'Draft'),
    ].join('\n'));

    const parsed = parseMasterPlan(md, 'fixture.md');
    expect(parsed.stages[0].status).toBe('Draft');
    expect(parsed.stages[0].tasks).toHaveLength(0);
  });
});

// ---------------------------------------------------------------------------
// Case 4 — Status normalization: "Done" → "Done (archived)"
// ---------------------------------------------------------------------------

describe('parseMasterPlan — status normalization', () => {
  it('normalizes "Done" to "Done (archived)"', () => {
    const md = plan([
      stageHeader('1', 'x'),
      tableHeader(),
      '| T1.1 | a | TECH-1 | Done | a |',
    ].join('\n'));

    const parsed = parseMasterPlan(md, 'fixture.md');
    expect(parsed.stages[0].tasks[0].status).toBe('Done (archived)');
  });

  it('keeps "Done (archived)" untouched', () => {
    const md = plan([
      stageHeader('1', 'x'),
      tableHeader(),
      '| T1.1 | a | TECH-1 | Done (archived) | a |',
    ].join('\n'));

    const parsed = parseMasterPlan(md, 'fixture.md');
    expect(parsed.stages[0].tasks[0].status).toBe('Done (archived)');
  });
});
