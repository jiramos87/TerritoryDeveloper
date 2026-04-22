import assert from 'node:assert';
import * as fs from 'node:fs';
import * as os from 'node:os';
import * as path from 'node:path';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';
import {
  buildCanonicalMap,
  computeRawDrift,
  renderDriftReport,
} from '../extract-cd-tokens.ts';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '..', '..', '..');
const DEFAULT_CSS = path.join(
  REPO_ROOT,
  'web/design-refs/step-8-console/ds/colors_and_type.css',
);
const DEFAULT_CD_PALETTE = path.join(
  REPO_ROOT,
  'web/design-refs/step-8-console/ds/palette.json',
);
const LOCKED_PALETTE = path.join(REPO_ROOT, 'web/lib/tokens/palette.json');

test('canonical map includes CD raw palette keys', () => {
  const map = buildCanonicalMap({
    cssPath: DEFAULT_CSS,
    cdPaletteJsonPath: DEFAULT_CD_PALETTE,
  });
  assert.strictEqual(map.raws.black, '#0a0a0a');
  assert.strictEqual(map.raws.blue, '#4a7bc8');
  assert.ok(map.semantic['--bg-canvas']);
  assert.ok(map.motion['--dur-fast']);
  assert.ok(map.typeScale['--text-xs']);
  assert.ok(map.spacing['--sp-4']);
});

test('drift is clean for repo locked palette', () => {
  const map = buildCanonicalMap({
    cssPath: DEFAULT_CSS,
    cdPaletteJsonPath: DEFAULT_CD_PALETTE,
  });
  const rows = computeRawDrift(map, LOCKED_PALETTE);
  assert.ok(rows.every((r) => r.match), renderDriftReport(rows));
});

test('drift reports mismatch when locked palette skews', () => {
  const map = buildCanonicalMap({
    cssPath: DEFAULT_CSS,
    cdPaletteJsonPath: DEFAULT_CD_PALETTE,
  });
  const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'cd-drift-'));
  const fakePalette = path.join(tmp, 'palette.json');
  const real = JSON.parse(fs.readFileSync(LOCKED_PALETTE, 'utf8')) as { raw: Record<string, string> };
  real.raw.black = '#ffffff';
  fs.writeFileSync(fakePalette, JSON.stringify(real, null, 2), 'utf8');
  const rows = computeRawDrift(map, fakePalette);
  assert.ok(rows.some((r) => !r.match));
  const md = renderDriftReport(rows);
  assert.match(md, /\| No \|/);
});
