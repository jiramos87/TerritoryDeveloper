#!/usr/bin/env npx tsx
/**
 * CD bundle → computed-styles.json (Stage 12 Step 14 P5.a — locked 2026-04-30).
 *
 * Renders `web/design-refs/step-1-game-ui/cd-bundle/Studio Rack Game UI.html`
 * headlessly at 1920×1080 (claude-design canvas native size, P5.b lock) and
 * dumps each panel/slot/interactive's `getComputedStyle(...)` snapshot into
 * structured JSON the agent can read offline (closes Step 13B/13C-style gap
 * where pixel-color expectations were inferred from token strings instead of
 * the actual rendered cascade).
 *
 * Captured per element:
 *   - data-cd-slug + tagName + nesting path
 *   - color, backgroundColor, borderColor, borderRadius, boxShadow
 *   - fontFamily, fontSize, fontWeight, lineHeight, letterSpacing, textAlign
 *   - opacity, transform (rotation/scale baked into the canvas)
 *
 * Output: `web/design-refs/step-1-game-ui/computed-styles.json`.
 *
 * Usage:
 *   npx tsx tools/scripts/extract-cd-computed-styles.ts
 *   npx tsx tools/scripts/extract-cd-computed-styles.ts --in <bundle-dir> --out <file>
 *
 * @packageDocumentation
 */
import * as fs from 'node:fs';
import * as path from 'node:path';
import * as process from 'node:process';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { chromium } from 'playwright';
import { serveBundle } from './cd-bundle-serve.ts';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '..', '..');

const DEFAULT_BUNDLE_DIR = path.join(REPO_ROOT, 'web/design-refs/step-1-game-ui/cd-bundle');
const DEFAULT_OUT = path.join(REPO_ROOT, 'web/design-refs/step-1-game-ui/computed-styles.json');
const DEFAULT_HTML_FILENAME = 'Studio Rack Game UI.html';
const VIEWPORT_W = 1920;
const VIEWPORT_H = 1080;
const RENDER_SETTLE_MS = 1500;

export interface ComputedStyleNode {
  /** node_kind: 'panel' (data-panel-slug), 'slot' (data-slot), or 'interactive' (class-rooted archetype). */
  node_kind: 'panel' | 'slot' | 'interactive';
  /** For panels: data-panel-slug; for slots: data-slot; for interactives: archetype class root (e.g. 'knob'). */
  cd_slug: string | null;
  /** Class list (interactives carry size/tone/state via knob--md, knob--tone-primary, knob--hover modifiers). */
  class_list: string[];
  tag_name: string;
  dom_path: string;
  computed: {
    color: string;
    backgroundColor: string;
    borderTopColor: string;
    borderRadius: string;
    boxShadow: string;
    fontFamily: string;
    fontSize: string;
    fontWeight: string;
    lineHeight: string;
    letterSpacing: string;
    textAlign: string;
    opacity: string;
    transform: string;
  };
}

export interface ComputedStylesArtifact {
  schema_version: 1;
  source_html: string;
  viewport: { width: number; height: number };
  generated_at_utc: string;
  node_count: number;
  nodes: ComputedStyleNode[];
}

interface CliArgs {
  bundleDir: string;
  outPath: string;
  htmlFilename: string;
}

function parseArgs(argv: string[]): CliArgs {
  let bundleDir = DEFAULT_BUNDLE_DIR;
  let outPath = DEFAULT_OUT;
  const htmlFilename = DEFAULT_HTML_FILENAME;

  for (let i = 2; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--in') {
      bundleDir = path.resolve(REPO_ROOT, argv[++i] ?? bundleDir);
    } else if (a === '--out') {
      outPath = path.resolve(REPO_ROOT, argv[++i] ?? outPath);
    } else if (a === '--help' || a === '-h') {
      console.log(`Usage: npx tsx tools/scripts/extract-cd-computed-styles.ts [options]

  --in  <dir>   CD bundle dir (default: web/design-refs/step-1-game-ui/cd-bundle)
  --out <file>  computed-styles.json output (default: web/design-refs/step-1-game-ui/computed-styles.json)

Renders ${DEFAULT_HTML_FILENAME} at ${VIEWPORT_W}x${VIEWPORT_H} via Playwright + chromium and dumps
getComputedStyle for every [data-cd-slug] node into structured JSON.
`);
      process.exit(0);
    }
  }
  return { bundleDir, outPath, htmlFilename };
}

