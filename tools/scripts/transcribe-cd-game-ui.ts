#!/usr/bin/env npx tsx
/**
 * CD bundle → IR JSON transcribe (Game UI Design System Stage 1 T1.5).
 *
 * Mirrors `tools/scripts/extract-cd-tokens.ts` structural pattern:
 *   - tsx shebang + REPO_ROOT calc + parseArgs --in/--out
 *   - parser fns exported for unit-test reuse
 *   - main() entry guard (only runs under `tsx tools/scripts/transcribe-cd-game-ui.ts`)
 *   - schema fail → stderr message + `process.exit(1)`
 *
 * CD bundle layout (locked by `docs/game-ui-mvp-authoring-approach-exploration.md` §Phase 4):
 *   `web/design-refs/step-1-game-ui/cd-bundle/`
 *     `tokens.css`       — five token subblocks via grouped CSS custom properties
 *     `panels.json`      — panels[] with archetype + slots[] (name/accepts/children)
 *     `interactives.json` — interactives[] with slug/kind/detail per StudioControl ring
 *
 * Tokens.css naming convention (one block per `:root.{group}-{slug}`):
 *   :root.palette-{slug}       --ramp-N: #hex;   (N = 0..k-1, ordered low→high)
 *   :root.frame_style-{slug}   --edge: single|double; --inner-shadow-alpha: 0..1;
 *   :root.font_face-{slug}     --family: '<family>'; --weight: 400;
 *   :root.motion_curve-{slug}  --kind: spring|cubic-bezier; --stiffness/--damping/--c1/--c2/--duration-ms;
 *   :root.illumination-{slug}  --color: #hex; --halo-radius-px: <px>;
 *
 * Output: typed `web/design-refs/step-1-game-ui/ir.json`.
 *
 * @packageDocumentation
 */
import * as fs from 'node:fs';
import * as path from 'node:path';
import * as process from 'node:process';
import { fileURLToPath, pathToFileURL } from 'node:url';
import {
  type Ir,
  type IrPanel,
  type IrTab,
  type IrRow,
  type IrInteractive,
  type IrTokens,
  type IrTokenPalette,
  type IrTokenFrameStyle,
  type IrTokenFontFace,
  type IrTokenMotionCurve,
  type IrTokenIllumination,
  validateIrShape,
  validateSlotAccept,
} from './ir-schema.ts';
import { runSplit, CANONICAL_ICON_SLUGS } from './icons-svg-split.ts';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '..', '..');

const DEFAULT_BUNDLE_DIR = path.join(REPO_ROOT, 'web/design-refs/step-1-game-ui/cd-bundle');
const DEFAULT_OUT = path.join(REPO_ROOT, 'web/design-refs/step-1-game-ui/ir.json');
const DEFAULT_ICONS_OUT_DIR = path.join(REPO_ROOT, 'Assets/Sprites/Icons');

// -- Token css parsing --------------------------------------------------------

interface RootBlock {
  group: string;
  slug: string;
  props: Record<string, string>;
}

/** Parse all `:root.{group}-{slug} { ... }` blocks from a CD tokens.css file. */
export function parseRootBlocks(css: string): RootBlock[] {
  const out: RootBlock[] = [];
  const re = /:root\.([a-z_]+)-([a-zA-Z0-9_-]+)\s*\{([\s\S]*?)\n\}/g;
  let m: RegExpExecArray | null;
  while ((m = re.exec(css)) !== null) {
    const group = m[1];
    const slug = m[2];
    const body = m[3];
    const props: Record<string, string> = {};
    const propRe = /--([\w-]+)\s*:\s*([^;]+);/g;
    let pm: RegExpExecArray | null;
    while ((pm = propRe.exec(body)) !== null) {
      props[pm[1]] = pm[2].trim();
    }
    out.push({ group, slug, props });
  }
  return out;
}

