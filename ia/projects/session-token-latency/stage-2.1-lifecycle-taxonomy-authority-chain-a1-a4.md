### Stage 2.1 — Lifecycle taxonomy authority chain (A1 + A4)


**Status:** In Progress — Pass 2 tail (TECH-577..580 open in backlog until `/closeout`)

**Pre-condition:** lifecycle-refactor Stage 10 T10.2 Done (`ia/skills/_preamble/stable-block.md` exists).

**Objectives:** Declare `docs/agent-lifecycle.md` as the single authoritative source for lifecycle taxonomy; collapse the 3 duplicates (CLAUDE.md §3, `ia/rules/agent-lifecycle.md`, AGENTS.md §3) to pointer stubs. Promote oversized MEMORY.md entries to per-file pointers.

**Exit:**

- `ia/rules/agent-lifecycle.md`: ≤12 lines, references `docs/agent-lifecycle.md` only.
- `CLAUDE.md` §3 Key files: ≤20 lines; lifecycle taxonomy row removed; `docs/agent-lifecycle.md` referenced as authority.
- `AGENTS.md` §3: ≤8 lines cross-reference section; full taxonomy table removed.
- `docs/agent-lifecycle.md`: explicitly marked `# {Title} — Canonical authority`; no duplicate prose removed (already authoritative).
- `.claude/memory/` dir: ≥1 `{slug}.md` file per oversized MEMORY.md entry; MEMORY.md index ≤180 lines.
- `npm run validate:all` green.

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T2.1.1 | Collapse rule + CLAUDE.md §3 | **TECH-577** | Pass 2 pending | Shrink `ia/rules/agent-lifecycle.md` to ≤12 lines: retain header + one-sentence purpose + `Full canonical doc: docs/agent-lifecycle.md` pointer + `## Ordered flow` stub linking there. Collapse `CLAUDE.md` §3 Key files: remove lifecycle taxonomy prose (≤20 lines remain); add `docs/agent-lifecycle.md` row to key-files table as sole lifecycle authority. Run `npm run validate:all`. |
| T2.1.2 | Collapse AGENTS.md §3 | **TECH-578** | Pass 2 pending | Shrink `AGENTS.md` §3 lifecycle section: replace full taxonomy table with ≤8-line block: "Full lifecycle flow: `docs/agent-lifecycle.md`. Surface map table: `ia/rules/agent-lifecycle.md` §Surface map." Remove restated step/stage/phase/task definitions. Verify no other AGENTS.md section duplicates CLAUDE.md key-files inventory. `npm run validate:all`. |
| T2.1.3 | MEMORY.md oversized-entry promotion | **TECH-579** | Pass 2 pending | Identify all MEMORY.md entries (both root `MEMORY.md` and `~/.claude-personal/projects/.../memory/MEMORY.md`) exceeding 10 lines. For each: write `{slug}.md` to `.claude/memory/` (repo-scoped entries) or `~/.claude-personal/projects/.../memory/` (user entries) with full content. Replace MEMORY.md inline content with pointer line `- [{Title}]({slug}.md) — {one-line hook}`. |
| T2.1.4 | MEMORY.md index validation | **TECH-580** | Pass 2 pending | Confirm both MEMORY.md files ≤200 lines (harness truncation threshold). Validate all pointer links resolve to existing files. Check `docs/agent-lifecycle.md` still has correct `Status:` + last-updated front matter after A1 edits. `npm run validate:all` green. |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: ""
  title: "Collapse rule + CLAUDE.md §3"
  priority: "medium"
  notes: |
    Shrink `ia/rules/agent-lifecycle.md` to pointer stub (≤12 lines). Collapse `CLAUDE.md` §3 Key files: drop lifecycle taxonomy prose; cite `docs/agent-lifecycle.md` as sole authority. Gate: `npm run validate:all`.
  depends_on:
    - "TECH-503"
  related: []
  stub_body:
    summary: |
      A1 doc-triangle: rule stub + CLAUDE key-files table point at `docs/agent-lifecycle.md` only; remove duplicate lifecycle taxonomy from always-loaded surfaces.
    goals: |
      - `ia/rules/agent-lifecycle.md` ≤12 lines with pointer to `docs/agent-lifecycle.md` + ordered-flow stub.
      - `CLAUDE.md` §3 Key files ≤20 lines; lifecycle row references canonical doc only.
      - `npm run validate:all` green.
    systems_map: |
      Touches: `ia/rules/agent-lifecycle.md`, `CLAUDE.md` §3, `docs/agent-lifecycle.md` (read-only confirm authority marker).
      Ref: `docs/session-token-latency-audit-exploration.md` Theme A; master plan Step 2 Stage 2.1 exit bullets.
    impl_plan_sketch: |
      Phase 1 — Rule + CLAUDE collapse.
      1. Edit `ia/rules/agent-lifecycle.md` to header + one-line purpose + pointer + `## Ordered flow` stub linking canonical doc.
      2. Trim `CLAUDE.md` §3: remove restated taxonomy; ensure key-files table includes `docs/agent-lifecycle.md` as lifecycle authority.
      3. `npm run validate:all`.

