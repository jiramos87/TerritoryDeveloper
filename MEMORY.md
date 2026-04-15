# Project memory — Territory Developer

Index of architectural decisions and durable context that doesn't fit cleanly in
specs, commit messages, or BACKLOG rows. One line per entry. Promote an entry to
its own file under `.claude/memory/{slug}.md` only when it grows past ~10 lines.

Format: `- [Title](path-or-anchor) — one-line hook`

## Architecture decisions

_(none indexed — promote entries here only when they exceed ~10 lines or need cross-session recall beyond what specs / rules / `CLAUDE.md` already capture.)_

## CLI / tooling tips

- macOS `sed` does not support `\b` word boundaries; use `perl -pi -e 's/\bFoo\b/Bar/g'` for symbol renames from CLI agents.
- `git mv` on a Unity `.cs` file leaves the adjacent `.meta` at the original name; `git mv` the `.meta` separately to preserve the GUID (prefab / scene references survive).
- Bridge `get_compilation_status` is a reliable compile gate when the Unity Editor holds the project lock and batchmode is blocked.
- Before filing a follow-up TECH from a project-spec audit, grep `BACKLOG.md` for an existing matching scope — duplicates waste id space and split tracking (TECH-04 already covered the Stage 1.2 invariant #5 cleanup scope).
- `InterstateManager.GenerateAndPlaceInterstate()` is the canonical single-call entry for scripted interstate builds; internally runs `RoadManager.PlaceInterstateFromPath` → `TryPrepareRoadPlacementPlan` → `PathTerraformPlan.Apply` → `InvalidateRoadCache()` → `NeighborCityBindingRecorder.RecordExits`. Satisfies invariants #2 + #10 — avoid direct `ComputePathPlan`.
- `MapGenerationSeed.SetSessionMasterSeed(int)` pins deterministic new-game seeds for testmode (consumed by `AgentTestModeBatchRunner -testSeed N`).
- Before authoring a spec that introduces a new static-helper class, grep the target namespace for the proposed name — nested patch-data structs (e.g. `BlipEnvelope` inside `BlipPatchTypes.cs`) collide w/ top-level type declarations and only surface at compile time (CS0101). Suffix helpers w/ `-Stepper` / `-Builder` / `-Service` when the bare noun already exists.
- Edit tool replacing a `for` loop header: include full body in `old_string`. Header-only replace orphans body block.
- `AutoZoningManager.ZoneSegmentStrip`: loop upper bound + segment removal condition (`zonedUpToIndex >= L - N`) are two sites that must stay consistent — update both when extending endpoint coverage.
- `SelectZoneTypeForSegment(CompletedSegment)` → extract `SelectZoneTypeForRing(UrbanRing)` to decouple ring-based demand logic from segment struct; reuse from frontier rescan paths.
- Stage-compress to single issue when every task is ≤1 file or docs-only and none kicked off — n→1 collapse cuts step overhead w/ zero sunk cost (first applied to web-platform Stage 1.1, 2026-04-14).
- Root npm script composition: prefer `npm --prefix {dir} run …` over `cd {dir} && …` — reliable exit-code propagation, no subshell state quirks.
- Vercel project link + first deploy are dashboard-only (no CLI auth in agent env) — pre-note as `[HUMAN ACTION]` in Phase plan before implementation, not mid-phase.
- `npx create-next-app@latest` auto-generates `AGENTS.md` + `CLAUDE.md` under target dir — review/overwrite post-scaffold to match repo conventions.
- `BlipTestFixtures.SampleEnvelopeLevels(buf, stride)` aliases oscillator phase when stride is not period-aligned; pick osc freq so `sampleRate/freq == stride` (e.g. 48 kHz / 3000 Hz = 16) so abs samples track envelope monotonically.
- `BlipPatch` SO has no `oscillatorCount` field — count derives from `oscillators.Length` in `BlipPatchFlat.FromSO`. Reflection-based `MakePatch` helpers must set the array, not a phantom int field.
- **Blip patch** SO canonical path: `Assets/Audio/Blip/Patches/BlipPatch_{Name}.asset` (not `Assets/Audio/BlipPatches/`). Filename prefix `BlipPatch_` mandatory for catalog wiring symmetry.
- `BlipFilterKind` MVP enum = `None=0`, `LowPass=1` only — HighPass semantics in §9 recipes (e.g. ToolRoadTick noise transient 4 kHz HP) encoded as `kind: None` + `cutoffHz` placeholder until post-MVP adds `HighPass=2`.
- Next.js `serverExternalPackages` accepts npm package names only — for workspace-relative files outside `node_modules/` (e.g. `tools/progress-tracker/parse.mjs` imported via `../../`), the correct escape hatch is `outputFileTracingIncludes` mapping a route → file glob array. Default path: pure-ESM zero-dep files get inlined by server trace w/o any config.
- Sprite-gen palette extraction: when a class spec drives `apply_variant` material-family swaps (wall_brick_red ↔ wall_brick_grey, roof_tile_brown ↔ roof_tile_grey), bootstrap JSON must ship the grey-family slots up front — name 8 K-means clusters as `{wall_brick_red, wall_brick_grey, roof_tile_brown, roof_tile_grey, window_glass, concrete, trim, mortar}` instead of `{…, shadow, highlight, …}`, else `apply_variant` raises `PaletteKeyError` at render time.
