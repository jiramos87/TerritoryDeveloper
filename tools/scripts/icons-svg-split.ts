#!/usr/bin/env npx tsx
/**
 * Icon SVG split tool (Stage 13.3 T1 — D6.A per-id PNG export).
 *
 * Reads `web/design-refs/step-1-game-ui/cd-bundle/icons.svg`, emits one
 * 128x128 transparent PNG per `<symbol id="icon-…">` into
 * `Assets/Sprites/Icons/` with a companion `.meta` file carrying Sprite
 * (2D and UI) importer settings (PPU=100, Bilinear filter, no mipmaps).
 *
 * Operator override (Stage 13.3): export whichever symbols exist in source
 * SVG. The canonical 27-id catalog lives in `CANONICAL_ICON_SLUGS`; if the
 * SVG is missing any of these, the tool warns with the missing list but
 * does NOT fail the run. Reserved-slug fallback path lives in
 * `ThemedIcon` (substitutes `icon-info` + dedup'd warning per slug).
 *
 * Usage:
 *   npm run icons:split
 *   npm run icons:split -- --source <svg> --out <dir>
 *
 * Schema fail / IO fail → stderr message + exit 1.
 *
 * @packageDocumentation
 */
import * as fs from 'node:fs';
import * as path from 'node:path';
import * as process from 'node:process';
import { fileURLToPath, pathToFileURL } from 'node:url';
import sharp from 'sharp';
import { createHash } from 'node:crypto';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '..', '..');

const DEFAULT_SOURCE = path.join(REPO_ROOT, 'web/design-refs/step-1-game-ui/cd-bundle/icons.svg');
const DEFAULT_OUT_DIR = path.join(REPO_ROOT, 'Assets/Sprites/Icons');
const RASTER_SIZE = 128;

/**
 * Canonical icon slug catalog (Stage 13.3 spec). The transcribe pipeline
 * targets all 27; designer SVG currently carries 22. Missing ids are
 * reserved as known slugs: at runtime, `ThemedIcon` substitutes the
 * `icon-info` sprite and emits a dedup'd warning per slug.
 */
export const CANONICAL_ICON_SLUGS: readonly string[] = [
  // 22 currently shipped in cd-bundle/icons.svg
  'icon-select',
  'icon-road',
  'icon-zone-residential',
  'icon-zone-commercial',
  'icon-zone-industrial',
  'icon-bulldoze',
  'icon-power',
  'icon-water',
  'icon-services',
  'icon-landmark',
  'icon-desirability',
  'icon-pollution',
  'icon-land-value',
  'icon-heat',
  'icon-pause',
  'icon-play',
  'icon-fast-forward',
  'icon-step',
  'icon-alert',
  'icon-info',
  'icon-success',
  'icon-autosave',
  // 5 reserved-slug hooks awaiting designer SVG handoff
  'icon-happiness',
  'icon-population',
  'icon-money',
  'icon-bond',
  'icon-envelope',
];

interface SymbolEntry {
  slug: string;
  inner: string; // children of <symbol>, sans wrapping element
  viewBox: string;
  attrs: string; // remaining attrs (fill, stroke, stroke-width…) hoisted to <svg>
}

/**
 * Extract every `<symbol id="icon-…">` block from the source SVG.
 *
 * Returns the inner XML (children only, no wrapping `<symbol>` tags) plus
 * the symbol's `viewBox` + outer presentation attrs (fill / stroke / etc.)
 * so we can rewrap as a standalone `<svg>` for sharp's rasterizer.
 */
export function extractSymbols(svgSrc: string): SymbolEntry[] {
  const out: SymbolEntry[] = [];
  const re = /<symbol\s+([^>]*?)>([\s\S]*?)<\/symbol>/g;
  let m: RegExpExecArray | null;
  while ((m = re.exec(svgSrc)) !== null) {
    const rawAttrs = m[1];
    const inner = m[2];
    const idMatch = rawAttrs.match(/\bid\s*=\s*["']([^"']+)["']/);
    if (!idMatch) continue;
    const slug = idMatch[1];
    if (!slug.startsWith('icon-')) continue;
    const viewBoxMatch = rawAttrs.match(/\bviewBox\s*=\s*["']([^"']+)["']/);
    const viewBox = viewBoxMatch ? viewBoxMatch[1] : '0 0 24 24';
    // strip id + viewBox; keep fill/stroke/stroke-width/etc. presentation attrs
    const otherAttrs = rawAttrs
      .replace(/\bid\s*=\s*["'][^"']+["']/, '')
      .replace(/\bviewBox\s*=\s*["'][^"']+["']/, '')
      .trim();
    out.push({ slug, inner, viewBox, attrs: otherAttrs });
  }
  return out;
}

/** Build a standalone `<svg>` wrapping a single symbol's body for rasterization. */
export function buildStandaloneSvg(entry: SymbolEntry): string {
  const attrs = entry.attrs.length > 0 ? ` ${entry.attrs}` : '';
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${RASTER_SIZE}" height="${RASTER_SIZE}" viewBox="${entry.viewBox}"${attrs}>${entry.inner}</svg>`;
}

