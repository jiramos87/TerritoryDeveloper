---
purpose: "TECH-5252 — Migration cookbook for carcass+section master-plan shape."
audience: both
loaded_by: ondemand
slices_via: none
---

# Parallel-carcass migration cookbook

> **Status:** Draft (Stage 2.4 / TECH-5252).
> **Audience:** plan authors birthing a new master-plan that needs section-parallel work.
> **Sibling docs:** [`parallel-carcass-exploration.md`](parallel-carcass-exploration.md) (design rationale + worked example source) · [`parallel-carcass-rollout-carcass3-evidence.md`](parallel-carcass-rollout-carcass3-evidence.md) (green-bar trace) · [`parallel-carcass-claims-sweep-ops.md`](parallel-carcass-claims-sweep-ops.md) (mutex sweep ops).

Canonical walkthrough: convert a hypothetical / future master-plan to the carcass+section shape. Five sections cover gate criteria, Phase A (architecture lock seal), Phase B (carcass sizing + signal kinds), Phase C (`section_id` + surface clustering), and a worked example promoting `parallel-carcass-exploration.md §6.6 game-ui-design-system-v2`.

This cookbook is **prescriptive**: copy-pasteable MCP calls + slash-command invocations. The exploration doc explains *why*; this cookbook explains *how*.

---

## §1 Gate criteria — when carcass+section, when flat?

| Trigger | Use carcass+section | Use flat |
|---|---|---|
| Number of stages | ≥ 6 stages with parallelizable surface boundaries | ≤ 5 stages, mostly serial |
| Surface clusters | ≥ 2 disjoint surface clusters (e.g. UI + bridge + docs) | one tight cluster |
| Dev-loop affordance | ≥ 2 carcass-eligible signals (smoke, capability, prototype) | linear ramp |
| Concurrent main sessions | plan owner intends parallel claim by ≥ 2 agents | single-threaded |
| Lock seal viable | clear `boundaries`, `end-state-contract`, `shared-seams` triplet | unclear contract — stay flat |

**Rule of thumb:** carcass+section pays off when section-A through section-E (≤ 5 sections, soft warning at 6) can each be claimed by a different main session post-carcass. Below ≤ 5 stages or without clear surface clusters → flat plan; revisit if scope grows.

**Hard precondition:** Phase A architecture-lock seal MUST land at least 3 plan-scoped `arch_decisions` rows (`boundaries` + `end-state-contract` + `shared-seams`); the recipe verify gate enforces this.

---

## §2 Phase A — architecture-first lock seal

Phase A is **deterministic**: 4 steps, 0 seams, single recipe invocation. End state: `ia_master_plans.architecture_locked_at` set, plan-scoped `arch_decisions` UPDATE-lock trigger armed.

### §2.1 Inputs

| Field | Required | Notes |
|---|---|---|
| `slug` | yes | kebab-case master-plan slug; matches `ia_master_plans.slug` |
| `title` | yes | display heading |
| `description` | no | ≤ 200 chars product overview |
| `preamble` | no | initial preamble markdown |
| `plan_slug` | **yes for plan-scoped seal** | threads into every `arch_decision_write` call + arms verify gate |
| `arch_decisions` | yes | array of ≥ 3 decision objects |
| `actor` | no | change-log row author |

Each `arch_decisions[i]` carries `{slug, title, rationale, alternatives?, surface_slugs?, status?}`. Decision slugs must match `^(DEC-A\d+|plan-[a-z0-9-]+-(boundaries|end-state-contract|shared-seams))$`.

### §2.2 Recipe invocation

Recipe path: `tools/recipes/master-plan-new-phase-a.yaml`. Run via the recipe engine (or `/master-plan-new-phase-a` slash-command when wired).

```yaml
# Inline example — three plan-scoped decisions for slug `my-plan-v2`.
inputs:
  slug: my-plan-v2
  title: "My Plan v2 — carcass+section pilot"
  plan_slug: my-plan-v2
  actor: "plan-author@harness"
  arch_decisions:
    - slug: plan-my-plan-v2-boundaries
      title: "boundaries"
      rationale: "owns Assets/Foo, Assets/Scripts/Bar; read-only on Assets/Tokens"
      surface_slugs: ["Assets/Foo", "Assets/Scripts/Bar"]
    - slug: plan-my-plan-v2-end-state-contract
      title: "end-state-contract"
      rationale: "every Foo prefab consumes BarTheme; no hard-coded RGBA"
    - slug: plan-my-plan-v2-shared-seams
      title: "shared-seams"
      rationale: "BarTheme.asset shared with asset-pipeline; UiBakeHandler bridge"
```

