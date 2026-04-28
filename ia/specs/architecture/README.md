# Architecture sub-spec index

Index for `ia/specs/architecture/**`. Doc-primary, DB-indexed (DEC-A10). Edit markdown — DB rows track relations.

## Sub-spec roles

- [`layers.md`](layers.md) — system layers + helper services + full dependency map.
- [`data-flows.md`](data-flows.md) — init order, simulation tick, player input, persistence, water, isometric geography.
- [`interchange.md`](interchange.md) — agent IA + MCP, JSON interchange, Postgres bridge contracts (B1/B3/P5), local verification, MCP tool catalog stub.
- [`decisions.md`](decisions.md) — DEC-A1..N table-driven decisions + trade-offs + rationale + alternatives.

## Lifecycle

1. Edit sub-spec under `ia/specs/architecture/**`.
2. Commit triggers `arch_changelog` row append (`spec_edit_commit` kind, resolved via `arch_surfaces.spec_path`). Validator step runs inline in `validate:arch-coherence`.
3. New decision row authored via `/design-explore` `Architecture Decision` phase (DEC-A15) — phase polls user for slug/rationale/alternatives/affected-surfaces, writes `arch_decisions` + `arch_changelog`, calls `arch_drift_scan` against open master plans.
4. Drift report appended inline to exploration doc under `### Architecture Decision` block; relentless human polling per affected Stage (DEC-A14 — never auto-rewrite plans).
5. Decisions persist to plan Change log entries + DEC table.

## DB tables (per DEC-A16)

- `arch_surfaces` — canonical inventory of architectural surfaces (slug + spec_path + role).
- `arch_decisions` — DEC-A* rows mirrored from `decisions.md` table; status active until superseded.
- `arch_changelog` — append-only history (kind: `spec_edit_commit`, `design_explore_decision`, `manual_supersede`).
- `stage_arch_surfaces` — link table joining `ia_stages` ↔ `arch_surfaces.slug` (DEC-A12 — stage-level grain).

## Cross-references

### MCP tools

- `arch_decision_get` — fetch one DEC row by slug.
- `arch_decision_list` — list active DEC rows.
- `arch_surface_resolve` — resolve `spec_path` → `arch_surfaces` slug.
- `arch_drift_scan` — scan open master plans for drift vs current arch state.
- `arch_changelog_since` — cursor-based changelog walk since given commit_sha.
- `arch_decision_write` (planned, T1.4.1) — INSERT new DEC row (status=active).
- `arch_changelog_append` (planned, T1.4.1) — append audit row.

### Skills

- [`ia/skills/arch-drift-scan/SKILL.md`](../../skills/arch-drift-scan/SKILL.md) — on-demand drift scan command.
- [`ia/skills/design-explore/SKILL.md`](../../skills/design-explore/SKILL.md) — `Architecture Decision` phase (per DEC-A15) for new decision authoring.

### Validators

- `npm run validate:arch-coherence` — orphan FK + missing surface checks; runs inline changelog-append for recent arch commits (T1.4.2).
- Wired into `validate:all` chain (T1.4.3).
