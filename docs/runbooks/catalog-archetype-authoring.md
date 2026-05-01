---
purpose: Spawn new archetype via MCP → fill required fields → wire sprite + audio refs → publish → consume.
audience: operator
last_walkthrough: 2026-04-30
---

# Catalog archetype authoring

Step-by-step authoring cycle for a new archetype (a versioned catalog row that pins sprite + audio refs and gets consumed by scenes / pools / tests).

## Pre-conditions

- `territory-ia` MCP server reachable.
- Sprite + audio entities for the archetype are already published (or will be created here in Steps 2–3).
- Glossary entry for the archetype name exists (`mcp__territory-ia__glossary_lookup { key: "<archetype-name>" }`).

## Steps

1. **Reserve archetype draft.**
   ```
   MCP: catalog_archetype_create {
     title: "<archetype title>",
     payload: { kind: "<archetype-kind>", description: "<one line>", refs: {} }
   }
   ```
   Expected: returns `entity_id` of the new draft (`status: 'draft'`).

2. **Wire sprite ref (create + publish if missing).**
   - If the sprite already exists: `catalog_sprite_search { query: "..." }` → capture `sprite_id`.
   - If new:
     ```
     MCP: catalog_sprite_create { title: "...", payload: { source_uri: "gen://<run_id>/<variant>", ... } }
     ```
     Then publish via the [catalog-publish-flow.md](catalog-publish-flow.md) runbook before continuing.

3. **Wire audio ref (same shape).**
   ```
   MCP: catalog_audio_search { query: "..." } | catalog_audio_create { ... }
   ```
   Publish via publish-flow runbook.

4. **Update archetype draft with refs.**
   ```
   MCP: catalog_archetype_update {
     entity_id: "<draft_id>",
     payload: {
       refs: {
         sprite: { entity_id: "<sprite_id>", version: <n> },
         audio:  { entity_id: "<audio_id>",  version: <m> }
       },
       behavior: { ... }
     }
   }
   ```
   Expected: returns updated draft with refs populated.

5. **Validate before publish.**
   ```bash
   cd $REPO_ROOT
   npm run validate:catalog-spine
   ```
   Expected: exit 0.

6. **Publish archetype.**
   ```
   MCP: catalog_archetype_publish { entity_id: "<draft_id>" }
   ```
   Expected: returns `version: 1`, `status: 'published'`.

7. **Verify consumers can resolve.**
   ```
   MCP: catalog_archetype_refs { entity_id: "<draft_id>" }
   ```
   Expected: empty (new archetype) or shows scenes / pools that already pinned a placeholder.

8. **Consume in a scene / fixture.**
   - Scene: edit `data/scenes/<scene>.json` → add archetype ref `{ entity_id, version }` → run `npm run scenario:build-from-descriptor`.
   - Fixture / test: edit the fixture; cite the archetype `entity_id` + `version`; run `npm run validate:fixtures`.

9. **Smoke in editor (Path A or Path B).**
   - Path A (batch test mode):
     ```bash
     npm run unity:testmode-batch -- --scenario-id <scene-id>
     ```
   - Path B (bridge in Editor):
     ```
     MCP: unity_bridge_command { ... apply scene ... }
     MCP: unity_bridge_get { ... assert entity instantiated ... }
     ```
   Expected: archetype renders / spawns / responds as designed.

10. **Record the new archetype in the change log.** Capture entity_id, version, ref ids, scene/fixture consumers.

## Failure-recovery branches

- **Step 1 fails on glossary miss** → add glossary row first (see `ia/specs/glossary.md`); re-run `npm run generate:ia-indexes` and Step 1.
- **Step 6 reports `unresolved_refs`** → one of sprite/audio refs is still draft; publish dependents first (Step 2 / 3); retry.
- **Step 9 entity does not render** → check `var/blobs/` resolves the sprite URI (`gen://...`) — if missing, regenerate the sprite blob (consult sprite-gen runbook if it exists) before retrying.

### Drift notes

(Record any command that needed adjustment during the most recent walkthrough here.)
