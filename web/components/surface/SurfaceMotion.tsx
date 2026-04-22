'use client';

import { useEffect, useState, type ReactNode } from 'react';
import { PADDING_CLASS, TONE_CLASS, type MotionKind, type PaddingKind, type ToneKind } from './surface-classes';

export interface SurfaceMotionProps {
  tone: ToneKind;
  padding: PaddingKind;
  motion: Exclude<MotionKind, 'none'>;
  className?: string;
  children?: ReactNode;
}

/**
 * Client island — `data-mounted` after mount for CSS enter transition (B2).
 */
export function SurfaceMotion({ tone, padding, motion, className, children }: SurfaceMotionProps) {
  const [mounted, setMounted] = useState(false);
  useEffect(() => {
    let active = true;
    const id = requestAnimationFrame(() => {
      if (active) setMounted(true);
    });
    return () => {
      active = false;
      cancelAnimationFrame(id);
    };
  }, []);
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
    <div
      className={merged}
      data-motion={motion}
      data-mounted={mounted ? 'true' : undefined}
    >
      {children}
    </div>
  );
}
