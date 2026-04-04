using System.IO;
using System.Text;
using Territory.Geography;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Writes <c>tools/reports/last-geography-init.json</c> (gitignored) from <see cref="GeographyManager.BuildGeographyInitReportJson"/> (TECH-39 §7.11.4).
/// </summary>
public static class GeographyInitReportMenu
{
    const string MenuPath = "Territory Developer/Reports/Export Geography Init Report (last-geography-init.json)";

    [MenuItem(MenuPath, priority = 22)]
    public static void ExportGeographyInitReport()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Geography init report", "Enter Play Mode after geography has initialized.", "OK");
            return;
        }

        var geography = Object.FindObjectOfType<GeographyManager>();
        if (geography == null)
        {
            EditorUtility.DisplayDialog("Geography init report", "No GeographyManager in the loaded scene.", "OK");
            return;
        }

        try
        {
            string json = geography.BuildGeographyInitReportJson();
            string reportsDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "tools", "reports"));
            Directory.CreateDirectory(reportsDir);
            string path = Path.Combine(reportsDir, "last-geography-init.json");
            File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            Debug.Log($"[GeographyInitReport] Wrote {path}");
            EditorUtility.RevealInFinder(path);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GeographyInitReport] Export failed: {ex.Message}");
        }
    }
}
