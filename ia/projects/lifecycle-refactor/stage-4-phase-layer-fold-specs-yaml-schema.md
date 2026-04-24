### Stage 4 — Data Migration: Master Plans + Backlog Schema / Phase Layer Fold: Specs + YAML Schema

**Status:** Done

**Objectives:** Remove Phase frontmatter from all open project specs. Remove `phase` field from all backlog yaml records. Regenerate BACKLOG views without Phase column. Validate no orphan specs or broken yaml after schema update.

**Exit:**

- All open `ia/projects/{ISSUE_ID}.md`: `parent_phase:` frontmatter line absent; `parent_stage:` present with correct value.
- All `ia/backlog/*.yaml`: `phase:` field absent; `parent_stage:` present; `id:` + counter untouched.
- `BACKLOG.md` regenerated: Phase column absent from all rows.
- `validate:dead-project-specs` passes (no orphan specs).
- `npm run validate:all` passes.
- Migration JSON M3 flipped to `done`.
- Phase 1 — Spec frontmatter fold + dead-spec validate.
- Phase 2 — YAML schema update + BACKLOG regen.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T4.1 | Fold parent_phase from open specs | **TECH-454** | Done (archived) | For each open `ia/projects/{ISSUE_ID}.md` (read list from migration JSON M3.specs array): remove `parent_phase:` line; ensure `parent_stage:` is set to old `(parent_step, parent_stage)` concatenated as `"{step}.{stage}"`; leave all body content (§Implementation, §Verification, §Audit etc.) untouched; update migration JSON M3.specs per-file to `done` immediately after edit. |
| T4.2 | Validate dead-spec + spec frontmatter | **TECH-455** | Done (archived) | Run `npm run validate:dead-project-specs`; fix any orphan specs flagged (spec file with no matching yaml entry in `ia/backlog/`); run `npm run validate:frontmatter` on all modified spec files; confirm no `parent_phase` field remains in any open spec. |
| T4.3 | Drop phase field from backlog yaml | **TECH-456** | Done (archived) | For each `ia/backlog/*.yaml` (read list from migration JSON M3.yaml array): remove `phase:` field; set `parent_stage:` to correct stage id; update `tools/mcp-ia-server/src/parser/` backlog-schema expectation to not require `phase` field (check `ia/templates/frontmatter-schema.md` + any schema validation in parser for field allowlist); update migration JSON M3.yaml per-file to `done`. |
| T4.4 | BACKLOG regen + M3 flip | **TECH-457** | Done (archived) | Run `bash tools/scripts/materialize-backlog.sh`; verify `BACKLOG.md` + `BACKLOG-ARCHIVE.md` emit without Phase column; run `npm run validate:all`; flip migration JSON M3 `done`. |

---
