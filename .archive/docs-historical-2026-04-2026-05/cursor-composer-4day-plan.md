---
purpose: "4-day Cursor + Composer 2 plan while Claude Code weekly limit paused. Concrete task list sourced from full-game-mvp roadmap + MCP tool refactor orchestrator. Bias: unblocking + easy-first."
audience: operator
loaded_by: manual
slices_via: none
---

# Cursor + Composer 2 — 4-Day Plan (Claude Code Gap)

> Legacy note: this plan was time-boxed to a specific gap window.  
> Canonical ongoing runbook now lives at `docs/cursor-skill-harness.md`.
>
> Keep this file for historical execution context and retrospective reference.

> **Window:** ~2026-04-20 → 2026-04-24 (4 days without Claude Code; Cursor Pro $20 + Composer 2 + Auto mode + autocomplete available).
>
> **Branch state at gap start:** FREEZE lifted today via remaining Claude Code usage. `feature/lifecycle-collapse-cognitive-split` merges back to parent `feature/master-plans-1` before gap begins. Cursor work lands on `feature/master-plans-1` directly. Normal dev branch hygiene resumes.
>
> **Sources of truth (the "North"):**
>
> 1. `ia/projects/full-game-mvp-master-plan.md` — 10 buckets + 1 sibling umbrella.
> 2. `docs/full-game-mvp-rollout-tracker.md` — per-bucket (a)–(g) lifecycle state.
> 3. `ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md` — 16 stages MCP reshape; Stages 1–5 Done, 6–16 pending (pure TypeScript surface, ideal Composer 2 fit).
>
> **Companion rules:** `docs/agent-lifecycle.md` parallel-work rule. `docs/cursor-agents-skills-mcp-study.md` — Cursor vs Claude Code surface map.

---

## 1. TL;DR

Priority bias this gap: **unblocking first, then easy-fit execute, then opportunistic**.

Four categories of safe work without Claude Code slash commands:

1. **Unblockers — align-gate (g) clears.** ~60 glossary rows pending across 7 buckets; 4 reference spec stubs absent (`economy-system.md`, `simulation-signals.md`, `utility-system.md`, `landmarks-system.md`). Clearing these unsticks `/release-rollout` on Claude Code return. Pure markdown + validator. Highest-leverage low-risk work.
2. **Execute already-filed Stage 1.1 tasks** — specs drafted under `ia/projects/TECH-XXX.md` across 10 buckets. Composer 2 reads spec → edits named files → `npm run validate:all` + `npm run unity:compile-check` → commits. Normal dev flow.
3. **MCP tool refactor (TypeScript)** — Stages 6–16 of the opus-4.7 audit plan. Pure `tools/mcp-ia-server/**` surface, zero Unity runtime risk, well suited to Composer 2 (multi-file TypeScript refactors). Can proceed with or without pre-filed TECH-XXX — orchestrator Task rows carry Intent prose that functions as a spec.
4. **Web workspace + sprite-gen + exploration docs** — disjoint from Unity compile, safe for any session.

Commits land on `feature/master-plans-1` normally. `/closeout` of open TECH-XXX waits for Claude Code return (slash command unavailable in Cursor).

**Hard avoid during gap:** `/closeout` simulation (bypassing yaml-archive move + id purge migration), manual edits to `ia/state/id-counter.json`, destructive git ops (`--force`, `reset --hard`), hooks / settings edits, brand-new glossary terms not already cited by a filed spec.

---

## 2. What Cursor Composer 2 Is Good At (March 2026)

Composer 2 = Cursor proprietary model, launched 2026-03-19. Trained for agentic sub-agent coding. ~4× faster than frontier; SWE-bench Multilingual 73.7, CursorBench 61.3, Terminal-Bench 2.0 61.7. Strong on file reads, targeted edits, structured multi-file refactors. Weaker on long-horizon planning (designed for sub-agent layer, not architect).

