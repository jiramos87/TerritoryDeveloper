import { describe, it, expect } from 'vitest';
import { buildPlanTree } from '../plan-tree';
import type { PlanData, PlanMetrics, Stage, Step, TaskRow } from '../plan-loader-types';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function makeTask(overrides: Partial<TaskRow> & { id: string }): TaskRow {
  return {
    id: overrides.id,
    name: overrides.name,
    phase: overrides.phase ?? '1',
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
    phases: [],
    tasks,
  };
}

function makeStep(id: string, stages: Stage[]): Step {
  return {
    id,
    title: `Step ${id}`,
    status: 'In Progress',
    statusDetail: '',
    stages,
  };
}

function makePlan(steps: Step[]): PlanData {
  return {
    title: 'Test Plan',
    filename: 'test-plan.md',
    overallStatus: 'In Progress',
    overallStatusDetail: '',
    siblingWarnings: [],
    steps,
    allTasks: steps.flatMap((s) => s.stages.flatMap((st) => st.tasks)),
  };
}

function makeMetrics(): PlanMetrics {
  return {
    completedCount: 0,
    totalCount: 0,
    statBarLabel: '0 / 0 done',
    chartData: [],
    stepCounts: {},
  };
}

// ---------------------------------------------------------------------------
// Case 1 — Stage-node counts sum across phases + tasks
// ---------------------------------------------------------------------------

describe('buildPlanTree — Case 1: stage-node counts', () => {
  it('sums counts across all phases within a stage', () => {
    const tasks = [
      makeTask({ id: 'T1.1.1', phase: '1', status: 'Done' }),
      makeTask({ id: 'T1.1.2', phase: '1', status: 'Done' }),
      makeTask({ id: 'T1.1.3', phase: '2', status: 'In Progress' }),
    ];
    const plan = makePlan([makeStep('1', [makeStage('1.1', tasks)])]);
    const tree = buildPlanTree(plan, makeMetrics());

    const stepNode = tree[0];
    expect(stepNode.counts).toEqual({ done: 2, total: 3 });

    const stageNode = stepNode.children[0];
    expect(stageNode.counts).toEqual({ done: 2, total: 3 });
  });
});

// ---------------------------------------------------------------------------
// Case 2 — Phase synthesis from groupBy(task.phase)
// ---------------------------------------------------------------------------

describe('buildPlanTree — Case 2: phase synthesis from task groupBy', () => {
  it('creates phase nodes from task.phase, not Stage.phases checklist', () => {
    const tasks = [
      makeTask({ id: 'T1', phase: '1', status: 'Done' }),
      makeTask({ id: 'T2', phase: '2', status: 'In Progress' }),
      makeTask({ id: 'T3', phase: '2', status: 'Draft' }),
    ];
    // Stage.phases intentionally set differently (diverged checklist)
    const stage: Stage = {
      ...makeStage('1.1', tasks),
      phases: [
        { checked: false, label: 'Phase A (unrelated checklist entry)' },
      ],
    };
    const plan = makePlan([makeStep('1', [stage])]);
    const tree = buildPlanTree(plan, makeMetrics());

    const stageNode = tree[0].children[0];
    // Must produce 2 phase nodes from task groupBy, not 1 from Stage.phases
    expect(stageNode.children).toHaveLength(2);
    expect(stageNode.children[0].label).toBe('Phase 1');
    expect(stageNode.children[1].label).toBe('Phase 2');
  });

  it('sorts phase nodes lexicographically by task.phase key', () => {
    const tasks = [
      makeTask({ id: 'T3', phase: '3', status: 'Done' }),
      makeTask({ id: 'T1', phase: '1', status: 'Done' }),
      makeTask({ id: 'T2', phase: '2', status: 'Done' }),
    ];
    const plan = makePlan([makeStep('1', [makeStage('1.1', tasks)])]);
    const tree = buildPlanTree(plan, makeMetrics());

    const phaseLabels = tree[0].children[0].children.map((p) => p.label);
    expect(phaseLabels).toEqual(['Phase 1', 'Phase 2', 'Phase 3']);
  });

  it('empty stage produces stage node with no phase children and zero counts', () => {
    const plan = makePlan([makeStep('1', [makeStage('1.1', [])])]);
    const tree = buildPlanTree(plan, makeMetrics());

    const stageNode = tree[0].children[0];
    expect(stageNode.children).toHaveLength(0);
    expect(stageNode.counts).toEqual({ done: 0, total: 0 });
    expect(stageNode.status).toBe('pending');
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
      const task = makeTask({ id: 'T1', phase: '1', status: taskStatus });
      const plan = makePlan([makeStep('1', [makeStage('1.1', [task])])]);
      const tree = buildPlanTree(plan, makeMetrics());

      const taskNode = tree[0].children[0].children[0].children[0];
      expect(taskNode.status).toBe(expectedStatus);
    });
  }
});

// ---------------------------------------------------------------------------
// Case 4 — All-done propagation up phase → stage → step
// ---------------------------------------------------------------------------

describe('buildPlanTree — Case 4: all-done propagation', () => {
  it('propagates done up through phase → stage → step when all tasks done', () => {
    const tasks = [
      makeTask({ id: 'T1', phase: '1', status: 'Done' }),
      makeTask({ id: 'T2', phase: '1', status: 'Done (archived)' }),
      makeTask({ id: 'T3', phase: '2', status: 'Done' }),
    ];
    const plan = makePlan([makeStep('1', [makeStage('1.1', tasks)])]);
    const tree = buildPlanTree(plan, makeMetrics());

    const stepNode = tree[0];
    const stageNode = stepNode.children[0];
    const phase1 = stageNode.children[0];
    const phase2 = stageNode.children[1];

    expect(phase1.status).toBe('done');
    expect(phase2.status).toBe('done');
    expect(stageNode.status).toBe('done');
    expect(stepNode.status).toBe('done');
  });

  it('in-progress if any child in-progress (mixed)', () => {
    const tasks = [
      makeTask({ id: 'T1', phase: '1', status: 'Done' }),
      makeTask({ id: 'T2', phase: '1', status: 'In Progress' }),
    ];
    const plan = makePlan([makeStep('1', [makeStage('1.1', tasks)])]);
    const tree = buildPlanTree(plan, makeMetrics());

    expect(tree[0].status).toBe('in-progress');
    expect(tree[0].children[0].status).toBe('in-progress');
  });

  it('pending propagates when all tasks pending', () => {
    const tasks = [
      makeTask({ id: 'T1', phase: '1', status: 'Draft' }),
      makeTask({ id: 'T2', phase: '2', status: '_pending_' }),
    ];
    const plan = makePlan([makeStep('1', [makeStage('1.1', tasks)])]);
    const tree = buildPlanTree(plan, makeMetrics());

    expect(tree[0].status).toBe('pending');
  });
});
