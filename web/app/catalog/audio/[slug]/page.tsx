import AudioDetailClient from "./AudioDetailClient";

type Ctx = { params: Promise<{ slug: string }> };

/**
 * Audio detail server-side shell — resolves the dynamic slug then hands
 * off to the client view (TECH-1958). Mirrors `sprites/[slug]/page.tsx`
 * verbatim per §Pending Decisions row 1.
 */
export default async function AudioDetailPage({ params }: Ctx) {
  const { slug } = await params;
  return <AudioDetailClient slug={slug} />;
}
