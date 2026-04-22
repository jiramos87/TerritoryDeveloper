import palette from './tokens/palette.json';

/**
 * Web design system tokens — see `web/lib/design-system.md`.
 * Nested `as const` for type inference and test stability.
 */
export const typeScale = {
  display: { rem: '3.815rem', fontWeight: 600, letterSpacing: '-0.02em' },
  h1: { rem: '3.052rem', fontWeight: 600, letterSpacing: '-0.02em' },
  h2: { rem: '2.442rem', fontWeight: 600, letterSpacing: '-0.015em' },
  h3: { rem: '1.953rem', fontWeight: 550, letterSpacing: '-0.01em' },
  'body-lg': { rem: '1.563rem', fontWeight: 400, letterSpacing: '0' },
  body: { rem: '1.25rem', fontWeight: 400, letterSpacing: '0' },
  'body-sm': { rem: '1rem', fontWeight: 400, letterSpacing: '0.01em' },
  caption: { rem: '0.8rem', fontWeight: 400, letterSpacing: '0.02em' },
  'mono-code': { rem: '0.64rem', fontWeight: 400, letterSpacing: '0' },
  'mono-meta': { rem: '0.512rem', fontWeight: 400, letterSpacing: '0.02em' },
} as const;

export const spacing = {
  '2xs': '0.25rem',
  xs: '0.5rem',
  sm: '0.75rem',
  md: '1rem',
  lg: '1.5rem',
  xl: '2rem',
  '2xl': '3rem',
  '3xl': '4rem',
  layout: '8rem',
} as const;

export const motion = {
  instant: 0,
  subtle: 120,
  gentle: 200,
  deliberate: 320,
  /**
   * When `prefers-reduced-motion: reduce` is active, the CSS layer in `globals.css`
   * sets all `--ds-duration-*` custom properties to 0ms; this object mirrors that contract.
   */
  reducedMotion: { duration: 0 },
} as const;

const raw = palette.raw;

export const text = {
  primary: raw.text,
  secondary: raw['grey-500'],
  meta: raw['grey-500'],
  disabled: raw['grey-500'],
} as const;

export const surface = {
  canvas: raw.black,
  raised: raw.panel,
  sunken: raw.sunken,
  inset: raw.inset,
} as const;

export const accent = {
  terrain: raw.terrainGreen,
  water: raw.waterBlue,
  warm: raw.amber,
} as const;
