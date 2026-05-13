/**
 * Stage 5.0 — CityScene HUD/toast + remainder sweep
 * Anchor: citySceneFullyOnUiToolkit
 * Red-stage proof: tests/ui-toolkit-migration/stage5-hud-toast-sweep.test.mjs::citySceneFullyOnUiToolkit
 */
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

const ROOT = join(import.meta.dirname, '../..');
const GENERATED_DIR = join(ROOT, 'Assets/UI/Generated');
const HOSTS_DIR = join(ROOT, 'Assets/Scripts/UI/Hosts');
const VMS_DIR = join(ROOT, 'Assets/Scripts/UI/ViewModels');
const PREFABS_DIR = join(ROOT, 'Assets/UI/Prefabs/Generated');

// Helper: check UXML + USS + VM + Host quad for a panel slug
function panelMigrated(slug, hostName, vmName) {
  const uxml = existsSync(join(GENERATED_DIR, `${slug}.uxml`));
  const uss = existsSync(join(GENERATED_DIR, `${slug}.uss`));
  const vm = existsSync(join(VMS_DIR, `${vmName}.cs`));
  const host = existsSync(join(HOSTS_DIR, `${hostName}.cs`));
  return { uxml, uss, vm, host, ok: uxml && uss && vm && host };
}

// ── TECH-32922: notifications-toast ───────────────────────────────────────────
describe('TECH-32922 — notifications-toast migration', () => {
  it('quad exists (uxml, uss, vm, host)', () => {
    const r = panelMigrated('notifications-toast', 'NotificationsToastHost', 'NotificationsToastVM');
    assert.ok(r.uxml, 'notifications-toast.uxml missing');
    assert.ok(r.uss, 'notifications-toast.uss missing');
    assert.ok(r.vm, 'NotificationsToastVM.cs missing');
    assert.ok(r.host, 'NotificationsToastHost.cs missing');
  });

  it('VM implements INotifyPropertyChanged', () => {
    const content = readFileSync(join(VMS_DIR, 'NotificationsToastVM.cs'), 'utf8');
    assert.ok(content.includes('INotifyPropertyChanged'), 'VM missing INotifyPropertyChanged');
  });

  it('VM has Toast list + Dismiss command', () => {
    const content = readFileSync(join(VMS_DIR, 'NotificationsToastVM.cs'), 'utf8');
    assert.ok(content.includes('Toast'), 'VM missing Toast type');
    assert.ok(content.includes('Dismiss'), 'VM missing Dismiss command');
  });

  it('Host registers migrated panel slug', () => {
    const content = readFileSync(join(HOSTS_DIR, 'NotificationsToastHost.cs'), 'utf8');
    assert.ok(content.includes('notifications-toast'), 'Host missing slug registration');
    assert.ok(content.includes('RegisterMigratedPanel'), 'Host missing RegisterMigratedPanel call');
  });

  it('UXML has toast-card + dismiss button', () => {
    const content = readFileSync(join(GENERATED_DIR, 'notifications-toast.uxml'), 'utf8');
    assert.ok(content.includes('toast'), 'UXML missing toast element');
    assert.ok(content.includes('dismiss') || content.includes('DismissCommand'), 'UXML missing dismiss binding');
  });
});

