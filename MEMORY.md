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
- Smoke-style verification specs need Phase 0 deploy/state precondition check before HTTP probes — else blocked-on-deploy mislabels as smoke failure. Add `git status --porcelain web/` to web-platform kickoff preflight: untracked web files silently break Vercel parity (live `/dashboard` → 404 while local green).
- Next 16 `usePathname()` from `next/navigation` returns non-nullable `string` (signature changed vs Next 13/14 `string | null`). Check `node_modules/next/dist/client/components/navigation.d.ts` before writing null guards on App Router hooks.
- Next.js 16 App Router RSC forbids `ssr: false` inside `next/dynamic()` — build error `'ssr: false' is not allowed with next/dynamic in Server Components`. Wrap the dynamic import in a thin `'use client'` component (e.g. `FooClient.tsx` → `const Foo = dynamic(() => import('./Foo'), { ssr: false, loading })`) and import the client wrapper from the RSC. Canonical reference: `node_modules/next/dist/docs/01-app/02-guides/lazy-loading.md`.
- Node ≥22 `ts-node` / native TS strip mode drops `const enum` support — the inline pass cannot resolve identifiers across file boundaries. Use `as const` object pattern + derived type alias (`type Foo = typeof Foo[keyof typeof Foo]`) for portable enum-like constants in `tools/scripts/*.ts`.
- `__dirname` behaviour flips when ts-node reparses scripts as ESM — verify path resolution against the script file's actual depth before writing `resolve(SCRIPT_DIR, '..', '..', …)`. One `..` level from `tools/scripts/` lands in `tools/`; two levels escapes the repo root.
- `BlipPatch` SO `deterministic` flag gates jitter bypass, NOT rng seed — variant 0 with `deterministic:false` + jitter params all zero still yields a reproducible fixture because the seed derives from `(patchHash ^ variantIndex)` guard (`0x9E3779B9` fallback on zero).
- `Blip.Tests.EditMode.asmdef` sets `includePlatforms: ["Editor"]` — whole asmdef excluded from Standalone builds, so `AssetDatabase.LoadAssetAtPath<T>` calls inside these tests do NOT need `#if UNITY_EDITOR` guards. Applies to any EditMode-only asmdef w/ the same platform gate.
- Blip golden fixture tests re-use `BlipTestFixtures.RenderPatch(in flat, sampleRate, seconds, variantIndex)` — third arg is **seconds**, not sampleCount. Derive `seconds = fixture.sampleCount / fixture.sampleRate` w/ divisibility assert to fail fast when fixture schema drifts.
- `BlipTestFixtures` (Stage 1.4) is the single canonical helper class for Blip EditMode renders / hashes / zero-crossings — do not spawn parallel `BlipTestHelpers`; extend in place. Tolerance floor of 1e-6 on `sumAbsHash` drift matches `BlipDeterminismTests` epsilon.
- Dashboard per-plan completion ratio counts from unfiltered `plan.allTasks` (not `plan.steps[*].stages[*].tasks`) — `filterPlans` prunes nested tasks only, so status / phase chips must not skew plan-level %. `DONE_STATUSES: ReadonlySet<TaskStatus>` covers both `'Done (archived)'` and short-form `'Done'`; single-string compare under-counts unarchived plans.
- Dashboard consumer-side StatBar wiring passes raw `value` + `max` counts — StatBar owns `(v/m)*100` + `[0,100]` clamp + `max ≤ 0 → 0` guard (TECH-232 contract). Do not pre-divide on the caller; reconcile any backlog snippet that shows `value={x / y * 100}` to `value={x} max={y}`.
