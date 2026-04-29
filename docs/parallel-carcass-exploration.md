# Parallel Carcass Master-Plans — Exploration

> **Status:** Design closed — ready for natural / dogfood implementation
> **Date:** 2026-04-29
> **Author:** Javier (+ agent)
> **Trigger:** linear master-plans → long-lived, drift-prone, sibling drift, slow human feedback. Proposed: architecture-first → fastest end-to-end "carcass" → parallel sessions develop sections concurrently. Stage = sequential unit only **inside** a section, not across plan.
> **Constraint A (DB-first):** master-plans + stages + tasks live in DB (`ia_master_plans`, `ia_stages`, `ia_tasks`, `ia_task_specs`). All planning + execution surfaces are MCP tools. No new file-based artifacts.
> **Constraint B (extend, do not duplicate):** the architecture index (mig 0034 / DEC-A16) — `arch_surfaces`, `arch_decisions`, `arch_changelog`, `stage_arch_surfaces` — plus `ia_stages.depends_on` (mig 0044), `ia_master_plan_health` MV (mig 0045), `master_plan_next_actionable` (TECH-3228), `master_plan_cross_impact_scan` (TECH-3229), `arch_drift_scan` MCP — already implement most primitives this proposal needs.
> **Constraint C (objective alignment):** every primitive must serve at least one of (1) fast prototyping, (2) parallelization of master-plan work, (3) avoidance of arch drift. Section §6 tags each delta with the objective it serves.

---

## 1. Problem statement

- Today: master-plan = strict global ordering Stage 1.1 → 1.2 → … → N.M, even though `depends_on` already supports a DAG. The DAG is rarely populated → topo walk degenerates to numerical order.
- Symptom A — long-lived plans: linear consumption ≈ N stages × stage cycle time. End-shape unobservable until last stage.
- Symptom B — sibling drift: `asset-pipeline` + `game-ui-design-system` + `db-lifecycle-extensions` touch overlapping `arch_surfaces`; `ia_master_plan_health` flags collisions but no plan-shape primitive forces resolution.
- Symptom C — no fanout: human can't drive multiple Claude Code sessions on one plan.
- Symptom D — no carcass milestone: nothing in the schema names the "first end-to-end visible slice" — plan completion is the only built-in milestone.

## 2. Proposal — three moves

### Move 1 — Architecture-first per plan (objective 3 — drift)

At plan birth (before any Stage row exists):

- N rows in `arch_decisions` scoped to the plan via new `plan_slug` column (e.g. `slug = 'plan-{slug}-boundaries'`).
- M rows in `arch_surfaces` for new owned surfaces.
- `ia_master_plans.architecture_locked_at timestamptz` + `locked_commit_sha text` stamped at end of authoring Phase A.
- **Hard lock trigger** on `arch_decisions`: UPDATE on `plan_slug` row blocked when parent plan has `architecture_locked_at IS NOT NULL`, except `status='superseded'` flip (single legal exit).

### Move 2 — Carcass milestone (objective 1 — fast prototyping)

- New column `ia_stages.carcass_role text` ∈ {`carcass`, `section`, NULL}.
- **Cardinality cap ≤ 3 carcass stages per plan** — enforced by Phase B authoring + DB CHECK reading `carcass_signal_kinds.max_carcass_stages_per_plan` config row.
- **Carcass-internal default = no `depends_on` edges.** Carcass stages are maximally parallel from day 0. Authoring skill warns if a carcass→carcass edge is introduced.
- "Noticeable" = `carcass_signal_kinds` table (extensible enum). Each carcass stage links to ≥1 signal kind via `stage_carcass_signals`.
- **Sections-imply-carcass invariant** (DB CHECK): a plan with any `carcass_role='section'` row MUST have ≥1 `carcass_role='carcass'` row.
- Plan health MV gains `carcass_done bool` — derived gate.

### Move 3 — Sections (objective 2 — parallelization)

