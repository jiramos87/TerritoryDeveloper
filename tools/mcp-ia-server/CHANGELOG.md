# @territory/mcp-ia-server — Changelog

## v1.2.0 — 2026-04-19 — Theme B audit remainder: parse cache, yaml-first, progressive disclosure, descriptor lints

Stage 17 of the Opus 4.7 audit master plan (`ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md`). Bundles parser/loader performance wins with surface-shape tightening and CI gates.

Shipped:

- **TECH-495 (B4)** — On-disk parse cache at `tools/mcp-ia-server/.cache/parse-cache.json` (mtime-keyed JSON). `parseDocument()` now: in-memory map → cache read → parse → write-through. Dist build wired; `.mcp.json` flipped from `tsx src/index.ts` to `node bin/launch.mjs` (launcher honors `MCP_SOURCE_MODE=1` dev fallback). Gitignore excludes `.cache/`.
- **TECH-496 (B8)** — YAML-first backlog loader + manifest cache (`ia/backlog/` + `ia/backlog-archive/` dir mtime keyed). Open-over-archive collision precedence preserved.
- **TECH-497 (B6)** — `tools/scripts/validate-mcp-readme.mjs` + `validate:mcp-readme` npm script composed into `validate:all`. Asserts README tool table matches registered `registerTool(...)` set.
- **TECH-498 (B5) — BREAKING** — `spec_outline` + `list_rules` default to trimmed payload. Pass `expand: true` for full tree / full rule set.
  - `spec_outline` default → depth-1 heading tree only. `expand: true` → full tree (prior behavior).
  - `list_rules` default → `alwaysApply: true` rules only. `expand: true` → all rules.
- **TECH-499 (B9)** — `tools/scripts/validate-mcp-descriptor-prose.mjs` + `validate:mcp-descriptor-prose` npm script composed into `validate:all`. Enforces ≤120-char `.describe()` budget on every Zod param + tool description in `src/tools/*.ts`.
- **TECH-500** — Descriptor remediation pass. `unity-bridge-command.ts` `scene_path` + `menu_path` descriptors trimmed to ≤120 chars.

### Migration table

| Tool | Old default | New default | To restore |
|---|---|---|---|
| `spec_outline` | full heading tree | depth-1 only | `{ spec, expand: true }` |
| `list_rules` | all rules | `alwaysApply: true` only | `{ expand: true }` |

**Rollback tag advisory:** tag this commit `mcp-pre-theme-b-remainder-v1.1.x` locally (or push the annotated tag) before landing future breaking shape changes. Provides the rollback target for the progressive-disclosure cut.

## v0.6.0 — 2026-04-18 — Quick wins: glossary bulk-terms + structured invariants

Step 1 Quick Wins band of the Opus 4.7 audit master plan (`ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md`). Additive, non-breaking — all pre-existing tool shapes preserved.

Shipped:

- **TECH-314** — `glossary_lookup` accepts `terms: string[]` alongside single `term`; returns `{ results, errors, meta.partial }` partial-result shape.
- **TECH-315** — Bulk-terms unit tests (happy path + partial failure + single-`term` back-compat + empty `terms: []`).
- **TECH-371** — `tools/mcp-ia-server/data/invariants-tags.json` sidecar — subsystem tags for all 13 invariants + guardrails.
- **TECH-372** — `invariants_summary` accepts optional `domain?: string` filter; returns structured `{ description, invariants: [{number, title, subsystem_tags}], guardrails: [{index, title, subsystem_tags}], markdown }`; `markdown` side-channel preserves prose rendering for text-only callers.
- **TECH-373** — `invariants_summary` unit tests (domain match filter, domain-no-match graceful, no-domain all-13, markdown side-channel).

**Rollback tag advisory:** tag this commit `mcp-pre-envelope-v0.5.0` locally (or push the annotated tag) before landing Step 2 (P2 envelope breaking cut). Provides the rollback target if the envelope rewrite needs to be reverted in the same PR boundary.
