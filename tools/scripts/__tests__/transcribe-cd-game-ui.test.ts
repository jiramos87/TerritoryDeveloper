import assert from 'node:assert';
import { spawnSync } from 'node:child_process';
import * as fs from 'node:fs';
import * as os from 'node:os';
import * as path from 'node:path';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';
import { buildIrFromBundle, parseTokensCss } from '../transcribe-cd-game-ui.ts';
import { validateIrShape, validateSlotAccept } from '../ir-schema.ts';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '..', '..', '..');
const SCRIPT = path.join(REPO_ROOT, 'tools/scripts/transcribe-cd-game-ui.ts');
const HAPPY = path.join(__dirname, 'fixtures/cd-game-ui/happy');
const SLOT_VIOLATION = path.join(__dirname, 'fixtures/cd-game-ui/slot-violation');
const HAPPY_EXPECTED = path.join(HAPPY, 'expected-ir.json');

test('parseTokensCss extracts five token subblocks from happy fixture', () => {
  const css = fs.readFileSync(path.join(HAPPY, 'tokens.css'), 'utf8');
  const tokens = parseTokensCss(css);
  assert.strictEqual(tokens.palette.length, 2);
  assert.deepStrictEqual(
    tokens.palette.find((p) => p.slug === 'canvas')?.ramp,
    ['#0a0e1a', '#1a2540', '#2a3a5e'],
  );
  assert.strictEqual(tokens.frame_style[0].slug, 'panel');
  assert.strictEqual(tokens.frame_style[0].edge, 'single');
  assert.strictEqual(tokens.font_face[0].family, 'Inter');
  assert.strictEqual(tokens.font_face[0].weight, 600);
  const spring = tokens.motion_curve.find((m) => m.kind === 'spring');
  assert.strictEqual(spring?.stiffness, 220);
  assert.strictEqual(spring?.damping, 18);
  const bezier = tokens.motion_curve.find((m) => m.kind === 'cubic-bezier');
  assert.deepStrictEqual(bezier?.c1, [0.4, 0.0]);
  assert.strictEqual(bezier?.durationMs, 180);
  assert.strictEqual(tokens.illumination[0].color, '#4afc8a');
  assert.strictEqual(tokens.illumination[0].haloRadiusPx, 6);
});

test('buildIrFromBundle on happy fixture passes shape + slot guards', () => {
  const ir = buildIrFromBundle({ bundleDir: HAPPY });
  const shape = validateIrShape(ir);
  assert.strictEqual(shape.ok, true, JSON.stringify(shape));
  const accept = validateSlotAccept(ir);
  assert.strictEqual(accept.ok, true, JSON.stringify(accept));
});

test('happy fixture transcribes byte-equal to expected-ir.json', () => {
  const ir = buildIrFromBundle({ bundleDir: HAPPY });
  const got = JSON.stringify(ir, null, 2) + '\n';
  const expected = fs.readFileSync(HAPPY_EXPECTED, 'utf8');
  assert.strictEqual(got, expected);
});

test('slot-violation fixture rejects via validateSlotAccept', () => {
  const ir = buildIrFromBundle({ bundleDir: SLOT_VIOLATION });
  const accept = validateSlotAccept(ir);
  assert.strictEqual(accept.ok, false);
  if (!accept.ok) {
    assert.strictEqual(accept.error, 'slot_accept_violation');
    assert.strictEqual(accept.panel, 'toolbar');
    assert.strictEqual(accept.slot, 'controls');
    assert.deepStrictEqual(accept.offending_children, ['speed-fader']);
  }
});

test('CLI exits 0 + writes ir.json for happy fixture', () => {
  const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'transcribe-happy-'));
  const out = path.join(tmp, 'ir.json');
  const r = spawnSync('npx', ['tsx', SCRIPT, '--in', HAPPY, '--out', out], {
    encoding: 'utf8',
  });
  assert.strictEqual(r.status, 0, `stderr: ${r.stderr}\nstdout: ${r.stdout}`);
  assert.ok(fs.existsSync(out));
  const written = fs.readFileSync(out, 'utf8');
  const expected = fs.readFileSync(HAPPY_EXPECTED, 'utf8');
  assert.strictEqual(written, expected);
});

test('CLI exits non-zero on slot-violation fixture with descriptive error', () => {
  const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'transcribe-violation-'));
  const out = path.join(tmp, 'ir.json');
  const r = spawnSync('npx', ['tsx', SCRIPT, '--in', SLOT_VIOLATION, '--out', out], {
    encoding: 'utf8',
  });
  assert.notStrictEqual(r.status, 0);
  assert.match(r.stderr, /slot_accept_violation/);
  assert.match(r.stderr, /toolbar/);
  assert.match(r.stderr, /controls/);
  assert.match(r.stderr, /speed-fader/);
  assert.strictEqual(fs.existsSync(out), false, 'should not write IR on schema fail');
});
