/** Audit log sentinel — Stage 8.3 owns the surface (TECH-1614). */
export default function AuditLogCatalogPage() {
  return (
    <div data-testid="catalog-sentinel-audit-log">
      <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">Audit log</h1>
      <p className="text-[var(--ds-text-muted)]">Audit log — Stage 8.3 authors this surface.</p>
    </div>
  );
}
