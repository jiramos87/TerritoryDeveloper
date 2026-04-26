"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

/**
 * Authoring-console left nav (DEC-A16, DEC-A34).
 *
 * Three groups in fixed order — Content / Configuration / Operations — each
 * lists the kind links the master plan promises to flesh out in later
 * stages. Active-state styling tracks `usePathname()` so deep linking lands
 * in the right group.
 *
 * @see ia/projects/asset-pipeline/stage-5.1.md — Authoring console scaffolding
 */

type CatalogLink = {
  href: string;
  label: string;
};

type CatalogGroup = {
  heading: string;
  links: ReadonlyArray<CatalogLink>;
};

const GROUPS: ReadonlyArray<CatalogGroup> = [
  {
    heading: "Content",
    links: [
      { href: "/catalog/sprites", label: "Sprites" },
      { href: "/catalog/assets", label: "Assets" },
      { href: "/catalog/buttons", label: "Buttons" },
      { href: "/catalog/panels", label: "Panels" },
      { href: "/catalog/audio", label: "Audio" },
    ],
  },
  {
    heading: "Configuration",
    links: [
      { href: "/catalog/pools", label: "Pools" },
      { href: "/catalog/tokens", label: "Tokens" },
      { href: "/catalog/archetypes", label: "Archetypes" },
    ],
  },
  {
    heading: "Operations",
    links: [
      { href: "/catalog/snapshots", label: "Snapshots" },
      { href: "/catalog/render-runs", label: "Render runs" },
      { href: "/catalog/audit-log", label: "Audit log" },
      { href: "/catalog/settings", label: "Settings" },
    ],
  },
];

export default function CatalogSidebar() {
  const pathname = usePathname();

  return (
    <nav
      aria-label="Catalog navigation"
      data-testid="catalog-sidebar"
      className="flex h-full w-60 flex-col gap-[var(--ds-spacing-md)] border-r border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] p-[var(--ds-spacing-md)] text-[var(--ds-text-primary)]"
    >
      <header className="text-[length:var(--ds-font-size-h4)] font-semibold">
        <Link href="/catalog/dashboard" className="text-[var(--ds-text-primary)] hover:text-[var(--ds-text-accent-info)]">
          Catalog
        </Link>
      </header>
      {GROUPS.map((group) => (
        <section
          key={group.heading}
          data-testid={`catalog-sidebar-group-${group.heading.toLowerCase()}`}
          className="flex flex-col gap-[var(--ds-spacing-xs)]"
        >
          <h3 className="text-[length:var(--ds-font-size-body-sm)] uppercase tracking-wide text-[var(--ds-text-muted)]">
            {group.heading}
          </h3>
          <ul className="flex flex-col gap-[var(--ds-spacing-xs)]">
            {group.links.map((link) => {
              const active = pathname === link.href;
              return (
                <li key={link.href}>
                  <Link
                    href={link.href}
                    data-testid={`catalog-sidebar-link-${link.href}`}
                    aria-current={active ? "page" : undefined}
                    className={
                      active
                        ? "block rounded px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)] bg-[var(--ds-bg-canvas)] text-[var(--ds-text-accent-warn)]"
                        : "block rounded px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)] text-[var(--ds-text-muted)] hover:text-[var(--ds-text-primary)]"
                    }
                  >
                    {link.label}
                  </Link>
                </li>
              );
            })}
          </ul>
        </section>
      ))}
    </nav>
  );
}
