/** Audio catalog sentinel — Stage 6.5 owns the surface (TECH-1614). */
export default function AudioCatalogPage() {
  return (
    <div data-testid="catalog-sentinel-audio">
      <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">Audio</h1>
      <p className="text-[var(--ds-text-muted)]">Audio catalog — Stage 6.5 authors this surface.</p>
    </div>
  );
}