- **Explicit identity** via new column `ia_stages.section_id text` (NULL for carcass + legacy stages). Author assigns at Phase C; `master_plan_sections` MCP returns sections grouped by `section_id`. Cluster detection on `arch_surfaces` becomes a **validation pass** (assert author-declared section matches surface clustering), not the source of truth.
- **Two-tier claim** — section-level + stage-level:
  - `ia_section_claims (slug, section_id, session_id, claimed_at, last_heartbeat, released_at)` PK `(slug, section_id)`.
  - `ia_stage_claims (slug, stage_id, session_id, claimed_at, last_heartbeat, released_at)` PK `(slug, stage_id)`.
  - `master_plan_next_actionable` returns a stage only if its section is unclaimed OR claimed by current session; AND the stage itself is unclaimed OR claimed by current session.
- Parallel execution: each main session calls `/section-claim foo {section_id}` → opens worktree at `../territory-developer.section-{section_id}` on branch `feature/{slug}-section-{section_id}` → runs `/ship-stage` per member stage → `/section-closeout` at the end.

```
Plan birth
   │
   ├─► seed arch_decisions (locks)              [Move 1]
   ├─► seed arch_surfaces (owned + shared)
   ├─► UPDATE ia_master_plans                   architecture_locked_at + sha
   │     │ trigger arms — plan-scoped arch_decisions become read-only
   │
   ├─► author ≤3 carcass stages                 [Move 2]
   │     · maximally parallel (no internal deps default)
   │     · linked to signal_kinds
   │
   └─► author N section stages                  [Move 3]
         · explicit section_id
         · depends_on edges INSIDE each section (sequential)
         · each section root depends on ≥1 carcass stage
         │
         ▼
[carcass wave]   parallel-internal, breadth-first stubs
         │  carcass_done = true → gate opens
         ▼
[next_actionable returns N section roots]
         │
   ┌─────┼──────┬──────┬──────┐
   ▼     ▼      ▼      ▼      ▼
 sess1 sess2  sess3  sess4  …      ← /section-claim + worktree per session
   │     │      │      │      │
   │     │      │      │      │  /ship-stage per stage:
   │     │      │      │      │    · pre: stage_claim + assert section claim
   │     │      │      │      │    · Pass B: arch_drift_scan(intra-plan)
   │     │      │      │      │    · post: stage_claim_release
   │     │      │      │      │
   ▼     ▼      ▼      ▼      ▼
 /section-closeout   /section-closeout   …   ← per section
         │
         ▼
[plan_done — all sections satisfied]
```

## 3. Why "Stage" survives only inside sections

- `Stage` semantics (atomic shippable + verify-loop + closeout) unchanged.
- What changes: the global *ordering* assumption. With `depends_on` populated and `master_plan_next_actionable` driving claim, "Stage 1.1 → 1.2 → … " is one possible topo linearization, not the contract.
- Sections impose local order via `depends_on` chains within the cluster. Cross-section order is loose — only the carcass→section gate is hard.

## 4. Implementation plan target

User confirmed: NOT a `*-master-plan.md`. DB-first, dogfood-driven. The implementation itself uses the new shape — bootstrap order resolves chicken-and-egg (§7).

---

## 5. Decision log

| # | Decision | Resolution |
|---|----------|------------|
| D1 | Carcass criteria shape | Extensible — `carcass_signal_kinds` table |
| D2 | Architecture-first persistence | `arch_decisions` rows + new `plan_slug` column |
| D3 | Carcass marker on stages | `ia_stages.carcass_role` enum column |
| D4 | Parallel coordination | `ia_stage_claims` + `ia_section_claims` (2-tier) + worktree per section |
| D5 | Migration scope | Future plans only; pilot on next plan birth |
| D6 | Authoring flow | Extend `master-plan-new` with Phases A/B/C |
| D7 | Section closeout | `section_closeout_apply` (pure-DB) + skill-side git merge |
| D8 | Branch model | Worktree per section on `feature/{slug}-section-{section_id}` |
| D9 | Drift detection | Extend `arch_drift_scan` with `scope='intra-plan'` + per-stage hook in `/ship-stage` Pass B |
| D10 | Visibility | Extend `ia_master_plan_health` MV |
| D11 | Final impl-plan persistence | Dogfooding — plan itself uses new shape |
| D12 | Carcass gate semantics | Hard gate — `next_actionable` returns only carcass stages until `carcass_done=true` |
| D13 | Claim release | Auto on closeout + heartbeat sweep + manual escape |
| D14 | Lock seal columns | `architecture_locked_at` + `locked_commit_sha` on `ia_master_plans` |
| D15 | Carcass cardinality | ≤ 3 per plan (config-driven, extensible) |
| D16 | Carcass internal deps | Default none — maximally parallel internally |
| D17 | Lock enforcement | Trigger blocks in-place UPDATE on locked plan-scoped `arch_decisions` rows |
| D18 | Sections-imply-carcass | DB CHECK + Phase B authoring fail |
| D19 | Section identity | Explicit `ia_stages.section_id` column |
| D20 | `/ship-stage` integration | Pre-step `stage_claim` + section assertion; Pass B `arch_drift_scan(intra-plan)`; post-step release |
| D21 | MCP boundary | `section_closeout_apply` MCP pure-DB; git merge in `/section-closeout` skill |
| D22 | Bootstrap shape | Single combined Wave 0 (primitives + skill extensions); dogfood as Wave 1 |

