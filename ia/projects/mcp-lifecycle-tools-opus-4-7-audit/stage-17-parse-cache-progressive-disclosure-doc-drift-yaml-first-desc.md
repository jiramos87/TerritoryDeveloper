### Stage 17 — Theme B MCP Surface Remainder (session-token-latency audit extension) / Parse Cache + Progressive Disclosure + Doc Drift + YAML-First + Descriptor Lint


**Status:** Done (2026-04-19)

**Source:** [`docs/session-token-latency-audit-exploration.md`](../../docs/session-token-latency-audit-exploration.md) §Design Expansion — Post-M8 Authoring Shape (Pass 2). Folds 5 independent Theme B items (B4 / B5 / B6 / B8 / B9) into this MCP plan per source doc's Pass 2 directive (Theme B MCP-surface work belongs to MCP-plan authority chain; sibling exploration ships standalone NEW orchestrator for Themes A / C / D-rest / E-rest / F).

**Depends on:**
- Stage 9 T9.4 (Draft) — `docs/mcp-ia-server.md` catalog rewrite must land BEFORE B6 doc-drift lint (T17.3) to avoid lint-fails-during-rewrite churn. T17.3 gated on T9.4 Done; remaining T17.* tasks independent.
- Session-token-latency NEW orchestrator Stage 1 (external dependency) — B1 server-split decision durable before B4 dist-build target chosen; dist output directory name coordinates with server-split output naming.

**Objectives:** Land the MCP-surface-angle remainder from the external 2026-04-19 token-economy + latency audit. B4 adds an on-disk parse cache + switches `.mcp.json` from `tsx`-on-source to compiled `dist/` entry (cold-start 1500 ms → ~200 ms). B5 flips `spec_outline` default to `depth=1` + `list_rules` default to `alwaysApply: true`-only, with opt-in `expand=true` for full payload (1–2k tokens saved per call, breaking change gated by envelope v1.0.0 precedent). B6 adds `validate:mcp-readme` CI lint comparing `registerTool(` count in `src/index.ts` to README table row count. B8 audits `tools/mcp-ia-server/src/parser/backlog-parser.ts` yaml-first call order + adds mtime-keyed manifest cache. B9 adds `validate:mcp-descriptor-prose` lint enforcing `.describe()` ≤120 chars per param. All independent of Stages 1–16 except T17.3's sequencing note on T9.4.

**Exit:**

- `tools/mcp-ia-server/.cache/parse-cache.json` populated on first parse; subsequent parses read from cache when source mtime unchanged (miss → reparse + rewrite).
- `.mcp.json` `args` points to compiled `tools/mcp-ia-server/dist/index.js` (with fallback `tsx` in a dev-env flag path, e.g. `MCP_SOURCE_MODE=1`).
- `spec_outline({ spec: "geo" })` default returns depth=1 heading tree; `spec_outline({ spec: "geo", expand: true })` returns full tree.
- `list_rules({})` default returns only `alwaysApply: true` rules; `list_rules({ expand: true })` returns all rules.
- `npm run validate:mcp-readme` exits 0 when `registerTool(` count == README tool-table row count; exits non-zero (descriptive diff) otherwise. Integrated into `validate:all`.
- `tools/mcp-ia-server/src/parser/backlog-parser.ts` checks `ia/backlog/{id}.yaml` BEFORE falling back to `BACKLOG.md`; manifest cache invalidates on dir mtime change.
- `npm run validate:mcp-descriptor-prose` exits 0 when every `.describe()` call in `src/tools/*.ts` passes a string ≤120 chars; exits non-zero listing offenders. Integrated into `validate:all`.
- `tools/mcp-ia-server/CHANGELOG.md` entry `v1.2.0 — Theme B audit remainder: parse cache + dist build, progressive-disclosure defaults, README drift CI, yaml-first parser cache, descriptor-prose ≤120-char lint`.
- Phase 1 — Performance + cache layer (B4 parse cache + dist; B8 yaml-first manifest cache).
- Phase 2 — Surface-shape + CI gates (B5 progressive disclosure; B6 README drift lint; B9 descriptor-prose lint; v1.2.0 release prep).

**Art:** None.

**Relevant surfaces (load when stage opens):**