**Pro $20 plan:** ~225 Sonnet-equivalent requests OR ~550 cheap-model requests per month. Auto mode = unlimited (Cursor picks model). Composer edits cost 5-10× single Chat calls because multi-file. Strategy: **Auto for scaffolding + explore, Composer 2 for execute, Sonnet (budget) only for tricky bugs**.

**Cursor capabilities confirmed:**
- Terminal access from agent tab → `npm run validate:all`, `npm run unity:compile-check`, `git add`, `git commit`, `git push`
- Worktree isolation for parallel experiments
- Codebase-aware edits (indexed)
- MCP support — `territory-ia` server can be wired (bonus; unwired fallback = targeted file reads)

**Cursor gaps vs Claude Code:**
- No `.claude/agents/*.md` subagents (no `spec-implementer`, `closeout`, `verify-loop`)
- No `.claude/commands/*.md` slash commands → no `/closeout`, no `/stage-file`, no `/verify-loop`
- No `ia/skills/**/SKILL.md` auto-activation (paste SKILL body into prompt if needed)
- No `caveman:caveman` plugin (paste rule snippet manually when authoring IA prose)
- No hook denylist — bash is safe as long as you avoid force-push, `rm -rf`, hand-edit `id-counter.json`

---

## 3. Guardrails for the Gap

### 3.1 Parallel-work rule (authority)

`docs/agent-lifecycle.md` + rollout tracker hard rule: NEVER run `/stage-file` or `/closeout` against two sibling child orchestrators concurrently on same branch. Cursor can't run those commands anyway, so rule reduces to: **one bucket actively implementing at a time per session**. Switch buckets between sessions, not within.

### 3.2 Commit hygiene (normal branch flow)

- Commit per task (not per phase). Message format: `feat(ui-polish): TECH-309 StudioRackBlock schema — additive token extension`.
- `npm run validate:all` green before commit. C# edits → also `npm run unity:compile-check`.
- Push to `feature/master-plans-1` freely. No force-push, no amend after push, no rewriting history of pushed commits.
- `/closeout` stays a Claude Code operation — don't manually archive yaml / delete spec / purge id. Leave TECH-XXX open; Claude Code closes on return with proper lessons migration.
- Hooks denylist not enforced in Cursor — stay off `git push --force`, `git reset --hard`, `rm -rf ia/`, `rm -rf .claude/`, `sudo *`.

### 3.3 Verification substitute for `/verify-loop`

Composer 2 has terminal access. Manual chain per C#-touching task:
- `npm run validate:all`
- `npm run unity:compile-check` (loads `.env.local` for Unity path)
- `npm run db:bridge-preflight` (when touching MCP bridge tools or journal)
- Optional: open Unity Editor manually, Play Mode smoke the affected scene
- Copy output into project spec `## 8. Verification Block` manually before committing

Skip Path A (`unity:testmode-batch`) + Path B (bridge hybrid) during gap — reserve for `/verify-loop` on Claude Code return.

### 3.4 Write / compose discipline

- Use `ia/specs/glossary.md` vocabulary verbatim. Don't invent new terms mid-implementation — if you notice a missing term, add it via glossary row task (§4.1) separately.
- Caveman default still applies to IA prose (specs, rules, skills, orchestrators). Full English allowed in `web/content/**`, commits / PR bodies, and code identifiers / comments.
- Don't write to `.claude/agents/**`, `.claude/commands/**`, `ia/skills/**/SKILL.md` bodies, `ia/state/**`, `ia/templates/**` — session-context infra; Claude Code owns evolution.

---

## 4. The 40 Tasks (bias: unblock first, then easy, then opportunistic)

Organized by leverage. Each task: id · est effort · composer-ability (1-5) · dependency / risk note.

### 4.1 Align-gate (g) unblockers — glossary + spec stubs (markdown only, highest leverage)

These clear the (g) gate on multiple rollout rows; every closed gate lets Claude Code advance the tracker on return without re-running checks.

