import type { ReactNode } from 'react';

/**
 * Status LED indicator.
 * Ported from CD `web/design-refs/step-8-console/src/console-primitives.jsx` (`LED`).
 */
export type LedState = 'off' | 'on' | 'blink' | 'error';
export type LedColor = 'green' | 'amber' | 'red' | 'info';

const LED_COLOR: Record<LedColor, string> = {
  green:
    'bg-[var(--ds-raw-green)] shadow-[inset_0_0_2px_rgba(0,0,0,0.6),0_0_6px_var(--ds-raw-green)]',
  amber:
    'bg-[var(--ds-raw-amber)] shadow-[inset_0_0_2px_rgba(0,0,0,0.6),0_0_6px_var(--ds-raw-amber)]',
  red: 'bg-[var(--ds-raw-red)] shadow-[inset_0_0_2px_rgba(0,0,0,0.6),0_0_8px_var(--ds-raw-red)]',
  info: 'bg-[var(--ds-raw-blue)] shadow-[inset_0_0_2px_rgba(0,0,0,0.6),0_0_6px_var(--ds-raw-blue)]',
};

export type LEDProps = {
  state?: LedState;
  color?: LedColor;
  title?: string;
  className?: string;
};

export function LED({
  state = 'off',
  color = 'green',
  title,
  className = '',
}: LEDProps): ReactNode {
  const lit = state === 'on' || state === 'blink' || state === 'error';
  const useErrorColor = state === 'error';
  const c = useErrorColor ? LED_COLOR.red : LED_COLOR[color];
  const base =
    'inline-block size-2 shrink-0 rounded-full bg-[#111] shadow-[inset_0_1px_1px_rgba(0,0,0,0.8)]';
  const blink = state === 'blink' || state === 'error' ? 'ds-console-led-blink' : '';
  const cls = `${base} ${lit ? c : ''} ${blink} ${className}`.trim();
  return <span className={cls} title={title} aria-label={title} />;
}
