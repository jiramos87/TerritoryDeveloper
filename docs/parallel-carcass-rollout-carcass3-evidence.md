# parallel-carcass-rollout — carcass3 evidence

Canonical worked example for **Stage 1.3 — End-to-end section closeout**.
One read-top-to-bottom trace of the V2 row-only parallel-carcass mutex
walking a 2-stage section through claim → heartbeat → stage flips →
section closeout → drift scan clean. Signed-off shape humans can poke;
section-D cookbook anchor.

Bound signal: `stage_carcass_signals(slug='parallel-carcass-rollout',
stage_id='1.3', signal_kind='runnable_prototype')` — written by migration
[`0054_parallel_carcass_runnable_prototype_signal.sql`](../db/migrations/0054_parallel_carcass_runnable_prototype_signal.sql).
Slot shifted from spec-named `0053` to `0054` because slot `0053` was
already taken by `0053_publish_lint_finding.sql` (asset-pipeline Stage
15.1 / TECH-4183) — `§Implementer Latitude` covers numbering correction
when the spec-time monotonic assumption goes stale.

## Setup — fixture topology

Sandbox slug: `parallel-carcass-rollout-carcass3-evidence`. Disjoint
from the test sandboxes (`__test_section_closeout_e2e__` /
`__test_section_closeout_apply__`) so manual replay does not collide
with `node --test` workers.

| Row | Table | Values |
|---|---|---|
| 1 | `ia_master_plans` | `slug='parallel-carcass-rollout-carcass3-evidence', title='carcass3 evidence sandbox'` |
| 2 | `ia_stages` | `slug=…, stage_id='carcass.1', status='done', carcass_role='carcass', section_id=NULL` |
| 3 | `ia_stages` | `slug=…, stage_id='section.A.1', status='pending', carcass_role='section', section_id='A'` |
| 4 | `ia_stages` | `slug=…, stage_id='section.A.2', status='pending', carcass_role='section', section_id='A'` |
| 5 | `ia_tasks` | `id='TECH-XXXX', slug=…, stage_id='section.A.1', status='pending'` |
| 6 | `ia_tasks` | `id='TECH-YYYY', slug=…, stage_id='section.A.2', status='pending'` |

Carcass row pre-done; only the section stages walk forward in this
trace.

## Trace — canonical run

### Step 1 — Section claim

```sql
SELECT * FROM section_claim('parallel-carcass-rollout-carcass3-evidence', 'A');
-- result: { applied: true, claim_id: 1, refreshed: false }
```

Inserts `ia_section_claims` row keyed by `(slug, section_id='A')` with
`released_at IS NULL`, `last_heartbeat = now()`. Concurrent INSERT race
throws `section_claim_held`; subsequent caller refreshes the open row.

### Step 2 — Stage claims (×2)

```sql
SELECT * FROM stage_claim('parallel-carcass-rollout-carcass3-evidence', 'section.A.1');
-- result: { applied: true, claim_id: 1, refreshed: false }

SELECT * FROM stage_claim('parallel-carcass-rollout-carcass3-evidence', 'section.A.2');
-- result: { applied: true, claim_id: 2, refreshed: false }
```

Each `stage_claim` asserts the open section row exists when the stage
carries `section_id` — without an open section row the call throws
`section_claim_required`. Stage rows write to `ia_stage_claims`.

### Step 3 — Heartbeat refresh

```sql
SELECT * FROM claim_heartbeat(
  'parallel-carcass-rollout-carcass3-evidence',
  'section.A.1'
);
-- result: { section_claims_refreshed: 1, stage_claims_refreshed: 1,
--           section_id: 'A', stage_id: 'section.A.1' }
```

Single stage heartbeat refreshes both rows: target stage row in
`ia_stage_claims` + parent section row in `ia_section_claims` (looked
up via `ia_stages.section_id`). Operator-driven from `/ship-stage`
Pass A iterations; background sweep (`claims_sweep`) releases rows past
`carcass_config.claim_heartbeat_timeout_minutes` (default 10).

### Step 4 — Stage status flips (×2)

```sql
-- Walk each section stage's task pending → implemented → verified → done
-- via mutateTaskStatusFlip, then close the stage.

UPDATE ia_tasks SET status = 'implemented' WHERE id = 'TECH-XXXX';
UPDATE ia_tasks SET status = 'verified'    WHERE id = 'TECH-XXXX';
UPDATE ia_tasks SET status = 'done'        WHERE id = 'TECH-XXXX';

SELECT * FROM stage_closeout_apply(
  'parallel-carcass-rollout-carcass3-evidence', 'section.A.1'
);
-- result: { stage_status: 'done', archived_task_count: 1 }
```

