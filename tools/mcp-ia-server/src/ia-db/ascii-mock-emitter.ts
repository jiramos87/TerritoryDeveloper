/**
 * ascii-mock-emitter.ts — Strategy γ POCO: deterministic ASCII tree renderer.
 *
 * renderAscii(tree, opts) → string
 *   - Box-drawing chars: ┌─┐│└┘├┤
 *   - Stable sort: (slot_name, order_idx) — deterministic across calls
 *   - LF newlines only; no Date.now() / randomness; NO wide unicode bleed
 *   - Width auto-fit bounded to pure ASCII byte width of longest line
 */

import type { PanelChildNode } from "./ui-catalog.js";

export interface AsciiMockOpts {
  /** Root label shown in header (panel slug). */
  rootLabel: string;
  /** Optional layout_template shown in header. */
  layoutTemplate?: string | null;
}

function nodeLabel(node: PanelChildNode): string {
  const slug = node.slug ?? node.kind;
  const slot = node.slot;
  return `${slot}: ${slug} (${node.kind})`;
}

function renderChildren(
  nodes: PanelChildNode[],
  prefix: string,
  lines: string[],
): void {
  // Stable sort: slot_name asc, order_idx asc
  const sorted = [...nodes].sort((a, b) => {
    if (a.slot < b.slot) return -1;
    if (a.slot > b.slot) return 1;
    return a.ord - b.ord;
  });

  for (let i = 0; i < sorted.length; i++) {
    const node = sorted[i]!;
    const isLast = i === sorted.length - 1;
    const connector = isLast ? "└─ " : "├─ ";
    const childPrefix = isLast ? "   " : "│  ";

    lines.push(`${prefix}${connector}${nodeLabel(node)}`);

    if (node.children && node.children.length > 0) {
      renderChildren(node.children, prefix + childPrefix, lines);
    }
  }
}

/**
 * Render a panel child tree as a deterministic ASCII box-drawing string.
 * Two calls with identical input → byte-identical output guaranteed.
 */
export function renderAscii(
  children: PanelChildNode[],
  opts: AsciiMockOpts,
): string {
  const header = opts.layoutTemplate
    ? `${opts.rootLabel} (${opts.layoutTemplate})`
    : opts.rootLabel;

  const lines: string[] = [];
  renderChildren(children, "", lines);

  // Compute width from all content lines + header
  const contentLines = [header, ...lines];
  const maxContent = contentLines.reduce(
    (max, l) => Math.max(max, l.length),
    0,
  );
  // Top/bottom bar width = maxContent + 4 (2 margin chars each side)
  const barWidth = maxContent + 4;
  const bar = "─".repeat(barWidth - 2);

  const top = `┌${bar}┐`;
  const bottom = `└${bar}┘`;
  const headerLine = `│ ${header.padEnd(barWidth - 4)} │`;

  const bodyLines = lines.map((l) => `│ ${l.padEnd(barWidth - 4)} │`);

  return [top, headerLine, ...bodyLines, bottom].join("\n");
}
