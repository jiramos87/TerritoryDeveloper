export type StatBarProps = {
  label: string
  value: number
  max: number
  thresholds?: { warn: number; critical: number }
}

const TIER_FILL = {
  default: 'bg-panel',
  warn: 'bg-[var(--color-text-accent-warn)]',
  critical: 'bg-[var(--color-text-accent-critical)]',
} as const

export function StatBar({ label, value, max, thresholds }: StatBarProps) {
  const pct = max <= 0 ? 0 : Math.max(0, Math.min(100, (value / max) * 100))
  const tier: keyof typeof TIER_FILL = !thresholds
    ? 'default'
    : value >= thresholds.critical
    ? 'critical'
    : value >= thresholds.warn
    ? 'warn'
    : 'default'

  return (
    <div className="flex flex-col gap-1">
      <div className="flex justify-between text-xs font-mono text-primary">
        <span>{label}</span>
        <span>{value}/{max}</span>
      </div>
      <div className="h-2 w-full rounded-sm bg-panel overflow-hidden">
        <div
          className={`h-full rounded-sm ${TIER_FILL[tier]}`}
          style={{ width: `${pct}%` }}
        />
      </div>
    </div>
  )
}
