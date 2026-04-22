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

/* CD-BUNDLE-TS-START */
export const cdBundle = {
  "raws": {
    "black": "#0a0a0a",
    "panel": "#1a1a1a",
    "text": "#e8e8e8",
    "red": "#d63838",
    "amber": "#e8a33d",
    "grey-500": "#6a6a6a",
    "green": "#3a9b4a",
    "blue": "#4a7bc8"
  },
  "semantic": {
    "--bg-canvas": "var(--raw-black)",
    "--bg-panel": "var(--raw-panel)",
    "--text-primary": "var(--raw-text)",
    "--text-muted": "var(--raw-grey-500)",
    "--text-accent-warn": "var(--raw-amber)",
    "--text-accent-critical": "var(--raw-red)",
    "--text-accent-info": "var(--raw-blue)",
    "--bg-status-done": "var(--raw-green)",
    "--text-status-done-fg": "var(--raw-black)",
    "--bg-status-progress": "var(--raw-amber)",
    "--text-status-progress-fg": "var(--raw-black)",
    "--bg-status-pending": "var(--raw-grey-500)",
    "--text-status-pending-fg": "var(--raw-text)",
    "--bg-status-blocked": "var(--raw-red)",
    "--text-status-blocked-fg": "var(--raw-text)",
    "--border-subtle": "rgba(232, 232, 232, 0.12)",
    "--border-strong": "rgba(232, 232, 232, 0.24)",
    "--overlay-panel": "rgba(26, 26, 26, 0.80)",
    "--focus-ring": "2px solid var(--text-accent-warn)",
    "--focus-ring-offset": "2px"
  },
  "motion": {
    "instant": "80ms",
    "subtle": "160ms",
    "gentle": "280ms",
    "deliberate": "480ms",
    "easeEnter": "cubic-bezier(0.2, 0.0, 0.0, 1.0)",
    "easeExit": "cubic-bezier(0.4, 0.0, 1.0, 1.0)"
  },
  "typeScale": {
    "--font-sans": "\"Geist\", system-ui, -apple-system, Segoe UI, sans-serif",
    "--font-mono": "\"Geist Mono\", ui-monospace, SFMono-Regular, Menlo, monospace",
    "--text-xs": "0.75rem",
    "--lh-xs": "1rem",
    "--text-sm": "0.875rem",
    "--lh-sm": "1.25rem",
    "--text-base": "1rem",
    "--lh-base": "1.5rem",
    "--text-lg": "1.125rem",
    "--lh-lg": "1.75rem",
    "--text-xl": "1.25rem",
    "--lh-xl": "1.75rem",
    "--text-2xl": "1.5rem",
    "--lh-2xl": "2rem"
  },
  "spacing": {
    "--sp-0": "0",
    "--sp-1": "0.25rem",
    "--sp-2": "0.5rem",
    "--sp-3": "0.75rem",
    "--sp-4": "1rem",
    "--sp-6": "1.5rem",
    "--sp-8": "2rem",
    "--sp-12": "3rem",
    "--radius-none": "0",
    "--radius-sm": "2px",
    "--radius-md": "4px",
    "--radius-lg": "8px",
    "--radius-pill": "9999px",
    "--shadow-0": "none",
    "--shadow-1": "0 1px 0 rgba(0,0,0,0.4)",
    "--shadow-2": "0 2px 4px rgba(0,0,0,0.5)"
  }
} as const;
/* CD-BUNDLE-TS-END */
