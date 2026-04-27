import { describe, it, expect } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import MigrationHintEditor from "@/components/archetype/MigrationHintEditor";
import type { SchemaDiff } from "@/lib/archetype/diff-schemas";

/** Static-render coverage for `<MigrationHintEditor />` (TECH-2461). */
const FULL_DIFF: SchemaDiff = {
  added: [
    { slug: "fresh", type: "integer" },
    { slug: "colour", type: "string" },
  ],
  removed: [
    { slug: "color", type: "string" },
    { slug: "legacy", type: "integer" },
  ],
  renamed_candidates: [
    { from: "color", to: "colour", type: "string", distance: 1 },
  ],
};

describe("<MigrationHintEditor />", () => {
  it("renders three section headings", () => {
    const html = renderToStaticMarkup(
      <MigrationHintEditor diff={FULL_DIFF} hint={{}} onChange={() => {}} />,
    );
    expect(html).toContain('data-testid="migration-hint-editor"');
    expect(html).toContain('data-testid="migration-hint-added"');
    expect(html).toContain('data-testid="migration-hint-removed"');
    expect(html).toContain('data-testid="migration-hint-suggestions"');
  });

  it("renders one row per added field with a default input", () => {
    const html = renderToStaticMarkup(
      <MigrationHintEditor diff={FULL_DIFF} hint={{}} onChange={() => {}} />,
    );
    expect(html).toContain('data-testid="migration-hint-added-fresh"');
    expect(html).toContain('data-testid="migration-hint-added-fresh-default"');
    expect(html).toContain('data-testid="migration-hint-added-colour"');
    expect(html).toContain('data-testid="migration-hint-added-colour-default"');
  });

  it("renders one row per removed field with rule select", () => {
    const html = renderToStaticMarkup(
      <MigrationHintEditor diff={FULL_DIFF} hint={{}} onChange={() => {}} />,
    );
    expect(html).toContain('data-testid="migration-hint-removed-color"');
    expect(html).toContain('data-testid="migration-hint-removed-color-rule"');
    expect(html).toContain('data-testid="migration-hint-removed-legacy"');
    expect(html).toContain('data-testid="migration-hint-removed-legacy-rule"');
  });

  it("renders rename target select only when rule=rename", () => {
    const html = renderToStaticMarkup(
      <MigrationHintEditor
        diff={FULL_DIFF}
        hint={{ rename: [{ from: "color", to: "colour" }] }}
        onChange={() => {}}
      />,
    );
    expect(html).toContain('data-testid="migration-hint-removed-color-target"');
    expect(html).not.toContain('data-testid="migration-hint-removed-legacy-target"');
  });

  it("renders rename suggestion with accept button", () => {
    const html = renderToStaticMarkup(
      <MigrationHintEditor diff={FULL_DIFF} hint={{}} onChange={() => {}} />,
    );
    expect(html).toContain('data-testid="migration-hint-suggestion-color"');
    expect(html).toContain('data-testid="migration-hint-suggestion-color-accept"');
  });

  it("disables accept button after the rule is applied", () => {
    const html = renderToStaticMarkup(
      <MigrationHintEditor
        diff={FULL_DIFF}
        hint={{ rename: [{ from: "color", to: "colour" }] }}
        onChange={() => {}}
      />,
    );
    expect(html).toMatch(
      /data-testid="migration-hint-suggestion-color-accept"[^>]*disabled=""/,
    );
  });

  it("renders empty-state copy when diff is empty", () => {
    const html = renderToStaticMarkup(
      <MigrationHintEditor
        diff={{ added: [], removed: [], renamed_candidates: [] }}
        hint={{}}
        onChange={() => {}}
      />,
    );
    expect(html).toContain("None added");
    expect(html).toContain("None removed");
    expect(html).toContain("No suggestions");
  });
});
