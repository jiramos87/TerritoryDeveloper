# Exploration: Spec-driven pipeline — Skills, MCP tools, scripts, and TDD integration

**Date:** 2026-04-04
**Scope:** Analysis of the territory-developer spec-driven development pipeline (5 Cursor Skills, 12 MCP tools, ~8 npm scripts) with proposals for gaps, MCP efficiency, script improvements, and TDD integration for a Unity 2D isometric city-builder.

---

## 1. Pipeline overview (as-is)

The current pipeline has five ordered stages, each backed by a Cursor Skill that orchestrates territory-ia MCP calls:

```
project-new → project-spec-kickoff → project-spec-implement → project-implementation-validation → project-spec-close
```

| Stage | Skill | MCP tools used (ordered) | Scripts |
|-------|-------|--------------------------|---------|
| **Create** | `project-new` | glossary_discover → glossary_lookup → router_for_task → spec_section → invariants_summary → backlog_issue | `validate:dead-project-specs` |
| **Enrich** | `project-spec-kickoff` | backlog_issue → invariants_summary → router_for_task → spec_section → glossary_discover → glossary_lookup | — |
| **Implement** | `project-spec-implement` | backlog_issue → invariants_summary → (per phase) router_for_task → spec_section → glossary_discover → glossary_lookup | AGENTS.md Pre-commit Checklist |
| **Validate** | `project-implementation-validation` | (optional backlog_issue) | `validate:dead-project-specs`, `test:ia`, `validate:fixtures`, `generate:ia-indexes --check`, advisory `verify` |
| **Close** | `project-spec-close` | backlog_issue → project_spec_closeout_digest → router_for_task → spec_section/spec_sections → glossary_* → invariants_summary | `closeout:worksheet`, `closeout:dependents`, `closeout:verify`, `validate:dead-project-specs` |

Planned domain guardrail skills (**TECH-45** roads, **TECH-46** terrain/water, **TECH-47** new manager) are designed to compose with `project-spec-implement` but are not yet shipped.

---

## 2. Pipeline gaps and missing steps

### 2.1 No pre-implementation impact analysis

**Problem:** Between kickoff (enrichment) and implementation, there is no systematic step that maps the spec's proposed changes to the subsystems they will affect and checks them against invariants. The agent jumps from "spec is ready" to "start coding."

**Proposal — `project-spec-impact-check` (lightweight, not a new skill; section in kickoff or implement)**

Add a **pre-flight checklist** to the end of `project-spec-kickoff` or the beginning of `project-spec-implement`:

1. For each file in backlog `Files` + Implementation Plan, classify as **read** vs **write**.
2. For each **write** file, list the invariants from `invariants_summary` that could be violated.
3. For files that cross subsystem boundaries (e.g. a road change that touches HeightMap), flag and require the agent to pull both domain slices.
4. Output a short "impact matrix" into the project spec's **Decision Log** or a new **§ Impact Analysis** subsection.

This prevents the common case where an agent starts Phase 1 without realizing Phase 3 will break a shore-band invariant.

### 2.2 No regression guard between phases of `project-spec-implement`

**Problem:** The implement skill says "repeat steps 5–11 for each phase" but there is no checkpoint between phases. If Phase 1 silently breaks something that Phase 2 depends on, the agent only discovers it at the end.

**Proposal — inter-phase validation hook**

After each phase in `project-spec-implement`, the agent should:

1. Re-read the invariants that the phase's files touch (filtered `invariants_summary` or a new MCP tool — see §3.3).
2. If the phase touched MCP/IA/schema paths, run the relevant `project-implementation-validation` steps (today these are deferred to "after all implementation").
3. Record phase exit status in the project spec's **§9 Issues Found**.

### 2.3 No structured "what changed" summary before close

**Problem:** `project-spec-close` starts with `project_spec_closeout_digest` which parses the spec, but it has no view of what the *implementation actually changed* (files, subsystems, invariant surfaces). The IA persistence checklist (G1–I1) is driven by spec prose, not by the diff.

