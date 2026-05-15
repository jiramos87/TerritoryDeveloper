# Stage 6 — game-ui-catalog-bake Findings (live log)

Caveman. Append-only. Newest entry on top. Fast agent resume during stage-authoring + ship-stage loop.

---

## Session start (2026-05-05)

Operator directive: run `/stage-authoring game-ui-catalog-bake Stage 6` then `/ship-stage-main-session game-ui-catalog-bake Stage 6`. Register findings + decisions here.

Pending entries below as phases complete.

---

## stage-authoring run (2026-05-05, Opus 4.7)

### Premise drift detected — CRITICAL

Stage 6 master-plan stub names targets that do not exist in HEAD:

| Stub literal | HEAD reality |
|---|---|
| `Assets/Scripts/UI/IRRenderer.cs` | absent (Glob `Assets/Scripts/UI/**/IRRenderer*.cs` empty) |
| `Assets/Scripts/UI/**/ImmediateRenderer*.cs` | absent (same) |
| `Immediate-Render (IR)` row in `ia/specs/glossary.md` | absent (grep returns 0 hits in glossary; only `IR archetype` mentions exist, referring to Intermediate Representation) |
| `ia/rules/*ir*.md` IR-bake rule files | only substring matches (none are IR-bake rules) |
| `tools/tests/validate-no-ir-bake-runtime-refs.test.mjs` | absent (red test must be created by TECH-11940) |
| EditMode anchor `IRRetiredNoDoubleRenderTest.cs::IRRenderersRemovedFromUIAssembly` (Stage `red_test_anchor`) | absent + targets phantom `IRRenderer` type |

Root cause: stub author conflated `IR` (Intermediate Representation, real) with `IR` (Immediate-Render, fictional). DEC-A24 §6 + fork 6 D6 in `docs/game-ui-catalog-bake-exploration.md` Decision log are canonical.

Reinterpretation locked in §Plan Digest: real Stage 6 = claude-design IR JSON pipeline demotion (D6 = sketchpad-only). Both digests anchor every stub-vs-DEC-A24 conflict in §Pending Decisions per rubric #3.

### Per-task locked decisions

#### TECH-11940 (Strip IR runtime hooks → claude-design IR JSON runtime severance)

| Decision | Choice | Source signal |
|---|---|---|
| target IR pipeline | claude-design IR JSON (Intermediate Representation) | DEC-A24 §6 + fork 6 D6 explicit; Glob confirms no `IRRenderer` symbol exists |
| demotion shape | sketchpad-only (D6) | exploration doc Decision log row D6 (2026-05-04) |
| runtime entry point | `Assets/Scripts/Editor/Bridge/UiBakeHandler.Frame.cs` `layout-rects.json` reads | only confirmed Unity-side reader of `web/design-refs/step-1-game-ui/*.json` |
| `layout-rects.json` runtime path | route through asset-pipeline catalog snapshot | DEC-A24 §3 D2 (snapshot is truth) |
| transcribe scripts disposition | orphan (files retained, removed from runtime + `validate:all` wiring) | D6 retains files; orphan matches "runtime path severed" |
| red test home + name | `tools/tests/validate-no-ir-bake-runtime-refs.test.mjs::NoRuntimeRefsToIrBakeHandler` | DEC-A24 §6 names `npm-test` command_kind + this exact anchor |
| Stage stub EditMode anchor (`IRRenderersRemovedFromUIAssembly`) | SUPERSEDED by npm-test anchor above | stub anchor targets phantom `IRRenderer` type |
| glossary edit | NOT IN SCOPE (handled by TECH-11941) | sibling-Task split |

#### TECH-11941 (Doc + glossary cleanup → IR-JSON advisory README + decisions.md change-log)

