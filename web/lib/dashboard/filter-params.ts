export const clearFiltersHref = '/dashboard'

/**
 * Collect all values for `key` from URL params, supporting both
 * repeated params (?k=a&k=b) and comma-delimited (?k=a,b).
 * Returns deduped, ascending-sorted array.
 */
export function parseFilterValues(
  params: URLSearchParams | { getAll: (k: string) => string[] },
  key: string
): string[] {
  const raw = typeof (params as URLSearchParams).getAll === 'function'
    ? (params as URLSearchParams).getAll(key)
    : []
  const values = new Set<string>()
  for (const occurrence of raw) {
    for (const part of occurrence.split(',')) {
      const trimmed = part.trim()
      if (trimmed !== '') values.add(trimmed)
    }
  }
  return Array.from(values).sort()
}

/**
 * Toggle `value` in the comma-delimited representation of `key`
 * within `currentSearch`. Returns new query string without leading `?`.
 */
export function toggleFilterParam(
  currentSearch: string,
  key: string,
  value: string
): string {
  const p = new URLSearchParams(currentSearch)
  const current = parseFilterValues(p, key)
  const next = current.includes(value)
    ? current.filter((v) => v !== value)
    : [...current, value].sort()
  p.delete(key)
  if (next.length > 0) p.set(key, next.join(','))
  return p.toString()
}
