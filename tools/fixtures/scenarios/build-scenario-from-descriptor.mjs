#!/usr/bin/env node
/**
 * Emits GameSaveData JSON from a scenario_descriptor_v1 file when the descriptor is Node-emitting:
 * layoutKind declarative, no roadStrokes (roads require Unity PathTerraformPlan + Apply — use unity-build-scenario-from-descriptor.sh).
 *
 * Usage:
 *   node tools/fixtures/scenarios/build-scenario-from-descriptor.mjs --descriptor PATH --output PATH
 */
import { readFileSync, writeFileSync, mkdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));

const W = 32;
const H = 32;
const TERRAIN_BASE_ORDER = 0;
const DEPTH_MULTIPLIER = 100;
const HEIGHT_MULTIPLIER = 10;
const tileWidth = 1;
const tileHeight = 0.5;

function calculateTerrainSortingOrder(x, y, height) {
  const isometricDepth = x + y;
  const depthOrder = -isometricDepth * DEPTH_MULTIPLIER;
  const heightOrder = height * HEIGHT_MULTIPLIER;
  return TERRAIN_BASE_ORDER + depthOrder + heightOrder;
}

function gridToWorldPlanar(gridX, gridY, heightLevel = 1) {
  const heightOffset = (heightLevel - 1) * (tileHeight / 2);
  const wx = (gridX - gridY) * (tileWidth / 2);
  const wy = (gridX + gridY) * (tileHeight / 2) + heightOffset;
  return { x: wx, y: wy };
}

function makeCell(x, y, height) {
  const pos = gridToWorldPlanar(x, y, height);
  const sortingOrder = calculateTerrainSortingOrder(x, y, height);
  return {
    hasRoadAtLeft: false,
    hasRoadAtTop: false,
    hasRoadAtRight: false,
    hasRoadAtBottom: false,
    population: 0,
    powerOutput: 0,
    powerConsumption: 0,
    waterConsumption: 0,
    buildingType: "Grass",
    buildingSize: 1,
    happiness: 0,
    prefabName: "",
    secondaryPrefabName: "",
    zoneType: "Grass",
    waterBodyType: "None",
    waterBodyId: 0,
    occupiedBuilding: { instanceID: 0 },
    occupiedBuildingName: "",
    isPivot: false,
    powerPlant: { instanceID: 0 },
    waterPlant: { instanceID: 0 },
    transformPosition: { x: pos.x, y: pos.y },
    x,
    y,
    sortingOrder,
    height,
    forestType: "None",
    forestPrefabName: "",
    hasTree: false,
    treePrefabName: "",
    isInterstate: false,
    desirability: 0,
    closeForestCount: 0,
    closeWaterCount: 0,
    prefab: { instanceID: 0 },
  };
}

function parseArgs(argv) {
  let descriptor = "";
  let output = "";
  for (let i = 2; i < argv.length; i++) {
    if (argv[i] === "--descriptor" && argv[i + 1]) {
      descriptor = argv[++i];
    } else if (argv[i] === "--output" && argv[i + 1]) {
      output = argv[++i];
    }
  }
  return { descriptor, output };
}

function validateDescriptorCore(d) {
  if (!d || typeof d !== "object") throw new Error("descriptor: invalid root");
  if (d.artifact !== "scenario_descriptor_v1") {
    throw new Error(
      'descriptor rejected: expected artifact "scenario_descriptor_v1" (Save data uses a different shape)',
    );
  }
  if (d.schemaVersion !== 1) throw new Error("descriptor rejected: schemaVersion must be 1");
  if (!d.scenarioId || typeof d.scenarioId !== "string") {
    throw new Error("descriptor rejected: scenarioId is required (kebab-case)");
  }
  if (!d.map || d.map.width !== W || d.map.height !== H) {
    throw new Error("descriptor rejected: map must be 32×32 for v1");
  }
  if (!d.terrain || !d.terrain.mode) throw new Error("descriptor rejected: terrain.mode is required");
  if (d.roadStrokes && d.roadStrokes.length > 0) {
    throw new Error(
      "descriptor has road strokes: Node cannot run PathTerraformPlan + Apply. Use tools/scripts/unity-build-scenario-from-descriptor.sh (see BUILDER.md).",
    );
  }
  if (d.waterMapData != null) {
    throw new Error(
      "descriptor includes waterMapData: Node builder v1 supports terrain-only; use Unity batch applier for water + shore refresh.",
    );
  }
  if (d.layoutKind !== "declarative") {
    throw new Error(
      `layoutKind "${d.layoutKind}": Node emits only declarative terrain-only saves; use Unity for AUTO-adjacent exports.`,
    );
  }
}

