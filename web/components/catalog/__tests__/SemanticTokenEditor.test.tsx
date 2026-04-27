import { describe, it, expect } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import SemanticTokenEditor from "@/components/catalog/tokens/SemanticTokenEditor";

/**
 * <SemanticTokenEditor /> coverage (TECH-2093 / Stage 10.1).
 *
 * SSR-only assertions — the cycle banner is driven by an effect+fetch pair
 * that does not run inside `renderToStaticMarkup`. Tests focus on initial
 * markup shape: alias picker present, role input present, no banner on first
 * render.
 *
 * Cycle DFS contract is covered by `lib/tokens/__tests__/semantic-cycle-check.test.ts`.
 */
describe("<SemanticTokenEditor /> SSR shape", () => {
  it("renders the alias picker + role input + no banner on initial mount", () => {
    const html = renderToStaticMarkup(
      <SemanticTokenEditor
        selfEntityId={42}
        value={{ token_role: "" }}
        targetRow={null}
        targetId={null}
        onTargetChange={() => {}}
        onValueChange={() => {}}
        onCycleChange={() => {}}
      />,
    );
    expect(html).toContain('data-testid="semantic-token-editor"');
    expect(html).toContain('data-testid="semantic-token-editor-target"');
    expect(html).toContain('data-testid="semantic-token-editor-role"');
    expect(html).not.toContain('data-testid="semantic-token-editor-cycle-banner"');
  });

  it("disables both surfaces when disabled prop set", () => {
    const html = renderToStaticMarkup(
      <SemanticTokenEditor
        selfEntityId={42}
        value={{ token_role: "surface.elevated" }}
        targetRow={null}
        targetId={null}
        onTargetChange={() => {}}
        onValueChange={() => {}}
        onCycleChange={() => {}}
        disabled
      />,
    );
    // Role input carries `disabled=""` boolean attr.
    expect(html).toMatch(/data-testid="semantic-token-editor-role"[^>]*disabled=""/);
  });

  it("shows alias-picker open CTA when no target, picker accepts kind=token", () => {
    const html = renderToStaticMarkup(
      <SemanticTokenEditor
        selfEntityId={42}
        value={{ token_role: "" }}
        targetRow={null}
        targetId={null}
        onTargetChange={() => {}}
        onValueChange={() => {}}
        onCycleChange={() => {}}
      />,
    );
    expect(html).toContain("Alias target (semantic → token)");
    expect(html).toContain("Pick token");
  });
});
