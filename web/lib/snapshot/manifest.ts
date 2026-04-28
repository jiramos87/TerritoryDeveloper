import { createHash } from "node:crypto";

/**
 * Manifest shape written to `Assets/StreamingAssets/catalog/manifest.json`
 * and consumed by Unity `CatalogLoader` (TECH-2675). Schema-version 2 marks
 * the per-kind v2 export. Kind list is the closed 8-kind set per DEC-A9.
 *
 * @see ia/projects/asset-pipeline/stage-13.1 — TECH-2673 §Plan Digest
 */

export const SNAPSHOT_KINDS = [
  "sprite",
  "asset",
  "button",
  "panel",
  "audio",
  "pool",
  "token",
  "archetype",
] as const;

export type SnapshotKind = (typeof SNAPSHOT_KINDS)[number];

export type EntityCounts = Record<SnapshotKind, number>;

export type Manifest = {
  schemaVersion: number;
  generatedAt: string;
  snapshotHash: string;
  entityCounts: EntityCounts;
};

/**
 * `computeManifestHash` — sha256 hex over kind-ordered concatenation of the
 * per-kind JSON file bytes. Deterministic given the same inputs in any caller
 * order: kinds are iterated in `SNAPSHOT_KINDS` order, not the iteration order
 * of the input record.
 */
export function computeManifestHash(
  files: Record<SnapshotKind, Buffer>,
): string {
  const hash = createHash("sha256");
  for (const kind of SNAPSHOT_KINDS) {
    const buf = files[kind];
    if (buf === undefined) {
      throw new Error(
        `computeManifestHash: missing buffer for kind "${kind}"`,
      );
    }
    hash.update(buf);
  }
  return hash.digest("hex");
}

export function emptyEntityCounts(): EntityCounts {
  const counts: Partial<EntityCounts> = {};
  for (const kind of SNAPSHOT_KINDS) counts[kind] = 0;
  return counts as EntityCounts;
}
