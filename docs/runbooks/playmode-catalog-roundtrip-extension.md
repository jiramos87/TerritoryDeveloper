---
purpose: Per-kind PlayMode roundtrip extension procedure once a runtime catalog ships.
audience: developer
last_walkthrough: 2026-05-01
---

# PlayMode catalog-roundtrip extension

Step-by-step procedure to extend PlayMode roundtrip coverage to a new catalog kind once its runtime catalog (sprite / button / panel / audio / pool / asset / archetype) lands. Reference cluster: `Assets/Tests/PlayMode/TokenCatalog/TokenCatalogRoundtripTests.cs` (Stage 10.1 / TECH-2095). Drop-in copy-and-adapt — do NOT invent scaffolding.

## Pre-conditions

- Runtime catalog class exists under `Assets/Scripts/UI/{Kind}Catalog.cs` (e.g. `SpriteCatalog.cs`).
- Runtime catalog exposes the proven binder API: `TryParseSnapshotJson(string, out RootDto, out string)`, `RebuildIndexes(RootDto)`, and one or more `TryGet*` accessors per indexed kind.
- Snapshot DTO + per-row sub-DTOs are `[Serializable]` + `JsonUtility`-friendly (flat fields, no nested unions).
- A reference cluster fixture already lives under `Assets/Tests/PlayMode/{Kind}Catalog/Fixtures/` OR you author one in Step 4.
- Upstream stages 8.1 (sprite / button / panel runtime) / 10.1 (token runtime, shipped) are the gating dependencies for non-token kinds.

## Asmdef pattern

Mirror `Assets/Tests/PlayMode/TokenCatalog/TokenCatalog.Tests.PlayMode.asmdef`:

```json
{
    "name": "{Kind}Catalog.Tests.PlayMode",
    "rootNamespace": "Territory.Tests.PlayMode.{Kind}Catalog",
    "references": ["TerritoryDeveloper.Game"],
    "includePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "optionalUnityReferences": ["TestAssemblies"],
    "noEngineReferences": false
}
```

## Test skeleton template

Copy the rhythm from `TokenCatalogRoundtripTests.cs`:

```csharp
using System.Collections;
using System.IO;
using NUnit.Framework;
using Territory.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Territory.Tests.PlayMode.{Kind}Catalog
{
    public sealed class {Kind}CatalogRoundtripTests
    {
        private const string FixtureRelativePath =
            "Assets/Tests/PlayMode/{Kind}Catalog/Fixtures/{kind}-catalog-fixture.json";

        private GameObject _host;
        private Territory.UI.{Kind}Catalog _catalog;
        private {Kind}CatalogSnapshotDto _snapshot;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            string path = Path.Combine(Application.dataPath, "..", FixtureRelativePath);
            string text = File.ReadAllText(path);
            Assert.IsTrue(
                Territory.UI.{Kind}Catalog.TryParseSnapshotJson(text, out _snapshot, out var err),
                $"Fixture parse failed: {err}");
            _host = new GameObject("{Kind}CatalogTestHost");
            _catalog = _host.AddComponent<Territory.UI.{Kind}Catalog>();
            _catalog.RebuildIndexes(_snapshot);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_host != null) Object.Destroy(_host);
            yield return null;
        }

        [Test]
        public void RebuildIndexes_AllRowsResolvable() { /* per-kind asserts */ }
    }
}
```

## Steps

1. **Verify runtime catalog API surface.** Confirm `Assets/Scripts/UI/{Kind}Catalog.cs` exposes `TryParseSnapshotJson` + `RebuildIndexes` + at least one `TryGet*` accessor. If any are missing → STOP and escalate `runtime_api_gap` (do NOT author the API in this task).

2. **Create cluster directory.**
   ```bash
   cd $REPO_ROOT
   mkdir -p Assets/Tests/PlayMode/{Kind}Catalog/Fixtures
   ```

