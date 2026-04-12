using System;
using UnityEngine;

namespace Territory.Persistence
{
    /// <summary>
    /// Interchange DTO for <c>geography_init_params</c> (JSON Schema v1). Not Save data — loaded from
    /// <see cref="GeographyInitParamsLoader"/> at Geography init. Field names match
    /// <c>docs/schemas/geography-init-params.v1.schema.json</c> → <see cref="JsonUtility"/> + Node/Zod parity.
    /// </summary>
    [Serializable]
    public class GeographyInitParamsDto
    {
        public string artifact;
        public int schema_version;
        public int seed;
        public GeographyInitMapDto map;
        public GeographyInitWaterDto water;
        public GeographyInitRiversDto rivers;
        public GeographyInitForestDto forest;
    }

    [Serializable]
    public class GeographyInitMapDto
    {
        public int width;
        public int height;
    }

    [Serializable]
    public class GeographyInitWaterDto
    {
        public float seaBias;
    }

    [Serializable]
    public class GeographyInitRiversDto
    {
        public bool enabled;
    }

    [Serializable]
    public class GeographyInitForestDto
    {
        public float coverageTarget;
    }
}
