using System;
using System.Collections.Generic;

namespace Territory.Terrain
{
    /// <summary>
    /// Serialization + legacy load paths extracted from WaterMap (Strategy γ Stage 3.2).
    /// Partial class — same assembly as WaterMap.cs.
    /// </summary>
    public sealed partial class WaterMap
    {
        public WaterMapData GetSerializableData()
        {
            var data = new WaterMapData
            {
                formatVersion = FormatVersionV3,
                width = width,
                height = height,
                waterBodyIds = new int[width * height]
            };
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    data.waterBodyIds[x + y * width] = waterBodyIds[x, y];
            var list = new List<WaterBodySerialized>();
            foreach (var kv in bodies)
            {
                var wb = kv.Value;
                var ser = new WaterBodySerialized
                {
                    id = wb.Id,
                    surfaceHeight = wb.SurfaceHeight,
                    bodyClassification = (int)wb.Classification,
                    cellIndicesFlat = new int[wb.CellIndices.Count]
                };
                int i = 0;
                foreach (int idx in wb.CellIndices)
                    ser.cellIndicesFlat[i++] = idx;
                list.Add(ser);
            }
            data.bodies = list.ToArray();
            return data;
        }

        public void LoadFromSerializableData(WaterMapData data)
        {
            if (data == null) return;
            if (data.formatVersion < FormatVersionV2 && data.waterCells != null && data.waterCells.Length == width * height)
            {
                LoadLegacyBoolFormat(data);
                return;
            }
            if (data.waterBodyIds == null || data.waterBodyIds.Length != width * height) return;
            ClearAllWater();
            for (int i = 0; i < data.waterBodyIds.Length; i++)
            {
                int x = i % width, y = i / width;
                waterBodyIds[x, y] = data.waterBodyIds[i];
            }
            bodies.Clear();
            nextBodyId = 1;
            if (data.bodies != null)
                foreach (var ser in data.bodies)
                {
                    WaterBodyType cls = DeserializeBodyClassification(ser);
                    var body = new WaterBody(ser.id, ser.surfaceHeight, cls);
                    if (ser.cellIndicesFlat != null)
                        foreach (int flat in ser.cellIndicesFlat)
                            body.AddCellIndex(flat);
                    bodies[ser.id] = body;
                    nextBodyId = Math.Max(nextBodyId, ser.id + 1);
                }
            RebuildBodyIdsFromCellsIfNeeded();
        }

        private static WaterBodyType DeserializeBodyClassification(WaterBodySerialized ser)
        {
            if (ser == null) return WaterBodyType.Lake;
            WaterBodyType cls = (WaterBodyType)ser.bodyClassification;
            return cls == WaterBodyType.None ? WaterBodyType.Lake : cls;
        }

        private void LoadLegacyBoolFormat(WaterMapData data)
        {
            ClearAllWater();
            const int legacyId = 1;
            var body = new WaterBody(legacyId, 0, WaterBodyType.Lake);
            bodies[legacyId] = body;
            nextBodyId = 2;
            for (int i = 0; i < data.waterCells.Length; i++)
            {
                if (!data.waterCells[i]) continue;
                int x = i % width, y = i / width;
                waterBodyIds[x, y] = legacyId;
                body.AddCellIndex(ToFlat(x, y));
            }
        }

        private void RebuildBodyIdsFromCellsIfNeeded()
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    int id = waterBodyIds[x, y];
                    if (id == 0) continue;
                    if (!bodies.ContainsKey(id)) waterBodyIds[x, y] = 0;
                }
        }
    }

    [Serializable]
    public sealed class WaterMapData
    {
        public int formatVersion = WaterMap.FormatVersionV3;
        public int width;
        public int height;
        /// <summary>V2: flattened water body id per cell (0 = dry).</summary>
        public int[] waterBodyIds;
        /// <summary>V1 legacy: flattened bool water mask.</summary>
        public bool[] waterCells;
        public WaterBodySerialized[] bodies;
    }

    [Serializable]
    public sealed class WaterBodySerialized
    {
        public int id;
        public int surfaceHeight;
        /// <summary>Values from <see cref="WaterBodyType"/> (Lake, Sea, River). 0 / missing → treat as Lake on load.</summary>
        public int bodyClassification;
        public int[] cellIndicesFlat;
    }
}
