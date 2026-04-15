const BUCKETS = [
  'bg-panel',
  'bg-[color-mix(in_srgb,var(--color-text-accent-warn)_35%,var(--color-bg-panel))]',
  'bg-[var(--color-text-accent-warn)]',
  'bg-[color-mix(in_srgb,var(--color-text-accent-critical)_60%,var(--color-text-accent-warn))]',
  'bg-[var(--color-text-accent-critical)]',
] as const

export function HeatmapCell({ intensity }: { intensity: number }) {
  const clamped = Math.max(0, Math.min(1, intensity))
  const idx = Math.min(4, Math.floor(clamped * 5))
  return <div className={`w-4 h-4 ${BUCKETS[idx]}`} />
}
