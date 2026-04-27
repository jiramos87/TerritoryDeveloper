"use client";

import { useEffect, useState } from "react";

import AudioPreviewPlayer from "@/components/catalog/AudioPreviewPlayer";
import {
  fetchAudioBySlug,
  type AudioDetailDto,
} from "@/lib/api/audio-renders";

/**
 * Audio entity detail client view (TECH-1958).
 *
 * Owns the interactive state: detail fetch, preview player wiring, and
 * the placeholder for archetype-driven render form + render-run history
 * (those land once archetype seeds and worker dispatch are in place
 * downstream of Stage 9.1). The shell is render-form-ready: pass an
 * `archetype_version_id` + bound `params_json` to the existing
 * `enqueueAudioRender` helper from `@/lib/api/audio-renders`.
 */
export default function AudioDetailClient({ slug }: { slug: string }) {
  const [audio, setAudio] = useState<AudioDetailDto | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    fetchAudioBySlug(slug).then((res) => {
      if (cancelled) return;
      if (!res.ok) {
        setLoadError(res.error);
        setAudio(null);
        setLoading(false);
        return;
      }
      setAudio(res.data);
      setLoadError(null);
      setLoading(false);
    });
    return () => {
      cancelled = true;
    };
  }, [slug]);

  if (loading) {
    return (
      <p
        data-testid="audio-detail-loading"
        className="text-[var(--ds-text-muted)]"
      >
        Loading audio…
      </p>
    );
  }
  if (loadError || !audio) {
    return (
      <p
        data-testid="audio-detail-error"
        role="alert"
        className="text-[var(--ds-text-accent-critical)]"
      >
        {loadError ?? "Audio entity not found"}
      </p>
    );
  }

  // Resolve preview URL: prefer promoted assets_path; fall back to gen://
  // URI when the entity is still draft. The /api/blob proxy resolves
  // gen:// URIs server-side.
  const blobUrl = audio.assets_path
    ? `/${audio.assets_path}`
    : audio.source_uri
      ? `/api/blob?uri=${encodeURIComponent(audio.source_uri)}`
      : null;

  return (
    <section
      data-testid="audio-detail"
      className="flex flex-col gap-[var(--ds-space-md)]"
    >
      <header className="flex items-baseline justify-between">
        <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">
          {audio.display_name}
        </h1>
        <span className="text-[var(--ds-text-muted)]">
          {audio.retired_at ? "retired" : "active"}
        </span>
      </header>

      <dl
        data-testid="audio-detail-meta"
        className="grid grid-cols-2 gap-[var(--ds-space-sm)]"
      >
        <dt className="text-[var(--ds-text-muted)]">Slug</dt>
        <dd>{audio.slug}</dd>
        <dt className="text-[var(--ds-text-muted)]">Duration</dt>
        <dd>{audio.duration_ms !== null ? `${audio.duration_ms} ms` : "—"}</dd>
        <dt className="text-[var(--ds-text-muted)]">Sample rate</dt>
        <dd>
          {audio.sample_rate !== null ? `${audio.sample_rate} Hz` : "—"}
        </dd>
        <dt className="text-[var(--ds-text-muted)]">Channels</dt>
        <dd>{audio.channels ?? "—"}</dd>
        <dt className="text-[var(--ds-text-muted)]">Loudness (LUFS)</dt>
        <dd>
          {audio.loudness_lufs !== null
            ? audio.loudness_lufs.toFixed(2)
            : "—"}
        </dd>
        <dt className="text-[var(--ds-text-muted)]">Peak (dB)</dt>
        <dd>{audio.peak_db !== null ? audio.peak_db.toFixed(2) : "—"}</dd>
        <dt className="text-[var(--ds-text-muted)]">Fingerprint</dt>
        <dd className="font-mono text-[length:var(--ds-font-size-caption)]">
          {audio.fingerprint ?? "—"}
        </dd>
      </dl>

      <AudioPreviewPlayer
        blobUrl={blobUrl}
        mimeType="audio/ogg"
        label={audio.assets_path ? "Promoted asset" : "Latest render"}
      />
    </section>
  );
}
