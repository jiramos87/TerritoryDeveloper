#!/usr/bin/env node
/**
 * design-explore-render.test.mjs
 *
 * Round-trip test for `render-design-explore-html.mjs` + `extract-exploration-md.mjs`.
 *
 * Invariants asserted:
 *   1. Render of fixture MD produces a non-empty HTML file containing the slot tokens replaced.
 *   2. Extract of that HTML returns byte-identical MD (modulo allowed trailing newline).
 *   3. `</script>` inside the MD body is escaped during render + reversed by extract.
 *
 * Wired into `validate:all:readonly` via `validate:design-explore-render` npm script.
 *
 * design-explore-html-effectiveness-uplift D3.
 */

import { readFileSync, writeFileSync, mkdtempSync } from "node:fs";
import { tmpdir } from "node:os";
import { resolve, join, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { execSync } from "node:child_process";
import { strictEqual, ok } from "node:assert/strict";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const REPO_ROOT = resolve(__dirname, "..", "..", "..");
const FIXTURE_DIR = resolve(
  REPO_ROOT,
  "tools/scripts/__fixtures__/design-explore-render",
);
const RENDERER = resolve(
  REPO_ROOT,
  "tools/scripts/render-design-explore-html.mjs",
);
const EXTRACTOR = resolve(
  REPO_ROOT,
  "tools/scripts/extract-exploration-md.mjs",
);

const failures = [];
function check(label, fn) {
  try {
    fn();
    console.log(`  ok  ${label}`);
  } catch (err) {
    failures.push({ label, err });
    console.error(`  FAIL ${label}: ${err.message}`);
  }
}

const fixtureMd = readFileSync(join(FIXTURE_DIR, "sample.md"), "utf8");
ok(fixtureMd.length > 0, "fixture MD non-empty");

// Run render in a temp workspace
const tmp = mkdtempSync(join(tmpdir(), "design-explore-render-"));
const tmpMd = join(tmp, "sample.md");
const tmpHtml = join(tmp, "sample.html");
writeFileSync(tmpMd, fixtureMd);

execSync(`node ${RENDERER} --path ${tmpMd}`, { stdio: "inherit" });
const html = readFileSync(tmpHtml, "utf8");

check("HTML non-empty", () => ok(html.length > 1000));
check("HTML contains TITLE slot replaced", () => {
  ok(/Design[ -]Explore[ -]Render[ -]Fixture/i.test(html) || html.includes("design-explore-render-fixture"));
});
check("HTML carries STAGES_JSON populated", () => {
  ok(/\"id\":\s*\"1.0\"/.test(html));
  ok(/\"id\":\s*\"2.0\"/.test(html));
});
check("HTML escapes </script> inside RAW_MD", () => {
  // The literal token must NOT appear inside the rawMarkdown script block,
  // but the escaped form `<\/script>` MUST appear (covered by fixture body).
  const rawStart = html.indexOf('<script id="rawMarkdown"');
  ok(rawStart !== -1);
  const rawEnd = html.indexOf("</script>", rawStart);
  const inner = html.slice(rawStart, rawEnd);
  ok(inner.includes("<\\/script>"), "escaped form present in inner block");
  ok(!inner.includes("</script"), "no unescaped </script> in inner block (besides closing tag)");
});

const extracted = execSync(`node ${EXTRACTOR} --path ${tmpHtml}`, { encoding: "utf8" });

check("Round-trip extract returns byte-identical MD", () => {
  if (extracted !== fixtureMd) {
    const a = fixtureMd, b = extracted;
    let i = 0;
    while (i < a.length && i < b.length && a[i] === b[i]) i++;
    throw new Error(`drift at byte ${i}: expected '${a.slice(i, i + 40)}' got '${b.slice(i, i + 40)}'`);
  }
  strictEqual(extracted, fixtureMd);
});

if (failures.length > 0) {
  console.error(`\ndesign-explore-render.test: ${failures.length} failure(s)`);
  process.exit(1);
}

console.log("\ndesign-explore-render.test: all checks pass");
process.exit(0);
