using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Territory.Audio;
using Territory.Persistence;

namespace Territory.UI
{
/// <summary>
/// Main menu UI: Continue, New Game, Load City, Options. Scene transition → MainScene
/// with appropriate <see cref="GameStartInfo"/>.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    private const string LastSavePathKey = "LastSavePath";
    private const int MainSceneBuildIndex = 1;

    [Header("Optional: assign in Inspector to use pre-built UI")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadCityButton;
    [SerializeField] private Button optionsButton;
    [Header("Theme (optional)")]
    [SerializeField] private UiTheme menuTheme;
    [SerializeField] private GameObject loadCityPanel;
    [SerializeField] private Transform savedGamesListContainer;
    [SerializeField] private GameObject savedGameButtonPrefab;
    [SerializeField] private Button loadCityBackButton;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Button optionsBackButton;
    private BlipVolumeController _volumeController;
    private GameObject _menuStripRoot;

    private string saveFolderPath;

    void Start()
    {
        saveFolderPath = Application.persistentDataPath;

        if (continueButton == null)
            BuildUI();
        else
        {
            EnsureSerializedMenuPanels();
            WireExistingUI();
        }

        ApplyMenuThemeIfAny();
        ApplyMenuOverlayPanelsFromTheme();
        UpdateContinueButtonState();
        WireHoverBlips();
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

    /// <summary>
    /// Tints load-city + options overlay roots when <see cref="UiTheme"/> assigned (modal dimmer + card surface).
    /// </summary>
    private void ApplyMenuOverlayPanelsFromTheme()
    {
        if (menuTheme == null)
            return;
        ApplyOverlayToPanelRoot(loadCityPanel);
        ApplyOverlayToPanelRoot(optionsPanel);
    }

    private void ApplyOverlayToPanelRoot(GameObject panelRoot)
    {
        if (panelRoot == null)
            return;
        var rootImage = panelRoot.GetComponent<Image>();
        if (rootImage != null)
            rootImage.color = menuTheme.ModalDimmerColor;
        foreach (Transform child in panelRoot.transform)
        {
            var img = child.GetComponent<Image>();
            if (img == null)
                continue;
            img.color = menuTheme.SurfaceCardHud;
            break;
        }
    }

    private void EnsureSerializedMenuPanels()
    {
        Canvas canvas = continueButton != null ? continueButton.GetComponentInParent<Canvas>() : null;
        if (canvas == null)
            return;

        Transform canvasTransform = canvas.transform;
        if (loadCityPanel == null)
            loadCityPanel = CreateLoadCityPanel(canvasTransform);
        if (optionsPanel == null)
            optionsPanel = CreateOptionsPanel(canvasTransform);

        // Detect the menu strip container — parent of the buttons unless it is the Canvas itself.
        if (continueButton != null)
        {
            var parent = continueButton.transform.parent?.gameObject;
            if (parent != null && parent.GetComponent<Canvas>() == null)
                _menuStripRoot = parent;
        }

        if (loadCityPanel != null)
            loadCityPanel.SetActive(false);
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
    }

    private void WireExistingUI()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
        if (newGameButton != null)
            newGameButton.onClick.AddListener(OnNewGameClicked);
        if (loadCityButton != null)
            loadCityButton.onClick.AddListener(OnLoadCityClicked);
        if (optionsButton != null)
            optionsButton.onClick.AddListener(OnOptionsClicked);
        if (loadCityBackButton != null)
            loadCityBackButton.onClick.AddListener(CloseLoadCityPanel);
        if (optionsBackButton != null)
            optionsBackButton.onClick.AddListener(CloseOptionsPanel);

        if (loadCityPanel != null)
            loadCityPanel.SetActive(false);
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
    }

    private void BuildUI()
    {
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        var root = new GameObject("MainMenuRoot");
        root.transform.SetParent(canvasObj.transform, false);
        _menuStripRoot = root;
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;

        float buttonWidth = 200f;
        float buttonHeight = 40f;
        float spacing = 10f;
        float startY = 80f;

        continueButton = CreateButton(root.transform, "Continue", new Vector2(0, startY), buttonWidth, buttonHeight);
        continueButton.onClick.AddListener(OnContinueClicked);

        newGameButton = CreateButton(root.transform, "New Game", new Vector2(0, startY - (buttonHeight + spacing)), buttonWidth, buttonHeight);
        newGameButton.onClick.AddListener(OnNewGameClicked);

        loadCityButton = CreateButton(root.transform, "Load City", new Vector2(0, startY - 2 * (buttonHeight + spacing)), buttonWidth, buttonHeight);
        loadCityButton.onClick.AddListener(OnLoadCityClicked);

        optionsButton = CreateButton(root.transform, "Options", new Vector2(0, startY - 3 * (buttonHeight + spacing)), buttonWidth, buttonHeight);
        optionsButton.onClick.AddListener(OnOptionsClicked);

        loadCityPanel = CreateLoadCityPanel(canvasObj.transform);
        optionsPanel = CreateOptionsPanel(canvasObj.transform);

        if (GameObject.Find("EventSystem") == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        ApplyThemeToMenuStrip(continueButton, newGameButton, loadCityButton, optionsButton);
        ApplyMenuOverlayPanelsFromTheme();
    }

    private Button CreateButton(Transform parent, string label, Vector2 pos, float w, float h)
    {
        var go = new GameObject(label + "Button");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(w, h);

        var image = go.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        var text = textGo.AddComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 18;
        text.color = Color.white;

        return button;
    }

    private GameObject CreateLoadCityPanel(Transform canvasTransform)
    {
        var panel = new GameObject("LoadCityPanel");
        panel.transform.SetParent(canvasTransform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0, 0, 0, 0.7f);

        var content = new GameObject("Content");
        content.transform.SetParent(panel.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(450, 450);
        contentRect.anchoredPosition = Vector2.zero;

        var contentBg = content.AddComponent<Image>();
        contentBg.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

        var scrollView = new GameObject("ScrollView");
        scrollView.transform.SetParent(content.transform, false);
        var scrollRect = scrollView.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.offsetMin = new Vector2(15, 50);
        scrollRect.offsetMax = new Vector2(-15, -55);

        var scroll = scrollView.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);
        var viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = viewportRect.offsetMax = Vector2.zero;
        viewport.AddComponent<Image>().color = new Color(1, 1, 1, 1);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        var listContent = new GameObject("ListContent");
        listContent.transform.SetParent(viewport.transform, false);
        var listContentRect = listContent.AddComponent<RectTransform>();
        listContentRect.anchorMin = new Vector2(0, 1);
        listContentRect.anchorMax = Vector2.one;
        listContentRect.pivot = new Vector2(0.5f, 1f);
        listContentRect.offsetMin = Vector2.zero;
        listContentRect.offsetMax = Vector2.zero;

        var layoutGroup = listContent.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 5;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.padding = new RectOffset(5, 5, 5, 5);

        var contentSizeFitter = listContent.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scroll.content = listContentRect;
        scroll.viewport = viewportRect;

        savedGamesListContainer = listContent.transform;

        loadCityBackButton = CreateButton(content.transform, "Back", new Vector2(0, -210), 120, 35);
        loadCityBackButton.onClick.AddListener(CloseLoadCityPanel);

        panel.SetActive(false);
        return panel;
    }

    private GameObject CreateOptionsPanel(Transform canvasTransform)
    {
        // Full-screen transparent wrapper — navigate approach, no dimmer needed.
        var panel = new GameObject("OptionsPanel");
        panel.transform.SetParent(canvasTransform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;

        // Card: 1/3 screen width via anchors, vertically centered, 220px tall.
        var card = new GameObject("Card");
        card.transform.SetParent(panel.transform, false);
        var cardRect = card.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(1f / 3f, 0.5f);
        cardRect.anchorMax = new Vector2(2f / 3f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.offsetMin = new Vector2(0f, -110f);
        cardRect.offsetMax = new Vector2(0f, 110f);
        var cardImg = card.AddComponent<Image>();
        cardImg.color = menuTheme != null ? menuTheme.SurfaceCardHud : new Color(0.12f, 0.16f, 0.24f, 0.97f);
        cardImg.sprite = CreateRoundedRectSprite(64, 64, 10);
        cardImg.type = Image.Type.Sliced;

        // Title
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(card.transform, false);
        var titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 72f);
        titleRect.sizeDelta = new Vector2(180f, 30f);
        var titleText = titleGo.AddComponent<Text>();
        titleText.text = "Options";
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 22;
        titleText.color = Color.white;

        // SFX Volume row
        CreateRowLabel(card.transform, "SFX Volume", posX: -68f, posY: 25f);
        var sfxSlider = CreateSliderWithVisuals(card.transform, posX: 42f, posY: 25f);

        // Mute SFX row
        CreateRowLabel(card.transform, "Mute SFX", posX: -68f, posY: -18f);
        var sfxToggle = CreateToggleWithVisuals(card.transform, posX: 2f, posY: -18f);

        // Back button — same factory as main menu buttons so style matches.
        optionsBackButton = CreateButton(card.transform, "Back", new Vector2(0f, -70f), 110f, 34f);
        optionsBackButton.onClick.AddListener(CloseOptionsPanel);

        var controller = panel.AddComponent<BlipVolumeController>();
        controller.Bind(sfxSlider, sfxToggle);
        controller.InitListeners();
        _volumeController = controller;

        panel.SetActive(false);
        return panel;
    }

    // -------------------------------------------------------------------------
    // Options panel helpers
    // -------------------------------------------------------------------------

    private void CreateRowLabel(Transform parent, string text, float posX, float posY)
    {
        var go = new GameObject(text.Replace(" ", "") + "Label");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(posX, posY);
        rect.sizeDelta = new Vector2(100f, 24f);
        var txt = go.AddComponent<Text>();
        txt.text = text;
        txt.alignment = TextAnchor.MiddleRight;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 14;
        txt.color = new Color(0.85f, 0.85f, 0.85f, 1f);
    }

    private Slider CreateSliderWithVisuals(Transform parent, float posX, float posY)
    {
        var go = new GameObject("SfxVolumeSlider");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(posX, posY);
        rect.sizeDelta = new Vector2(130f, 20f);

        var slider = go.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        // Track
        var bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        var bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.25f);
        bgRect.anchorMax = new Vector2(1f, 0.75f);
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.22f, 0.22f, 0.32f, 1f);

        // Fill area + fill
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        var faRect = fillArea.AddComponent<RectTransform>();
        faRect.anchorMin = new Vector2(0f, 0.25f);
        faRect.anchorMax = new Vector2(1f, 0.75f);
        faRect.offsetMin = new Vector2(5f, 0f);
        faRect.offsetMax = new Vector2(-5f, 0f);
        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
        fill.AddComponent<Image>().color = new Color(0.35f, 0.55f, 0.95f, 1f);
        slider.fillRect = fillRect;

        // Handle area + handle
        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(go.transform, false);
        var haRect = handleArea.AddComponent<RectTransform>();
        haRect.anchorMin = Vector2.zero;
        haRect.anchorMax = Vector2.one;
        haRect.offsetMin = new Vector2(10f, 0f);
        haRect.offsetMax = new Vector2(-10f, 0f);
        var handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        var handleRect = handle.AddComponent<RectTransform>();
        handleRect.anchorMin = handleRect.anchorMax = new Vector2(0f, 0.5f);
        handleRect.sizeDelta = new Vector2(20f, 20f);
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;

        // Blip on drag start
        var et = go.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        entry.callback.AddListener(_ => BlipEngine.Play(BlipId.UiButtonHover));
        et.triggers.Add(entry);

        return slider;
    }

    private Toggle CreateToggleWithVisuals(Transform parent, float posX, float posY)
    {
        var go = new GameObject("SfxMuteToggle");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(posX, posY);
        rect.sizeDelta = new Vector2(24f, 24f);

        var toggle = go.AddComponent<Toggle>();

        // Box background
        var bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        var bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.22f, 0.22f, 0.32f, 1f);
        toggle.targetGraphic = bgImg;

        // Checkmark
        var check = new GameObject("Checkmark");
        check.transform.SetParent(bg.transform, false);
        var checkRect = check.AddComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.15f, 0.15f);
        checkRect.anchorMax = new Vector2(0.85f, 0.85f);
        checkRect.offsetMin = checkRect.offsetMax = Vector2.zero;
        var checkImg = check.AddComponent<Image>();
        checkImg.color = new Color(0.35f, 0.55f, 0.95f, 1f);
        toggle.graphic = checkImg;

