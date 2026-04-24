### Stage 6 — Envelope Foundation (Breaking Cut) / Caller Sweep + Snapshot Tests + CI Gate


**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Sweep all lifecycle skill bodies, agent bodies, and docs for legacy param aliases and bare tool-recipe sequences; author snapshot test fixtures for all 32 tools; add `validate:mcp-envelope-shape` CI script; bump to v1.0.0 with rollback note.

**Exit:**

- `npm run validate:mcp-envelope-shape` exits 0 (no bare non-envelope returns in `src/tools/*.ts`).
- `tools/mcp-ia-server/tests/envelope.test.ts` snapshots exist for all 32 tools.
- All `ia/skills/**/SKILL.md` tool-recipe sections reference canonical param names; no `section_heading`/`key`/`doc`/`maxChars` in any skill/agent/doc.
- `docs/mcp-ia-server.md` updated with alias-drop migration note + new tools from Stage 2.2 (`rule_section`).
- `tools/mcp-ia-server/package.json` at `1.0.0`.
- Phase 1 — Snapshot tests + caller sweep.
- Phase 2 — CI gate + release prep.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.1 | Snapshot tests | _pending_ | _pending_ | Author `tools/mcp-ia-server/tests/envelope.test.ts` — one `ok: true` + one `ok: false` fixture per tool (input → output JSON); cover alias-rejection responses, `db_unconfigured`, partial-batch shape. Run `npm run validate:all` post-regen to confirm no regressions. |
| T6.2 | Caller sweep | _pending_ | _pending_ | Grep `\b(spec_section\ | spec_sections\ | router_for_task\ | invariants_summary\ | glossary_lookup\ | glossary_discover)\b` across `ia/skills/**/SKILL.md`, `.claude/agents/**/*.md`, `ia/rules/**/*.md`, `docs/**/*.md`, `CLAUDE.md`, `AGENTS.md`; replace legacy aliases + bare patterns with canonical params + envelope-aware call patterns; update 8+ lifecycle skill tool-recipe sections to note composite first (Step 3). |
| T6.3 | CI envelope-shape script | _pending_ | _pending_ | Author `tools/scripts/validate-mcp-envelope-shape.mjs` — greps `tools/mcp-ia-server/src/tools/*.ts` for function bodies that `return {` without `wrapTool`; exits non-zero if found. Add `"validate:mcp-envelope-shape"` to root `package.json` scripts + add to `validate:all` composition. |
| T6.4 | Release prep v1.0.0 | _pending_ | _pending_ | Bump `tools/mcp-ia-server/package.json` to `1.0.0`; add `CHANGELOG.md` entry `v1.0.0 — Breaking: unified ToolEnvelope, alias removal, structured prose tools, partial-result batch`; include migration table (alias → canonical); note rollback path (`git revert <merge-sha>`) and pre-envelope tag `mcp-pre-envelope-v0.5.0`. |

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