---

## 6. Design — final (deltas folded)

### 6.1 Schema delta

**Migration `0049_parallel_carcass_primitives.sql` — combined:**

```sql
BEGIN;

-- Move 1 — arch_decisions plan-scope + lock seal --------------------------

ALTER TABLE arch_decisions
  ADD COLUMN plan_slug text;        -- NULL = global decision (DEC-A*)
                                    -- non-NULL = plan-scoped lock
CREATE INDEX arch_decisions_plan_slug_idx ON arch_decisions (plan_slug)
  WHERE plan_slug IS NOT NULL;

ALTER TABLE ia_master_plans
  ADD COLUMN architecture_locked_at timestamptz,
  ADD COLUMN locked_commit_sha      text;

-- Trigger: block in-place UPDATE on locked plan-scoped arch_decisions
--          (D17). Single legal exit: status='superseded'.
CREATE OR REPLACE FUNCTION arch_decisions_locked_guard()
RETURNS TRIGGER AS $$
DECLARE
  v_locked timestamptz;
BEGIN
  IF NEW.plan_slug IS NULL THEN
    RETURN NEW;            -- global decisions unaffected
  END IF;
  SELECT architecture_locked_at INTO v_locked
    FROM ia_master_plans WHERE slug = NEW.plan_slug;
  IF v_locked IS NULL THEN
    RETURN NEW;            -- pre-lock authoring window
  END IF;
  -- Locked: only supersession may flip status; no other field may move.
  IF (NEW.status = 'superseded' AND OLD.status = 'active'
      AND NEW.title = OLD.title
      AND NEW.rationale = OLD.rationale) THEN
    RETURN NEW;
  END IF;
  RAISE EXCEPTION
    'arch_decisions row % is locked (plan % locked at %); only status-flip to superseded is allowed',
    NEW.slug, NEW.plan_slug, v_locked
    USING ERRCODE = 'P0001';
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS arch_decisions_locked_guard_t ON arch_decisions;
CREATE TRIGGER arch_decisions_locked_guard_t
  BEFORE UPDATE ON arch_decisions
  FOR EACH ROW EXECUTE FUNCTION arch_decisions_locked_guard();

-- Move 2 — carcass role + signal kinds + cardinality cap ------------------

ALTER TABLE ia_stages
  ADD COLUMN carcass_role text
    CHECK (carcass_role IN ('carcass','section') OR carcass_role IS NULL),
  ADD COLUMN section_id   text;     -- NULL for carcass + legacy stages
CREATE INDEX ia_stages_carcass_role_idx ON ia_stages (slug, carcass_role)
  WHERE carcass_role IS NOT NULL;
CREATE INDEX ia_stages_section_id_idx  ON ia_stages (slug, section_id)
  WHERE section_id IS NOT NULL;

CREATE TABLE carcass_signal_kinds (
  slug         text PRIMARY KEY,
  label        text NOT NULL,
  verify_hint  text,
  created_at   timestamptz NOT NULL DEFAULT now()
);
INSERT INTO carcass_signal_kinds (slug, label, verify_hint) VALUES
  ('visible_ui',          'Visible UI/UX change',           'open game/web; eyeball'),
  ('dev_loop_affordance', 'New dev-loop affordance',        'invoke new command/MCP'),
  ('agent_capability',    'New agent capability via bridge','call new MCP tool'),
  ('runnable_prototype',  'Running prototype humans poke',  'launch standalone artifact');

-- Config row: cardinality cap (extensible without schema change).
CREATE TABLE carcass_config (
  key    text PRIMARY KEY,
  value  text NOT NULL
);
INSERT INTO carcass_config (key, value) VALUES
  ('max_carcass_stages_per_plan', '3'),
  ('section_count_warn_threshold', '6'),
  ('claim_heartbeat_timeout_minutes', '10');

CREATE TABLE stage_carcass_signals (
  slug          text NOT NULL,
  stage_id      text NOT NULL,
  signal_kind   text NOT NULL REFERENCES carcass_signal_kinds (slug),
  PRIMARY KEY (slug, stage_id, signal_kind),
  FOREIGN KEY (slug, stage_id) REFERENCES ia_stages (slug, stage_id)
    ON DELETE CASCADE
);

-- Carcass cardinality + sections-imply-carcass invariants (D15, D18).
CREATE OR REPLACE FUNCTION ia_stages_carcass_invariants()
RETURNS TRIGGER AS $$
DECLARE
  v_carcass_count int;
  v_section_count int;
  v_cap           int;
BEGIN
  SELECT value::int INTO v_cap
    FROM carcass_config WHERE key = 'max_carcass_stages_per_plan';

  SELECT
    COUNT(*) FILTER (WHERE carcass_role = 'carcass'),
    COUNT(*) FILTER (WHERE carcass_role = 'section')
    INTO v_carcass_count, v_section_count
    FROM ia_stages
   WHERE slug = COALESCE(NEW.slug, OLD.slug);

  IF v_carcass_count > v_cap THEN
    RAISE EXCEPTION 'plan % has % carcass stages; cap is %',
      COALESCE(NEW.slug, OLD.slug), v_carcass_count, v_cap
      USING ERRCODE = 'P0001';
  END IF;
  IF v_section_count > 0 AND v_carcass_count = 0 THEN
    RAISE EXCEPTION 'plan % has section stages but zero carcass stages',
      COALESCE(NEW.slug, OLD.slug)
      USING ERRCODE = 'P0001';
  END IF;
  RETURN COALESCE(NEW, OLD);
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS ia_stages_carcass_invariants_t ON ia_stages;
CREATE CONSTRAINT TRIGGER ia_stages_carcass_invariants_t
  AFTER INSERT OR UPDATE OR DELETE ON ia_stages
  DEFERRABLE INITIALLY DEFERRED
  FOR EACH ROW EXECUTE FUNCTION ia_stages_carcass_invariants();

-- Move 3 — two-tier claim mutex ------------------------------------------

CREATE TABLE ia_section_claims (
  slug            text NOT NULL,
  section_id      text NOT NULL,
  session_id      text NOT NULL,
  claimed_at      timestamptz NOT NULL DEFAULT now(),
  last_heartbeat  timestamptz NOT NULL DEFAULT now(),
  released_at     timestamptz,
  PRIMARY KEY (slug, section_id)
);
CREATE INDEX ia_section_claims_session_idx ON ia_section_claims (session_id)
  WHERE released_at IS NULL;

CREATE TABLE ia_stage_claims (
  slug            text NOT NULL,
  stage_id        text NOT NULL,
  session_id      text NOT NULL,
  claimed_at      timestamptz NOT NULL DEFAULT now(),
  last_heartbeat  timestamptz NOT NULL DEFAULT now(),
  released_at     timestamptz,
  PRIMARY KEY (slug, stage_id),
  FOREIGN KEY (slug, stage_id) REFERENCES ia_stages (slug, stage_id)
    ON DELETE CASCADE
);
CREATE INDEX ia_stage_claims_session_idx ON ia_stage_claims (session_id)
  WHERE released_at IS NULL;

COMMIT;
```

