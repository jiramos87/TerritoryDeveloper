'use client';

import { useEffect, useRef } from 'react';
import * as d3 from 'd3';

export interface DepNode extends d3.SimulationNodeDatum {
  id: string;
  title: string;
  status: string;
  external: boolean;
  stage?: string | null;
}

export interface DepLink extends d3.SimulationLinkDatum<DepNode> {
  source: string | DepNode;
  target: string | DepNode;
  kind: 'depends_on' | 'related';
}

export interface DepGraphProps {
  nodes: DepNode[];
  links: DepLink[];
  width?: number;
  height?: number;
}

const STATUS_COLOR: Record<string, string> = {
  done: 'var(--ds-raw-green)',
  archived: 'var(--ds-raw-green)',
  verified: 'var(--ds-raw-green)',
  implemented: 'var(--ds-raw-amber)',
  pending: 'var(--ds-text-meta)',
};

// Stage palette — first dotted-decimal segment of stage_id buckets the hue.
const STAGE_PALETTE = [
  '#5fb3d9', '#d97f5f', '#a06fd9', '#5fd97f', '#d9d65f',
  '#d95f9b', '#5fd9d2', '#d9b35f', '#9bd95f', '#5f7fd9',
];

function stageBucket(stage: string | null | undefined): number {
  if (!stage) return -1;
  const head = stage.split('.')[0];
  const n = Number(head);
  return Number.isFinite(n) ? n : -1;
}

function stageColor(stage: string | null | undefined): string {
  const b = stageBucket(stage);
  if (b < 0) return 'var(--ds-text-meta)';
  return STAGE_PALETTE[b % STAGE_PALETTE.length];
}

