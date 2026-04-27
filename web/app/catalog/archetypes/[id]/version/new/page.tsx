import VersionBumpClient from "./VersionBumpClient";

type Ctx = { params: Promise<{ id: string }> };

/** Archetype version-bump server shell (TECH-2461). Resolves dynamic id then hands off. */
export default async function VersionBumpPage({ params }: Ctx) {
  const { id } = await params;
  return <VersionBumpClient entityId={id} />;
}
