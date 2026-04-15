/**
 * reading-time.ts — word-count to estimated minutes helper.
 * Baseline: 200 wpm, rounded up, floor 1 minute.
 */

const WPM = 200;

/**
 * Compute estimated reading time in minutes.
 * Strips MDX/HTML-ish tokens (angle-bracket tags, frontmatter fences, JSX
 * expressions) before counting whitespace-delimited words.
 */
export function computeReadingTime(body: string): number {
  const stripped = body
    .replace(/<[^>]*>/g, ' ')        // remove HTML/JSX tags
    .replace(/\{[^}]*\}/g, ' ')      // remove JSX expressions
    .replace(/^---[\s\S]*?---/m, '') // strip frontmatter block if present
    .replace(/[#*`_~[\]()>|]/g, ' ') // strip Markdown punctuation
    .trim();

  const wordCount = stripped.split(/\s+/).filter(Boolean).length;
  return Math.max(1, Math.ceil(wordCount / WPM));
}