| # | Task | Scope | Effort | Fit |
|---|------|-------|--------|-----|
| 1 | zone-s-economy glossary batch | ~10 rows: Zone S, ZoneSubTypeRegistry, envelope (budget sense), zone sub-type, zone-sub-types.json sidecar, IsStateServiceZone, ZoneType enum drift, save-schema v3, economy envelope, zone demand factor | 1h | 5 |
| 2 | citystats glossary batch | 4 rows: StatsFacade, ColumnarStatsStore, StatKey, IStatsReadModel | 30m | 5 |
| 3 | city-sim-depth glossary batch (Stage 1.1 scope) | 8 rows: SimulationSignal, SignalField, SignalFieldRegistry, ISignalProducer, ISignalConsumer, SignalMetadataRegistry, DiffusionKernel, SignalTickScheduler | 1h | 5 |
| 4 | ui-polish glossary batch (Stage 1.1 scope only) | 3 rows from TECH-313: UiTheme token ring, Studio-rack token, Motion token | 30m | 5 |
| 5 | utilities glossary batch | 7 rows: Utility pool, Utility contributor, Utility consumer, Pool status, Freeze flag, EMA warning, Deficit cascade | 1h | 5 |
| 6 | landmarks glossary batch | 8 rows: landmark, big project, LandmarkProgressionService, BigProjectService, LandmarkPlacementService, LandmarkCatalogStore, LandmarkCatalogRow, tier-defining landmark, intra-tier reward landmark | 1h | 5 |
| 7 | `ia/specs/economy-system.md` stub | Frontmatter + §1 Purpose + §2 placeholder headings (Zones / Jobs / Budget / Taxation / Save schema). Deep prose deferred. Blocks zone-s-economy (g). | 1-2h | 5 |
| 8 | `ia/specs/simulation-signals.md` stub | Same pattern. Blocks city-sim-depth (g). Content sourced from **TECH-308** (see [`BACKLOG.md`](../BACKLOG.md)). | 1-2h | 5 |
| 9 | `ia/specs/utility-system.md` stub | Same pattern. Blocks utilities (g). | 1-2h | 5 |
| 10 | `ia/specs/landmarks-system.md` stub | Same pattern. Blocks landmarks (g). | 1-2h | 5 |

### 4.2 Easy filed Stage 1.1 C# tasks (high Composer 2 fit, low blast)

Specs already drafted, Implementation Plans clear, narrow blast radius. Composer 2 reads spec → edits named files → compile-check → commit.

| # | Task id | Scope | Effort | Fit | Notes |
|---|---------|-------|--------|-----|-------|
| 11 | TECH-347 | Distribution `BuildInfo` SO schema | 30m | 5 | Pure serializable class, zero runtime paths |
| 12 | TECH-348 | `DefaultBuildInfo.asset` scaffold | 30m | 4 | Asset file; commit with placeholder values |
| 13 | TECH-349 | `SemverCompare` helper | 1h | 5 | Pure C# utility; string parsing |
| 14 | TECH-350 | `SemverCompare` unit tests | 1h | 5 | NUnit tests; happy + edge cases |
| 15 | TECH-309 | `UiTheme.StudioRackBlock` nested schema | 1-2h | 5 | 10 fields, additive; compile-check green |
| 16 | TECH-310 | `UiTheme.MotionBlock` nested schema + curves | 1-2h | 5 | Same pattern as TECH-309 |
| 17 | TECH-305 | `SimulationSignal` enum + producer/consumer interfaces | 1-2h | 5 | Greenfield dir `Assets/Scripts/Simulation/Signals/` |
| 18 | TECH-306 | `SignalField` + `SignalMetadataRegistry` ScriptableObject | 2h | 5 | Float[,] backing, clamp floor 0 |
| 19 | TECH-307 | `SignalFieldRegistry` MonoBehaviour + inspector fallback | 1-2h | 4 | Invariant #4 (no new singleton) |

### 4.3 Medium filed Stage 1.1 C# tasks (multi-file, heavier review)

