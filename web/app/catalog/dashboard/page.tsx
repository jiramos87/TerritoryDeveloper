/**
 * Catalog dashboard landing tile (TECH-1614).
 *
 * Three placeholder slots — unresolved-refs widget, lint summary, render
 * queue — fleshed out by Stage 15.1.
 */

const PLACEHOLDERS = [
  {
    title: "Unresolved refs",
    body: "Cross-entity reference health rolls up here. Wired in Stage 15.1.",
  },
  {
    title: "Lint summary",
    body: "Aggregate lint counts across all kinds land here. Wired in Stage 15.1.",
  },
  {
    title: "Render queue",
    body: "Active and queued render runs surface here. Wired in Stage 15.1.",
  },
];

export default function CatalogDashboardPage() {
  return (
    <div data-testid="catalog-dashboard" className="flex flex-col gap-[var(--ds-spacing-lg)]">
      <header>
        <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">Authoring console</h1>
        <p className="text-[var(--ds-text-muted)]">
          Health overview across content, configuration, and operations.
        </p>
      </header>
      <div className="grid grid-cols-1 gap-[var(--ds-spacing-md)] md:grid-cols-3">
        {PLACEHOLDERS.map((tile) => (
          <section
            key={tile.title}
            data-testid={`dashboard-tile-${tile.title.toLowerCase().replace(/\s+/g, "-")}`}
            className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] p-[var(--ds-spacing-md)]"
          >
            <h2 className="text-[length:var(--ds-font-size-h4)] font-semibold">{tile.title}</h2>
            <p className="mt-[var(--ds-spacing-sm)] text-[var(--ds-text-muted)]">{tile.body}</p>
          </section>
        ))}
      </div>
    </div>
  );
}
