import SpriteDetailClient from "./SpriteDetailClient";

type Ctx = { params: Promise<{ slug: string }> };

/** Sprite detail server-side shell — resolves the dynamic slug then hands off to the client view. */
export default async function SpriteDetailPage({ params }: Ctx) {
  const { slug } = await params;
  return <SpriteDetailClient slug={slug} />;
}