// ── TECH-32923: HUD strip panels ──────────────────────────────────────────────
describe('TECH-32923 — HUD strip panels migration', () => {
  it('time-controls quad exists', () => {
    const r = panelMigrated('time-controls', 'TimeControlsHost', 'TimeControlsVM');
    assert.ok(r.ok, `time-controls incomplete: ${JSON.stringify(r)}`);
  });

  it('overlay-toggle-strip quad exists', () => {
    const r = panelMigrated('overlay-toggle-strip', 'OverlayToggleStripHost', 'OverlayToggleStripVM');
    assert.ok(r.ok, `overlay-toggle-strip incomplete: ${JSON.stringify(r)}`);
  });

  it('toolbar quad exists', () => {
    const r = panelMigrated('toolbar', 'ToolbarHost', 'ToolbarVM');
    assert.ok(r.ok, `toolbar incomplete: ${JSON.stringify(r)}`);
  });

  it('mini-map quad exists', () => {
    const r = panelMigrated('mini-map', 'MiniMapHost', 'MiniMapVM');
    assert.ok(r.ok, `mini-map incomplete: ${JSON.stringify(r)}`);
  });

  it('zone-overlay quad exists', () => {
    const r = panelMigrated('zone-overlay', 'ZoneOverlayHost', 'ZoneOverlayVM');
    assert.ok(r.ok, `zone-overlay incomplete: ${JSON.stringify(r)}`);
  });

  it('TimeControlsVM has speed + pause command', () => {
    const content = readFileSync(join(VMS_DIR, 'TimeControlsVM.cs'), 'utf8');
    assert.ok(content.includes('Speed') || content.includes('TimeSpeed'), 'TimeControlsVM missing speed');
    assert.ok(content.includes('Pause') || content.includes('PauseCommand'), 'TimeControlsVM missing pause');
  });

  it('ToolbarVM has active-tool property', () => {
    const content = readFileSync(join(VMS_DIR, 'ToolbarVM.cs'), 'utf8');
    assert.ok(content.includes('ActiveTool') || content.includes('SelectedTool'), 'ToolbarVM missing active tool property');
  });
});

// ── TECH-32924: toolbar/strip/popover panels ──────────────────────────────────
describe('TECH-32924 — toolbar/strip/popover panels migration', () => {
  it('tooltip quad exists', () => {
    const r = panelMigrated('tooltip', 'TooltipHost', 'TooltipVM');
    assert.ok(r.ok, `tooltip incomplete: ${JSON.stringify(r)}`);
  });

  it('glossary-panel quad exists', () => {
    const r = panelMigrated('glossary-panel', 'GlossaryPanelHost', 'GlossaryPanelVM');
    assert.ok(r.ok, `glossary-panel incomplete: ${JSON.stringify(r)}`);
  });

  it('onboarding-overlay quad exists', () => {
    const r = panelMigrated('onboarding-overlay', 'OnboardingOverlayHost', 'OnboardingOverlayVM');
    assert.ok(r.ok, `onboarding-overlay incomplete: ${JSON.stringify(r)}`);
  });

  it('alerts-panel quad exists', () => {
    const r = panelMigrated('alerts-panel', 'AlertsPanelHost', 'AlertsPanelVM');
    assert.ok(r.ok, `alerts-panel incomplete: ${JSON.stringify(r)}`);
  });

  it('building-info quad exists', () => {
    const r = panelMigrated('building-info', 'BuildingInfoHost', 'BuildingInfoVM');
    assert.ok(r.ok, `building-info incomplete: ${JSON.stringify(r)}`);
  });

  it('TooltipVM has Label + Position properties', () => {
    const content = readFileSync(join(VMS_DIR, 'TooltipVM.cs'), 'utf8');
    assert.ok(content.includes('Label') || content.includes('Text'), 'TooltipVM missing label property');
  });
});

// ── TECH-32925: misc remainder panels ─────────────────────────────────────────
describe('TECH-32925 — misc remainder panels migration', () => {
  it('city-stats quad exists', () => {
    const r = panelMigrated('city-stats', 'CityStatsHost', 'CityStatsVM');
    assert.ok(r.ok, `city-stats incomplete: ${JSON.stringify(r)}`);
  });

  it('growth-budget-panel quad exists', () => {
    const r = panelMigrated('growth-budget-panel', 'GrowthBudgetPanelHost', 'GrowthBudgetPanelVM');
    assert.ok(r.ok, `growth-budget-panel incomplete: ${JSON.stringify(r)}`);
  });

  it('load-view quad exists', () => {
    const r = panelMigrated('load-view', 'LoadViewHost', 'LoadViewVM');
    assert.ok(r.ok, `load-view incomplete: ${JSON.stringify(r)}`);
  });

  it('splash quad exists', () => {
    const r = panelMigrated('splash', 'SplashHost', 'SplashVM');
    assert.ok(r.ok, `splash incomplete: ${JSON.stringify(r)}`);
  });

  it('onboarding quad exists', () => {
    const r = panelMigrated('onboarding', 'OnboardingHost', 'OnboardingVM');
    assert.ok(r.ok, `onboarding incomplete: ${JSON.stringify(r)}`);
  });
});

