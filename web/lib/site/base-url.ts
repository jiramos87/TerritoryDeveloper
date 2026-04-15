/**
 * Returns the absolute base URL for this deployment.
 * Reads NEXT_PUBLIC_SITE_URL; falls back to http://localhost:3000 for local dev.
 * Trailing slash is always stripped.
 */
export function getBaseUrl(): string {
  const raw = process.env.NEXT_PUBLIC_SITE_URL ?? 'http://localhost:3000';
  return raw.replace(/\/$/, '');
}
