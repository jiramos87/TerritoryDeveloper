---
purpose: Draft → diff → publish → version bump → consumer ref refresh cycle for catalog entities.
audience: operator
last_walkthrough: 2026-04-30
---

# Catalog publish flow

End-to-end publish cycle covering all `catalog_*_publish` MCP tools (sprite, archetype, audio, button, panel, pool, token, asset). Use after a draft has been authored + validated and is ready to bump consumers.

## Pre-conditions

- `territory-ia` MCP server reachable from the agent harness.
- Draft exists: `catalog_{kind}_get` returns `status: 'draft'` with the intended payload.
- No outstanding retired-entity GC clean needed (`npm run gc:catalog -- --dry-run` is clean).

## Steps

1. **Identify draft id.**
   ```
   MCP: catalog_{kind}_search { query: "<title or slug fragment>" }
   ```
   Expected: returns rows including `entity_id` of the draft.

2. **Inspect draft payload.**
   ```
   MCP: catalog_{kind}_get { entity_id: "<id>" }
   ```
   Expected: `status: 'draft'`, all required fields populated.

3. **Diff draft vs latest published.**
   ```
   MCP: catalog_{kind}_get_version { entity_id: "<id>", version: "latest_published" }
   ```
   Compare field-by-field with Step 2 output. Capture diff for the change log.

4. **Run pre-publish validators.**
   ```bash
   cd $REPO_ROOT
   npm run validate:catalog-spine
   npm run validate:all
   ```
   Expected: exit 0 on both.

5. **Check downstream refs.**
   ```
   MCP: catalog_{kind}_refs { entity_id: "<id>" }
   ```
   Expected: list of consumers (other catalog rows, scenes, fixtures). Note count + ids.

6. **Publish.**
   ```
   MCP: catalog_{kind}_publish { entity_id: "<id>" }
   ```
   Expected: returns `version: <n+1>`, `status: 'published'`. Idempotent re-call returns the same version.

7. **Verify version bump.**
   ```
   MCP: catalog_{kind}_get { entity_id: "<id>" }
   ```
   Expected: `status: 'published'`, `version` incremented by 1, `published_at` set to now.

8. **Refresh consumer refs.**
   For each consumer captured in Step 5, run the consumer's own `*_publish` if it pins the version. (Sprites pinned by archetypes; archetypes pinned by scenes via `data/scenes/`.) Skip when consumer uses floating `latest_published`.

9. **Smoke dashboard + scene.**
   ```bash
   npm --prefix web run dev
   ```
   Open `http://localhost:3000/dashboard`; assert no red banners; navigate to a scene that consumes the published entity; assert it renders.

10. **Record the publish in the change log.**
    Capture: entity_id, kind, prev → new version, diff summary from Step 3, timestamp.

## Failure-recovery branches

- **Step 4 catalog-spine fails** → DO NOT publish. Run `npm run gc:catalog -- --dry-run` first to confirm no aged-retired rows are blocking; investigate validator output verbatim.
- **Step 6 returns `entity_retired`** → entity was retired between Step 2 and Step 6; restore via `catalog_{kind}_restore` first, re-run from Step 2.
- **Step 9 dashboard 500** → publish committed but consumer cache stale; reload web server (`Ctrl-C` + `npm --prefix web run dev`).

## Per-kind notes

- `sprite` — blob URI must resolve via `gen://{run_id}/{variant_idx}` against `var/blobs/`; orphan sweep dashboard widget should not jump after publish.
- `archetype` — pins sprite + audio refs by version; bumping a sprite does NOT auto-bump archetypes that pinned a prior sprite version.
- `pool` — spawn-pool publish triggers re-roll; consumers should refetch.
- `audio` / `button` / `panel` / `token` / `asset` — same draft → publish → bump pattern.

### Drift notes

(Record any command that needed adjustment during the most recent walkthrough here.)
