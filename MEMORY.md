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