### §2.3 Recipe step trace

| Step | Kind | Action |
|---|---|---|
| `insert_plan` | `mcp` `master_plan_insert` | INSERT `ia_master_plans` row (errors on duplicate slug). |
| `seed_decisions` | `flow: foreach` | per decision, `mcp.arch_decision_write` (upsert by slug; threads `plan_slug`). |
| `verify_seeded_decisions` | `sql: query` | `SELECT COUNT(*) FROM arch_decisions WHERE plan_slug = $1` (skipped if `plan_slug` absent). |
| `verify_count_gate` | `bash` predicate | `verify-seeded-count.sh` trips with exit 1 when count < 3. |
| `head_sha` | `bash` | resolves `git rev-parse HEAD` for the lock seal. |
| `lock_arch` | `mcp` `master_plan_lock_arch` | sets `architecture_locked_at` + arms plan-scoped UPDATE-lock trigger. |

### §2.4 Common errors

| Error code | Cause | Fix |
|---|---|---|
| `unknown_master_plan_slug` | `plan_slug` arg refers to a slug not in `ia_master_plans` | run `insert_plan` first OR pass an existing slug |
| `count_mismatch` | fewer than 3 plan-scoped decisions seeded | add missing rows of the boundaries/end-state-contract/shared-seams triplet |
| `arch_decisions_lock_violated` | UPDATE on a locked decision after `lock_arch` ran | seal happened; un-locking requires schema migration (intentional immutability) |

---

## §3 Phase B — carcass sizing + signal selection

Phase B sits **between** Phase A (architecture lock) and Phase C (section parallelism). Carcass stages prove the contract before sections fan out.

### §3.1 Carcass sizing

| Constraint | Value |
|---|---|
| Min carcass stages | 1 |
| Max carcass stages (hard) | 3 |
| Max carcass stages (soft warn) | 4 |
| Each stage | 1 atomic affordance demonstrating the lock contract |

If you need > 3 carcass stages, the contract is too loose — refine `end-state-contract` until 3 stages can prove it.

### §3.2 Signal kind selection

`stage_carcass_signals.signal_kind` — what does this carcass stage *prove*?

| `signal_kind` | When | Example |
|---|---|---|
| `dev_loop_affordance` | inner-loop validator passes (smoke / lint / health MV) | `next_actionable` returns only carcass stages for new slug |
| `agent_capability` | agent can call a new MCP tool / new bridge mutation | `theme_render_preview` returns rendered prefab thumbnails |
| `runnable_prototype` | end-to-end trace through new primitives, evidence doc bound | section-claim → stage-claim → closeout drift-clean |
| `claim_mutex` | row-only mutex demonstration (rare; usually folded into prototype) | two agents race `section_claim`, PK enforces winner |
| `section_closeout` | section-level affordance proven before fan-out | `section_closeout_apply` rejects non-terminal stages |

**One signal kind per stage** — composite signals dilute the contract. If a stage demonstrates two affordances, split it.

### §3.3 Carcass stage shape

Each carcass stage has:
- `carcass_role = 'carcass'` on `ia_stages`.
- 1 row in `stage_carcass_signals` keyed by `(slug, stage_id, signal_kind)`.
- `section_id IS NULL` (carcass rows never carry a section).
- 1–3 tasks (deeper trees push into Phase C sections).

Carcass evidence binding: when a carcass stage closes, its `signal_kind` row binds to a markdown evidence doc under `docs/{slug}-carcass{N}-evidence.md` (see `parallel-carcass-rollout-carcass3-evidence.md` as the green-bar exemplar).

---

## §4 Phase C — section_id assignment + surface clustering

Phase C fans out parallel sections after carcass closes. This is where **multi-agent parallelism actually happens** — section-A through section-E, each claimable by a different main session.

