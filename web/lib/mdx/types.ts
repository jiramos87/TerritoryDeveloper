/** Frontmatter shape for devlog posts under web/content/devlog/. */
export interface DevlogFrontmatter {
  /** Post title — required. */
  title: string;
  /** Publication date YYYY-MM-DD — required. */
  date: string;
  /** Tag list — required (may be empty array). */
  tags: string[];
  /** Short excerpt shown on list cards — required. */
  excerpt: string;
  /** Optional cover image path relative to /public. */
  cover?: string;
}

/** Frontmatter shape for all top-level pages under web/content/pages/. */
export interface PageFrontmatter {
  /** Page title — required. */
  title: string;
  /** Short page description — required. */
  description: string;
  /** Last-updated ISO-8601 date, YYYY-MM-DD — required. */
  updated: string;
  /** Optional hero image path relative to /public. */
  hero?: string;
}

/**
 * Return shape from loadMdxContent / loadMdxPage.
 * source = raw MDX body with frontmatter stripped.
 * Consumer passes source to <MDXRemote> or equivalent.
 */
export interface MdxLoadResult<T = PageFrontmatter> {
  source: string;
  frontmatter: T;
}
