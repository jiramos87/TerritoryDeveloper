using Territory.UI;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor checks for <see cref="UiTheme"/> assets used by menu + HUD styling.
/// Menu: <b>Territory Developer → Reports → Validate UI Theme asset</b>.
/// </summary>
public static class UiThemeValidationMenu
{
    const string MenuRoot = "Territory Developer/Reports/";
    const string DefaultThemePath = "Assets/UI/Theme/DefaultUiTheme.asset";

    [MenuItem(MenuRoot + "Validate UI Theme asset", priority = 13)]
    public static void ValidateDefaultUiTheme()
    {
        var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(DefaultThemePath);
        if (theme == null)
        {
            Debug.LogWarning($"[UI Theme] Missing asset at {DefaultThemePath}. Create via Assets → Create → Territory → UI → Ui Theme.");
            return;
        }

        Debug.Log($"[UI Theme] OK: loaded {DefaultThemePath} — menu font={theme.MenuButtonFontSize}, " +
                  $"display/body/caption={theme.FontSizeDisplay}/{theme.FontSizeBody}/{theme.FontSizeCaption}, " +
                  $"surfaceCardHud.a={theme.SurfaceCardHud.a:F2}.");
    }

    [MenuItem(MenuRoot + "Reseed catalog-shape fields", priority = 14)]
    public static void ReseedCatalogShapeFields()
    {
        var asset = AssetDatabase.LoadAssetAtPath<UiTheme>(DefaultThemePath);
        if (asset == null)
        {
            Debug.LogWarning($"[UI Theme] Missing asset at {DefaultThemePath}.");
            return;
        }

        var so = new SerializedObject(asset);

        ReseedKvList(so.FindProperty("frameStyleEntries"), "catalog_sprite_slug");
        ReseedKvList(so.FindProperty("fontFaceEntries"), "font_catalog_slug");

        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Debug.Log("[UI Theme] Reseed catalog-shape fields: done. catalog_sprite_slug + font_catalog_slug populated from entry slugs.");
    }

    static void ReseedKvList(SerializedProperty list, string catalogSlugField)
    {
        if (list == null || !list.isArray) return;
        for (int i = 0; i < list.arraySize; i++)
        {
            var entry = list.GetArrayElementAtIndex(i);
            var slugProp = entry.FindPropertyRelative("slug");
            var valueProp = entry.FindPropertyRelative("value");
            if (slugProp == null || valueProp == null) continue;
            var catalogField = valueProp.FindPropertyRelative(catalogSlugField);
            if (catalogField != null && string.IsNullOrEmpty(catalogField.stringValue))
                catalogField.stringValue = slugProp.stringValue;
        }
    }
}
