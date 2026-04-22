export type ToneKind = 'raised' | 'sunken' | 'inset';
export type PaddingKind = 'sm' | 'md' | 'lg' | 'section';
export type MotionKind = 'none' | 'subtle' | 'gentle' | 'deliberate';

export const TONE_CLASS: Record<ToneKind, string> = {
  raised: 'bg-[var(--ds-surface-raised)]',
  sunken: 'bg-[var(--ds-surface-sunken)]',
  inset: 'bg-[var(--ds-surface-inset)]',
};

export const PADDING_CLASS: Record<PaddingKind, string> = {
  sm: 'p-[var(--ds-spacing-sm)]',
  md: 'p-[var(--ds-spacing-md)]',
  lg: 'p-[var(--ds-spacing-lg)]',
  section: 'p-[var(--ds-spacing-layout)]',
};
