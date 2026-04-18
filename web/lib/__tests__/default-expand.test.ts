import { describe, it, expect } from 'vitest';
import { deriveDefaultExpandedStepId } from '../releases/default-expand';
import type { PlanData, PlanMetrics } from '../plan-loader-types';

function makePlan(stepIds: string[], stepStatuses?: Record<string, string>): PlanData {
  return {
    title: 'Test Plan',
    filename: 'test-plan.md',
    overallStatus: 'In Progress',
    overallStatusDetail: '',
    siblingWarnings: [],
    steps: stepIds.map((id) => ({
      id,
      title: `Step ${id}`,
      status: ((stepStatuses?.[id] ?? 'In Progress') as PlanData['steps'][0]['status']),
      statusDetail: '',
      stages: [],
    })),
    allTasks: [],
  };
}

function makeCounts(record: Record<string, { done: number; total: number }>): PlanMetrics['stepCounts'] {
  return record;
}

function makeMetrics(stepCounts: PlanMetrics['stepCounts']): PlanMetrics {
  return {
    completedCount: 0,
    totalCount: 0,
    statBarLabel: '0 / 0 done',
    chartData: [],
    stepCounts,
  };
}

describe('deriveDefaultExpandedStepId', () => {
  it('returns id of first step where done < total', () => {
    const plan = makePlan(['1', '2', '3']);
    const metrics = makeMetrics(makeCounts({
      '1': { done: 5, total: 5 },
      '2': { done: 3, total: 7 },
      '3': { done: 0, total: 4 },
    }));
    expect(deriveDefaultExpandedStepId(plan, metrics)).toBe('2');
  });

  it('returns null when all steps are done', () => {
    const plan = makePlan(['1', '2']);
    const metrics = makeMetrics(makeCounts({
      '1': { done: 3, total: 3 },
      '2': { done: 5, total: 5 },
    }));
    expect(deriveDefaultExpandedStepId(plan, metrics)).toBeNull();
  });

  it('returns first step id when all steps are pending (done=0, total>0)', () => {
    const plan = makePlan(['A', 'B', 'C']);
    const metrics = makeMetrics(makeCounts({
      'A': { done: 0, total: 4 },
      'B': { done: 0, total: 2 },
      'C': { done: 0, total: 6 },
    }));
    expect(deriveDefaultExpandedStepId(plan, metrics)).toBe('A');
  });

  it('ignores stale step-header status; uses task counts as ground truth', () => {
    // Step 1 has status='Final' (prose says done) but task counts say incomplete
    const plan = makePlan(['1', '2'], { '1': 'Final', '2': 'In Progress' });
    const metrics = makeMetrics(makeCounts({
      '1': { done: 2, total: 5 },
      '2': { done: 1, total: 3 },
    }));
    // Must return '1' despite 'Final' status prose — counts are ground truth
    expect(deriveDefaultExpandedStepId(plan, metrics)).toBe('1');
  });

  it('returns null for empty plan.steps', () => {
    const plan = makePlan([]);
    const metrics = makeMetrics({});
    expect(deriveDefaultExpandedStepId(plan, metrics)).toBeNull();
  });
});
