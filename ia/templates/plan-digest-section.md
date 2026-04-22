---
purpose: "§Plan Digest section template fragment for ia/projects task specs (plan-digest pass)."
audience: agent
loaded_by: ondemand
slices_via: none
---

<!--
  §Plan Digest section template. Opus-authored per Task spec during Stage-scoped plan-digest pass.
  Placement: between §10 Lessons Learned and §Open Questions in ia/projects/{ISSUE_ID}.md.
  Replaces §Plan Author in the final spec (Q5 2026-04-22). §Plan Author is ephemeral intermediate.
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
