'use client';

import { useState, type ReactNode } from 'react';

import { MediaTransport, type MediaTransportState } from '@/components/console/MediaTransport';
import { TIcon } from '@/components/console/icons/TIcon';

const GLYPHS = [
  ['Play', TIcon.Play],
  ['Pause', TIcon.Pause],
  ['Stop', TIcon.Stop],
  ['Record', TIcon.Record],
  ['Rewind', TIcon.Rewind],
  ['Fast forward', TIcon.FastForward],
  ['Rewind to start', TIcon.RewindEnd],
  ['Fast forward to end', TIcon.FastForwardEnd],
  ['Eject', TIcon.Eject],
  ['Loop', TIcon.Loop],
  ['Shuffle', TIcon.Shuffle],
  ['Mute', TIcon.Mute],
  ['Solo', TIcon.Solo],
] as const;

const MEDIA_STATES: MediaTransportState[] = ['stopped', 'playing', 'paused', 'recording'];

/**
 * Client island: TIcon matrix + four MediaTransport state snapshots (Stage 26).
 */
export function ConsoleMediaShowcase(): ReactNode {
  const [last, setLast] = useState<string | null>(null);

  return (
    <div className="space-y-10">
      <section className="space-y-3">
        <h3 className="text-sm font-mono uppercase tracking-widest text-text-muted">TIcon family</h3>
        <p className="text-sm text-text-muted">Thirteen transport / mixer glyphs — `currentColor` for theme.</p>
        <div className="flex flex-wrap gap-6 text-[var(--ds-raw-amber)]">
          {GLYPHS.map(([label, Comp]) => (
            <div key={label} className="flex flex-col items-center gap-1">
              <Comp size={28} aria-label={label} />
              <span className="text-[10px] font-mono text-text-muted">{label}</span>
            </div>
          ))}
        </div>
      </section>

      <section className="space-y-3">
        <h3 className="text-sm font-mono uppercase tracking-widest text-text-muted">Media transport</h3>
        <p className="text-sm text-text-muted">Four `state` values; buttons dispatch optional `actions`.</p>
        {last ? (
          <p className="font-mono text-xs text-text-muted">Last action: {last}</p>
        ) : null}
        <div className="grid gap-6 md:grid-cols-2">
          {MEDIA_STATES.map((s) => (
            <div key={s} className="space-y-2 rounded border border-text-muted/20 p-3">
              <p className="text-xs font-mono uppercase text-text-muted">state=&quot;{s}&quot;</p>
              <MediaTransport
                state={s}
                actions={{
                  play: () => {
                    setLast('play');
                  },
                  pause: () => {
                    setLast('pause');
                  },
                  stop: () => {
                    setLast('stop');
                  },
                  rewind: () => {
                    setLast('rewind');
                  },
                  ff: () => {
                    setLast('ff');
                  },
                  eject: () => {
                    setLast('eject');
                  },
                }}
              />
            </div>
          ))}
        </div>
      </section>
    </div>
  );
}
