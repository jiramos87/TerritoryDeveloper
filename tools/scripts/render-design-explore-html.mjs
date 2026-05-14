#!/usr/bin/env node
/**
 * render-design-explore-html.mjs
 *
 * Renders `docs/explorations/{slug}.md` into `docs/explorations/{slug}.html`
 * by reading the static template `ia/templates/design-explore.html.template`
 * and filling handlebars-style slots.
 *
 * Slots:
 *   {{TITLE}}             — derived from frontmatter `slug` or `title`
 *   {{META_CHIPS_JSON}}   — JSON array of {label, value} chips for the doc head
 *   {{FRONTMATTER_RAW}}   — raw frontmatter text (visible inside the metadata accordion)
 *   {{RAW_MD}}            — full original MD verbatim, with `</script>` sentinel escaped
 *   {{STAGES_JSON}}       — JSON-stringified `stages[]` from frontmatter (drives stage cards, gantt, dep graph, task table)
 *   {{DECISIONS_JSON}}    — JSON-stringified `decisions[]` if present, else "[]"
 *   {{REFERENCES_JSON}}   — JSON-stringified `references[]` if present, else "[]"
 *   {{PANELS_JSON}}       — JSON-stringified `panels[]` if present, else "[]"
 *   {{CUSTOM_BLOCKS}}     — inline HTML chunks from frontmatter `custom_blocks_html:` key (bespoke per-exploration widgets)
 *   {{VISUAL_GOALS_JSON}} — JSON-stringified `visual_goals[]` from frontmatter (drives the visual-goals card)
 *   {{PATTERNS_JSON}}     — JSON-stringified `patterns_observed[]` from frontmatter (drives the patterns-observed callout)
 *   {{HANDOFF_TEMPLATE_JSON}} — JSON-stringified handoff prompt template body (per-stage paste-ready block);
 *                              defaults to the canonical template when frontmatter omits `handoff_template:`
 *
 * Usage:
 *   node tools/scripts/render-design-explore-html.mjs ui-toolkit-migration
 *   node tools/scripts/render-design-explore-html.mjs --path docs/explorations/foo.md
 *
 * Output:
 *   Writes docs/explorations/{slug}.html. Returns absolute path on success.
 *
 * design-explore-html-effectiveness-uplift D3.
 */

