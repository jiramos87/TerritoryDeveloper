#!/usr/bin/env node
/**
 * build-asset-manifest.mjs
 * Stage 1.0 tracer — Pass 0 manifest + preflight builder.
 *
 * Usage:
 *   node build-asset-manifest.mjs [--family=residential] [--dry-run]
 *
 * Outputs: tools/scripts/asset-tree-reorg/manifest.csv
 * Columns: current_path,target_path,current_name,target_name,family,reason,meta_guid
 *
 * Preflight steps (run before manifest build):
 *   1. Orphan .meta scan — every asset has sibling .meta and vice versa (TECH-16995)
 *   2. Resources.Load audit — zero in-scope Sprites/* / Prefabs/* string hits (TECH-16996)
 *   3. Slug-string audit — old filename stems in ia/ + BACKLOG.md (TECH-16997)
 */

import { readFileSync, existsSync, readdirSync, statSync, writeFileSync, mkdirSync } from 'fs';
import { join, dirname, basename, extname, relative } from 'path';
import { execSync } from 'child_process';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

// ── Config ────────────────────────────────────────────────────────────────────

const REPO_ROOT = join(__dirname, '..', '..');
const SPRITES_ROOT = join(REPO_ROOT, 'Assets', 'Sprites');
const PREFABS_ROOT = join(REPO_ROOT, 'Assets', 'Prefabs');
const OUT_DIR = join(__dirname, 'asset-tree-reorg');
const OUT_CSV = join(OUT_DIR, 'manifest.csv');
const PASS_25_CSV = join(OUT_DIR, 'pass-2-5-slug-hits.csv');

// 10-family flat taxonomy
const FAMILY_MAP = {
  residential: ['residential', 'house', 'light-residential', 'medium-residential', 'heavy-residential',
    'Residential', 'House', 'LightResidential', 'MediumResidential', 'HeavyResidential'],
  commercial: ['commercial', 'Commercial', 'shop', 'Shop'],
  industrial: ['industrial', 'Industrial'],
  power: ['power', 'Power', 'PowerPlant', 'powerplant', 'nuclear'],
  water: ['water', 'Water', 'WaterPlant', 'waterplant'],
  roads: ['road', 'Road', 'Roads'],
  forest: ['forest', 'Forest', 'tree', 'Tree'],
  terrain: ['grass', 'Grass', 'cliff', 'Cliff', 'slope', 'Slope', 'terrain', 'Terrain',
    'Slopes', 'State', 'diamond-tile', 'clifftemp'],
  ui: ['button', 'Button', 'icon', 'Icon', 'cursor', 'Cursor', 'Buttons', 'Icons',
    'bulldoze', 'Bulldoze', 'Bulldozer', 'arrow', 'Arrow', 'hud', 'HUD',
    'zoom', 'Zoom', 'sign', 'Sign'],
  fx: ['fx', 'FX', 'effect', 'Effect', 'Effects', 'explosion', 'Explosion',
    'animation', 'Animation', 'anim', 'Anim', 'sprite-sheet', 'SpriteSheet',
    'Demolition', 'demolition'],
};

// Folder-name → family (override heuristic for ambiguous names)
const FOLDER_FAMILY_OVERRIDE = {
  Residential: 'residential',
  Grass: 'terrain',
  Cliff: 'terrain',
  Slopes: 'terrain',
  Forest: 'forest',
  Roads: 'roads',
  PowerPlant: 'power',
  WaterPlant: 'water',
  Effects: 'fx',
  Buttons: 'ui',
  Icons: 'ui',
  State: 'terrain',
  Generated: null, // skip Generated/
};

// ── CLI args ───────────────────────────────────────────────────────────────────

const args = process.argv.slice(2);
const dryRun = args.includes('--dry-run');
const familyFilter = args.find(a => a.startsWith('--family='))?.split('=')[1] ?? null;

// ── Helpers ───────────────────────────────────────────────────────────────────

/**
 * Recursively walk a directory, returning all files (not dirs).
 */
function walkDir(dir) {
  if (!existsSync(dir)) return [];
  const entries = readdirSync(dir, { withFileTypes: true });
  const files = [];
  for (const entry of entries) {
    const full = join(dir, entry.name);
    if (entry.isDirectory()) {
      files.push(...walkDir(full));
    } else {
      files.push(full);
    }
  }
  return files;
}

/**
 * Read meta_guid from a Unity .meta file. Returns '' if not found.
 */
function readMetaGuid(metaPath) {
  if (!existsSync(metaPath)) return '';
  const content = readFileSync(metaPath, 'utf-8');
  const match = content.match(/^guid:\s*([a-f0-9]+)/m);
  return match ? match[1] : '';
}

/**
 * Infer family from folder path + filename.
 * Returns family string or 'unknown'.
 */
