/**
 * PublishDialog tests (TECH-2570 / Stage 12.1).
 *
 * Pure-helper coverage (`publishSubmitDisabled` + `publishJustificationArg`)
 * + server-rendered markup checks for severity sections + textarea visibility
 * + button disabled attribute. No DOM event firing — RTL not in repo deps.
 *
 * @see ia/projects/asset-pipeline/stage-12.1 — TECH-2570 §Test Blueprint
 */

import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import PublishDialog, {
  publishJustificationArg,
  publishSubmitDisabled,
  type PublishDialogResults,
} from "@/components/publish/PublishDialog";
import type { LintResult } from "@/lib/lint/types";

const BLOCK_ROW: LintResult = {
  rule_id: "audio.loudness_out_of_range",
  severity: "block",
  message: "LUFS -30 outside window",
  measured: -30,
  threshold: "[-23, -10] LUFS",
};
const WARN_ROW: LintResult = {
  rule_id: "sprite.missing_pivot",
  severity: "warn",
  message: "pivot offset unset",
};
const INFO_ROW: LintResult = {
  rule_id: "token.no_consumers",
  severity: "info",
  message: "no inbound refs",
};

const EMPTY: PublishDialogResults = { block: [], warn: [], info: [] };

describe("publishSubmitDisabled — TECH-2570 §Acceptance row 4 + 5", () => {
  it("block row → disabled true regardless of justification", () => {
    expect(publishSubmitDisabled({ ...EMPTY, block: [BLOCK_ROW] }, "")).toBe(
      true,
    );
    expect(
      publishSubmitDisabled({ ...EMPTY, block: [BLOCK_ROW] }, "ack"),
    ).toBe(true);
  });

  it("warn row + empty justification → disabled true", () => {
    expect(publishSubmitDisabled({ ...EMPTY, warn: [WARN_ROW] }, "")).toBe(
      true,
    );
  });

  it("warn row + whitespace-only justification → disabled true (trim-aware)", () => {
    expect(publishSubmitDisabled({ ...EMPTY, warn: [WARN_ROW] }, "   ")).toBe(
      true,
    );
  });

  it("warn row + non-empty justification → disabled false", () => {
    expect(
      publishSubmitDisabled({ ...EMPTY, warn: [WARN_ROW] }, "ack"),
    ).toBe(false);
  });

  it("info-only → disabled false", () => {
    expect(publishSubmitDisabled({ ...EMPTY, info: [INFO_ROW] }, "")).toBe(
      false,
    );
  });

  it("empty results → disabled false", () => {
    expect(publishSubmitDisabled(EMPTY, "")).toBe(false);
  });
});

describe("publishJustificationArg — TECH-2570 §Acceptance row 6 + 7", () => {
  it("warn rows present → returns trimmed justification string", () => {
    expect(
      publishJustificationArg({ ...EMPTY, warn: [WARN_ROW] }, "  ack  "),
    ).toBe("ack");
  });

  it("no warn rows → returns undefined", () => {
    expect(publishJustificationArg(EMPTY, "anything")).toBeUndefined();
    expect(
      publishJustificationArg({ ...EMPTY, info: [INFO_ROW] }, ""),
    ).toBeUndefined();
  });
});

describe("<PublishDialog /> render", () => {
  it("block row → submit button has disabled attr + block note rendered", () => {
    const html = renderToStaticMarkup(
      <PublishDialog
        kind="audio"
        entityId="42"
        versionId="100"
        results={{ ...EMPTY, block: [BLOCK_ROW] }}
        onSubmit={() => {}}
        onCancel={() => {}}
      />,
    );
    expect(html).toContain('data-testid="publish-dialog-section-block"');
    expect(html).toContain('data-testid="publish-dialog-block-note"');
    const submitTag = html.match(
      /<button[^>]*data-testid="publish-dialog-submit"[^>]*>/,
    );
    expect(submitTag?.[0]).toMatch(/disabled=""|disabled/);
  });

  it("warn row → justification textarea visible", () => {
    const html = renderToStaticMarkup(
      <PublishDialog
        kind="sprite"
        entityId="1"
        versionId="1"
        results={{ ...EMPTY, warn: [WARN_ROW] }}
        onSubmit={() => {}}
        onCancel={() => {}}
      />,
    );
    expect(html).toContain('data-testid="publish-dialog-justification"');
    expect(html).toContain('data-testid="publish-dialog-section-warn"');
  });

  it("info-only → no justification textarea, submit enabled", () => {
    const html = renderToStaticMarkup(
      <PublishDialog
        kind="token"
        entityId="1"
        versionId="1"
        results={{ ...EMPTY, info: [INFO_ROW] }}
        onSubmit={() => {}}
        onCancel={() => {}}
      />,
    );
    expect(html).not.toContain('data-testid="publish-dialog-justification"');
    const submitTag = html.match(
      /<button[^>]*data-testid="publish-dialog-submit"[^>]*>/,
    );
    expect(submitTag?.[0]).not.toMatch(/disabled=""/);
  });

  it("empty results → ready-to-publish summary, submit enabled, no textarea", () => {
    const html = renderToStaticMarkup(
      <PublishDialog
        kind="sprite"
        entityId="1"
        versionId="1"
        results={EMPTY}
        onSubmit={() => {}}
        onCancel={() => {}}
      />,
    );
    expect(html).toContain("No lint findings");
    expect(html).not.toContain('data-testid="publish-dialog-justification"');
    expect(html).not.toContain('data-testid="publish-dialog-block-note"');
  });

  it("block + warn + info → all 3 sections render in order", () => {
    const html = renderToStaticMarkup(
      <PublishDialog
        kind="sprite"
        entityId="1"
        versionId="1"
        results={{ block: [BLOCK_ROW], warn: [WARN_ROW], info: [INFO_ROW] }}
        onSubmit={() => {}}
        onCancel={() => {}}
      />,
    );
    const blockIdx = html.indexOf("publish-dialog-section-block");
    const warnIdx = html.indexOf("publish-dialog-section-warn");
    const infoIdx = html.indexOf("publish-dialog-section-info");
    expect(blockIdx).toBeGreaterThanOrEqual(0);
    expect(warnIdx).toBeGreaterThan(blockIdx);
    expect(infoIdx).toBeGreaterThan(warnIdx);
  });

  it("cancel button always renders", () => {
    const html = renderToStaticMarkup(
      <PublishDialog
        kind="sprite"
        entityId="1"
        versionId="1"
        results={EMPTY}
        onSubmit={() => {}}
        onCancel={() => {}}
      />,
    );
    expect(html).toContain('data-testid="publish-dialog-cancel"');
  });

  it("measured + threshold chips render when present", () => {
    const html = renderToStaticMarkup(
      <PublishDialog
        kind="audio"
        entityId="1"
        versionId="1"
        results={{ ...EMPTY, block: [BLOCK_ROW] }}
        onSubmit={() => {}}
        onCancel={() => {}}
      />,
    );
    expect(html).toContain("measured: -30");
    expect(html).toContain("threshold: [-23, -10] LUFS");
  });
});
