"use client";

import type { ReactNode } from "react";

export type DashboardCardProps = {
  title: string;
  children?: ReactNode;
  loading?: boolean;
  error?: string | null;
  testId?: string;
};

export function DashboardCard({ title, children, loading, error, testId }: DashboardCardProps) {
  return (
    <section
      data-testid={testId}
      className="flex flex-col gap-[var(--ds-spacing-sm)] rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] p-[var(--ds-spacing-md)]"
    >
      <h2 className="text-[length:var(--ds-font-size-h4)] font-semibold text-[var(--ds-text-primary)]">
        {title}
      </h2>
      {error ? (
        <p
          data-testid={testId ? `${testId}-error` : undefined}
          role="alert"
          className="text-[var(--ds-text-accent-critical)] text-sm"
        >
          {error}
        </p>
      ) : loading ? (
        <div
          data-testid={testId ? `${testId}-skeleton` : undefined}
          className="h-16 animate-pulse rounded bg-[var(--ds-border-subtle)]"
          aria-label="Loading…"
        />
      ) : (
        children
      )}
    </section>
  );
}