**Proposal — `implementation_diff_summary` MCP tool or script**

A lightweight tool that, given an issue id, scans the git log for commits mentioning that id (or on the current branch) and returns:
- Files changed (grouped by subsystem: Managers, Controllers, Utilities, specs, rules, docs, MCP)
- Invariant surfaces potentially affected (cross-reference file paths with the invariants/guardrails registry)
- Whether MCP/schema/IA-index paths were touched (auto-flags I1)

This would feed into `project-spec-close` step 4 and make IA persistence less reliant on agent recall.

### 2.4 No rollback or "undo phase" guidance

**Problem:** If an implementation phase introduces a bug that is caught late, there is no skill guidance for reverting a phase cleanly. The agent either continues forward or manually reverts.

**Proposal:** Add a short "Phase rollback" section to `project-spec-implement` that says: if a phase fails verification, `git stash` or `git revert` the phase's commits, update **§9 Issues Found**, and re-enter the phase after resolving. Light touch — no new tool needed.

### 2.5 No "dependency freshness" check

**Problem:** `backlog_issue` loads the backlog row, but does not check whether **Depends on** issues are actually completed. An agent could start implementing TECH-39 while TECH-37 is still open.

**Proposal — enhance `backlog_issue` response**

Add a `depends_on_status` field to the `backlog_issue` output:
```json
{
  "depends_on": ["TECH-37"],
  "depends_on_status": [{"id": "TECH-37", "status": "open", "warning": "dependency not completed"}]
}
```
This requires the backlog parser to resolve cited ids — minor extension to `parseBacklogIssue`.

---

## 3. MCP tool improvements

### 3.1 Token reduction: `context_bundle` composite tool

**Problem:** Every skill recipe starts with 3–5 sequential MCP calls (backlog_issue → invariants_summary → router_for_task → spec_section → glossary_discover). Each call costs a round-trip plus the model must decide the next call. For a typical kickoff, this is 6–8 tool calls before any editorial work begins.

**Proposal:** A `context_bundle` tool that accepts:
```json
{
  "issue_id": "TECH-75",
  "include_invariants": true,
  "domains": ["roads", "save"],
  "spec_sections": [{"spec": "geo", "section": "13.4", "max_chars": 2000}],
  "glossary_keywords": ["wet run", "road stroke"]
}
```
Returns a single JSON payload with all results keyed by source. Internally, it reuses existing tool functions (`runSpecSectionExtract`, `rankGlossaryDiscover`, `parseBacklogIssue`, etc.).

**Token savings estimate:** Eliminates 4–6 round trips per pipeline stage, saving ~2000–4000 tokens of tool-call overhead per stage. Over the full 5-stage pipeline, that is 10k–20k tokens saved.

**Risk:** Composite tools can return large payloads. Mitigate with per-section `max_chars` and an overall `max_total_chars` parameter.

### 3.2 Smarter `router_for_task`: accept file paths, not just domain strings

**Problem:** `router_for_task` only matches against domain label strings. Agents frequently mis-phrase domains and get `no_matching_domain`. The `README.md` lesson learned confirms this.

**Proposal:** Accept an optional `files` array (e.g. `["GridManager.cs", "RoadPlacementHelper.cs"]`) and match file names/paths against the backlog's `Files` fields and the router table's known spec scopes. This provides a second matching path that works even when the agent doesn't know the router table vocabulary.

Implementation: maintain a small map of file-pattern → domain (e.g. `Road*.cs` → "Road logic", `Water*.cs` → "Water, terrain", `*Manager*.cs` → "Manager responsibilities"). Return both `domain_matches` and `file_matches`.

### 3.3 Filtered `invariants_for_files` tool

**Problem:** `invariants_summary` returns all 12 invariants and 9 guardrails every time. For an issue that only touches MCP tooling, most of these are irrelevant noise.

