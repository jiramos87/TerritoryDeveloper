'use client';

import {
  Eject,
  FastForward,
  Pause,
  Play,
  Rewind,
  Square,
  type LucideIcon,
} from 'lucide-react';
import type { ComponentPropsWithoutRef, ReactNode } from 'react';

/**
 * Transport control row (rewind, play, pause, stop, fast-forward, eject).
 * Source: `web/design-refs/step-8-console/src/console-primitives.jsx` (`TransportStrip`), simplified actions for web shell.
 */
export type TransportStripState = 'stopped' | 'playing' | 'paused';

export type TransportAction =
  | 'rewind'
  | 'play'
  | 'pause'
  | 'stop'
  | 'fastForward'
  | 'eject';

export type TransportStripProps = {
  state?: TransportStripState;
  onAction: (action: TransportAction) => void;
  className?: string;
};

const TBTN =
  'inline-flex h-10 w-10 shrink-0 cursor-pointer items-center justify-center rounded border border-black bg-[linear-gradient(180deg,#2a2a2a,#151515)] text-[var(--ds-text-muted)] shadow-[inset_0_1px_0_rgba(255,255,255,0.08),inset_0_-2px_2px_rgba(0,0,0,0.6),0_2px_3px_rgba(0,0,0,0.5)] transition-[color,box-shadow] duration-[var(--ds-duration-subtle)] active:translate-y-px';

const activePlaying = 'text-[var(--ds-raw-green)] shadow-[inset_0_1px_0_rgba(255,255,255,0.08),0_0_0_1px_rgba(58,155,74,0.35),0_0_10px_rgba(58,155,74,0.3)]';
const activePaused =
  'text-[var(--ds-raw-amber)] shadow-[inset_0_1px_0_rgba(255,255,255,0.08),0_0_0_1px_rgba(232,163,61,0.35),0_0_10px_rgba(232,163,61,0.3)]';
const activeStopped =
  'shadow-[inset_0_1px_0_rgba(255,255,255,0.08),0_0_0_1px_rgba(232,232,232,0.12)] text-[var(--ds-text-primary)]';

function ActionButton({
  action,
  label,
  Icon,
  className = '',
  ...rest
}: {
  action: TransportAction;
  label: string;
  Icon: LucideIcon;
  className?: string;
} & Omit<ComponentPropsWithoutRef<'button'>, 'children' | 'type'>): ReactNode {
  return (
    <button type="button" aria-label={label} className={`${TBTN} ${className}`.trim()} {...rest}>
      <Icon size={18} strokeWidth={2} aria-hidden />
    </button>
  );
}

export function TransportStrip({
  state = 'stopped',
  onAction,
  className = '',
}: TransportStripProps): ReactNode {
  return (
    <div
      className={`flex flex-wrap items-center gap-2 p-[var(--ds-spacing-sm)] ${className}`.trim()}
    >
      <ActionButton
        action="rewind"
        label="Rewind"
        Icon={Rewind}
        onClick={() => onAction('rewind')}
      />
      <ActionButton
        action="play"
        label="Play"
        Icon={Play}
        className={state === 'playing' ? `h-12 w-12 ${activePlaying}` : 'h-12 w-12'}
        onClick={() => onAction('play')}
      />
      <ActionButton
        action="pause"
        label="Pause"
        Icon={Pause}
        className={state === 'paused' ? `h-12 w-12 ${activePaused}` : 'h-12 w-12'}
        onClick={() => onAction('pause')}
      />
      <ActionButton
        action="stop"
        label="Stop"
        Icon={Square}
        className={state === 'stopped' ? activeStopped : ''}
        onClick={() => onAction('stop')}
      />
      <ActionButton
        action="fastForward"
        label="Fast forward"
        Icon={FastForward}
        onClick={() => onAction('fastForward')}
      />
      <ActionButton
        action="eject"
        label="Eject"
        Icon={Eject}
        onClick={() => onAction('eject')}
      />
    </div>
  );
}
