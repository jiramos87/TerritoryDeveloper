// Stage 27 T27.2 — CD ScreenLanding port (see HomeLandingClient).
import type { Metadata } from 'next';
import { HomeLandingClient } from '@/components/landing/HomeLanding.client';
import { buildPageMetadata } from '@/lib/site/metadata';

export async function generateMetadata(): Promise<Metadata> {
  return buildPageMetadata('landing');
}

export default function Home() {
  return <HomeLandingClient />;
}
