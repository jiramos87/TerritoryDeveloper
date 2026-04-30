import assert from 'node:assert';
import * as path from 'node:path';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';
import {
  bucketModifiers,
  buildInteractions,
  parseArchetypeFile,
  scanCssForRoot,
} from '../extract-cd-interactions.ts';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '..', '..', '..');
const FIXTURE_DIR = path.join(__dirname, 'fixtures', 'cd-interactions');
const REAL_BUNDLE_DIR = path.join(REPO_ROOT, 'web/design-refs/step-1-game-ui/cd-bundle');

test('parseArchetypeFile recovers component name + defaults + class root', () => {
  const defs = parseArchetypeFile(path.join(FIXTURE_DIR, 'archetypes.jsx'));
  const knob = defs.find((d) => d.component_name === 'Knob');
  assert.ok(knob, 'expected Knob component');
  assert.strictEqual(knob!.class_root, 'knob');
  assert.strictEqual(knob!.default_props.size, 'md');
  assert.strictEqual(knob!.default_props.tone, 'primary');
  assert.strictEqual(knob!.default_props.rotation, 0);
  assert.strictEqual(knob!.default_props.state, null); // no default
  const iled = defs.find((d) => d.component_name === 'ILed');
  assert.ok(iled, 'expected ILed component');
  assert.strictEqual(iled!.class_root, 'iled');
  assert.strictEqual(iled!.default_props.lit, false);
});

test('scanCssForRoot returns only matching root selectors with line numbers', () => {
  const css = path.join(FIXTURE_DIR, 'tokens.css');
  const knobHits = scanCssForRoot(css, 'knob');
  // base `.knob` (no `--` modifier) is filtered by the BEM pattern; expect
  // size + state + tone modifier hits only.
  const selectors = knobHits.map((h) => h.selector).sort();
  assert.ok(selectors.includes('.knob--sm'));
  assert.ok(selectors.includes('.knob--md'));
  assert.ok(selectors.includes('.knob--hover'));
  assert.ok(selectors.includes('.knob--tone-primary'));
  // No iled rows when scanning for `knob`.
  assert.ok(!selectors.some((s) => s.startsWith('.iled')));
  for (const h of knobHits) {
    assert.ok(h.line > 0, `expected line > 0 for ${h.selector}`);
    assert.ok(h.source_file.endsWith('tokens.css'));
  }
});

test('bucketModifiers groups suffixes by axis', () => {
  const css = path.join(FIXTURE_DIR, 'tokens.css');
  const hits = scanCssForRoot(css, 'knob');
  const axes = bucketModifiers(hits, 'knob');
  assert.deepStrictEqual(axes.size.sort(), ['.knob--lg', '.knob--md', '.knob--sm']);
  assert.deepStrictEqual(
    axes.state.sort(),
    ['.knob--disabled', '.knob--focus', '.knob--hover', '.knob--pressed'],
  );
  assert.deepStrictEqual(
    axes.tone.sort(),
    ['.knob--tone-alert', '.knob--tone-neutral', '.knob--tone-primary'],
  );
  assert.deepStrictEqual(axes.orientation, []);
  assert.deepStrictEqual(axes.lit, []);
});

test('buildInteractions against real cd-bundle covers all 8 interactives', () => {
  const artifact = buildInteractions({
    bundleDir: REAL_BUNDLE_DIR,
    outPath: '/dev/null',
  });
  assert.strictEqual(artifact.schema_version, 1);
  assert.strictEqual(artifact.interactive_count, 8);
  const slugs = artifact.interactives.map((i) => i.slug).sort();
  assert.deepStrictEqual(slugs, [
    'detent-ring',
    'fader',
    'illuminated-button',
    'knob',
    'led',
    'oscilloscope',
    'segmented-readout',
    'vu-meter',
  ]);
  const knob = artifact.interactives.find((i) => i.slug === 'knob');
  assert.ok(knob);
  assert.strictEqual(knob!.class_root, 'knob');
  assert.strictEqual(knob!.component_name, 'Knob');
  assert.ok(knob!.modifiers.size.length >= 3, 'expected 3+ size modifiers');
  assert.ok(knob!.modifiers.state.length >= 4, 'expected 4+ state modifiers');
  assert.ok(knob!.modifiers.tone.length >= 3, 'expected 3+ tone modifiers');
  assert.ok(knob!.selectors.length > 0, 'expected non-empty selector hits');
});

test('buildInteractions surfaces oscilloscope class root + extension CSS hits', () => {
  const artifact = buildInteractions({
    bundleDir: REAL_BUNDLE_DIR,
    outPath: '/dev/null',
  });
  const osc = artifact.interactives.find((i) => i.slug === 'oscilloscope');
  assert.ok(osc, 'expected oscilloscope entry');
  assert.strictEqual(osc!.class_root, 'osc');
  assert.strictEqual(osc!.component_name, 'Osc');
  assert.ok(
    osc!.selectors.some((s) => s.source_file.endsWith('archetypes-extension.css')),
    'expected at least one selector hit from archetypes-extension.css',
  );
});