### §4.1 Section identity

Sections are letter-keyed: `A`, `B`, `C`, `D`, `E` (≤ 5; soft warn at 6, hard cap at 8). Stages within a section number as `section.{LETTER}.{N}` (e.g. `section.A.1`, `section.A.2`).

`ia_stages` rows for section stages carry:
- `carcass_role = 'section'`
- `section_id = 'A'` (or B/C/D/E)
- stage_id like `section.A.1`

### §4.2 Surface clustering rule (DEC-A19)

A section owns a **disjoint surface cluster** — paths under specific directories or specific MCP tools. Two sections must NOT both write to the same surface; cross-section writes trigger `arch_drift_scan(scope='intra-plan')` hard-fail.

| Section | Owns | Read-only on |
|---|---|---|
| section-A | `Assets/UI/Themed/Toolbar/**`, `tools/recipes/toolbar-*` | `Assets/UI/Theme/*` |
| section-B | `Assets/UI/Themed/Menu/**` | `Assets/UI/Theme/*` |
| section-C | `Assets/Scripts/UI/Themed/Renderers/**` | `Assets/UI/Theme/*` |
| section-D | `Assets/Scripts/Editor/Bridge/UiBakeHandler.cs`, bake tests | `Assets/UI/Theme/*` |
| section-E | `docs/**`, `ia/specs/glossary.md` rows | none |

(Example clustering — your plan defines its own boundaries via `arch_surfaces` rows linked to section stages.)

### §4.3 Mutex hierarchy (V2 row-only)

Two-tier claim mutex enforces parallel safety without filesystem locks:

1. **Section claim** — `mcp.section_claim(slug, section_id)` — INSERT-or-fail on `(slug, section_id)`. Only one main session holds the section row at a time.
2. **Stage claim** — `mcp.stage_claim(slug, stage_id)` — INSERT-or-fail on `(slug, stage_id)`; asserts open section row exists when stage carries `section_id`.

Heartbeat: `mcp.claim_heartbeat({slug, stage_id})` refreshes both row's `last_heartbeat`. Stale claims swept by `claims_sweep` past `claim_heartbeat_timeout_minutes` (default 30 min).

Release sequence:
- Stage close → `stage_claim_release(slug, stage_id)` (row-only release; stage stays `done`).
- Section close → `/section-closeout` recipe → `section_closeout_apply` releases the section row + flips all stages to `archived`.

### §4.4 Section closeout

When all stages in a section reach `done`, run `/section-closeout {SLUG} {SECTION_ID}`:

1. `arch_drift_scan(scope='intra-plan', plan_id=SLUG)` — must be drift-clean (no cross-section surface writes).
2. `gate.zero_open_events` — no orphan tasks, no open journal events.
3. `section_closeout_apply(slug, section_id)` — flips section's `done` stages to `archived`, releases the section row, appends change-log row.
4. No git merge — same-branch + same-worktree (parallel agents share branch in this design).

---

## §5 Worked example — `game-ui-design-system-v2` pilot

Source: [`parallel-carcass-exploration.md §6.6`](parallel-carcass-exploration.md). Promoted here as a copy-pasteable template for the next plan author.

**Goal:** rebuild the game UI design system on a new `UiTheme` schema with semantic tokens; thread through every HUD row + menu prefab; bake validation via `theme_render_preview` MCP.

### §5.1 Phase A — architecture lock

```yaml
# tools/recipes/master-plan-new-phase-a.yaml invocation
inputs:
  slug: game-ui-design-system-v2
  title: "Game UI Design System v2"
  description: "Semantic-token UiTheme schema; HUD+menu prefab rethemnig; preview MCP."
  plan_slug: game-ui-design-system-v2
  actor: "plan-author@harness"
  arch_decisions:
    - slug: plan-game-ui-design-system-v2-boundaries
      title: boundaries
      rationale: "owns Assets/UI/Theme, Assets/Scripts/UI/Themed, Assets/UI/Prefabs/Generated; read-only on Assets/UI/Tokens"
      surface_slugs: ["Assets/UI/Theme", "Assets/Scripts/UI/Themed", "Assets/UI/Prefabs/Generated"]
    - slug: plan-game-ui-design-system-v2-end-state-contract
      title: end-state-contract
      rationale: "every HUD row + menu prefab consumes UiTheme; no hard-coded RGBA; theme_render_preview returns matching pixels"
    - slug: plan-game-ui-design-system-v2-shared-seams
      title: shared-seams
      rationale: "UiTheme.asset shared with asset-pipeline; UiBakeHandler bridge shared with agent-led-verification"
      surface_slugs: ["Assets/UI/Theme/UiTheme.asset", "Assets/Scripts/Editor/Bridge/UiBakeHandler.cs"]
```

