using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Territory.Economy;
using Territory.Zones;

namespace Territory.UI
{
    /// <summary>Family that drives <see cref="SubtypePickerController"/> row enumeration. R/C/I = density tiers; StateService = catalog rows; Stage 9.8 adds Roads/Forests/Power/Water.</summary>
    public enum ToolFamily
    {
        Residential,
        Commercial,
        Industrial,
        StateService,
        Roads,
        Forests,
        Power,
        Water
    }

    /// <summary>
    /// TECH-15891 (Stage 9.7): catalog-driven subtype picker.
    /// Panel shape sourced from <c>subtype_picker</c> asset-registry panel row.
    /// Tile geometry sourced from <c>picker_tile_72</c> archetype row.
    /// Tile sprite sourced from sprite-catalog slug <c>picker-{family}-{tier}-icon-72</c>.
    /// Drops the legacy <c>ResolveZoningSprite</c> SpriteRenderer-yank pattern.
    /// Behavior parity for R/C/I/S.
    /// </summary>
    public class SubtypePickerController : MonoBehaviour
    {
        [SerializeField] private ZoneSubTypeRegistry registry;
        [SerializeField] private UiTheme uiTheme;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform rowContainer;

        /// <summary>UI asset catalog — Inspector first; Awake falls back to FindObjectOfType (invariant #4).</summary>
        [SerializeField] private UiAssetCatalog uiAssetCatalog;

        /// <summary>Stage 9.8 (TECH-15897) — contributor registry for Roads/Forests/Power/Water families. Inspector first; Awake falls back (invariant #4).</summary>
        [SerializeField] private ContributorArchetypeRegistry contributorArchetypeRegistry;

        /// <summary>Stage 9.8 fix-in-place — scene zone prefab source for R/C/I picker tile sprite. Awake fallback to FindObjectOfType.</summary>
        [SerializeField] private ZoneManager zoneManager;

        [Header("SFX — TECH-15225")]
        [SerializeField] private AudioClip sfxPanelOpen;
        [SerializeField] private AudioClip sfxPanelClose;
        [SerializeField] private AudioClip sfxPickerConfirm;

        // DS-* token audit — TECH-15227: picker surface.
        // Picker panel background + tile selected-state tint use ad-hoc Color values from UiAssetCatalog.
        // Migrate to uiTheme palette entries (ds-surface-elevated, ds-accent-primary) in Stage N token-bake.

        private UIManager uiManager;
        private ToolFamily currentFamily;
        private bool isVisible;
        private bool uiBuilt;

        /// <summary>Stage 9.8 (TECH-15898) — smoke test readable picker visibility state.</summary>
        public bool IsPickerVisible => isVisible;
        private readonly List<GameObject> spawnedRows = new List<GameObject>();
        private readonly List<int> spawnedRowKeys = new List<int>();
        private readonly List<Action> spawnedRowActions = new List<Action>();
        private int selectedKey = int.MinValue;

