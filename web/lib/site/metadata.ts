import type { Metadata } from 'next';
import { getBaseUrl } from './base-url';
import { loadMdxPage } from '@/lib/mdx/loader';

/** Canonical site title. */
export const siteTitle = 'Territory Developer';

/** Canonical site tagline. */
export const siteTagline = 'A city builder where geography shapes every decision.';

/**
 * Builds a Metadata object for a page from its MDX frontmatter.
 * Call from each public RSC's generateMetadata export.
 */
export async function buildPageMetadata(slug: string): Promise<Metadata> {
  const { frontmatter } = await loadMdxPage(slug);
  const base = getBaseUrl();
  const url = slug === 'landing' ? base : `${base}/${slug}`;

  return {
    title: frontmatter.title,
    description: frontmatter.description,
    openGraph: {
      title: frontmatter.title,
      description: frontmatter.description,
      url,
      type: 'article',
    },
    twitter: {
      card: 'summary_large_image',
    },
  };
}
