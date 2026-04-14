export type Chip = { label: string; active: boolean }
export type FilterChipsProps = { chips: Chip[] }

export function FilterChips({ chips }: FilterChipsProps) {
  return (
    <div className="flex gap-2">
      {chips.map((c) => (
        <span
          key={c.label}
          className={`inline-flex items-center rounded px-2 py-0.5 text-xs font-mono ${
            c.active ? 'bg-panel text-primary' : 'bg-canvas text-muted'
          }`}
        >
          {c.label}
        </span>
      ))}
    </div>
  )
}