3. **Author asmdef.** Drop `Assets/Tests/PlayMode/{Kind}Catalog/{Kind}Catalog.Tests.PlayMode.asmdef` using the [Asmdef pattern](#asmdef-pattern) above. Substitute `{Kind}` with the actual kind name (e.g. `Sprite`).

4. **Author minimal fixture.** Write `Assets/Tests/PlayMode/{Kind}Catalog/Fixtures/{kind}-catalog-fixture.json` with the smallest viable row set — one row per indexed kind PLUS edge cases the runtime contract pins (e.g. terminating alias chain, retired-row absence). Match the runtime DTO shape verbatim — `JsonUtility` rejects mismatched fields silently.

5. **Copy + adapt the test skeleton.** Drop `Assets/Tests/PlayMode/{Kind}Catalog/{Kind}CatalogRoundtripTests.cs`. Copy `TokenCatalogRoundtripTests.cs` verbatim, then:
   - Swap namespace to `Territory.Tests.PlayMode.{Kind}Catalog`.
   - Swap `TokenCatalog` → `{Kind}Catalog`, `TokenCatalogSnapshotDto` → `{Kind}CatalogSnapshotDto`.
   - Swap fixture path constant.
   - Adapt `[Test]` bodies to the per-kind `TryGet*` surface.

6. **Compile gate.**
   ```bash
   cd $REPO_ROOT
   npm run unity:compile-check
   ```
   Expected: exit 0. New asmdef must compile clean inside the existing graph.

7. **PlayMode gate.**
   ```bash
   cd $REPO_ROOT
   npm run unity:test-playmode
   ```
   Expected: exit 0. NUnit report lists the new `Territory.Tests.PlayMode.{Kind}Catalog.{Kind}CatalogRoundtripTests` FQN with all new test names green AND existing `TokenCatalogRoundtripTests` (+ `TokenCatalogEdgeCaseTests`) zero regression.

8. **Update index + bump frontmatter.** Edit `docs/runbooks/README.md`:
   - Add a new index row for the kind (`Per-kind PlayMode roundtrip extension once {Kind}Catalog runtime ships.`).
   - Bump `last_walkthrough:` frontmatter to today UTC.

## Failure-recovery branches

- **Step 1: API surface missing** → escalate `runtime_api_gap`. Cite the missing method. Do NOT extend the runtime catalog from a test-authoring task.
- **Step 4: `JsonUtility` parse silently drops fields** → DTO sub-row mismatch. Inspect runtime catalog's per-kind sub-DTO; mirror exact field names + types. `JsonUtility` is case-sensitive + only honors public fields.
- **Step 6: compile error in new asmdef** → check `references` array; runtime catalogs typically live in `TerritoryDeveloper.Game` asmdef. Add it if missing.
- **Step 7: existing TokenCatalog regression** → escalate `existing_token_cluster_regression`. Do NOT proceed; sibling cluster cross-talk via shared fixture parser path is a real risk and must be diagnosed before shipping the new cluster.
- **Step 7: new tests fail with `Fixture parse failed`** → escalate `fixture_parse_drift`. Check the snapshot `schemaVersion` is `>= 1` and the JSON root is a single object. Re-read runtime `TryParseSnapshotJson` validation rules.
- **Step 8: walkthrough run-time blocker** → escalate `runbook_walkthrough_blocked`. Capture the failing step + verbatim output; file a `BUG-` issue citing this runbook step number.

## Stage dependency citation

Sprite / button / panel runtime catalogs land in upstream **Stage 8.1** (sprite + button) and **Stage 10.1** (panel host wiring). Audio / pool / asset / archetype runtime catalogs are tracked in their respective master-plan stages. This runbook is dormant for a kind until that kind's runtime catalog class exists under `Assets/Scripts/UI/`.

### Drift notes

(Record any command that needed adjustment during the most recent walkthrough here. 2026-05-01 — initial authoring; reference walkthrough run against `TokenCatalogRoundtripTests.cs` skeleton + `TokenCatalogEdgeCaseTests.cs` sibling cluster as the canonical example.)
