/**
 * VersionCloseEntry render + predicate tests — TECH-12645.
 *
 * Asserts:
 *   - `isVersionClosePayload` returns true on full-shape payload, false on missing fields.
 *   - Component renders tag, version label, short sha, sections count, validate ok badge.
 *   - validate_all_result.ok=false → "validate:all red" badge text.
 *   - empty sections_closed → "none" italic.
 */

import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import {
  isVersionClosePayload,
  VersionCloseEntry,
  type VersionClosePayload,
} from "../timeline/VersionCloseEntry";

const FULL: VersionClosePayload = {
  plan_slug: "ship-protocol",
  version: 2,
  tag: "ship-protocol-v2",
  sha: "abcdef0123456789",
  validate_all_result: { ok: true, scripts: ["validate:all"] },
  sections_closed: ["arch", "ship-final"],
};

describe("isVersionClosePayload predicate", () => {
  it("full payload → true", () => {
    expect(isVersionClosePayload({ ...FULL })).toBe(true);
  });

  it("null → false", () => {
    expect(isVersionClosePayload(null)).toBe(false);
  });

  it("missing tag → false", () => {
    const { tag, ...rest } = FULL;
    void tag;
    expect(isVersionClosePayload(rest as Record<string, unknown>)).toBe(false);
  });

  it("validate_all_result.ok non-boolean → false", () => {
    expect(
      isVersionClosePayload({
        ...FULL,
        validate_all_result: { ok: "yes", scripts: [] },
      } as unknown as Record<string, unknown>),
    ).toBe(false);
  });

  it("sections_closed non-array → false", () => {
    expect(
      isVersionClosePayload({
        ...FULL,
        sections_closed: "arch,ship-final",
      } as unknown as Record<string, unknown>),
    ).toBe(false);
  });
});

describe("VersionCloseEntry render", () => {
  it("renders tag, version label, short sha, sections, ok badge", () => {
    const html = renderToStaticMarkup(
      <VersionCloseEntry
        recorded_at="2026-05-05T12:00:00Z"
        payload={FULL}
      />,
    );
    expect(html).toContain("ship-protocol-v2");
    expect(html).toContain("v2");
    expect(html).toContain("abcdef0"); // 7-char short sha
    expect(html).not.toContain("abcdef01"); // confirm 7-char trim
    expect(html).toContain("validate:all ok");
    expect(html).toContain("Sections closed (2)");
    expect(html).toContain(">arch<");
    expect(html).toContain(">ship-final<");
    expect(html).toContain('data-payload-kind="version_close"');
  });

  it("validate_all_result.ok=false → red badge text", () => {
    const html = renderToStaticMarkup(
      <VersionCloseEntry
        recorded_at="2026-05-05T12:00:00Z"
        payload={{
          ...FULL,
          validate_all_result: { ok: false, scripts: ["validate:all"] },
        }}
      />,
    );
    expect(html).toContain("validate:all red");
  });

  it("empty sections_closed → 'none' italic", () => {
    const html = renderToStaticMarkup(
      <VersionCloseEntry
        recorded_at="2026-05-05T12:00:00Z"
        payload={{ ...FULL, sections_closed: [] }}
      />,
    );
    expect(html).toContain("Sections closed (0)");
    expect(html).toContain("none");
  });
});