function inferFamily(filePath) {
  const rel = relative(REPO_ROOT, filePath);
  const parts = rel.split('/');

  // Folder override check (e.g. Assets/Sprites/Residential/...)
  for (let i = 0; i < parts.length; i++) {
    const seg = parts[i];
    if (seg in FOLDER_FAMILY_OVERRIDE) {
      const fam = FOLDER_FAMILY_OVERRIDE[seg];
      if (fam === null) return null; // skip (Generated, etc.)
      return fam;
    }
  }

  // Filename heuristic
  const name = basename(filePath);
  for (const [family, tokens] of Object.entries(FAMILY_MAP)) {
    for (const token of tokens) {
      if (name.includes(token)) return family;
    }
  }
  return 'unknown';
}

/**
 * Compute kebab-case target name from current name (planning only — actual rename is Pass 2).
 * Current pass (Pass 0) target_path = same location (no moves yet; Pass 1 moves folders).
 */
function toKebabCase(name) {
  // Already kebab-case if all lowercase with hyphens
  // Convert PascalCase / underscores → kebab-case
  return name
    .replace(/_/g, '-')
    .replace(/([A-Z]+)([A-Z][a-z])/g, '$1-$2')
    .replace(/([a-z\d])([A-Z])/g, '$1-$2')
    .toLowerCase();
}

/**
 * Derive target path given family inference.
 * Stage 1.0: target_path = canonical family subfolder under same root (Sprites or Prefabs),
 * file name unchanged. Full rename happens in Pass 2.
 */
function deriveTargetPath(currentPath, family) {
  const rel = relative(REPO_ROOT, currentPath);
  const isSprite = rel.startsWith('Assets/Sprites/');
  const isPrefab = rel.startsWith('Assets/Prefabs/');
  const name = basename(currentPath);

  if (!isSprite && !isPrefab) return currentPath;

  const root = isSprite ? 'Assets/Sprites' : 'Assets/Prefabs';
  const familyFolder = family.charAt(0).toUpperCase() + family.slice(1);
  return join(REPO_ROOT, root, familyFolder, name);
}

// ── TECH-16995: Orphan .meta scan ─────────────────────────────────────────────

// Extensions that Unity tracks with .meta files
const UNITY_TRACKED_EXTENSIONS = new Set([
  '.png', '.jpg', '.jpeg', '.tga', '.psd', '.gif', '.bmp', '.tiff',
  '.prefab', '.unity', '.mat', '.anim', '.controller', '.asset', '.shader',
  '.cs', '.js', '.ttf', '.otf', '.wav', '.mp3', '.ogg', '.fbx', '.obj',
  '.svg', '.renderTexture', '.flare', '.guiskin', '.fontsettings', '.asmdef',
  '.spriteatlas', '.physicsMaterial2D', '.playable',
]);

function runOrphanMetaScan(allFiles) {
  const orphans = [];

  // Only check files with Unity-tracked extensions (skip .gitkeep, .DS_Store, etc.)
  const assetFiles = new Set(
    allFiles.filter(f => {
      const ext = extname(f);
      return !f.endsWith('.meta') && UNITY_TRACKED_EXTENSIONS.has(ext);
    })
  );
  const metaFiles = allFiles.filter(f => f.endsWith('.meta'));

  // Every tracked asset must have sibling .meta
  for (const asset of assetFiles) {
    const expectedMeta = asset + '.meta';
    if (!existsSync(expectedMeta)) {
      orphans.push({ path: asset, reason: 'missing-meta' });
    }
  }

  // Every .meta must have sibling tracked asset (ignore .meta for dirs + untracked files)
  for (const meta of metaFiles) {
    const expectedAsset = meta.replace(/\.meta$/, '');
    const ext = extname(expectedAsset);
    if (!UNITY_TRACKED_EXTENSIONS.has(ext)) continue; // skip dir .meta or untracked-ext .meta
    if (!existsSync(expectedAsset)) {
      orphans.push({ path: meta, reason: 'orphan-meta-no-asset' });
    }
  }

  return orphans;
}

// ── TECH-16996: Resources.Load audit ─────────────────────────────────────────

