import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

vi.mock("next/navigation", () => ({
  usePathname: vi.fn(),
}));

vi.mock("next/link", () => ({
  default: ({
    href,
    children,
    ...rest
  }: {
    href: string;
    children: React.ReactNode;
    [key: string]: unknown;
  }) => (
    <a href={href} {...rest}>
      {children}
    </a>
  ),
}));

import CatalogSidebar from "@/components/catalog/CatalogSidebar";
import { usePathname } from "next/navigation";

const mockedUsePathname = vi.mocked(usePathname);

const ALL_LINKS: ReadonlyArray<{ href: string; label: string }> = [
  { href: "/catalog/sprites", label: "Sprites" },
  { href: "/catalog/assets", label: "Assets" },
  { href: "/catalog/buttons", label: "Buttons" },
  { href: "/catalog/panels", label: "Panels" },
  { href: "/catalog/audio", label: "Audio" },
  { href: "/catalog/pools", label: "Pools" },
  { href: "/catalog/tokens", label: "Tokens" },
  { href: "/catalog/archetypes", label: "Archetypes" },
  { href: "/catalog/snapshots", label: "Snapshots" },
  { href: "/catalog/render-runs", label: "Render runs" },
  { href: "/catalog/audit-log", label: "Audit log" },
  { href: "/catalog/settings", label: "Settings" },
];

describe("<CatalogSidebar />", () => {
  beforeEach(() => {
    mockedUsePathname.mockReset();
  });

  it("renders three group headings (Content / Configuration / Operations)", () => {
    mockedUsePathname.mockReturnValue("/catalog/dashboard");
    const html = renderToStaticMarkup(<CatalogSidebar />);
    expect(html).toContain('data-testid="catalog-sidebar-group-content"');
    expect(html).toContain('data-testid="catalog-sidebar-group-configuration"');
    expect(html).toContain('data-testid="catalog-sidebar-group-operations"');
    expect(html).toContain(">Content<");
    expect(html).toContain(">Configuration<");
    expect(html).toContain(">Operations<");
  });

  it("renders all 12 kind links", () => {
    mockedUsePathname.mockReturnValue("/catalog/dashboard");
    const html = renderToStaticMarkup(<CatalogSidebar />);
    for (const link of ALL_LINKS) {
      expect(html).toContain(`data-testid="catalog-sidebar-link-${link.href}"`);
      expect(html).toContain(`href="${link.href}"`);
      expect(html).toContain(`>${link.label}<`);
    }
  });

  it("groups appear in fixed order: Content → Configuration → Operations", () => {
    mockedUsePathname.mockReturnValue("/catalog/dashboard");
    const html = renderToStaticMarkup(<CatalogSidebar />);
    const contentIdx = html.indexOf('data-testid="catalog-sidebar-group-content"');
    const configIdx = html.indexOf('data-testid="catalog-sidebar-group-configuration"');
    const opsIdx = html.indexOf('data-testid="catalog-sidebar-group-operations"');
    expect(contentIdx).toBeGreaterThanOrEqual(0);
    expect(configIdx).toBeGreaterThan(contentIdx);
    expect(opsIdx).toBeGreaterThan(configIdx);
  });

  it("applies active styling to the link matching the current pathname", () => {
    mockedUsePathname.mockReturnValue("/catalog/sprites");
    const html = renderToStaticMarkup(<CatalogSidebar />);
    const spritesAnchor = html.match(/data-testid="catalog-sidebar-link-\/catalog\/sprites"[^>]*/)?.[0] ?? "";
    expect(spritesAnchor).toContain('aria-current="page"');
    expect(spritesAnchor).toContain("text-[var(--ds-text-accent-warn)]");

    const assetsAnchor = html.match(/data-testid="catalog-sidebar-link-\/catalog\/assets"[^>]*/)?.[0] ?? "";
    expect(assetsAnchor).not.toContain('aria-current="page"');
  });

  it("active-state styling sources color from ds-* design tokens (no inline color literals)", () => {
    mockedUsePathname.mockReturnValue("/catalog/render-runs");
    const html = renderToStaticMarkup(<CatalogSidebar />);
    expect(html).toContain("var(--ds-text-accent-warn)");
    // Sanity: no hex / rgb literals inlined
    expect(html).not.toMatch(/#[0-9a-fA-F]{6}/);
    expect(html).not.toMatch(/rgb\(/);
  });
});