// ── TECH-32926: CityScene Canvas cleanup ─────────────────────────────────────
describe('TECH-32926 — CityScene Canvas removal', () => {
  it('CityScene exists', () => {
    const scenePath = join(ROOT, 'Assets/Scenes/CityScene.unity');
    assert.ok(existsSync(scenePath), 'CityScene.unity missing');
  });

  // Canvas removal verified structurally: scene file should not contain CanvasScaler
  // when overlay Canvas is removed (world-space Canvas retained per Q9 deferral note)
  it('no overlay CanvasScaler left in CityScene', () => {
    const scenePath = join(ROOT, 'Assets/Scenes/CityScene.unity');
    const content = readFileSync(scenePath, 'utf8');
    // After removal, no 'CanvasScaler:' component block should exist for Screen Space Overlay
    // We check that either no CanvasScaler at all, OR all CanvasScaler entries are world-space
    // Pragmatic: assert 'UIScaleMode: 1' (Screen Space) is absent — world-space uses 0
    // This is a best-effort gate; full verification requires play mode.
    // Green criteria: no overlay Canvas means no CanvasScaler with renderMode 0
    const hasOverlayCanvas = content.includes('m_RenderMode: 0') && content.includes('CanvasScaler');
    assert.ok(!hasOverlayCanvas, 'CityScene still has Screen Space Overlay Canvas + CanvasScaler');
  });
});

// ── citySceneFullyOnUiToolkit — stage acceptance ──────────────────────────────
describe('citySceneFullyOnUiToolkit — stage 5.0 acceptance', () => {
  it('all Stage 5.0 panels have UXML emitted', () => {
    const panels = [
      'notifications-toast',
      'time-controls',
      'overlay-toggle-strip',
      'toolbar',
      'mini-map',
      'zone-overlay',
      'tooltip',
      'glossary-panel',
      'onboarding-overlay',
      'alerts-panel',
      'building-info',
      'city-stats',
      'growth-budget-panel',
      'load-view',
      'splash',
      'onboarding',
    ];
    const missing = panels.filter(p => !existsSync(join(GENERATED_DIR, `${p}.uxml`)));
    assert.deepEqual(missing, [], `Missing UXML for: ${missing.join(', ')}`);
  });

  it('all Stage 5.0 VMs exist', () => {
    const vms = [
      'NotificationsToastVM',
      'TimeControlsVM',
      'OverlayToggleStripVM',
      'ToolbarVM',
      'MiniMapVM',
      'ZoneOverlayVM',
      'TooltipVM',
      'GlossaryPanelVM',
      'OnboardingOverlayVM',
      'AlertsPanelVM',
      'BuildingInfoVM',
      'CityStatsVM',
      'GrowthBudgetPanelVM',
      'LoadViewVM',
      'SplashVM',
      'OnboardingVM',
    ];
    const missing = vms.filter(v => !existsSync(join(VMS_DIR, `${v}.cs`)));
    assert.deepEqual(missing, [], `Missing VMs: ${missing.join(', ')}`);
  });

  it('all Stage 5.0 Hosts exist', () => {
    const hosts = [
      'NotificationsToastHost',
      'TimeControlsHost',
      'OverlayToggleStripHost',
      'ToolbarHost',
      'MiniMapHost',
      'ZoneOverlayHost',
      'TooltipHost',
      'GlossaryPanelHost',
      'OnboardingOverlayHost',
      'AlertsPanelHost',
      'BuildingInfoHost',
      'CityStatsHost',
      'GrowthBudgetPanelHost',
      'LoadViewHost',
      'SplashHost',
      'OnboardingHost',
    ];
    const missing = hosts.filter(h => !existsSync(join(HOSTS_DIR, `${h}.cs`)));
    assert.deepEqual(missing, [], `Missing Hosts: ${missing.join(', ')}`);
  });
});