**Proposal:** A `invariants_for_files` tool (or parameter on `invariants_summary`) that accepts file paths or domain keywords and returns only the relevant subset:
- HeightMap files → invariants #1, #7, #8, guardrails about HeightMap/Cell.height
- Road files → invariants #2, #10, guardrails about road preparation family
- MCP/tooling files → empty or advisory-only

This reduces tokens for tooling-only issues and focuses the agent on the invariants that matter.

### 3.4 `spec_section` with `include_children` flag

**Problem:** When fetching a top-level section (e.g. `spec_section(geo, "13")`), the tool returns all subsections (13.1–13.5) which may be very large. But sometimes the agent only needs the intro paragraph of §13 without subsections.

**Proposal:** Add `include_children: boolean` (default `true` for backward compatibility). When `false`, return only the content between the heading and its first child heading.

### 3.5 `backlog_issue` with dependency resolution

As described in §2.5 — add `depends_on_status` to the response by parsing cited issue ids from the `Depends on` field and checking their open/closed status in BACKLOG.md.

### 3.6 `project_spec_status` lightweight tool

**Problem:** During implementation, the agent often needs to check: "Is this spec Final? Are there unresolved Open Questions?" This currently requires reading the whole file.

**Proposal:** A tool that returns only the frontmatter (Status, Created, Last updated) and the Open Questions section — much lighter than `read_file` or `project_spec_closeout_digest`.

---

## 4. Script improvements and new scripts

### 4.1 `npm run impact:check -- --issue TECH-75`

A new script that, given an issue id:
1. Reads the project spec's Implementation Plan and Files.
2. Cross-references with `invariants.mdc` to produce a matrix of files × invariants.
3. Outputs Markdown or JSON showing which invariants each phase could affect.

This powers the pre-implementation impact analysis (§2.1).

### 4.2 `npm run diff:summary -- --issue TECH-75` (or `--branch feature/TECH-75`)

A script that examines git history for an issue and produces:
1. Changed files grouped by subsystem.
2. Whether MCP/schema/IA-index paths are in the diff (flags I1 for closeout).
3. Invariant surfaces touched.
4. Summary suitable for inclusion in a PR description.

This powers §2.3 and could also feed `project-spec-close` step 4.

### 4.3 Enhance `closeout:verify` to include diff-based IA checks

Currently `closeout:verify` runs `validate:dead-project-specs` + `generate:ia-indexes --check`. Extend it to also check that if the diff touched `.cursor/specs/glossary.md`, the committed `glossary-index.json` is in sync (already covered by `generate:ia-indexes --check`, but make the error message explicit about which source changed).

### 4.4 `npm run validate:backlog-deps -- --issue TECH-75`

A script that parses the `Depends on` field of a backlog issue and checks whether all cited ids are in Completed status. Emits warnings for open dependencies. Lightweight version of TECH-30 scoped to one issue.

### 4.5 Aggregate validation: `npm run validate:all`

Root script that runs all validation steps in order:
1. `validate:dead-project-specs`
2. `test:ia`
3. `validate:fixtures`
4. `generate:ia-indexes -- --check`

Currently these must be run individually or via `project-implementation-validation`. A single command reduces friction. (Noted as a deferred decision in TECH-52 closure — this is a vote to ship it.)

---

## 5. TDD integration with the spec-driven pipeline

### 5.1 Core idea

Integrate **test definition** into the **kickoff** phase and **test execution** into the **validation** phase:

```
project-new → project-spec-kickoff (+ define test contracts)
    → project-spec-implement (+ write tests as first phase)
    → project-implementation-validation (+ run tests)
    → project-spec-close
```

### 5.2 Where tests are defined

Add a new section to the project spec template:

```markdown
## 7b. Test Contracts (define during kickoff, implement during Phase 1)

<!-- Testable assertions derived from §8 Acceptance Criteria.
     Each test should be:
     - Named with the pattern: Test_{Subsystem}_{Behavior}_{ExpectedOutcome}
     - Mapped to one or more Acceptance Criteria
     - Classified by type (see table below)
-->

| # | Test name | Type | Maps to AC | Description |
|---|-----------|------|------------|-------------|
| 1 | Test_RoadPlacement_BridgeOverWater_HeightPreserved | Node/golden | AC-1 | ... |
| 2 | Test_HeightMap_CellSync_AfterTerraform | Node/golden | AC-2 | ... |
```