        private void Awake()
        {
            if (registry == null)
                registry = FindObjectOfType<ZoneSubTypeRegistry>();
            if (uiAssetCatalog == null)
                uiAssetCatalog = FindObjectOfType<UiAssetCatalog>();
            if (uiAssetCatalog == null)
            {
                // Stage 9.7 missed scene-wiring; lazy-spawn host so picker panel/archetype defaults
                // (matching 0080 seed) become available without a scene edit.
                var go = new GameObject("UiAssetCatalog");
                uiAssetCatalog = go.AddComponent<UiAssetCatalog>();
            }
            if (contributorArchetypeRegistry == null)
                contributorArchetypeRegistry = FindObjectOfType<ContributorArchetypeRegistry>();
            if (zoneManager == null)
                zoneManager = FindObjectOfType<ZoneManager>();
            EnsureRuntimePanelRootIfNeeded();
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        /// <summary>
        /// Open picker for given family. Horizontal strip of icon tiles; selection commits via UIManager
        /// and closes (R/C/I) or persists for StateService catalog rows. Cancel (ESC) routes through
        /// PopupStack → <see cref="Hide"/>(cancelled:true).
        /// </summary>
        public void Show(UIManager caller, ToolFamily family, int defaultKey = int.MinValue)
        {
            uiManager = caller;
            currentFamily = family;
            selectedKey = defaultKey;
            EnsureUiBuilt();
            if (panelRoot == null) return;
            ClearRows();
            BuildRows(family);
            panelRoot.SetActive(true);
            isVisible = true;
            UiSfxPlayer.Play(sfxPanelOpen);
            // Auto-select first tile so cursor prefab attaches + tool ready to place default subtype.
            // Caller-provided defaultKey wins if it matches a built row; else fall back to first key.
            if (spawnedRowKeys.Count > 0)
            {
                int idx = 0;
                if (selectedKey != int.MinValue)
                {
                    int found = spawnedRowKeys.IndexOf(selectedKey);
                    if (found >= 0) idx = found;
                }
                selectedKey = spawnedRowKeys[idx];
                spawnedRowActions[idx]?.Invoke();
                RefreshSelectionVisuals();
            }
        }

        /// <summary>Close picker. Cancelled = ESC / outside-click → reset to Grass tool.</summary>
        public void Hide(bool cancelled)
        {
            if (!isVisible) return;
            isVisible = false;
            if (panelRoot != null)
                panelRoot.SetActive(false);
            UiSfxPlayer.Play(sfxPanelClose);
            if (cancelled && uiManager != null)
            {
                uiManager.SetCurrentSubTypeId(-1);
                uiManager.OnGrassButtonClicked();
            }
        }

        private void EnsureRuntimePanelRootIfNeeded()
        {
            if (panelRoot != null) return;
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            // Panel shape from catalog (subtype_picker panel row). Bail when catalog absent — no hardcode.
            UiPanelDef panelDef = null;
            bool hasCatalog = uiAssetCatalog != null && uiAssetCatalog.TryGetPanel("subtype_picker", out panelDef);

            if (!hasCatalog || panelDef == null)
            {
                Debug.LogError("[SubtypePickerController] subtype_picker panel row not found in UiAssetCatalog. Panel build aborted.");
                return;
            }

            GameObject root = new GameObject("SubtypePickerRoot");
            root.transform.SetParent(canvas.transform, false);
            root.transform.SetAsLastSibling();
            RectTransform rt = root.AddComponent<RectTransform>();
            rt.anchorMin = panelDef.anchorMin;
            rt.anchorMax = panelDef.anchorMax;
            rt.pivot = panelDef.pivot;
            rt.sizeDelta = panelDef.sizeDelta;
            rt.anchoredPosition = new Vector2(-50f, 24f);
            var bg = root.AddComponent<Image>();
            bg.color = uiTheme != null ? uiTheme.SurfaceCardHud : new Color(0.08f, 0.08f, 0.1f, 0.96f);

            var hlg = root.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = panelDef.spacing;
            // padding: x=left, y=right, z=top, w=bottom
            hlg.padding = new RectOffset(
                Mathf.RoundToInt(panelDef.padding.x),
                Mathf.RoundToInt(panelDef.padding.y),
                Mathf.RoundToInt(panelDef.padding.z),
                Mathf.RoundToInt(panelDef.padding.w));
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var fitter = root.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            panelRoot = root;
            rowContainer = root.transform;
        }

        private void EnsureUiBuilt()
        {
            if (uiBuilt) return;
            uiBuilt = true;
            if (rowContainer == null && panelRoot != null)
                rowContainer = panelRoot.transform;
        }

        private void BuildRows(ToolFamily family)
        {
            if (rowContainer == null) return;
            switch (family)
            {
                case ToolFamily.Residential:
                    AddZoningTile((int)Zone.ZoneType.ResidentialLightZoning, "Light", "residential", "light", () => uiManager.OnLightResidentialButtonClicked());
                    AddZoningTile((int)Zone.ZoneType.ResidentialMediumZoning, "Medium", "residential", "medium", () => uiManager.OnMediumResidentialButtonClicked());
                    AddZoningTile((int)Zone.ZoneType.ResidentialHeavyZoning, "Heavy", "residential", "heavy", () => uiManager.OnHeavyResidentialButtonClicked());
                    break;
                case ToolFamily.Commercial:
                    AddZoningTile((int)Zone.ZoneType.CommercialLightZoning, "Light", "commercial", "light", () => uiManager.OnLightCommercialButtonClicked());
                    AddZoningTile((int)Zone.ZoneType.CommercialMediumZoning, "Medium", "commercial", "medium", () => uiManager.OnMediumCommercialButtonClicked());
                    AddZoningTile((int)Zone.ZoneType.CommercialHeavyZoning, "Heavy", "commercial", "heavy", () => uiManager.OnHeavyCommercialButtonClicked());
                    break;
                case ToolFamily.Industrial:
                    AddZoningTile((int)Zone.ZoneType.IndustrialLightZoning, "Light", "industrial", "light", () => uiManager.OnLightIndustrialButtonClicked());
                    AddZoningTile((int)Zone.ZoneType.IndustrialMediumZoning, "Medium", "industrial", "medium", () => uiManager.OnMediumIndustrialButtonClicked());
                    AddZoningTile((int)Zone.ZoneType.IndustrialHeavyZoning, "Heavy", "industrial", "heavy", () => uiManager.OnHeavyIndustrialButtonClicked());
                    break;
                case ToolFamily.StateService:
                    BuildStateServiceRows();
                    break;
                // Stage 9.8 — contributor-registry-driven families.
                case ToolFamily.Roads:
                case ToolFamily.Forests:
                case ToolFamily.Power:
                case ToolFamily.Water:
                    BuildContributorRows(family);
                    break;
            }
        }

        /// <summary>
        /// Stage 9.8 fix-in-place — scene-prefab-driven family rows.
        /// Picker tile sprite = SpriteRenderer.sprite from same prefab the player will place.
        /// Bypasses broken contributor-json prefabPath="Buildings/*" (those prefabs live in Assets/Prefabs/, not Resources/).
        /// </summary>
        private void BuildContributorRows(ToolFamily family)
        {
            if (uiManager == null) return;
            switch (family)
            {
                case ToolFamily.Power:
                    AddPrefabTile(0, "Power Plant", uiManager.powerPlantAPrefab, () => uiManager.OnNuclearPowerPlantButtonClicked());
                    break;
                case ToolFamily.Water:
                    AddPrefabTile(0, "Water Pump", uiManager.waterPumpPrefab, () => uiManager.OnMediumWaterPumpPlantButtonClicked());
                    break;
                case ToolFamily.Forests:
                    AddPrefabTile(0, "Sparse",  uiManager.sparseForestPrefab, () => uiManager.OnSparseForestButtonClicked());
                    AddPrefabTile(1, "Medium",  uiManager.mediumForestPrefab, () => uiManager.OnMediumForestButtonClicked());
                    AddPrefabTile(2, "Dense",   uiManager.denseForestPrefab,  () => uiManager.OnDenseForestButtonClicked());
                    break;
                case ToolFamily.Roads:
                {
                    GameObject roadPrefab = null;
                    if (uiManager.gridManager != null && uiManager.gridManager.roadManager != null)
                        roadPrefab = uiManager.gridManager.roadManager.roadTilePrefab1;
                    AddPrefabTile(0, "Two-Way", roadPrefab, () => uiManager.OnTwoWayRoadButtonClicked());
                    break;
                }
            }
        }

        /// <summary>Tile sprite ripped from prefab's SpriteRenderer (same sprite the player will place on the grid).</summary>
        private void AddPrefabTile(int key, string label, GameObject prefab, Action onClick)
        {
            Sprite sprite = GetPrefabSprite(prefab);
            AddIconTile(key, label, sprite, onClick);
        }

        private static Sprite GetPrefabSprite(GameObject prefab)
        {
            if (prefab == null) return null;
            var sr = prefab.GetComponent<SpriteRenderer>();
            if (sr == null) sr = prefab.GetComponentInChildren<SpriteRenderer>(true);
            return sr != null ? sr.sprite : null;
        }

        private void BuildStateServiceRows()
        {
            if (registry == null) return;
            var entries = registry.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                int subTypeId = entries[i].id;
                string label;
                if (registry.TryGetPickerLabelForSubType(subTypeId, out string line, out _))
                    label = line;
                else
                    label = entries[i].displayName;
                // Sprite ripped from prefab — same building the player will place.
                Sprite icon = GetPrefabSprite(entries[i].prefab);
                AddIconTile(subTypeId, label, icon, () => OnStateServiceRowSelected(subTypeId));
            }
        }