| # | Task id | Scope | Effort | Fit | Notes |
|---|---------|-------|--------|-----|-------|
| 20 | TECH-278 | `ZoneType` enum + predicates + `Zone.subTypeId` sidecar | 2h | 5 | Save-schema adjacent; read spec carefully |
| 21 | TECH-279 | `ZoneSubTypeRegistry` MonoBehaviour + JSON loader | 2h | 5 | Invariant #4 fallback pattern |
| 22 | TECH-281 | `IsStateServiceZone` predicate + consumer updates | 1h | 4 | Cross-file C# edit — verify all call sites |
| 23 | TECH-291 | Multi-scale allocation audit pass | 1-2h | 4 | Read-heavy grep + minor refactor |
| 24 | TECH-293 | Multi-scale tick budget test harness | 1-2h | 5 | Unit tests only; Composer 2 strong |
| 25 | TECH-303 | CityStats `StatsFacade` skeleton | 2h | 4 | Pair with #2 glossary |
| 26 | TECH-311 | `DefaultUiTheme.asset` Inspector defaults | 1h | 3 | Needs Unity Editor; ship skeleton otherwise |
| 27 | TECH-331..334 | Utilities Stage 1.1 pool scaffolding | 3-4h | 4 | Greenfield; pair with #5 glossary |
| 28 | TECH-335..338 | Landmarks Stage 1.1 catalog + progression service | 3-4h | 4 | Greenfield; pair with #6 glossary |

### 4.4 MCP tool refactor — TypeScript surface (Stage 6–16 of opus-4.7 audit)

Pure `tools/mcp-ia-server/**` TypeScript, zero Unity coupling. Each orchestrator Task row carries an Intent paragraph functioning as a spec — Composer 2 reads Intent + linked surface area → writes code + tests. Stage 6 first (terminal Step 2 cleanup), then Stages 7–9 (composites).

| # | Task ref | Scope | Effort | Fit | Notes |
|---|----------|-------|--------|-----|-------|
| 29 | Stage 6 T6.1 Snapshot tests | `tools/mcp-ia-server/tests/envelope.test.ts` — one `ok: true` + one `ok: false` fixture per tool (32 tools) | 3-4h | 5 | Test authoring is Composer 2 strongest surface |
| 30 | Stage 6 T6.2 Caller sweep | Grep legacy aliases (`section_heading`/`key`/`doc`/`maxChars`) across `ia/skills/**/SKILL.md`, `.claude/agents/**/*.md`, `ia/rules/**/*.md`, `docs/**/*.md`, `CLAUDE.md`, `AGENTS.md`. Replace with canonical params. Mechanical grep + replace | 2-3h | 5 | Composer 2 multi-file edit sweet spot |
| 31 | Stage 6 T6.3 CI envelope-shape script | `tools/scripts/validate-mcp-envelope-shape.mjs` + wire into `validate:all` composition | 2h | 5 | Node.js scripting, pattern-follow `validate-*.mjs` peers |
| 32 | Stage 6 T6.4 Release prep v1.0.0 | Bump `tools/mcp-ia-server/package.json` to `1.0.0` + `CHANGELOG.md` entry + migration table | 30m | 5 | Straight bookkeeping |
| 33 | Stage 7 T7.1 `issue_context_bundle` tool | New file `tools/mcp-ia-server/src/tools/issue-context-bundle.ts`; fan out to 5 sub-fetches under one `wrapTool` envelope | 3-4h | 4 | Multi-call aggregation; needs careful error-code mapping |
| 34 | Stage 7 T7.2 bundle tests | Happy path + depends-archived + db_unconfigured + not-found | 2h | 5 | Tests |
| 35 | Stage 8 T8.1 orchestrator parser | `tools/mcp-ia-server/src/parser/orchestrator-parser.ts` — parses `ia/projects/*master-plan*.md` → snapshot shape | 3-4h | 4 | Markdown parsing; edge cases numerous |
| 36 | Stage 8 T8.4 parser unit tests | Happy + partial stage tables + status-pointer regex + checkbox variants | 2h | 5 | |
| 37 | Stage 9 T9.1 graph freshness | Extend `glossary-lookup.ts` + `glossary-discover.ts` with mtime check + `refresh_graph` spawn flag | 1-2h | 5 | Single-tool extension |
| 38 | Stage 9 T9.2 freshness tests | Mock `fs.stat`, env override, spawn spy | 1-2h | 5 | Jest + fs mocks |

