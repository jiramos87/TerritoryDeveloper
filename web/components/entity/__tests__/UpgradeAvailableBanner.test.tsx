import { describe, it, expect } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import UpgradeAvailableBanner from "@/components/entity/UpgradeAvailableBanner";

/**
 * Static-render coverage for <UpgradeAvailableBanner /> (TECH-2462).
 * Mirrors archetype component test shape — no jsdom; assert markup tokens.
 */
describe("<UpgradeAvailableBanner />", () => {
  it("renders banner + CTA when pinned version lags latest", () => {
    const html = renderToStaticMarkup(
      <UpgradeAvailableBanner
        entityVersionId="100"
        pinnedArchetypeVersionId="42"
        latestArchetypeVersionId="44"
        entityId="9"
      />,
    );
    expect(html).toContain('data-testid="upgrade-available-banner"');
    expect(html).toContain('data-testid="upgrade-available-banner-cta"');
    expect(html).toContain("v42");
    expect(html).toContain("v44");
    expect(html).toContain("/catalog/entities/9/upgrade");
    expect(html).toContain("source_version_id=100");
    expect(html).toContain("target_archetype_version_id=44");
  });

  it("returns null when pinned matches latest", () => {
    const html = renderToStaticMarkup(
      <UpgradeAvailableBanner
        entityVersionId="100"
        pinnedArchetypeVersionId="44"
        latestArchetypeVersionId="44"
        entityId="9"
      />,
    );
    expect(html).toBe("");
  });

  it("returns null when pinned is null", () => {
    const html = renderToStaticMarkup(
      <UpgradeAvailableBanner
        entityVersionId="100"
        pinnedArchetypeVersionId={null}
        latestArchetypeVersionId="44"
        entityId="9"
      />,
    );
    expect(html).toBe("");
  });

  it("returns null when latest is null", () => {
    const html = renderToStaticMarkup(
      <UpgradeAvailableBanner
        entityVersionId="100"
        pinnedArchetypeVersionId="44"
        latestArchetypeVersionId={null}
        entityId="9"
      />,
    );
    expect(html).toBe("");
  });
});