- `docs/session-token-latency-audit-exploration.md` §Problem + §Approaches surveyed + §Design Expansion Post-M8 Pass 2 — canonical source for Theme B MCP-surface items.
- `docs/ai-mechanics-audit-2026-04-19.md` — original external audit (B4 = M5, B5 = M6, B6 = M7, B8 = m5, B9 = m6).
- `docs/mcp-ia-server.md` — tool catalog (B6 lint target; T9.4 rewrites this first).
- `tools/mcp-ia-server/src/index.ts` — `registerTool(` call site (B6 drift counter).
- `tools/mcp-ia-server/src/tools/spec-outline.ts` + `tools/mcp-ia-server/src/tools/list-rules.ts` — B5 targets (progressive disclosure defaults).
- `tools/mcp-ia-server/src/tools/*.ts` — B9 lint target (every `.describe()` call site).
- `tools/mcp-ia-server/src/parser/backlog-parser.ts` — B8 yaml-first call order audit.
- `tools/mcp-ia-server/src/parser/markdown-parser.ts` — B4 parse cache integration point.
- `.mcp.json` — B4 dist switch target (currently `tools/mcp-ia-server/node_modules/.bin/tsx` on `src/index.ts`; DEBUG_MCP_COMPUTE=1 already shipped via Theme-0-round-1 TECH issue).
- `tools/mcp-ia-server/package.json` + `tools/mcp-ia-server/dist/` — B4 dist target (dir already exists; build script wiring).
- `tools/mcp-ia-server/CHANGELOG.md` — v1.2.0 release entry.
- Prior stage handoff: Stage 16 (Dry-run Preview, Draft) — dry-run semantics carry over to any new mutations, but Stage 17 tools are read-only + tooling, so no dry-run coupling.

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T17.1 | Parse cache + dist build (B4) | 1 | **TECH-495** | Done (archived) | Author `tools/mcp-ia-server/src/parser/parse-cache.ts` — mtime-keyed JSON cache at `tools/mcp-ia-server/.cache/parse-cache.json`; `readCached(path, mtime)` returns parsed AST on hit, `null` on miss; `writeCached(path, mtime, ast)` persists. Wire into `markdown-parser.ts` `parseDocument()` — cache lookup first, parse on miss, write-through on success. Add `tools/mcp-ia-server/package.json` `"build": "tsc -p tsconfig.build.json"` producing `dist/index.js`; flip `.mcp.json` `args` to `["tools/mcp-ia-server/dist/index.js"]` (preserve `REPO_ROOT` + `DEBUG_MCP_COMPUTE` env). Dev-env fallback: `MCP_SOURCE_MODE=1` env flag swaps args back to `tsx` on source — documented in `CLAUDE.md §2` or server README. Gitignore `.cache/` dir. |
| T17.2 | YAML-first parser + manifest cache (B8) | 1 | **TECH-496** | Done (archived) | Audit `tools/mcp-ia-server/src/parser/backlog-parser.ts` resolution order — confirm `ia/backlog/{id}.yaml` is checked BEFORE `BACKLOG.md` fallback for every id lookup; rewrite any ordering violation. Add manifest cache: read `ia/backlog/` dir mtime at first call per session; cache `{id → yaml-path}` map keyed by mtime; invalidate + re-scan on mtime change. Target: cumulative savings on highest-frequency MCP tool (`backlog_issue`). Unit tests: yaml-first ordering on mixed-state (yaml + archived yaml + BACKLOG-only); manifest cache hit + miss paths; archived-yaml resolution. |
| T17.3 | README drift CI (B6) | 2 | **TECH-497** | Done (archived) | Author `tools/scripts/validate-mcp-readme.mjs` — parse `tools/mcp-ia-server/README.md` tool-table row count; grep `registerTool\(` count in `tools/mcp-ia-server/src/index.ts`; exit non-zero with descriptive diff (missing rows / extra rows) when counts differ. Add `"validate:mcp-readme": "node tools/scripts/validate-mcp-readme.mjs"` to root `package.json` scripts; compose into `validate:all`. **Depends on Stage 9 T9.4 Done** — do not land until catalog rewrite merges, otherwise lint churns against a stale README. Confirm T9.4 complete at `/stage-file` time. |
| T17.4 | Progressive disclosure — spec_outline + list_rules (B5) | 2 | **TECH-498** | Done (archived) | Extend `tools/mcp-ia-server/src/tools/spec-outline.ts` Zod schema with `expand?: boolean` (default `false`); when `false`, filter returned heading tree to depth 1 only; when `true`, return full tree (current behavior). Extend `tools/mcp-ia-server/src/tools/list-rules.ts` input shape with `expand?: boolean` (default `false`); when `false`, filter output rules to those with `alwaysApply: true` in frontmatter; when `true`, return all rules. Update descriptors (B9 budget ≤120 chars). Breaking change — document in CHANGELOG entry + migration note: callers wanting full payload pass `expand: true`. Unit tests: default depth=1 / alwaysApply-only; `expand: true` full payload; unknown spec → existing `spec_not_found` unchanged. |
| T17.5 | Descriptor-prose lint (B9) | 2 | **TECH-499** | Done (archived) | Author `tools/scripts/validate-mcp-descriptor-prose.mjs` — AST-walk (or regex) every `.describe("...")` call in `tools/mcp-ia-server/src/tools/*.ts`; exit non-zero when any description string > 120 chars, listing file + line + length + offending prose. Add `"validate:mcp-descriptor-prose"` to root `package.json` scripts; compose into `validate:all`. Pre-lint pass: shorten known offenders (`unity_bridge_command` param descriptions currently 300+ chars per source-doc B9 note). Unit fixture: synthetic `.ts` file with one ≤120-char description + one 150-char description → lint emits 1 error. |
| T17.6 | Descriptor-prose remediation sweep | 2 | **TECH-500** | Done (archived) | Paired with T17.5 lint: grep `.describe(` across `tools/mcp-ia-server/src/tools/*.ts`; identify every param descriptor >120 chars; trim while preserving param semantics (prefer abbreviation + hint-next-tools pointer over verbose prose); `unity-bridge-command.ts` is the top offender — rewrite its 300+ char param descriptions into ≤120-char primary + structured secondary rendered in tool output rather than descriptor. Run T17.5 lint post-sweep; validate:all green. |
| T17.7 | Release prep v1.2.0 | 2 | **TECH-501** | Done (archived) | Bump `tools/mcp-ia-server/package.json` `version` to `1.2.0`. Append `CHANGELOG.md` entry `v1.2.0 — Theme B audit remainder: parse cache (mtime-keyed) + dist build (.mcp.json switched from tsx to dist); yaml-first parser + manifest cache; progressive-disclosure defaults on spec_outline + list_rules (breaking — callers want full payload pass expand:true); validate:mcp-readme CI lint; validate:mcp-descriptor-prose CI lint ≤120-char per param`. Migration table: `spec_outline` → pass `expand: true` for full tree; `list_rules` → pass `expand: true` for all rules. Advisory tag: `mcp-pre-theme-b-remainder-v1.1.x` pre-commit for rollback target. |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- operation: file_task
  task_key: T17.1
  reserved_id: TECH-495
  title: "Parse cache + dist build (B4)"
  priority: medium
  issue_type: TECH
  notes: |
    Author `tools/mcp-ia-server/src/parser/parse-cache.ts` — mtime-keyed JSON cache at `tools/mcp-ia-server/.cache/parse-cache.json`. Wire into `markdown-parser.ts` `parseDocument()`. Add `"build": "tsc -p tsconfig.build.json"` to `tools/mcp-ia-server/package.json`; flip `.mcp.json` `args` to compiled `dist/index.js` with `MCP_SOURCE_MODE=1` dev fallback. Gitignore `.cache/`. Target: cold-start 1500 ms → ~200 ms.
  depends_on: []
  related:
    - TECH-496
    - TECH-497
    - TECH-498
    - TECH-499
    - TECH-500
    - TECH-501
  stub_body:
    summary: |
      Parse cache + dist build switch. On-disk mtime-keyed JSON cache for `parseDocument()` hits; `.mcp.json` flips from `tsx`-on-source to compiled `dist/index.js`. Cold-start win ~1300 ms per session.
    goals: |
      - mtime-keyed cache at `tools/mcp-ia-server/.cache/parse-cache.json`; hit returns parsed AST, miss reparses + write-through.
      - `tools/mcp-ia-server/package.json` `build` script producing `dist/index.js` via `tsconfig.build.json`.
      - `.mcp.json` `args` → compiled dist entry; `MCP_SOURCE_MODE=1` env fallback swaps back to `tsx` on source for dev.
      - Gitignore `.cache/`; preserve existing `REPO_ROOT` + `DEBUG_MCP_COMPUTE` env passthrough.
    systems_map: |
      - `tools/mcp-ia-server/src/parser/parse-cache.ts` (new)
      - `tools/mcp-ia-server/src/parser/markdown-parser.ts` (integration point)
      - `tools/mcp-ia-server/package.json` (build script)
      - `tools/mcp-ia-server/tsconfig.build.json` (new or existing)
      - `.mcp.json` (args flip + env flag docs)
      - `.gitignore` (add `.cache/`)
    impl_plan_sketch: |
      Phase 1 — Author `parse-cache.ts` with `readCached(path, mtime)` / `writeCached(path, mtime, ast)`. Wire into `markdown-parser.ts`. Add `build` script + `tsconfig.build.json`. Flip `.mcp.json`. Doc `MCP_SOURCE_MODE=1` fallback in CLAUDE.md §2 or server README. Gitignore `.cache/`. Unit test cache hit/miss + mtime invalidation.