import { readFileSync, writeFileSync, existsSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

import yaml from "js-yaml";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const REPO_ROOT = resolve(__dirname, "..", "..");
const TEMPLATE_PATH = resolve(
  REPO_ROOT,
  "ia/templates/design-explore.html.template",
);

function resolveMdPath(arg) {
  if (arg.startsWith("--path=")) return arg.slice("--path=".length);
  if (arg.endsWith(".md")) return arg;
  return `docs/explorations/${arg}.md`;
}

function splitFrontmatter(src) {
  if (!src.startsWith("---\n")) {
    return { fm: {}, fmText: "", body: src };
  }
  const closeIdx = src.indexOf("\n---", 4);
  if (closeIdx === -1) {
    return { fm: {}, fmText: "", body: src };
  }
  const fmText = src.slice(4, closeIdx);
  const bodyStart = closeIdx + 4; // skip "\n---"
  const body = src.slice(bodyStart).replace(/^\n/, "");
  let fm = {};
  try {
    fm = yaml.load(fmText) || {};
  } catch (err) {
    console.error("render-design-explore-html: YAML parse failed:", err.message);
    process.exit(1);
  }
  return { fm, fmText, body };
}

function buildMetaChips(fm) {
  const chips = [];
  if (fm.slug) chips.push({ label: "slug", value: String(fm.slug) });
  if (fm.target_version) chips.push({ label: "target_version", value: String(fm.target_version) });
  if (fm.audience) chips.push({ label: "audience", value: String(fm.audience) });
  if (fm.created_at) chips.push({ label: "created", value: String(fm.created_at) });
  if (Array.isArray(fm.stages)) chips.push({ label: "stages", value: String(fm.stages.length) });
  if (Array.isArray(fm.visual_goals) && fm.visual_goals.length > 0) chips.push({ label: "visual goals", value: String(fm.visual_goals.length) });
  if (Array.isArray(fm.patterns_observed) && fm.patterns_observed.length > 0) chips.push({ label: "patterns", value: String(fm.patterns_observed.length) });
  return chips;
}

function deriveTitle(fm) {
  if (typeof fm.title === "string" && fm.title.length > 0) return fm.title;
  if (typeof fm.slug === "string" && fm.slug.length > 0) {
    return fm.slug
      .split("-")
      .map((w) => w.charAt(0).toUpperCase() + w.slice(1))
      .join(" ");
  }
  return "Exploration";
}

function escapeRawMdForScript(md) {
  // Embed the full MD inside <script id="rawMarkdown" type="text/plain">.
  // Only the literal `</script>` sequence must be escaped so the browser does
  // not prematurely close the script tag. The extractor reverses this exact
  // substitution.
  return md.replace(/<\/script>/g, "<\\/script>");
}

function fillSlot(tpl, slot, value) {
  // Simple literal token replacement; slots appear once per template
  // by design. Use a global regex anyway so duplicate slot tokens stay safe.
  const re = new RegExp(`\\{\\{${slot}\\}\\}`, "g");
  return tpl.replace(re, value);
}

function render({ mdPath, outPath }) {
  if (!existsSync(TEMPLATE_PATH)) {
    throw new Error(`template not found: ${TEMPLATE_PATH}`);
  }
  if (!existsSync(mdPath)) {
    throw new Error(`source MD not found: ${mdPath}`);
  }
  const tpl = readFileSync(TEMPLATE_PATH, "utf8");
  const md = readFileSync(mdPath, "utf8");
  const { fm } = splitFrontmatter(md);
  const title = deriveTitle(fm);
  const metaChips = buildMetaChips(fm);
  // Frontmatter raw text — extract verbatim between the fences (no parsing
  // re-emit) so the accordion shows exactly what the author wrote.
  const fmTextMatch = md.match(/^---\n([\s\S]*?)\n---\n/);
  const fmText = fmTextMatch ? fmTextMatch[1] : "";
  let html = tpl;
  html = fillSlot(html, "TITLE", escapeHtml(title));
  html = fillSlot(html, "META_CHIPS_JSON", JSON.stringify(metaChips));
  html = fillSlot(html, "FRONTMATTER_RAW", escapeHtml(fmText));
  html = fillSlot(html, "RAW_MD", escapeRawMdForScript(md));
  html = fillSlot(html, "STAGES_JSON", JSON.stringify(fm.stages || []));
  html = fillSlot(html, "DECISIONS_JSON", JSON.stringify(fm.decisions || []));
  html = fillSlot(html, "REFERENCES_JSON", JSON.stringify(fm.references || []));
  html = fillSlot(html, "PANELS_JSON", JSON.stringify(fm.panels || []));
  html = fillSlot(html, "CUSTOM_BLOCKS", fm.custom_blocks_html || "");
  html = fillSlot(html, "VISUAL_GOALS_JSON", JSON.stringify(fm.visual_goals || []));
  html = fillSlot(html, "PATTERNS_JSON", JSON.stringify(fm.patterns_observed || []));
  html = fillSlot(html, "HANDOFF_TEMPLATE_JSON", JSON.stringify(resolveHandoffTemplate(fm)));
  writeFileSync(outPath, html);
  return resolve(outPath);
}

// Canonical handoff template — composed at render time, per-stage scope_summary slot injected by JS.
// Author can override via frontmatter `handoff_template:` (string with {{STAGE_ID}} / {{STAGE_TITLE}} /
// {{SCOPE_SUMMARY}} / {{SLUG}} placeholders).
const DEFAULT_HANDOFF_TEMPLATE = `# Handoff — Stage {{STAGE_ID}} of {{SLUG}}

You are picking up Stage {{STAGE_ID}} — {{STAGE_TITLE}} — of the {{SLUG}}
master plan. Read the stage card in this HTML doc end-to-end (scope, tasks,
red-stage proof, edge cases, failure modes, checkpoint screenshots,
iteration log) before touching any file.

## Read first (in order)
  1. The §Latest state header at the top of this HTML doc.
  2. The §Patterns observed callout — cross-stage lessons; do not repeat them.
  3. This stage's card (Stage {{STAGE_ID}}) — full body, all tasks.
  4. \`MEMORY.md\` at repo root + the user's personal MEMORY index — Javier's
     preferences + project-specific rules + accumulated feedback.
  5. The DB-backed master plan state: run \`master_plan_state {{SLUG}}\` MCP
     to confirm stage status and unblock-on-deps before starting.

## Stage scope (this run)
{{SCOPE_SUMMARY}}

## Hard rules
  - **Main worktree only.** Single-developer single-stream project.
  - No \`git commit --amend\`, no \`git push\`, no \`--no-verify\`.
  - Close Unity Editor before \`npm run unity:compile-check\`.
  - Simple product language in chat replies (caveman-tech in docs + code only).
  - One commit per task on the active feature branch. Use Conventional Commits.

## Workflow per task
  Read task card → implement minimal diff → \`npm run unity:compile-check\`
  → commit → append a row to this stage's iteration log (frontmatter
  \`stages[].iteration_log[]\`) → re-render the HTML
  (\`npm run design-explore:render-html {{SLUG}}\`) → await user verdict.

## Completion signal
When the user accepts this stage's visual + functional checkpoint, attach
checkpoint screenshots to \`stages[].checkpoint_screenshots[]\`, set the
stage \`status: done\`, re-render the HTML, and surface the next-stage
handoff link from §Latest state.
`;

function resolveHandoffTemplate(fm) {
  if (typeof fm.handoff_template === "string" && fm.handoff_template.length > 0) {
    return fm.handoff_template;
  }
  return DEFAULT_HANDOFF_TEMPLATE;
}

function escapeHtml(s) {
  return String(s)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

function main() {
  const args = process.argv.slice(2);
  if (args.length === 0) {
    console.error(
      "Usage: render-design-explore-html.mjs <slug>  OR  --path docs/explorations/{slug}.md",
    );
    process.exit(2);
  }
  let mdPath;
  if (args[0] === "--path") {
    mdPath = args[1];
  } else {
    mdPath = resolveMdPath(args[0]);
  }
  if (!mdPath) {
    console.error("render-design-explore-html: missing path arg");
    process.exit(2);
  }
  const absMd = resolve(mdPath);
  const outPath = absMd.replace(/\.md$/, ".html");
  const written = render({ mdPath: absMd, outPath });
  console.log(`render-design-explore-html: wrote ${written}`);
}

main();

export { render, splitFrontmatter, escapeRawMdForScript, fillSlot };
