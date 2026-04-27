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

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '..', '..');

const DEFAULT_BUNDLE_DIR = path.join(REPO_ROOT, 'web/design-refs/step-1-game-ui/cd-bundle');
const DEFAULT_OUT = path.join(REPO_ROOT, 'web/design-refs/step-1-game-ui/ir.json');

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

// -- Bundle assembly ---------------------------------------------------------

export interface TranscribeOpts {
  bundleDir: string;
}

export function buildIrFromBundle(opts: TranscribeOpts): Ir {
  const cssPath = path.join(opts.bundleDir, 'tokens.css');
  const panelsPath = path.join(opts.bundleDir, 'panels.json');
  const interactivesPath = path.join(opts.bundleDir, 'interactives.json');

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

  return { tokens, panels, interactives };
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

function main() {
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
}

const entry = process.argv[1];
if (entry && import.meta.url === pathToFileURL(path.resolve(entry)).href) {
  main();
}
