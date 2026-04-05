using Territory.UI;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor checks for <see cref="UiTheme"/> assets used by menu and HUD styling.
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

        Debug.Log($"[UI Theme] OK: loaded {DefaultThemePath} (menu button font size={theme.MenuButtonFontSize}).");
    }
}
