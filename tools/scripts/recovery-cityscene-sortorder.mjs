#!/usr/bin/env node
/**
 * recovery-cityscene-sortorder — Phase F of the UI Toolkit Parity Recovery plan.
 *
 * Walks Assets/Scenes/CityScene.unity. For each UIDocument component referencing a UXML
 * under Assets/UI/Generated/, looks up the panel slug by GUID and rewrites the adjacent
 * m_SortingOrder field per the plan's z-stack policy:
 *
 *   hud-bar, city-stats, mini-map           = 10
 *   toolbar, time-controls,
 *   overlay-toggle-strip, zone-overlay      = 20
 *   tool-subtype-picker                     = 25 (HUD-anchored picker, not modal)
 *   onboarding-overlay, onboarding          = 150
 *   tooltip                                 = 200
 *   everything else (modals)                = 100
 *
 * Idempotent.
 */

import * as fs from 'node:fs';
import * as path from 'node:path';

const REPO_ROOT = path.resolve(path.dirname(new URL(import.meta.url).pathname), '..', '..');
const SCENE_PATH = path.join(REPO_ROOT, 'Assets', 'Scenes', 'CityScene.unity');

const SORT_ORDER = {
  'hud-bar': 10,
  'city-stats': 10,
  'mini-map': 10,
  'toolbar': 20,
  'time-controls': 20,
  'overlay-toggle-strip': 20,
  'zone-overlay': 20,
  'tool-subtype-picker': 25,
  'onboarding-overlay': 150,
  'onboarding': 150,
  'tooltip': 200,
};
const DEFAULT_MODAL_SORT = 100;

function slugByGuid() {
  const dir = path.join(REPO_ROOT, 'Assets', 'UI', 'Generated');
  const map = {};
  for (const f of fs.readdirSync(dir)) {
    if (!f.endsWith('.uxml.meta')) continue;
    const slug = f.replace(/\.uxml\.meta$/, '');
    const txt = fs.readFileSync(path.join(dir, f), 'utf8');
    const m = txt.match(/guid:\s*([a-z0-9]+)/i);
    if (m) map[m[1]] = slug;
  }
  return map;
}

function main() {
  const guidToSlug = slugByGuid();
  let text = fs.readFileSync(SCENE_PATH, 'utf8');

  // Walk the file as one string. Find sourceAsset blocks of the form
  // "sourceAsset: {fileID: ..., guid: <GUID>, type: 3}\n  m_SortingOrder: N".
  // Replace N when slug recognized.
  const re = /sourceAsset:\s*\{[^}]*guid:\s*([a-z0-9]+)[^}]*\}\s*\n(\s*)m_SortingOrder:\s*(\-?\d+)/gi;
  let mutated = 0;
  const updates = [];
  text = text.replace(re, (match, guid, indent, current) => {
    const slug = guidToSlug[guid];
    if (!slug) return match; // not a generated UXML (e.g. UI Toolkit theme)
    const target = SORT_ORDER[slug] ?? DEFAULT_MODAL_SORT;
    if (Number(current) === target) return match;
    updates.push({ slug, from: Number(current), to: target });
    mutated++;
    return match.replace(/m_SortingOrder:\s*\-?\d+/, `m_SortingOrder: ${target}`);
  });

  if (mutated > 0) fs.writeFileSync(SCENE_PATH, text);
  console.log(JSON.stringify({ mutated, updates }, null, 2));
}

main();
