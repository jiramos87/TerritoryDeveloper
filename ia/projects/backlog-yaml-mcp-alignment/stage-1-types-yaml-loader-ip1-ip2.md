### Stage 1 — HIGH band (IP1–IP5) / Types + yaml loader (IP1 + IP2)

**Status:** Final

**Backlog state (Stage 1.1):** 7 filed

**Objectives:** Land the `ParsedBacklogIssue` shape extension + yaml-loader field mapping. Fix the soft-dep marker preservation bug (correctness). Resolve the `proposed_solution` fate via a Grep audit and execute the resulting path (drop from the type OR add to yaml schema + loader).

**Exit:**

- `priority`, `related`, `created` present on `ParsedBacklogIssue`; loader maps from yaml; markdown fallback sets sane defaults (`null` / `[]`).
- `depends_on_raw` fallback in loader prefers the yaml source string; only synthesizes from array when source was empty. Soft markers (e.g. `FEAT-12 (soft)`) preserved across round-trip.
- `proposed_solution` Grep audit complete — zero consumers → removed from the type + every read call-site; ≥1 consumer → added to yaml schema via `buildYaml` + loader + validator.
- Tests extended under `tools/mcp-ia-server/tests/**` with fixtures covering: all three new fields present / absent, soft-dep marker preservation, `proposed_solution` presence/absence per decision.
- `npm run validate:all` green.
- Phase 1 — Type + loader extension (three new fields + soft-dep fallback fix).
- Phase 2 — `proposed_solution` decision + execution.
- Phase 3 — Test coverage + downstream payload surfacing.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T1.1 | Extend `ParsedBacklogIssue` shape | **TECH-295** | Done (archived) | Add `priority: string \ | null`, `related: string[]`, `created: string \ | null` to `ParsedBacklogIssue` in `tools/mcp-ia-server/src/parser/backlog-parser.ts` (or the extracted types module if one exists). Update any dependent type exports. No behavior change yet — loader mapping lands in T1.1.2. |
| T1.2 | Map new fields in yaml loader | **TECH-296** | Done (archived) | In `tools/mcp-ia-server/src/parser/backlog-yaml-loader.ts`, `yamlToIssue` sets `priority`, `related`, `created` from yaml record. Markdown-path callers (legacy parser) default to `null` / `[]` when yaml absent. Cover both existing fixtures + at least one new fixture with all three fields. |
| T1.3 | Fix `depends_on_raw` soft-marker fallback | **TECH-297** | Done (archived) | In `backlog-yaml-loader.ts`, replace fallback `depends_on_raw = array.join(", ")` with: prefer yaml source string when non-empty; only synthesize from array when raw is absent. Add fixture with `depends_on: ["FEAT-12"]` + `depends_on_raw: "FEAT-12 (soft)"` and assert `resolveDependsOnStatus` sees the `(soft)` marker. |
| T1.1 | Grep-audit `proposed_solution` consumers | **TECH-298** | Done (archived) | Run `Grep` across repo for reads of `.proposed_solution` / `proposed_solution:` / `"proposed_solution"`. Record the full consumer list in a scratch note on the issue. Decision rule: zero consumers → choose Option A (drop); ≥1 consumer → choose Option B (add to yaml). Record decision + rationale in the spec's §1 of the filed project spec. |
| T1.2 | Execute `proposed_solution` decision | **TECH-299** | Done (archived) | Execute per T1.2.1 decision. **Option A:** remove `proposed_solution` from `ParsedBacklogIssue` + loader + parser + all reads; no yaml schema change. **Option B:** add `proposed_solution?: string` to yaml schema via `buildYaml` in `tools/scripts/migrate-backlog-to-yaml.mjs`, emit in loader, extend validator schema, update at least one fixture. Either option lands tests for the chosen behavior. |
| T1.1 | Surface new fields in `backlog_issue` + `backlog_search` payloads | **TECH-300** | Done (archived) | In `tools/mcp-ia-server/src/tools/backlog-issue.ts` + `backlog-search.ts`, return the three new fields (`priority`, `related`, `created`) in the MCP response payload. No new filters here (IP9 adds them). Snapshot-update existing tests. |
| T1.2 | Round-trip soft-dep marker integration test | **TECH-301** | Done (archived) | Integration test under `tools/mcp-ia-server/tests/tools/` — load a yaml fixture with `depends_on_raw: "FEAT-12 (soft)"`, call `parseBacklogIssue` + `resolveDependsOnStatus`, assert `soft_only: true`. Plain-id counter-fixture asserts `soft_only: false`. `[optional]` deferred (parser has no classifier — see TECH-301 §OpenQ1). Prevents regression of the loader bug. |
