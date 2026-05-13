/**
 * Stage 6.0 — Purge prep: quarantine legacy reactive + token + themed primitives
 * Anchor: design-only:Assets/Scripts/UI/UiBindRegistry.cs::ObsoleteAttribute
 * Red-stage proof: tests/ui-toolkit-migration/stage6-purge-prep.test.mjs::legacyQuarantineGreen
 */
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

const ROOT = join(import.meta.dirname, '../..');

function readCs(relPath) {
  const abs = join(ROOT, relPath);
  assert.ok(existsSync(abs), `${relPath} not found`);
  return readFileSync(abs, 'utf-8');
}

// ── TECH-32927: UiBindRegistry quarantine ────────────────────────────────────
describe('TECH-32927 — UiBindRegistry quarantine', () => {
  it('[Obsolete] present on class', () => {
    const src = readCs('Assets/Scripts/UI/Registry/UiBindRegistry.cs');
    assert.ok(
      src.includes('[Obsolete(') || src.includes('[System.Obsolete('),
      'UiBindRegistry missing [Obsolete] attribute'
    );
    assert.ok(
      src.includes('TECH-32927'),
      'UiBindRegistry [Obsolete] missing TECH-32927 reference'
    );
  });

  it('[Obsolete] present on public members', () => {
    const src = readCs('Assets/Scripts/UI/Registry/UiBindRegistry.cs');
    // Must have at least 3 member-level Obsolete annotations
    const memberObsolete = (src.match(/\[Obsolete\("UiBindRegistry\./g) || []).length;
    assert.ok(memberObsolete >= 3, `Expected ≥3 member [Obsolete] annotations, got ${memberObsolete}`);
  });
});

// ── TECH-32928: UiTheme quarantine ───────────────────────────────────────────
describe('TECH-32928 — UiTheme quarantine', () => {
  it('[Obsolete] present on UiTheme class', () => {
    const src = readCs('Assets/Scripts/Managers/GameManagers/UiTheme.cs');
    assert.ok(
      src.includes('[Obsolete(') || src.includes('[System.Obsolete('),
      'UiTheme missing [Obsolete] attribute'
    );
    assert.ok(
      src.includes('TECH-32928'),
      'UiTheme [Obsolete] missing TECH-32928 reference'
    );
  });
});

// ── TECH-32929: ThemedPrimitiveBase ring quarantine ──────────────────────────
describe('TECH-32929 — Themed primitive ring quarantine', () => {
  const PRIMITIVES = [
    'Assets/Scripts/UI/Themed/ThemedPrimitiveBase.cs',
    'Assets/Scripts/UI/Themed/IThemed.cs',
    'Assets/Scripts/UI/Themed/ThemedLabel.cs',
    'Assets/Scripts/UI/Themed/ThemedDivider.cs',
    'Assets/Scripts/UI/Themed/ThemedBadge.cs',
    'Assets/Scripts/UI/Themed/ThemedIcon.cs',
    'Assets/Scripts/UI/Themed/ThemedButton.cs',
    'Assets/Scripts/UI/Themed/ThemedFrame.cs',
    'Assets/Scripts/UI/Themed/ThemedList.cs',
    'Assets/Scripts/UI/Themed/ThemedPanel.cs',
    'Assets/Scripts/UI/Themed/ThemedToggle.cs',
    'Assets/Scripts/UI/Themed/ThemedSlider.cs',
    'Assets/Scripts/UI/Themed/ThemedTooltip.cs',
    'Assets/Scripts/UI/Themed/ThemedSectionHeader.cs',
    'Assets/Scripts/UI/Themed/ThemedIlluminationLayer.cs',
    'Assets/Scripts/UI/Themed/ThemedOverlayToggleRow.cs',
    'Assets/Scripts/UI/Themed/ThemedTabCell.cs',
    'Assets/Scripts/UI/Themed/Renderers/ThemedPrimitiveRendererBase.cs',
    'Assets/Scripts/UI/Themed/Renderers/ThemedListRenderer.cs',
    'Assets/Scripts/UI/Themed/Renderers/ThemedTabBarRenderer.cs',
    'Assets/Scripts/UI/Themed/Renderers/ThemedToggleRenderer.cs',
    'Assets/Scripts/UI/Themed/Renderers/ThemedSliderRenderer.cs',
    'Assets/Scripts/UI/Themed/Renderers/ThemedTooltipRenderer.cs',
    'Assets/Scripts/UI/Themed/Renderers/ThemedOverlayToggleRowRenderer.cs',
  ];

  for (const relPath of PRIMITIVES) {
    it(`[Obsolete] present in ${relPath.split('/').pop()}`, () => {
      const src = readCs(relPath);
      assert.ok(
        src.includes('[Obsolete(') || src.includes('[System.Obsolete('),
        `${relPath} missing [Obsolete] attribute`
      );
    });
  }

  it('ThemedTabBar already had [Obsolete] from Stage 4.0', () => {
    const src = readCs('Assets/Scripts/UI/Themed/ThemedTabBar.cs');
    assert.ok(
      src.includes('[Obsolete(') || src.includes('[System.Obsolete('),
      'ThemedTabBar missing [Obsolete] attribute'
    );
  });

  it('glossary updated with retired primitive terms', () => {
    const glossary = readFileSync(join(ROOT, 'ia/specs/glossary.md'), 'utf-8');
    assert.ok(
      glossary.includes('TECH-32927') && glossary.includes('TECH-32928') && glossary.includes('TECH-32929'),
      'glossary missing Stage 6.0 quarantine entries'
    );
    assert.ok(
      glossary.includes('UiBindRegistry') && glossary.includes('UiTheme') && glossary.includes('ThemedPrimitiveBase'),
      'glossary missing quarantined term rows'
    );
  });
});

// ── TECH-32930: validate-no-legacy-ugui-refs.mjs exists ─────────────────────
describe('TECH-32930 — validate-no-legacy-ugui-refs validator', () => {
  it('script file exists', () => {
    const p = join(ROOT, 'tools/scripts/validate-no-legacy-ugui-refs.mjs');
    assert.ok(existsSync(p), 'validate-no-legacy-ugui-refs.mjs missing');
  });

  it('wired into package.json', () => {
    const pkg = readFileSync(join(ROOT, 'package.json'), 'utf-8');
    assert.ok(
      pkg.includes('"validate:no-legacy-ugui-refs"'),
      'validate:no-legacy-ugui-refs not in package.json scripts'
    );
  });

  it('wired into validate:all:readonly run-p chain', () => {
    const pkg = readFileSync(join(ROOT, 'package.json'), 'utf-8');
    const readonly = pkg.match(/"validate:all:readonly":\s*"([^"]+)"/)?.[1] ?? '';
    assert.ok(
      readonly.includes('validate:no-legacy-ugui-refs'),
      'validate:no-legacy-ugui-refs not in validate:all:readonly chain'
    );
  });
});

// ── TECH-32931: uGUI deletion sweep exploration seed ─────────────────────────
describe('TECH-32931 — uGUI deletion sweep seed doc', () => {
  it('exploration seed doc exists', () => {
    const p = join(ROOT, 'docs/explorations/ugui-deletion-sweep.md');
    assert.ok(existsSync(p), 'docs/explorations/ugui-deletion-sweep.md missing');
  });

  it('seed doc references zero-gate + validate:no-legacy-ugui-refs', () => {
    const src = readFileSync(join(ROOT, 'docs/explorations/ugui-deletion-sweep.md'), 'utf-8');
    assert.ok(
      src.includes('validate:no-legacy-ugui-refs'),
      'ugui-deletion-sweep.md missing zero-gate reference'
    );
    assert.ok(
      src.includes('TECH-32931'),
      'ugui-deletion-sweep.md missing TECH-32931 reference'
    );
  });
});

// ── Stage-wide green criteria ─────────────────────────────────────────────────
describe('legacyQuarantineGreen — stage-wide criteria', () => {
  it('UiBindRegistry, UiTheme, ThemedPrimitive all quarantined via [Obsolete]', () => {
    const registrySrc = readCs('Assets/Scripts/UI/Registry/UiBindRegistry.cs');
    const themeSrc = readCs('Assets/Scripts/Managers/GameManagers/UiTheme.cs');
    const baseSrc = readCs('Assets/Scripts/UI/Themed/ThemedPrimitiveBase.cs');

    assert.ok(registrySrc.includes('[Obsolete('), 'UiBindRegistry not quarantined');
    assert.ok(themeSrc.includes('[Obsolete('), 'UiTheme not quarantined');
    assert.ok(baseSrc.includes('[Obsolete('), 'ThemedPrimitiveBase not quarantined');
  });

  it('validator script + package.json wiring green', () => {
    const pkg = readFileSync(join(ROOT, 'package.json'), 'utf-8');
    assert.ok(pkg.includes('validate:no-legacy-ugui-refs'), 'validator not wired');
  });

  it('followup plan exploration seed filed', () => {
    assert.ok(
      existsSync(join(ROOT, 'docs/explorations/ugui-deletion-sweep.md')),
      'ugui-deletion-sweep.md missing'
    );
  });
});