### 4.5 Web + sprite-gen + exploration (caveman exception OK for web)

| # | Task | Scope | Effort | Fit |
|---|------|-------|--------|-----|
| 39 | Web — console-rack component tests (Rack / Bezel / Screen / LED / TapeReel / VuStrip / TransportStrip); OR wiki seed from glossary (Country, HeightMap, Zone S, Blip, SignalField); OR devlog draft "Lifecycle refactor big-bang — what we changed and why" | 3-4h | 5 |
| 40 | `docs/cursor-as-daily-driver-exploration.md` post-mortem of this 4-day experiment — what Composer 2 nailed, what bungled, division-of-labor recommendation going forward | 2-3h | 5 |

---

## 5. Unblocking Index (cross-ref tasks → rollout cells)

When Claude Code returns, these tasks landing means these rollout cells flip on `/release-rollout`:

| Rollout row | (g) blockers cleared by tasks | Status cell flip enabled |
|-------------|-------------------------------|--------------------------|
| zone-s-economy | #1 + #7 | (g) ◐ → ✓ |
| citystats-overhaul | #2 | (g) ◐ → ✓ |
| city-sim-depth | #3 + #8 (Stage 1.1 scope; Steps 3–5 glossary rows land later) | (g) ◐ → ✓ (partial for Stage 1.1) |
| ui-polish | #4 (Stage 1.1 scope; Steps 2–6 rows land later via filed tasks) | (g) ◐ → ✓ (partial) |
| utilities | #5 + #9 | (g) ◐ → ✓ |
| landmarks | #6 + #10 | (g) ◐ → ✓ |
| distribution | #11 + #12 + #13 + #14 (Stage 1.1 executes directly) | (f) stays ✓, execute drives Stage 1.1 Final on return |
| MCP tool refactor Stage 6 | #29 + #30 + #31 + #32 | Stage 6 → Final, enables Stages 7+ |

---

## 6. 4-Day Schedule (~6h focused work/day)

### Day 1 — unblock align gates + distribution Stage 1.1 warm-up

Goal: clear 4 bucket (g) blockers + land distribution Stage 1.1 Final-able.

- **Morning:** #1 + #2 + #4 glossary batches (zone-s + citystats + ui-polish Stage 1.1 scope). ~2h. `validate:frontmatter` + commit per batch.
- **Midday:** #11 + #12 + #13 + #14 distribution Stage 1.1 — `BuildInfo` SO + asset + `SemverCompare` + tests. ~3h. Pure utility scope, best Composer 2 warm-up.
- **Afternoon:** #7 + #8 spec stubs (`economy-system.md` + `simulation-signals.md`) — frontmatter + §1 + placeholder headings. ~2h.

EOD checkpoint: 4 glossary batches + 4 distribution tasks + 2 spec stubs committed. 10 items. `validate:all` green.

### Day 2 — MCP Stage 6 close-out + city-sim-depth foundation

Goal: drive MCP Stage 6 to Final + land city-sim-depth Stage 1.1 C# burst.

- **Morning:** #29 snapshot tests (MCP envelope). ~3h. One fixture per tool.
- **Midday:** #30 caller sweep (MCP alias removal). ~2h. Grep + replace.
- **Afternoon:** #17 + #18 + #19 city-sim-depth Stage 1.1 C# (`SimulationSignal` + `SignalField` + `SignalFieldRegistry`). ~3h. Single bucket burst.

EOD checkpoint: MCP Stage 6 T6.1 + T6.2 done + 3 city-sim-depth tasks. 5 more items. 15 total.

### Day 3 — MCP Stage 6 release prep + zone-s-economy + remaining unblockers

