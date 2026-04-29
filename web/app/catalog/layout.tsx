import type { ReactNode } from "react";

import CatalogSidebar from "@/components/catalog/CatalogSidebar";
import { SearchBar } from "@/components/catalog/SearchBar";

/**
 * Authoring-console root layout (DEC-A16, DEC-A34, TECH-1614).
 *
 * Persistent left sidebar + flex main pane. Per-kind pages mount into
 * `children`; sidebar stays static while routes change.
 */
export default function CatalogLayout({ children }: { children: ReactNode }) {
  return (
    <div className="flex h-screen w-full bg-[var(--ds-bg-canvas)] text-[var(--ds-text-primary)]">
      <CatalogSidebar />
      <main className="flex-1 overflow-y-auto p-[var(--ds-spacing-lg)]" data-testid="catalog-main">
        {children}
      </main>
      <SearchBar />
    </div>
  );
}
