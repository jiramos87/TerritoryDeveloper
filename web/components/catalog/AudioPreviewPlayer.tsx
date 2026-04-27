"use client";

import { useEffect, useRef, useState } from "react";

export type AudioPreviewPlayerProps = {
  /**
   * Resolved blob URL or remote URL. When null/undefined, renders an
   * empty-state placeholder.
   */
  blobUrl: string | null;
  /** MIME type for the audio source; defaults to `audio/ogg`. */
  mimeType?: string;
  /** Optional label shown above the player (e.g. "Latest variant"). */
  label?: string;
};

/**
 * Native `<audio>` wrapper for catalog audio previews (TECH-1958).
 *
 * Browser handles play / pause / seek; component owns load + error
 * states and exposes a stable `data-testid` for smoke tests. Waveform
 * visualisation is a §Implementer Latitude follow-up — not in MVP scope.
 *
 * The shell remounts the inner playback subtree via `key={blobUrl ?? ""}`
 * so the error state automatically resets on source change without
 * tripping the `react-hooks/set-state-in-effect` /
 * `react-hooks/refs` lint rules.
 */
export default function AudioPreviewPlayer(props: AudioPreviewPlayerProps) {
  const { blobUrl, mimeType = "audio/ogg", label } = props;

  if (!blobUrl) {
    return (
      <div
        data-testid="audio-preview-empty"
        className="text-[var(--ds-text-muted)]"
      >
        No render available — submit the render form to generate a preview.
      </div>
    );
  }

  return (
    <AudioPreviewPlayerInner
      key={blobUrl}
      blobUrl={blobUrl}
      mimeType={mimeType}
      label={label}
    />
  );
}

function AudioPreviewPlayerInner({
  blobUrl,
  mimeType,
  label,
}: {
  blobUrl: string;
  mimeType: string;
  label?: string;
}) {
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const el = audioRef.current;
    if (!el) return;
    el.load();
  }, []);

  return (
    <div data-testid="audio-preview-player" className="flex flex-col gap-2">
      {label ? (
        <span className="text-[length:var(--ds-font-size-caption)] text-[var(--ds-text-muted)]">
          {label}
        </span>
      ) : null}
      <audio
        ref={audioRef}
        controls
        preload="auto"
        onError={() => setError("Audio failed to load")}
        data-testid="audio-preview-element"
      >
        <source src={blobUrl} type={mimeType} />
        Your browser does not support the audio element.
      </audio>
      {error ? (
        <span
          role="alert"
          data-testid="audio-preview-error"
          className="text-[var(--ds-text-accent-critical)]"
        >
          {error}
        </span>
      ) : null}
    </div>
  );
}
