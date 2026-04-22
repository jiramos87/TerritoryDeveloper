'use client';

import { useState, type ReactNode } from 'react';

import {
  Bezel,
  LED,
  Rack,
  Screen,
  TapeReel,
  TransportStrip,
  VuStrip,
  type TransportAction,
  type TransportStripState,
} from '@/components/console';

/**
 * Dev-only demo island for Stage 25 console primitives (interactive transport).
 */
export function ConsoleChromeShowcase(): ReactNode {
  const [transportState, setTransportState] = useState<TransportStripState>('stopped');
  const [lastAction, setLastAction] = useState<TransportAction | null>(null);

  const onAction = (action: TransportAction): void => {
    setLastAction(action);
    if (action === 'play') setTransportState('playing');
    else if (action === 'pause') setTransportState('paused');
    else if (action === 'stop') setTransportState('stopped');
  };

  return (
    <div className="space-y-6">
      <Rack label="CONSOLE" tone="default" padding="md">
        <div className="flex flex-col gap-4">
          <Bezel tone="default" padding="md">
            <Screen tone="readout" inset sweep={false}>
              <span className="font-mono text-sm tracking-wide">READOUT · OK</span>
            </Screen>
          </Bezel>
          <div className="flex flex-wrap gap-3" aria-label="LED matrix demo">
            <LED state="on" color="green" title="Power on" />
            <LED state="blink" color="amber" title="Activity" />
            <LED state="off" color="red" title="Standby" />
            <LED state="error" color="green" title="Error state" />
          </div>
          <div className="flex flex-wrap items-center gap-6">
            <div className="flex flex-col gap-1">
              <span className="text-[10px] font-mono uppercase tracking-widest text-text-muted">
                Tape reel
              </span>
              <TapeReel spinning size="md" />
            </div>
            <div className="flex min-w-[200px] flex-col gap-1">
              <span className="text-[10px] font-mono uppercase tracking-widest text-text-muted">
                VU strip
              </span>
              <VuStrip level={0.72} peak />
            </div>
          </div>
          <div className="flex flex-col gap-1">
            <span className="text-[10px] font-mono uppercase tracking-widest text-text-muted">
              Transport
            </span>
            <TransportStrip state={transportState} onAction={onAction} />
          </div>
          {lastAction ? (
            <p className="font-mono text-xs text-text-muted">Last transport action: {lastAction}</p>
          ) : null}
        </div>
      </Rack>
    </div>
  );
}
