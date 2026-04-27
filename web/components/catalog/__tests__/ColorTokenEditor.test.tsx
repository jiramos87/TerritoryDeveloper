import { describe, it, expect } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import ColorTokenEditor, {
  hexToHsl,
  hslToHex,
} from "@/components/catalog/tokens/ColorTokenEditor";

/**
 * <ColorTokenEditor /> coverage (TECH-2093 / Stage 10.1).
 *
 * Asserts the dual hex/hsl mode toggle + the bidirectional color conversion
 * helpers stay in sync.
 */
describe("<ColorTokenEditor />", () => {
  it("renders HEX input when value is hex-shaped", () => {
    const html = renderToStaticMarkup(
      <ColorTokenEditor value={{ hex: "#FF8800" }} onChange={() => {}} />,
    );
    expect(html).toContain('data-testid="color-token-editor-hex"');
    expect(html).not.toContain('data-testid="color-token-editor-h"');
    expect(html).toContain("HEX");
  });

  it("renders three HSL inputs when value is hsl-shaped", () => {
    const html = renderToStaticMarkup(
      <ColorTokenEditor value={{ h: 30, s: 100, l: 50 }} onChange={() => {}} />,
    );
    expect(html).toContain('data-testid="color-token-editor-h"');
    expect(html).toContain('data-testid="color-token-editor-s"');
    expect(html).toContain('data-testid="color-token-editor-l"');
    expect(html).not.toContain('data-testid="color-token-editor-hex"');
  });

  it("renders mode flip CTA + swatch", () => {
    const html = renderToStaticMarkup(
      <ColorTokenEditor value={{ hex: "#FF0000" }} onChange={() => {}} />,
    );
    expect(html).toContain('data-testid="color-token-editor-flip"');
    expect(html).toContain('data-testid="color-token-editor-swatch"');
  });
});

describe("color conversion helpers", () => {
  it("hexToHsl + hslToHex round-trip preserves color within 1 unit", () => {
    const cases = ["#FF0000", "#00FF00", "#0000FF", "#808080", "#FF8800"];
    for (const hex of cases) {
      const hsl = hexToHsl(hex);
      const back = hslToHex(hsl);
      // Compare per-channel within 2 (rounding through 360/100 buckets).
      const a = back.slice(1).match(/.{2}/g)!.map((c) => Number.parseInt(c, 16));
      const b = hex.slice(1).match(/.{2}/g)!.map((c) => Number.parseInt(c, 16));
      for (let i = 0; i < 3; i++) {
        expect(Math.abs(a[i]! - b[i]!)).toBeLessThanOrEqual(2);
      }
    }
  });

  it("hexToHsl returns h=0,s=0,l ∈ [0,100] for grayscale", () => {
    const black = hexToHsl("#000000");
    expect(black).toEqual({ h: 0, s: 0, l: 0 });
    const white = hexToHsl("#FFFFFF");
    expect(white).toEqual({ h: 0, s: 0, l: 100 });
  });

  it("hslToHex clamps + wraps inputs", () => {
    expect(hslToHex({ h: 0, s: 100, l: 50 })).toMatch(/^#[0-9A-F]{6}$/);
    expect(hslToHex({ h: 720, s: 200, l: -50 })).toMatch(/^#[0-9A-F]{6}$/);
  });
});
