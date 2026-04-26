import { describe, it, expect } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import ErrorBanner, {
  DEFAULT_ERROR_MESSAGES,
  ERROR_SEVERITY,
} from "@/components/catalog/ErrorBanner";
import { makeError } from "@/lib/error-envelope";

describe("<ErrorBanner>", () => {
  it("renders default message per code", () => {
    for (const code of Object.keys(DEFAULT_ERROR_MESSAGES) as Array<keyof typeof DEFAULT_ERROR_MESSAGES>) {
      const env = makeError(
        code,
        // Pass empty default message so component falls back to DEFAULT_ERROR_MESSAGES.
        DEFAULT_ERROR_MESSAGES[code],
      );
      const html = renderToStaticMarkup(<ErrorBanner envelope={env} />);
      expect(html).toContain(DEFAULT_ERROR_MESSAGES[code]);
      expect(html).toContain(`data-error-code="${code}"`);
      expect(html).toContain(`data-severity="${ERROR_SEVERITY[code]}"`);
      expect(html).toContain('role="alert"');
    }
  });

  it("renders messageOverride when present and falls back to default otherwise", () => {
    const env = makeError("stale", DEFAULT_ERROR_MESSAGES.stale, {
      current_payload: {},
      current_updated_at: "2026-04-26T00:00:00.000Z",
    });
    const html = renderToStaticMarkup(
      <ErrorBanner envelope={env} messageOverride={{ stale: "Stale custom" }} />,
    );
    expect(html).toContain("Stale custom");
    expect(html).not.toContain(DEFAULT_ERROR_MESSAGES.stale);
  });

  it("renders Retry CTA when retry_hint is present", () => {
    const env = makeError("queue_full", DEFAULT_ERROR_MESSAGES.queue_full, {
      retry_after_seconds: 30,
    });
    const html = renderToStaticMarkup(<ErrorBanner envelope={env} />);
    expect(html).toContain("Retry in 30s");
  });

  it("renders Retry CTA when showRetry is true even without retry_hint", () => {
    const env = makeError("internal", DEFAULT_ERROR_MESSAGES.internal);
    const html = renderToStaticMarkup(<ErrorBanner envelope={env} showRetry />);
    expect(html).toContain("Retry");
  });

  it("hides Retry CTA when neither retry_hint nor showRetry is set", () => {
    const env = makeError("forbidden", DEFAULT_ERROR_MESSAGES.forbidden);
    const html = renderToStaticMarkup(<ErrorBanner envelope={env} />);
    expect(html).not.toContain("Retry");
  });

  it("returns null body when envelope is null", () => {
    const html = renderToStaticMarkup(<ErrorBanner envelope={null} />);
    expect(html).toBe("");
  });

  it("renders validation field list under details", () => {
    const env = makeError("validation", DEFAULT_ERROR_MESSAGES.validation, {
      fields: [
        { field: "slug", message: "required" },
        { field: "name", message: "min 3 chars" },
      ],
    });
    const html = renderToStaticMarkup(<ErrorBanner envelope={env} />);
    expect(html).toContain("slug");
    expect(html).toContain("required");
    expect(html).toContain("name");
    expect(html).toContain("min 3 chars");
  });

  it("renders lint_blocked failed_gate_ids as chips", () => {
    const env = makeError("lint_blocked", DEFAULT_ERROR_MESSAGES.lint_blocked, {
      failed_gate_ids: ["sprite_palette_match", "panel_padding_min"],
    });
    const html = renderToStaticMarkup(<ErrorBanner envelope={env} />);
    expect(html).toContain("sprite_palette_match");
    expect(html).toContain("panel_padding_min");
  });
});
