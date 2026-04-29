/**
 * EntityVersionDiff dispatcher tests (TECH-3302 → TECH-3304 / Stage 14.3).
 *
 * Asserts dispatcher routes each of 8 catalog kinds to its renderer; default
 * branch = TypeScript exhaustiveness `never` (no placeholder fallback).
 *
 * @see web/components/diff/EntityVersionDiff.tsx
 */
import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import EntityVersionDiff from "@/components/diff/EntityVersionDiff";
import type { CatalogKind } from "@/lib/refs/types";
import type { KindDiff } from "@/lib/diff/kind-schemas";

const KIND_TO_TESTID: ReadonlyArray<{ kind: CatalogKind; testid: string }> = [
  { kind: "sprite", testid: "sprite-renderer" },
  { kind: "asset", testid: "asset-renderer" },
  { kind: "button", testid: "button-renderer" },
  { kind: "panel", testid: "panel-renderer" },
  { kind: "pool", testid: "pool-renderer" },
  { kind: "token", testid: "token-renderer" },
  { kind: "archetype", testid: "archetype-renderer" },
  { kind: "audio", testid: "audio-renderer" },
];

const emptyDiff: KindDiff = { added: [], removed: [], changed: [] };

describe("EntityVersionDiff dispatcher (TECH-3302 → TECH-3304)", () => {
  for (const { kind, testid } of KIND_TO_TESTID) {
    it(`routes kind=${kind} to its renderer`, () => {
      const html = renderToStaticMarkup(
        <EntityVersionDiff kind={kind} diff={emptyDiff} />,
      );
      expect(html).toContain(`data-testid="${testid}"`);
      expect(html).not.toContain('data-testid="kind-renderer-placeholder"');
      expect(html).not.toContain("Renderer pending");
    });
  }
});
