using UnityEngine;
using Territory.Forests;

namespace Territory.Geography
{
    /// <summary>Placement category — drives suitability checks in <see cref="IGeography.IsPositionSuitableForPlacement"/>.</summary>
    public enum PlacementType
    {
        Forest,
        Water,
        Building,
        Zone,
        Infrastructure
    }

    /// <summary>Per-cell environmental bonus aggregating desirability + adjacency counts + on-cell forest type.</summary>
    [System.Serializable]
    public struct EnvironmentalBonus
    {
        public float desirability;
        public int adjacentForests;
        public int adjacentWater;
        public Forest.ForestType forestType;

        public float GetTotalBonus()
        {
            float bonus = desirability + (adjacentForests * 2f) + (adjacentWater * 3f);

            switch (forestType)
            {
                case Forest.ForestType.Sparse:
                    bonus += 1f;
                    break;
                case Forest.ForestType.Medium:
                    bonus += 2f;
                    break;
                case Forest.ForestType.Dense:
                    bonus += 3f;
                    break;
            }

            return bonus;
        }
    }

    /// <summary>Snapshot of geography pipeline state — terrain dims, water + forest cell counts.</summary>
    [System.Serializable]
    public struct GeographyData
    {
        [Header("Terrain Data")]
        public bool hasTerrainData;
        public int terrainWidth;
        public int terrainHeight;

        [Header("Water Data")]
        public bool hasWaterData;
        public int waterCellCount;

        [Header("Forest Data")]
        public bool hasForestData;
        public int forestCellCount;
        public float forestCoveragePercentage;
        public int sparseForestCount;
        public int mediumForestCount;
        public int denseForestCount;
    }

    /// <summary>Forest density tally over a radius region — sparse/medium/dense counts + coverage.</summary>
    [System.Serializable]
    public struct ForestRegionInfo
    {
        public int sparseCount;
        public int mediumCount;
        public int denseCount;
        public int totalCells;
        public float forestCoverage;

        public int GetTotalForests()
        {
            return sparseCount + mediumCount + denseCount;
        }
    }
}
