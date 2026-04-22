#!/usr/bin/env npx tsx
/**
 * Consumes canonical map JSON from `extract-cd-tokens.ts`, applies D1/D2 renames,
 * and writes (1) a CSS fragment into `web/app/globals.css` inside CD markers and
 * (2) `export const cdBundle` into `web/lib/design-tokens.ts`.
 *
 * **Halt:** when `web/design-refs/step-8-console/.drift-report.md` contains a drift
 * row with Match? = No, or when drift cannot be confirmed clean.
 *
 * **D1 (motion names → production `ds-duration` vocabulary):** CD `--dur-*` maps to
 * `cdBundle.motion` keys aligned with Stage 24 master-plan intent (`--dur-fast` →
 * `instant`, `--dur-base` → `subtle`, `--dur-slow` → `gentle`, `--dur-reveal` →
 * `deliberate`). CSS emission uses non-colliding `--ds-cd-*` custom properties so
 * existing `@theme` duration tokens stay unchanged.
 *
 * **D2 (prefix rename):** CD `--raw-*` hex values emit as `--ds-raw-{key}`; semantic
 * `--text-*` / `--bg-*` (and peers) emit as `--ds-text-*` / `--ds-bg-*` with
 * `var(--raw-*)` expanded to hex where possible.
 */
import * as fs from 'node:fs';
import * as path from 'node:path';
import * as process from 'node:process';
import { fileURLToPath } from 'node:url';
import {
  type CanonicalMap,
  buildCanonicalMap,
  computeRawDrift,
} from './extract-cd-tokens.ts';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '..', '..');

const DEFAULT_DRIFT = path.join(
  REPO_ROOT,
  'web/design-refs/step-8-console/.drift-report.md',
);
const DEFAULT_GLOBALS = path.join(REPO_ROOT, 'web/app/globals.css');
const DEFAULT_DESIGN_TOKENS = path.join(REPO_ROOT, 'web/lib/design-tokens.ts');
const DEFAULT_LOCKED_PALETTE = path.join(REPO_ROOT, 'web/lib/tokens/palette.json');
const DEFAULT_CSS = path.join(
  REPO_ROOT,
  'web/design-refs/step-8-console/ds/colors_and_type.css',
);
const DEFAULT_CD_PALETTE = path.join(
  REPO_ROOT,
  'web/design-refs/step-8-console/ds/palette.json',
);

/** D1: CD duration token → cdBundle.motion key + `--ds-cd-duration-*` CSS name */
const D1_DURATION_CSS: Record<string, string> = {
  '--dur-fast': '--ds-cd-duration-instant',
  '--dur-base': '--ds-cd-duration-subtle',
  '--dur-slow': '--ds-cd-duration-gentle',
  '--dur-reveal': '--ds-cd-duration-deliberate',
};

