/** Settings sentinel — Stage 8.4 owns the surface (TECH-1614). */
export default function SettingsCatalogPage() {
  return (
    <div data-testid="catalog-sentinel-settings">
      <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">Settings</h1>
      <p className="text-[var(--ds-text-muted)]">Settings — Stage 8.4 authors this surface.</p>
    </div>
  );
}
