/** Pools catalog sentinel — Stage 7.1 owns the surface (TECH-1614). */
export default function PoolsCatalogPage() {
  return (
    <div data-testid="catalog-sentinel-pools">
      <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">Pools</h1>
      <p className="text-[var(--ds-text-muted)]">Pools catalog — Stage 7.1 authors this surface.</p>
    </div>
  );
}
