/** Sprites catalog sentinel — Stage 6.1 owns the surface (TECH-1614). */
export default function SpritesCatalogPage() {
  return (
    <div data-testid="catalog-sentinel-sprites">
      <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">Sprites</h1>
      <p className="text-[var(--ds-text-muted)]">Sprites catalog — Stage 6.1 authors this surface.</p>
    </div>
  );
}