        // Blip on click
        toggle.onValueChanged.AddListener(_ => BlipEngine.Play(BlipId.UiButtonClick));

        return toggle;
    }

    /// <summary>Generates a small 9-sliceable rounded-rectangle sprite at runtime.</summary>
    private static Sprite CreateRoundedRectSprite(int w, int h, int radius)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float cx = Mathf.Clamp(px, radius, w - radius);
                float cy = Mathf.Clamp(py, radius, h - radius);
                float dx = px - cx, dy = py - cy;
                byte a = (dx * dx + dy * dy) <= (float)(radius * radius) ? (byte)255 : (byte)0;
                pixels[y * w + x] = new Color32(255, 255, 255, a);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        float r = radius;
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
    }

    private void UpdateContinueButtonState()
    {
        if (continueButton == null) return;
        bool hasValidSave = GetMostRecentSavePath() != null;
        continueButton.interactable = hasValidSave;
    }

    /// <summary>Returns LastSavePath if valid, otherwise the most recent save file in the folder.</summary>
    private string GetMostRecentSavePath()
    {
        string lastPath = PlayerPrefs.GetString(LastSavePathKey, "");
        if (!string.IsNullOrEmpty(lastPath) && File.Exists(lastPath))
            return lastPath;
        var entries = GetSortedSaveEntries();
        return entries.Count > 0 ? entries[0].filePath : null;
    }

    public void OnContinueClicked()
    {
        BlipEngine.Play(BlipId.UiButtonClick);
        string path = GetMostRecentSavePath();
        if (string.IsNullOrEmpty(path))
            return;
        GameStartInfo.SetPendingLoadPath(path);
        SceneManager.LoadScene(MainSceneBuildIndex);
    }

    public void OnNewGameClicked()
    {
        BlipEngine.Play(BlipId.UiButtonClick);
        GameStartInfo.SetStartModeNewGame();
        SceneManager.LoadScene(MainSceneBuildIndex);
    }

    public void OnLoadCityClicked()
    {
        BlipEngine.Play(BlipId.UiButtonClick);
        if (loadCityPanel != null)
        {
            loadCityPanel.SetActive(true);
            PopulateSavedGamesList();
        }
    }

    private void PopulateSavedGamesList()
    {
        if (savedGamesListContainer == null) return;

        foreach (Transform child in savedGamesListContainer)
            Destroy(child.gameObject);

        var entries = GetSortedSaveEntries();
        if (entries.Count == 0)
        {
            var empty = new GameObject("EmptyText");
            empty.transform.SetParent(savedGamesListContainer, false);
            var rect = empty.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 30);
            var le = empty.AddComponent<LayoutElement>();
            le.preferredHeight = 30;
            le.minHeight = 30;
            var text = empty.AddComponent<Text>();
            text.text = "No saved games found.";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.color = Color.gray;
        }
        else
        {
            foreach (var entry in entries)
            {
                Button btn;
                if (savedGameButtonPrefab != null)
                {
                    var go = Instantiate(savedGameButtonPrefab, savedGamesListContainer);
                    btn = go.GetComponent<Button>();
                    if (go.GetComponent<LayoutElement>() == null)
                    {
                        var le = go.AddComponent<LayoutElement>();
                        le.preferredHeight = 35;
                        le.minHeight = 35;
                    }
                    var text = go.GetComponentInChildren<Text>();
                    if (text != null)
                        text.text = entry.displayName;
                }
                else
                {
                    btn = CreateSaveListButton(entry.displayName);
                }
                string path = entry.filePath;
                btn.onClick.AddListener(() => OnSavedGameSelected(path));
            }
        }

        if (savedGamesListContainer is RectTransform rt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    private Button CreateSaveListButton(string label)
    {
        var go = new GameObject("SaveButton");
        go.transform.SetParent(savedGamesListContainer, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(350, 35);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 35;
        le.minHeight = 35;
        var image = go.AddComponent<Image>();
        image.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = image;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(5, 0);
        textRect.offsetMax = new Vector2(-5, 0);
        var text = textGo.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 14;
        text.color = Color.white;
        return btn;
    }

    private List<(string filePath, string displayName)> GetSortedSaveEntries()
    {
        var entries = new List<(string filePath, string displayName)>();
        if (!Directory.Exists(saveFolderPath)) return entries;

        string[] files = Directory.GetFiles(saveFolderPath, "*.json");
        var withDates = new List<(string path, string name, DateTime date)>();

        foreach (string path in files)
        {
            var meta = GameSaveManager.GetSaveMetadata(path);
            withDates.Add((path, meta.displayName, meta.sortDate));
        }

        withDates.Sort((a, b) => b.date.CompareTo(a.date));

        foreach (var t in withDates)
            entries.Add((t.path, t.name));

        return entries;
    }

    private void OnSavedGameSelected(string saveFilePath)
    {
        CloseLoadCityPanel();
        GameStartInfo.SetPendingLoadPath(saveFilePath);
        SceneManager.LoadScene(MainSceneBuildIndex);
    }

    private void CloseLoadCityPanel()
    {
        BlipEngine.Play(BlipId.UiButtonClick);
        if (loadCityPanel != null)
            loadCityPanel.SetActive(false);
    }

    public void OnOptionsClicked()
    {
        BlipEngine.Play(BlipId.UiButtonClick);
        ShowMenuStrip(false);
        if (optionsPanel != null)
            optionsPanel.SetActive(true);
    }

    private void CloseOptionsPanel()
    {
        BlipEngine.Play(BlipId.UiButtonClick);
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
        ShowMenuStrip(true);
    }

    private void ShowMenuStrip(bool show)
    {
        if (_menuStripRoot != null)
        {
            _menuStripRoot.SetActive(show);
            return;
        }
        foreach (var b in new Button[] { continueButton, newGameButton, loadCityButton, optionsButton })
            if (b != null) b.gameObject.SetActive(show);
    }

    // -------------------------------------------------------------------------
    // Hover blip wiring — programmatic EventTrigger, no new fields.
    // Called once in Start() after BuildUI/WireExistingUI populates all buttons.
    // -------------------------------------------------------------------------

    private void AddHoverBlip(Button btn)
    {
        if (btn == null) return;
        var trig = btn.GetComponent<EventTrigger>() ?? btn.gameObject.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback.AddListener(_ => BlipEngine.Play(BlipId.UiButtonHover));
        trig.triggers.Add(entry);
    }

    public void StartNewGame(int mapSize, int seed, int scenarioIndex)
    {
        BlipEngine.Play(BlipId.UiButtonClick);
        Territory.Persistence.GameStartInfo.SetStartModeNewGame(mapSize, seed, scenarioIndex);
        SceneManager.LoadScene(MainSceneBuildIndex);
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
        AddHoverBlip(loadCityBackButton);
        AddHoverBlip(optionsBackButton);
    }
}
}
