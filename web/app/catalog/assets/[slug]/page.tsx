import AssetDetailClient from "./AssetDetailClient";

type Ctx = { params: Promise<{ slug: string }> };

/** Spine-aware asset detail server shell — resolves dynamic slug then hands off. */
export default async function AssetDetailPage({ params }: Ctx) {
  const { slug } = await params;
  return <AssetDetailClient slug={slug} />;
}
