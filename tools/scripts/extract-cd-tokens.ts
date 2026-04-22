#!/usr/bin/env npx tsx
/**
 * CD pilot bundle → canonical token map (Stage 24 T24.1).
 *
 * **B-CD1 (drift-on-mutation guard):** The CD tree under `web/design-refs/step-8-console/`
 * is read-only ingestion input. Downstream drift + transcription must halt when raws skew
 * vs locked `web/lib/tokens/palette.json` (see T24.2).
 *
 * @packageDocumentation
 */
import * as fs from 'node:fs';
import * as path from 'node:path';
import * as process from 'node:process';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '..', '..');

export type CanonicalMap = {
  raws: Record<string, string>;
  semantic: Record<string, string>;
  motion: Record<string, string>;
  typeScale: Record<string, string>;
  spacing: Record<string, string>;
};

const DEFAULT_CSS = path.join(
  REPO_ROOT,
  'web/design-refs/step-8-console/ds/colors_and_type.css',
);
const DEFAULT_CD_PALETTE = path.join(
  REPO_ROOT,
  'web/design-refs/step-8-console/ds/palette.json',
);
const DEFAULT_LOCKED_PALETTE = path.join(REPO_ROOT, 'web/lib/tokens/palette.json');
const DEFAULT_DRIFT_OUT = path.join(
  REPO_ROOT,
  'web/design-refs/step-8-console/.drift-report.md',
);

function readFirstRootBlock(css: string): string {
  const m = css.match(/:root\s*\{([\s\S]*?)\n\}/);
  return m ? m[1] : '';
}

/** Parse custom properties inside a :root block body (no nested `{` in values for CD file). */
export function parseCustomProperties(block: string): Record<string, string> {
  const out: Record<string, string> = {};
  const re = /--([\w-]+)\s*:\s*([^;]+);/g;
  let mm: RegExpExecArray | null;
  while ((mm = re.exec(block)) !== null) {
    const name = `--${mm[1]}`;
    out[name] = mm[2].trim();
  }
  return out;
}

function bucketMap(flat: Record<string, string>): CanonicalMap {
  const raws: Record<string, string> = {};
  const semantic: Record<string, string> = {};
  const motion: Record<string, string> = {};
  const typeScale: Record<string, string> = {};
  const spacing: Record<string, string> = {};

  for (const [k, v] of Object.entries(flat)) {
    if (k.startsWith('--raw-')) {
      raws[k.slice('--raw-'.length)] = v;
    } else if (/^--text-(xs|sm|base|lg|xl|2xl)$/.test(k)) {
      typeScale[k] = v;
    } else if (k.startsWith('--lh-')) {
      typeScale[k] = v;
    } else if (k.startsWith('--font-')) {
      typeScale[k] = v;
    } else if (k.startsWith('--dur-') || k.startsWith('--ease-')) {
      motion[k] = v;
    } else if (k.startsWith('--sp-') || k.startsWith('--radius-') || k.startsWith('--shadow-')) {
      spacing[k] = v;
    } else if (
      k.startsWith('--bg-') ||
      k.startsWith('--text-') ||
      k.startsWith('--border-') ||
      k.startsWith('--overlay-') ||
      k.startsWith('--focus-')
    ) {
      semantic[k] = v;
    }
  }

  return { raws, semantic, motion, typeScale, spacing };
}

/** Merge optional CD `palette.json` object into `raws` / buckets when file exists. */
function mergeCdPaletteJson(map: CanonicalMap, jsonPath: string): void {
  if (!fs.existsSync(jsonPath)) return;
  const raw = JSON.parse(fs.readFileSync(jsonPath, 'utf8')) as Record<string, unknown>;
  if (raw.raw && typeof raw.raw === 'object' && raw.raw !== null) {
    for (const [key, val] of Object.entries(raw.raw as Record<string, string>)) {
      if (typeof val === 'string' && map.raws[key] === undefined) {
        map.raws[key] = val;
      }
    }
  }
}

export function buildCanonicalMap(opts: {
  cssPath: string;
  cdPaletteJsonPath: string;
}): CanonicalMap {
  const css = fs.readFileSync(opts.cssPath, 'utf8');
  const block = readFirstRootBlock(css);
  const flat = parseCustomProperties(block);
  const map = bucketMap(flat);
  mergeCdPaletteJson(map, opts.cdPaletteJsonPath);
  return map;
}

export type DriftRow = {
  key: string;
  cdValue: string;
  paletteValue: string;
  match: boolean;
};