**Migration `0050_master_plan_health_carcass_extension.sql`** — adds derived columns to MV:

- `carcass_done bool` — `(count(carcass) > 0) AND (every carcass stage status='done')`.
- `n_sections int` — `count(distinct section_id) WHERE carcass_role='section'`.
- `n_sections_done int` — sections where every member stage is `done`.
- `sections_in_flight text[]` — section_ids with active `ia_section_claims` rows (formatted `{section_id}@{session_id}`).
- `carcass_cardinality_breach bool` — `count(carcass) > carcass_config.max_carcass_stages_per_plan` (defensive surface alongside trigger).

### 6.2 MCP tool delta

| Tool | Status | Behavior |
|------|--------|----------|
| `master_plan_next_actionable` | **modify** | Honor carcass gate (carcass_done=false → only carcass-role stages). Honor 2-tier claim — return stage only if its section is unclaimed OR claimed by `session_id` arg AND stage itself unclaimed OR claimed by same session. |
| `master_plan_sections` | **NEW** | `({slug})` → returns sections grouped by `section_id`: member stages, owned arch_surfaces, surface-cluster validation result, claim status. |
| `section_claim` | **NEW** | `({slug, section_id, session_id})` → INSERT into `ia_section_claims`; fail if active row exists. UPSERT heartbeat on already-owned. |
| `section_claim_release` | **NEW** | `({slug, section_id, session_id})` → set `released_at = now()` if session matches. Cascade-releases stale stage claims for the section. |
| `stage_claim` | **NEW** | `({slug, stage_id, session_id})` → assert section claim held by same session_id; INSERT stage claim. |
| `stage_claim_release` | **NEW** | Symmetric. |
| `claim_heartbeat` | **NEW** | `({session_id})` → UPDATE both `ia_section_claims` + `ia_stage_claims` for session. Single call refreshes both layers. |
| `claims_sweep` | **NEW** | Background sweep — uses `carcass_config.claim_heartbeat_timeout_minutes`. Releases stale rows in both tables. |
| `section_closeout_apply` | **NEW (pure-DB)** | `({slug, section_id})` → verify all section stages done; verify zero open `arch_drift_scan(scope='intra-plan')` events for section; append `master_plan_change_log` row (`kind='section_done'`); release section claim + cascade stage claims. **Does NOT run git** — caller skill handles merge. |
| `arch_drift_scan` | **modify** | Add `scope` arg ∈ `{global, cross-plan, intra-plan}`. `intra-plan` joins `stage_arch_surfaces × arch_changelog` grouped by section, flags cross-section surface edits. |
| `master_plan_lock_arch` | **NEW** | `({slug, commit_sha})` → set `architecture_locked_at = now()`, `locked_commit_sha = $sha`; `master_plan_change_log_append (kind='arch_locked')`. |

