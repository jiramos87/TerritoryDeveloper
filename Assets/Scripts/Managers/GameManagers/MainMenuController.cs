using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Territory.Audio;
using Territory.Persistence;
using Territory.UI.Registry;

namespace Territory.UI
{
/// <summary>
/// Main menu UI: Continue, New Game, Load City, Options. Scene transition → CityScene
/// with appropriate <see cref="GameStartInfo"/>.
/// Wave A1 (TECH-27065): also acts as bind-dispatcher consumer when useBakedUi=true.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    private const string LastSavePathKey = "LastSavePath";
    private const int CitySceneBuildIndex = 1;

    [Header("Optional: assign in Inspector to use pre-built UI")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadCityButton;
    [SerializeField] private Button optionsButton;
    [Header("Theme (optional)")]
    [SerializeField] private UiTheme menuTheme;
    // Wave A3 (TECH-27077): useBakedUi permanently true — flag removed; baked-UI path is the only path.
    // Wave A1 (TECH-27065): optional explicit wires; resolved via GetComponent when null.
    [SerializeField] private UiActionRegistry actionRegistry;
    [SerializeField] private UiBindRegistry bindRegistry;

    private string saveFolderPath;

    void Awake()
    {
        // Resolve registries from scene if not serialized.
        if (actionRegistry == null)
            actionRegistry = GetComponentInParent<UiActionRegistry>()
                ?? FindObjectOfType<UiActionRegistry>();
        if (bindRegistry == null)
            bindRegistry = GetComponentInParent<UiBindRegistry>()
                ?? FindObjectOfType<UiBindRegistry>();

        RegisterBakedUiHandlers();
        UpdateContinueButtonStateBaked();
    }

    void Start()
    {
        saveFolderPath = Application.persistentDataPath;
        // Baked-UI path only (Wave A3 permanent cutover — TECH-27077).
        ApplyMenuThemeIfAny();
        WireHoverBlips();
    }

    // ── Wave A1 baked-UI bind-dispatcher path ─────────────────────────────────

    /// <summary>Register 7 action handlers + subscribe contentScreen enum bind.</summary>
    private void RegisterBakedUiHandlers()
    {
        if (actionRegistry == null)
        {
            Debug.LogWarning("[MainMenuController] UiActionRegistry not found — baked-UI handlers skipped.");
            return;
        }

        actionRegistry.Register("mainmenu.continue",      _ => OnContinueClicked());
        actionRegistry.Register("mainmenu.new-game",      _ => OnNewGameClicked());
        actionRegistry.Register("mainmenu.load",          _ => OnLoadCityClicked());
        actionRegistry.Register("mainmenu.settings",      _ => OnOptionsClicked());
        actionRegistry.Register("mainmenu.quit",          _ => OnQuitClicked());
        actionRegistry.Register("mainmenu.quit-confirmed",_ => OnQuitConfirmed());
        actionRegistry.Register("mainmenu.back",          _ => OnBackClicked());

        if (bindRegistry != null)
        {
            bindRegistry.Subscribe<string>("mainmenu.contentScreen", OnContentScreenChanged);
        }
    }

    /// <summary>Drive mainmenu.continue.disabled bind from HasAnySave.</summary>
    private void UpdateContinueButtonStateBaked()
    {
        if (bindRegistry == null) return;
        bool hasSave = GameSaveManager.HasAnySave(Application.persistentDataPath);
        bindRegistry.Set("mainmenu.continue.disabled", !hasSave);
    }

    private void OnContentScreenChanged(string screenId)
    {
        // Sub-view visibility driven by bind — host renders declared sub-view.
        // Placeholder: log; Wave A2+ will drive real sub-view swap.
        Debug.Log($"[MainMenuController] contentScreen → {screenId}");
    }

    private void OnQuitClicked()
    {
        BlipEngine.Play(BlipId.UiButtonClick);
        // Trigger confirm-button countdown via bind; confirm-button archetype handles.
        bindRegistry?.Set("mainmenu.contentScreen", "quit-confirm");
    }

    private void OnQuitConfirmed()
    {
        BlipEngine.Play(BlipId.UiButtonClick);
        Application.Quit();
    }

    private void OnBackClicked()
    {
        BlipEngine.Play(BlipId.UiButtonClick);
        bindRegistry?.Set("mainmenu.contentScreen", "main");
    }

    /// <summary>
    /// When menu buttons authored in scene but overlay panels omitted → create load/options panels
    /// under serialized <see cref="Canvas"/> at runtime.
    /// </summary>
    private void ApplyMenuThemeIfAny()
    {
        if (menuTheme == null)
            return;
        ApplyThemeToMenuStrip(continueButton, newGameButton, loadCityButton, optionsButton);
    }

    private void ApplyThemeToMenuStrip(Button continueBtn, Button newGameBtn, Button loadCityBtn, Button optionsBtn)
    {
        if (menuTheme == null)
            return;
        foreach (Button b in new[] { continueBtn, newGameBtn, loadCityBtn, optionsBtn })
        {
            if (b == null)
                continue;
            var graphic = b.GetComponent<Image>();
            if (graphic != null)
                graphic.color = menuTheme.MenuButtonColor;
            var label = b.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.color = menuTheme.MenuButtonTextColor;
                label.fontSize = menuTheme.MenuButtonFontSize;
            }
        }
    }

