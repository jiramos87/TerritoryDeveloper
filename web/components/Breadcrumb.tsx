import Link from 'next/link'

export type Crumb = { label: string; href?: string }

/**
 * RSC-compatible breadcrumb trail.
 * Renders nothing when crumbs.length <= 1 (no meaningful trail).
 * Last crumb = current page (no link). Ancestors = muted links.
 */
export function Breadcrumb({ crumbs }: { crumbs: Crumb[] }) {
  if (crumbs.length <= 1) return null

  return (
    <nav aria-label="Breadcrumb" className="flex items-center flex-wrap gap-2 text-base py-3 mb-6">
      {crumbs.map((crumb, i) => {
        const isLast = i === crumbs.length - 1
        return (
          <span key={i} className="flex items-center gap-2">
            {i > 0 && (
              <span className="text-text-muted opacity-40 select-none font-light" aria-hidden>
                /
              </span>
            )}
            {isLast || !crumb.href ? (
              <span className={isLast ? 'text-text-primary font-medium' : 'text-text-muted'}>
                {crumb.label}
              </span>
            ) : (
              <Link
                href={crumb.href}
                className="text-text-muted hover:text-text-primary transition-colors underline-offset-2 hover:underline"
              >
                {crumb.label}
              </Link>
            )}
          </span>
        )
      })}
    </nav>
  )
}
