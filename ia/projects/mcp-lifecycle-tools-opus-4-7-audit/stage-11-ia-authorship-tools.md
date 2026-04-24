### Stage 11 — Mutations + Authorship + Bridge + Journal Lifecycle / IA Authorship Tools


**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Implement 4 IA-authorship tools — `glossary_row_create`, `glossary_row_update`, `spec_section_append`, `rule_create` — with cross-ref validation and `caller_agent` gating. All four trigger non-blocking index regen after successful write.

**Exit:**

- `glossary_row_create({ caller_agent: "plan-author", row: {...} })` appends to correct category bucket in `ia/specs/glossary.md`; triggers `npm run build:glossary-index` regen non-blocking. (Post-M6 caller; retired `spec-kickoff` value remains in the allowlist for backwards compatibility with archived scripts but new callers use `plan-author`.)
- Duplicate term (case-insensitive) → `invalid_input`.
- `spec_reference` pointing to non-existent spec → `invalid_input` (hint: nearest spec name).
- `spec_section_append` validates heading uniqueness via `spec_outline`.
- `rule_create` validates filename uniqueness.
- Tests green for all 4 tools including `unauthorized_caller` paths.
- Phase 1 — Glossary authorship tools.
- Phase 2 — Spec + rule authorship tools.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T11.1 | glossary_row_create | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/glossary-row-create.ts` via `wrapTool` + `checkCaller`: validate `spec_reference` → call `list_specs` to confirm spec exists; check duplicate term (case-insensitive) against glossary index; append row to correct `## {Category}` bucket in `ia/specs/glossary.md`; spawn non-blocking `npm run build:glossary-index`; return `{ term, inserted_at, graph_regen_triggered: true }`. |
| T11.2 | glossary_row_update | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/glossary-row-update.ts` via `wrapTool` + `checkCaller`: fuzzy-then-exact term match against glossary index; apply `patch` fields (`definition`, `spec_reference`, `category`); write back; spawn non-blocking regen; term not found → `{ ok: false, error: { code: "issue_not_found", hint: "Use glossary_row_create." } }`. |
| T11.3 | spec_section_append | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/spec-section-append.ts` via `wrapTool` + `checkCaller`: validate `spec` exists via `list_specs`; call `spec_outline` to check heading uniqueness (duplicate heading → `invalid_input`); append new section markdown to bottom of spec file; spawn non-blocking `npm run build:spec-index`; return `{ spec, heading, appended_at }`. |
| T11.4 | rule_create + authorship tests | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/rule-create.ts` via `wrapTool` + `checkCaller`: validate `path` under `ia/rules/`; check file uniqueness; write file with required frontmatter; return `{ path, created_at }`. Tests for all 4 authorship tools: happy paths; unauthorized caller → `unauthorized_caller`; cross-ref validation failure → `invalid_input` with nearest-match hint; duplicate guard. |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_pending — populated by `/audit {{this-doc}} Stage {{N.M}}` once all Tasks reach Done post-verify._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