| Decision | Choice | Source signal |
|---|---|---|
| target IR pipeline (cleanup scope) | claude-design IR JSON | identical to TECH-11940 reasoning |
| glossary row edit | NO-OP (zero matching rows) | grep `Immediate-Render` against `ia/specs/glossary.md` returns 0 hits |
| `web/design-refs/step-1-game-ui/README.md` content | short banner naming D6 + Stage 6 + cross-link to exploration doc §6 | D6 explicit "files retained, runtime path severed, dirs flagged advisory in README" |
| `web/design-refs/README.md` umbrella scope | covers `step-1-game-ui/` + `step-1-game-ui-v2/` + `step-8-console/` | all three are claude-design sketchpad surfaces |
| `ia/skills/ui-fidelity-review/SKILL.md` disposition | update IR-pipeline mentions to point at catalog-bake (or RETIRED-mark per audit) | skill is live; mentions are stale references |
| exploration doc Conclusion edit | append literal `IR demoted — see master plan Stage 6` under existing §Decision log | stub directive locks the literal line |
| `ia/rules/*ir*.md` cleanup | NO-OP (no IR-rule files exist) | glob hits are coincidental substring matches |

### Drift warnings (`drift_warnings: true`)

1. Master-plan Stage 6 `red_test_anchor` field is stale — points to phantom EditMode test. TECH-11940 §Work Items includes a step to update the master-plan body to the canonical npm-test anchor. Surfaces at Pass A entry-gate capture if not handled first.
2. Stage stub author premise inversion — stub conflates Intermediate Representation with Immediate-Render. Stub author should be re-prompted with DEC-A24 §6 verbatim before next stage-file pass.
3. `BACKLOG.md` row text for TECH-11941 currently reads `Doc + glossary cleanup`; cleanup is doc-only after re-scoping (no glossary row exists). TECH-11941 §Work Items includes a sibling-row sanity check.

### Section overrun counters (`n_section_overrun`)

| Task | Section | Bytes | Cap | Status |
|---|---|---|---|---|
| TECH-11940 | §Pending Decisions | 2692 | 1500 | OVERRUN (warn-only) |
| TECH-11940 | §Implementer Latitude | 929 | 800 | OVERRUN (warn-only) |
| TECH-11940 | §Goal | 260 | 400 | OK |
| TECH-11940 | §Acceptance | 887 | 1500 | OK |
| TECH-11940 | §Work Items | 1360 | 2000 | OK |
| TECH-11941 | (all sections within caps) | — | — | OK |

