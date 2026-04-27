// TECH-2094 / Stage 10.1 — <ButtonPreview /> SSR shape + slot DOM contract.

import { describe, expect, test } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import ButtonPreview from "@/components/preview/ButtonPreview";
import EntityPreview from "@/components/preview/EntityPreview";

import canonical from "@/__fixtures__/preview/button-canonical.json";

describe("<ButtonPreview /> SSR shape (TECH-2094)", () => {
  test("renders all 6 sprite slots in DOM", () => {
    const html = renderToStaticMarkup(
      <ButtonPreview spriteSlots={canonical.spriteSlots} label={canonical.label} />,
    );
    for (const slot of ["idle", "hover", "pressed", "disabled", "icon", "badge"] as const) {
      expect(html).toContain(`data-testid="button-preview-slot-${slot}"`);
    }
  });

  test("idle slot is the only visible slot by default (others gated via CSS)", () => {
    const html = renderToStaticMarkup(
      <ButtonPreview spriteSlots={canonical.spriteSlots} label="X" />,
    );
    // Slot DOM nodes always present; visibility transitions handled by preview.css.
    expect(html).toContain("button-preview-slot--idle");
    expect(html).toContain("button-preview-slot--hover");
    expect(html).toContain("button-preview-slot--pressed");
    expect(html).toContain("button-preview-slot--disabled");
  });

  test("aria-disabled='true' when disabled prop set", () => {
    const html = renderToStaticMarkup(
      <ButtonPreview spriteSlots={canonical.spriteSlots} label="X" disabled />,
    );
    expect(html).toMatch(/aria-disabled="true"/);
  });

  test("missing slots still render the empty DOM node (no crash)", () => {
    const html = renderToStaticMarkup(<ButtonPreview label="Bare" />);
    for (const slot of ["idle", "hover", "pressed", "disabled", "icon", "badge"] as const) {
      expect(html).toContain(`data-testid="button-preview-slot-${slot}"`);
    }
  });
});

describe("<EntityPreview kind='button'> token CSS injection (TECH-2094)", () => {
  test("emits CSS custom properties from tokens map at preview root", () => {
    const html = renderToStaticMarkup(
      <EntityPreview kind="button" tokens={canonical.tokens} label={canonical.label} />,
    );
    expect(html).toMatch(/--token-palette-bg:#3366cc/);
    expect(html).toMatch(/--token-frame-radius:4px/);
    expect(html).toMatch(/data-kind="button"/);
  });

  test("normalizes token keys without -- prefix", () => {
    const html = renderToStaticMarkup(
      <EntityPreview kind="button" tokens={{ "token-foo": "bar" }} label="X" />,
    );
    expect(html).toMatch(/--token-foo:bar/);
  });
});
