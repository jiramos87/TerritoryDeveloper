---
purpose: "§Plan Digest section template fragment for task specs (stage-authoring pass)."
audience: agent
loaded_by: ondemand
slices_via: none
---

<!--
  §Plan Digest section template. Opus-authored per Task spec during Stage-scoped `stage-authoring` pass.
  Direct §Plan Digest authoring (no §Plan Author intermediate — folded post-Step-7).
-->

## §Plan Digest

### §Goal

<!-- 1–2 sentences — task outcome in product / domain terms. Glossary-aligned. -->

### §Acceptance

<!-- Checkbox list — refined per-Task acceptance. Narrower than Stage Exit. -->

- [ ] …

### §Test Blueprint

<!-- Structured tuples consumed by /implement + /verify-loop. -->

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|

### §Examples

<!-- Concrete inputs/outputs + edge cases. Tables or code blocks. -->

### §Mechanical Steps

<!-- Sequential, pre-decided. Each step carries: Goal / Edits (before+after strings) / Gate / STOP / MCP hints. -->

#### Step 1 — {name}

**Goal:** …

**Edits:**
- `{repo-relative-path}` — **before**:
  ```
  …
  ```
  **after**:
  ```
  …
  ```

**Gate:**
```bash
…
```

**STOP:** …

**MCP hints:** `plan_digest_resolve_anchor`, `{other}` …

#### Step 2 — …