Overruns justified by mandatory phantom-stub correction rationale rows (rubric #3 = LOCKED rationale required). Per rubric #10, soft caps emit warnings, do NOT abort.

### DB writes

| Task | history_id | heading_normalized | readback ok |
|---|---|---|---|
| TECH-11940 | 2935 | false | true |
| TECH-11941 | 2936 | false | true |

`validate:master-plan-status` clean (0 plans checked, 0 drift rows).

### Hand-off summary

```
stage-authoring done. STAGE_ID=6 AUTHORED=2 SKIPPED=0 (split: 1 sub-pass)
Per-Task:
  TECH-11940: §Plan Digest written (7 work items, 6 acceptance rows, 8 decisions LOCKED, 4 latitude rows, 4 test intents); fold: 0/0; section_overrun=2 (PendingDecisions+ImplementerLatitude); n_heading_normalized=0; n_unresolved_decisions=0.
  TECH-11941: §Plan Digest written (6 work items, 7 acceptance rows, 7 decisions LOCKED, 3 latitude rows, 3 test intents); fold: 0/0; section_overrun=0; n_heading_normalized=0; n_unresolved_decisions=0.
drift_warnings: true (premise drift between Stage stub literals and DEC-A24 §6 — see findings)
DB writes: 2 task_spec_section_write OK; 0 unchanged; 0 heading_normalized.
next=stage-authoring-chain-continue
```

### Recommended next step

Before `/ship-stage`, update master-plan Stage 6 body (`red_test_anchor`, Objectives, Exit criteria, Tasks intent column) to align with DEC-A24 §6. Otherwise Pass A entry-gate captures the phantom EditMode anchor and halts.

---

## ship-stage-main-session run (2026-05-05, Opus 4.7)

### Operator decision

Polled "Patch stub + ship" via AskUserQuestion. Patched master-plan Stage 6 body inline (red_test_anchor → npm-test, Objectives + Exit criteria realigned to DEC-A24 §6 D6, Tasks intent column re-scoped). Then drove Pass A → Pass B inline (no subagents).

### Pass A — implement loop (NO commits)

| Task | Outcome | Status flip | Compile gate |
|---|---|---|---|
| TECH-11940 | Deleted `Assets/Scripts/UI/Bridge/LayoutRectsLoader.cs` (+ .meta); scrubbed `UiBakeHandler.Frame.cs` `layout-rects.json` reader path; created `tools/tests/validate-no-ir-bake-runtime-refs.test.mjs`; wired `validate:no-ir-bake-runtime-refs` script in `package.json`; collateral scrubs in `UiBakeHandler.cs/.Archetype.cs`, `ThemedPanel.cs`, `UiTheme.cs`, `UiThemeIconPopulator.cs`, `AgentBridgeCommandRunner.Conformance.cs` | `pending → implemented` (prior session) | `unity:compile-check` PASS |
| TECH-11941 | Created `web/design-refs/README.md` umbrella banner; created `web/design-refs/step-1-game-ui/README.md` per-folder banner; appended Conclusion line `IR demoted — see master plan Stage 6` to `docs/game-ui-catalog-bake-exploration.md`; prepended RETIRED banner to `ia/skills/ui-fidelity-review/SKILL.md`; added `validate:no-ir-bake-runtime-refs` to `tools/scripts/validate-fast-path-map.json` baseline; appended DEC-A24 change-log row id=46622 via `arch_changelog_append` | `pending → implemented` @ 14:18:14Z | n/a (doc-only) |

### Pass B — verify-loop + closeout

#### 6.1 verify-loop

| Validator | Result | Notes |
|---|---|---|
| `validate:no-ir-bake-runtime-refs` | PASS | Red test green — no runtime refs to IR-bake surfaces remain |
| `validate:fast-coverage` | PASS (after fix) | Initial gap: `validate:no-ir-bake-runtime-refs` not in path-map. Fixed by adding to `baseline[]` in `validate-fast-path-map.json` |
| `generate:ia-indexes` | PASS | IA indexes regenerated |
| `validate:web:conditional` | OUT-OF-SCOPE FAILURES | 42 web test failures (panel-child-set, panels-children, refs-repo, catalog versions route). Verified pre-existing on clean HEAD via stash + retest. Not Stage 6 regressions. Treated as baseline rot — escalate as separate cleanup. |

verdict = `pass` (Stage 6 surfaces green; pre-existing baseline rot acknowledged + scoped out).

#### 6.2 verified → done flips

| Task | verified at | done at |
|---|---|---|
| TECH-11940 | 2026-05-05T14:20:37.296Z | 2026-05-05T14:20:40.365Z |
| TECH-11941 | 2026-05-05T14:20:37.473Z | 2026-05-05T14:20:40.685Z |

### Phase 7 — inline closeout

| MCP call | Result |
|---|---|
| `stage_closeout_apply(slug=game-ui-catalog-bake, stage_id=6)` | OK — archived_task_count=2; stage_status=done |
| `master_plan_change_log_append(kind=stage_closed)` | entry_id=2437 @ 14:21:02Z |

### Phase 8 — stage commit + verification record

(populated post-commit)

### Drift / friction signal

1. Stub-vs-DEC-A24 premise inversion was the entire Stage 6 risk surface. Patching the stub before Pass A unblocked everything; without the patch, Pass A entry-gate would have halted on the phantom EditMode anchor.
2. `validate:fast-coverage` meta-gate caught the missing path-map row immediately — TECH-12640 plumbing working as designed.
3. Web baseline rot is independent of Stage 6 scope. Recommend separate `/project-new` for refs-repo + panel-child-set test rehab.
4. ui-fidelity-review SKILL retirement signaled by Stage 6 closeout — RETIRED banner is the lightweight path; full skill teardown deferred until consumers re-pointed.

### Hand-off summary

```
SHIP_STAGE 6: PASSED (after stage commit + verification flip)
  slug          : game-ui-catalog-bake (Game UI Catalog Bake)
  tasks shipped : 2 (TECH-11940, TECH-11941)
  stage commit  : (post-commit)
  stage verify  : pass
```

