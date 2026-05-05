"use client";

import { useCallback, useEffect, useRef, useState } from "react";

import type { SearchResultRow } from "@/lib/catalog/search-query";

type SearchState = {
  results: SearchResultRow[];
  loading: boolean;
  error: string | null;
};

const DEBOUNCE_MS = 200;

/**
 * Debounced search hook for the global Cmd+K search panel.
 * Aborts in-flight fetches on new input; returns loading+error states.
 */
export function useSearchDebounce(q: string): SearchState {
  const [state, setState] = useState<SearchState>({
    results: [],
    loading: false,
    error: null,
  });

  const abortRef = useRef<AbortController | null>(null);

  const fetchResults = useCallback(async (query: string, signal: AbortSignal) => {
    setState((s) => ({ ...s, loading: true, error: null }));
    try {
      const params = new URLSearchParams({ q: query, limit: "50" });
      const res = await fetch(`/api/catalog/search?${params}`, { signal });
      if (!res.ok) {
        const text = await res.text();
        setState({ results: [], loading: false, error: text });
        return;
      }
      const json = (await res.json()) as {
        ok: boolean;
        data: { results: SearchResultRow[]; total: number };
      };
      setState({ results: json.data.results, loading: false, error: null });
    } catch (err) {
      if ((err as { name?: string }).name === "AbortError") return;
      setState({ results: [], loading: false, error: String(err) });
    }
  }, []);

  const trimmed = q.trim();

  useEffect(() => {
    if (!trimmed) {
      abortRef.current?.abort();
      return;
    }

    const timer = setTimeout(() => {
      abortRef.current?.abort();
      const controller = new AbortController();
      abortRef.current = controller;
      void fetchResults(trimmed, controller.signal);
    }, DEBOUNCE_MS);

    return () => clearTimeout(timer);
  }, [trimmed, fetchResults]);

  if (!trimmed) {
    return { results: [], loading: false, error: null };
  }
  return state;
}