        private void OnStateServiceRowSelected(int subTypeId)
        {
            if (uiManager != null)
                uiManager.SetCurrentSubTypeId(subTypeId);
            selectedKey = subTypeId;
            RefreshSelectionVisuals();
        }

        /// <summary>
        /// Stage 9.8 fix-in-place — zoning tile uses real zone prefab sprite.
        /// Sprite source = same prefab <see cref="ZoneManager.GetRandomZonePrefab"/> picks at placement time.
        /// </summary>
        private void AddZoningTile(int key, string label, string family, string tier, Action onClick)
        {
            Zone.ZoneType zt = (Zone.ZoneType)key;
            GameObject prefab = (zoneManager != null) ? zoneManager.GetRandomZonePrefab(zt, 1) : null;
            Sprite sprite = GetPrefabSprite(prefab);
            AddIconTile(key, label, sprite, onClick);
        }

        private void AddIconTile(int key, string label, Sprite icon, Action onClick)
        {
            // Tile geometry from picker_tile_72 archetype row.
            UiArchetypeDef arch = null;
            bool hasArch = uiAssetCatalog != null && uiAssetCatalog.TryGetArchetype("picker_tile_72", out arch);

            float tileW = hasArch ? arch.tileWidth  : 72f;
            float tileH = hasArch ? arch.tileHeight : 72f;
            Vector2 iconMin = hasArch ? arch.iconOffsetMin : new Vector2(6f, 18f);
            Vector2 iconMax = hasArch ? arch.iconOffsetMax : new Vector2(-6f, -6f);
            float captH   = hasArch ? arch.captionHeight : 12f;
            string hover  = hasArch ? arch.motionHover  : "tint";

            GameObject tile = new GameObject($"PickerTile_{spawnedRows.Count}", typeof(RectTransform), typeof(Button), typeof(Image));
            tile.transform.SetParent(rowContainer, false);
            var img = tile.GetComponent<Image>();
            Color baseColor = uiTheme != null ? uiTheme.SurfaceElevated : new Color(0.16f, 0.16f, 0.2f, 1f);
            img.color = baseColor;

            var le = tile.AddComponent<LayoutElement>();
            le.preferredWidth  = tileW;
            le.preferredHeight = tileH;
            le.minWidth  = tileW;
            le.minHeight = tileH;
            le.flexibleWidth  = 0;
            le.flexibleHeight = 0;

            // Selection outline — toggled by RefreshSelectionVisuals.
            var outline = tile.AddComponent<Outline>();
            outline.effectColor = uiTheme != null ? uiTheme.AccentPrimary : new Color(0.29f, 0.62f, 1f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.enabled = (key == selectedKey);

            // Hover branch — motion.hover enum from archetype.
            var btn = tile.GetComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.normalColor   = baseColor;
            cb.pressedColor  = LerpToward(baseColor, Color.black, 0.12f);
            cb.selectedColor = baseColor;
            cb.disabledColor = baseColor;
            cb.colorMultiplier = 1f;
            cb.fadeDuration  = 0.1f;
            switch (hover)
            {
                case "tint":
                    cb.highlightedColor = LerpToward(baseColor, Color.white, 0.18f);
                    break;
                case "glow":
                    throw new NotImplementedException("[SubtypePickerController] motion.hover='glow' not yet implemented.");
                case "scale":
                    throw new NotImplementedException("[SubtypePickerController] motion.hover='scale' not yet implemented.");
                default:
                    cb.highlightedColor = LerpToward(baseColor, Color.white, 0.18f);
                    break;
            }
            btn.colors = cb;

            // Icon centered — offsets from archetype.
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(tile.transform, false);
            var irt = iconObj.GetComponent<RectTransform>();
            irt.anchorMin = Vector2.zero;
            irt.anchorMax = Vector2.one;
            irt.offsetMin = iconMin;
            irt.offsetMax = iconMax;
            var iconImg = iconObj.GetComponent<Image>();
            iconImg.sprite = icon;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            if (icon == null) iconImg.color = new Color(1f, 1f, 1f, 0.15f);

            // Caption — strip at bottom; height from archetype.
            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObj.transform.SetParent(tile.transform, false);
            var lrt = labelObj.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0f);
            lrt.anchorMax = new Vector2(1f, 0f);
            lrt.pivot = new Vector2(0.5f, 0f);
            lrt.offsetMin = new Vector2(2f, 2f);
            lrt.offsetMax = new Vector2(-2f, 2f + captH);
            var t = labelObj.GetComponent<Text>();
            t.text = label;
            t.fontSize = 10;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = uiTheme != null ? uiTheme.TextPrimary : Color.white;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null) t.font = font;