function assertCleanDrift(driftPath: string): void {
  if (!fs.existsSync(driftPath)) {
    process.stderr.write(`transcribe-cd-tokens: missing drift report ${driftPath} — run extract-cd-tokens first\n`);
    process.exit(1);
  }
  const text = fs.readFileSync(driftPath, 'utf8');
  if (/\|\s*`[^`]+`\s*\|\s*`[^`]*`\s*\|\s*`[^`]*`\s*\|\s*No\s*\|/.test(text)) {
    process.stderr.write(`transcribe-cd-tokens: drift report has mismatches — fix palette or CD bundle first\n`);
    process.exit(1);
  }
}

function resolveCssVar(
  value: string,
  semantic: Record<string, string>,
  raws: Record<string, string>,
): string {
  let v = value.trim();
  for (let d = 0; d < 16; d++) {
    const m = /^var\((--[\w-]+)\)$/.exec(v);
    if (!m) return v;
    const name = m[1];
    if (name.startsWith('--raw-')) {
      const k = name.slice('--raw-'.length);
      const hex = raws[k];
      if (hex) return hex;
      return v;
    }
    const next = semantic[name];
    if (next) {
      v = next;
      continue;
    }
    return v;
  }
  return v;
}

/** Replace every `var(--*)` token in a compound value (e.g. `2px solid var(--text-accent-warn)`). */
function resolveVarsInValue(
  value: string,
  semantic: Record<string, string>,
  raws: Record<string, string>,
): string {
  return value.replace(/var\((--[\w-]+)\)/g, (full) =>
    resolveCssVar(full, semantic, raws),
  );
}

function semanticToDsName(prop: string): string {
  if (prop.startsWith('--raw-')) return `--ds-raw-${prop.slice('--raw-'.length)}`;
  if (prop.startsWith('--text-')) return `--ds-text-${prop.slice('--text-'.length)}`;
  if (prop.startsWith('--bg-')) return `--ds-bg-${prop.slice('--bg-'.length)}`;
  if (prop.startsWith('--border-')) return `--ds-border-${prop.slice('--border-'.length)}`;
  if (prop.startsWith('--overlay-')) return `--ds-overlay-${prop.slice('--overlay-'.length)}`;
  if (prop.startsWith('--focus-')) return `--ds-focus-${prop.slice('--focus-'.length)}`;
  return `--ds${prop}`;
}

function buildCssFragment(map: CanonicalMap): string {
  const lines: string[] = [];
  const { raws, semantic, motion, typeScale, spacing } = map;

  for (const [k, v] of Object.entries(raws)) {
    lines.push(`  --ds-raw-${k}: ${v};`);
  }

  for (const [k, v] of Object.entries(semantic)) {
    const name = semanticToDsName(k);
    const val = resolveVarsInValue(v, semantic, raws);
    lines.push(`  ${name}: ${val};`);
  }

  for (const [k, v] of Object.entries(motion)) {
    if (D1_DURATION_CSS[k]) {
      lines.push(`  ${D1_DURATION_CSS[k]}: ${v};`);
    } else {
      const short = k.replace(/^--/, '').replace(/-/g, '');
      lines.push(`  --ds-cd-${short}: ${v};`);
    }
  }

  for (const [k, v] of Object.entries(typeScale)) {
    const short = k.replace(/^--/, '');
    lines.push(`  --ds-cd-${short}: ${v};`);
  }

  for (const [k, v] of Object.entries(spacing)) {
    const short = k.replace(/^--/, '');
    lines.push(`  --ds-cd-${short}: ${v};`);
  }

  return lines.join('\n');
}

function buildCdBundleObject(map: CanonicalMap): string {
  const motion: Record<string, string> = {};
  const order = ['--dur-fast', '--dur-base', '--dur-slow', '--dur-reveal'];
  const labels = ['instant', 'subtle', 'gentle', 'deliberate'];
  order.forEach((k, i) => {
    const v = map.motion[k];
    if (v) motion[labels[i]] = v;
  });
  if (map.motion['--ease-enter']) motion.easeEnter = map.motion['--ease-enter'];
  if (map.motion['--ease-exit']) motion.easeExit = map.motion['--ease-exit'];

  const obj = {
    raws: map.raws,
    semantic: map.semantic,
    motion,
    typeScale: map.typeScale,
    spacing: map.spacing,
  };
  return JSON.stringify(obj, null, 2);
}

function injectGlobalsMarker(css: string, fragment: string): string {
  const block =
    `\n  /* CD-BUNDLE-START */\n${fragment}\n  /* CD-BUNDLE-END */\n`;
  const re = /\/\* CD-BUNDLE-START \*\/[\s\S]*?\/\* CD-BUNDLE-END \*\//;
  if (re.test(css)) {
    return css.replace(re, block.trimStart());
  }
  const anchor = css.indexOf('\n}\n\n@media (prefers-reduced-motion: reduce)');
  if (anchor === -1) {
    throw new Error('transcribe-cd-tokens: could not find @theme closing anchor in globals.css');
  }
  return css.slice(0, anchor) + block + css.slice(anchor);
}

function injectDesignTokens(ts: string, bundleLiteral: string): string {
  const exportLine = `export const cdBundle = ${bundleLiteral} as const;`;
  const wrapped = `\n/* CD-BUNDLE-TS-START */\n${exportLine}\n/* CD-BUNDLE-TS-END */\n`;
  const re = /\/\* CD-BUNDLE-TS-START \*\/[\s\S]*?\/\* CD-BUNDLE-TS-END \*\//;
  if (re.test(ts)) {
    return ts.replace(re, wrapped.trim());
  }
  return ts.trimEnd() + '\n' + wrapped;
}

function parseArgs(argv: string[]) {
  let inPath: string | null = null;
  let driftPath = DEFAULT_DRIFT;
  let globalsPath = DEFAULT_GLOBALS;
  let tokensPath = DEFAULT_DESIGN_TOKENS;
  let fromSource = false;

  for (let i = 2; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--in' && argv[i + 1]) {
      inPath = path.resolve(REPO_ROOT, argv[++i]);
    } else if (a === '--drift' && argv[i + 1]) {
      driftPath = path.resolve(REPO_ROOT, argv[++i]);
    } else if (a === '--globals' && argv[i + 1]) {
      globalsPath = path.resolve(REPO_ROOT, argv[++i]);
    } else if (a === '--design-tokens' && argv[i + 1]) {
      tokensPath = path.resolve(REPO_ROOT, argv[++i]);
    } else if (a === '--from-source') {
      fromSource = true;
    } else if (a === '--help' || a === '-h') {
      console.log(`Usage: npx tsx tools/scripts/transcribe-cd-tokens.ts [options]

  --in <file>       Canonical map JSON (default: stdin)
  --from-source     Re-run extract paths instead of --in
  --drift <file>    Drift report to gate on (default: web/design-refs/.../.drift-report.md)
  --globals <file>  globals.css path
  --design-tokens   design-tokens.ts path
`);
      process.exit(0);
    }
  }
  return { inPath, driftPath, globalsPath, tokensPath, fromSource };
}

function readStdin(): Promise<string> {
  return new Promise((resolve, reject) => {
    const chunks: Buffer[] = [];
    process.stdin.on('data', (c) => chunks.push(c));
    process.stdin.on('end', () => resolve(Buffer.concat(chunks).toString('utf8')));
    process.stdin.on('error', reject);
  });
}

async function main() {
  const { inPath, driftPath, globalsPath, tokensPath, fromSource } = parseArgs(process.argv);

  assertCleanDrift(driftPath);

  let map: CanonicalMap;
  if (fromSource) {
    map = buildCanonicalMap({
      cssPath: DEFAULT_CSS,
      cdPaletteJsonPath: DEFAULT_CD_PALETTE,
    });
    const rows = computeRawDrift(map, DEFAULT_LOCKED_PALETTE);
    if (rows.some((r) => !r.match)) {
      process.stderr.write('transcribe-cd-tokens: raws still drift vs locked palette\n');
      process.exit(1);
    }
  } else if (inPath) {
    map = JSON.parse(fs.readFileSync(inPath, 'utf8')) as CanonicalMap;
  } else if (process.stdin.isTTY) {
    process.stderr.write('transcribe-cd-tokens: provide --in, --from-source, or pipe JSON\n');
    process.exit(1);
  } else {
    map = JSON.parse(await readStdin()) as CanonicalMap;
  }

  const fragment = buildCssFragment(map);
  const globals = fs.readFileSync(globalsPath, 'utf8');
  fs.writeFileSync(globalsPath, injectGlobalsMarker(globals, fragment), 'utf8');

  const bundleObj = buildCdBundleObject(map);
  const ts = fs.readFileSync(tokensPath, 'utf8');
  fs.writeFileSync(tokensPath, injectDesignTokens(ts, bundleObj), 'utf8');

  process.stderr.write(
    `transcribe-cd-tokens: updated ${path.relative(REPO_ROOT, globalsPath)} + ${path.relative(REPO_ROOT, tokensPath)}\n`,
  );
}

main().catch((e) => {
  process.stderr.write(String(e) + '\n');
  process.exit(1);
});