### 5.3 Test types by domain

The key challenge is that Territory Developer is a Unity game. Different types of changes require different testing strategies:

#### Type A: Pure computational logic (Node-testable)

**Applies to:** Math functions, coordinate conversions, pathfinding cost calculations, growth ring classification, schema validation.

**Strategy:** Extract pure functions into `tools/compute-lib/` (TECH-37/38 pipeline) and test with `node:test`. Golden JSON fixtures provide regression snapshots.

**Examples:**
- `isometric_world_to_grid(x, y)` → expected grid coordinates
- `pathfinding_cost(from, to, terrain)` → expected cost
- `growth_ring_classify(cells, centroid)` → expected ring assignments
- JSON schema validation of interchange formats

**Pipeline integration:**
- **Kickoff:** Define test contracts with input/output examples.
- **Implement Phase 1:** Write the Node test with the expected output (fails initially — TDD red).
- **Implement Phase N:** Make the test pass (TDD green).
- **Validation:** `npm run test:ia` covers these.

#### Type B: Invariant assertions (scriptable, no Unity runtime)

**Applies to:** HeightMap ↔ Cell.height sync, road cache invalidation, shore band constraints, river monotonicity.

**Strategy:** For each invariant in `invariants.mdc`, create a corresponding assertion function that can be evaluated against a serialized grid state (JSON). These run in Node against golden grid snapshots exported from Unity.

**Examples:**
- Given a grid JSON, assert that for every cell, `heightMap[x][y] == cell.height`.
- Given a grid JSON with water, assert that all shore-band cells have `height <= min(S)` of neighbor water cells.
- Given a road network JSON, assert monotonicity of river bed heights.

**Pipeline integration:**
- **Kickoff:** If the spec touches invariant-relevant subsystems, mandate at least one invariant assertion test.
- **Implement:** Export a "before" grid snapshot via `tools/reports/` (Editor → Reports). Write assertion. Export "after" snapshot. Run assertion.
- **Validation:** Add `npm run test:invariants` to the validation manifest.

**Key enabler:** TECH-38 (extract pure computational modules) must ship to make grid state easily serializable without Unity runtime. Until then, these tests depend on Editor-exported JSON golden files.

#### Type C: Unity behavior (Edit Mode / Play Mode tests)

**Applies to:** Manager interactions, MonoBehaviour lifecycle, scene-level behavior, visual sorting, prefab instantiation.

**Strategy:** Unity Test Framework (UTF) with Edit Mode tests (no Play Mode needed for most logic):
- Edit Mode tests can instantiate MonoBehaviours, call methods, and assert state without running the full game loop.
- Play Mode tests (heavier) for integration scenarios like "place a road, then place water, verify shore refreshes."

**Current state:** No UTF tests exist in the project today. TECH-15/TECH-16 are backlog items for Unity batchmode/test infrastructure. TECH-31 covers scenario/fixture generation.

**Pipeline integration (phased):**

**Phase 1 (now, without UTF):**
- Use "golden file" comparison: before/after JSON exports from Editor → Reports.
- During kickoff, define expected grid state changes as JSON diffs.
- During validation, manually (or via script) compare exported state against expected.

**Phase 2 (after TECH-15/16):**
- Edit Mode tests using NUnit + UnityEngine.TestTools.
- `project-spec-implement` gains a step: "if this phase adds a new public method on a manager, write an Edit Mode test."
- `project-implementation-validation` gains: `unity -batchmode -runTests -testPlatform EditMode` as a manifest step.

**Phase 3 (after TECH-38):**
- Pure computational functions tested in Node (Type A).
- Integration tested via golden JSON fixtures comparing Node output with Unity output.
- UTF Play Mode tests for critical end-to-end paths.

