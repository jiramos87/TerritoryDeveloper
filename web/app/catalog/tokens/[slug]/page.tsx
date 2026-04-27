import TokenDetailClient from "./TokenDetailClient";

type Ctx = { params: Promise<{ slug: string }> };

/** Spine token detail server shell — resolves dynamic slug then hands off (TECH-2093). */
export default async function TokenDetailPage({ params }: Ctx) {
  const { slug } = await params;
  return <TokenDetailClient slug={slug} />;
}