function runResourcesLoadAudit() {
  let output;
  try {
    output = execSync(
      `git -C "${REPO_ROOT}" grep -n "Resources.Load" -- "Assets/Scripts/**/*.cs" 2>/dev/null || true`,
      { encoding: 'utf-8' }
    );
  } catch {
    output = '';
  }

  const hits = [];
  const lines = output.split('\n').filter(Boolean);
  for (const line of lines) {
    // Only flag hits referencing Sprites/* or Prefabs/*
    if (/Resources\.Load.*["'](Sprites|Prefabs)/.test(line)) {
      hits.push(line);
    }
  }
  return hits;
}

// ── TECH-16997: Slug-string audit ─────────────────────────────────────────────

function runSlugStringAudit(manifestRows) {
  // Build set of old filename stems (without extension)
  const stems = new Set(
    manifestRows.map(r => basename(r.current_name, extname(r.current_name)))
  );

  const hits = [];
  const scanPaths = ['ia/', 'BACKLOG.md'];

  for (const stem of stems) {
    if (stem.length < 4) continue; // skip trivially short stems
    for (const scanPath of scanPaths) {
      let output;
      try {
        output = execSync(
          `git -C "${REPO_ROOT}" grep -rn "${stem}" -- "${scanPath}" 2>/dev/null || true`,
          { encoding: 'utf-8' }
        );
      } catch {
        output = '';
      }
      const lines = output.split('\n').filter(Boolean);
      for (const line of lines) {
        hits.push({ stem, location: line });
      }
    }
  }
  return hits;
}

// ── Main ───────────────────────────────────────────────────────────────────────

console.log('build-asset-manifest.mjs — Pass 0 preflight + manifest builder');
console.log(`  dryRun=${dryRun}  familyFilter=${familyFilter ?? 'all'}`);
console.log('');

// Collect all files
const allSprites = walkDir(SPRITES_ROOT);
const allPrefabs = walkDir(PREFABS_ROOT);
const allFiles = [...allSprites, ...allPrefabs];

// ── Step 1: Orphan .meta scan ─────────────────────────────────────────────────
console.log('[1/4] Orphan .meta scan...');
const orphans = runOrphanMetaScan(allFiles);
if (orphans.length > 0) {
  console.error('ABORT — orphan .meta detected:');
  for (const o of orphans) {
    console.error(`  ${o.reason}: ${relative(REPO_ROOT, o.path)}`);
  }
  process.exit(1);
}
console.log('  OK — zero orphan .meta files');

// ── Step 2: Resources.Load audit ──────────────────────────────────────────────
console.log('[2/4] Resources.Load audit...');
const resourcesHits = runResourcesLoadAudit();
if (resourcesHits.length > 0) {
  console.error('ABORT — Resources.Load hits referencing in-scope paths:');
  for (const h of resourcesHits) {
    console.error(`  ${h}`);
  }
  process.exit(1);
}
console.log('  OK — zero Resources.Load hits for Sprites/* / Prefabs/*');

// ── Step 3: Build manifest rows ───────────────────────────────────────────────
console.log('[3/4] Building manifest rows...');

const ASSET_EXTENSIONS = new Set(['.png', '.prefab', '.anim', '.controller']);
const manifestRows = [];

for (const filePath of allFiles) {
  const ext = extname(filePath);
  if (!ASSET_EXTENSIONS.has(ext)) continue;

  const family = inferFamily(filePath);
  if (family === null) continue; // skip Generated/

  if (familyFilter && family !== familyFilter) continue;

  const currentName = basename(filePath);
  const targetName = toKebabCase(currentName);
  const targetPath = deriveTargetPath(filePath, family === 'unknown' ? 'unknown' : family);
  const metaPath = filePath + '.meta';
  const metaGuid = readMetaGuid(metaPath);

  const currentPathRel = relative(REPO_ROOT, filePath);
  const targetPathRel = relative(REPO_ROOT, targetPath);
  const reason = family === 'unknown' ? 'needs-manual-review' : 'family-inferred';

  manifestRows.push({
    current_path: currentPathRel,
    target_path: targetPathRel,
    current_name: currentName,
    target_name: targetName,
    family,
    reason,
    meta_guid: metaGuid,
  });
}

console.log(`  ${manifestRows.length} rows built (family=${familyFilter ?? 'all'})`);

// ── Step 4: Slug-string audit ─────────────────────────────────────────────────
console.log('[4/4] Slug-string audit (ia/ + BACKLOG.md)...');
const slugHits = runSlugStringAudit(manifestRows);
let pass25Required = false;
if (slugHits.length > 0) {
  console.warn(`  WARN — ${slugHits.length} slug-string hit(s) found → pass_2_5_required=true`);
  pass25Required = true;
} else {
  console.log('  OK — zero slug-string hits');
}

// ── Write CSV ─────────────────────────────────────────────────────────────────
const CSV_HEADER = 'current_path,target_path,current_name,target_name,family,reason,meta_guid';
const csvLines = [
  CSV_HEADER,
  ...manifestRows.map(r =>
    [r.current_path, r.target_path, r.current_name, r.target_name, r.family, r.reason, r.meta_guid]
      .map(v => `"${String(v).replace(/"/g, '""')}"`)
      .join(',')
  ),
];

if (!dryRun) {
  mkdirSync(OUT_DIR, { recursive: true });
  writeFileSync(OUT_CSV, csvLines.join('\n') + '\n', 'utf-8');
  console.log(`\nManifest written: ${relative(REPO_ROOT, OUT_CSV)} (${manifestRows.length} rows)`);

  if (pass25Required) {
    const hitLines = [
      'stem,location',
      ...slugHits.map(h => `"${h.stem}","${h.location.replace(/"/g, '""')}"`),
    ];
    writeFileSync(PASS_25_CSV, hitLines.join('\n') + '\n', 'utf-8');
    console.log(`Pass 2.5 hit list: ${relative(REPO_ROOT, PASS_25_CSV)} (${slugHits.length} hits)`);
  }
} else {
  console.log('\n[dry-run] No files written.');
  console.log('CSV preview (first 5 rows):');
  csvLines.slice(0, 6).forEach(l => console.log(' ', l));
}

if (pass25Required) {
  console.log('\npass_2_5_required=true — review pass-2-5-slug-hits.csv before Pass 1.');
  process.exitCode = 0; // not an abort — escalation only
}

console.log('\nDone. Preflight green.');
