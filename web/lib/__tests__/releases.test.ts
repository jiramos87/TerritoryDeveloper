import { describe, it, expect } from 'vitest';
import { resolveRelease } from '../releases';
import { getReleasePlans } from '../releases/resolve';
import type { PlanData } from '../plan-loader-types';

// Minimal PlanData stub — only `filename` matters for getReleasePlans
function makePlan(filename: string): PlanData {
  return {
    title: filename,
    filename,
    overallStatus: '',
    overallStatusDetail: '',
    siblingWarnings: [],
    stages: [],
    allTasks: [],
  };
}

describe('resolveRelease', () => {
  it('case 1 — returns matching Release for known id', () => {
    const result = resolveRelease('full-game-mvp');
    expect(result).not.toBeNull();
    expect(result?.id).toBe('full-game-mvp');
  });

  it('case 2 — returns null for unknown id (no throw)', () => {
    const result = resolveRelease('nope');
    expect(result).toBeNull();
  });
});

describe('getReleasePlans', () => {
  const release = {
    id: 'test-release',
    label: 'Test',
    umbrellaMasterPlan: 'umbrella.md',
    children: ['alpha.md', 'beta.md', 'gamma.md'],
  };

  it('case 3 — returns matched plans in release.children order, not allPlans order', () => {
    const allPlans = [
      makePlan('gamma.md'),
      makePlan('alpha.md'),
      makePlan('beta.md'),
    ];
    const result = getReleasePlans(release, allPlans);
    expect(result.map((p) => p.filename)).toEqual(['alpha.md', 'beta.md', 'gamma.md']);
  });

  it('case 4 — silently drops children absent from allPlans (no throw)', () => {
    const allPlans = [makePlan('alpha.md')];
    expect(() => getReleasePlans(release, allPlans)).not.toThrow();
    const result = getReleasePlans(release, allPlans);
    expect(result).toHaveLength(1);
    expect(result[0].filename).toBe('alpha.md');
  });

  it('case 5 — umbrella self-inclusion: umbrellaMasterPlan in children appears in result', () => {
    const selfIncluding = {
      ...release,
      children: ['umbrella.md', 'alpha.md'],
    };
    const allPlans = [makePlan('umbrella.md'), makePlan('alpha.md')];
    const result = getReleasePlans(selfIncluding, allPlans);
    expect(result.map((p) => p.filename)).toEqual(['umbrella.md', 'alpha.md']);
  });
});