Goal: Stage 6 shippable + zone-s Stage 1.1 landed + utilities/landmarks glossary done.

- **Morning:** #31 + #32 MCP envelope CI script + v1.0.0 release prep. ~2h.
- **Midday:** #20 + #21 + #22 zone-s-economy Stage 1.1 C# (ZoneType enum + registry + predicate). ~3h. Save-schema adjacent — read spec carefully.
- **Afternoon:** #5 + #6 + #9 + #10 utilities + landmarks glossary batches + spec stubs. ~2h.

EOD checkpoint: MCP Stage 6 shippable at v1.0.0 + zone-s Stage 1.1 landed + 2 more (g) gates cleared. 7 more items. 22 total.

### Day 4 — MCP Stage 7–9 opportunistic + ui-polish + handoff prep

Goal: start MCP composite bundles + polish ui-polish + exploration doc for post-mortem.

- **Morning:** #33 + #34 MCP `issue_context_bundle` tool + tests. ~4h.
- **Midday:** #15 + #16 ui-polish TECH-309 + TECH-310 schema extensions. ~3h.
- **Afternoon:** #39 web work (pick one: component tests OR wiki seed OR devlog). ~2h.
- **Evening:** #40 post-mortem exploration doc. ~1h.

EOD checkpoint: 22+4+2+1+1 = ~30 tasks in 4 days. Branch has ~20-25 commits. Every commit passes `validate:all`. Zero `/closeout` attempted. Branch on `feature/master-plans-1` ready for `/release-rollout` batch advance on Claude Code return.

**Stretch goals** (if any day has spare capacity): #23 (multi-scale audit), #24 (multi-scale test harness), #25 (CityStats StatsFacade), #27 (utilities pool scaffolding), #28 (landmarks catalog + progression), #35 + #36 (MCP orchestrator parser + tests), #37 + #38 (MCP graph freshness).

---

## 7. Claude Code Return Protocol

When weekly limit resets:

1. **Pull branch.** `git log --oneline origin/feature/master-plans-1..HEAD` — review what Cursor landed.
2. **Run `/verify-loop`** on HEAD to confirm no regressions escaped Cursor's `validate:all` gate.
3. **Batch `/closeout`** over completed TECH-XXX issues one at a time. Respect parallel-work rule (one bucket close at a time).
4. **`/release-rollout {row-slug}`** per row where (g) blockers cleared — advances (g) ◐ → ✓ via `release-rollout-track` helper.
5. **`/stage-file`** for any MCP Stage 6+ tasks Cursor authored without filed TECH-XXX — retrofit the id + yaml + spec stub, then `/closeout` them as Final.
6. **Flip any Cursor-authored exploration docs** through `/design-explore --against` if they need gap-analysis vs umbrella before `/master-plan-extend`.
7. **Review #40 post-mortem** — decide which categories to keep in Cursor long-term.

---

## 8. Anti-Patterns to Avoid

- **Do NOT** let Composer 2 write to `.claude/**`, `ia/skills/**/SKILL.md` bodies, `ia/templates/**`, `ia/state/**` — session-context / lifecycle infra; needs Claude Code to validate coherently.
- **Do NOT** manually archive / delete / purge TECH-XXX specs or yaml records. Leave open for Claude Code `/closeout`.
- **Do NOT** run `npm run deploy:web` from Cursor unless Vercel secrets already in shell. Safer: skip deploys.
- **Do NOT** invent new glossary terms not already cited by a filed spec. Term birth should come from `/design-explore` skill output on return.
- **Do NOT** amend or force-push commits after pushing. If Composer 2 produces a bad commit, `git revert` (new commit) instead.
- **Do NOT** trust Composer 2's first draft on save-schema changes. Zone S Stage 1.1 has a v3→v4 bump — read the spec yourself, pair-program, don't auto-accept.
- **Do NOT** skip `npm run validate:all`. Composer 2 confidently ships broken YAML frontmatter. Validator catches it; you must run it.
- **Do NOT** start > 1 bucket's Stage 1.1 C# in the same day — glossary merge conflicts painful, parallel-work rule says sequence buckets.
- **Do NOT** start MCP Stage 6 tasks without first running `npm run validate:all` + `cd tools/mcp-ia-server && npm test` to confirm clean baseline before edits.

