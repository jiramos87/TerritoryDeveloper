#!/usr/bin/env node
/**
 * Emits tools/fixtures/scenarios/reference-flat-32x32/save.json — flat grass 32×32 GameSaveData
 * compatible with Unity JsonUtility + GameSaveManager.LoadGame.
 *
 * Sorting order matches TerrainManager.CalculateTerrainSortingOrder (TERRAIN_BASE_ORDER=0, DEPTH=100, HEIGHT=10).
 * World positions match IsometricGridMath.GridToWorldPlanar (tileWidth=1, tileHeight=0.5, heightLevel=1).
 */
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const outDir = join(__dirname, "reference-flat-32x32");
const outFile = join(outDir, "save.json");

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

function makeCell(x, y) {
  const height = 1;
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

const gridData = [];
for (let y = 0; y < H; y++) {
  for (let x = 0; x < W; x++) {
    gridData.push(makeCell(x, y));
  }
}

const save = {
  saveName: "reference-flat-32x32",
  cityName: "Test32",
  realWorldSaveTimeTicks: 0,
  inGameTime: { day: 27, month: 8, year: 2024 },
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
    cityName: "Test32",
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

mkdirSync(outDir, { recursive: true });
writeFileSync(outFile, JSON.stringify(save), "utf8");
console.log("Wrote", outFile);
