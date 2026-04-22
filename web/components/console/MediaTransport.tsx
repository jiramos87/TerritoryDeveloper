'use client';

import type { ReactNode } from 'react';

import { TIcon } from './icons/TIcon';
import { TransportStrip, type TransportAction, type TransportStripState } from './TransportStrip';

/**
 * NB-CD3: no gratuitous motion on this strip — only TransportStrip + static glyphs.
 * Composes `TransportStrip` (Stage 8.4) with optional recording row + `TIcon` chrome.
 */
export type MediaTransportState = 'stopped' | 'playing' | 'paused' | 'recording';

export type MediaTransportActionKey = 'play' | 'pause' | 'stop' | 'rewind' | 'ff' | 'eject';

export type MediaTransportProps = {
  state: MediaTransportState;
  actions: Partial<Record<MediaTransportActionKey, () => void>>;
  className?: string;
};

function toStripState(state: MediaTransportState): TransportStripState {
  if (state === 'recording') {
    return 'playing';
  }
  return state;
}

function emitAction(
  table: Partial<Record<MediaTransportActionKey, () => void>>,
  action: TransportAction
): void {
  const key: MediaTransportActionKey | undefined =
    action === 'fastForward' ? 'ff' : (action as MediaTransportActionKey);
  table[key]?.();
}

export function MediaTransport({ state, actions, className = '' }: MediaTransportProps): ReactNode {
  return (
    <div className={`flex flex-col gap-2 ${className}`.trim()}>
      {state === 'recording' ? (
        <div
          className="inline-flex items-center gap-2 text-[var(--ds-raw-red,#d63838)]"
          aria-live="polite"
        >
          <TIcon.Record size={20} className="shrink-0" aria-label="Recording" />
          <span className="font-mono text-xs uppercase tracking-widest">Recording</span>
        </div>
      ) : null}
      <TransportStrip
        state={toStripState(state)}
        onAction={(a) => {
          emitAction(actions, a);
        }}
      />
    </div>
  );
}
