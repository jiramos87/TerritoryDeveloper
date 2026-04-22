import { describe, it, expect } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';

import { Bezel, LED, Rack, Screen } from '@/components/console';

describe('console chrome frame (Rack, Bezel, Screen, LED)', () => {
  it('renders Rack with tones and padding without throw', () => {
    const tones = ['default', 'muted'] as const;
    const pads = ['sm', 'md', 'lg'] as const;
    for (const tone of tones) {
      for (const padding of pads) {
        const html = renderToStaticMarkup(
          <Rack tone={tone} padding={padding} label="TEST">
            <span>child</span>
          </Rack>
        );
        expect(html).toContain('child');
        expect(html).toContain('--ds-spacing-');
      }
    }
  });

  it('renders Bezel with thin and full padding', () => {
    const a = renderToStaticMarkup(
      <Bezel tone="default" padding="md" thin>
        <span>x</span>
      </Bezel>
    );
    expect(a).toContain('x');
    const b = renderToStaticMarkup(
      <Bezel tone="muted" padding="lg">
        <span>y</span>
      </Bezel>
    );
    expect(b).toContain('y');
  });

  it('renders Screen tones and inset', () => {
    for (const tone of ['dark', 'readout'] as const) {
      const html = renderToStaticMarkup(
        <Screen tone={tone} inset sweep={false}>
          readout
        </Screen>
      );
      expect(html).toContain('readout');
      expect(html).toContain('--ds-surface-canvas');
    }
  });

  it('renders LED matrix of state × color', () => {
    const states = ['off', 'on', 'blink', 'error'] as const;
    const colors = ['green', 'amber', 'red', 'info'] as const;
    for (const state of states) {
      for (const color of colors) {
        const html = renderToStaticMarkup(<LED state={state} color={color} title="t" />);
        expect(html).toMatch(/span/);
        if (state === 'on' || state === 'blink' || state === 'error') {
          expect(html).toContain('--ds-raw-');
        }
      }
    }
  });
});