function heightAt(d, x, y) {
  const t = d.terrain;
  if (t.mode === "uniform") {
    const h = t.uniformHeight;
    if (typeof h !== "number" || h < 1 || h > 32) throw new Error("uniformHeight out of range");
    return h;
  }
  if (t.mode === "rowMajor") {
    const arr = t.heightsRowMajor;
    if (!Array.isArray(arr) || arr.length !== W * H) {
      throw new Error("heightsRowMajor must have length 1024 (y * 32 + x)");
    }
    return arr[y * W + x];
  }
  throw new Error(`unknown terrain.mode "${t.mode}"`);
}

function buildSave(d) {
  const gridData = [];
  for (let y = 0; y < H; y++) {
    for (let x = 0; x < W; x++) {
      const height = heightAt(d, x, y);
      gridData.push(makeCell(x, y, height));
    }
  }

  const saveName = d.saveOverlay?.saveName || d.scenarioId;
  const cityName = d.saveOverlay?.cityName || "Test32";
  const inGameTime = d.saveOverlay?.inGameTime || { day: 27, month: 8, year: 2024 };

  return {
    saveName,
    cityName,
    realWorldSaveTimeTicks: 0,
    inGameTime,
    gridData,
    gridWidth: W,
    gridHeight: H,
    isConnectedToInterstate: false,
    cityStats: {
      population: 0,
      money: 20000,
      happiness: 50,
      pollution: 0,
      residentialZoneCount: 0,
      residentialBuildingCount: 0,
      commercialZoneCount: 0,
      commercialBuildingCount: 0,
      industrialZoneCount: 0,
      industrialBuildingCount: 0,
      residentialLightBuildingCount: 0,
      residentialLightZoningCount: 0,
      residentialMediumBuildingCount: 0,
      residentialMediumZoningCount: 0,
      residentialHeavyBuildingCount: 0,
      residentialHeavyZoningCount: 0,
      commercialLightBuildingCount: 0,
      commercialLightZoningCount: 0,
      commercialMediumBuildingCount: 0,
      commercialMediumZoningCount: 0,
      commercialHeavyBuildingCount: 0,
      commercialHeavyZoningCount: 0,
      industrialLightBuildingCount: 0,
      industrialLightZoningCount: 0,
      industrialMediumBuildingCount: 0,
      industrialMediumZoningCount: 0,
      industrialHeavyBuildingCount: 0,
      industrialHeavyZoningCount: 0,
      roadCount: 0,
      grassCount: W * H,
      cityPowerConsumption: 0,
      cityPowerOutput: 0,
      cityWaterConsumption: 0,
      cityWaterOutput: 0,
      cityName,
      forestCellCount: 0,
      forestCoveragePercentage: 0,
      simulateGrowth: false,
      communes: [],
    },
    growthBudget: {
      totalGrowthBudget: 5000,
      growthBudgetPercent: 10,
      roadBudgetPercent: 25,
      energyBudgetPercent: 25,
      waterBudgetPercent: 25,
      zoningBudgetPercent: 25,
      roadSpentThisCycle: 0,
      energySpentThisCycle: 0,
      waterSpentThisCycle: 0,
      zoningSpentThisCycle: 0,
    },
    minimapActiveLayers: 3,
  };
}

function main() {
  const { descriptor, output } = parseArgs(process.argv);
  if (!descriptor || !output) {
    console.error("Usage: node build-scenario-from-descriptor.mjs --descriptor PATH --output PATH");
    process.exitCode = 1;
    return;
  }
  const raw = readFileSync(descriptor, "utf8");
  const d = JSON.parse(raw);
  validateDescriptorCore(d);
  const save = buildSave(d);
  mkdirSync(dirname(output), { recursive: true });
  writeFileSync(output, JSON.stringify(save), "utf8");
  console.log("Wrote", output);
}

main();
