#!/usr/bin/env node
/**
 * extract-exploration-md.mjs
 *
 * Inverse of `render-design-explore-html.mjs`. Reads
 * `docs/explorations/{slug}.html`, extracts the canonical MD body embedded
 * inside `<script id="rawMarkdown" type="text/plain">...</script>`, unescapes
 * the `<\/script>` sentinel, and prints the raw MD to stdout.
 *
 * Round-trip invariant: render(extract(html)) === html (byte-clean).
 *
 * Usage:
 *   node tools/scripts/extract-exploration-md.mjs ui-toolkit-migration > /tmp/out.md
 *   node tools/scripts/extract-exploration-md.mjs --path docs/explorations/foo.html
 *
 * design-explore-html-effectiveness-uplift D3.
 */

import { readFileSync, existsSync } from "node:fs";
import { resolve } from "node:path";

const SCRIPT_ID = "rawMarkdown";
const OPEN_RE = new RegExp(
  `<script\\s+id=["']${SCRIPT_ID}["']\\s+type=["']text/plain["']\\s*>`,
);
const CLOSE_TOKEN = "</script>";

function resolveHtmlPath(arg) {
  if (arg.startsWith("--path=")) return arg.slice("--path=".length);
  if (arg === "--path") return null;
  if (arg.endsWith(".html")) return arg;
  return `docs/explorations/${arg}.html`;
}

function unescapeRawMd(escaped) {
  // Renderer escapes the lone end-tag sentinel inside the raw MD payload so
  // the embedded script block stays well-formed. We reverse the same
  // substitution here. No other characters require unescape — the script
  // block is `type="text/plain"` so HTML entities pass through verbatim.
  return escaped.replace(/<\\\/script>/g, "</script>");
}

function extract(html) {
  const openMatch = html.match(OPEN_RE);
  if (!openMatch) {
    throw new Error(
      `extract-exploration-md: cannot find <script id="${SCRIPT_ID}" type="text/plain"> open tag`,
    );
  }
  const openEnd = openMatch.index + openMatch[0].length;
  const closeStart = html.indexOf(CLOSE_TOKEN, openEnd);
  if (closeStart === -1) {
    throw new Error(
      `extract-exploration-md: missing closing </script> after open tag at offset ${openEnd}`,
    );
  }
  const escaped = html.slice(openEnd, closeStart);
  return unescapeRawMd(escaped);
}

function main() {
  const args = process.argv.slice(2);
  if (args.length === 0) {
    console.error(
      "Usage: extract-exploration-md.mjs <slug>  OR  --path docs/explorations/{slug}.html",
    );
    process.exit(2);
  }
  let htmlPath;
  if (args[0] === "--path") {
    htmlPath = args[1];
  } else {
    htmlPath = resolveHtmlPath(args[0]);
  }
  if (!htmlPath) {
    console.error("extract-exploration-md: missing path arg");
    process.exit(2);
  }
  const abs = resolve(htmlPath);
  if (!existsSync(abs)) {
    console.error(`extract-exploration-md: not found: ${abs}`);
    process.exit(1);
  }
  const html = readFileSync(abs, "utf8");
  const md = extract(html);
  process.stdout.write(md);
}

main();
