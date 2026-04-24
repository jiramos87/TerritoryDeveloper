### Stage 9 — UI surfaces + CityStats integration + economy-system reference spec / `economy-system.md` reference spec + closeout alignment

**Status:** Done

**Objectives:** Author the new `ia/specs/economy-system.md` reference spec (invariant #12 — covers permanent domain). Glossary rows authored in Steps 1/2 get proper authoritative spec cross-refs. Run full validation + confirm umbrella rollout tracker row ready to advance to column (f) "≥1 task filed" on first `/stage-file` call. Final stage — nothing player-visible added, but spec + docs + alignment land.

**Exit:**

- `ia/specs/economy-system.md` authored with sections: Overview, Zone S (enum + sub-type registry + placement pipeline), Budget envelope (`IBudgetAllocator` contract + `TryDraw` semantics + monthly reset), Treasury floor clamp (hard cap), Bond ledger (`IBondLedger` contract + single-concurrent rule + arrears state), Maintenance contributor registry (`IMaintenanceContributor` + deterministic iteration), Save schema v3→v4 migration, Glossary cross-refs.
- All glossary rows added in Steps 1/2 re-point to `ia/specs/economy-system.md` sections (replace placeholder exploration-doc links).
- `ia/rules/agent-router.md` table gets new row(s) for economy / Zone S domain → `economy-system.md` sections.
- `tools/mcp-ia-server/data/spec-index.json` regenerated (captures new spec).
- `npm run validate:all` green.
- Umbrella rollout tracker (`ia/projects/full-game-mvp-rollout-tracker.md`) Bucket 3 row columns (a)–(e) verified complete; column (g) align gate closed.
- Phase 1 — Author `economy-system.md` + glossary repointing + router table update.
- Phase 2 — Index regen + full validation + umbrella alignment.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T9.1 | Author `ia/specs/economy-system.md` | **TECH-593** | Done | New reference spec under `ia/specs/economy-system.md`. Sections per stage Exit list. Follows existing spec authoring conventions (frontmatter, ToC, glossary cross-refs). Cross-references `persistence-system.md` (save) + `managers-reference.md` (Zones). Caveman prose per `ia/rules/agent-output-caveman.md` §authoring. |
| T9.2 | Repoint glossary rows to new spec | **TECH-594** | Done | Update 10 glossary rows added in Steps 1/2 (`Zone S`, `BudgetAllocationService`, `BondLedgerService`, `TreasuryFloorClampService`, `ZoneSService`, `IMaintenanceContributor`, `ZoneSubTypeRegistry`, `IBudgetAllocator`, `IBondLedger`, `envelope (budget)`) — replace exploration-doc placeholder links with `ia/specs/economy-system.md#{anchor}` links. Preserves cross-link integrity. |
| T9.3 | Router-table row for economy domain | **TECH-595** | Done | Update `ia/rules/agent-router.md` routing table: add row(s) mapping task-domain keywords ("zone s", "economy", "budget", "bond", "maintenance") to `economy-system.md` sections. Ensures MCP `router_for_task` dispatches correctly in future agent sessions. |
| T9.4 | Index regen + `validate:all` | **TECH-596** | Done | Run `npm run mcp-ia-index` to regenerate `tools/mcp-ia-server/data/spec-index.json` + `glossary-index.json` + `glossary-graph-index.json`. Run `npm run validate:all`; fix any frontmatter / dead-link issues. Confirm MCP tests pass (`tools/mcp-ia-server/tests`). |
| T9.5 | Umbrella rollout-tracker alignment check | **TECH-597** | Done | Read `ia/projects/full-game-mvp-rollout-tracker.md` Bucket 3 row. Verify columns (a)–(e) marked complete (design-explore → master-plan → stage-file → project-spec-kickoff → glossary rows landed). Verify column (g) align gate closed (spec + router + glossary all pointing to `economy-system.md`). Do NOT tick column (f) — that's `/stage-file` authoring, not this closeout stage. Document state in closeout notes. |

<!-- sizing-gate-waiver: Stage 9 IA-only (spec + glossary + router + index + tracker); multi-subsystem doc touch expected; accepted -->

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- operation: file_task
  reserved_id: ""
  issue_type: TECH
  title: "Author `ia/specs/economy-system.md`"
  priority: medium
  notes: |
    New **reference spec** under `ia/specs/economy-system.md` per Stage 9 Exit: Zone S, **BudgetAllocationService** / **IBudgetAllocator**, **TreasuryFloorClampService**, **BondLedgerService** / **IBondLedger**, **IMaintenanceContributor**, save v3→v4, glossary-style cross-refs to **persistence-system** + **managers-reference**. Caveman authoring per `agent-output-caveman.md`.
  depends_on: []
  related:
    - "TECH-594"
    - "TECH-595"
    - "TECH-596"
    - "TECH-597"
  stub_body:
    summary: |
      Land authoritative **economy-system** reference spec: Zone S enums, sub-type registry, placement pipeline, budget envelope semantics, treasury floor, bond ledger contracts, maintenance contributors, save migration notes — aligns glossary rows from prior stages.
    goals: |
      - `ia/specs/economy-system.md` exists with Overview + sections listed in Stage 9 Exit (Zone S, budget, bonds, maintenance, save).
      - Cross-refs to **persistence-system**, **managers-reference**, **isometric-geography** where relevant; no orphan anchors.
      - Frontmatter + ToC match existing `ia/specs/` conventions.
    systems_map: |
      - `ia/specs/economy-system.md` (new), `ia/specs/glossary.md`, `ia/specs/persistence-system.md`, `ia/specs/managers-reference.md`
    impl_plan_sketch: |
      Phase 1 — Outline sections from Stage Exit checklist. Phase 2 — Fill domain text + cross-refs + validate links locally.

- operation: file_task
  reserved_id: ""
  issue_type: TECH
  title: "Repoint glossary rows to new spec"
  priority: medium
  notes: |
    Update 10 glossary rows (**Zone S**, **BudgetAllocationService**, **BondLedgerService**, **TreasuryFloorClampService**, **ZoneSService**, **IMaintenanceContributor**, **ZoneSubTypeRegistry**, **IBudgetAllocator**, **IBondLedger**, **envelope (budget)**): replace exploration-doc placeholders with `ia/specs/economy-system.md#{anchor}` links; preserve table integrity.
  depends_on:
    - "TECH-593"
  related:
    - "TECH-593"
    - "TECH-595"
    - "TECH-596"
    - "TECH-597"
  stub_body:
    summary: |
      Point economy-related glossary rows at **economy-system** spec sections so MCP + humans resolve authoritative definitions.
    goals: |
      - Each listed row links to correct `economy-system.md` anchor; no stale exploration-only URLs.
      - Glossary table formatting unchanged; `npm run validate:all` glossary-index path green after edits.
    systems_map: |
      - `ia/specs/glossary.md`, `ia/specs/economy-system.md`
    impl_plan_sketch: |
      Phase 1 — Map row → section anchor. Phase 2 — Edit glossary + spot-check `glossary-index` / dead links.

- operation: file_task
  reserved_id: ""
  issue_type: TECH
  title: "Router-table row for economy domain"
  priority: medium
  notes: |
    Extend `ia/rules/agent-router.md` Task → Spec routing: keywords (zone s, economy, budget, bond, maintenance) → `economy-system.md` section slices. Keeps **router_for_task** + agent-router table aligned.
  depends_on:
    - "TECH-593"
  related:
    - "TECH-593"
    - "TECH-594"
    - "TECH-596"
    - "TECH-597"
  stub_body:
    summary: |
      Add router rows so IA routing sends economy/Zone S work to **economy-system** spec slices (MCP + human agent-router).
    goals: |
      - New table row(s) with keywords + target spec sections.
      - No duplicate or conflicting routes vs existing geography/zones rows.
    systems_map: |
      - `ia/rules/agent-router.md`, `ia/specs/economy-system.md`
    impl_plan_sketch: |
      Phase 1 — Keyword list from Stage Objectives. Phase 2 — Insert rows + verify `router_for_task` doc alignment.

- operation: file_task
  reserved_id: ""
  issue_type: TECH
  title: "Index regen + `validate:all`"
  priority: medium
  notes: |
    Run `npm run mcp-ia-index` (regen `spec-index.json`, glossary indexes). Run `npm run validate:all`; fix frontmatter/dead-link issues; confirm `tools/mcp-ia-server/tests` pass.
  depends_on:
    - "TECH-593"
    - "TECH-594"
    - "TECH-595"
  related:
    - "TECH-593"
    - "TECH-594"
    - "TECH-595"
    - "TECH-597"
  stub_body:
    summary: |
      Regenerate MCP IA indexes after spec + glossary + router land; full repo validation green.
    goals: |
      - `tools/mcp-ia-server/data/spec-index.json` + glossary indexes updated.
      - `validate:all` exit 0; MCP package tests pass.
    systems_map: |
      - `tools/mcp-ia-server/`, `package.json` scripts, `ia/specs/`
    impl_plan_sketch: |
      Phase 1 — `npm run mcp-ia-index`. Phase 2 — `validate:all` + fix any IA drift.

- operation: file_task
  reserved_id: ""
  issue_type: TECH
  title: "Umbrella rollout-tracker alignment check"
  priority: medium
  notes: |
    Verify `ia/projects/full-game-mvp-rollout-tracker.md` Bucket 3 row: columns (a)–(e) complete; column (g) align gate closed. Document findings in spec closeout notes; do not tick column (f) here.
  depends_on:
    - "TECH-596"
  related:
    - "TECH-593"
    - "TECH-594"
    - "TECH-595"
    - "TECH-596"
  stub_body:
    summary: |
      Close-the-loop check vs umbrella rollout tracker: Bucket 3 alignment before program calls column (f) done elsewhere.
    goals: |
      - Tracker row state documented; mismatches filed or noted for umbrella owner.
      - Explicit note that column (f) tick is out of scope for this task.
    systems_map: |
      - `ia/projects/full-game-mvp-rollout-tracker.md`, `ia/projects/zone-s-economy-master-plan.md`
    impl_plan_sketch: |
      Phase 1 — Read tracker + compare to repo. Phase 2 — Write findings into Task spec §Verification / Decision Log as needed.
```

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

---
