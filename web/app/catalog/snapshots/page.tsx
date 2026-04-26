/** Snapshots catalog sentinel — Stage 8.1 owns the surface (TECH-1614). */
export default function SnapshotsCatalogPage() {
  return (
    <div data-testid="catalog-sentinel-snapshots">
      <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">Snapshots</h1>
      <p className="text-[var(--ds-text-muted)]">Snapshots catalog — Stage 8.1 authors this surface.</p>
    </div>
  );
}