- operation: file_task
  task_key: T17.2
  reserved_id: TECH-496
  title: "YAML-first parser + manifest cache (B8)"
  priority: medium
  issue_type: TECH
  notes: |
    Audit `tools/mcp-ia-server/src/parser/backlog-parser.ts` — confirm `ia/backlog/{id}.yaml` checked BEFORE `BACKLOG.md` fallback for every id lookup; rewrite any ordering violation. Add manifest cache keyed by `ia/backlog/` dir mtime; `{id → yaml-path}` map invalidates on mtime change. Target: cumulative savings on highest-frequency `backlog_issue` tool.
  depends_on: []
  related:
    - TECH-495
    - TECH-497
    - TECH-498
    - TECH-499
    - TECH-500
    - TECH-501
  stub_body:
    summary: |
      YAML-first `backlog_issue` resolution + mtime-keyed manifest cache. Confirms yaml checked before `BACKLOG.md` fallback; caches `{id → path}` map per session. Highest-ROI cache since `backlog_issue` is top-frequency MCP call.
    goals: |
      - Verify `ia/backlog/` + `ia/backlog-archive/` yaml paths checked before `BACKLOG.md` fallback in every id lookup path.
      - Add manifest cache: read dir mtime at first call per session; build `{id → yaml-path}` map; invalidate + re-scan on mtime change.
      - Unit tests — mixed-state (yaml + archived yaml + BACKLOG-only); cache hit/miss; archived-yaml resolution.
    systems_map: |
      - `tools/mcp-ia-server/src/parser/backlog-parser.ts` (audit + rewrite)
      - `ia/backlog/` + `ia/backlog-archive/` (sources)
      - `BACKLOG.md` + `BACKLOG-ARCHIVE.md` (fallback)
      - `tools/mcp-ia-server/tests/parser/backlog-parser.test.ts` (unit tests)
    impl_plan_sketch: |
      Phase 1 — Grep `backlog-parser.ts` for all id-resolution sites; confirm yaml-first order; add manifest cache helper (mtime-keyed Map); wire lookups through cache; unit tests for hit/miss/invalidation + mixed-state resolution.