### 6.3 Skill catalogue delta

**`master-plan-new` extended** — new phase order:

```
Phase A — architecture-first (D2 + D17)
  1. resolve plan slug + boundaries
  2. seed arch_surfaces (owned + shared)
  3. seed arch_decisions rows w/ plan_slug = $slug
  4. master_plan_lock_arch({slug, commit_sha=HEAD})
  5. arms the trigger — subsequent in-place UPDATEs on plan-scoped rows blocked

Phase B — carcass-define (D15 + D16 + D18)
  1. assert carcass shape selected for plan (else skip — legacy linear path)
  2. author 1..3 carcass stages (carcass_role='carcass')
       cap enforced by Phase B authoring + DB trigger
  3. for each: link signal_kinds via stage_carcass_signals (≥1 required)
  4. NO depends_on edges between carcass stages (default)
       skill warns if author insists; allowed but flagged
  5. fail-fast if Phase C will produce sections without ≥1 carcass stage

Phase C — section-decompose (D19 + D8)
  1. derive proposed section clusters from end-state contract surfaces
  2. author section stages (carcass_role='section', section_id=$id)
  3. depends_on:
       within section: sequential chain
       across section: forbidden (validated)
       section root → ≥1 carcass stage (validated)
  4. populate stage_arch_surfaces per section (owned surfaces)
  5. master_plan_sections({slug}) validation pass — surface cluster matches author intent
```

**`/section-claim` new skill** — wraps `section_claim` MCP. Opens git worktree at `../territory-developer.section-{section_id}` on branch `feature/{slug}-section-{section_id}`. Starts heartbeat loop (cron-style or per-tool-call).

