#!/usr/bin/env npx tsx
/**
 * CD bundle → layout-rects.json (Stage 12 Step 14 P5.a — locked 2026-04-30).
 *
 * Renders `web/design-refs/step-1-game-ui/cd-bundle/Studio Rack Game UI.html`
 * headlessly at 1920×1080 and dumps each panel/slot/interactive's
 * `getBoundingClientRect()` plus parent-relative offsets — the truth source
 * for ThemedPanel / ThemedButton / ThemedKnob anchor + size_delta values
 * baked by `bake_ui_from_ir`. Closes the gap where Step 13 had to eyeball
 * RectTransform layout.
 *
 * Captured per element:
 *   - data-cd-slug + tag + dom_path
 *   - viewport_rect: { x, y, width, height } from getBoundingClientRect
 *   - parent_relative_rect: same coords but relative to nearest [data-cd-slug] ancestor
 *   - aspect_ratio (width / height) for round elements
 *
 * Output: `web/design-refs/step-1-game-ui/layout-rects.json`.
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
const DEFAULT_OUT = path.join(REPO_ROOT, 'web/design-refs/step-1-game-ui/layout-rects.json');
const DEFAULT_HTML_FILENAME = 'Studio Rack Game UI.html';
const VIEWPORT_W = 1920;
const VIEWPORT_H = 1080;
const RENDER_SETTLE_MS = 1500;

export interface LayoutRect {
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface LayoutRectNode {
  /** node_kind: 'panel' (data-panel-slug), 'slot' (data-slot), or 'interactive' (class-rooted archetype). */
  node_kind: 'panel' | 'slot' | 'interactive';
  cd_slug: string | null;
  class_list: string[];
  tag_name: string;
  dom_path: string;
  parent_kind: 'panel' | 'slot' | 'interactive' | null;
  parent_cd_slug: string | null;
  viewport_rect: LayoutRect;
  parent_relative_rect: LayoutRect;
  aspect_ratio: number;
}

export interface LayoutRectsArtifact {
  schema_version: 1;
  source_html: string;
  viewport: { width: number; height: number };
  generated_at_utc: string;
  node_count: number;
  nodes: LayoutRectNode[];
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
      console.log(`Usage: npx tsx tools/scripts/extract-cd-layout-rects.ts [options]

  --in  <dir>   CD bundle dir (default: web/design-refs/step-1-game-ui/cd-bundle)
  --out <file>  layout-rects.json output (default: web/design-refs/step-1-game-ui/layout-rects.json)

Renders ${DEFAULT_HTML_FILENAME} at ${VIEWPORT_W}x${VIEWPORT_H} via Playwright + chromium and dumps
getBoundingClientRect + parent-relative rect for every [data-cd-slug] node.
`);
      process.exit(0);
    }
  }
  return { bundleDir, outPath, htmlFilename };
}

export async function extractLayoutRects(args: CliArgs): Promise<LayoutRectsArtifact> {
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
    await page.waitForTimeout(RENDER_SETTLE_MS);

    const interactiveClassRoots = ['knob', 'fader', 'vu', 'illuminated-button', 'segmented-readout', 'oscilloscope', 'detent-ring', 'led'];
    const nodes: LayoutRectNode[] = await page.evaluate(
      (args: { roots: string[]; vw: number; vh: number }) => {
        // Browser-context helpers — arrow expressions to avoid esbuild's
        // `__name(...)` wrapper leaking into page.evaluate (no helper there).
        const classRootMatch = (el: Element): string | null => {
          for (const root of args.roots) {
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
        const nearestClassifiedAncestor = (el: Element): Element | null => {
          let cursor: Element | null = el.parentElement;
          while (cursor) {
            if (classifyNode(cursor)) return cursor;
            cursor = cursor.parentElement;
          }
          return null;
        };
        const rectOf = (el: Element): { x: number; y: number; width: number; height: number } => {
          const r = el.getBoundingClientRect();
          return {
            x: Math.round(r.left * 100) / 100,
            y: Math.round(r.top * 100) / 100,
            width: Math.round(r.width * 100) / 100,
            height: Math.round(r.height * 100) / 100,
          };
        };
        const all = Array.from(document.querySelectorAll<HTMLElement>('*'));
        const out: any[] = [];
        for (const el of all) {
          const cls = classifyNode(el);
          if (!cls) continue;
          const vp = rectOf(el);
          const parentEl = nearestClassifiedAncestor(el);
          const parentCls = parentEl ? classifyNode(parentEl) : null;
          const parentVp = parentEl ? rectOf(parentEl) : { x: 0, y: 0, width: args.vw, height: args.vh };
          const aspect = vp.height > 0 ? Math.round((vp.width / vp.height) * 1000) / 1000 : 0;
          out.push({
            node_kind: cls.node_kind,
            cd_slug: cls.slug,
            class_list: Array.from(el.classList),
            tag_name: el.tagName.toLowerCase(),
            dom_path: domPath(el),
            parent_kind: parentCls ? parentCls.node_kind : null,
            parent_cd_slug: parentCls ? parentCls.slug : null,
            viewport_rect: vp,
            parent_relative_rect: {
              x: Math.round((vp.x - parentVp.x) * 100) / 100,
              y: Math.round((vp.y - parentVp.y) * 100) / 100,
              width: vp.width,
              height: vp.height,
            },
            aspect_ratio: aspect,
          });
        }
        return out;
      },
      { roots: interactiveClassRoots, vw: VIEWPORT_W, vh: VIEWPORT_H },
    );

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
  let artifact: LayoutRectsArtifact;
  try {
    artifact = await extractLayoutRects(args);
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    process.stderr.write(`extract-cd-layout-rects: ${msg}\n`);
    process.exit(1);
    return;
  }
  fs.mkdirSync(path.dirname(args.outPath), { recursive: true });
  fs.writeFileSync(args.outPath, JSON.stringify(artifact, null, 2) + '\n', 'utf8');
  process.stdout.write(
    `extract-cd-layout-rects: wrote ${args.outPath} (${artifact.node_count} nodes)\n`,
  );
}

const entry = process.argv[1];
if (entry && import.meta.url === pathToFileURL(path.resolve(entry)).href) {
  void main();
}