- operation: file_task
  task_key: T17.3
  reserved_id: TECH-497
  title: "README drift CI (B6)"
  priority: medium
  issue_type: TECH
  notes: |
    Author `tools/scripts/validate-mcp-readme.mjs` — parse `tools/mcp-ia-server/README.md` tool-table row count; grep `registerTool\(` count in `src/index.ts`; exit non-zero w/ descriptive diff when counts differ. Add `validate:mcp-readme` script; compose into `validate:all`. **Soft-depends on Stage 9 T9.4** (`docs/mcp-ia-server.md` catalog rewrite) — T9.4 not yet filed; do not land T17.3 until T9.4 Done to avoid lint churn on stale README.
  depends_on: []
  related:
    - TECH-495
    - TECH-496
    - TECH-498
    - TECH-499
    - TECH-500
    - TECH-501
  stub_body:
    summary: |
      CI lint comparing `registerTool(` call count in `src/index.ts` to README tool-table row count. Catches README drift (tool registered w/o doc row, or doc row w/o registration) before merge.
    goals: |
      - `tools/scripts/validate-mcp-readme.mjs` — parses README tool table, counts `registerTool(` hits; diff → exit non-zero w/ list.
      - `validate:mcp-readme` root npm script; composed into `validate:all`.
      - Gated on Stage 9 T9.4 Done — confirm at implementation time; block until catalog rewrite lands.
    systems_map: |
      - `tools/scripts/validate-mcp-readme.mjs` (new)
      - `tools/mcp-ia-server/README.md` (parse target — tool table)
      - `tools/mcp-ia-server/src/index.ts` (grep target — `registerTool(`)
      - `package.json` (root script + `validate:all` composition)
    impl_plan_sketch: |
      Phase 1 — Confirm T9.4 Done. Author validator mjs; regex `registerTool\(`; parse README markdown table rows; descriptive diff on mismatch. Wire npm script + `validate:all` composition. Run green post-landing.

