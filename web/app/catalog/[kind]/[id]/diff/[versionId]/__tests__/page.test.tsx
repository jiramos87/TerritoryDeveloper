/**
 * Diff page route tests (TECH-3304 / Stage 14.3).
 *
 * 8-kind smoke + bad-kind 404 + API-404 paths. Mocks `next/headers` to return
 * a static host, mocks `next/navigation.notFound` (throws NEXT_NOT_FOUND), and
 * stubs `globalThis.fetch` per case.
 *
 * @see web/app/catalog/[kind]/[id]/diff/[versionId]/page.tsx
 */
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import type { CatalogKind } from "@/lib/refs/types";

vi.mock("next/headers", () => ({
  headers: async () =>
    new Map<string, string>([
      ["host", "localhost:3000"],
      ["x-forwarded-proto", "http"],
    ]) as unknown as Headers,
}));

class NotFoundError extends Error {
  digest = "NEXT_NOT_FOUND";
  constructor() {
    super("NEXT_NOT_FOUND");
  }
}
vi.mock("next/navigation", () => ({
  notFound: () => {
    throw new NotFoundError();
  },
}));

const KINDS: CatalogKind[] = [
  "sprite",
  "asset",
  "button",
  "panel",
  "pool",
  "token",
  "archetype",
  "audio",
];

function makeDiffEnvelope(kind: CatalogKind) {
  return {
    ok: true,
    data: {
      from: {
        id: "1",
        entity_id: "10",
        version_number: 1,
        status: "published",
        created_at: "2026-04-29T12:00:00.000Z",
        parent_version_id: null,
        archetype_version_id: null,
      },
      to: {
        id: "2",
        entity_id: "10",
        version_number: 2,
        status: "published",
        created_at: "2026-04-29T12:30:00.000Z",
        parent_version_id: "1",
        archetype_version_id: null,
      },
      diff: {
        added: ["new_field"],
        removed: [],
        changed: [
          {
            field: "name",
            before: `${kind}_old`,
            after: `${kind}_new`,
            hint: "scalar",
          },
        ],
      },
    },
  };
}

function fetchOk<T>(body: T) {
  return vi.fn().mockResolvedValue({
    ok: true,
    status: 200,
    json: async () => body,
  });
}

function fetch404() {
  return vi.fn().mockResolvedValue({
    ok: false,
    status: 404,
    json: async () => ({ ok: false, error: { code: "not_found", message: "" } }),
  });
}

let originalFetch: typeof globalThis.fetch | undefined;

beforeEach(() => {
  originalFetch = globalThis.fetch;
});

afterEach(() => {
  if (originalFetch !== undefined) {
    globalThis.fetch = originalFetch;
  }
  vi.restoreAllMocks();
});

async function importPage() {
  const mod = await import("@/app/catalog/[kind]/[id]/diff/[versionId]/page");
  return mod.default;
}

describe("Diff page route (TECH-3304) — 8-kind smoke", () => {
  for (const kind of KINDS) {
    it(`renders without 'Renderer pending' for kind=${kind}`, async () => {
      globalThis.fetch = fetchOk(makeDiffEnvelope(kind)) as typeof fetch;
      const Page = await importPage();
      const element = await Page({
        params: Promise.resolve({ kind, id: "10", versionId: "2" }),
      });
      const html = renderToStaticMarkup(element as React.ReactElement);
      expect(html).toContain('data-testid="diff-page-root"');
      expect(html).toContain('data-testid="diff-page-header"');
      expect(html).toContain("v1");
      expect(html).toContain("v2");
      expect(html).not.toContain("Renderer pending");
      expect(html).not.toContain("kind-renderer-placeholder");
    });
  }

  it("renders (root) marker when from version is null", async () => {
    const env = makeDiffEnvelope("sprite");
    env.data.from = null as unknown as typeof env.data.from;
    globalThis.fetch = fetchOk(env) as typeof fetch;
    const Page = await importPage();
    const element = await Page({
      params: Promise.resolve({ kind: "sprite", id: "10", versionId: "2" }),
    });
    const html = renderToStaticMarkup(element as React.ReactElement);
    expect(html).toContain("(root)");
    expect(html).toContain("v2");
  });
});

describe("Diff page route (TECH-3304) — error paths", () => {
  it("notFound() on bad-kind path", async () => {
    globalThis.fetch = fetchOk(makeDiffEnvelope("sprite")) as typeof fetch;
    const Page = await importPage();
    await expect(
      Page({
        params: Promise.resolve({
          kind: "garbage",
          id: "10",
          versionId: "2",
        }),
      }),
    ).rejects.toThrow("NEXT_NOT_FOUND");
  });

  it("notFound() when API returns 404", async () => {
    globalThis.fetch = fetch404() as typeof fetch;
    const Page = await importPage();
    await expect(
      Page({
        params: Promise.resolve({ kind: "sprite", id: "10", versionId: "999" }),
      }),
    ).rejects.toThrow("NEXT_NOT_FOUND");
  });
});
