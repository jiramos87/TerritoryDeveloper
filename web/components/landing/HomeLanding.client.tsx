'use client';

import type { ReactNode } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { useState } from 'react';
import { Button } from '@/components/Button';
import { Heading } from '@/components/type/Heading';
import {
  Bezel,
  LED,
  Rack,
  Screen,
  TapeReel,
  TransportStrip,
  type TransportStripState,
} from '@/components/console';
import { VuStrip } from '@/components/console/VuStrip';

const HSTACK = 'flex flex-row items-center';

/**
 * Stage 27 T27.2 — CD `ScreenLanding` port (client interactivity: transport strip).
 */
export function HomeLandingClient(): ReactNode {
  const [tState, setTState] = useState<TransportStripState>('playing');

  return (
    <div className="p-[var(--ds-spacing-md)]">
      <div
        className="mb-3 flex flex-wrap gap-2 font-mono text-[11px] uppercase tracking-wide text-[var(--ds-text-meta)]"
      >
        <span className="text-[var(--ds-raw-blue)]">Territory Developer // Console</span>
      </div>

      <div className="mb-6 grid grid-cols-1 items-stretch gap-6 min-[900px]:grid-cols-[1.1fr_1fr]">
        <Rack className="hero-left flex flex-col gap-4" label="STUDIO A">
          <span className="inline-flex items-center gap-2 font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-raw-amber)]">
            <LED color="amber" state="blink" title="On air" />
            On Air // Internal engineering
          </span>
          <Heading level="display" className="max-w-[18ch]">
            Plan the territory. Ship the signal.
          </Heading>
          <p className="max-w-[52ch] text-[var(--text-lg)] leading-[1.5] text-[var(--ds-text-primary)]">
            An internal console for the Territory Developer pilot. Every step, stage, phase and
            task routed through one mixing desk — with release telemetry, density heatmaps, and a
            drill-down that goes all the way to the task row.
          </p>
          <div className="mt-2 flex flex-wrap gap-3">
            <Link
              href="/dashboard"
              className="inline-flex items-center justify-center rounded px-4 py-2 font-mono text-base transition-colors bg-[var(--ds-accent-terrain)] text-[var(--ds-text-status-done-fg)]"
            >
              Enter dashboard →
            </Link>
            <Button variant="secondary" size="lg" href="/design-system">
              Design guide
            </Button>
          </div>
          <div
            className={`${HSTACK} gap-1.5 border-t border-black pt-3`}
            style={{ boxShadow: '0 1px 0 rgba(255,255,255,.04)' }}
          >
            <div className={`${HSTACK} gap-1.5`}>
              <LED color="green" state="on" title="Build" />
              <span
                className="font-mono text-[10px] tracking-widest text-[var(--ds-text-meta)]"
                style={{ letterSpacing: '0.1em' }}
              >
                Build v0.4.0-α
              </span>
            </div>
            <div className={`${HSTACK} gap-1.5`}>
              <TapeReel size="sm" />
              <span
                className="font-mono text-[10px] tracking-widest text-[var(--ds-text-meta)]"
                style={{ letterSpacing: '0.1em' }}
              >
                7/12 nodes
              </span>
            </div>
            <div className={`${HSTACK} gap-1.5`}>
              <LED color="info" state="on" title="Signal" />
              <span
                className="font-mono text-[10px] tracking-widest text-[var(--ds-text-meta)]"
                style={{ letterSpacing: '0.1em' }}
              >
                Signal nominal
              </span>
            </div>
          </div>
        </Rack>
        <Rack className="hero-right p-0" label="Viewport // live">
          <div className="relative h-full min-h-[200px] w-full overflow-hidden">
            <Image
              src="/design/hero-art.svg"
              alt=""
              width={800}
              height={600}
              className="h-full w-full object-cover"
              priority
            />
          </div>
        </Rack>
      </div>

      <Rack
        className="mb-5 grid grid-cols-1 items-center gap-4 min-[600px]:grid-cols-[auto_1fr_auto]"
        label="Now playing"
      >
        <Bezel>
          <Screen tone="readout" sweep={false} className="lcd min-w-[220px] px-3.5 py-2.5">
            <div
              className="lcd-label font-mono text-[9px] uppercase tracking-[0.25em] opacity-60"
            >
              Current release
            </div>
            <div
              className="font-mono text-[26px] font-bold leading-none tracking-tight text-[var(--ds-raw-amber)] v"
            >
              v0.4.0-α
            </div>
          </Screen>
        </Bezel>
        <VuStrip level={0.62} />
        <div className="flex min-w-0 flex-wrap gap-2.5 font-mono text-[9px] uppercase tracking-widest text-[var(--ds-text-meta)]">
          <span className={`${HSTACK} gap-1`}>
            <LED color="green" state="on" />
            CI
          </span>
          <span className={`${HSTACK} gap-1`}>
            <LED color="green" state="on" />
            DB
          </span>
          <span className={`${HSTACK} gap-1`}>
            <LED color="amber" state="blink" />
            Mig
          </span>
          <span className={`${HSTACK} gap-1`}>
            <LED color="red" state="off" />
            Blk
          </span>
          <span className={`${HSTACK} gap-1`}>
            <LED color="info" state="on" />
            Info
          </span>
        </div>
      </Rack>

      <div className="mt-6">
        <div
          className="mb-3 font-mono text-[10px] uppercase tracking-widest text-[var(--ds-text-meta)]"
        >
          {'// Feature pillars'}
        </div>
        <div className="mt-5 grid grid-cols-[repeat(auto-fit,minmax(220px,1fr))] gap-3">
          {[
            { src: '/design/pillar-planet.svg', k: '01 · Sim', t: 'A living territory that ships with the code.' },
            { src: '/design/pillar-signal.svg', k: '02 · Telemetry', t: 'Every commit graphed like a waveform.' },
            { src: '/design/pillar-mixer.svg', k: '03 · Mix', t: 'Pick the channels that matter this week.' },
            { src: '/design/pillar-radar.svg', k: '04 · Sweep', t: "Release radar — what's landing, what's blocked." },
            { src: '/design/pillar-tape.svg', k: '05 · Tape', t: 'Full project history, persistent and rewindable.' },
          ].map((p) => (
            <div
              key={p.src}
              className="relative aspect-[4/3] overflow-hidden rounded-[var(--ds-cd-radius-md)] border border-black"
            >
              <Image
                src={p.src}
                alt=""
                fill
                className="object-cover"
                sizes="(min-width: 900px) 20vw, 100vw"
              />
              <div className="absolute inset-x-0 bottom-0 bg-gradient-to-t from-black/85 to-transparent p-3 pl-4 text-[var(--ds-text-primary)]">
                <div className="mb-0.5 font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-raw-amber)]">
                  {p.k}
                </div>
                <div className="text-[var(--text-lg)] font-semibold leading-snug tracking-tight">
                  {p.t}
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      <div className="mt-6">
        <TransportStrip
          state={tState}
          onAction={(a) => {
            if (a === 'play') setTState('playing');
            if (a === 'pause') setTState('paused');
            if (a === 'stop') setTState('stopped');
            if (a === 'rewind' || a === 'fastForward' || a === 'eject') setTState('stopped');
          }}
        />
      </div>
    </div>
  );
}
