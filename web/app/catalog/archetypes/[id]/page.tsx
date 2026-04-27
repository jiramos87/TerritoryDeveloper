import ArchetypeDetailClient from "./ArchetypeDetailClient";

type Ctx = { params: Promise<{ id: string }> };

/** Archetype detail server shell — resolves dynamic id then hands off (TECH-2459). */
export default async function ArchetypeDetailPage({ params }: Ctx) {
  const { id } = await params;
  return <ArchetypeDetailClient entityId={id} />;
}