/**
 * Deterministic 16-byte guid derived from slug. Unity meta needs hex guid;
 * derive from md5 so identical slug always lands on same guid (importer
 * stays sane across re-runs).
 */
function deriveGuid(slug: string): string {
  return createHash('md5').update(`territory-icon-${slug}`).digest('hex');
}

/** Sprite importer meta template, mirroring `Assets/Sprites/Icons/population-sta.png.meta`. */
export function buildSpriteMeta(guid: string): string {
  return `fileFormatVersion: 2
guid: ${guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 12
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 3
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 3
    buildTarget: WebGL
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: ${guid}
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
`;
}

export interface SplitOpts {
  sourcePath: string;
  outDir: string;
}

export interface SplitResult {
  emitted: string[];
  missing: string[];
  unknownInSource: string[];
}

/**
 * Run the split: read source SVG, rasterize each `<symbol id="icon-…">` to
 * `{outDir}/{slug}.png` + `{outDir}/{slug}.png.meta`. Returns the slugs we
 * emitted, plus the canonical-but-absent set (reserved-slug hooks).
 */
export async function runSplit(opts: SplitOpts): Promise<SplitResult> {
  if (!fs.existsSync(opts.sourcePath)) {
    throw new Error(`icons-svg-split: missing source SVG at ${opts.sourcePath}`);
  }
  const svgSrc = fs.readFileSync(opts.sourcePath, 'utf8');
  const symbols = extractSymbols(svgSrc);
  fs.mkdirSync(opts.outDir, { recursive: true });

  const emitted: string[] = [];
  const presentSlugs = new Set(symbols.map((s) => s.slug));
  const canonicalSet = new Set(CANONICAL_ICON_SLUGS);
  const missing = CANONICAL_ICON_SLUGS.filter((s) => !presentSlugs.has(s));
  const unknownInSource = [...presentSlugs].filter((s) => !canonicalSet.has(s));

  for (const sym of symbols) {
    const standalone = buildStandaloneSvg(sym);
    const pngPath = path.join(opts.outDir, `${sym.slug}.png`);
    const metaPath = `${pngPath}.meta`;
    await sharp(Buffer.from(standalone), { density: 384 })
      .resize(RASTER_SIZE, RASTER_SIZE, { fit: 'contain', background: { r: 0, g: 0, b: 0, alpha: 0 } })
      .png()
      .toFile(pngPath);
    if (!fs.existsSync(metaPath)) {
      const guid = deriveGuid(sym.slug);
      fs.writeFileSync(metaPath, buildSpriteMeta(guid), 'utf8');
    }
    emitted.push(sym.slug);
  }

  return { emitted, missing, unknownInSource };
}

// -- CLI ---------------------------------------------------------------------

function parseArgs(argv: string[]) {
  let sourcePath = DEFAULT_SOURCE;
  let outDir = DEFAULT_OUT_DIR;

  for (let i = 2; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--source' && argv[i + 1]) {
      sourcePath = path.resolve(REPO_ROOT, argv[++i]);
    } else if (a === '--out' && argv[i + 1]) {
      outDir = path.resolve(REPO_ROOT, argv[++i]);
    } else if (a === '--help' || a === '-h') {
      process.stdout.write(`Usage: npx tsx tools/scripts/icons-svg-split.ts [options]

  --source <svg>  Source SVG (default: web/design-refs/step-1-game-ui/cd-bundle/icons.svg)
  --out <dir>     Output dir (default: Assets/Sprites/Icons)

Each <symbol id="icon-..."> becomes a 128x128 transparent PNG with companion .meta.
Missing canonical ids reported on stderr (not fatal).
`);
      process.exit(0);
    }
  }
  return { sourcePath, outDir };
}

async function main() {
  const { sourcePath, outDir } = parseArgs(process.argv);
  let result: SplitResult;
  try {
    result = await runSplit({ sourcePath, outDir });
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    process.stderr.write(`icons-svg-split: ${msg}\n`);
    process.exit(1);
    return;
  }

  process.stdout.write(
    `icons-svg-split: emitted ${result.emitted.length}/${CANONICAL_ICON_SLUGS.length} → ${outDir}\n`,
  );
  if (result.missing.length > 0) {
    process.stderr.write(
      `icons-svg-split: WARNING ${result.missing.length} canonical id(s) absent from source SVG (reserved-slug hooks): ${result.missing.join(', ')}\n`,
    );
  }
  if (result.unknownInSource.length > 0) {
    process.stderr.write(
      `icons-svg-split: WARNING ${result.unknownInSource.length} symbol id(s) in source SVG not in canonical catalog: ${result.unknownInSource.join(', ')}\n`,
    );
  }
}

const entry = process.argv[1];
if (entry && import.meta.url === pathToFileURL(path.resolve(entry)).href) {
  main();
}
