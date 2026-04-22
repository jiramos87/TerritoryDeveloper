using UnityEditor;
using UnityEngine;

/// <summary>Editor menu to call <see cref="GridAssetCatalog.ReloadFromDisk"/> without a domain reload (TECH-673 stub).</summary>
public static class GridAssetCatalogMenu
{
    const string MenuPath = "Territory Developer/Catalog/Reload Grid Asset Catalog";

    [MenuItem(MenuPath, priority = 50)]
    public static void ReloadGridAssetCatalog()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Grid asset catalog",
                "Enter Play Mode (scene must contain a GridAssetCatalog) or load a scene with the component.",
                "OK");
            return;
        }

        var c = Object.FindObjectOfType<GridAssetCatalog>();
        if (c == null)
        {
            EditorUtility.DisplayDialog("Grid asset catalog", "No GridAssetCatalog in the loaded scene.", "OK");
            return;
        }

        c.ReloadFromDisk();
        Debug.Log("[GridAssetCatalog] ReloadFromDisk() completed via menu.");
    }
}