export async function extractComputedStyles(args: CliArgs): Promise<ComputedStylesArtifact> {
  const htmlAbs = path.join(args.bundleDir, args.htmlFilename);
  if (!fs.existsSync(htmlAbs)) {
    throw new Error(`html_not_found: ${htmlAbs}`);
  }
  // Serve via loopback HTTP — `file://` blocks the bundle's XHR-loaded JSX
  // siblings, leaving the DOM empty.
  const served = await serveBundle(args.bundleDir);
  const fileUrl = `${served.url}/${encodeURIComponent(args.htmlFilename)}`;

  const browser = await chromium.launch({ headless: true });
  try {
    const ctx = await browser.newContext({
      viewport: { width: VIEWPORT_W, height: VIEWPORT_H },
      deviceScaleFactor: 1,
    });
    const page = await ctx.newPage();
    // tsx/esbuild wraps anonymous functions passed to page.evaluate with
    // `__name(fn, "name")` — that helper is missing in the browser context.
    // Inject a no-op shim so the wrapper resolves to identity.
    await page.addInitScript(() => {
      // @ts-expect-error — runtime polyfill for esbuild keepNames artefact
      globalThis.__name = (fn: unknown) => fn;
    });
    await page.goto(fileUrl, { waitUntil: 'load' });
    // Babel-standalone compiles JSX after `load`; settle window covers the slow path.
    await page.waitForTimeout(RENDER_SETTLE_MS);

    const interactiveClassRoots = ['knob', 'fader', 'vu', 'illuminated-button', 'segmented-readout', 'oscilloscope', 'detent-ring', 'led'];
    const nodes: ComputedStyleNode[] = await page.evaluate((roots: string[]) => {
      // Browser-context helpers — declared as arrow expressions so esbuild's
      // `__name(...)` wrapper (injected for named declarations under tsx) does
      // not leak into page.evaluate, which has no access to that helper.
      const classRootMatch = (el: Element): string | null => {
        for (const root of roots) {
          if (el.classList.contains(root)) return root;
        }
        return null;
      };
      const classifyNode = (el: Element): { node_kind: 'panel' | 'slot' | 'interactive'; slug: string | null } | null => {
        const panelSlug = el.getAttribute('data-panel-slug');
        if (panelSlug) return { node_kind: 'panel', slug: panelSlug };
        const slot = el.getAttribute('data-slot');
        if (slot) return { node_kind: 'slot', slug: slot };
        const interactiveRoot = classRootMatch(el);
        if (interactiveRoot) return { node_kind: 'interactive', slug: interactiveRoot };
        return null;
      };
      const domPath = (el: Element): string => {
        const stack: string[] = [];
        let cursor: Element | null = el;
        while (cursor && cursor !== document.body) {
          const cls = classifyNode(cursor);
          const tag = cursor.tagName.toLowerCase();
          stack.unshift(cls ? `${tag}[${cls.node_kind}:${cls.slug}]` : tag);
          cursor = cursor.parentElement;
        }
        return stack.join('>');
      };
      const all = Array.from(document.querySelectorAll<HTMLElement>('*'));
      const out: any[] = [];
      for (const el of all) {
        const cls = classifyNode(el);
        if (!cls) continue;
        const cs = window.getComputedStyle(el);
        out.push({
          node_kind: cls.node_kind,
          cd_slug: cls.slug,
          class_list: Array.from(el.classList),
          tag_name: el.tagName.toLowerCase(),
          dom_path: domPath(el),
          computed: {
            color: cs.color,
            backgroundColor: cs.backgroundColor,
            borderTopColor: cs.borderTopColor,
            borderRadius: cs.borderRadius,
            boxShadow: cs.boxShadow,
            fontFamily: cs.fontFamily,
            fontSize: cs.fontSize,
            fontWeight: cs.fontWeight,
            lineHeight: cs.lineHeight,
            letterSpacing: cs.letterSpacing,
            textAlign: cs.textAlign,
            opacity: cs.opacity,
            transform: cs.transform,
          },
        });
      }
      return out;
    }, interactiveClassRoots);

    return {
      schema_version: 1,
      source_html: path.relative(REPO_ROOT, htmlAbs),
      viewport: { width: VIEWPORT_W, height: VIEWPORT_H },
      generated_at_utc: new Date().toISOString(),
      node_count: nodes.length,
      nodes,
    };
  } finally {
    await browser.close();
    await served.close();
  }
}

async function main(): Promise<void> {
  const args = parseArgs(process.argv);
  let artifact: ComputedStylesArtifact;
  try {
    artifact = await extractComputedStyles(args);
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    process.stderr.write(`extract-cd-computed-styles: ${msg}\n`);
    process.exit(1);
    return;
  }
  fs.mkdirSync(path.dirname(args.outPath), { recursive: true });
  fs.writeFileSync(args.outPath, JSON.stringify(artifact, null, 2) + '\n', 'utf8');
  process.stdout.write(
    `extract-cd-computed-styles: wrote ${args.outPath} (${artifact.node_count} nodes)\n`,
  );
}

const entry = process.argv[1];
if (entry && import.meta.url === pathToFileURL(path.resolve(entry)).href) {
  void main();
}