export default function DepGraph({ nodes, links, width = 960, height = 540 }: DepGraphProps) {
  const ref = useRef<SVGSVGElement>(null);

  useEffect(() => {
    if (!ref.current || nodes.length === 0) return;
    const svg = d3.select(ref.current);
    svg.selectAll('*').remove();

    // Compute connected-node set so isolated nodes can be faded.
    const connected = new Set<string>();
    for (const l of links) {
      connected.add(typeof l.source === 'string' ? l.source : l.source.id);
      connected.add(typeof l.target === 'string' ? l.target : l.target.id);
    }
    const isConnected = (d: DepNode) => connected.has(d.id);

    // Scale virtual canvas with node count so dense graphs (80+) are not crushed.
    const area = Math.max(width * height, nodes.length * 3500);
    const aspect = width / height;
    const innerH = Math.sqrt(area / aspect);
    const innerW = innerH * aspect;

    svg
      .attr('viewBox', `0 0 ${innerW} ${innerH}`)
      .attr('preserveAspectRatio', 'xMidYMid meet')
      .style('width', '100%')
      .style('height', 'auto')
      .style('max-height', `${height}px`)
      .style('cursor', 'grab');

    // Arrow marker for depends_on direction.
    const defs = svg.append('defs');
    defs
      .append('marker')
      .attr('id', 'dep-arrow')
      .attr('viewBox', '0 -5 10 10')
      .attr('refX', 14)
      .attr('refY', 0)
      .attr('markerWidth', 6)
      .attr('markerHeight', 6)
      .attr('orient', 'auto')
      .append('path')
      .attr('d', 'M0,-4L10,0L0,4')
      .attr('fill', 'var(--ds-raw-blue)');

    const root = svg.append('g').attr('class', 'zoom-root');

    const linkDist = nodes.length > 40 ? 22 : 60;
    const charge = nodes.length > 40 ? -35 : -180;
    const collideR = nodes.length > 40 ? 14 : 20;
    const nodeR = nodes.length > 40 ? 4 : 7;
    const nodeRext = nodes.length > 40 ? 5 : 9;
    const fontSize = nodes.length > 40 ? 8 : 10;

    // Stage-radial: bucket nodes around concentric ring per stage_id head.
    const stageBuckets = Array.from(
      new Set(nodes.map((n) => stageBucket(n.stage))),
    ).sort((a, b) => a - b);
    const stageIndex = new Map(stageBuckets.map((b, i) => [b, i]));
    const ringStep = Math.min(innerW, innerH) / (stageBuckets.length + 2) / 2;

    const sim = d3
      .forceSimulation<DepNode>(nodes.map((n) => ({ ...n })))
      .force(
        'link',
        d3
          .forceLink<DepNode, DepLink>(links.map((l) => ({ ...l })))
          .id((d) => d.id)
          .distance(linkDist)
          .strength(0.6),
      )
      .force('charge', d3.forceManyBody().strength(charge))
      .force('center', d3.forceCenter(innerW / 2, innerH / 2))
      .force(
        'radial',
        d3
          .forceRadial<DepNode>(
            (d) => ((stageIndex.get(stageBucket(d.stage)) ?? 0) + 1) * ringStep,
            innerW / 2,
            innerH / 2,
          )
          .strength(0.25),
      )
      .force('collision', d3.forceCollide<DepNode>().radius(collideR));

    const link = root
      .append('g')
      .attr('stroke-opacity', 0.85)
      .selectAll('line')
      .data(links)
      .enter()
      .append('line')
      .attr('stroke', (d) =>
        d.kind === 'depends_on' ? 'var(--ds-raw-blue)' : 'var(--ds-text-meta)',
      )
      .attr('stroke-width', 1.6)
      .attr('stroke-dasharray', (d) => (d.kind === 'related' ? '4 3' : null))
      .attr('marker-end', (d) => (d.kind === 'depends_on' ? 'url(#dep-arrow)' : null));

    const node = root
      .append('g')
      .selectAll<SVGGElement, DepNode>('g')
      .data(sim.nodes())
      .enter()
      .append('g')
      .attr('opacity', (d) => (isConnected(d) ? 1 : 0.28));

    node
      .append('circle')
      .attr('r', (d) => (d.external ? nodeRext : nodeR))
      .attr('fill', (d) => stageColor(d.stage))
      .attr('stroke', (d) =>
        d.external
          ? 'var(--ds-raw-amber)'
          : STATUS_COLOR[d.status] ?? 'var(--ds-bg-panel)',
      )
      .attr('stroke-width', (d) => (d.external ? 2 : 1.5));

    node
      .append('title')
      .text(
        (d) =>
          `${d.id} — ${d.title}\nstage ${d.stage ?? '—'} · ${d.status}${d.external ? ' (cross-plan)' : ''}`,
      );

    node
      .append('text')
      .attr('dx', nodeR + 3)
      .attr('dy', 4)
      .attr('font-family', 'var(--font-mono, ui-monospace)')
      .attr('font-size', `${fontSize}px`)
      .attr('fill', 'var(--ds-text-muted)')
      .text((d) => d.id);

    sim.on('tick', () => {
      link
        .attr('x1', (d) => (d.source as unknown as DepNode & { x: number }).x)
        .attr('y1', (d) => (d.source as unknown as DepNode & { y: number }).y)
        .attr('x2', (d) => (d.target as unknown as DepNode & { x: number }).x)
        .attr('y2', (d) => (d.target as unknown as DepNode & { y: number }).y);
      node.attr(
        'transform',
        (d) => `translate(${(d as DepNode & { x: number }).x},${(d as DepNode & { y: number }).y})`,
      );
    });

    // Pan + zoom (scroll-wheel / drag). Keep root group as zoom target.
    const zoom = d3
      .zoom<SVGSVGElement, unknown>()
      .scaleExtent([0.2, 4])
      .on('zoom', (event) => {
        root.attr('transform', event.transform.toString());
      });
    svg.call(zoom);
    // Fit-to-viewport on first layout (after sim has settled a bit).
    setTimeout(() => {
      const bbox = (root.node() as SVGGElement | null)?.getBBox();
      if (!bbox || bbox.width === 0) return;
      const pad = 16;
      const scale = Math.min(
        innerW / (bbox.width + pad * 2),
        innerH / (bbox.height + pad * 2),
        1,
      );
      const tx = innerW / 2 - scale * (bbox.x + bbox.width / 2);
      const ty = innerH / 2 - scale * (bbox.y + bbox.height / 2);
      svg
        .transition()
        .duration(400)
        .call(zoom.transform, d3.zoomIdentity.translate(tx, ty).scale(scale));
    }, 600);

    return () => {
      sim.stop();
    };
  }, [nodes, links, width, height]);

  if (nodes.length === 0) return <p className="text-text-muted text-sm">No dependencies</p>;

  // Legend mirrors STAGE_PALETTE buckets actually present in the graph.
  const presentBuckets = Array.from(
    new Set(nodes.map((n) => stageBucket(n.stage))),
  ).sort((a, b) => a - b);

  return (
    <div className="space-y-2">
      <svg ref={ref} />
      <div className="flex flex-wrap gap-x-4 gap-y-1 font-mono text-[10px] text-[var(--ds-text-meta)]">
        <span className="flex items-center gap-1">
          <svg width="22" height="8">
            <line x1="0" y1="4" x2="18" y2="4" stroke="var(--ds-raw-blue)" strokeWidth="1.6" markerEnd="url(#dep-arrow)" />
          </svg>
          depends on →
        </span>
        <span className="flex items-center gap-1">
          <svg width="22" height="8">
            <line x1="0" y1="4" x2="22" y2="4" stroke="var(--ds-text-meta)" strokeWidth="1.6" strokeDasharray="4 3" />
          </svg>
          related
        </span>
        <span className="flex items-center gap-1">
          <span
            className="inline-block h-2.5 w-2.5 rounded-full border-2"
            style={{ borderColor: 'var(--ds-raw-amber)', background: 'var(--ds-text-meta)' }}
          />
          cross-plan
        </span>
        <span className="opacity-70">isolated nodes faded</span>
        {presentBuckets
          .filter((b) => b >= 0)
          .map((b) => (
            <span key={b} className="flex items-center gap-1">
              <span
                className="inline-block h-2.5 w-2.5 rounded-full"
                style={{ background: STAGE_PALETTE[b % STAGE_PALETTE.length] }}
              />
              stage {b}.x
            </span>
          ))}
      </div>
    </div>
  );
}
