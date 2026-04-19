import { describe, it, expect } from 'vitest';
import { deriveDefaultExpandedStageId } from '../releases/default-expand';
import type { PlanData, PlanMetrics, HierarchyStatus } from '../plan-loader-types';

function makePlan(stageIds: string[], stageStatuses?: Record<string, string>): PlanData {
  return {
    title: 'Test Plan',
    filename: 'test-plan.md',
    overallStatus: 'In Progress',
    overallStatusDetail: '',
    siblingWarnings: [],
    stages: stageIds.map((id) => ({
      id,
      title: `Stage ${id}`,
      status: ((stageStatuses?.[id] ?? 'In Progress') as HierarchyStatus),
      statusDetail: '',
      tasks: [],
    })),
    allTasks: [],
  };
}

function makeCounts(record: Record<string, { done: number; total: number }>): PlanMetrics['stageCounts'] {
  return record;
}

function makeMetrics(stageCounts: PlanMetrics['stageCounts']): PlanMetrics {
  return {
    completedCount: 0,
    totalCount: 0,
    statBarLabel: '0 / 0 done',
    chartData: [],
    stageCounts,
  };
}

describe('deriveDefaultExpandedStageId', () => {
  it('returns id of first stage where done < total', () => {
    const plan = makePlan(['1', '2', '3']);
    const metrics = makeMetrics(makeCounts({
      '1': { done: 5, total: 5 },
      '2': { done: 3, total: 7 },
      '3': { done: 0, total: 4 },
    }));
    expect(deriveDefaultExpandedStageId(plan, metrics)).toBe('2');
  });

  it('returns null when all stages are done', () => {
    const plan = makePlan(['1', '2']);
    const metrics = makeMetrics(makeCounts({
      '1': { done: 3, total: 3 },
      '2': { done: 5, total: 5 },
    }));
    expect(deriveDefaultExpandedStageId(plan, metrics)).toBeNull();
  });

  it('returns first stage id when all stages are pending (done=0, total>0)', () => {
    const plan = makePlan(['A', 'B', 'C']);
    const metrics = makeMetrics(makeCounts({
      'A': { done: 0, total: 4 },
      'B': { done: 0, total: 2 },
      'C': { done: 0, total: 6 },
    }));
    expect(deriveDefaultExpandedStageId(plan, metrics)).toBe('A');
  });

  it('ignores stale stage-header status; uses task counts as ground truth', () => {
    const plan = makePlan(['1', '2'], { '1': 'Final', '2': 'In Progress' });
    const metrics = makeMetrics(makeCounts({
      '1': { done: 2, total: 5 },
      '2': { done: 1, total: 3 },
    }));
    expect(deriveDefaultExpandedStageId(plan, metrics)).toBe('1');
  });

  it('returns null for empty plan.stages', () => {
    const plan = makePlan([]);
    const metrics = makeMetrics({});
    expect(deriveDefaultExpandedStageId(plan, metrics)).toBeNull();
  });
});
