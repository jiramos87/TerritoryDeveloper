---
slug: design-explore-render-fixture
target_version: 1
audience: agent
created_at: 2026-05-13
stages:
  - id: "1.0"
    title: "Tracer slice"
    exit: "tracer renders end-to-end"
    enriched:
      edge_cases:
        - { input: "empty input", state: "first-run", expected: "no-op" }
        - { input: "duplicate input", state: "warm", expected: "idempotent" }
        - { input: "malformed input", state: "warm", expected: "error surfaced" }
    tasks:
      - id: "1.0.1"
        prefix: TECH
        title: "Wire harness"
        depends_on: []
        digest_outline: "Wire test harness"
        touched_paths: ["tools/scripts/harness.mjs"]
        kind: code
        enriched:
          glossary_anchors: ["harness", "tracer-slice"]
          failure_modes:
            - "Fails if harness path absent"
          touched_paths_with_preview:
            - { path: "tools/scripts/harness.mjs", loc: null, kind: "new", summary: "Test harness shell" }
  - id: "2.0"
    title: "Follow-on stage"
    exit: "follow-on done"
    tasks:
      - id: "2.0.1"
        prefix: TECH
        title: "Follow-on task"
        depends_on: ["1.0.1"]
        digest_outline: "..."
        touched_paths: []
        kind: code
decisions:
  - { id: "Q1", topic: "Test scope", pick: "happy path", rationale: "minimal viable" }
references:
  - { cat: "docs", title: "Schema rule", href: "../../ia/rules/design-explore-output-schema.md", desc: "Source schema." }
---

## Design Expansion

### Core Prototype

- verb: render-and-extract
- hardcoded_scope: fixture MD
- stubbed_systems: none
- throwaway: none
- forward_living: renderer + extractor

### Iteration Roadmap

| Stage | Scope | Visibility delta |
|---|---|---|
| 2.0 | Follow-on edge | second pass works |

Body text. Contains a literal `&lt;/script&gt;` reference (escaped already) plus a real
closing `</script>` token nested in prose:

The renderer escapes `</script>` automatically and the extractor reverses it. This
fixture covers the bijection.
