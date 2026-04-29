"use client";

import { useEffect, useRef, useState } from "react";

export type PollState<T> =
  | { status: "loading"; data: null; error: null }
  | { status: "ok"; data: T; error: null }
  | { status: "error"; data: null; error: string };

const POLL_INTERVAL_MS = 30_000;

/**
 * Polls a GET endpoint every 30s. Pauses when the tab is hidden
 * (document.visibilityState === 'hidden'). Fires immediately on mount.
 * Resumes on visibility change. Clears interval on unmount.
 */
export function useDashboardPoll<T>(url: string): PollState<T> {
  const [state, setState] = useState<PollState<T>>({
    status: "loading",
    data: null,
    error: null,
  });
  const urlRef = useRef(url);

  useEffect(() => {
    let cancelled = false;

    async function run() {
      if (typeof document !== "undefined" && document.visibilityState === "hidden") {
        return;
      }
      try {
        const res = await fetch(urlRef.current);
        if (cancelled) return;
        if (!res.ok) {
          const text = await res.text().catch(() => "fetch error");
          if (!cancelled) setState({ status: "error", data: null, error: text });
          return;
        }
        const json = (await res.json()) as { ok: boolean; data: T };
        if (!cancelled) {
          if (!json.ok) {
            setState({ status: "error", data: null, error: "endpoint returned ok:false" });
          } else {
            setState({ status: "ok", data: json.data, error: null });
          }
        }
      } catch (e) {
        if (!cancelled) {
          setState({
            status: "error",
            data: null,
            error: e instanceof Error ? e.message : "unknown fetch error",
          });
        }
      }
    }

    void run();
    const id = setInterval(() => void run(), POLL_INTERVAL_MS);

    function onVisibility() {
      if (document.visibilityState === "visible") {
        void run();
      }
    }
    document.addEventListener("visibilitychange", onVisibility);

    return () => {
      cancelled = true;
      clearInterval(id);
      document.removeEventListener("visibilitychange", onVisibility);
    };
  }, []);

  return state;
}
