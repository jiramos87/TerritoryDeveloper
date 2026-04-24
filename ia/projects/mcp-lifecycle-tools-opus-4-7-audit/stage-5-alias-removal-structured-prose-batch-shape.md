### Stage 5 — Envelope Foundation (Breaking Cut) / Alias Removal + Structured Prose + Batch Shape


**Status:** Final (2026-04-18)

**Backlog state (Stage 2.3):** 4 filed, all Done (archived) — TECH-426 / TECH-427 / TECH-428 / TECH-429

**Objectives:** Hard-remove all legacy param aliases from `spec_section`, `spec_sections`, `project_spec_journal_*`; convert `rule_content` to structured payload + `markdown` side-channel; implement partial-result batch schema for `spec_sections` and `glossary_lookup (terms)`.

**Exit:**

- `spec_section({ section_heading: "..." })` → `{ ok: false, error: { code: "invalid_input", message: "Unknown param 'section_heading'. Canonical: 'section'." } }`.
- `spec_sections` returns `{ results: {[key]: ...}, errors: {[key]: ...}, meta.partial }` — one bad key does not fail whole batch.
- `rule_content` returns `{ rule_key, title, sections: [{id, heading, body}], markdown?: string }`.
- `glossary_lookup({ terms: [...] })` returns partial-result shape (from Stage 1.1, now wrapped).
- Phase 1 — Alias removal + structured rule_content.
- Phase 2 — Partial-result batch shape.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T5.1 | Drop spec_section aliases | **TECH-426** | Done (archived) | Remove alias params from `spec-section.ts` Zod schema: `key`/`doc`/`document_key` → reject with `invalid_input` (hint: "Use 'spec'"); `section_heading`/`section_id`/`heading` → reject (hint: "Use 'section'"); `maxChars` → reject (hint: "Use 'max_chars'"). Same cleanup for `spec-sections.ts` and `project-spec-journal.ts` journal-search params. |
| T5.2 | Structured rule_content | **TECH-427** | Done (archived) | Convert `rule-content.ts` response to `{ rule_key, title, sections: [{id, heading, body}], markdown?: string }` — parse headings from rule markdown; `markdown` side-channel = raw file text. Ensures `rule_section` tool (T2.2.2) has a structured base to slice. |
| T5.3 | Batch partial-result — spec_sections | **TECH-428** | Done (archived) | Refactor `spec-sections.ts` to return `{ results: {[spec_key]: {sections: [...]}}, errors: {[spec_key]: {code, message}}, meta: {partial: {succeeded, failed}} }`. One bad input key → `errors[key]`, rest still succeed; envelope `ok: true` when ≥1 succeeds. |
| T5.4 | Batch partial-result — glossary_lookup | **TECH-429** | Done (archived) | Wire partial-result shape for `glossary_lookup({ terms: [...] })` (handler extended in Stage 1.1) through the Stage 2.2 envelope wrapper; ensure `meta.partial` propagates to `EnvelopeMeta`; single-`term` path still returns unwrapped `GlossaryEntry` in `payload`. |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
