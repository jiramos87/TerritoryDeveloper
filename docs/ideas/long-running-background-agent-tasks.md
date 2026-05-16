# Long-running background agent tasks

Ideas for `/goal` skill TUI surface in `claude-personal agents` view. Each entry = candidate for autonomous end-to-end run: scan whole repo, produce output (mutation or report). Split into **sweeps** (do work in place) and **audits** (produce list → spawn followup plans).

Each entry includes a copy-pasteable `/goal is to …` handoff prompt for a fresh session.

Status: brainstorm. No skill stubs yet.

---

## Sweeps — do work in place

### Code hygiene

**C# XML doc backfill** — public service methods missing `<summary>` get stubs from method name + param hints. (DONE 2026-05-16 09:05)

```
/goal is to add <summary> XML doc to every public method in Assets/Scripts/**/Domains/*/Services/*.cs that lacks one. Infer prose from method name + param names + body. Skip private/internal. Run `npm run unity:compile-check` after each batch of 20 files. Commit per service folder with message `docs(xml-doc): backfill {ServiceName}`. Stop + report on first compile fail.
```

**TODO/FIXME triage** — scan source, cluster by topic, file BACKLOG row per cluster, rewrite comment with id ref.

```
/goal is to scan Assets/**, web/**, tools/** for TODO/FIXME/HACK comments. Cluster by topic (path-root + comment text similarity). For each cluster: file one BACKLOG row via `task_insert` MCP (type TECH or BUG, depending on phrasing), then rewrite the comment as `// TODO({ISSUE_ID}): {summary}`. Output: cluster count + filed id list.
```

**Magic number → named constant** — Unity hot paths; `[SerializeField]` where designer-tunable. (DONE 2026-05-16 09:05)

```
/goal is to scan Assets/Scripts/**/*.cs for numeric literals in non-test code (skip 0, 1, -1, common indexes). Group by file. For each file: extract repeated literals to `private const` (PascalCase), or `[SerializeField] private float` with default when the value looks designer-tunable (used >1x in MonoBehaviour). Run `npm run unity:compile-check` after each file. Commit per file.
```

**Dead code prune** — unused privates, unreachable branches, orphan `.cs` files.

```
/goal is to find unused private members + unreachable branches + orphan .cs files under Assets/Scripts/**. Use `unity_callers_of` MCP to verify zero callers. Skip [SerializeField], [UsedImplicitly], MonoBehaviour lifecycle methods (Start/Update/etc.). Delete with one-line commit per file. Run compile-check + scene-wiring preflight after each batch of 10 files.
```

**Null-guard backfill** — public API surfaces of `Domains/*/Services/` (DONE 2026-05-16 09:05)

```
/goal is to scan public methods in Assets/Scripts/Domains/*/Services/*.cs whose params include reference types. Add `ArgumentNullException` guard at top of method body when the param is dereferenced. Skip when param only flows into another guarded call. Run `npm run unity:compile-check` + smoke test per service folder.
```

**Using-statement prune** — unused `using` lines.

```
/goal is to remove unused `using` lines across Assets/Scripts/**/*.cs, tools/mcp-ia-server/**/*.ts, web/**/*.ts. Run `npm run validate:all` + `npm run unity:compile-check` after each folder. No semantic changes. Commit per top-level folder.
```

**LINQ allocation audit** — `Update`/`FixedUpdate`/`OnGUI` paths.

```
/goal is to find LINQ chains (.Select/.Where/.ToList/.OrderBy) inside Unity Update/FixedUpdate/OnGUI methods across Assets/Scripts/**. Output report at docs/audits/linq-hotpath-{YYYY-MM-DD}.md with one row per call site (file:line + suggested for-loop rewrite). No code mutation — audit only. File one TECH BACKLOG row per hotspot cluster via `task_insert`.
```

**async/await correctness** — fire-and-forget detector + missing `ConfigureAwait`.

```
/goal is to find fire-and-forget async calls (async method invoked without await, not assigned to `_`) in tools/mcp-ia-server/** + web/**. Add `await` or explicit `_ = ` per call. Add `ConfigureAwait(false)` in MCP server library code (NOT in Next.js handlers). Run `npm run validate:all` per package after each file.
```

**Namespace ↔ folder coherence** — file path matches namespace declaration.

```
/goal is to assert C# namespace matches folder path under Assets/Scripts/**. When mismatch: rename namespace to match folder (canonical = folder). Run `npm run unity:compile-check` after each file. Skip Editor/ folder (different namespace convention). Commit per top-level namespace.
```

**Logger format consistency** — interpolation over concatenation.

```
/goal is to convert Debug.Log/Logger.* calls from string concatenation (`+`) to `$"{}"` interpolation across Assets/Scripts/**. Skip Debug.LogFormat callers (already format-string style). Run compile-check per file. Commit per service folder.
```

### Prose / docs

**Caveman backfill** — convert IA prose per `ia/rules/agent-output-caveman.md`.  (DONE 2026-05-16 15:05)

```
/goal is to convert IA prose in ia/** + docs/** to caveman format per ia/rules/agent-output-caveman.md. EXCLUDE web/app/**/page.tsx user-facing copy, security/auth rationale, error messages, code blocks, JSON, MCP tool descriptors. Process one file at a time. Run `npm run validate:all` after each directory. Commit per directory.
```

**Glossary slug drift** — ad-hoc synonyms → canonical glossary term.

```
/goal is to scan ia/**, docs/**, C# XML doc comments for ad-hoc synonyms of canonical glossary terms (source of truth: ia/specs/glossary.md). Use `glossary_discover` MCP to enumerate canonical slugs. Replace synonym → canonical. Run `npm run generate:ia-indexes` + `npm run validate:all` after. Commit per directory.
```

**Internal link rot** — broken markdown links + anchors.

```
/goal is to find broken relative markdown links + heading anchors across ia/** + docs/**. When target moved (rename trail in `git log --follow`): fix the link. When target deleted: flag at docs/audits/link-rot-{YYYY-MM-DD}.md. Mutation only for renames; deletions go to audit.
```

**Stale docs/ audit** — no commit in 180 days.

```
/goal is to find docs/**/*.md files with no commit touching them in 180 days (use `git log --since`). Output report at docs/audits/stale-docs-{YYYY-MM-DD}.md with last-touched date + path + first-paragraph excerpt. No deletions — propose archive list for user review.
```

**Spec-to-code drift** — specs referencing renamed/deleted APIs.

```
/goal is to scan ia/specs/**/*.md for code refs (class names, method names, file paths). Verify each ref still exists via `unity_callers_of` MCP or filesystem check. Output report at docs/audits/spec-code-drift-{YYYY-MM-DD}.md with one row per dead ref. File one TECH BACKLOG row per spec file with ≥3 dead refs.
```

**Caveman exception boundary check** — caveman vs simple-English boundaries.

```
/goal is to verify caveman-exception surfaces stay simple-English (web/app/**/page.tsx user copy, security/auth rationale, error messages) AND non-exception surfaces (ia/**, docs/**, agent prose) stay caveman. Output report at docs/audits/caveman-boundary-{YYYY-MM-DD}.md. No mutation — audit only.
```

### IA integrity

**Master-plan drift sweep** — `arch_drift_scan` across all open slugs.

```
/goal is to run `arch_drift_scan` MCP on every open master-plan slug (enumerate via `master_plan_state` returning all open slugs). For each plan with drift findings: emit §Plan Fix tuple list to docs/audits/master-plan-drift-{YYYY-MM-DD}.md. No plan mutation — change-log only. File one TECH BACKLOG row per drifted plan.
```

**BACKLOG yaml health** — incomplete fields, orphan rows.

```
/goal is to enumerate every row via `backlog_list` MCP. For each: verify `depends_on` refs exist, acceptance criteria present, status valid, type valid. Output report at docs/audits/backlog-health-{YYYY-MM-DD}.md. Auto-fix obvious typos via `task_spec_section_write` MCP. Flag structural issues for user review.
```

**Skill SKILL.md frontmatter drift** — generated surface parity.

```
/goal is to run `npm run validate:skill-drift`. For each drift: regenerate via `npm run skill:sync:all`. Verify `.claude/agents/*.md` + `.claude/commands/*.md` reflect current SKILL.md source. Run `npm run validate:all` after. Commit with message `chore(skill-sync): regenerate from SKILL.md sources`.
```

**Anchor reindex** — full rebuild.

```
/goal is to fire `cron_anchor_reindex_enqueue` MCP for full repo. Poll cron drain status. Output row count delta (before vs after).
```

**Glossary backlinks rebuild** — full rebuild.

```
/goal is to fire `cron_glossary_backlinks_enqueue` MCP for full repo. Poll cron drain status. Output backlink count delta.
```

**Dead project spec audit** — archive specs without open BACKLOG row.

```
/goal is to scan ia/projects/*.md. For each: cross-check matching BACKLOG row exists + is open via `backlog_issue` MCP. If row closed/archived or missing: move spec file to ia/projects/_archive/. Output archived count + path list.
```

**Retired skill orphan scan** — refs to `_retired/` in active prose.

```
/goal is to grep ia/skills/**, .claude/agents/**, .claude/commands/**, ia/rules/**, docs/** for refs to `_retired/` paths or names of retired skills. Output report at docs/audits/retired-skill-orphans-{YYYY-MM-DD}.md. File one BUG BACKLOG row per cluster of orphan refs.
```

### Architecture

**DEC drift audit** — decisions vs current code.

```
/goal is to load every row via `arch_decision_list` MCP. For each: scan code/specs for refs to deprecated patterns the DEC outlawed AND refs to new patterns DEC mandates. Output report at docs/audits/dec-drift-{YYYY-MM-DD}.md with one row per DEC + drift count. Flag superseded-but-active DECs for status fix.
```

**Layer dependency violation scan** — upward refs.

```
/goal is to parse ia/specs/architecture/layers.md dependency rules. For each C# file: extract `using` + namespace refs. Flag upward refs (e.g. Domain referencing Application, Infrastructure referencing Domain wrongly). Output report at docs/audits/layer-violations-{YYYY-MM-DD}.md.
```

**Atomize candidate sweep** — file `/atomize-file` rows for >2500 LOC files.

```
/goal is to find every C# file >2500 LOC under Assets/Scripts/Managers/. For each: file one TECH BACKLOG row via `task_insert` MCP titled `Atomize {ClassName} per Strategy γ`, body referencing `/atomize-file {PATH}` + LOC count. Output filed id list.
```

**arch_surface backfill** — code patterns without surface rows.

```
/goal is to scan code for patterns matching arch_surface kinds (event bus subscribers, MCP tool registrations, hook handlers, scene-wired hubs) without matching `arch_surface` row via `arch_surface_resolve`. Auto-write rows via `arch_surface_write` MCP. Run `arch_surfaces_backfill` MCP after. Output written-row count.
```

**arch_changelog completeness** — every closed Stage has changelog.

```
/goal is to enumerate closed Stages via `master_plan_state`. For each: call `arch_changelog_since` with Stage close timestamp. If empty: file backfill entry via `cron_arch_changelog_append_enqueue` MCP referencing Stage id + commit sha. Output backfilled count.
```

### Test gaps

**Stage test file presence** — companion test per closed Stage.

```
/goal is to enumerate closed Stages via `master_plan_state`. For each: check `tests/{slug}/stage{N}-*.test.{mjs|cs}` exists on disk. Missing: file one TECH BACKLOG row to author the test file. Output filed id list per plan slug.
```

**Public service coverage gap** — services without companion test.

```
/goal is to enumerate every class in Assets/Scripts/Domains/*/Services/*.cs. For each: grep tests/** for refs to class name. No ref → file one TECH BACKLOG row to author smoke test. Output gap count per domain.
```

**Red-Stage proof anchor sweep** — repo-wide drift check.

```
/goal is to run `npm run validate:red-stage-proof-anchor` on the whole repo. For each drift finding: when method body still matches surface keywords → regenerate anchor prose. When method changed semantically → file BUG BACKLOG row referencing anchor + Stage id.
```

**Test mode scenario backfill** — gameplay surfaces without scenario.

```
/goal is to enumerate gameplay surfaces via `arch_surface_resolve kind=gameplay-feature`. For each: check tools/testmode/scenarios/*.json contains matching scenario id. Missing: file one TECH BACKLOG row to author scenario JSON + expected screenshot.
```

### Unity-specific

> Note: `FindObjectOfType` repo sweep is **out of scope** — owned by `cityscene-perf-large-grids` plan.

**Scene wiring drift** — every inspector-bound hub still present + wired.

```
/goal is to load every .unity scene + .prefab via `unity_bridge_command`. For each MonoBehaviour with serialized fields: assert refs resolve (not None/missing-script). Output report at docs/audits/scene-wiring-{YYYY-MM-DD}.md. Capture screenshot via `unity_bridge_command capture_screenshot` for any dirty scene.
```

**ScriptableObject orphan scan** — assets nothing references.

```
/goal is to enumerate every .asset under Assets/. For each: grep refs in .prefab + .unity + .cs (AssetDatabase.Load*). Zero refs → move to Assets/_unused/ (or list at docs/audits/so-orphans-{YYYY-MM-DD}.md when destructive move feels risky). Output orphan count.
```

**Prefab GUID rot** — broken script refs.

```
/goal is to scan every .prefab + .unity for missing script refs (guid: 00000000000000000000000000000000 or unresolved m_Script). Output report at docs/audits/prefab-guid-rot-{YYYY-MM-DD}.md with one row per broken ref + last-touching commit sha.
```

**Sprite atlas dedup** — same sprite in multiple atlases.

```
/goal is to enumerate sprite refs in every .spriteatlas under Assets/. For each sprite appearing in >1 atlas: flag for dedup. Output report at docs/audits/atlas-dedup-{YYYY-MM-DD}.md with sprite name + atlas list per row.
```

### Web

**Unused exports** — tree-shake candidates.

```
/goal is to run `ts-prune` (or equivalent) across web/. For each unused export: delete or mark `// @internal`. Run `npm run validate:all` + `npx next build` after each file. Commit per top-level web/ subfolder.
```

**Design token drift** — `ds-`* className vs design-tokens.ts.

```
/goal is to scan web/**/*.tsx for `ds-`* className uses. Verify each token exists in web/lib/design-tokens.ts + matches web/lib/design-system.md spec. Output report at docs/audits/design-token-drift-{YYYY-MM-DD}.md with one row per orphan/typo token.
```

**Route reachability** — pages without inbound links.

```
/goal is to enumerate routes under web/app/**/page.tsx. For each: search web/ for Link/href refs targeting that route. No inbound → flag at docs/audits/web-route-reach-{YYYY-MM-DD}.md. No deletions — audit only.
```

### Repo hygiene

**File size outlier audit** — bloat scan.

```
/goal is to find files >5MB tracked in git (use `git ls-tree -lr HEAD` + sort). Output report at docs/audits/large-files-{YYYY-MM-DD}.md grouped by extension. Cross-check .gitignore coverage for typical bloat patterns (.psd, .blend, .mp4, .wav). Propose .gitignore additions.
```

**Branch staleness** — remote branches >30d.

```
/goal is to enumerate `git branch -r` with `git for-each-ref --sort=-committerdate`. Output report at docs/audits/stale-branches-{YYYY-MM-DD}.md with branches >30d unmerged into main. No deletion — audit only.
```

**Migration sequence integrity** — chain replay.

```
/goal is to run `npm run db:migrate -- --dry-run` against fresh schema. Verify no gaps in migration ids. Output pass/fail per migration to docs/audits/migration-integrity-{YYYY-MM-DD}.md.
```

**Cron health** — failed rows + retry storms.

```
/goal is to query failed cron rows from last 7 days (cron_* tables). Group by `cron_kind`. Output report at docs/audits/cron-health-{YYYY-MM-DD}.md + propose retry/purge strategy per cluster. File one BUG BACKLOG row per cron_kind with >10 failures.
```

**Section claim stale row sweep** — force-release expired claims.

```
/goal is to call `claims_sweep` MCP. Output released-row count + (slug, section_id) list. Idempotent — safe to run repeatedly.
```

---

## Audits — produce report, spawn new plan rows

Audit itself = background job; followups = separate plans/issues filed via `task_insert` MCP.

**Atomize backlog**

```
/goal is to find every C# file >2500 LOC under Assets/Scripts/Managers/. For each: file one TECH BACKLOG row via `task_insert` MCP titled `Atomize {ClassName} per Strategy γ`, body containing `/atomize-file {PATH}` + LOC count + concern guesses. Output filed id list.
```

**Caveman debt backlog**

```
/goal is to scan ia/** + docs/** files. For each file with >5 non-caveman paragraphs (filler/hedging/articles per ia/rules/agent-output-caveman.md): file one TECH BACKLOG row to caveman-convert. Output filed id count + total paragraph debt across repo.
```

**Glossary debt backlog**

```
/goal is to extract domain terms from code/docs via `glossary_discover` MCP that have no canonical glossary row. Cluster by domain (zoning / pathfinding / sprite-gen / etc.). File one ART BACKLOG row per cluster to expand glossary + assign canonical slug.
```

**DEC backlog**

```
/goal is to find code patterns implying an architectural decision was made (singleton, factory, plugin loader, event bus pattern, hook chain) without matching `arch_decisions` row. File one ART BACKLOG row per gap to record DEC. Output filed id count.
```

**Skill friction backlog**

```
/goal is to run `/skill-train` against every skill in ia/skills/. Aggregate recurring frictions (≥2 occurrences across all skills). File one TECH BACKLOG row per skill with friction proposal diff attached as spec body via `task_spec_section_write` MCP.
```

**MCP tool gap audit**

```
/goal is to scan agent transcript history (last 30 days) for repeated reads of same file slice (≥3 reads of same path+offset). Cluster by intent. Output report at docs/audits/mcp-gap-{YYYY-MM-DD}.md proposing one new MCP slice per cluster + suggested tool signature.
```

**Test gap audit**

```
/goal is to list every public service in Assets/Scripts/Domains/*/Services/ without companion test file. Cluster by domain. File one TECH BACKLOG row per domain to backfill tests. Output filed id list.
```

**Performance hotspot audit**

```
/goal is to run Unity profiler via `unity_bridge_command capture_profile` on a known gameplay scenario. Correlate top-N samples with LINQ allocation scan results. Output report at docs/audits/perf-hotspot-{YYYY-MM-DD}.md. File one TECH BACKLOG row per hotspot cluster ≥1ms/frame.
```

**Dead surface audit**

```
/goal is to find code/specs/rules nothing references — no inbound link, no caller via `unity_callers_of`, no spec mention via `glossary_discover`. Output archive proposal list at docs/audits/dead-surface-{YYYY-MM-DD}.md. No deletion — propose only.
```

**Retired-skill cleanup audit**

```
/goal is to grep refs to `_retired/` skills across active prose (ia/skills/**, ia/rules/**, docs/**, .claude/**). For each: file one BUG BACKLOG row to remove stale ref or migrate caller to current skill. Output filed id count per retired skill slug.
```

**Migration debt audit**

```
/goal is to diff current DB schema against last applied migration file. For each table/column/index/constraint added without corresponding migration: file one BUG BACKLOG row. Output filed id count.
```

**Cron job audit**

```
/goal is to enumerate every cron_kind in cron_* tables. For each without matching monitoring entry in web/ dashboard: file one FEAT BACKLOG row to add dashboard panel. Output filed id list.
```

**Bridge tool gap audit**

```
/goal is to enumerate Unity Editor surfaces (menu items, Editor windows, inspector buttons commonly used by agents) and cross-check against `mcp__territory-ia-bridge__*` tool list. For each gap: file one FEAT BACKLOG row to add bridge kind + handler stub.
```

---

## Suggested rollout order

Highest ROI first:

1. **Caveman backfill** — touches most files, mechanical, unblocks future drift detection.
2. **Atomize candidate sweep** — produces concrete `/atomize-file` BACKLOG rows directly (skill already exists).
3. **Master-plan drift sweep** — `arch_drift_scan` on every open slug; surfaces stale plans before next `/ship-cycle` hits them.

Next decisions:

- Pick top 3 → draft skill stubs under `ia/skills/`.
- Decide `/goal` invocation pattern: one skill per sweep, or one `goal-sweep` skill with `{sweep_type}` arg.
- Decide cadence: on-demand vs cron-scheduled.