**`/section-closeout` new skill** — runs after every section stage `done`:

1. `arch_drift_scan(slug, scope='intra-plan', section_id)` — must return 0 open events.
2. `section_closeout_apply` MCP (pure-DB ops).
3. Skill-side git merge — `git merge --no-ff feature/{slug}-section-{section_id}` into `feature/{slug}` plan branch.
4. `claim_heartbeat` final beat → `section_claim_release`.
5. Worktree teardown.

**`/ship-stage` extended (D20)**:

- Pass A pre-step: `stage_claim({slug, stage_id, session_id})` — fails if section not claimed by current session.
- Pass A iterations: periodic `claim_heartbeat`.
- Pass B: existing verify-loop + **new** `arch_drift_scan(scope='intra-plan')` filtered to *other* sections; hard-fail on cross-section drift; soft-warn on intra-section.
- Pass B post-step: `stage_claim_release`.

**`stage-decompose` extended** — emits `carcass_role` + `section_id` on every authored stage.

### 6.4 Workflow — happy path

```
Day 0  (single session — carcass wave)
  /design-explore docs/foo-exploration.md
    → Design Expansion block
  /master-plan-new docs/foo-exploration.md
    Phase A: arch_decisions seeded + master_plan_lock_arch
    Phase B: 1..3 carcass stages, no internal deps, signal_kinds linked
    Phase C: N sections, each section_id assigned, depends_on populated
  /stage-file foo Stage carcass.1
  /ship-stage foo carcass.1   ← Pass B intra-plan drift = 0 (expected)
  ... (all carcass stages, parallel-eligible)
  → ia_master_plan_health.carcass_done = true

Day N  (multiple sessions — section wave)
  Session A:
    /section-claim foo section-A      → worktree + branch + section claim row
    /ship-stage foo section-A.1       → stage_claim + Pass B intra-plan drift scan
    /ship-stage foo section-A.2
    /section-closeout foo section-A   → drift scan + DB closeout + git merge
  Session B (parallel):
    /section-claim foo section-B
    ...
  Session C (parallel): section-C
  ...
  → all sections done → master_plan_close foo
```

### 6.5 Drift + visibility

- **Plan-birth lock seal** (Phase A): `master_plan_lock_arch` arms trigger; subsequent edits to plan-scoped `arch_decisions` blocked except `status='superseded'` (D17).
- **Per-stage drift hook** (`/ship-stage` Pass B): `arch_drift_scan(scope='intra-plan')` filtered to other sections. Hard-fails ship on cross-section drift (D20).
- **Per-section closeout gate** (`/section-closeout`): `arch_drift_scan` must return zero open events (D9).
- **Plan-health surface**: `ia_master_plan_health` MV (D10) extended w/ carcass + section columns. Single MCP call shows full picture.
- **Cross-plan still served by `master_plan_cross_impact_scan`** — unchanged (intra-plan is additive).

### 6.6 Worked example — applying carcass+section to a hypothetical `game-ui-design-system-v2` plan

> Illustration only — not a commitment.

**Phase A — architecture-first locks:**

- `plan-game-ui-design-system-v2-boundaries` — owns `Assets/UI/Theme`, `Assets/Scripts/UI/Themed`, `Assets/UI/Prefabs/Generated`. Read-only on `Assets/UI/Tokens`.
- `plan-game-ui-design-system-v2-end-state-contract` — every HUD row + menu prefab consumes `UiTheme`, no hard-coded RGBA, MCP `theme_render_preview` returns matching pixels.
- `plan-game-ui-design-system-v2-shared-seams` — `UiTheme.asset` (shared with `asset-pipeline`), `UiBakeHandler` bridge command (shared with `agent-led-verification`).

**Phase B — carcass (≤3, parallel-eligible):**

- **carcass.1 — UiTheme v2 schema lands** (signal: `dev_loop_affordance`) — stub theme.asset with new fields, `UiBakeHandler` reads them; one prefab consumes the new field stub.
- **carcass.2 — Theme preview MCP** (signal: `agent_capability`) — `theme_render_preview` returns rendered prefab thumbnails for the stub theme.
- **carcass.3 — One HUD row themed end-to-end** (signal: `visible_ui`) — `vu-meter.prefab` consumes new theme schema; visible in PlayMode batch screenshot.

