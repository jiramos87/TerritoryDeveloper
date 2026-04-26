/**
 * Stable plan-slug derivation — shared by dashboard RSC + chart data builders so
 * that ring/bar interactions can resolve `#plan-{slug}` anchors on the page.
 */
export function toPlanSlug(title: string): string {
  return title
    .toLowerCase()
    .replace(/\s+/g, '-')
    .replace(/[^a-z0-9-]/g, '');
}