            int capturedKey = key;
            btn.onClick.AddListener(() =>
            {
                UiSfxPlayer.Play(sfxPickerConfirm);
                onClick?.Invoke();
                selectedKey = capturedKey;
                if (currentFamily == ToolFamily.StateService)
                    RefreshSelectionVisuals();
                else
                    Hide(cancelled: false);
            });

            spawnedRows.Add(tile);
            spawnedRowKeys.Add(key);
            spawnedRowActions.Add(onClick);
        }

        private static Color LerpToward(Color from, Color target, float t)
        {
            return new Color(
                Mathf.Lerp(from.r, target.r, t),
                Mathf.Lerp(from.g, target.g, t),
                Mathf.Lerp(from.b, target.b, t),
                from.a);
        }

        private void RefreshSelectionVisuals()
        {
            for (int i = 0; i < spawnedRows.Count; i++)
            {
                if (spawnedRows[i] == null) continue;
                var outline = spawnedRows[i].GetComponent<Outline>();
                if (outline != null)
                    outline.enabled = (spawnedRowKeys[i] == selectedKey);
            }
        }

        private void ClearRows()
        {
            for (int i = 0; i < spawnedRows.Count; i++)
            {
                if (spawnedRows[i] != null)
                    Destroy(spawnedRows[i]);
            }
            spawnedRows.Clear();
            spawnedRowKeys.Clear();
            spawnedRowActions.Clear();
        }
    }
}