- reserved_id: ""
  title: "Collapse AGENTS.md §3"
  priority: "medium"
  notes: |
    Replace AGENTS.md §3 full taxonomy with ≤8-line cross-ref block to `docs/agent-lifecycle.md` + `ia/rules/agent-lifecycle.md` §Surface map. Verify no duplicate key-files inventory vs CLAUDE. `npm run validate:all`.
  depends_on:
    - "TECH-503"
  related: []
  stub_body:
    summary: |
      A1: AGENTS.md lifecycle section becomes short cross-reference; no restated step/stage/phase/task definitions.
    goals: |
      - `AGENTS.md` §3 ≤8 lines; points to canonical doc + surface map stub.
      - No section duplicates CLAUDE key-files list inappropriately.
      - `npm run validate:all` green.
    systems_map: |
      Touches: `AGENTS.md` §3, `docs/agent-lifecycle.md`, `ia/rules/agent-lifecycle.md` (surface map anchor).
    impl_plan_sketch: |
      Phase 1 — AGENTS collapse.
      1. Replace §3 body with cross-ref block per Stage 2.1 Intent.
      2. Grep AGENTS for accidental CLAUDE inventory duplication; fix.
      3. `npm run validate:all`.

- reserved_id: ""
  title: "MEMORY.md oversized-entry promotion"
  priority: "medium"
  notes: |
    Find MEMORY.md entries >10 lines (repo root + optional user path). Promote each to `.claude/memory/{slug}.md` or user memory dir; replace inline body with one-line pointer. Doc-only; no runtime C#.
  depends_on:
    - "TECH-503"
  related: []
  stub_body:
    summary: |
      A4: split oversized MEMORY bullets into slug files; keep MEMORY.md as short index with links + hooks.
    goals: |
      - Every >10-line entry promoted to dedicated file; MEMORY.md lines replaced with pointer rows.
      - `.claude/memory/` populated for repo-scoped content per Intent.
      - Paths outside repo documented if user-local-only (optional scope note in spec).
    systems_map: |
      Touches: root `MEMORY.md`, `.claude/memory/`, `~/.claude-personal/.../memory/MEMORY.md` (if present on dev machine).
    impl_plan_sketch: |
      Phase 2 — Promotion sweep.
      1. Enumerate entries; measure line counts.
      2. For each oversized: write `{slug}.md`; replace with `- [Title](slug.md) — hook` line.
      3. Commit repo-scoped files; document user-dir handling in §7 if needed.

- reserved_id: ""
  title: "MEMORY.md index validation"
  priority: "medium"
  notes: |
    Enforce MEMORY index size caps (≤200 lines per file per harness); validate pointer targets exist; re-check `docs/agent-lifecycle.md` front matter after A1. `npm run validate:all` green.
  depends_on:
    - "TECH-503"
  related: []
  stub_body:
    summary: |
      Close A4: index hygiene + link integrity + doc metadata consistency after lifecycle edits.
    goals: |
      - Both MEMORY.md files ≤200 lines; all links resolve.
      - `docs/agent-lifecycle.md` Status + last-updated front matter still correct post-A1.
      - `npm run validate:all` green.
    systems_map: |
      Touches: `MEMORY.md` (root), user memory path if applicable, `docs/agent-lifecycle.md` metadata.
    impl_plan_sketch: |
      Phase 2 — Validation pass.
      1. Line-count + link check on MEMORY indices.
      2. Verify `docs/agent-lifecycle.md` header fields.
      3. `npm run validate:all`.
```

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_pending — populated by `/audit {{this-doc}} Stage {{N.M}}` once all Tasks reach Done post-verify._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