**Phase C — sections (parallel after carcass):**

- **section-A — Toolbar family** (5 prefabs, sequential internal): `toolbar`, `illuminated-button`, `segmented-readout`, `overlay-toggle-strip`, `themed-overlay-toggle-row`.
- **section-B — Menu family** (4 prefabs).
- **section-C — Renderers** (themed renderers under `Assets/Scripts/UI/Themed/Renderers/`).
- **section-D — Bake pipeline hardening** (`UiBakeHandler` round-trip tests, drift scan integration).
- **section-E — Docs + glossary** (terminology, design-system.md update).

5 sections — under the soft warning threshold (6). Each section claimable by a different main session post-carcass.

---

## 7. Implementation plan — natural prose, dogfood-driven

> **No TECH issue filing in this phase.** Implementation lands naturally. Schema + MCP primitives ship as a single combined wave (Wave 0); the rest of the system rides the new shape (Wave 1 dogfood). Future plans pilot the shape natively (Wave 2).

### Wave 0 — combined bootstrap (D22)

Single ship cycle covers schema, MCP, and skill extensions. Trade-off: bigger PR; benefit: removes inter-wave drift window (delta 1.3 + 3.4 mooted).

**Phase 1 — schema (one migration file):**

- `db/migrations/0049_parallel_carcass_primitives.sql` (entire §6.1 first block).
- `db/migrations/0050_master_plan_health_carcass_extension.sql` (MV extension).
- Run `npm run db:migrate`. Run `npm run validate:all`.

**Phase 2 — MCP tools:**

- New tool files under `tools/mcp-ia-server/src/tools/`:
  - `master-plan-sections.ts` — read-only cluster + validation query.
  - `section-claim.ts` (handles `section_claim`, `section_claim_release`).
  - `stage-claim.ts` (handles `stage_claim`, `stage_claim_release`).
  - `claim-heartbeat.ts` (handles `claim_heartbeat`, `claims_sweep`).
  - `section-closeout-apply.ts` — pure-DB closeout (NO git).
  - `master-plan-lock-arch.ts` — Phase A lock seal.
- Modify `master-plan-next-actionable.ts` — carcass gate + 2-tier claim awareness.
- Modify `arch-drift-scan.ts` — add `scope='intra-plan'` mode.
- Register all new tools in `tools/mcp-ia-server/src/index.ts`.
- Unit + integration tests under `tools/mcp-ia-server/test/`.

**Phase 3 — skill catalogue (via `npm run skill:sync:all`):**

- `ia/skills/master-plan-new/SKILL.md` + `agent-body.md` — Phase A/B/C extension per §6.3.
- `ia/skills/stage-decompose/SKILL.md` — emit `carcass_role` + `section_id`.
- `ia/skills/ship-stage/SKILL.md` + `agent-body.md` — Pass A pre-claim, Pass B intra-plan drift scan, Pass B post-release.
- New `ia/skills/section-claim/{SKILL.md, agent-body.md, command-body.md}`.
- New `ia/skills/section-closeout/{SKILL.md, agent-body.md, command-body.md}`.
- Run `npm run skill:sync:all`.

**Phase 4 — verify:**

- `npm run validate:all` (catches skill drift + DB schema drift).
- `npm run unity:compile-check` (defensive).
- `npm run verify:local`.

**Phase 5 — commit + ship.**

→ At end of Wave 0: every primitive exists. Legacy plans untouched (NULL `carcass_role` → unchanged `next_actionable` behavior).

### Wave 1 — dogfood (first plan birthed in new shape)

**Slug:** `parallel-carcass-rollout` (the rollout plan dogfooding itself for ongoing system polish).

Run `/master-plan-new docs/parallel-carcass-exploration.md` (this doc) → seeds:

**Phase A architecture locks:**