    /// <summary>Returns LastSavePath if valid, otherwise the most recent save file in the folder.</summary>
    private string GetMostRecentSavePath()
    {
        string lastPath = PlayerPrefs.GetString(LastSavePathKey, "");
        if (!string.IsNullOrEmpty(lastPath) && File.Exists(lastPath))
            return lastPath;
        var files = GameSaveManager.GetSaveFiles(Application.persistentDataPath);
        return files.Length > 0 ? files[0].FilePath : null;
    }

    public void OnContinueClicked()
    {
        BlipEngine.Play(BlipId.UiButtonClick);
        string path = GetMostRecentSavePath();
        if (string.IsNullOrEmpty(path))
            return;
        GameStartInfo.SetPendingLoadPath(path);
        SceneManager.LoadScene(CitySceneBuildIndex);
    }

    public void OnNewGameClicked()
    {
        BlipEngine.Play(BlipId.UiButtonClick);
        GameStartInfo.SetStartModeNewGame();
        SceneManager.LoadScene(CitySceneBuildIndex);
    }

    public void OnLoadCityClicked()
    {
        BlipEngine.Play(BlipId.UiButtonClick);
        // Wave A3: drive baked-UI content slot via bind.
        bindRegistry?.Set("mainmenu.contentScreen", "save-load");
    }

    public void OnOptionsClicked()
    {
        BlipEngine.Play(BlipId.UiButtonClick);
        // Wave A3: drive baked-UI content slot via bind.
        bindRegistry?.Set("mainmenu.contentScreen", "settings");
    }

    // -------------------------------------------------------------------------
    // Hover blip wiring
    // -------------------------------------------------------------------------

    private void AddHoverBlip(Button btn)
    {
        if (btn == null) return;
        var trig = btn.GetComponent<EventTrigger>() ?? btn.gameObject.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback.AddListener(_ => BlipEngine.Play(BlipId.UiButtonHover));
        trig.triggers.Add(entry);
    }

    /// <summary>
    /// Wave A2 (TECH-27071) — 4-arg overload: launches CityScene with chosen budget, city name + seed.
    /// Wires <see cref="Territory.Economy.EconomyManager.SetStartingFunds"/> +
    /// <see cref="Territory.Economy.CityStats.SetCityName"/> before scene transition when managers available.
    /// </summary>
    public void StartNewGame(int mapSize, int startingBudget, string cityName, int seed)
    {
        BlipEngine.Play(BlipId.UiButtonClick);

        // Apply pre-game-start values to managers if already loaded in scene.
        var eco = FindObjectOfType<Territory.Economy.EconomyManager>();
        if (eco != null) eco.SetStartingFunds(startingBudget);
        var stats = FindObjectOfType<Territory.Economy.CityStats>();
        if (stats != null) stats.SetCityName(cityName);

        // Store in GameStartInfo for CityScene Awake to consume (budget + name + seed).
        Territory.Persistence.GameStartInfo.SetStartModeNewGame(mapSize, seed, 0);
        Territory.Persistence.GameStartInfo.SetPendingNewGameConfig(startingBudget, cityName);
        SceneManager.LoadScene(CitySceneBuildIndex);
    }

    /// <summary>Legacy 3-arg overload kept for one ship cycle.</summary>
    [System.Obsolete("Use 4-arg overload; remove next ship cycle.")]
    public void StartNewGame(int mapSize, int seed, int scenarioIndex)
    {
        BlipEngine.Play(BlipId.UiButtonClick);
        Territory.Persistence.GameStartInfo.SetStartModeNewGame(mapSize, seed, scenarioIndex);
        SceneManager.LoadScene(CitySceneBuildIndex);
    }

    public void ResumeGame() { }

    public void OpenSettings() { OnOptionsClicked(); }

    public void SaveGame() { }

    public void LoadGame() { OnLoadCityClicked(); }

    public void ReturnToMainMenu() { SceneManager.LoadScene(0); }

    public void QuitGame() { Application.Quit(); }

    private void WireHoverBlips()
    {
        AddHoverBlip(continueButton);
        AddHoverBlip(newGameButton);
        AddHoverBlip(loadCityButton);
        AddHoverBlip(optionsButton);
    }
}
}
