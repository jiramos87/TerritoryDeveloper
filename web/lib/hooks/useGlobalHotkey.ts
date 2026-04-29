"use client";

import { useEffect } from "react";

type UseGlobalHotkeyOpts = {
  /** Called when the hotkey fires outside an active text input. */
  onTrigger: () => void;
  /** Disabled when true (e.g. when panel is already open and you want Esc only). */
  disabled?: boolean;
};

/**
 * Binds Cmd+K (mac) / Ctrl+K (win/linux) globally.
 * Skips when focus is inside a text-entry element to avoid hijacking
 * browser/application shortcuts in input contexts.
 */
export function useGlobalHotkey({ onTrigger, disabled }: UseGlobalHotkeyOpts) {
  useEffect(() => {
    if (disabled) return;

    function handler(e: KeyboardEvent) {
      const isMac = navigator.platform.toUpperCase().includes("MAC");
      const modDown = isMac ? e.metaKey : e.ctrlKey;
      if (!modDown || e.key !== "k") return;

      const tag = (e.target as HTMLElement | null)?.tagName ?? "";
      const isInput =
        tag === "INPUT" || tag === "TEXTAREA" || tag === "SELECT" ||
        (e.target as HTMLElement | null)?.isContentEditable;
      if (isInput) return;

      e.preventDefault();
      onTrigger();
    }

    window.addEventListener("keydown", handler);
    return () => window.removeEventListener("keydown", handler);
  }, [onTrigger, disabled]);
}
