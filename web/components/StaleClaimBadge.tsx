/**
 * StaleClaimBadge — presentational pill for parallel-carcass section claims.
 *
 * Three states driven by `lastHeartbeat` + `timeoutMinutes`:
 *   - null heartbeat       → "free"   (no active claim row)
 *   - age > timeout (min)  → "stale"  (claim held but heartbeat past TTL)
 *   - age ≤ timeout        → "active" (claim healthy)
 *
 * Pure presentational — props in, span out. Threshold computed inline so the
 * badge can reuse the same payload across the sections page (per-card) and
 * the plan landing tile (aggregate). Colors via `ds-*` tokens only.
 *
 * Stage 2.2 / TECH-5245 of `parallel-carcass-rollout`.
 */

import type { ReactNode } from "react";

interface StaleClaimBadgeProps {
  lastHeartbeat: Date | null;
  timeoutMinutes: number;
}

type ClaimState = "free" | "active" | "stale";

function classifyClaim(
  lastHeartbeat: Date | null,
  timeoutMinutes: number,
): { state: ClaimState; ageMinutes: number | null } {
  if (!lastHeartbeat) return { state: "free", ageMinutes: null };
  const ageMs = Date.now() - lastHeartbeat.getTime();
  const ageMinutes = ageMs / 60_000;
  if (ageMinutes > timeoutMinutes) return { state: "stale", ageMinutes };
  return { state: "active", ageMinutes };
}

function formatAge(ageMinutes: number): string {
  if (ageMinutes < 1) return "<1m";
  if (ageMinutes < 60) return `${Math.floor(ageMinutes)}m`;
  const hours = ageMinutes / 60;
  if (hours < 24) return `${Math.floor(hours)}h`;
  return `${Math.floor(hours / 24)}d`;
}

export function StaleClaimBadge({
  lastHeartbeat,
  timeoutMinutes,
}: StaleClaimBadgeProps): ReactNode {
  const { state, ageMinutes } = classifyClaim(lastHeartbeat, timeoutMinutes);

  if (state === "free") {
    return (
      <span
        data-testid="claim-pill"
        data-claim-state="free"
        className="inline-flex items-center rounded-full border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-canvas)] px-2 py-0.5 font-mono text-xs text-[var(--ds-text-muted)]"
      >
        free
      </span>
    );
  }

  if (state === "stale") {
    return (
      <span
        data-testid="claim-pill"
        data-claim-state="stale"
        className="inline-flex items-center rounded-full border border-[var(--ds-raw-amber)] bg-[var(--ds-bg-panel)] px-2 py-0.5 font-mono text-xs text-[var(--ds-raw-amber)]"
      >
        {`stale ${formatAge(ageMinutes!)}`}
      </span>
    );
  }

  return (
    <span
      data-testid="claim-pill"
      data-claim-state="active"
      className="inline-flex items-center rounded-full border border-[var(--ds-raw-blue)] bg-[var(--ds-bg-panel)] px-2 py-0.5 font-mono text-xs text-[var(--ds-raw-blue)]"
    >
      {`active ${formatAge(ageMinutes!)}`}
    </span>
  );
}

export type { StaleClaimBadgeProps };
