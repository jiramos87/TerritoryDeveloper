"use client";

import { useEffect, useState } from "react";

import AudioList, {
  type AudioListFilter,
  type AudioListRow,
} from "@/components/catalog/AudioList";
import {
  fetchAudioList,
  type AudioListItemDto,
} from "@/lib/api/audio-renders";

/**
 * Audio catalog list (TECH-1958 / asset-pipeline Stage 9.1 T9.1.3).
 *
 * Replaces the Stage 6.5 sentinel with a real list of `audio` kind
 * entities. Mirrors the sprite-list pattern (TECH-1672) verbatim — the
 * pure presentational `<AudioList />` lives under
 * `web/components/catalog/`, this page owns fetch + filter state.
 *
 * Filter changes remount `<AudioCatalogPageInner />` via `key={filter}`
 * so each tab gets a fresh mount with `loading=true` initial state. This
 * sidesteps the `react-hooks/set-state-in-effect` lint rule.
 */
export default function AudioCatalogPage() {
  const [filter, setFilter] = useState<AudioListFilter>("active");
  return (
    <AudioCatalogPageInner
      key={filter}
      filter={filter}
      onFilterChange={setFilter}
    />
  );
}

function AudioCatalogPageInner({
  filter,
  onFilterChange,
}: {
  filter: AudioListFilter;
  onFilterChange: (next: AudioListFilter) => void;
}) {
  const [rows, setRows] = useState<AudioListRow[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    fetchAudioList(filter, 50).then((res) => {
      if (cancelled) return;
      if (!res.ok) {
        setError(res.error);
        setRows([]);
        setLoading(false);
        return;
      }
      const next: AudioListRow[] = res.data.items.map(
        (it: AudioListItemDto) => ({
          entity_id: it.entity_id,
          slug: it.slug,
          display_name: it.display_name,
          status: it.retired_at ? "retired" : "active",
          duration_ms: it.duration_ms,
          loudness_lufs: it.loudness_lufs,
          updated_at: it.updated_at,
        }),
      );
      setRows(next);
      setError(null);
      setLoading(false);
    });
    return () => {
      cancelled = true;
    };
  }, [filter]);

  return (
    <AudioList
      rows={rows}
      filter={filter}
      onFilterChange={onFilterChange}
      loading={loading}
      error={error}
    />
  );
}
