import type { ReactNode } from "react";

/**
 * Tooltip — RSC-safe hover popover.
 *
 * Pure CSS (no client state) — uses Tailwind `group/tooltip` + `group-hover/tooltip:*`
 * to reveal a positioned popover on hover or keyboard focus. Safe inside
 * Server Components; no `'use client'` needed.
 *
 * Usage:
 *   <Tooltip content="…definition…">
 *     <strong>{term}</strong>
 *   </Tooltip>
 */
export interface TooltipProps {
  /** Hover-reveal content (string or node). */
  content: ReactNode;
  /** Optional small label rendered above the content (e.g. canonical term). */
  label?: string;
  /** Trigger element — typically a span/strong wrapping highlighted text. */
  children: ReactNode;
  /** Optional className applied to the wrapper span. */
  className?: string;
}

export function Tooltip({ content, label, children, className }: TooltipProps) {
  const wrapBase =
    "group/tooltip relative inline-block cursor-help align-baseline";
  const wrap = className ? `${wrapBase} ${className}` : wrapBase;

  return (
    <span className={wrap} tabIndex={0}>
      {children}
      <span
        role="tooltip"
        className={[
          "pointer-events-none absolute left-1/2 top-full z-50",
          "mt-1 -translate-x-1/2",
          "min-w-[14rem] max-w-[24rem]",
          "rounded-md border border-[var(--ds-border-subtle)]",
          "bg-[var(--ds-bg-panel)] px-3 py-2",
          "text-left font-sans text-xs leading-snug text-[var(--ds-text-primary)]",
          "shadow-lg",
          "opacity-0 invisible",
          "transition-opacity duration-100",
          "group-hover/tooltip:opacity-100 group-hover/tooltip:visible",
          "group-focus/tooltip:opacity-100 group-focus/tooltip:visible",
        ].join(" ")}
      >
        {label && (
          <span className="mb-1 block font-mono text-[10px] uppercase tracking-wider text-[var(--ds-text-muted)]">
            {label}
          </span>
        )}
        <span className="block whitespace-normal">{content}</span>
      </span>
    </span>
  );
}
