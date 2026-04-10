using System;
using Territory.Terrain;
using Territory.Timing;
using UnityEngine;

namespace Territory.Testing
{
    /// <summary>
    /// Root DTO for <c>scenario_descriptor_v1</c> interchange JSON (camelCase keys for <see cref="JsonUtility"/>).
    /// Used by <see cref="ScenarioDescriptorRuntimeApplier"/> and batch tooling — not player <b>Save data</b>.
    /// </summary>
    [Serializable]
    public sealed class ScenarioDescriptorV1
    {
        public string artifact;
        public int schemaVersion;
        public string scenarioId;
        public string layoutKind;
        public ScenarioMapV1 map;
        public ScenarioTerrainV1 terrain;
        public WaterMapData waterMapData;
        public RoadStrokeV1[] roadStrokes;
        public ScenarioSaveOverlayV1 saveOverlay;
    }

    [Serializable]
    public sealed class ScenarioMapV1
    {
        public int width;
        public int height;
    }

    [Serializable]
    public sealed class ScenarioTerrainV1
    {
        public string mode;
        public int uniformHeight;
        public int[] heightsRowMajor;
    }

    [Serializable]
    public sealed class RoadStrokeV1
    {
        public string kind;
        public GridPointV1[] cells;
    }

    [Serializable]
    public sealed class GridPointV1
    {
        public int x;
        public int y;
    }

    [Serializable]
    public sealed class ScenarioSaveOverlayV1
    {
        public string saveName;
        public string cityName;
        public InGameTime inGameTime;
    }
}