- operation: file_task
  task_key: T17.4
  reserved_id: TECH-498
  title: "Progressive disclosure — spec_outline + list_rules (B5)"
  priority: medium
  issue_type: TECH
  notes: |
    Extend `spec-outline.ts` + `list-rules.ts` Zod with `expand?: boolean` (default `false`). Default responses: `spec_outline` depth=1 heading tree; `list_rules` only `alwaysApply: true` rules. Opt-in `expand: true` returns full payload. Breaking change — 1–2k tokens saved per call. Document migration in CHANGELOG (T17.7).
  depends_on: []
  related:
    - TECH-495
    - TECH-496
    - TECH-497
    - TECH-499
    - TECH-500
    - TECH-501
  stub_body:
    summary: |
      Progressive-disclosure defaults on `spec_outline` + `list_rules`. Default responses trim to depth=1 / `alwaysApply: true` only; callers pass `expand: true` for full payload. Saves 1–2k tokens per call.
    goals: |
      - `spec_outline` — add `expand?: boolean` default `false`; filter heading tree to depth 1; `expand: true` → full tree (current behavior).
      - `list_rules` — add `expand?: boolean` default `false`; filter to `alwaysApply: true` rules; `expand: true` → all rules.
      - Descriptor prose ≤120 chars (T17.5 budget).
      - Breaking change documented in CHANGELOG + migration note (T17.7).
    systems_map: |
      - `tools/mcp-ia-server/src/tools/spec-outline.ts`
      - `tools/mcp-ia-server/src/tools/list-rules.ts`
      - Rule frontmatter `alwaysApply` field (existing)
      - `tools/mcp-ia-server/tests/tools/spec-outline.test.ts` + `list-rules.test.ts`
      - `tools/mcp-ia-server/CHANGELOG.md` (migration note under T17.7)
    impl_plan_sketch: |
      Phase 1 — Extend Zod schemas; add filter branch keyed on `expand`; preserve existing `ok: false` paths; unit tests for default + expand behaviors; confirm `spec_not_found` unchanged.

- operation: file_task
  task_key: T17.5
  reserved_id: TECH-499
  title: "Descriptor-prose lint (B9)"
  priority: medium
  issue_type: TECH
  notes: |
    Author `tools/scripts/validate-mcp-descriptor-prose.mjs` — AST-walk or regex every `.describe("...")` call in `src/tools/*.ts`; exit non-zero listing file + line + length when string >120 chars. Add `validate:mcp-descriptor-prose` npm script; compose into `validate:all`. Pairs w/ T17.6 remediation sweep.
  depends_on: []
  related:
    - TECH-495
    - TECH-496
    - TECH-497
    - TECH-498
    - TECH-500
    - TECH-501
  stub_body:
    summary: |
      CI lint enforcing `.describe()` param descriptors ≤120 chars. Keeps tool schemas scannable; blocks verbose-descriptor regression (per source-doc B9 finding — `unity_bridge_command` currently 300+ chars).
    goals: |
      - `tools/scripts/validate-mcp-descriptor-prose.mjs` — AST-walk or regex `.describe("...")`; >120 chars → exit non-zero w/ file:line:length:prose.
      - `validate:mcp-descriptor-prose` npm script; composed into `validate:all`.
      - Unit fixture — synthetic `.ts` with ≤120-char + 150-char `.describe` → lint emits 1 error.
    systems_map: |
      - `tools/scripts/validate-mcp-descriptor-prose.mjs` (new)
      - `tools/mcp-ia-server/src/tools/*.ts` (scan target)
      - `package.json` (root script + `validate:all` composition)
      - `tools/mcp-ia-server/tests/scripts/validate-descriptor-prose.test.ts` (fixture test)
    impl_plan_sketch: |
      Phase 1 — Author validator mjs; regex `\.describe\(\s*"([^"]*)"\s*\)`; length check + offender report. Wire npm script + `validate:all`. Fixture test. Run post-T17.6 sweep green.

