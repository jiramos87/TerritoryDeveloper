import UpgradePreviewClient from "./UpgradePreviewClient";

type Ctx = {
  params: Promise<{ id: string }>;
  searchParams: Promise<{
    source_version_id?: string;
    target_archetype_version_id?: string;
  }>;
};

/**
 * Entity-version upgrade preview server shell (TECH-2462).
 * Resolves dynamic id + query params, hands off to client for fetch + diff render.
 */
export default async function UpgradePreviewPage({ params, searchParams }: Ctx) {
  const { id } = await params;
  const sp = await searchParams;
  return (
    <UpgradePreviewClient
      entityId={id}
      sourceVersionId={sp.source_version_id ?? null}
      targetArchetypeVersionId={sp.target_archetype_version_id ?? null}
    />
  );
}
