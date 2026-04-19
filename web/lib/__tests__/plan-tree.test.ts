import { describe, it, expect } from 'vitest';
import { buildPlanTree } from '../plan-tree';
import type { PlanData, PlanMetrics, Stage, TaskRow } from '../plan-loader-types';

// ---------------------------------------------------------------------------
// Helpers (2-level Stage → Task)
// ---------------------------------------------------------------------------

function makeTask(overrides: Partial<TaskRow> & { id: string }): TaskRow {
  return {
    id: overrides.id,
    name: overrides.name,
    issue: overrides.issue ?? 'TECH-1',
    status: overrides.status ?? 'In Progress',
    intent: overrides.intent ?? 'some task',
  };
}

function makeStage(id: string, tasks: TaskRow[]): Stage {
  return {
    id,
    title: `Stage ${id}`,
    status: 'In Progress',
    statusDetail: '',
    tasks,
  };
}

function makePlan(stages: Stage[]): PlanData {
  return {
    title: 'Test Plan',
    filename: 'test-plan.md',
    overallStatus: 'In Progress',
    overallStatusDetail: '',
    siblingWarnings: [],
    stages,
    allTasks: stages.flatMap((st) => st.tasks),
  };
}

function makeMetrics(): PlanMetrics {
  return {
    completedCount: 0,
    totalCount: 0,
    statBarLabel: '0 / 0 done',
    chartData: [],
    stageCounts: {},
  };
}

// ---------------------------------------------------------------------------
// Case 1 — Stage-node counts sum across tasks
// ---------------------------------------------------------------------------

describe('buildPlanTree — Case 1: stage-node counts', () => {
  it('sums counts across all tasks within a stage', () => {
    const tasks = [
      makeTask({ id: 'T1.1.1', status: 'Done' }),
      makeTask({ id: 'T1.1.2', status: 'Done' }),
      makeTask({ id: 'T1.1.3', status: 'In Progress' }),
    ];
    const plan = makePlan([makeStage('1.1', tasks)]);
    const tree = buildPlanTree(plan, makeMetrics());

    const stageNode = tree[0];
    expect(stageNode.kind).toBe('stage');
    expect(stageNode.counts).toEqual({ done: 2, total: 3 });
    expect(stageNode.children).toHaveLength(3);
  });
});

// ---------------------------------------------------------------------------
// Case 2 — Direct Stage → Task nesting (no Phase layer)
// ---------------------------------------------------------------------------

describe('buildPlanTree — Case 2: direct task children', () => {
  it('renders tasks directly under stage (no phase grouping)', () => {
    const tasks = [
      makeTask({ id: 'T1', status: 'Done' }),
      makeTask({ id: 'T2', status: 'In Progress' }),
      makeTask({ id: 'T3', status: 'Draft' }),
    ];
    const plan = makePlan([makeStage('1.1', tasks)]);
    const tree = buildPlanTree(plan, makeMetrics());

    const stageNode = tree[0];
    expect(stageNode.children).toHaveLength(3);
    expect(stageNode.children.map((c) => c.kind)).toEqual(['task', 'task', 'task']);
    expect(stageNode.children.map((c) => c.id)).toEqual(['T1', 'T2', 'T3']);
  });

  it('empty stage produces stage node with no children and zero counts', () => {
    const plan = makePlan([makeStage('1.1', [])]);
    const tree = buildPlanTree(plan, makeMetrics());

    const stageNode = tree[0];
    expect(stageNode.children).toHaveLength(0);
    expect(stageNode.counts).toEqual({ done: 0, total: 0 });
    expect(stageNode.status).toBe('pending');
    expect(stageNode.kind === 'stage' && stageNode.pendingDecompose).toBe(true);
  });
});

// ---------------------------------------------------------------------------
// Case 3 — Status derivation: TaskStatus → BadgeChip Status
// ---------------------------------------------------------------------------

describe('buildPlanTree — Case 3: status derivation', () => {
  const cases: Array<[TaskRow['status'], string]> = [
    ['Done', 'done'],
    ['Done (archived)', 'done'],
    ['In Progress', 'in-progress'],
    ['In Review', 'in-progress'],
    ['Draft', 'pending'],
    ['_pending_', 'pending'],
  ];

  for (const [taskStatus, expectedStatus] of cases) {
    it(`maps TaskStatus "${taskStatus}" → "${expectedStatus}"`, () => {
      const task = makeTask({ id: 'T1', status: taskStatus });
      const plan = makePlan([makeStage('1.1', [task])]);
      const tree = buildPlanTree(plan, makeMetrics());

      const stageNode = tree[0];
      const taskNode = stageNode.children[0];
      expect(taskNode.status).toBe(expectedStatus);
    });
  }
});

// ---------------------------------------------------------------------------
// Case 4 — All-done propagation up task → stage
// ---------------------------------------------------------------------------

describe('buildPlanTree — Case 4: all-done propagation', () => {
  it('propagates done up through stage when all tasks done', () => {
    const tasks = [
      makeTask({ id: 'T1', status: 'Done' }),
      makeTask({ id: 'T2', status: 'Done (archived)' }),
      makeTask({ id: 'T3', status: 'Done' }),
    ];
    const plan = makePlan([makeStage('1.1', tasks)]);
    const tree = buildPlanTree(plan, makeMetrics());

    const stageNode = tree[0];
    expect(stageNode.status).toBe('done');
    expect(stageNode.counts).toEqual({ done: 3, total: 3 });
  });

  it('in-progress if any child in-progress (mixed)', () => {
    const tasks = [
      makeTask({ id: 'T1', status: 'Done' }),
      makeTask({ id: 'T2', status: 'In Progress' }),
    ];
    const plan = makePlan([makeStage('1.1', tasks)]);
    const tree = buildPlanTree(plan, makeMetrics());

    expect(tree[0].status).toBe('in-progress');
  });

  it('pending propagates when all tasks pending', () => {
    const tasks = [
      makeTask({ id: 'T1', status: 'Draft' }),
      makeTask({ id: 'T2', status: '_pending_' }),
    ];
    const plan = makePlan([makeStage('1.1', tasks)]);
    const tree = buildPlanTree(plan, makeMetrics());

    expect(tree[0].status).toBe('pending');
  });
});
