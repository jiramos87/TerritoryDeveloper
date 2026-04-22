import { describe, it, expect } from 'vitest';
import {
  typeScale,
  spacing,
  motion,
  text,
  surface,
  accent,
} from '../design-tokens';
import palette from '../tokens/palette.json';

function remToNumber(rem: string): number {
  return parseFloat(rem.replace('rem', ''));
}

describe('design-tokens', () => {
  it('typeScale has 10 levels with strictly decreasing rem', () => {
    const entries = Object.values(typeScale);
    expect(entries).toHaveLength(10);
    for (let i = 0; i < entries.length - 1; i++) {
      const a = remToNumber(entries[i].rem);
      const b = remToNumber(entries[i + 1].rem);
      expect(a).toBeGreaterThan(b);
    }
  });

  it('spacing has 9 stops', () => {
    expect(Object.keys(spacing)).toHaveLength(9);
  });

  it('motion has instant, subtle, gentle, deliberate + reducedMotion', () => {
    expect(motion.instant).toBe(0);
    expect(motion.subtle).toBe(120);
    expect(motion.gentle).toBe(200);
    expect(motion.deliberate).toBe(320);
    expect(motion.reducedMotion.duration).toBe(0);
  });

  it('accent hex values match palette raw entries', () => {
    expect(accent.terrain).toBe(palette.raw.terrainGreen);
    expect(accent.water).toBe(palette.raw.waterBlue);
    expect(accent.warm).toBe(palette.raw.amber);
  });

  it('text and surface resolve to palette raws', () => {
    expect(text.primary).toBe(palette.raw.text);
    expect(text.secondary).toBe(palette.raw['grey-500']);
    expect(surface.canvas).toBe(palette.raw.black);
    expect(surface.raised).toBe(palette.raw.panel);
    expect(surface.sunken).toBe(palette.raw.sunken);
    expect(surface.inset).toBe(palette.raw.inset);
  });
});
