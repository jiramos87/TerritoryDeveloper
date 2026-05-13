// Stage 4 — CityScene modal sweep — pop-up modal panels — incremental TDD red→green test file.
//
// Protocol (ia/rules/agent-principles.md §Testing + verification):
//   ONE test file per Stage. First task creates file in failing/red state.
//   Each subsequent task extends with new assertions tied to its phase.
//   File stays red until last task of the Stage. Stage close = file green.
//
// Stage anchor: visibility-delta-test:tests/ui-toolkit-migration/stage4-modal-sweep.test.mjs::modalsRouteViaUiDocument
//
// Tasks (anchored by §Red-Stage Proof per task spec):
//   4.0.1  Extend ModalCoordinator with Show(VisualElement) co-existence overload
//   4.0.2  Migrate budget-panel (uxml + vm + host + adapter delete)
//   4.0.3  Migrate info-panel (uxml + vm + host + adapter delete)
//   4.0.4  Migrate stats-panel (tabs + chart + stacked bar)
//   4.0.5  Migrate map-panel (uxml + vm + host + adapter delete)
//   4.0.6  Migrate tool-subtype-picker (uxml + vm + host + adapter delete)

import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { readFileSync, existsSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = join(__dirname, "../..");

// ── TECH-32916: ModalCoordinator Show(VisualElement) co-existence overload ────

describe("modalCoordinatorCoExistence", () => {
  it("modalCoordinatorHasShowOverload [task 4.0.1] — ModalCoordinator.cs has Show(string) overload for migrated panels", () => {
    const path = join(repoRoot, "Assets/Scripts/UI/Modals/ModalCoordinator.cs");
    assert.ok(existsSync(path), `ModalCoordinator.cs must exist: ${path}`);
    const src = readFileSync(path, "utf8");
    assert.ok(src.includes("Show(string"), "must have Show(string) overload routing migrated panels");
    assert.ok(src.includes("RegisterMigratedPanel"), "must have RegisterMigratedPanel method");
    assert.ok(src.includes("_migratedPanels"), "must have _migratedPanels dict for UIDocument route");
    assert.ok(src.includes("HideMigrated"), "must have HideMigrated counterpart to Show");
  });

  it("modalCoordinatorImportsUIElements [task 4.0.1] — ModalCoordinator.cs imports UnityEngine.UIElements", () => {
    const path = join(repoRoot, "Assets/Scripts/UI/Modals/ModalCoordinator.cs");
    assert.ok(existsSync(path), "ModalCoordinator.cs must exist");
    const src = readFileSync(path, "utf8");
    assert.ok(src.includes("UnityEngine.UIElements"), "must import UnityEngine.UIElements for VisualElement");
  });

  it("modalCoordinatorHasVisualElementRoute [task 4.0.1] — migrated branch routes VisualElement (display:flex / display:none)", () => {
    const path = join(repoRoot, "Assets/Scripts/UI/Modals/ModalCoordinator.cs");
    assert.ok(existsSync(path), "ModalCoordinator.cs must exist");
    const src = readFileSync(path, "utf8");
    assert.ok(src.includes("DisplayStyle.Flex") || src.includes("display = DisplayStyle"), "must set DisplayStyle.Flex for migrated show");
    assert.ok(src.includes("DisplayStyle.None"), "must set DisplayStyle.None for migrated hide");
  });
});

// ── TECH-32917: budget-panel UXML + USS + BudgetPanelVM + BudgetPanelHost ────

describe("budgetPanelMigration", () => {
  it("budgetPanelUxmlExists [task 4.0.2] — Assets/UI/Generated/budget-panel.uxml emitted", () => {
    const uxmlPath = join(repoRoot, "Assets/UI/Generated/budget-panel.uxml");
    assert.ok(existsSync(uxmlPath), `budget-panel.uxml must exist: ${uxmlPath}`);
    const src = readFileSync(uxmlPath, "utf8");
    assert.ok(src.includes("xmlns") || src.includes("<ui:"), "must be valid UXML");
    assert.ok(src.includes("budget-panel"), "must reference budget-panel");
  });

  it("budgetPanelUxmlHasTaxBindings [task 4.0.2] — UXML has binding-path for tax sliders", () => {
    const uxmlPath = join(repoRoot, "Assets/UI/Generated/budget-panel.uxml");
    assert.ok(existsSync(uxmlPath), "budget-panel.uxml must exist");
    const src = readFileSync(uxmlPath, "utf8");
    assert.ok(src.includes('binding-path="TaxResidential"'), "must have TaxResidential binding-path");
    assert.ok(src.includes('binding-path="ApplyCommand"'), "must have ApplyCommand binding-path");
    assert.ok(src.includes('binding-path="CancelCommand"'), "must have CancelCommand binding-path");
  });

  it("budgetPanelUssExists [task 4.0.2] — Assets/UI/Generated/budget-panel.uss emitted", () => {
    const ussPath = join(repoRoot, "Assets/UI/Generated/budget-panel.uss");
    assert.ok(existsSync(ussPath), `budget-panel.uss must exist: ${ussPath}`);
    const src = readFileSync(ussPath, "utf8");
    assert.ok(src.includes("--ds-") || src.includes("@import"), "must reference --ds-* tokens or import dark.tss");
  });

  it("budgetPanelVMExists [task 4.0.2] — BudgetPanelVM.cs POCO ViewModel present", () => {
    const vmPath = join(repoRoot, "Assets/Scripts/UI/ViewModels/BudgetPanelVM.cs");
    assert.ok(existsSync(vmPath), `BudgetPanelVM.cs must exist: ${vmPath}`);
    const src = readFileSync(vmPath, "utf8");
    assert.ok(src.includes("BudgetPanelVM"), "BudgetPanelVM class must be present");
    assert.ok(
      src.includes("INotifyPropertyChanged") || src.includes("PropertyChanged"),
      "BudgetPanelVM must implement INotifyPropertyChanged",
    );
    assert.ok(src.includes("TaxResidential"), "VM must expose TaxResidential property");
    assert.ok(src.includes("ApplyCommand"), "VM must expose ApplyCommand");
    assert.ok(src.includes("CancelCommand"), "VM must expose CancelCommand");
  });

  it("budgetPanelHostExists [task 4.0.2] — BudgetPanelHost.cs MonoBehaviour Host present", () => {
    const hostPath = join(repoRoot, "Assets/Scripts/UI/Hosts/BudgetPanelHost.cs");
    assert.ok(existsSync(hostPath), `BudgetPanelHost.cs must exist: ${hostPath}`);
    const src = readFileSync(hostPath, "utf8");
    assert.ok(src.includes("BudgetPanelHost"), "BudgetPanelHost class must be present");
    assert.ok(src.includes("UIDocument"), "BudgetPanelHost must reference UIDocument");
    assert.ok(src.includes("dataSource"), "BudgetPanelHost must set rootVisualElement.dataSource");
    assert.ok(src.includes("BudgetPanelVM"), "BudgetPanelHost must instantiate BudgetPanelVM");
    assert.ok(src.includes("RegisterMigratedPanel"), "BudgetPanelHost must register in ModalCoordinator migrated branch");
  });
});

// ── TECH-32918: info-panel UXML + USS + InfoPanelVM + InfoPanelHost ──────────

describe("infoPanelMigration", () => {
  it("infoPanelUxmlExists [task 4.0.3] — Assets/UI/Generated/info-panel.uxml emitted", () => {
    const uxmlPath = join(repoRoot, "Assets/UI/Generated/info-panel.uxml");
    assert.ok(existsSync(uxmlPath), `info-panel.uxml must exist: ${uxmlPath}`);
    const src = readFileSync(uxmlPath, "utf8");
    assert.ok(src.includes("xmlns") || src.includes("<ui:"), "must be valid UXML");
    assert.ok(src.includes("info-panel"), "must reference info-panel");
  });

  it("infoPanelUxmlHasSelectionBindings [task 4.0.3] — UXML has binding-path for selection context", () => {
    const uxmlPath = join(repoRoot, "Assets/UI/Generated/info-panel.uxml");
    assert.ok(existsSync(uxmlPath), "info-panel.uxml must exist");
    const src = readFileSync(uxmlPath, "utf8");
    assert.ok(src.includes('binding-path="CellCoord"') || src.includes('binding-path="EntityType"'), "must have selection context binding-paths");
    assert.ok(src.includes('binding-path="CloseCommand"'), "must have CloseCommand binding-path");
  });

  it("infoPanelUssExists [task 4.0.3] — Assets/UI/Generated/info-panel.uss emitted", () => {
    const ussPath = join(repoRoot, "Assets/UI/Generated/info-panel.uss");
    assert.ok(existsSync(ussPath), `info-panel.uss must exist: ${ussPath}`);
    const src = readFileSync(ussPath, "utf8");
    assert.ok(src.includes("--ds-") || src.includes("@import"), "must reference --ds-* tokens or import dark.tss");
  });

  it("infoPanelVMExists [task 4.0.3] — InfoPanelVM.cs POCO ViewModel present", () => {
    const vmPath = join(repoRoot, "Assets/Scripts/UI/ViewModels/InfoPanelVM.cs");
    assert.ok(existsSync(vmPath), `InfoPanelVM.cs must exist: ${vmPath}`);
    const src = readFileSync(vmPath, "utf8");
    assert.ok(src.includes("InfoPanelVM"), "InfoPanelVM class must be present");
    assert.ok(
      src.includes("INotifyPropertyChanged") || src.includes("PropertyChanged"),
      "InfoPanelVM must implement INotifyPropertyChanged",
    );
    assert.ok(src.includes("CellCoord"), "VM must expose CellCoord property");
    assert.ok(src.includes("EntityType"), "VM must expose EntityType property");
    assert.ok(src.includes("CloseCommand"), "VM must expose CloseCommand");
  });

  it("infoPanelHostExists [task 4.0.3] — InfoPanelHost.cs MonoBehaviour Host present", () => {
    const hostPath = join(repoRoot, "Assets/Scripts/UI/Hosts/InfoPanelHost.cs");
    assert.ok(existsSync(hostPath), `InfoPanelHost.cs must exist: ${hostPath}`);
    const src = readFileSync(hostPath, "utf8");
    assert.ok(src.includes("InfoPanelHost"), "InfoPanelHost class must be present");
    assert.ok(src.includes("UIDocument"), "InfoPanelHost must reference UIDocument");
    assert.ok(src.includes("dataSource"), "InfoPanelHost must set rootVisualElement.dataSource");
    assert.ok(src.includes("InfoPanelVM"), "InfoPanelHost must instantiate InfoPanelVM");
    assert.ok(src.includes("RegisterMigratedPanel"), "InfoPanelHost must register in ModalCoordinator migrated branch");
  });
});

// ── TECH-32919: stats-panel UXML + USS + StatsPanelVM + StatsPanelHost ───────

describe("statsPanelMigration", () => {
  it("statsPanelUxmlExists [task 4.0.4] — Assets/UI/Generated/stats-panel.uxml emitted", () => {
    const uxmlPath = join(repoRoot, "Assets/UI/Generated/stats-panel.uxml");
    assert.ok(existsSync(uxmlPath), `stats-panel.uxml must exist: ${uxmlPath}`);
    const src = readFileSync(uxmlPath, "utf8");
    assert.ok(src.includes("xmlns") || src.includes("<ui:"), "must be valid UXML");
    assert.ok(src.includes("stats-panel"), "must reference stats-panel");
  });

  it("statsPanelUxmlHasTabStrip [task 4.0.4] — UXML has tab strip with population/services/economy tabs", () => {
    const uxmlPath = join(repoRoot, "Assets/UI/Generated/stats-panel.uxml");
    assert.ok(existsSync(uxmlPath), "stats-panel.uxml must exist");
    const src = readFileSync(uxmlPath, "utf8");
    assert.ok(src.includes("tab-strip") || src.includes("tab-population"), "must have tab strip");
    assert.ok(src.includes("SelectPopulationTab") || src.includes("Population"), "must reference population tab");
    assert.ok(src.includes("SelectServicesTab") || src.includes("Services"), "must reference services tab");
    assert.ok(src.includes("SelectEconomyTab") || src.includes("Economy"), "must reference economy tab");
  });

  it("statsPanelUxmlHasStackedBar [task 4.0.4] — UXML has stacked-bar element", () => {
    const uxmlPath = join(repoRoot, "Assets/UI/Generated/stats-panel.uxml");
    assert.ok(existsSync(uxmlPath), "stats-panel.uxml must exist");
    const src = readFileSync(uxmlPath, "utf8");
    assert.ok(src.includes("stacked-bar") || src.includes("bar-segment"), "must have stacked bar elements");
  });

  it("statsPanelUssExists [task 4.0.4] — Assets/UI/Generated/stats-panel.uss emitted", () => {
    const ussPath = join(repoRoot, "Assets/UI/Generated/stats-panel.uss");
    assert.ok(existsSync(ussPath), `stats-panel.uss must exist: ${ussPath}`);
    const src = readFileSync(ussPath, "utf8");
    assert.ok(src.includes("--ds-") || src.includes("@import"), "must reference --ds-* tokens or import dark.tss");
    assert.ok(src.includes("tab") || src.includes("chart"), "must have tab or chart styling");
  });

  it("statsPanelVMExists [task 4.0.4] — StatsPanelVM.cs POCO ViewModel present", () => {
    const vmPath = join(repoRoot, "Assets/Scripts/UI/ViewModels/StatsPanelVM.cs");
    assert.ok(existsSync(vmPath), `StatsPanelVM.cs must exist: ${vmPath}`);
    const src = readFileSync(vmPath, "utf8");
    assert.ok(src.includes("StatsPanelVM"), "StatsPanelVM class must be present");
    assert.ok(
      src.includes("INotifyPropertyChanged") || src.includes("PropertyChanged"),
      "StatsPanelVM must implement INotifyPropertyChanged",
    );
    assert.ok(src.includes("ActiveTab"), "VM must expose ActiveTab property");
    assert.ok(src.includes("SelectPopulationTab") || src.includes("SelectServicesTab"), "VM must expose tab selection commands");
    assert.ok(src.includes("BarPopulationWidth") || src.includes("BarServicesWidth"), "VM must expose stacked-bar width properties");
  });

  it("statsPanelHostExists [task 4.0.4] — StatsPanelHost.cs MonoBehaviour Host present", () => {
    const hostPath = join(repoRoot, "Assets/Scripts/UI/Hosts/StatsPanelHost.cs");
    assert.ok(existsSync(hostPath), `StatsPanelHost.cs must exist: ${hostPath}`);
    const src = readFileSync(hostPath, "utf8");
    assert.ok(src.includes("StatsPanelHost"), "StatsPanelHost class must be present");
    assert.ok(src.includes("UIDocument"), "StatsPanelHost must reference UIDocument");
    assert.ok(src.includes("dataSource"), "StatsPanelHost must set rootVisualElement.dataSource");
    assert.ok(src.includes("StatsPanelVM"), "StatsPanelHost must instantiate StatsPanelVM");
    assert.ok(src.includes("RegisterMigratedPanel"), "StatsPanelHost must register in ModalCoordinator migrated branch");
  });

  it("themedTabBarObsoleteMarked [task 4.0.4] — ThemedTabBar.cs marked Obsolete per Q3 verdict", () => {
    const path = join(repoRoot, "Assets/Scripts/UI/Themed/ThemedTabBar.cs");
    assert.ok(existsSync(path), `ThemedTabBar.cs must exist: ${path}`);
    const src = readFileSync(path, "utf8");
    assert.ok(src.includes("[Obsolete]") || src.includes("Obsolete("), "ThemedTabBar must be marked [Obsolete] per Stage 4.0 Q3 verdict");
  });
});

// ── TECH-32920: map-panel UXML + USS + MapPanelVM + MapPanelHost ─────────────

describe("mapPanelMigration", () => {
  it("mapPanelUxmlExists [task 4.0.5] — Assets/UI/Generated/map-panel.uxml emitted", () => {
    const uxmlPath = join(repoRoot, "Assets/UI/Generated/map-panel.uxml");
    assert.ok(existsSync(uxmlPath), `map-panel.uxml must exist: ${uxmlPath}`);
    const src = readFileSync(uxmlPath, "utf8");
    assert.ok(src.includes("xmlns") || src.includes("<ui:"), "must be valid UXML");
    assert.ok(src.includes("map-panel"), "must reference map-panel");
  });

  it("mapPanelUxmlHasLayerToggles [task 4.0.5] — UXML has minimap surface + layer toggles", () => {
    const uxmlPath = join(repoRoot, "Assets/UI/Generated/map-panel.uxml");
    assert.ok(existsSync(uxmlPath), "map-panel.uxml must exist");
    const src = readFileSync(uxmlPath, "utf8");
    assert.ok(src.includes("minimap") || src.includes("layer"), "must have minimap or layer elements");
    assert.ok(src.includes("ZoomInCommand") || src.includes("ZoomOutCommand"), "must have zoom controls");
  });

  it("mapPanelUssExists [task 4.0.5] — Assets/UI/Generated/map-panel.uss emitted", () => {
    const ussPath = join(repoRoot, "Assets/UI/Generated/map-panel.uss");
    assert.ok(existsSync(ussPath), `map-panel.uss must exist: ${ussPath}`);
    const src = readFileSync(ussPath, "utf8");
    assert.ok(src.includes("--ds-") || src.includes("@import"), "must reference --ds-* tokens or import dark.tss");
  });

  it("mapPanelVMExists [task 4.0.5] — MapPanelVM.cs POCO ViewModel present", () => {
    const vmPath = join(repoRoot, "Assets/Scripts/UI/ViewModels/MapPanelVM.cs");
    assert.ok(existsSync(vmPath), `MapPanelVM.cs must exist: ${vmPath}`);
    const src = readFileSync(vmPath, "utf8");
    assert.ok(src.includes("MapPanelVM"), "MapPanelVM class must be present");
    assert.ok(
      src.includes("INotifyPropertyChanged") || src.includes("PropertyChanged"),
      "MapPanelVM must implement INotifyPropertyChanged",
    );
    assert.ok(src.includes("ShowTerrain") || src.includes("ShowZones"), "VM must expose layer toggle properties");
    assert.ok(src.includes("ZoomInCommand"), "VM must expose ZoomInCommand");
    assert.ok(src.includes("ZoomOutCommand"), "VM must expose ZoomOutCommand");
  });

  it("mapPanelHostExists [task 4.0.5] — MapPanelHost.cs MonoBehaviour Host present", () => {
    const hostPath = join(repoRoot, "Assets/Scripts/UI/Hosts/MapPanelHost.cs");
    assert.ok(existsSync(hostPath), `MapPanelHost.cs must exist: ${hostPath}`);
    const src = readFileSync(hostPath, "utf8");
    assert.ok(src.includes("MapPanelHost"), "MapPanelHost class must be present");
    assert.ok(src.includes("UIDocument"), "MapPanelHost must reference UIDocument");
    assert.ok(src.includes("dataSource"), "MapPanelHost must set rootVisualElement.dataSource");
    assert.ok(src.includes("MapPanelVM"), "MapPanelHost must instantiate MapPanelVM");
    assert.ok(src.includes("RegisterMigratedPanel"), "MapPanelHost must register in ModalCoordinator migrated branch");
  });
});

// ── TECH-32921: tool-subtype-picker UXML + USS + ToolSubtypePickerVM + Host ──

describe("toolSubtypePickerMigration", () => {
  it("toolSubtypePickerUxmlExists [task 4.0.6] — Assets/UI/Generated/tool-subtype-picker.uxml emitted", () => {
    const uxmlPath = join(repoRoot, "Assets/UI/Generated/tool-subtype-picker.uxml");
    assert.ok(existsSync(uxmlPath), `tool-subtype-picker.uxml must exist: ${uxmlPath}`);
    const src = readFileSync(uxmlPath, "utf8");
    assert.ok(src.includes("xmlns") || src.includes("<ui:"), "must be valid UXML");
    assert.ok(src.includes("tool-subtype-picker"), "must reference tool-subtype-picker");
  });

  it("toolSubtypePickerUxmlHasGridBinding [task 4.0.6] — UXML has ListView or grid binding for subtypes", () => {
    const uxmlPath = join(repoRoot, "Assets/UI/Generated/tool-subtype-picker.uxml");
    assert.ok(existsSync(uxmlPath), "tool-subtype-picker.uxml must exist");
    const src = readFileSync(uxmlPath, "utf8");
    assert.ok(src.includes("ListView") || src.includes('binding-path="Subtypes"'), "must have ListView or Subtypes binding");
    assert.ok(src.includes("CloseCommand"), "must have CloseCommand binding-path");
  });

  it("toolSubtypePickerUssExists [task 4.0.6] — Assets/UI/Generated/tool-subtype-picker.uss emitted", () => {
    const ussPath = join(repoRoot, "Assets/UI/Generated/tool-subtype-picker.uss");
    assert.ok(existsSync(ussPath), `tool-subtype-picker.uss must exist: ${ussPath}`);
    const src = readFileSync(ussPath, "utf8");
    assert.ok(src.includes("--ds-") || src.includes("@import"), "must reference --ds-* tokens or import dark.tss");
  });

  it("toolSubtypePickerVMExists [task 4.0.6] — ToolSubtypePickerVM.cs POCO ViewModel present", () => {
    const vmPath = join(repoRoot, "Assets/Scripts/UI/ViewModels/ToolSubtypePickerVM.cs");
    assert.ok(existsSync(vmPath), `ToolSubtypePickerVM.cs must exist: ${vmPath}`);
    const src = readFileSync(vmPath, "utf8");
    assert.ok(src.includes("ToolSubtypePickerVM"), "ToolSubtypePickerVM class must be present");
    assert.ok(
      src.includes("INotifyPropertyChanged") || src.includes("PropertyChanged"),
      "ToolSubtypePickerVM must implement INotifyPropertyChanged",
    );
    assert.ok(src.includes("Subtypes"), "VM must expose Subtypes collection");
    assert.ok(src.includes("SelectCommand"), "VM must expose SelectCommand");
    assert.ok(src.includes("CloseCommand"), "VM must expose CloseCommand");
  });

  it("toolSubtypePickerHostExists [task 4.0.6] — ToolSubtypePickerHost.cs MonoBehaviour Host present", () => {
    const hostPath = join(repoRoot, "Assets/Scripts/UI/Hosts/ToolSubtypePickerHost.cs");
    assert.ok(existsSync(hostPath), `ToolSubtypePickerHost.cs must exist: ${hostPath}`);
    const src = readFileSync(hostPath, "utf8");
    assert.ok(src.includes("ToolSubtypePickerHost"), "ToolSubtypePickerHost class must be present");
    assert.ok(src.includes("UIDocument"), "ToolSubtypePickerHost must reference UIDocument");
    assert.ok(src.includes("dataSource"), "ToolSubtypePickerHost must set rootVisualElement.dataSource");
    assert.ok(src.includes("ToolSubtypePickerVM"), "ToolSubtypePickerHost must instantiate ToolSubtypePickerVM");
    assert.ok(src.includes("RegisterMigratedPanel"), "ToolSubtypePickerHost must register in ModalCoordinator migrated branch");
  });
});

// ── Stage anchor test — green when all 6 tasks complete ─────────────────────

describe("modalsRouteViaUiDocument", () => {
  it("allModalUxmlFilesExist [stage 4.0 complete] — all 5 modal UXML files present", () => {
    const panels = ["budget-panel", "info-panel", "stats-panel", "map-panel", "tool-subtype-picker"];
    for (const panel of panels) {
      const uxmlPath = join(repoRoot, `Assets/UI/Generated/${panel}.uxml`);
      assert.ok(existsSync(uxmlPath), `${panel}.uxml must exist`);
    }
  });

  it("allModalVMFilesExist [stage 4.0 complete] — all 5 VM classes present", () => {
    const vms = ["BudgetPanelVM", "InfoPanelVM", "StatsPanelVM", "MapPanelVM", "ToolSubtypePickerVM"];
    for (const vm of vms) {
      const vmPath = join(repoRoot, `Assets/Scripts/UI/ViewModels/${vm}.cs`);
      assert.ok(existsSync(vmPath), `${vm}.cs must exist`);
    }
  });

  it("allModalHostFilesExist [stage 4.0 complete] — all 5 Host classes present", () => {
    const hosts = ["BudgetPanelHost", "InfoPanelHost", "StatsPanelHost", "MapPanelHost", "ToolSubtypePickerHost"];
    for (const host of hosts) {
      const hostPath = join(repoRoot, `Assets/Scripts/UI/Hosts/${host}.cs`);
      assert.ok(existsSync(hostPath), `${host}.cs must exist`);
    }
  });

  it("modalCoordinatorSupportsCoExistence [stage 4.0 complete] — ModalCoordinator has full co-existence API", () => {
    const path = join(repoRoot, "Assets/Scripts/UI/Modals/ModalCoordinator.cs");
    assert.ok(existsSync(path), "ModalCoordinator.cs must exist");
    const src = readFileSync(path, "utf8");
    assert.ok(src.includes("RegisterMigratedPanel"), "must have RegisterMigratedPanel");
    assert.ok(src.includes("Show(string"), "must have Show(string) overload");
    assert.ok(src.includes("HideMigrated"), "must have HideMigrated");
    assert.ok(src.includes("IsMigrated"), "must have IsMigrated query");
  });
});