Repeat for `section.A.2` / `TECH-YYYY`. Both section stages now
`status='done'`. Stage-level closeout does NOT release stage claims —
release fires only on section-level closeout. Stage claims stay open
post-step-4 (release_at IS NULL).

### Step 5 — Section closeout

```sql
SELECT * FROM section_closeout_apply(
  'parallel-carcass-rollout-carcass3-evidence', 'A'
);
-- result: {
--   applied: true,
--   stages_total: 2,
--   stages_done: 2,
--   change_log_entry_id: 42,
--   section_claim_released: true,
--   cascaded_stage_releases: 2,
--   error: null
-- }
```

Atomic mutate:

1. Asserts every stage where `(slug, section_id='A')` has
   `status='done'`. Partial-done → `applied=false, error='stages_not_done'`,
   no further side effects.
2. Writes `ia_master_plan_change_log` row `kind='section_done'` with
   body JSON `{section_id: 'A', stages: ['section.A.1', 'section.A.2']}`.
3. Releases the section claim (`ia_section_claims.released_at = now()`).
4. Cascade-releases stage claims by row key alone (joined to `ia_stages`
   by `section_id='A'`).

### Step 6 — Drift scan clean

```sql
SELECT * FROM arch_drift_scan(
  plan_id => 'parallel-carcass-rollout-carcass3-evidence',
  scope   => 'intra-plan',
  section_id => 'A'
);
-- result: { affected_stages: [] }
```

Intra-plan drift scan filtered to the closed section returns zero
affected stages. Closes the carcass3 acceptance loop: section closeout
left the plan internally consistent.

## Outcome — terminal DB state

| Table | Row | Field | Value |
|---|---|---|---|
| `ia_section_claims` | `(slug, 'A')` | `released_at` | `<timestamp>` (released) |
| `ia_stage_claims` | `(slug, 'section.A.1')` | `released_at` | `<timestamp>` (released) |
| `ia_stage_claims` | `(slug, 'section.A.2')` | `released_at` | `<timestamp>` (released) |
| `ia_stages` | `(slug, 'section.A.1')` | `status` | `done` |
| `ia_stages` | `(slug, 'section.A.2')` | `status` | `done` |
| `ia_master_plan_change_log` | `(slug, 42)` | `kind` | `section_done` |
| `ia_master_plan_change_log` | `(slug, 42)` | `body` | `{"section_id":"A","stages":["section.A.1","section.A.2"]}` |
| `stage_carcass_signals` | `(slug='parallel-carcass-rollout', '1.3', 'runnable_prototype')` | row count | 1 |

## Cross-references — executable analog

This doc is the human-readable trace. The two `node --test` files
below execute the same shape against synthetic sandbox slugs and
assert each side effect:

- [`tools/mcp-ia-server/tests/tools/section-closeout.e2e.test.ts`](../tools/mcp-ia-server/tests/tools/section-closeout.e2e.test.ts)
  — TECH-5070. Drives a 2-section topology end-to-end pending →
  implemented → verified → done via `mutateTaskStatusFlip` +
  `mutateStageCloseoutApply`. Asserts the V2 release-timing rule —
  stage-level closeout does NOT release stage claims; releases cascade
  only when section-level closeout fires.
- [`tools/mcp-ia-server/tests/tools/section-closeout-apply.test.ts`](../tools/mcp-ia-server/tests/tools/section-closeout-apply.test.ts)
  — TECH-5071. Drives `applySectionCloseout` happy + negative paths.
  Happy path asserts all 7 result fields, change_log JSON body shape,
  drift scan empty, claims released. Negative path flips only 1 of 2
  stages done → asserts `applied=false, error='stages_not_done'`,
  change_log unchanged, claims still open.

Shared fixture:
[`tools/mcp-ia-server/tests/fixtures/section-closeout.fixture.ts`](../tools/mcp-ia-server/tests/fixtures/section-closeout.fixture.ts).

## Sibling docs

- [`docs/parallel-carcass-exploration.md`](parallel-carcass-exploration.md) §6 — design
  rationale + section-D migration cookbook.
- [`docs/parallel-carcass-rollout-tracker.md`](parallel-carcass-rollout-tracker.md) — rollout state.
