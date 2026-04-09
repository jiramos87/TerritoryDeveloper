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

**Editor / development player:** pass the arguments on the process command line (e.g. **Player → Resolution and Presentation → Command-line arguments** in the Editor, or your IDE run configuration).

### Batch (`-batchmode`) — **Agent test mode batch**

From the **repository root** (loads **`.env`** / **`.env.local`** for **`UNITY_EDITOR_PATH`**; **macOS** can infer Hub Unity from **`ProjectSettings/ProjectVersion.txt`** — same as **`npm run unity:compile-check`**):

```bash
npm run unity:testmode-batch
```

This runs **`tools/scripts/unity-testmode-batch.sh`**, which launches Unity with **`-batchmode`**, **`-nographics`**, **`-executeMethod Territory.Testing.AgentTestModeBatchRunner.Run`**, and forwards scenario flags. The script does **not** pass **`-quit`**: the C# runner must finish **Play Mode** work and then calls **`EditorApplication.Exit`** (adding **`-quit`** would exit before the update pump runs). Default scenario id is **`reference-flat-32x32`** when you omit **`--scenario-id`** / **`--scenario-path`**.

| Argument (shell) | Forwarded to Unity |
|------------------|-------------------|
| `--scenario-id ID` | `-testScenarioId ID` |
| `--scenario-path PATH` | `-testScenarioPath PATH` |
| `--simulation-ticks N` | `-testSimulationTicks N` (default **0** in script; capped in C#) |
| `--quit-editor-first` | Runs **`tools/scripts/unity-quit-project.sh`** first (**`Temp/UnityLockfile`** + **`lsof`**, **SIGTERM** then **SIGKILL**) |
| `--` … | Extra Unity CLI tokens |

Machine-readable result: **`tools/reports/agent-testmode-batch-*.json`** (and a Unity log under **`tools/reports/unity-testmode-batch-*.log`**). While a run is in progress, **`tools/reports/.agent-testmode-batch-state.json`** may exist (transient; same ignore rules as other report artifacts). **`unity-quit-project.sh --help`** documents lock-based quit and why **`pkill`** / global **AppleScript** quit is not the default.

**Exit codes** (runner / shell): see **`unity-testmode-batch.sh --help`** (**0** success; **2** missing Unity binary; **3** quit helper failed; **4** / **6** / **7** from **`EditorApplication.Exit`** in **`AgentTestModeBatchRunner`**).

Example args only (Editor Play Mode or batch):

```text
-testScenarioId reference-flat-32x32
```

Or an absolute path:

```text
-testScenarioPath /absolute/path/to/save.json
```

Optional simulation steps after **`GameSaveManager.LoadGame`** (same entry point as **TimeManager** — **`SimulationManager.ProcessSimulationTick`**):

```text
-testSimulationTicks 3
```

## Agent / Editor queue (no CLI restart)

**Editor only:** write a **single line** with the **scenario id** to:

`tools/fixtures/scenarios/.queued-test-scenario-id`

Then enter **Play Mode** (or enqueue **`unity_bridge_command`** `enter_play_mode`). On the first loaded scene, `TestModeCommandLineBootstrap` resolves the id, **deletes** the file, sets **`GameStartInfo`**, and loads **MainScene** like CLI mode. The file is **gitignored**.

## Driver matrix (local vs **CI**)

| Driver | **Postgres** | Notes |
|--------|----------------|-------|
| Editor Play Mode + CLI args or **queue file** | No | Queue file + **`enter_play_mode`** suits agents without Hub CLI. |
| **`npm run unity:testmode-batch`** (`-batchmode` + **`AgentTestModeBatchRunner.Run`**) | No | Load smoke + optional **`ProcessSimulationTick`** loop; **`tools/reports/`** JSON (**glossary** **Agent test mode batch**). **UTF** / broader **CI** harness still tracked under **TECH-15** / **TECH-16**. |
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
