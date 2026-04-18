'use client';
/**
 * PlanTree.tsx — Client island owning Set<string> expand state for a single plan's tree.
 *
 * Seeds expand state from server-computed `initialExpanded` prop
 * (default-expanded step id from deriveDefaultExpandedStepId — TECH-341).
 * Renders root TreeNodeData[] list, passing `expanded` + `onToggle` to each
 * <TreeNode> (TECH-351). ONLY Client island on the progress surface —
 * progress page.tsx (TECH-354) stays RSC.
 */

import { useState } from 'react';
import type { ReactNode } from 'react';
import type { TreeNodeData } from '../lib/plan-tree';
import { TreeNode } from './TreeNode';

export interface PlanTreeProps {
  nodes: TreeNodeData[];
  initialExpanded: Set<string>;
}

export function PlanTree({ nodes, initialExpanded }: PlanTreeProps): ReactNode {
  const [expanded, setExpanded] = useState<Set<string>>(() => new Set(initialExpanded));

  const onToggle = (id: string) =>
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  return (
    <ul role="tree" className="list-none">
      {nodes.map((n) => (
        <TreeNode key={n.id} node={n} expanded={expanded} onToggle={onToggle} />
      ))}
    </ul>
  );
}
