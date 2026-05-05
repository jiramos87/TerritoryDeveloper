/**
 * Renders a `payload_kind='version_close'` journal row from
 * `ia_ship_stage_journal` (TECH-12645).
 *
 * Payload shape (validated server-side in `journal_append` tool):
 *   {plan_slug, version, tag, sha, validate_all_result:{ok, scripts[]}, sections_closed[]}
 *
 * Render: tag · v{N} · 7-char short sha · sections count · validate_all_result.ok badge.
 */

import type { ReactNode } from 'react';

interface ValidateAllResult {
  ok: boolean;
  scripts: string[];
  [key: string]: unknown;
}

export interface VersionClosePayload {
  plan_slug: string;
  version: number;
  tag: string;
  sha: string;
  validate_all_result: ValidateAllResult;
  sections_closed: string[];
  [key: string]: unknown;
}

interface VersionCloseEntryProps {
  recorded_at: string;
  payload: VersionClosePayload;
}

function shortSha(sha: string): string {
  return sha.slice(0, 7);
}

function formatTs(ts: string): string {
  return new Date(ts).toISOString().replace('T', ' ').replace(/\..+$/, '');
}

export function isVersionClosePayload(
  payload: Record<string, unknown> | null,
): payload is VersionClosePayload {
  if (!payload) return false;
  const v = payload.validate_all_result as Record<string, unknown> | undefined;
  return (
    typeof payload.plan_slug === 'string' &&
    typeof payload.version === 'number' &&
    typeof payload.tag === 'string' &&
    typeof payload.sha === 'string' &&
    !!v &&
    typeof v === 'object' &&
    typeof v.ok === 'boolean' &&
    Array.isArray(v.scripts) &&
    Array.isArray(payload.sections_closed)
  );
}

export function VersionCloseEntry({ recorded_at, payload }: VersionCloseEntryProps): ReactNode {
  const { plan_slug, version, tag, sha, validate_all_result, sections_closed } = payload;
  const okBadge = validate_all_result.ok
    ? 'bg-[var(--ds-raw-green)]/15 text-[var(--ds-raw-green)]'
    : 'bg-[var(--ds-raw-red)]/15 text-[var(--ds-raw-red)]';

  return (
    <div
      data-payload-kind="version_close"
      className="rounded-md border border-[var(--ds-border-subtle)] bg-[var(--ds-surface-1)] p-3"
    >
      <div className="flex flex-wrap items-baseline gap-2 font-mono">
        <span className="text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
          version-close
        </span>
        <span className="font-semibold text-[var(--ds-text)]">{tag}</span>
        <span className="text-xs text-[var(--ds-text-meta)]">v{version}</span>
        <span className="text-xs text-[var(--ds-text-meta)]">{plan_slug}</span>
        <code className="text-xs text-[var(--ds-text-muted)]">{shortSha(sha)}</code>
        <span className={`inline-flex rounded-full px-2 py-0.5 text-[10px] font-mono ${okBadge}`}>
          validate:all {validate_all_result.ok ? 'ok' : 'red'}
        </span>
        <span className="ml-auto text-[10px] text-[var(--ds-text-meta)]">
          {formatTs(recorded_at)}
        </span>
      </div>
      <div className="mt-2 text-xs text-[var(--ds-text-muted)]">
        Sections closed ({sections_closed.length}):{' '}
        {sections_closed.length === 0 ? (
          <span className="italic">none</span>
        ) : (
          sections_closed.map((s) => (
            <code
              key={s}
              className="mr-1 inline-block rounded bg-[var(--ds-surface-2)] px-1.5 py-0.5 font-mono text-[11px]"
            >
              {s}
            </code>
          ))
        )}
      </div>
      {validate_all_result.scripts.length > 0 && (
        <div className="mt-1 text-[10px] text-[var(--ds-text-meta)]">
          scripts: {validate_all_result.scripts.join(', ')}
        </div>
      )}
    </div>
  );
}
