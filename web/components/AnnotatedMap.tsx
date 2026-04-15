const BUCKETS = [
  'bg-panel',
  'bg-[color-mix(in_srgb,var(--color-text-accent-warn)_35%,var(--color-bg-panel))]',
  'bg-[var(--color-text-accent-warn)]',
  'bg-[color-mix(in_srgb,var(--color-text-accent-critical)_60%,var(--color-text-accent-warn))]',
  'bg-[var(--color-text-accent-critical)]',
] as const

function bucketClassForIntensity(intensity?: number): string {
  if (intensity == null) return BUCKETS[0]
  const clamped = Math.max(0, Math.min(1, intensity))
  const idx = Math.min(4, Math.floor(clamped * 5))
  return BUCKETS[idx]
}

type Region = { id: string; path: string; intensity?: number }
type Annotation = { x: number; y: number; label: string }
type Props = { regions: Region[]; annotations: Annotation[] }

export function AnnotatedMap({ regions, annotations }: Props) {
  return (
    <svg viewBox="0 0 1000 600" role="img">
      {regions.map((r) => (
        <path key={r.id} d={r.path} className={bucketClassForIntensity(r.intensity)} />
      ))}
      {annotations.map((a) => (
        <text
          key={a.label}
          x={a.x}
          y={a.y}
          style={{ letterSpacing: '0.15em', textTransform: 'uppercase' }}
          className="text-primary text-[10px] font-mono"
        >
          {a.label}
        </text>
      ))}
    </svg>
  )
}
