# Full-Game MVP — Rollout Tracker

> **Status:** Active — tracks rollout of every child master-plan under the full-game-mvp umbrella until each reaches step (f) ≥1-task-filed. Permanent artifact; NEVER closeable via `/closeout` (sibling to `ia/projects/full-game-mvp-master-plan.md`).
>
> **Scope:** Rollout management of [`ia/projects/full-game-mvp-master-plan.md`](full-game-mvp-master-plan.md) and its children. Does NOT re-decide scope (parent owns). Tracks lifecycle completion (a)–(g) per row + logs skill bugs encountered during rollout + aggregates per-skill changelog entries.
>
> **Baseline SHA:** `9822c08` (`chore(rollout): snapshot prior agent progress before release-rollout bootstrap`). All "Done" cells below reference this SHA unless newer commit cited.
>
> **Read first if landing cold:**
> - [`ia/projects/full-game-mvp-master-plan.md`](full-game-mvp-master-plan.md) — umbrella orchestrator. 10 buckets + 1 sibling. Source of truth for scope.
> - [`docs/full-game-mvp-exploration.md`](../../docs/full-game-mvp-exploration.md) — full Design Expansion prose.
> - [`ia/rules/agent-lifecycle.md`](../rules/agent-lifecycle.md) — per-bucket lifecycle surfaces.
> - [`ia/skills/release-rollout/SKILL.md`](../skills/release-rollout/SKILL.md) — umbrella rollout skill (authors / maintains this tracker).
>
> **Hard rules:**
> - Tracker rows reflect child master-plans. One row per orchestrator (+ sibling). Parent umbrella NOT a row — tracker operates ON the umbrella's bucket list.
> - Cells hold completion ticket (SHA / doc path / issue id) OR status marker (`—` not started, `◐` partial, `✓` done, `❓` verify, `⚠️` disagreement). Single-char markers enable grep + dashboard parsing.
> - Rollout terminal state = every row column (f) `✓`. No /ship required for rollout completion (execution deferred to handoff agents).
> - Align gate (column (g)): child cannot tick (e) unless new domain entities present in `ia/specs/glossary.md` + relevant `ia/specs/*.md` + resolve via MCP `glossary_lookup` / `router_for_task`.
> - Parallel-work rule (inherited from umbrella): NEVER run `/stage-file` or `/closeout` against two sibling child orchestrators concurrently on same branch.

---

## Rollout steps definition

Columns (a)–(g) below apply to every row. Skill column names short; full semantics here.

| Col | Short | Meaning | Gate |
|-----|-------|---------|------|
| (a) | Enumerate | Row exists in tracker; concern / bucket identified against parent docs | `release-rollout-enumerate` seeds. |
| (b) | Explore | `docs/{slug}-exploration.md` exists AND has `## Design Expansion` block (or semantic equivalent) | `/design-explore` fills. Stubs do NOT tick. |
| (c) | Plan | `ia/projects/{slug}-master-plan.md` exists AND decomposes scope into Steps | `/master-plan-new` or `/master-plan-extend` fills. |
| (d) | Stage-present | ≥1 Stage defined on a Step in the child master-plan | Part of `/master-plan-new` output per `ia/rules/project-hierarchy.md`. |
| (e) | Stage-decomposed | ≥1 Stage decomposed into Phases + Tasks | `/stage-decompose` or native `/master-plan-new` decomposition. |
| (f) | Task-filed | ≥1 decomposed Task filed as BACKLOG row + `ia/projects/{ISSUE_ID}.md` | `/stage-file`. |
| (g) | Align | Glossary + `ia/specs/*.md` cover new domain entities + MCP resolves | Hard gate before (e). Verifies alignment inline. |

Cell glyphs: `✓` done, `◐` partial / in progress, `—` not started, `❓` verify, `⚠️` disagreement with parent docs (see Disagreements appendix). Cells MAY carry a parenthetical ticket: SHA, file path, issue id.

---

## Rollout matrix

