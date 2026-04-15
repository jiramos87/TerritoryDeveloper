export type TimelineRow = {
  date: string;
  milestone: string;
  notes: string;
};

export const TIMELINE_ROWS: TimelineRow[] = [
  {
    date: '2026-01-20',
    milestone: 'Multi-scale plan filed',
    notes: 'Master plan decomposed into Blip, sprite-gen, and web-platform lanes.',
  },
  {
    date: '2026-02-10',
    milestone: 'Blip DSP kernel shipped',
    notes: 'Bake pipeline complete — envelope, silence, and determinism tests passing.',
  },
  {
    date: '2026-03-01',
    milestone: 'Sprite-gen Stage 1 complete',
    notes: 'CLI render pipeline operational; first archetype YAML authored and validated.',
  },
  {
    date: '2026-04-01',
    milestone: 'Web platform bootstrapped',
    notes: 'Next.js workspace live; landing page, token system, and MDX loader shipped.',
  },
  {
    date: '2026-04-15',
    milestone: 'Public pages shipped',
    notes: 'About, install, and history routes live with RSC + MDX content.',
  },
];
