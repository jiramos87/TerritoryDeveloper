export type Chip = { label: string; active: boolean; href?: string }
export type FilterChipsProps = { chips: Chip[] }

const chipClass = (active: boolean) =>
  `inline-flex items-center rounded px-2 py-0.5 text-xs font-mono ${
    active ? 'bg-panel text-primary' : 'bg-canvas text-muted'
  }`

export function FilterChips({ chips }: FilterChipsProps) {
  return (
    <div className="flex gap-2 flex-wrap">
      {chips.map((c) =>
        c.href != null ? (
          <a key={c.label} href={c.href} className={chipClass(c.active)}>
            {c.label}
          </a>
        ) : (
          <span key={c.label} className={chipClass(c.active)}>
            {c.label}
          </span>
        )
      )}
    </div>
  )
}