Rows = child master-plans, ordered by **designed implementation priority** (not umbrella bucket numbering). `Order` column = rollout pick order; `Tier` column = audit tier (A→E, crit = critical path); `Bucket` column = umbrella bucket mapping for cross-ref. Rationale in §Implementation-order rationale below.

| Order | Tier | Bucket | Row slug | (a) Enum | (b) Explore | (c) Plan | (d) Stage | (e) Decomp | (f) Filed | (g) Align |
|-------|------|--------|----------|----------|-------------|----------|-----------|------------|-----------|-----------|
| 1 | crit | 3 | zone-s-economy | ✓ | ✓ (`docs/zone-s-economy-exploration.md` §Design Expansion line 51 — stub heading but expansion present) | ✓ (`ia/projects/zone-s-economy-master-plan.md` — 3 steps, 9 stages, 54 tasks; 2026-04-17) | ✓ (Steps 1–3 fully decomposed; 9 stages defined) | ◐ (all stages structurally decomposed; (g) align gate pending — glossary rows scheduled TECH-282; `economy-system.md` scheduled Stage 3.3) | ✓ (TECH-278..TECH-283 filed Stage 1.1 — 2026-04-17) | ◐ (10 new glossary rows needed; `economy-system.md` absent; TECH-282 + Stage 3.3 task filed) |
| 2 | A | 7 | blip (audio-polish-and-blip) | ✓ | ✓ (`docs/blip-procedural-sfx-exploration.md`) | ✓ (`ia/projects/blip-master-plan.md`) | ✓ (Steps 1–5 Final; Steps 6–7 skeleton) | ◐ (Stages 5.3–5.4 _pending_; Steps 6–7 skeleton — decompose when Step 5 Final) | ✓ (TECH-285..TECH-288 filed Stage 5.3 — 2026-04-17) | ❓ verify |
| 3 | A | 1 | multi-scale | ✓ | ❓ (no dedicated `docs/multi-scale-exploration.md`; relies on parent §Bucket 1 + `ia/specs/multi-scale-topology.md` — verify Design-Expansion equivalence) | ✓ (`ia/projects/multi-scale-master-plan.md`) | ✓ (Step 2 of 6 in-progress) | ◐ (Steps 1 Final; Step 2 decomposed pending file) | ◐ (prior issues filed; Step 2 Stage 2.1 pending file) | ❓ verify |
| 4 | A | 9 | web-platform | ✓ | ✓ (`docs/web-platform-exploration.md`) | ✓ (`ia/projects/web-platform-master-plan.md`) | ✓ (Steps 1–4 Final; 5–6 Paused) | ✓ (Steps 1–6 decomposed) | ✓ (many issues closed; Step 5 Stage 5.2 active — TECH-263/264/275/276 + BUG-56 archived at `9822c08`) | ❓ verify |
| 5 | A | 8 | citystats-overhaul | ✓ | ✓ (`docs/citystats-overhaul-exploration.md` §Design Expansion line 50) | ✓ (`ia/projects/citystats-overhaul-master-plan.md` new at `9822c08`) | ❓ verify | ❓ verify | — | ❓ verify |
| 6 | B | 2 | city-sim-depth | ✓ | ✓ (`docs/city-sim-depth-exploration.md` §Design Expansion line 62) | ✓ (`ia/projects/city-sim-depth-master-plan.md` new at `9822c08`) | ❓ verify (check master plan Stage presence) | ❓ verify | — | ❓ verify |
| 7 | D | 6 | ui-polish | ✓ | ✓ (`docs/ui-polish-exploration.md` §Design Expansion line 68) | — (`ia/projects/ui-polish-master-plan.md` absent) | — | — | — | — |
| 8 | C+D | 4a | utilities | ⚠️ (parent still names Bucket 4 `utilities-and-landmarks` merged; user split into 4a + 4b — see Disagreements #1) | — (`docs/utilities-exploration.md` stub — no Design Expansion; needs `/design-explore`) | — (`ia/projects/utilities-master-plan.md` absent) | — | — | — | — |
| 9 | C+D | 4b | landmarks | ⚠️ (same as 4a) | — (`docs/landmarks-exploration.md` stub — no Design Expansion; needs `/design-explore`) | — (`ia/projects/landmarks-master-plan.md` absent) | — | — | — | — |
| 10 | E | 5 | sprite-gen | ✓ | ✓ (`docs/isometric-sprite-generator-exploration.md`) | ✓ (`ia/projects/sprite-gen-master-plan.md`) | ✓ (Step 1 of 5 in-progress) | ◐ (Step 1 decomposed; later steps TBD) | ◐ (prior issues filed; Stages 1.3–1.4 pending close) | ❓ verify |
| 11 | E | 10 | distribution | ⚠️ (parent lists as `distribution-master-plan.md` NEW or fold — prior-agent audit recommends fold into web-platform as Step 10; see Disagreements #2) | — | — | — | — | — | — |
| 12 | sibling | — | music-player | ✓ | ✓ (`docs/music-player-jazz-exploration.md`) | ✓ (`ia/projects/music-player-master-plan.md`) | ✓ (Step 1 Stage 1.1 pending file) | ◐ (Stage 1.1 decomposed) | — (Stage 1.1 pending file — next `/stage-file` target) | ❓ verify |

**Row count:** 12 (10 buckets + 1 sibling, with Bucket 4 split into 4a / 4b pending disagreement resolution). Terminal = 12× column (f) `✓`.

**First actionable row (agent rollout entry point):** `zone-s-economy` (Order 1, crit — save-schema v2→v3 lock blocks downstream consumers).

---

## Implementation-order rationale

Order picked per prior-agent audit Tier chain + save-schema critical path:

- **Order 1 (crit) — zone-s-economy:** owns save-schema v2→v3 lock. Every downstream consumer (multi-scale snapshot layout, CityStats overhaul field additions, web-platform dashboard read-model) depends on v3 shape being locked BEFORE they file `/stage-file` work — otherwise re-migration churn. Start here regardless of other readiness.
- **Orders 2–5 (Tier A, parallel-safe EXTENDs, sequential filing):** four orchestrators already at (c) `✓` that need `/master-plan-extend` or `/stage-file` on pending stages. Parallel-safe because each touches disjoint subsystems (audio / world-model / web / UI-stats). Sequential filing required because parallel-work rule forbids concurrent `/stage-file` on sibling orchestrators. Internal order by near-term file readiness: **blip** (Stage 4.1 pending file NOW) → **multi-scale** (Step 2 Stage 2.1 pending file) → **web-platform** (Step 5 Stage 5.2 active) → **citystats-overhaul** (Stage presence verify first).
- **Order 6 (Tier B) — city-sim-depth:** greenfield master plan just authored. Stage presence + decomposition verify before `/stage-file`. After Tier A drains.
- **Order 7 (Tier D) — ui-polish:** exploration done but no master plan. Run `/master-plan-new` → `/stage-file`. No dependency on Order 1 lock.
- **Orders 8–9 (Tier C+D, disagreement-gated) — utilities / landmarks:** both `⚠️` pending user pick on Disagreements #1 (split vs merge). Cannot start agent rollout until user resolves.
- **Orders 10–11 (Tier E) — sprite-gen / distribution:** sprite-gen mid-rollout (Stages 1.3–1.4 pending close — low priority, late MVP polish). distribution `⚠️` pending Disagreements #2 (fold into web-platform vs new orchestrator).
- **Order 12 (sibling) — music-player:** side-quest audio feature, lowest priority. Stage 1.1 pending file when bandwidth available.

Gate recap: Orders 1–7 + 10 + 12 actionable immediately. Orders 8 / 9 / 11 blocked on user disagreement picks (see Disagreements appendix).

---

## Column (g) align gate detail

Column (g) blocks (e) per Q7 answer. Specifically:

- Before a child master-plan can tick (e) `◐` → `✓`, its new domain entities must appear in:
  1. `ia/specs/glossary.md` — new row with canonical English term + definition + cross-ref to spec section.
  2. Relevant `ia/specs/{domain}.md` — canonical section introducing the term.
  3. MCP resolution — `glossary_lookup` + `router_for_task` + `spec_section` return the term + its anchor.

- Skill `release-rollout-track` MUST verify (g) via MCP calls before flipping (e) cell. Failure → (g) = `—`, write note to Skill Iteration Log naming the unresolved terms.

- (g) applies to NEW domain entities only. Existing terms already aligned (country, CountryCell, HeightMap, road stroke, etc.) do not re-verify per rollout.

---

## Disagreements appendix

Persistent conflicts between parent docs, user intent, and child artifacts. Rollout halts affected rows until user resolves.

### #1 — Bucket 4 split (utilities vs landmarks)

- **Parent master plan:** single Bucket 4 slug `utilities-and-landmarks-master-plan.md` (umbrella §Bucket table row 4).
- **Parent exploration:** single Bucket 4 section merging utilities v1 + landmarks progression (line 291 "merges utilities v1 + landmarks"; line 305 "Why merged").
- **User intent (via prior-agent exploration split):** two independent master plans — `utilities-master-plan.md` + `landmarks-master-plan.md`. Separate stub exploration docs already exist.
- **Proposed resolution (pending user pick):**
  - **Option A (split, recommended):** Keep two rows 4a + 4b. Run `/design-explore` on each stub separately. Run `/master-plan-new` on each. Update parent umbrella Bucket 4 row in `full-game-mvp-master-plan.md` to two rows OR note "split into 4a/4b". Update parent exploration doc Bucket 4 section — note the split as a post-plan decision.
  - **Option B (merge back):** Consolidate stubs into one `docs/utilities-and-landmarks-exploration.md`. Single `/master-plan-new`. Aligns with parent docs as written.
- **Gate:** rows 4a + 4b stay `⚠️` until user picks. No `/design-explore` fires until resolved.

### #2 — Bucket 10 distribution (new orchestrator vs fold)

- **Parent master plan:** lists Bucket 10 as "NEW (or fold)" — `distribution-master-plan.md` OR fold into `web-platform-master-plan.md` as Step 10.
- **Prior-agent audit recommendation:** fold. Keeps single CI/CD lane. No new orchestrator needed.
- **Proposed resolution (pending user pick):**
  - **Option A (fold, recommended):** add Bucket 10 scope as Step 10 in `web-platform-master-plan.md`. Tracker row 10 collapses into row 9 tail. `/master-plan-extend ia/projects/web-platform-master-plan.md docs/full-game-mvp-exploration.md` with Bucket 10 slice. Update umbrella bucket table note.
  - **Option B (new):** author standalone `ia/projects/distribution-master-plan.md`. Needs dedicated exploration (`docs/distribution-exploration.md` absent) → seed + `/design-explore` first.
- **Gate:** row 10 stays `⚠️` until user picks. No `/design-explore` fires until resolved.

### #3 — Multi-scale exploration equivalence

- **Observation:** `docs/multi-scale-exploration.md` does NOT exist. Multi-scale Bucket 1 (a)–(c) ticks `✓` because parent exploration §Bucket 1 + `ia/specs/multi-scale-topology.md` cover the scope.
- **Risk:** column (b) gate cannot point at a dedicated Design Expansion block; `/master-plan-extend` skill's Phase 0 pre-condition check may fail.
- **Proposed resolution (low-priority, user call):**
  - **Option A (accept, recommended):** treat parent §Bucket 1 + existing spec as the Design-Expansion equivalent. Document the equivalence here + in child master-plan header.
  - **Option B (backfill):** author `docs/multi-scale-exploration.md` from existing master-plan + spec. Adds surface for future extends.
- **Gate:** row 1 column (b) = `❓` until user picks. Does not block downstream — (c)–(f) already `✓` / `◐` from prior work.

---

## Skill Iteration Log (aggregator)

Entries logged during rollout. Full per-skill bug + fix detail lives in each `ia/skills/{name}/SKILL.md` §Changelog (per Q6 decision). Table below aggregates rollout-scope entries only.

| Date | Skill | Rollout row | Bug / gap | Fix SHA | SKILL.md anchor |
|------|-------|-------------|-----------|---------|-----------------|
| 2026-04-17 | master-plan-extend | (setup) | 6 gaps from prior-agent audit (first-run guardrail, Phase 7a header sync, partial section load, Phase 3 re-fire, umbrella row-flip, duplication playbook) | `9822c08` | [`ia/skills/master-plan-extend/SKILL.md#2026-04-17--6-gap-audit-patches-release-rollout-bootstrap`](../skills/master-plan-extend/SKILL.md#2026-04-17--6-gap-audit-patches-release-rollout-bootstrap) |
| 2026-04-17 | release-rollout | (all rows) | Phase 4 dispatch + §Next step emitted bare `/release-rollout {next-row}` — not paste-ready in terminal. Wrap all emit sites as `claude-personal "/..."`. | _pending_ | [`ia/skills/release-rollout/SKILL.md#2026-04-17--shell-wrap-handoff-commands`](../skills/release-rollout/SKILL.md#2026-04-17--shell-wrap-handoff-commands) |
| 2026-04-17 | release-rollout-enumerate | standalone | Phase 4 + §Next step emitted bare `/release-rollout {UMBRELLA_SPEC} {row-slug}` template — placeholders + no shell wrap. Wrapped to `claude-personal "/..."` + resolve-before-emit reminder. | _pending_ | [`ia/skills/release-rollout-enumerate/SKILL.md#2026-04-17--shell-wrap-phase-4--next-step-handoffs`](../skills/release-rollout-enumerate/SKILL.md#2026-04-17--shell-wrap-phase-4--next-step-handoffs) |
| 2026-04-17 | release-rollout + design-explore | all rows | Phase 4 emitted paste commands instead of calling Agent tool — no autonomous chaining. (b) incomplete had no product-language interview protocol. Fixed: (b) ✓ → Agent chain master-plan-new→stage-file; (b) incomplete → game-design vocabulary interview (≤5 q); `Agent` added to subagent tools. | _pending_ | [`ia/skills/release-rollout/SKILL.md#2026-04-17--autonomous-chain--product-language-interview`](../skills/release-rollout/SKILL.md#2026-04-17--autonomous-chain--product-language-interview) |
| 2026-04-17 | release-rollout | zone-s-economy | Autonomous chain (b)✓→(f) passed through column (e) without stopping for column (g) align gate. Unresolved terms: `Zone S`, `BudgetAllocationService`, `BondLedgerService`, `TreasuryFloorClampService`, `ZoneSService`, `IMaintenanceContributor`, `ZoneSubTypeRegistry`, `IBudgetAllocator`, `IBondLedger`, `envelope (budget sense)`. Also: `ia/specs/economy-system.md` absent. Both gaps scheduled as filed tasks (TECH-282 glossary rows; Stage 3.3 spec authoring). Tracker (e) flipped to ◐ pending align gate resolution; (g) = ◐. Recommendation: TECH-282 is Stage 1.1 Phase 3 — advance it early so downstream tasks have glossary coverage. | _pending_ | *(no per-skill §Changelog required — operational gap, not skill code bug)* |

Rollout agents append rows chronologically. Each entry MUST link to a per-skill `## Changelog` anchor.

---

## Handoff contract for sequential agents

Fresh-context agents pick up rollout without prior conversation state. Contract:

1. **Entry point:** invoke `/release-rollout {row-slug}` (umbrella skill). Agent reads this tracker + targeted row + underlying parent docs via MCP.
2. **State source:** this file + `BACKLOG.md` + MCP (`backlog_issue`, `spec_outline`, `glossary_lookup`, `router_for_task`). No conversation memory required.
3. **Exit artifact:** tracker row cell updates + commit SHA referenced in cell + optional Skill Iteration Log entry if skill bug hit.
4. **No parallel handoff:** one row active at a time per branch (parallel-work rule). Multiple rollout branches OK if they touch disjoint rows + share no skill patches.
5. **Failure mode:** if agent blocks, append `⚠️ (blocker note)` to affected cell, write Skill Iteration Log entry, surface to user. Do NOT force completion.

Handoff task list (Tier A→E order from prior-agent audit, after infrastructure steps 1–7 land):

- **T1 — skill patches:** apply master-plan-extend fixes #1–#6. One commit. Tracker Skill Iteration Log row fix SHA.
- **T2 — disagreement resolution:** user picks for #1 (Bucket 4 split) + #2 (distribution fold) + #3 (multi-scale equivalence). Tracker updated.
- **T3 — Bucket 3 priority (save-schema lock):** if chosen path for (zone-s-economy) is design + new plan, run `/design-explore docs/zone-s-economy-exploration.md` (verify Design Expansion already present per matrix — skip to /master-plan-new). `/master-plan-new docs/zone-s-economy-exploration.md`. `/stage-file` Stage 1.
- **T4 — Tier A EXTENDs (parallel-safe, sequential filing):** `/master-plan-extend` for blip + multi-scale + web-platform + citystats-overhaul per audit.
- **T5 — Tier C design-explore stubs:** utilities + landmarks (if split) or utilities-and-landmarks (if merge back).
- **T6 — Tier D NEW master plans:** utilities / landmarks / ui-polish → `/master-plan-new` each.
- **T7 — Tier B greenfield NEW:** city-sim-depth verify + close any (c)/(d)/(e) gaps.
- **T8 — Tier E sprite-gen merge + distribution fold:** per user resolution of #2.
- **T9 — per-row `/stage-file`:** ≥1 stage filed per row → column (f) `✓`.

Each handoff agent touches ONE T-step at a time. Appends tracker row cell + Skill Iteration Log + commit SHA.

---

## Change log

| Date | Delta | Author |
|------|-------|--------|
| 2026-04-17 | Initial authoring — tracker scaffold seeded from parent master-plan + exploration docs + repo state at `9822c08`. 11 rows pre-filled. 3 disagreements logged. Skill Iteration Log seeded with 1 pending entry (master-plan-extend patches). | release-rollout infrastructure bootstrap |
| 2026-04-17 | Matrix re-ordered by designed implementation priority (Tier chain + save-schema critical path). Added `Order` + `Tier` columns. `zone-s-economy` pinned Order 1 (crit). §Implementation-order rationale appended. | rollout order lock |
| 2026-04-17 | `release-rollout` + `release-rollout-enumerate` skills patched — handoff commands now shell-wrapped as `claude-personal "/..."` with resolved args per `feedback_exact_command_handoff.md`. 2 aggregator rows appended, 2 per-skill Changelog entries. | skill handoff shell-wrap fix |
| 2026-04-17 | Autonomous chain added: (b) ✓ → Agent tool calls master-plan-new → stage-file without user pause. (b) incomplete: product/game-design language interview (no C# internals, ≤5 q). `Agent` tool added to subagent. Skill Iteration Log row appended. | autonomous chain + product-language interview |
| 2026-04-17 | `zone-s-economy` row advanced: (c)✓ master-plan authored (3 steps / 9 stages / 54 tasks) → (d)✓ stages defined → (e)◐ structurally decomposed but (g) align gate pending 10 glossary rows + `economy-system.md` spec (TECH-282 + Stage 3.3) → (f)✓ TECH-278..TECH-283 filed Stage 1.1. validate:all green. | release-rollout autonomous chain |
| 2026-04-17 | `blip` row advanced: (f)◐ → ✓ — Stage 5.3 filed (TECH-285..TECH-288: LFO types, BlipLutPool stub, SmoothOnePole, LFO routing+glossary). Stale (d)/(f) notes updated (Steps 1–5 Final; Stage 5.3 now filed). validate:all green. | release-rollout autonomous chain |
