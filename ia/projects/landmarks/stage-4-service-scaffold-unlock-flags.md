### Stage 4 — LandmarkProgressionService (unlock-only) / Service scaffold + unlock flags

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Scaffold the MonoBehaviour + seed unlock flags from catalog. No tick logic yet — just structure + Inspector wiring.

**Exit:**

- `LandmarkProgressionService` MonoBehaviour exists with serialized refs + `FindObjectOfType` fallback.
- `Awake` populates `unlockedById` with one `false` entry per catalog row.
- `IsUnlocked(string id)` read API returns the flag value.
- Compile clean; no runtime side effects yet.
- Phase 1 — MonoBehaviour scaffold + ref wiring + unlock dictionary seed.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T4.1 | Service MonoBehaviour scaffold | _pending_ | _pending_ | Add `LandmarkProgressionService.cs` MonoBehaviour. `[SerializeField] private ScaleTierController scaleTier`, `[SerializeField] private PopulationAggregator population`, `[SerializeField] private LandmarkCatalogStore catalog`. `Awake` applies `FindObjectOfType` fallback per invariant #4; log error if any remain null. |
| T4.2 | Unlock dictionary seed | _pending_ | _pending_ | In `Awake` (after catalog load + ref fallback), populate `Dictionary<string, bool> unlockedById` — one false entry per `catalog.GetAll()` row. Guard against duplicate ids (catalog validator catches but runtime defensive). |
| T4.3 | IsUnlocked read API | _pending_ | _pending_ | Add `public bool IsUnlocked(string id)` — returns dict flag or false on miss. XML doc clarifies that miss = unknown landmark (log warning once). Used by `BigProjectService.TryCommission` in Stage 3.2. |
