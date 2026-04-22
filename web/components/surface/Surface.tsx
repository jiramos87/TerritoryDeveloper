import type { ReactNode } from 'react';
import { SurfaceMotion } from './SurfaceMotion';
import { PADDING_CLASS, TONE_CLASS, type MotionKind, type PaddingKind, type ToneKind } from './surface-classes';

export type { MotionKind, PaddingKind, ToneKind } from './surface-classes';

export interface SurfaceProps {
  tone: ToneKind;
  padding: PaddingKind;
  /** Default `none` keeps this branch RSC-safe (B2). */
  motion?: MotionKind;
  className?: string;
  children?: ReactNode;
}

/**
 * Panel surface — `tone` + `padding`; optional motion uses client island + `globals.css` rules.
 * See `docs/web-platform-post-mvp-extensions.md` Example 2.
 */
export function Surface({ tone, padding, motion = 'none', className, children }: SurfaceProps) {
  if (motion === 'none') {
    const merged = [
      'ds-surface',
      'rounded-sm',
      'text-text-primary',
      TONE_CLASS[tone],
      PADDING_CLASS[padding],
      className,
    ]
      .filter(Boolean)
      .join(' ');
    return (
      <div className={merged} data-motion="none">
        {children}
      </div>
    );
  }
  return (
    <SurfaceMotion tone={tone} padding={padding} motion={motion} className={className}>
      {children}
    </SurfaceMotion>
  );
}