- operation: file_task
  task_key: T17.6
  reserved_id: TECH-500
  title: "Descriptor-prose remediation sweep"
  priority: medium
  issue_type: TECH
  notes: |
    Paired w/ T17.5 lint. Grep `.describe(` across `src/tools/*.ts`; trim every param descriptor >120 chars while preserving semantics. Top offender: `unity-bridge-command.ts` (300+ char param descriptions per source-doc B9). Prefer abbreviation + hint-next-tools pointer over verbose prose. Run T17.5 lint post-sweep; `validate:all` green.
  depends_on:
    - TECH-499
  related:
    - TECH-495
    - TECH-496
    - TECH-497
    - TECH-498
    - TECH-501
  stub_body:
    summary: |
      Remediation sweep shortening every `.describe()` descriptor >120 chars in `src/tools/*.ts`. Lands alongside T17.5 lint. Primary target: `unity-bridge-command.ts` (300+ char params).
    goals: |
      - Trim every `.describe()` >120 chars across `src/tools/*.ts`.
      - Preserve param semantics — abbreviation + structured secondary (rendered in tool output) over verbose prose.
      - Rewrite `unity-bridge-command.ts` ≥4 param descriptors to ≤120-char primary.
      - T17.5 lint green post-sweep; `validate:all` green.
    systems_map: |
      - `tools/mcp-ia-server/src/tools/unity-bridge-command.ts` (primary offender)
      - `tools/mcp-ia-server/src/tools/*.ts` (scan + trim all)
      - `tools/scripts/validate-mcp-descriptor-prose.mjs` (lint gate from T17.5)
    impl_plan_sketch: |
      Phase 1 — Run T17.5 lint in advisory mode to list every offender. Trim each; verify tool behavior unchanged (snapshot tests). Shift verbose guidance into tool output prose instead of schema descriptor where needed. Re-run lint → zero offenders. `validate:all` green.

- operation: file_task
  task_key: T17.7
  reserved_id: TECH-501
  title: "Release prep v1.2.0"
  priority: low
  issue_type: TECH
  notes: |
    Bump `tools/mcp-ia-server/package.json` to `1.2.0`. Append CHANGELOG entry covering parse cache + dist build + yaml-first parser + progressive-disclosure defaults + 2 CI lints. Migration table — `spec_outline` + `list_rules` callers pass `expand: true` for full payload. Advisory tag `mcp-pre-theme-b-remainder-v1.1.x` pre-commit for rollback target.
  depends_on:
    - TECH-495
    - TECH-496
    - TECH-497
    - TECH-498
    - TECH-499
    - TECH-500
  related: []
  stub_body:
    summary: |
      v1.2.0 release prep. Version bump + CHANGELOG entry covering Theme B MCP-surface remainder (parse cache + dist + yaml-first + progressive disclosure + 2 CI lints). Migration table for breaking `expand` default flip.
    goals: |
      - `tools/mcp-ia-server/package.json` version → `1.2.0`.
      - CHANGELOG entry — concise scope summary + migration table (`expand: true` opt-in for full payload on `spec_outline` + `list_rules`).
      - Advisory pre-commit tag `mcp-pre-theme-b-remainder-v1.1.x` for rollback.
      - `validate:all` green post-bump.
    systems_map: |
      - `tools/mcp-ia-server/package.json`
      - `tools/mcp-ia-server/CHANGELOG.md`
      - Git tag `mcp-pre-theme-b-remainder-v1.1.x` (advisory; human-applied)
    impl_plan_sketch: |
      Phase 1 — Bump version + append CHANGELOG entry (scope + migration + rollback pointer). Run `validate:all`. Advise human to tag pre-commit before merge.
```

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `ia/projects/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
