### Stage 3 — Catalog + data model + glossary/spec seed / LandmarkCatalogStore + glossary + spec stub

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship the runtime loader MonoBehaviour + seed glossary vocabulary + author `ia/specs/landmarks-system.md` stub. Vocabulary lands early so Step 2+ tasks can cite canonical terms in intent prose. Same Stage-1-seeds-vocab pattern as utilities.

**Exit:**

- `LandmarkCatalogStore.cs` MonoBehaviour — `Awake` loads `Application.streamingAssetsPath/landmark-catalog.yaml`, deserializes into `List<LandmarkCatalogRow>`, builds `Dictionary<string, LandmarkCatalogRow> byId` index. API: `GetAll()`, `GetById(string id)`. `FindObjectOfType` fallback pattern documented.
- Glossary rows added to `ia/specs/glossary.md`: **Landmark**, **Big project**, **Tier-defining landmark**, **Intra-tier reward landmark**, **Landmark catalog row**, **Landmark sidecar**, **Commission ledger**, **Super-utility building**. `specReference` = `landmarks-system §{section}` (stub sections exist).
- `ia/specs/landmarks-system.md` — new file, frontmatter per `ia/templates/spec-template.md`. Sections: §1 Overview (fill), §2 Catalog schema (fill), §3 Progression state machine (stub — "filled in Stage 4.2"), §4 Commission pipeline (stub), §5 Placement + reconciliation (stub), §6 Landmarks↔Utilities bridge (stub), §7 BUG-20 interaction (stub), §8 Save schema (stub).
- MCP regen — `npm run validate:all` updates `tools/mcp-ia-server/data/glossary-index.json` + `spec-index.json` including new spec + glossary rows.
- EditMode test — instantiate Store in test scene w/ fixture YAML, assert 6 rows loaded, assert `GetById("big_power_plant").contributorScalingFactor == 10`.
- Phase 1 — `LandmarkCatalogStore` MonoBehaviour + YAML parse.
- Phase 2 — Glossary rows + spec stub + MCP regen.
- Phase 3 — Store EditMode test.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T3.1 | LandmarkCatalogStore scaffold | _pending_ | _pending_ | Add `Assets/Scripts/Managers/GameManagers/LandmarkCatalogStore.cs` — MonoBehaviour, `[SerializeField] private string catalogRelativePath = "landmark-catalog.yaml"`, internal `List<LandmarkCatalogRow>` + `Dictionary<string, LandmarkCatalogRow>`. Invariant #4 pattern — Inspector + `FindObjectOfType` fallback. |
| T3.2 | YAML load + GetById/GetAll | _pending_ | _pending_ | Implement `Awake` YAML load via existing Unity YAML parser (check `UtilityContributorRegistry` serialization stack in sibling Bucket 4-a OR add minimal YamlDotNet dep). Build `byId` index; expose `GetAll()` + `GetById(id)`. |
| T3.3 | Glossary rows seed | _pending_ | _pending_ | Edit `ia/specs/glossary.md` — add 8 rows per Step 1 exit criteria (**Landmark**, **Big project**, **Tier-defining landmark**, **Intra-tier reward landmark**, **Landmark catalog row**, **Landmark sidecar**, **Commission ledger**, **Super-utility building**). `specReference` = `landmarks-system §{stub id}`. |
| T3.4 | landmarks-system.md spec stub | _pending_ | _pending_ | Create `ia/specs/landmarks-system.md` — frontmatter per `ia/templates/spec-template.md`, §1 Overview + §2 Catalog schema populated (copy schema table from LandmarkCatalogRow XML docs), §3–§8 stub headings with "filled in Stage 4.2" placeholder + exploration-doc link. |
| T3.5 | MCP index regen | _pending_ | _pending_ | Run `npm run validate:all` → regenerates glossary + spec indexes. Commit regenerated JSON artifacts alongside spec + glossary edits. |
| T3.6 | CatalogStore EditMode test | _pending_ | _pending_ | Add `Assets/Tests/EditMode/Landmarks/LandmarkCatalogStoreTests.cs` — fixture test scene w/ Store component + fixture YAML under `Assets/Tests/Fixtures/landmark-catalog.yaml`. Assert 6 rows, assert tier-defining rows have `commissionCost == 0`, assert `big_power_plant.contributorScalingFactor == 10`. |
| T3.7 | GetById miss + duplicate id test | _pending_ | _pending_ | Add tests — `GetById("unknown")` returns null + logs warning once; fixture YAML w/ duplicate id fails validator (T1.2.3) before Store load (sanity cross-check the validator catches what Store would otherwise silently dedupe). |