#### Type D: IA/tooling changes (already covered)

**Applies to:** MCP tools, parsers, scripts, CI workflows.

**Strategy:** `node:test` unit tests + `npm run verify` integration test. Already in the pipeline via `project-implementation-validation`.

### 5.4 Practical TDD integration without Unity runtime

Since the project doesn't have UTF infrastructure yet, here's a practical approach for today:

**1. Invariant-as-code assertions (Node)**

Create `tools/invariant-checks/` with one file per invariant:

```typescript
// tools/invariant-checks/heightmap-cell-sync.ts
export function checkHeightMapCellSync(grid: GridState): AssertionResult {
  for (const cell of grid.cells) {
    if (grid.heightMap[cell.x][cell.y] !== cell.height) {
      return { pass: false, cell: [cell.x, cell.y],
               expected: grid.heightMap[cell.x][cell.y], actual: cell.height };
    }
  }
  return { pass: true };
}
```

These consume the same JSON format that `tools/reports/` exports. They can run in CI without Unity.

**2. Golden file tests for behavioral changes**

When a spec changes game behavior (e.g. "shore band should now use max(S) instead of min(S)"):
- During kickoff: define the expected output for a known input grid.
- During implementation: export the actual output from Unity Editor.
- During validation: `npm run test:golden` compares expected vs actual JSON.

**3. Contract tests for MCP tool responses**

Already partially implemented via `verify-mcp.ts`. Extend to cover new tools systematically:
- For each new MCP tool added during implementation, require at least one happy-path and one error-path assertion in `verify-mcp.ts` or the `tests/` suite.

### 5.5 Proposed changes to skills for TDD

**`project-spec-kickoff` additions:**

After the editorial pass (Open Questions, Implementation Plan), add:

> **Test contract pass:** For each Acceptance Criterion in §8, determine which test type (A/B/C/D) is appropriate. Add entries to **§7b Test Contracts** with:
> - Test name, type, mapped AC, and brief description.
> - For Type A: specify input/output JSON shapes.
> - For Type B: specify which invariant(s) and the grid state conditions.
> - For Type C: describe the manual verification or future UTF test.
> - For Type D: specify the npm test command.
>
> If the issue is pure tooling (no game logic), write "N/A — Type D only; covered by `project-implementation-validation`."

**`project-spec-implement` additions:**

Add to Phase 1 or as a Phase 0:

> **Test scaffolding:** Before implementing business logic, write failing tests for the §7b Test Contracts:
> - Type A: `node:test` files under `tools/compute-lib/tests/` or `tools/mcp-ia-server/tests/`.
> - Type B: assertion functions under `tools/invariant-checks/` (if grid JSON exists).
> - Type D: test stubs in `tools/mcp-ia-server/tests/`.
>
> Tests should fail (TDD red). Implementation phases then make them pass (TDD green).

**`project-implementation-validation` additions:**

Add to the validation manifest:

| Step | Command | Notes |
|------|---------|-------|
| 6 | `npm run test:golden` (when golden files exist) | Compare expected vs actual grid JSON |
| 7 | `npm run test:invariants` (when invariant checks exist) | Run invariant assertion functions |

### 5.6 TDD feasibility matrix

| Change type | TDD feasible today? | Enabler needed | Test type |
|-------------|---------------------|----------------|-----------|
| MCP tool / parser / script | **Yes** | — | D (node:test) |
| JSON schema / fixture | **Yes** | — | D (validate:fixtures) |
| Pure math / coordinate / cost | **Yes** (if extracted) | TECH-37/38 | A (node:test + golden) |
| Invariant assertion on grid state | **Partially** (needs grid JSON export) | TECH-38, Editor Reports | B (node assertion on JSON) |
| Manager interaction / lifecycle | **No** (needs UTF) | TECH-15/16 | C (Edit Mode test) |
| Visual sorting / rendering | **No** (needs UTF + visual) | TECH-15/16 + manual QA | C (Play Mode or manual) |
| Full gameplay scenario | **No** (needs UTF) | TECH-31 | C (Play Mode + fixtures) |