/** Compare CD `raws` to locked `palette.json` `raw` for every CD raw key. */
export function computeRawDrift(
  map: CanonicalMap,
  lockedPalettePath: string,
): DriftRow[] {
  const palette = JSON.parse(fs.readFileSync(lockedPalettePath, 'utf8')) as {
    raw: Record<string, string>;
  };
  const rows: DriftRow[] = [];
  const norm = (h: string) => h.trim().toLowerCase();
  for (const [key, cdVal] of Object.entries(map.raws)) {
    const pVal = palette.raw[key];
    const match = pVal !== undefined && norm(pVal) === norm(cdVal);
    rows.push({
      key,
      cdValue: cdVal,
      paletteValue: pVal === undefined ? '(missing)' : pVal,
      match,
    });
  }
  return rows;
}

export function renderDriftReport(rows: DriftRow[]): string {
  const lines: string[] = [
    '# CD bundle drift report',
    '',
    'Compares CD-derived **raw** keys from `extract-cd-tokens` to `web/lib/tokens/palette.json` `raw`.',
    '',
    '| Key | CD value | palette.json value | Match? |',
    '| --- | --- | --- | --- |',
  ];
  for (const r of rows) {
    lines.push(
      `| \`${r.key}\` | \`${r.cdValue}\` | \`${r.paletteValue}\` | ${r.match ? 'Yes' : 'No'} |`,
    );
  }
  if (rows.length === 0) {
    lines.push('| — | — | — | (no CD raw keys) |');
  }
  lines.push('');
  return lines.join('\n');
}

function parseArgs(argv: string[]) {
  let outPath: string | null = null;
  let cssPath = DEFAULT_CSS;
  let cdPalettePath = DEFAULT_CD_PALETTE;
  let lockedPalettePath = DEFAULT_LOCKED_PALETTE;
  let driftPath = DEFAULT_DRIFT_OUT;
  let skipDrift = true;

  for (let i = 2; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--out' && argv[i + 1]) {
      outPath = argv[++i];
    } else if (a === '--css' && argv[i + 1]) {
      cssPath = path.resolve(REPO_ROOT, argv[++i]);
    } else if (a === '--cd-palette' && argv[i + 1]) {
      cdPalettePath = path.resolve(REPO_ROOT, argv[++i]);
    } else if (a === '--locked-palette' && argv[i + 1]) {
      lockedPalettePath = path.resolve(REPO_ROOT, argv[++i]);
    } else if (a === '--drift-out' && argv[i + 1]) {
      driftPath = path.resolve(REPO_ROOT, argv[++i]);
    } else if (a === '--with-drift') {
      skipDrift = false;
    } else if (a === '--help' || a === '-h') {
      console.log(`Usage: npx tsx tools/scripts/extract-cd-tokens.ts [options]

  --out <file>          Write JSON map (default: stdout)
  --css <file>          CD colors_and_type.css (default: web/design-refs/.../colors_and_type.css)
  --cd-palette <file>   Optional CD palette.json (default: ds/palette.json if present)
  --with-drift          Run drift pass: write .drift-report.md + exit 1 on mismatch
  --drift-out <file>    Drift markdown path (default: web/design-refs/step-8-console/.drift-report.md)
  --locked-palette <f>  Locked palette for drift (default: web/lib/tokens/palette.json)
`);
      process.exit(0);
    }
  }

  return { outPath, cssPath, cdPalettePath, lockedPalettePath, driftPath, skipDrift };
}

function main() {
  const { outPath, cssPath, cdPalettePath, lockedPalettePath, driftPath, skipDrift } =
    parseArgs(process.argv);

  const map = buildCanonicalMap({ cssPath, cdPaletteJsonPath: cdPalettePath });
  const json = JSON.stringify(map, null, 2);

  if (outPath) {
    fs.mkdirSync(path.dirname(outPath), { recursive: true });
    fs.writeFileSync(outPath, json, 'utf8');
  } else {
    process.stdout.write(json + '\n');
  }

  if (!skipDrift) {
    const rows = computeRawDrift(map, lockedPalettePath);
    const md = renderDriftReport(rows);
    fs.mkdirSync(path.dirname(driftPath), { recursive: true });
    fs.writeFileSync(driftPath, md, 'utf8');
    const bad = rows.some((r) => !r.match);
    if (bad) {
      process.stderr.write(`extract-cd-tokens: drift detected — see ${driftPath}\n`);
      process.exit(1);
    }
  }
}

main();
