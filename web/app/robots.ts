import type { MetadataRoute } from 'next';
import { getBaseUrl } from '@/lib/site/base-url';

export default function robots(): MetadataRoute.Robots {
  const base = getBaseUrl();
  return {
    rules: {
      userAgent: '*',
      allow: '/',
      disallow: ['/design', '/auth'],
    },
    sitemap: `${base}/sitemap.xml`,
  };
}