---

## 6. Priority recommendations

### Quick wins (low effort, high impact)

1. **Add §7b Test Contracts to project spec template** — Costs nothing; improves every future spec. Even if tests are initially "manual verification: ..." this makes acceptance criteria testable.

2. **Add `depends_on_status` to `backlog_issue` response** — Small parser change; prevents starting work on unmet dependencies.

3. **Ship `npm run validate:all`** — One-liner aggregating existing commands. Removes friction from validation.

4. **Add pre-flight invariant check to `project-spec-implement`** — Text addition to the skill; no new tooling.

### Medium effort, high impact

5. **`context_bundle` MCP tool** — Requires implementing a composite handler reusing existing functions. Significant token savings across all pipeline stages.

6. **`router_for_task` file-path matching** — Moderate parser work; eliminates the most common MCP usage mistake.

7. **`invariants_for_files` filtered tool** — Small new tool; reduces noise for tooling-only issues.

8. **`npm run diff:summary`** — New script; improves closeout quality and PR descriptions.

### Strategic (higher effort, long-term payoff)

9. **Invariant-as-code assertions (`tools/invariant-checks/`)** — Foundational for TDD Type B. Depends on grid JSON export format stabilization.

10. **Golden file comparison infrastructure** — Pairs with TECH-38 extractions. Once compute-lib exists, golden tests become the default regression guard.

11. **UTF Edit Mode test infrastructure (TECH-15/16)** — Unlocks TDD for Unity behavior. Highest effort but transforms quality assurance.

12. **`implementation_diff_summary` for close** — Git-log parsing tool; makes IA persistence more reliable.

---

## 7. Summary of proposed new artifacts

| Artifact | Type | Location | Powers |
|----------|------|----------|--------|
| §7b Test Contracts | Template section | `.cursor/templates/project-spec-template.md` | Kickoff, implement |
| `context_bundle` | MCP tool | `tools/mcp-ia-server/src/tools/context-bundle.ts` | All skills |
| `invariants_for_files` | MCP tool | `tools/mcp-ia-server/src/tools/invariants-for-files.ts` | Implement, close |
| `project_spec_status` | MCP tool | `tools/mcp-ia-server/src/tools/project-spec-status.ts` | Implement |
| `depends_on_status` | Enhancement | `tools/mcp-ia-server/src/parser/backlog-parser.ts` | All skills |
| `router_for_task` file matching | Enhancement | `tools/mcp-ia-server/src/tools/router-for-task.ts` | All skills |
| `npm run validate:all` | Script | `package.json` | Validation |
| `npm run impact:check` | Script | `tools/mcp-ia-server/scripts/impact-check.ts` | Kickoff, implement |
| `npm run diff:summary` | Script | `tools/mcp-ia-server/scripts/diff-summary.ts` | Close, PRs |
| `tools/invariant-checks/` | Test suite | `tools/invariant-checks/` | Validation (TDD Type B) |
| `npm run test:invariants` | Script | `package.json` | Validation |
| `npm run test:golden` | Script | `package.json` | Validation |

---

## 8. Relationship to existing backlog

| Proposal | Related backlog | Status |
|----------|-----------------|--------|
| context_bundle | TECH-48 (MCP discovery) | Open — partially overlaps |
| File-path router | TECH-48 | Open — complementary |
| invariants_for_files | None | New |
| depends_on_status | TECH-30 (validate issue ids) | Open — subset |
| §7b Test Contracts | TECH-31 (scenario fixtures), TECH-35 (invariant fuzzing) | Open — foundational |
| Invariant-as-code | TECH-35, TECH-38 | Open — depends on extraction |
| Golden file tests | TECH-38, TECH-31 | Open — depends on compute-lib |
| UTF infrastructure | TECH-15, TECH-16 | Open — independent |
| validate:all | TECH-52 closure note | Deferred — ready to ship |
| diff:summary | None | New |
