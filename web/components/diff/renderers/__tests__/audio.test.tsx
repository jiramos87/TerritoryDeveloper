/**
 * AudioDiff render tests (TECH-3304 / Stage 14.3).
 *
 * @see web/components/diff/renderers/audio.tsx
 */
import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import AudioDiff from "@/components/diff/renderers/audio";
import type { KindDiff } from "@/lib/diff/kind-schemas";

import addedFixture from "./fixtures/audio-added.json" with { type: "json" };
import removedFixture from "./fixtures/audio-removed.json" with { type: "json" };
import changedFixture from "./fixtures/audio-changed.json" with { type: "json" };

describe("AudioDiff (TECH-3304)", () => {
  it("renders added field names", () => {
    const html = renderToStaticMarkup(
      <AudioDiff diff={addedFixture as KindDiff} />,
    );
    expect(html).toContain('data-testid="audio-renderer"');
    expect(html).toContain("audio_path");
    expect(html).toContain("tags");
    expect(html).toContain("bg-green-50");
  });

  it("renders removed field names", () => {
    const html = renderToStaticMarkup(
      <AudioDiff diff={removedFixture as KindDiff} />,
    );
    expect(html).toContain("waveform_path");
    expect(html).toContain("bg-red-50");
  });

  it("routes changed fields to blob / scalar / list fallbacks", () => {
    const html = renderToStaticMarkup(
      <AudioDiff diff={changedFixture as KindDiff} />,
    );
    expect(html).toContain('data-testid="route-by-hint"');
    // blob
    expect(html).toContain("audio/old.wav");
    expect(html).toContain("audio/new.wav");
    // scalar (boolean)
    expect(html).toContain("false");
    expect(html).toContain("true");
    // list
    expect(html).toContain("+ long");
  });
});
