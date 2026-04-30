import assert from 'node:assert';
import * as path from 'node:path';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';
import { extractLayoutRects } from '../extract-cd-layout-rects.ts';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '..', '..', '..');
const REAL_BUNDLE_DIR = path.join(REPO_ROOT, 'web/design-refs/step-1-game-ui/cd-bundle');

test('extractLayoutRects renders cd-bundle and emits viewport + parent-relative rects', { timeout: 60000 }, async () => {
  const artifact = await extractLayoutRects({
    bundleDir: REAL_BUNDLE_DIR,
    outPath: '/dev/null',
    htmlFilename: 'Studio Rack Game UI.html',
  });
  assert.strictEqual(artifact.schema_version, 1);
  assert.strictEqual(artifact.viewport.width, 1920);
  assert.strictEqual(artifact.viewport.height, 1080);
  assert.ok(artifact.node_count > 50, `expected > 50 nodes, got ${artifact.node_count}`);

  // Every node has a viewport_rect with non-negative width/height.
  for (const n of artifact.nodes) {
    assert.ok(n.viewport_rect.width >= 0, `${n.dom_path} width >= 0`);
    assert.ok(n.viewport_rect.height >= 0, `${n.dom_path} height >= 0`);
    assert.ok(n.parent_relative_rect.width >= 0);
  }

  const knob = artifact.nodes.find((n) => n.node_kind === 'interactive' && n.cd_slug === 'knob');
  assert.ok(knob, 'expected at least one knob interactive');
  assert.ok(knob!.viewport_rect.width > 0, 'knob has non-zero width');
  assert.ok(knob!.viewport_rect.height > 0, 'knob has non-zero height');
  // Round knob → aspect ratio ≈ 1 (allow generous tolerance for sub-pixel rounding).
  assert.ok(
    Math.abs(knob!.aspect_ratio - 1) < 0.05,
    `knob aspect_ratio ≈ 1, got ${knob!.aspect_ratio}`,
  );

  // At least one node has a non-null parent_cd_slug + parent_kind.
  const child = artifact.nodes.find((n) => n.parent_kind !== null);
  assert.ok(child, 'expected at least one node with a classified parent');
});
