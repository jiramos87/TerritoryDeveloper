"use client";

/**
 * Publish dialog component (TECH-2570 / Stage 12.1).
 *
 * Reusable across all 8 catalog kinds. Pure presentational — caller
 * pre-computes `results` via `runLayer1` + `runLayer2` + `aggregateLintResults`
 * (TECH-2568 / TECH-2569) and passes them in as props.
 *
 * Submit gate (DEC-A30):
 *   - block rows present  → submit disabled.
 *   - warn rows present   → justification textarea required (non-empty after
 *                            trim).
 *   - info-only / empty   → submit enabled.
 *
 * @see ia/projects/asset-pipeline/stage-12.1 — TECH-2570 §Plan Digest
 */

import { useState } from "react";

import type { LintResult } from "@/lib/lint/types";

export type PublishDialogResults = {
  block: LintResult[];
  warn: LintResult[];
  info: LintResult[];
};

export type PublishDialogProps = {
  kind: string;
  entityId: string;
  versionId: string;
  results: PublishDialogResults;
  onSubmit: (justification?: string) => void | Promise<void>;
  onCancel: () => void;
};

/**
 * Pure submit-gate predicate — exported for unit testing. Returns true when
 * submit should be disabled per DEC-A30 gating rules.
 */
export function publishSubmitDisabled(
  results: PublishDialogResults,
  justification: string,
): boolean {
  if (results.block.length > 0) return true;
  if (results.warn.length > 0 && justification.trim().length === 0) return true;
  return false;
}

/**
 * Pure resolver for the justification arg passed to `onSubmit`. `undefined`
 * when no warn rows; trimmed string when warn rows present.
 */
export function publishJustificationArg(
  results: PublishDialogResults,
  justification: string,
): string | undefined {
  return results.warn.length > 0 ? justification.trim() : undefined;
}

function LintRow({ row }: { row: LintResult }) {
  return (
    <li
      className="ds-publish-dialog-row"
      data-testid={`publish-dialog-row-${row.severity}`}
    >
      <div className="ds-publish-dialog-row-header">
        <code className="ds-publish-dialog-rule-id">{row.rule_id}</code>
        <span className="ds-publish-dialog-row-message">{row.message}</span>
      </div>
      {(row.measured !== undefined || row.threshold !== undefined) && (
        <div className="ds-publish-dialog-row-chips">
          {row.measured !== undefined && row.measured !== null && (
            <span className="ds-publish-dialog-chip">
              measured: {String(row.measured)}
            </span>
          )}
          {row.threshold !== undefined && row.threshold !== null && (
            <span className="ds-publish-dialog-chip">
              threshold: {String(row.threshold)}
            </span>
          )}
        </div>
      )}
    </li>
  );
}

function SeveritySection({
  title,
  rows,
  severity,
}: {
  title: string;
  rows: LintResult[];
  severity: "block" | "warn" | "info";
}) {
  if (rows.length === 0) return null;
  return (
    <section
      className={`ds-publish-dialog-section ds-publish-dialog-section-${severity}`}
      data-testid={`publish-dialog-section-${severity}`}
    >
      <h3 className="ds-publish-dialog-section-title">
        {title} ({rows.length})
      </h3>
      <ul className="ds-publish-dialog-row-list">
        {rows.map((row, idx) => (
          <LintRow key={`${row.rule_id}-${idx}`} row={row} />
        ))}
      </ul>
    </section>
  );
}

export default function PublishDialog(props: PublishDialogProps) {
  const { kind, entityId, versionId, results, onSubmit, onCancel } = props;
  const [justification, setJustification] = useState("");

  const submitDisabled = publishSubmitDisabled(results, justification);
  const showJustification = results.warn.length > 0;

  const handleSubmit = () => {
    if (submitDisabled) return;
    const arg = publishJustificationArg(results, justification);
    void onSubmit(arg);
  };

  const totalRows =
    results.block.length + results.warn.length + results.info.length;

  return (
    <div
      className="ds-publish-dialog"
      data-testid="publish-dialog"
      data-kind={kind}
      data-entity-id={entityId}
      data-version-id={versionId}
    >
      <header className="ds-publish-dialog-header">
        <h2 className="ds-publish-dialog-title">Publish review</h2>
        <p className="ds-publish-dialog-summary">
          {totalRows === 0
            ? "No lint findings — ready to publish."
            : `${results.block.length} block · ${results.warn.length} warn · ${results.info.length} info`}
        </p>
      </header>

      <SeveritySection
        title="Block"
        rows={results.block}
        severity="block"
      />
      <SeveritySection
        title="Warn"
        rows={results.warn}
        severity="warn"
      />
      <SeveritySection
        title="Info"
        rows={results.info}
        severity="info"
      />

      {showJustification && (
        <label className="ds-publish-dialog-justification">
          <span className="ds-publish-dialog-justification-label">
            Justification (required to override warn rows)
          </span>
          <textarea
            className="ds-publish-dialog-justification-input"
            data-testid="publish-dialog-justification"
            value={justification}
            onChange={(e) => setJustification(e.target.value)}
            rows={3}
            placeholder="Why is publishing safe despite the warn rows above?"
          />
        </label>
      )}

      <footer className="ds-publish-dialog-footer">
        {results.block.length > 0 && (
          <p
            className="ds-publish-dialog-block-note"
            data-testid="publish-dialog-block-note"
          >
            Cannot publish — fix block rows above
          </p>
        )}
        <button
          type="button"
          className="ds-publish-dialog-cancel"
          data-testid="publish-dialog-cancel"
          onClick={onCancel}
        >
          Cancel
        </button>
        <button
          type="button"
          className="ds-publish-dialog-submit"
          data-testid="publish-dialog-submit"
          disabled={submitDisabled}
          onClick={handleSubmit}
        >
          Publish
        </button>
      </footer>
    </div>
  );
}