---

## 9. Composer 2 Prompt Templates

Paste into Cursor chat when starting each task.

### Template A — execute filed Stage 1.1 task

```
Context: I'm implementing {TECH-XXX} for Territory Developer.
Read the spec at ia/projects/{TECH-XXX}.md in full.
Read ia/rules/invariants.md for hard rules.
Read ia/specs/glossary.md for canonical term usage (do NOT invent new terms).

Do:
1. Implement every goal in §2.1 of the spec, minimal diff.
2. Do NOT touch non-goals in §2.2.
3. Respect invariants #3, #4, #6 (no per-frame FindObjectOfType, no new singletons, no GridManager bloat).
4. After edits, run `npm run validate:all` and `npm run unity:compile-check`.
5. Commit with message `feat({bucket}): TECH-XXX {short summary} — {one-line what}`.
6. Report: files changed, compile status, any deviations from spec.

Do NOT:
- Run /closeout or flip yaml status to archived.
- Touch other TECH-XXX specs.
- Edit ia/skills/, ia/templates/, ia/state/, or .claude/.
```

### Template B — glossary row

```
Add a glossary row to ia/specs/glossary.md for {term}.

Definition source: ia/projects/{TECH-XXX}.md §1 + §4 OR ia/projects/{slug}-master-plan.md Stage X.Y Task intent.
Target section: {Simulation | Economy | UI | Utilities | Landmarks | Zones | ...}.
Row format: | Term | Definition | Spec anchor | (match existing table shape).

After edit, run `npm run validate:frontmatter` and `npm run validate:all`.
Commit: `docs(glossary): add {term} for {bucket} Stage 1.1 align gate`.
Report diff + validator output.
```

### Template C — web content (caveman exception = full English)

```
Writing user-facing copy for web/content/{path}. Full English marketing prose allowed (per ia/rules/agent-output-caveman.md §exceptions #8).

Constraints:
- No backlog ids (TECH-XXX) in copy.
- Use glossary terms verbatim (ia/specs/glossary.md).
- Tone: friendly, technical-literate, no marketing fluff.
- Target: ~400 words for devlog, ~150 words per wiki page.
```

### Template D — MCP Stage 6+ TypeScript (orchestrator-intent-driven)

```
Context: I'm implementing Stage {N} Task T{X.Y} of the MCP lifecycle-tools opus-4.7 audit.

Read:
- ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md — locate Stage {N} Task T{X.Y} row; the Intent paragraph functions as the spec.
- docs/mcp-lifecycle-tools-opus-4-7-audit-exploration.md §Design Expansion — ground truth for the whole refactor.
- tools/mcp-ia-server/src/envelope.ts — ToolEnvelope + wrapTool + ErrorCode (already landed Stage 2.1).
- tools/mcp-ia-server/src/auth/caller-allowlist.ts (if mutation or authorship tool).

Do:
1. Implement the Intent minimally. No gold-plating.
2. Author tests in tools/mcp-ia-server/tests/ matching existing pattern.
3. Run `cd tools/mcp-ia-server && npm test` and `npm run validate:all` (from root) — both green before commit.
4. Commit: `feat(mcp): Stage {N}.T{X.Y} {short summary}`.
5. Report: files changed, test counts, any Intent deviations.

Do NOT:
- File a TECH-XXX issue or create a project spec. Leave for Claude Code return.
- Edit tools/mcp-ia-server/CHANGELOG.md version numbers unless task is explicit release-prep (T6.4).
- Touch other Stage N task scope.
```

---

## 10. Quick Reference — Filed Task Inventory + MCP Pending Inventory

### Filed Stage 1.1 tasks (per rollout tracker 2026-04-18)

