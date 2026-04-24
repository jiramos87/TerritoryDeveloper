### Stage 13 — Skill patches + plan consumers / Read skills (author / implement)

**Status:** Draft (tasks _pending_ — not yet filed; T13.1 + T13.3 cancelled by M6 collapse)

**Objectives:** Teach the surviving live read skills to consume `surfaces` / `mcp_slices` / `skill_hints` from yaml before round-tripping `router_for_task` / `spec_section`. Append-only `surfaces` guardrail fires inside `plan-author` (absorbs the retired `project-spec-kickoff` surface-reading path per M6 collapse). Plan-row-flip is now owned by the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode stage-closeout), already MCP-driven; no separate patch needed. All optional — fallbacks (router / grep) kept for MCP-unavailable + field-absent cases.

**Exit:**

- `ia/skills/plan-author/SKILL.md` — reads `surfaces` / `mcp_slices` / `skill_hints` FIRST during Stage-bulk §Plan Author authoring; append-only guardrail on `surfaces` in §4 / §5.2 regions (never reorder / rewrite / drop). Guardrail documented + enforced via validator warning.
- `ia/skills/project-spec-implement/SKILL.md` — `skill_hints` consumed as routing hint (advisory, not mandate); doc notes hint NOT enforced on drift (N5 policy).
- `parent_plan_validate` gains a `surfaces`-guardrail check — warns on reorder / rename / drop relative to last-seen state (tracked via content hash in yaml + new optional field `surfaces_hash` OR just warns on any diff vs plan's Relevant-surfaces block).
- Phase 1 — plan-author + implement patches.
- Phase 2 — surfaces-guardrail validator extension + fixtures.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T13.1 | ~~Patch kickoff~~ | Cancelled (obsolete) | Cancelled | Retired surface `project-spec-kickoff`. Functionality absorbed into `plan-author` Stage-bulk authoring — surface-reading patch should attach to `plan-author/SKILL.md` spec-section-load step instead. File as replacement task if still desired. |
| T13.2 | Patch implementer — skill_hints as advisory | _pending_ | _pending_ | Edit `ia/skills/project-spec-implement/SKILL.md` routing step — consume `skill_hints` from yaml as advisory suggestion; document fallback to `router_for_task` when empty; explicitly non-binding per N5 (hint, not mandate). |
| T13.3 | ~~Patch close skill — MCP plan-row flip~~ | Cancelled (obsolete) | Cancelled | Retired surface `project-spec-close`. Plan-row-flip now owned by `plan-applier` Mode stage-closeout (Stage-scoped `/closeout` pair), already calls `master_plan_locate` — patch not needed. |
| T13.4 | Surfaces-guardrail validator check | _pending_ | _pending_ | Extend `tools/mcp-ia-server/src/parser/parent-plan-validator.ts` (from T3.3.1) with a `surfaces` append-only check — warn when yaml `surfaces` list reorders / drops / renames entries relative to the last-written order (computed by storing a `surfaces_hash` in yaml OR diff-parsing the yaml history — pick during implementation). Warning, not error. |
| T13.5 | Fixture test for surfaces guardrail | _pending_ | _pending_ | Add fixtures under `tools/scripts/test-fixtures/surfaces-guardrail/` — `append-ok/`, `reorder-warn/`, `drop-warn/`, `rename-warn/`. Extend `parent-plan-validate.test.ts` to assert warning outputs per fixture. Matches exploration Example 4. |
