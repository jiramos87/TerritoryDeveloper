import ButtonDetailClient from "./ButtonDetailClient";

type Ctx = { params: Promise<{ slug: string }> };

/** Spine button detail server shell — resolves dynamic slug then hands off (TECH-1885). */
export default async function ButtonDetailPage({ params }: Ctx) {
  const { slug } = await params;
  return <ButtonDetailClient slug={slug} />;
}