- **zone-s-economy:** TECH-278, TECH-279, TECH-280, TECH-281, TECH-282, TECH-283 (6 tasks)
- **blip:** TECH-285, TECH-286, TECH-287, TECH-288 (4 tasks)
- **multi-scale:** TECH-290, TECH-291, TECH-292, TECH-293 (4 tasks)
- **citystats-overhaul:** TECH-303, TECH-304 (2 tasks)
- **city-sim-depth:** TECH-305, TECH-306, TECH-307, TECH-308 (4 tasks)
- **ui-polish:** TECH-309, TECH-310, TECH-311, TECH-312, TECH-313 (5 tasks)
- **music-player:** TECH-316, TECH-317, TECH-318, TECH-319, TECH-320, TECH-321 (6 tasks)
- **utilities:** TECH-331, TECH-332, TECH-333, TECH-334 (4 tasks)
- **landmarks:** TECH-335, TECH-336, TECH-337, TECH-338 (4 tasks)
- **distribution:** TECH-347, TECH-348, TECH-349, TECH-350 (4 tasks)

Total = **43 filed Stage 1.1 tasks** across 10 buckets.

### MCP refactor pending inventory (per `mcp-lifecycle-tools-opus-4-7-audit-master-plan.md`)

- **Stage 6** (envelope foundation final): T6.1 T6.2 T6.3 T6.4 (4 tasks _pending_)
- **Stage 7** (composite bundles): T7.1–T7.4 (4 tasks _pending_)
- **Stage 8** (orchestrator snapshot): T8.1–T8.4 (4 tasks _pending_)
- **Stage 9** (graph freshness + skill sweep): T9.1–T9.4 (4 tasks _pending_)
- **Stages 10–16** (mutations, authorship, bridge, journal, master-plan authoring, mutation_batch, dry-run): ~28 tasks _pending_

Total MCP = **~44 pending TypeScript tasks**. Stages 6–9 (~16 tasks) prioritized this gap; 10–16 deferred to Claude Code return for filed-TECH-XXX discipline + caller-allowlist gating review.

### Composer 2 throughput estimate

4 days × 6h focused = 24h. Task median 1.5h → ~16 tasks realistic. Easy glossary/stub tasks amortize lower (~30m each → allows 4-6 per session block). Target: 25-30 committed deliverables across the gap.

---

## 11. Sources

Composer 2 + Cursor Pro reference (March 2026):

- [Cursor Composer 2: Frontier Agentic Coding Model Debuts — AI CERTs News](https://www.aicerts.ai/news/cursor-composer-2-frontier-agentic-coding-model-debuts/)
- [What Is Cursor Composer 2? — MindStudio](https://www.mindstudio.ai/blog/what-is-cursor-composer-2-coding-model)
- [Introducing Cursor 2.0 and Composer — Cursor blog](https://cursor.com/blog/2-0)
- [Composer 2 Technical Report — Cursor Research](https://cursor.com/resources/Composer2.pdf)
- [Cursor Pro $20 plan analysis — NxCode](https://www.nxcode.io/resources/news/cursor-ai-pricing-plans-guide-2026)
- [Cursor Models & Pricing — official docs](https://cursor.com/docs/models-and-pricing)

Internal cross-refs:

- `ia/projects/full-game-mvp-master-plan.md` — umbrella bucket table + tier lanes
- `docs/full-game-mvp-rollout-tracker.md` — (a)-(g) state per row
- `ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md` — MCP reshape 16 stages, Stages 1-5 Done, 6-16 pending
- `docs/mcp-lifecycle-tools-opus-4-7-audit-exploration.md` — MCP design expansion ground truth
- `docs/cursor-agents-skills-mcp-study.md` — Cursor vs Claude Code surface study
- `docs/agent-lifecycle.md` — parallel-work rule (still enforced; Cursor can't trigger it anyway)
- `ia/rules/invariants.md` — 13 invariants + guardrails

---

*Authored 2026-04-19 ahead of Claude Code weekly limit reset. Review this doc as Task #40 post-mortem at gap end to rate which predictions held.*
