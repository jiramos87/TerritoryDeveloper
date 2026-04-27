import PanelDetailClient from "./PanelDetailClient";

type Ctx = { params: Promise<{ slug: string }> };

/** Spine panel detail server shell — resolves dynamic slug then hands off (TECH-1886). */
export default async function PanelDetailPage({ params }: Ctx) {
  const { slug } = await params;
  return <PanelDetailClient slug={slug} />;
}
