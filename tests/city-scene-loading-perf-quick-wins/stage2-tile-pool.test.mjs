/**
 * Stage 2.0 — TilePool pre-warm
 * Red-stage proof anchor: Assets/Scripts/Domains/Grid/Services/TilePool.cs::PreWarm
 *
 * These are static analysis / structural tests (no Unity runtime required).
 * They assert file presence, API surface, and wiring points per the spec.
 */

import { readFileSync, existsSync } from 'fs';
import { resolve } from 'path';
import assert from 'assert/strict';

const REPO_ROOT = resolve(new URL('.', import.meta.url).pathname, '../..');

function readCs(rel) {
  const abs = resolve(REPO_ROOT, rel);
  assert.ok(existsSync(abs), `File missing: ${rel}`);
  return readFileSync(abs, 'utf8');
}

// TECH-33429: TilePool service
{
  const src = readCs('Assets/Scripts/Domains/Grid/Services/TilePool.cs');
  assert.ok(src.includes('public void PreWarm(int count, GameObject prefab)'), 'TilePool.PreWarm signature');
  assert.ok(src.includes('public GameObject Get(GameObject prefab, Vector3 position)'), 'TilePool.Get signature');
  assert.ok(src.includes('public void Return(GameObject tile)'), 'TilePool.Return signature');
  assert.ok(src.includes('public int ActiveCount'), 'TilePool.ActiveCount property');
  assert.ok(src.includes('ObjectPool<GameObject>'), 'Uses UnityEngine.Pool.ObjectPool');
  assert.ok(src.includes('_poolRoot'), 'Pool root transform present');
  console.log('PASS TECH-33429: TilePool service API surface');
}

// TECH-33430: GridManager wiring
{
  const gmSrc = readCs('Assets/Scripts/Managers/GameManagers/GridManager.cs');
  assert.ok(gmSrc.includes('[SerializeField] private TilePool tilePool'), 'SerializeField tilePool in GridManager');
  assert.ok(gmSrc.includes('public TilePool TilePool => tilePool'), 'TilePool getter exposed');
  assert.ok(gmSrc.includes('public GameObject TilePrefab'), 'TilePrefab getter exposed');

  const implSrc = readCs('Assets/Scripts/Managers/GameManagers/GridManager.Impl.cs');
  assert.ok(implSrc.includes('tilePool.Get(tilePrefab'), 'CreateGrid uses tilePool.Get');
  assert.ok(implSrc.includes('TilePool not wired'), 'Null-guard fallback warning present');
  assert.ok(implSrc.includes('tilePool.Return(child)'), 'RestoreGrid returns tiles to pool');
  console.log('PASS TECH-33430: GridManager TilePool wiring');
}

// TECH-33431: GeographyInitService pre-warm
{
  const geoSrc = readCs('Assets/Scripts/Managers/GameManagers/GeographyInitService.cs');
  assert.ok(geoSrc.includes('pool.PreWarm(w * h, tilePrefab)'), 'PreWarm called before InitializeGrid');
  assert.ok(geoSrc.includes('TilePool not wired'), 'Null-guard warning for missing pool');
  assert.ok(geoSrc.includes('TilePool pre-warm skipped'), 'Null-guard warning for missing prefab');
  // PreWarm must appear before InitializeGrid call in source order
  const preWarmIdx = geoSrc.indexOf('pool.PreWarm(w * h, tilePrefab)');
  const initGridIdx = geoSrc.indexOf('_hub.gridManager.InitializeGrid()');
  assert.ok(preWarmIdx < initGridIdx, 'PreWarm appears before InitializeGrid in source');
  console.log('PASS TECH-33431: GeographyInitService pre-warm wiring');
}

console.log('\nAll Stage 2.0 tests passed.');
