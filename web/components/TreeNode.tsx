/**
 * TreeNode.tsx — Recursive render primitive for a single TreeNodeData node.
 *
 * Server-safe: no 'use client', no hooks. State (expanded set) lives in the
 * parent PlanTree component (TECH-352), which owns the Client boundary.
 *
 * Props:
 *   node      — TreeNodeData node to render (step | stage | phase | task)
 *   expanded  — Set of node ids currently expanded
 *   onToggle  — Called with node.id when a branch toggle is clicked
 */

import type { ReactNode } from 'react';
import type { TreeNodeData } from '../lib/plan-tree';
import { STATUS_TOKEN_CLASS } from './status-tokens';

export interface TreeNodeProps {
  node: TreeNodeData;
  expanded: Set<string>;
  onToggle: (id: string) => void;
}

export function TreeNode({ node, expanded, onToggle }: TreeNodeProps): ReactNode {
  const isLeaf = node.kind === 'task';
  const isExpanded = expanded.has(node.id);
  const childListId = `tree-children-${node.id}`;

  if (isLeaf) {
    // TaskNode — leaf render: status glyph + label + optional issue id
    const taskNode = node;
    return (
      <li className="flex items-baseline gap-1 py-0.5 text-sm">
        <span
          className={`inline-block text-xs ${STATUS_TOKEN_CLASS[taskNode.status]}`}
          aria-hidden="true"
        >
          ●
        </span>
        <span>{taskNode.label}</span>
        {taskNode.issue !== '_pending_' && (
          <span className="font-mono text-xs opacity-60">{taskNode.issue}</span>
        )}
      </li>
    );
  }

  // Branch node (step | stage | phase) — toggle button + optional children list
  const branchNode = node;
  const hasObjective = (branchNode.kind === 'step' || branchNode.kind === 'stage') && branchNode.objective;
  const isPendingDecompose = (branchNode.kind === 'step' || branchNode.kind === 'stage') && branchNode.pendingDecompose;
  return (
    <li className="py-0.5">
      <button
        type="button"
        aria-expanded={isExpanded}
        aria-controls={childListId}
        onClick={() => onToggle(branchNode.id)}
        className="flex items-baseline gap-1 text-left text-sm font-medium hover:underline focus:outline-none focus-visible:ring-2"
      >
        <span aria-hidden="true">{isExpanded ? '▼' : '▶'}</span>
        <span>{branchNode.label}</span>
        {isPendingDecompose ? (
          <span className="font-mono text-xs font-normal opacity-40 italic">pending decompose</span>
        ) : (
          <span className="font-mono text-xs font-normal opacity-60">
            {branchNode.counts.done}/{branchNode.counts.total}
          </span>
        )}
      </button>
      {(node.kind === 'step' || node.kind === 'stage') && node.objective && (
        <p className="ml-4 mt-0.5 text-xs opacity-60 leading-relaxed">{node.objective}</p>
      )}
      {isExpanded && (
        <ul id={childListId} role="group" className="ml-4 mt-0.5 list-none">
          {branchNode.children.map((child) => (
            <TreeNode
              key={child.id}
              node={child}
              expanded={expanded}
              onToggle={onToggle}
            />
          ))}
        </ul>
      )}
    </li>
  );
}
