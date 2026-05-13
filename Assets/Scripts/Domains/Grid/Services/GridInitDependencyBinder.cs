using System;
using System.Collections.Generic;
using UnityEngine;

namespace Domains.Grid.Services
{
/// <summary>Validates the 13 inspector refs on GridManager before InitializeGrid runs. Fail-fast with readable log line instead of mid-pipeline NRE.</summary>
public static class GridInitDependencyBinder
{
    /// <summary>Result of a dependency validation pass.</summary>
    public struct ValidationResult
    {
        public List<string> missing;
        public int missing_count;
    }

    private static readonly string[] RequiredFields =
    {
        "zoneManager", "uiManager", "cityStats", "cursorManager", "terrainManager",
        "demandManager", "waterManager", "GameNotificationManager", "forestManager",
        "cameraController", "roadManager", "interstateManager", "buildingSelectorMenuController"
    };

    /// <summary>Validate all 13 GridManager inspector refs via reflection. Returns result with missing field names.</summary>
    public static ValidationResult Validate(MonoBehaviour gm)
    {
        var result = new ValidationResult { missing = new List<string>() };
        if (gm == null) { result.missing.AddRange(RequiredFields); result.missing_count = result.missing.Count; return result; }

        Type type = gm.GetType();
        foreach (string fieldName in RequiredFields)
        {
            var field = type.GetField(fieldName,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            if (field == null) { result.missing.Add(fieldName); continue; }
            object value = field.GetValue(gm);
            bool isNull = value == null || (value is UnityEngine.Object uo && uo == null);
            if (isNull) result.missing.Add(fieldName);
        }

        result.missing_count = result.missing.Count;
        return result;
    }
}
}
