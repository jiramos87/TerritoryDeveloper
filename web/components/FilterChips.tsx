/**
 * A single filter chip descriptor.
 *
 * Each chip's `active` state is independent — supports multi-select callers
 * (any number of chips may be active simultaneously; no single-active invariant).
 *
 * `href` is optional: when present the chip renders as an `<a>` (navigable link);
 * when absent it renders as a `<span>` (non-navigable static state indicator).
 *
 * RSC-compatible — no client hooks; safe for use inside Server Components.
 */
export type Chip = { label: string; active: boolean; href?: string }
export type FilterChipsProps = { chips: Chip[] }

const chipClass = (active: boolean) =>
  `inline-flex items-center rounded px-2 py-0.5 text-xs font-mono ${
    active
      ? 'bg-[var(--ds-bg-panel)] text-[var(--ds-text-primary)]'
      : 'bg-[var(--ds-bg-canvas)] text-[var(--ds-text-muted)]'
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
