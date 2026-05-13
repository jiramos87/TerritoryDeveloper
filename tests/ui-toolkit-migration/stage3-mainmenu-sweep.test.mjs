// Stage 3 — MainMenu sweep — full-screen menu panels — incremental TDD red→green test file.
//
// Protocol (ia/rules/agent-principles.md §Testing + verification):
//   ONE test file per Stage. First task creates file in failing/red state.
//   Each subsequent task extends with new assertions tied to its phase.
//   File stays red until last task of the Stage. Stage close = file green.
//
// Stage anchor: visibility-delta-test:tests/ui-toolkit-migration/stage3-mainmenu-sweep.test.mjs::mainMenuSceneFullyOnUiToolkit
//
// Tasks (anchored by §Red-Stage Proof per task spec):
//   3.0.1  Migrate main-menu (uxml + vm + host + adapter delete)
//   3.0.2  Migrate new-game-form (uxml + vm + host + adapter delete)
//   3.0.3  Migrate save-load-view (uxml + vm + host + adapter delete)
//   3.0.4  Migrate settings-view (uxml + vm + host + adapter delete)
//   3.0.5  MainMenu scene cleanup — remove uGUI Canvas component

import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { readFileSync, existsSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = join(__dirname, "../..");

// ── TECH-32911: main-menu UXML + USS + MainMenuVM + MainMenuHost ─────────────

describe("mainMenuMigration", () => {
  it("mainMenuUxmlExists [task 3.0.1] — Assets/UI/Generated/main-menu.uxml emitted", () => {
    const uxmlPath = join(repoRoot, "Assets/UI/Generated/main-menu.uxml");
    assert.ok(existsSync(uxmlPath), `main-menu.uxml must exist: ${uxmlPath}`);
    const src = readFileSync(uxmlPath, "utf8");
    assert.ok(src.includes("xmlns") || src.includes("<ui:"), "must be valid UXML");
    assert.ok(src.includes("main-menu"), "must reference main-menu");
  });

  it("mainMenuUxmlHasNavBindings [task 3.0.1] — UXML has binding-path for nav commands", () => {
    const uxmlPath = join(repoRoot, "Assets/UI/Generated/main-menu.uxml");
    assert.ok(existsSync(uxmlPath), "main-menu.uxml must exist");
    const src = readFileSync(uxmlPath, "utf8");
    assert.ok(src.includes('binding-path="NewGameCommand"'), "must have NewGameCommand binding-path");
    assert.ok(src.includes('binding-path="LoadCommand"'), "must have LoadCommand binding-path");
    assert.ok(src.includes('binding-path="QuitCommand"'), "must have QuitCommand binding-path");
  });

  it("mainMenuUssExists [task 3.0.1] — Assets/UI/Generated/main-menu.uss emitted", () => {
    const ussPath = join(repoRoot, "Assets/UI/Generated/main-menu.uss");
    assert.ok(existsSync(ussPath), `main-menu.uss must exist: ${ussPath}`);
    const src = readFileSync(ussPath, "utf8");
    assert.ok(src.includes("--ds-") || src.includes("@import"), "must reference --ds-* tokens or import dark.tss");
  });

  it("mainMenuVMExists [task 3.0.1] — MainMenuVM.cs POCO ViewModel present", () => {
    const vmPath = join(repoRoot, "Assets/Scripts/UI/ViewModels/MainMenuVM.cs");
    assert.ok(existsSync(vmPath), `MainMenuVM.cs must exist: ${vmPath}`);
    const src = readFileSync(vmPath, "utf8");
    assert.ok(src.includes("MainMenuVM"), "MainMenuVM class must be present");
    assert.ok(
      src.includes("INotifyPropertyChanged") || src.includes("PropertyChanged"),
      "MainMenuVM must implement INotifyPropertyChanged",
    );
    assert.ok(src.includes("NewGameCommand"), "VM must expose NewGameCommand");
    assert.ok(src.includes("LoadCommand"), "VM must expose LoadCommand");
    assert.ok(src.includes("QuitCommand"), "VM must expose QuitCommand");
  });

  it("mainMenuHostExists [task 3.0.1] — MainMenuHost.cs MonoBehaviour Host present", () => {
    const hostPath = join(repoRoot, "Assets/Scripts/UI/Hosts/MainMenuHost.cs");
    assert.ok(existsSync(hostPath), `MainMenuHost.cs must exist: ${hostPath}`);
    const src = readFileSync(hostPath, "utf8");
    assert.ok(src.includes("MainMenuHost"), "MainMenuHost class must be present");
    assert.ok(src.includes("UIDocument"), "MainMenuHost must reference UIDocument");
    assert.ok(src.includes("dataSource"), "MainMenuHost must set rootVisualElement.dataSource");
    assert.ok(src.includes("MainMenuVM"), "MainMenuHost must instantiate MainMenuVM");
  });
});

// ── TECH-32912: new-game-form UXML + USS + NewGameFormVM + NewGameFormHost ────

describe("newGameFormMigration", () => {
  it("newGameFormUxmlExists [task 3.0.2] — Assets/UI/Generated/new-game-form.uxml emitted", () => {
    const uxmlPath = join(repoRoot, "Assets/UI/Generated/new-game-form.uxml");
    assert.ok(existsSync(uxmlPath), `new-game-form.uxml must exist: ${uxmlPath}`);
    const src = readFileSync(uxmlPath, "utf8");
    assert.ok(src.includes("xmlns") || src.includes("<ui:"), "must be valid UXML");
    assert.ok(src.includes("new-game-form"), "must reference new-game-form");
  });

  it("newGameFormUxmlHasFormBindings [task 3.0.2] — UXML has binding-path for form fields", () => {
    const uxmlPath = join(repoRoot, "Assets/UI/Generated/new-game-form.uxml");
    assert.ok(existsSync(uxmlPath), "new-game-form.uxml must exist");
    const src = readFileSync(uxmlPath, "utf8");
    assert.ok(src.includes('binding-path="CityName"'), "must have CityName binding-path");
    assert.ok(src.includes('binding-path="SubmitCommand"'), "must have SubmitCommand binding-path");
    assert.ok(src.includes('binding-path="CancelCommand"'), "must have CancelCommand binding-path");
  });

  it("newGameFormUssExists [task 3.0.2] — Assets/UI/Generated/new-game-form.uss emitted", () => {
    const ussPath = join(repoRoot, "Assets/UI/Generated/new-game-form.uss");
    assert.ok(existsSync(ussPath), `new-game-form.uss must exist: ${ussPath}`);
    const src = readFileSync(ussPath, "utf8");
    assert.ok(src.includes("--ds-") || src.includes("@import"), "must reference --ds-* tokens or import dark.tss");
  });

  it("newGameFormVMExists [task 3.0.2] — NewGameFormVM.cs POCO ViewModel present", () => {
    const vmPath = join(repoRoot, "Assets/Scripts/UI/ViewModels/NewGameFormVM.cs");
    assert.ok(existsSync(vmPath), `NewGameFormVM.cs must exist: ${vmPath}`);
    const src = readFileSync(vmPath, "utf8");
    assert.ok(src.includes("NewGameFormVM"), "NewGameFormVM class must be present");
    assert.ok(
      src.includes("INotifyPropertyChanged") || src.includes("PropertyChanged"),
      "NewGameFormVM must implement INotifyPropertyChanged",
    );
    assert.ok(src.includes("CityName"), "VM must expose CityName property");
    assert.ok(src.includes("SubmitCommand"), "VM must expose SubmitCommand");
    assert.ok(src.includes("CancelCommand"), "VM must expose CancelCommand");
  });

  it("newGameFormHostExists [task 3.0.2] — NewGameFormHost.cs MonoBehaviour Host present", () => {
    const hostPath = join(repoRoot, "Assets/Scripts/UI/Hosts/NewGameFormHost.cs");
    assert.ok(existsSync(hostPath), `NewGameFormHost.cs must exist: ${hostPath}`);
    const src = readFileSync(hostPath, "utf8");
    assert.ok(src.includes("NewGameFormHost"), "NewGameFormHost class must be present");
    assert.ok(src.includes("UIDocument"), "NewGameFormHost must reference UIDocument");
    assert.ok(src.includes("dataSource"), "NewGameFormHost must set rootVisualElement.dataSource");
    assert.ok(src.includes("NewGameFormVM"), "NewGameFormHost must instantiate NewGameFormVM");
  });
});

// ── TECH-32913: save-load-view UXML + USS + SaveLoadViewVM + SaveLoadViewHost ──

describe("saveLoadViewMigration", () => {
  it("saveLoadViewUxmlExists [task 3.0.3] — Assets/UI/Generated/save-load-view.uxml emitted", () => {
    const uxmlPath = join(repoRoot, "Assets/UI/Generated/save-load-view.uxml");
    assert.ok(existsSync(uxmlPath), `save-load-view.uxml must exist: ${uxmlPath}`);
    const src = readFileSync(uxmlPath, "utf8");
    assert.ok(src.includes("xmlns") || src.includes("<ui:"), "must be valid UXML");
    assert.ok(src.includes("save-load-view"), "must reference save-load-view");
  });

  it("saveLoadViewUxmlHasListBinding [task 3.0.3] — UXML has binding-path for Slots collection", () => {
    const uxmlPath = join(repoRoot, "Assets/UI/Generated/save-load-view.uxml");
    assert.ok(existsSync(uxmlPath), "save-load-view.uxml must exist");
    const src = readFileSync(uxmlPath, "utf8");
    assert.ok(
      src.includes('binding-path="Slots"') || src.includes("ListView"),
      "must have Slots binding or ListView element",
    );
    assert.ok(src.includes('binding-path="LoadCommand"'), "must have LoadCommand binding-path");
  });

  it("saveLoadViewUssExists [task 3.0.3] — Assets/UI/Generated/save-load-view.uss emitted", () => {
    const ussPath = join(repoRoot, "Assets/UI/Generated/save-load-view.uss");
    assert.ok(existsSync(ussPath), `save-load-view.uss must exist: ${ussPath}`);
    const src = readFileSync(ussPath, "utf8");
    assert.ok(src.includes("--ds-") || src.includes("@import"), "must reference --ds-* tokens or import dark.tss");
  });

  it("saveLoadViewVMExists [task 3.0.3] — SaveLoadViewVM.cs POCO ViewModel present", () => {
    const vmPath = join(repoRoot, "Assets/Scripts/UI/ViewModels/SaveLoadViewVM.cs");
    assert.ok(existsSync(vmPath), `SaveLoadViewVM.cs must exist: ${vmPath}`);
    const src = readFileSync(vmPath, "utf8");
    assert.ok(src.includes("SaveLoadViewVM"), "SaveLoadViewVM class must be present");
    assert.ok(
      src.includes("INotifyPropertyChanged") || src.includes("PropertyChanged"),
      "SaveLoadViewVM must implement INotifyPropertyChanged",
    );
    assert.ok(src.includes("LoadCommand"), "VM must expose LoadCommand");
    assert.ok(src.includes("SaveCommand"), "VM must expose SaveCommand");
    assert.ok(src.includes("DeleteCommand"), "VM must expose DeleteCommand");
  });

  it("saveLoadViewHostExists [task 3.0.3] — SaveLoadViewHost.cs MonoBehaviour Host present", () => {
    const hostPath = join(repoRoot, "Assets/Scripts/UI/Hosts/SaveLoadViewHost.cs");
    assert.ok(existsSync(hostPath), `SaveLoadViewHost.cs must exist: ${hostPath}`);
    const src = readFileSync(hostPath, "utf8");
    assert.ok(src.includes("SaveLoadViewHost"), "SaveLoadViewHost class must be present");
    assert.ok(src.includes("UIDocument"), "SaveLoadViewHost must reference UIDocument");
    assert.ok(src.includes("dataSource"), "SaveLoadViewHost must set rootVisualElement.dataSource");
    assert.ok(src.includes("SaveLoadViewVM"), "SaveLoadViewHost must instantiate SaveLoadViewVM");
  });
});

// ── TECH-32914: settings-view UXML + USS + SettingsViewVM + SettingsViewHost ──

describe("settingsViewMigration", () => {
  it("settingsViewUxmlExists [task 3.0.4] — Assets/UI/Generated/settings-view.uxml emitted", () => {
    const uxmlPath = join(repoRoot, "Assets/UI/Generated/settings-view.uxml");
    assert.ok(existsSync(uxmlPath), `settings-view.uxml must exist: ${uxmlPath}`);
    const src = readFileSync(uxmlPath, "utf8");
    assert.ok(src.includes("xmlns") || src.includes("<ui:"), "must be valid UXML");
    assert.ok(src.includes("settings-view"), "must reference settings-view");
  });

  it("settingsViewUxmlHasSettingBindings [task 3.0.4] — UXML has binding-path for settings", () => {
    const uxmlPath = join(repoRoot, "Assets/UI/Generated/settings-view.uxml");
    assert.ok(existsSync(uxmlPath), "settings-view.uxml must exist");
    const src = readFileSync(uxmlPath, "utf8");
    assert.ok(src.includes('binding-path="MasterVolume"'), "must have MasterVolume binding-path");
    assert.ok(src.includes('binding-path="ApplyCommand"'), "must have ApplyCommand binding-path");
  });

  it("settingsViewUssExists [task 3.0.4] — Assets/UI/Generated/settings-view.uss emitted", () => {
    const ussPath = join(repoRoot, "Assets/UI/Generated/settings-view.uss");
    assert.ok(existsSync(ussPath), `settings-view.uss must exist: ${ussPath}`);
    const src = readFileSync(ussPath, "utf8");
    assert.ok(src.includes("--ds-") || src.includes("@import"), "must reference --ds-* tokens or import dark.tss");
  });

  it("settingsViewVMExists [task 3.0.4] — SettingsViewVM.cs POCO ViewModel present", () => {
    const vmPath = join(repoRoot, "Assets/Scripts/UI/ViewModels/SettingsViewVM.cs");
    assert.ok(existsSync(vmPath), `SettingsViewVM.cs must exist: ${vmPath}`);
    const src = readFileSync(vmPath, "utf8");
    assert.ok(src.includes("SettingsViewVM"), "SettingsViewVM class must be present");
    assert.ok(
      src.includes("INotifyPropertyChanged") || src.includes("PropertyChanged"),
      "SettingsViewVM must implement INotifyPropertyChanged",
    );
    assert.ok(src.includes("MasterVolume"), "VM must expose MasterVolume property");
    assert.ok(src.includes("ApplyCommand"), "VM must expose ApplyCommand");
    assert.ok(src.includes("ResetCommand"), "VM must expose ResetCommand");
  });

  it("settingsViewHostExists [task 3.0.4] — SettingsViewHost.cs MonoBehaviour Host present", () => {
    const hostPath = join(repoRoot, "Assets/Scripts/UI/Hosts/SettingsViewHost.cs");
    assert.ok(existsSync(hostPath), `SettingsViewHost.cs must exist: ${hostPath}`);
    const src = readFileSync(hostPath, "utf8");
    assert.ok(src.includes("SettingsViewHost"), "SettingsViewHost class must be present");
    assert.ok(src.includes("UIDocument"), "SettingsViewHost must reference UIDocument");
    assert.ok(src.includes("dataSource"), "SettingsViewHost must set rootVisualElement.dataSource");
    assert.ok(src.includes("SettingsViewVM"), "SettingsViewHost must instantiate SettingsViewVM");
  });
});

// ── TECH-32915: MainMenu scene cleanup — Canvas removal ──────────────────────

describe("mainMenuSceneFullyOnUiToolkit", () => {
  it("mainMenuSceneExists [task 3.0.5] — Assets/Scenes/MainMenu.unity present", () => {
    const scenePath = join(repoRoot, "Assets/Scenes/MainMenu.unity");
    assert.ok(existsSync(scenePath), `MainMenu.unity must exist: ${scenePath}`);
  });

  it("mainMenuSceneHasUiDocumentRef [task 3.0.5] — MainMenu.unity references UIDocument component", () => {
    const scenePath = join(repoRoot, "Assets/Scenes/MainMenu.unity");
    assert.ok(existsSync(scenePath), "MainMenu.unity must exist");
    const src = readFileSync(scenePath, "utf8");
    // UIDocument is serialized as UnityEngine.UIElements.UIDocument in the YAML scene file
    assert.ok(
      src.includes("UIDocument") || src.includes("UIElements"),
      "MainMenu.unity must reference UIDocument or UIElements",
    );
  });

  it("checksumManifestHasMainMenuEntries [task 3.0.5] — .checksum-manifest.json has main-menu panel entries", () => {
    const manifestPath = join(repoRoot, "tools/visual-baseline/golden/.checksum-manifest.json");
    assert.ok(existsSync(manifestPath), "checksum manifest must exist");
    const manifest = JSON.parse(readFileSync(manifestPath, "utf8"));
    assert.ok(manifest.version === 1, "manifest version must be 1");
    assert.ok(
      "main-menu.png" in manifest.entries,
      "manifest must have main-menu.png entry",
    );
  });

  it("perfStage3SnapshotExists [task 3.0.5] — tools/perf-baseline/mainmenu-stage3.json present", () => {
    const perfPath = join(repoRoot, "tools/perf-baseline/mainmenu-stage3.json");
    assert.ok(existsSync(perfPath), `mainmenu-stage3.json must exist: ${perfPath}`);
    const snap = JSON.parse(readFileSync(perfPath, "utf8"));
    assert.ok("stage" in snap, "snapshot must have stage field");
    assert.ok(snap.stage === "stage-3-0", "snapshot stage must be stage-3-0");
  });
});
