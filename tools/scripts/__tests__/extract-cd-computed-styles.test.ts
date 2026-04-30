import assert from 'node:assert';
import * as path from 'node:path';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';
import { extractComputedStyles } from '../extract-cd-computed-styles.ts';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '..', '..', '..');
const REAL_BUNDLE_DIR = path.join(REPO_ROOT, 'web/design-refs/step-1-game-ui/cd-bundle');

// Playwright + chromium boot is heavy — single end-to-end run validates the
// whole serve → render → walk → snapshot pipeline. Per-axis assertions live in
// extract-cd-interactions.test.ts and the IR transcribe tests.
test('extractComputedStyles renders cd-bundle and snapshots panels + slots + interactives', { timeout: 60000 }, async () => {
  const artifact = await extractComputedStyles({
    bundleDir: REAL_BUNDLE_DIR,
    outPath: '/dev/null',
    htmlFilename: 'Studio Rack Game UI.html',
  });
  assert.strictEqual(artifact.schema_version, 1);
  assert.strictEqual(artifact.viewport.width, 1920);
  assert.strictEqual(artifact.viewport.height, 1080);
  assert.ok(artifact.node_count > 50, `expected > 50 nodes, got ${artifact.node_count}`);

  const kinds = new Set(artifact.nodes.map((n) => n.node_kind));
  assert.ok(kinds.has('panel'), 'expected at least one panel node');
  assert.ok(kinds.has('slot'), 'expected at least one slot node');
  assert.ok(kinds.has('interactive'), 'expected at least one interactive node');

  const panel = artifact.nodes.find((n) => n.node_kind === 'panel' && n.cd_slug !== null);
  assert.ok(panel, 'expected a panel node with cd_slug');
  assert.match(panel!.computed.fontFamily, /\S+/);

  const knob = artifact.nodes.find((n) => n.node_kind === 'interactive' && n.cd_slug === 'knob');
  assert.ok(knob, 'expected at least one knob interactive');
  assert.match(knob!.computed.borderRadius, /\d/);
});
