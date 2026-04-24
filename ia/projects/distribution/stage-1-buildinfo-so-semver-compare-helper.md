### Stage 1 — Unity build pipeline + versioning manifest / BuildInfo SO + semver compare helper

**Status:** In Progress (TECH-347, TECH-348, TECH-349, TECH-350 filed)

**Objectives:** Land the runtime data model (BuildInfo ScriptableObject) + pure semver compare helper with EditMode coverage. Both are inert dependencies for the editor build script in Stage 1.2 and the notifier in Stage 2.3. No build pipeline wiring yet.

**Exit:**

- `Assets/Scripts/Runtime/Distribution/BuildInfo.cs` compiles; `[CreateAssetMenu]` populates Unity menu; `WriteFields` gated on `#if UNITY_EDITOR`.
- `Assets/Resources/BuildInfo.asset` instance created via editor menu; default values `0.0.0-dev` / `unknown` / `unknown`; `Resources.Load<BuildInfo>("BuildInfo")` returns non-null at runtime.
- `Assets/Scripts/Runtime/Distribution/SemverCompare.cs` static `Compare(string a, string b) → int` handles `MAJOR.MINOR.PATCH` + optional `-PRERELEASE` suffix per Design Expansion IP-8 subset.
- EditMode test `Assets/Tests/EditMode/Distribution/SemverCompareTests.cs` exercises ≥6 truth-table cases (equal, greater major, greater minor, greater patch, prerelease ordering, malformed input fallback).
- Glossary rows added to `ia/specs/glossary.md` — **BuildInfo ScriptableObject**, **Unsigned installer tier**, **Release manifest (`latest.json`)**, **Update notifier** (forward-ref the latter two to Stage 2.2 / 2.3).
- `npm run unity:compile-check` + EditMode tests green.
- Phase 1 — Author BuildInfo ScriptableObject + committed asset instance.
- Phase 2 — Author SemverCompare helper + EditMode test coverage.
- Phase 3 — Register distribution glossary rows in `ia/specs/glossary.md`.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T1.1 | BuildInfo SO type | **TECH-347** | Draft | Author `Assets/Scripts/Runtime/Distribution/BuildInfo.cs` matching Design Expansion IP-3 verbatim — `[CreateAssetMenu(fileName = "BuildInfo", menuName = "Territory/BuildInfo")]`, private serialized `version` / `gitSha` / `buildTimestamp` fields with default `"0.0.0-dev"` / `"unknown"` / `"unknown"`, public getters, editor-gated `WriteFields(string, string, string)` under `#if UNITY_EDITOR`. |
| T1.2 | BuildInfo asset instance | **TECH-348** | Draft | Create `Assets/Resources/BuildInfo.asset` via the Territory/BuildInfo menu command; commit both `.asset` + `.asset.meta`. Verify `Resources.Load<BuildInfo>("BuildInfo")` returns non-null in an EditMode fixture. |
| T1.3 | SemverCompare helper + tests | **TECH-349** | Draft | Author `Assets/Scripts/Runtime/Distribution/SemverCompare.cs` static `Compare(string, string) → int` per IP-8 (subset: MAJOR.MINOR.PATCH + optional `-PRERELEASE`). Author `Assets/Tests/EditMode/Distribution/SemverCompareTests.cs` with truth table (equal, major >, minor >, patch >, prerelease ordering, malformed input → 0 fallback). No external semver library. |
| T1.4 | Glossary rows for distribution terms | **TECH-350** | Draft | Append rows to `ia/specs/glossary.md` for **BuildInfo ScriptableObject** (ref `Assets/Scripts/Runtime/Distribution/BuildInfo.cs`), **Release manifest (`latest.json`)** (forward-ref Stage 2.2), **Update notifier** (forward-ref Stage 2.3), **Unsigned installer tier** (forward-ref Stage 2.1). Follow glossary authoring rules in `ia/rules/terminology-consistency-authoring.md`. |