- `plan-parallel-carcass-rollout-boundaries` — owns `db/migrations/0049+`, `tools/mcp-ia-server/src/tools/{master-plan-sections,section-claim,stage-claim,claim-heartbeat,section-closeout-apply,master-plan-lock-arch}.ts`, `ia/skills/{master-plan-new,stage-decompose,ship-stage,section-claim,section-closeout}/`. Read-only on `arch_*` tables (already owned by arch-spec).
- `plan-parallel-carcass-rollout-end-state-contract` — every plan birthed after rollout uses Phase A/B/C; legacy linear path remains for plans that opt out.
- `plan-parallel-carcass-rollout-shared-seams` — `arch_decisions`, `ia_master_plans`, `ia_stages`, `ia_master_plan_health` MV.

**Phase B — 3 carcass stages (parallel-eligible):**

- **carcass.1 — Health MV proves carcass gate.** Seed synthetic plan, assert `next_actionable` returns only carcass stages. Signal: `dev_loop_affordance`.
- **carcass.2 — Two-tier claim demo.** Two sessions race a section_claim; one wins. Signal: `agent_capability`.
- **carcass.3 — End-to-end section closeout.** Synthetic 2-stage section, run `/ship-stage` ×2 + `/section-closeout`, observe branch merge + audit row + drift scan green. Signal: `runnable_prototype`.

**Phase C — sections (parallel after carcass):**

- **section-A — Drift hardening:** integration tests for `arch_drift_scan(scope='intra-plan')`, edge cases, perf bench.
- **section-B — Visibility:** web route `web/app/plans/[slug]/sections/page.tsx` consuming MV; dashboard tile.
- **section-C — Cron sweep:** scheduled `claims_sweep` job + ops docs.
- **section-D — Migration cookbook:** docs/RFC for converting future plans; explicit pilot guidance + example walkthrough (§6.6 promoted).
- **section-E — Skill train pass:** retrospect `master-plan-new`, `stage-decompose`, `ship-stage`, `section-claim`, `section-closeout` after first dogfood. Capture friction, propose deltas via `/skill-train`.

5 sections — within soft warning threshold. Each claimable by a parallel main session.

### Wave 2 — first non-bootstrap pilot

Next exploration → master-plan birth uses Phase A/B/C natively. Validate: end-to-end signal ≤ 1 stage cycle from plan birth (carcass close); section parallelism reduces total wall-clock vs legacy linear baseline.

### Risks + mitigations

| Risk | Mitigation |
|------|-----------|
| `master_plan_next_actionable` change breaks legacy plans (no carcass stages). | Carcass gate fires only if ≥1 `carcass_role='carcass'` row for slug. Legacy slugs (all NULL) → unchanged behavior. |
| Section branch merges produce conflicts. | `arch_drift_scan(scope='intra-plan')` runs per-stage in `/ship-stage` Pass B + per-section in `/section-closeout`; merge happens on green only. |
| Stale claims strand stages. | Heartbeat sweep + manual `claim_release`. |
| Big Wave 0 PR review burden. | Phase 1–4 changes are mechanical + isolated; one commit per phase inside the PR. |
| Lock trigger blocks legitimate edits. | Trigger only fires for `plan_slug IS NOT NULL`; supersession path remains open; skill `/architecture-supersede` (post-MVP) can wrap the workflow. |
| MV refresh cost from new derived columns. | Bench during Phase 4; if > 200ms, split into a separate `ia_plan_section_health` MV refreshed on `ia_*_claims` UPDATE only (D10 fallback). |
| Two-tier claim adds chattiness. | `claim_heartbeat` is a single MCP call refreshing both layers; sessions call once per minute. |

### Definition of done

- Wave 0 PR merged; `validate:all` green.
- Wave 1 `parallel-carcass-rollout` master-plan reaches `done` via the new shape.
- At least one parallel main-session demonstration: 2 sections claimed by 2 sessions concurrently, both close cleanly.
- `ia_master_plan_health` reports `carcass_done=true` + `n_sections_done=n_sections` for the rollout plan.
- `arch_drift_scan(scope='intra-plan')` green on all rollout sections at closeout.
- `/skill-train` proposals captured if friction ≥ 2 across rollout sections.

---

## 8. Next action

User decides invocation cadence. No TECH issue filing in this exploration — implementation lands naturally as the user drives Wave 0 work or, post-Wave 0 primitives, dogfoods the rollout plan via `/master-plan-new docs/parallel-carcass-exploration.md`.
