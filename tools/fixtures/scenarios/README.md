# Committed scenarios (`GameSaveData`)

Authoritative **save**-shaped JSON for **test mode** and future agent tooling. Loads through **`GameSaveManager.LoadGame`** only (normal **Load pipeline** — see **persistence-system**).

## Layout and **scenario id**

- Root: `tools/fixtures/scenarios/`.
- **Scenario id**: **kebab-case**, ASCII, unique (e.g. `reference-flat-32x32`).
- Resolution order (see `ScenarioPathResolver` in `Assets/Scripts/Testing/`):
  1. `{scenario-id}/save.json`
  2. `{scenario-id}.json` in this folder

## **32×32** test map policy

- Reference scenarios for **TECH-31** stage **31a** use a **32×32** grid (`gridWidth` / `gridHeight` and `gridData` bounds must agree).
- The **MainScene** `GridManager` may still default to a larger Inspector size; **load** overwrites `width` / `height` from the save before `ResetGridForLoad`.
- After changing grid size, verify **camera** framing and **chunk culling** (`chunkSize` vs map) in Play Mode if anything looks off.

## **`GameSaveData` compatibility**

- There is no separate on-disk **schema version** field; compatibility is defined by the **`GameSaveData`** / `CellData` / related types in C# (`JsonUtility`).
- When adding or renaming serialized fields, **regenerate** affected fixtures (see `generate-reference-flat-32x32.mjs`) and re-run **`npm run validate:all`** + **`npm run unity:compile-check`** after Unity changes.

## **Test mode** CLI (dev-only)

Honored only in **Editor**, **development builds**, or when **`TERRITORY_ALLOW_TEST_MODE`** is defined. Release players ignore these flags.

| Argument | Meaning |
|----------|---------|
| `-testScenarioId {id}` | Load `{id}` via `ScenarioPathResolver` under this directory. |
| `-testScenarioPath {path}` | Load that `.json` file directly (`Path.GetFullPath` from the player working directory — prefer an **absolute** path in automation). |

**Editor / development player:** pass the arguments on the process command line (e.g. **Player → Resolution and Presentation → Command-line arguments** in the Editor, or your IDE run configuration). A **UTF** / `-batchmode` **executeMethod** harness is tracked under **TECH-15** / **TECH-16**; until it lands, use Editor Play Mode with CLI args or a **development build** player.

Example args only:

```text
-testScenarioId reference-flat-32x32
```

Or an absolute path:

```text
-testScenarioPath /absolute/path/to/save.json
```

## Agent / Editor queue (no CLI restart)

**Editor only:** write a **single line** with the **scenario id** to:

`tools/fixtures/scenarios/.queued-test-scenario-id`

Then enter **Play Mode** (or enqueue **`unity_bridge_command`** `enter_play_mode`). On the first loaded scene, `TestModeCommandLineBootstrap` resolves the id, **deletes** the file, sets **`GameStartInfo`**, and loads **MainScene** like CLI mode. The file is **gitignored**.

## Driver matrix (local vs **CI**)

| Driver | **Postgres** | Notes |
|--------|----------------|-------|
| Editor Play Mode + CLI args or **queue file** | No | Queue file + **`enter_play_mode`** suits agents without Hub CLI. |
| **UTF** / `-batchmode` | No | Preferred for **CI** once harness lands (**TECH-15** / **TECH-16**). |
| **`verify:local`** / bridge smoke | Yes (when used) | Full dev chain; see **`ARCHITECTURE.md`** — **Local verification**. |

## Regenerating `reference-flat-32x32`

```bash
node tools/fixtures/scenarios/generate-reference-flat-32x32.mjs
```

## Implementing agent verification gate

From the repository root after touching this tree or Unity **C#** / scenes:

1. `npm run validate:all`
2. `npm run unity:compile-check`

See `projects/TECH-31a-test-mode-and-load.md`.
