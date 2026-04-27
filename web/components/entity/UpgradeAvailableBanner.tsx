"use client";

import Link from "next/link";

/**
 * Surfaces an upgrade CTA when an entity_version is pinned to an older
 * archetype version than the latest published one (TECH-2462 / Stage 11.1).
 *
 * Render contract: returns null when the pin is current. Renders a banner
 * with "Preview upgrade" link when `pinned_archetype_version_id` differs
 * from `latest_archetype_version_id`.
 *
 * @see ia/projects/asset-pipeline/stage-11.1 — TECH-2462 §Plan Digest
 */
export type UpgradeAvailableBannerProps = {
  /** entity_version row id of the consuming entity. */
  entityVersionId: string;
  /** Archetype version id this entity is pinned to. */
  pinnedArchetypeVersionId: string | null;
  /** Latest published archetype version id (null when no published version). */
  latestArchetypeVersionId: string | null;
  /** entity_version.entity_id (catalog_entity.id) — used to build upgrade link. */
  entityId: string;
};

export default function UpgradeAvailableBanner({
  entityVersionId,
  pinnedArchetypeVersionId,
  latestArchetypeVersionId,
  entityId,
}: UpgradeAvailableBannerProps) {
  if (pinnedArchetypeVersionId == null || latestArchetypeVersionId == null) return null;
  if (pinnedArchetypeVersionId === latestArchetypeVersionId) return null;

  return (
    <aside
      data-testid="upgrade-available-banner"
      role="status"
      className="flex items-center justify-between rounded border border-[var(--ds-text-accent-info)] bg-[var(--ds-bg-elevated)] p-[var(--ds-spacing-sm)]"
    >
      <span className="text-[var(--ds-text-default)]">
        Newer archetype version available. This entity is pinned to v
        {pinnedArchetypeVersionId}; latest published is v{latestArchetypeVersionId}.
      </span>
      <Link
        data-testid="upgrade-available-banner-cta"
        href={`/catalog/entities/${encodeURIComponent(entityId)}/upgrade?source_version_id=${encodeURIComponent(entityVersionId)}&target_archetype_version_id=${encodeURIComponent(latestArchetypeVersionId)}`}
        className="rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] text-[var(--ds-text-accent-info)]"
      >
        Preview upgrade
      </Link>
    </aside>
  );
}