Verify gate: 3 plan-scoped rows seeded → `verify-seeded-count.sh` exits 0 → `lock_arch` arms the trigger → `architecture_locked_at` set.

### §5.2 Phase B — 3 carcass stages

| Stage | Signal kind | Affordance |
|---|---|---|
| `carcass.1 — UiTheme v2 schema lands` | `dev_loop_affordance` | stub `theme.asset` with new fields, `UiBakeHandler` reads them, one prefab consumes the new field stub |
| `carcass.2 — Theme preview MCP` | `agent_capability` | `theme_render_preview` returns rendered prefab thumbnails for the stub theme |
| `carcass.3 — One HUD row themed end-to-end` | `runnable_prototype` | `vu-meter.prefab` consumes new theme schema; visible in PlayMode batch screenshot |

Each carcass stage closes via `/ship-stage` + binds evidence to `docs/game-ui-design-system-v2-carcass{1,2,3}-evidence.md`. Pattern reference: [`parallel-carcass-rollout-carcass3-evidence.md`](parallel-carcass-rollout-carcass3-evidence.md) — green-bar trace for the rollout-plan's own `carcass.3`.

### §5.3 Phase C — 5 sections

| Section | `section_id` | Surface cluster |
|---|---|---|
| Toolbar family | A | `toolbar`, `illuminated-button`, `segmented-readout`, `overlay-toggle-strip`, `themed-overlay-toggle-row` (5 prefabs, sequential internal) |
| Menu family | B | 4 menu prefabs |
| Renderers | C | `Assets/Scripts/UI/Themed/Renderers/**` |
| Bake pipeline hardening | D | `UiBakeHandler` round-trip tests, drift scan integration |
| Docs + glossary | E | terminology table, `design-system.md` update |

5 sections — under the soft warning threshold. Each section claimable by a different main session post-carcass.

### §5.4 Section run pattern

For each section (parallel main sessions):

1. `mcp.section_claim(game-ui-design-system-v2, A)` — claim section.
2. For each stage in section: `/ship-stage game-ui-design-system-v2 section.A.{N}` — Pass A implements + Pass B closes.
3. After all section stages `done`: `/section-closeout game-ui-design-system-v2 A` — drift scan + gate + section row release.

Drift safety: any cross-section write (e.g. section-A touching `Assets/Scripts/UI/Themed/Renderers/`) trips `arch_drift_scan(scope='intra-plan')` and hard-fails the closeout.

### §5.5 Done state

When all 5 sections close, plan reaches done state per `end-state-contract`. No global git merge needed — same branch, same worktree, evidence trail in MCP + change-log rows.

---

## Cross-references

- **Design rationale + worked example source:** [`parallel-carcass-exploration.md`](parallel-carcass-exploration.md).
- **Green-bar trace exemplar:** [`parallel-carcass-rollout-carcass3-evidence.md`](parallel-carcass-rollout-carcass3-evidence.md).
- **Mutex sweep ops:** [`parallel-carcass-claims-sweep-ops.md`](parallel-carcass-claims-sweep-ops.md).
- **Architecture decisions table:** `arch_decisions` rows where `plan_slug = 'parallel-carcass-rollout'`; surface DEC-A18 (carcass roles), DEC-A19 (section identity + surface clustering).
- **Recipe source:** [`tools/recipes/master-plan-new-phase-a.yaml`](../tools/recipes/master-plan-new-phase-a.yaml).
- **Predicate helper:** [`tools/scripts/recipe-engine/master-plan-new-phase-a/verify-seeded-count.sh`](../tools/scripts/recipe-engine/master-plan-new-phase-a/verify-seeded-count.sh).