function stripQuotes(v: string): string {
  return v.replace(/^['"]|['"]$/g, '');
}

function parseNumberList(v: string): number[] {
  return v
    .split(',')
    .map((s) => s.trim())
    .filter((s) => s.length > 0)
    .map((s) => Number(s));
}

export function parseTokensCss(css: string): IrTokens {
  const blocks = parseRootBlocks(css);
  const palette: IrTokenPalette[] = [];
  const frame_style: IrTokenFrameStyle[] = [];
  const font_face: IrTokenFontFace[] = [];
  const motion_curve: IrTokenMotionCurve[] = [];
  const illumination: IrTokenIllumination[] = [];

  for (const b of blocks) {
    if (b.group === 'palette') {
      const ramp: string[] = [];
      const stops = Object.keys(b.props)
        .filter((k) => /^ramp-\d+$/.test(k))
        .sort((a, b) => Number(a.slice(5)) - Number(b.slice(5)));
      for (const k of stops) ramp.push(b.props[k]);
      palette.push({ slug: b.slug, ramp });
    } else if (b.group === 'frame_style') {
      frame_style.push({
        slug: b.slug,
        edge: b.props.edge ?? '',
        innerShadowAlpha: Number(b.props['inner-shadow-alpha'] ?? '0'),
      });
    } else if (b.group === 'font_face') {
      font_face.push({
        slug: b.slug,
        family: stripQuotes(b.props.family ?? ''),
        weight: Number(b.props.weight ?? '400'),
      });
    } else if (b.group === 'motion_curve') {
      const kind = b.props.kind ?? '';
      const row: IrTokenMotionCurve = { slug: b.slug, kind };
      if (b.props.stiffness !== undefined) row.stiffness = Number(b.props.stiffness);
      if (b.props.damping !== undefined) row.damping = Number(b.props.damping);
      if (b.props.c1 !== undefined) row.c1 = parseNumberList(b.props.c1);
      if (b.props.c2 !== undefined) row.c2 = parseNumberList(b.props.c2);
      if (b.props['duration-ms'] !== undefined) row.durationMs = Number(b.props['duration-ms']);
      motion_curve.push(row);
    } else if (b.group === 'illumination') {
      illumination.push({
        slug: b.slug,
        color: b.props.color ?? '',
        haloRadiusPx: Number(b.props['halo-radius-px'] ?? '0'),
      });
    }
  }

  return { palette, frame_style, font_face, motion_curve, illumination };
}

// -- Panels + interactives JSON parsing --------------------------------------

export function parsePanelsJson(raw: unknown): IrPanel[] {
  if (!Array.isArray(raw)) {
    throw new Error('panels.json: top-level must be array');
  }
  return raw as IrPanel[];
}

export function parseInteractivesJson(raw: unknown): IrInteractive[] {
  if (!Array.isArray(raw)) {
    throw new Error('interactives.json: top-level must be array');
  }
  return raw as IrInteractive[];
}

// -- panels.jsx tabs/rows extraction (Stage 13.1 — IR v2) --------------------

/**
 * Locate the `function PanelXxx() { ... }` block in `panels.jsx` source whose
 * body declares `data-panel-slug="${slug}"`. Returns the function body src
 * (including the brace-matched body), or `null` when not found. Used by
 * `extractTabs` + `extractRows` to scope the heuristics per panel.
 *
 * Determinism: relies on first occurrence of `data-panel-slug="${slug}"`; the
 * CD bundle source declares one section per slug.
 */
export function locatePanelSrc(jsxSrc: string, slug: string): string | null {
  const marker = `data-panel-slug="${slug}"`;
  const idx = jsxSrc.indexOf(marker);
  if (idx < 0) return null;
  const fnIdx = jsxSrc.lastIndexOf('function Panel', idx);
  if (fnIdx < 0) return null;
  const braceStart = jsxSrc.indexOf('{', fnIdx);
  if (braceStart < 0) return null;
  let depth = 0;
  for (let i = braceStart; i < jsxSrc.length; i++) {
    const c = jsxSrc[i];
    if (c === '{') depth++;
    else if (c === '}') {
      depth--;
      if (depth === 0) return jsxSrc.slice(fnIdx, i + 1);
    }
  }
  return null;
}

/**
 * Extract IR v2 tab descriptors from a panel's JSX source (D2 heuristic).
 *
 * Recognized markers (deterministic, first-match-wins per id):
 *   1. `<Tab id="..." label="..." [active]>` JSX nodes.
 *   2. `role="tab"` attrs paired with `data-tab-id="..." [data-tab-label="..."]`.
 *
 * Returns `[]` when no tab markers detected. Empty arrays are dropped by the
 * caller before emit (`tabs` field omitted on flat panels).
 */
export function extractTabs(panelSrc: string): IrTab[] {
  const out: IrTab[] = [];
  const seen = new Set<string>();

  const tabRe = /<Tab\b([^>]*?)\/?>/g;
  let m: RegExpExecArray | null;
  while ((m = tabRe.exec(panelSrc)) !== null) {
    const attrs = m[1];
    const id = (attrs.match(/\bid=["']([^"']+)["']/) ?? [])[1];
    if (!id || seen.has(id)) continue;
    const label = (attrs.match(/\blabel=["']([^"']+)["']/) ?? [])[1] ?? id;
    const active = /\bactive\b(?!=)/.test(attrs) || /\bactive=\{?true\}?/.test(attrs);
    seen.add(id);
    const tab: IrTab = { id, label };
    if (active) tab.active = true;
    out.push(tab);
  }

  const roleRe = /role=["']tab["']([^>]*?)\/?>/g;
  while ((m = roleRe.exec(panelSrc)) !== null) {
    const attrs = m[1];
    const id = (attrs.match(/\bdata-tab-id=["']([^"']+)["']/) ?? [])[1];
    if (!id || seen.has(id)) continue;
    const label = (attrs.match(/\bdata-tab-label=["']([^"']+)["']/) ?? [])[1] ?? id;
    const active = /\bdata-tab-active=["']true["']/.test(attrs);
    seen.add(id);
    const tab: IrTab = { id, label };
    if (active) tab.active = true;
    out.push(tab);
  }

  return out;
}

function splitObjectLiterals(arrayBody: string): string[] {
  const out: string[] = [];
  let depth = 0;
  let start = -1;
  let inStr: '"' | "'" | null = null;
  for (let i = 0; i < arrayBody.length; i++) {
    const c = arrayBody[i];
    if (inStr) {
      if (c === '\\') {
        i++;
        continue;
      }
      if (c === inStr) inStr = null;
      continue;
    }
    if (c === '"' || c === "'") {
      inStr = c;
      continue;
    }
    if (c === '{') {
      if (depth === 0) start = i;
      depth++;
    } else if (c === '}') {
      depth--;
      if (depth === 0 && start >= 0) {
        out.push(arrayBody.slice(start, i + 1));
        start = -1;
      }
    }
  }
  return out;
}

function parseRowEntry(entry: string): IrRow | null {
  const inner = entry.replace(/^\{\s*|\s*\}$/g, '');
  const fields: Record<string, string> = {};
  const fieldRe = /(\w+)\s*:\s*(?:"([^"]*)"|'([^']*)'|(null|true|false|[\d.]+|\{[^}]*\}))/g;
  let fm: RegExpExecArray | null;
  while ((fm = fieldRe.exec(inner)) !== null) {
    const name = fm[1];
    if (fm[2] !== undefined) fields[name] = fm[2];
    else if (fm[3] !== undefined) fields[name] = fm[3];
    else fields[name] = fm[4] ?? '';
  }

  const label = fields.label ?? fields.name ?? fields.n ?? '';
  const value = fields.value ?? fields.year ?? '';
  if (!label && !value) return null;

  let kind: IrRow['kind'] = 'stat';
  if (fields.name && (fields.id || fields.year)) kind = 'detail';

  const row: IrRow = { kind };
  if (label) row.label = label;
  if (value) row.value = value;
  if (fields.segments) {
    const n = Number(fields.segments);
    if (Number.isFinite(n)) row.segments = n;
  }
  return row;
}

/**
 * Extract IR v2 row descriptors from a panel's JSX source (D3 heuristic).
 *
 * Recognized pattern: panel declares `const rows = [...]` (or `slots` / `steps` /
 * `tools`), each entry being an object literal carrying `label` / `value` /
 * `name` / `year` fields, then iterates via `.map(...)` to render rows.
 *
 * Returns `[]` when no array-of-objects-mapped-to-render pattern detected.
 * Empty arrays are dropped by the caller before emit.
 *
 * Kind mapping:
 *   - object has `name` + (`id` | `year`) → `detail` (slot/save-game-style row).
 *   - otherwise → `stat` (label/value pair, optionally segmented).
 */
export function extractRows(panelSrc: string): IrRow[] {
  const out: IrRow[] = [];
  const constRe = /const\s+(rows|slots|steps|tools)\s*=\s*\[([\s\S]*?)\];/g;
  let cm: RegExpExecArray | null;
  while ((cm = constRe.exec(panelSrc)) !== null) {
    const name = cm[1];
    const body = cm[2];
    const mapRe = new RegExp(`\\b${name}\\.map\\(`);
    if (!mapRe.test(panelSrc)) continue;

    // `tools` is a 3x3 patchbay grid in `toolbar` panel — not a row layout.
    // Keep heuristic narrow: skip `tools` unless rendered inside `data-slot`
    // suffixed `-list` or `row-list`. Heuristic per acceptance crit ("list of
    // rows OR flex row containers").
    if (name === 'tools' && !/data-slot=["']row-list["']|data-slot=["'][^"']*-list["']/.test(panelSrc)) {
      continue;
    }

    for (const entry of splitObjectLiterals(body)) {
      const row = parseRowEntry(entry);
      if (row) out.push(row);
    }
    if (out.length > 0) break; // first matching const wins — deterministic
  }
  return out;
}

/**
 * Stage 13.4 (TECH-9867) — derive IR v2 `defaultTabIndex` per panel.
 *
 * Resolution order:
 *   1. D1 override — slug `city-stats-handoff` opens on the Infrastructure tab.
 *      First tab whose `id` or `label` matches `/infrastructure/i`.
 *   2. First tab carrying `active` (set by `extractTabs` when the JSX tab node
 *      declared `active` / `active={true}` / `data-tab-active="true"`).
 *   3. `0` (sane fallback when tabs[] non-empty but no active marker).
 *
 * Returns `undefined` when `tabs.length === 0` (caller drops field on tabless
 * panels — keeps IR shape stable for v1-shape entries).
 */
export function extractDefaultTabIndex(
  slug: string,
  tabs: IrTab[],
): number | undefined {
  if (tabs.length === 0) return undefined;

  if (slug === 'city-stats-handoff') {
    const infraIdx = tabs.findIndex(
      (t) => /infrastructure/i.test(t.id) || /infrastructure/i.test(t.label),
    );
    if (infraIdx >= 0) return infraIdx;
  }

  const activeIdx = tabs.findIndex((t) => t.active === true);
  if (activeIdx >= 0) return activeIdx;

  return 0;
}

// -- Bundle assembly ---------------------------------------------------------

export interface TranscribeOpts {
  bundleDir: string;
}

export function buildIrFromBundle(opts: TranscribeOpts): Ir {
  const cssPath = path.join(opts.bundleDir, 'tokens.css');
  const panelsPath = path.join(opts.bundleDir, 'panels.json');
  const interactivesPath = path.join(opts.bundleDir, 'interactives.json');
  const panelsJsxPath = path.join(opts.bundleDir, 'panels.jsx');

  if (!fs.existsSync(cssPath)) {
    throw new Error(`transcribe: missing tokens.css at ${cssPath}`);
  }
  if (!fs.existsSync(panelsPath)) {
    throw new Error(`transcribe: missing panels.json at ${panelsPath}`);
  }
  if (!fs.existsSync(interactivesPath)) {
    throw new Error(`transcribe: missing interactives.json at ${interactivesPath}`);
  }

  const tokens = parseTokensCss(fs.readFileSync(cssPath, 'utf8'));
  const panels = parsePanelsJson(JSON.parse(fs.readFileSync(panelsPath, 'utf8')));
  const interactives = parseInteractivesJson(
    JSON.parse(fs.readFileSync(interactivesPath, 'utf8')),
  );

  // IR v2 enrichment (Stage 13.1 — DEC-A21 Path C). Read panels.jsx (when
  // present in bundle) and attach per-panel `tabs[]` / `rows[]` extracted
  // from JSX source. Panels not declared in panels.jsx (or with empty
  // extraction) emit no tabs/rows fields → flat v1-shape on those entries.
  if (fs.existsSync(panelsJsxPath)) {
    const jsxSrc = fs.readFileSync(panelsJsxPath, 'utf8');
    for (const panel of panels) {
      const panelSrc = locatePanelSrc(jsxSrc, panel.slug);
      if (!panelSrc) continue;
      const tabs = extractTabs(panelSrc);
      const rows = extractRows(panelSrc);
      if (tabs.length > 0) {
        panel.tabs = tabs;
        const defaultIdx = extractDefaultTabIndex(panel.slug, tabs);
        if (defaultIdx !== undefined) panel.defaultTabIndex = defaultIdx;
      }
      if (rows.length > 0) panel.rows = rows;
    }
  }

  return { tokens, panels, interactives, schemaVersion: 2 };
}

// -- CLI ---------------------------------------------------------------------

function parseArgs(argv: string[]) {
  let bundleDir = DEFAULT_BUNDLE_DIR;
  let outPath = DEFAULT_OUT;

  for (let i = 2; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--in' && argv[i + 1]) {
      bundleDir = path.resolve(REPO_ROOT, argv[++i]);
    } else if (a === '--out' && argv[i + 1]) {
      outPath = path.resolve(REPO_ROOT, argv[++i]);
    } else if (a === '--help' || a === '-h') {
      console.log(`Usage: npx tsx tools/scripts/transcribe-cd-game-ui.ts [options]

  --in  <dir>   CD bundle dir (default: web/design-refs/step-1-game-ui/cd-bundle)
  --out <file>  IR JSON output (default: web/design-refs/step-1-game-ui/ir.json)

Schema fail → stderr + exit 1. Slot accept-rule violation reports panel/slot/offending children.
`);
      process.exit(0);
    }
  }

  return { bundleDir, outPath };
}

async function main() {
  const { bundleDir, outPath } = parseArgs(process.argv);

  let ir: Ir;
  try {
    ir = buildIrFromBundle({ bundleDir });
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    process.stderr.write(`transcribe-cd-game-ui: ${msg}\n`);
    process.exit(1);
    return;
  }

  const shape = validateIrShape(ir);
  if (!shape.ok) {
    process.stderr.write(
      `transcribe-cd-game-ui: ${shape.error} at ${shape.path} — ${shape.detail}\n`,
    );
    process.exit(1);
    return;
  }

  const accept = validateSlotAccept(ir);
  if (!accept.ok) {
    process.stderr.write(
      `transcribe-cd-game-ui: ${accept.error} — panel '${accept.panel}' slot '${accept.slot}' has offending children [${accept.offending_children.join(', ')}] not in accepts [${accept.accepts.join(', ')}]\n`,
    );
    process.exit(1);
    return;
  }

  fs.mkdirSync(path.dirname(outPath), { recursive: true });
  fs.writeFileSync(outPath, JSON.stringify(ir, null, 2) + '\n', 'utf8');
  process.stdout.write(`transcribe-cd-game-ui: wrote ${outPath}\n`);

  // Stage 13.3 T1 — chain icon split as a sub-step. Source SVG lives in the
  // CD bundle; emit one PNG per `<symbol id="icon-…">` into Assets/Sprites/Icons.
  // Missing canonical ids are non-fatal — ThemedIcon falls back at runtime.
  const iconsSvgPath = path.join(bundleDir, 'icons.svg');
  if (fs.existsSync(iconsSvgPath)) {
    try {
      const split = await runSplit({ sourcePath: iconsSvgPath, outDir: DEFAULT_ICONS_OUT_DIR });
      process.stdout.write(
        `transcribe-cd-game-ui: icons split emitted ${split.emitted.length}/${CANONICAL_ICON_SLUGS.length} → ${DEFAULT_ICONS_OUT_DIR}\n`,
      );
      if (split.missing.length > 0) {
        process.stderr.write(
          `transcribe-cd-game-ui: icons split WARNING ${split.missing.length} canonical id(s) absent (reserved-slug hooks): ${split.missing.join(', ')}\n`,
        );
      }
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      process.stderr.write(`transcribe-cd-game-ui: icons split failed — ${msg}\n`);
      process.exit(1);
      return;
    }
  } else {
    process.stderr.write(
      `transcribe-cd-game-ui: no icons.svg in bundle dir — skipping icon split step\n`,
    );
  }
}

const entry = process.argv[1];
if (entry && import.meta.url === pathToFileURL(path.resolve(entry)).href) {
  main().catch((e) => {
    const msg = e instanceof Error ? e.message : String(e);
    process.stderr.write(`transcribe-cd-game-ui: ${msg}\n`);
    process.exit(1);
  });
}
