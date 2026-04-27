import PoolDetailClient from "./PoolDetailClient";

type Ctx = { params: Promise<{ slug: string }> };

/** Spine pool detail server shell — resolves dynamic slug then hands off. */
export default async function PoolDetailPage({ params }: Ctx) {
  const { slug } = await params;
  return <PoolDetailClient slug={slug} />;
}
