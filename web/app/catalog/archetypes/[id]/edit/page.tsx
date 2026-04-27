import ArchetypeEditClient from "./ArchetypeEditClient";

type Ctx = { params: Promise<{ id: string }> };

/** Archetype draft-edit server shell — resolves dynamic id then hands off (TECH-2460). */
export default async function ArchetypeEditPage({ params }: Ctx) {
  const { id } = await params;
  return <ArchetypeEditClient entityId={id} />;
}
