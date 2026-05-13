// long-file-allowed: legacy hub — scope outside current atomization plan; deferred to future sweep
using UnityEngine;
using UnityEngine.UI;

namespace Territory.Economy
{
    /// <summary>
    /// HUD-accessible budget panel: seven envelope percentage sliders (one per Zone S sub-type),
    /// and remaining-this-month readouts.
    /// Commits via <see cref="BudgetAllocationService.SetEnvelopePct"/>; UI re-reads
    /// normalized values after commit so sliders reflect stored state.
    /// </summary>
    public class BudgetPanel : MonoBehaviour
    {
        [SerializeField] private BudgetAllocationService budgetAllocation;
        [SerializeField] private ZoneSubTypeRegistry registry;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform sliderContainer;
        // BUG-61 W10 — palette tokens for chrome (panel bg / label text / slider bg+fill).
        // ScriptableObject ref; assign UiTheme asset in Inspector. Literal fallback colors
        // preserved when ref is null so runtime-instantiated panels still render legibly.
        [SerializeField] private Territory.UI.UiTheme uiTheme;

        private Slider[] envelopeSliders;
        private Text[] remainingLabels;
        private Text[] pctLabels;
        private bool isVisible;
        private bool suppressCallbacks;
        private bool uiBuilt;

        private const int SubTypeCount = 7;

        private void Awake()
        {
            if (budgetAllocation == null)
                budgetAllocation = FindObjectOfType<BudgetAllocationService>();
            if (registry == null)
                registry = FindObjectOfType<ZoneSubTypeRegistry>();
            EnsureRuntimePanelRootIfNeeded();
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        /// <summary>
        /// When no prefab assigns <see cref="panelRoot"/>, build a centered panel under the main Canvas
        /// so HUD / code-only setups still open the budget UI.
        /// </summary>
        private void EnsureRuntimePanelRootIfNeeded()
        {
            if (panelRoot != null)
                return;
            Canvas c = FindObjectOfType<Canvas>();
            if (c == null)
                return;
            GameObject root = new GameObject("BudgetPanelRoot");
            // Parent under this component (runtime bootstrap places BudgetPanel under Canvas).
            root.transform.SetParent(transform, false);
            root.transform.SetAsLastSibling();
            RectTransform rt = root.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(520, 640);
            rt.anchoredPosition = Vector2.zero;
            var img = root.AddComponent<Image>();
            img.color = uiTheme != null ? uiTheme.SurfaceCardHud : new Color(0.08f, 0.08f, 0.1f, 0.96f);
            var v = root.AddComponent<VerticalLayoutGroup>();
            v.spacing = 6;
            v.padding = new RectOffset(12, 12, 12, 12);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = false;
            v.childForceExpandWidth = true;
            panelRoot = root;
        }

        /// <summary>Open budget panel; refresh from allocator state.</summary>
        public void Show()
        {
            if (panelRoot == null) return;
            EnsureUiBuilt();
            panelRoot.SetActive(true);
            isVisible = true;
            RefreshFromModel();
        }

        /// <summary>Close budget panel.</summary>
        public void Hide()
        {
            if (panelRoot == null) return;
            panelRoot.SetActive(false);
            isVisible = false;
        }

        private void EnsureUiBuilt()
        {
            if (uiBuilt) return;
            uiBuilt = true;

            if (sliderContainer == null && panelRoot != null)
                sliderContainer = panelRoot.transform;

            envelopeSliders = new Slider[SubTypeCount];
            remainingLabels = new Text[SubTypeCount];
            pctLabels = new Text[SubTypeCount];

            var entries = registry != null ? registry.Entries : null;
            // BUG-61 W5 — glossary-aligned fallback labels when ZoneSubTypeRegistry asset
            // is missing or under-populated. Order matches `ia/specs/glossary.md`
            // ZoneSubTypeRegistry row: police, fire, education, health, parks,
            // public housing, public offices.
            string[] fallbackLabels = { "Police", "Fire", "Education", "Health", "Parks", "Public Housing", "Public Offices" };

            for (int i = 0; i < SubTypeCount; i++)
            {
                string label = entries != null && i < entries.Count
                    ? entries[i].displayName
                    : (i < fallbackLabels.Length ? fallbackLabels[i] : $"Sub-type {i}");
                int capturedId = i;

                GameObject row = CreateSliderRow(label, capturedId);
                row.transform.SetParent(sliderContainer, false);
            }
        }

        private GameObject CreateSliderRow(string label, int subTypeId)
        {
            GameObject row = new GameObject($"EnvelopeRow_{subTypeId}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.spacing = 8;

            GameObject labelObj = CreateLabel(label, row.transform);
            GameObject sliderObj = CreateSlider(row.transform, 0f, 1f, (val) => OnEnvelopeSliderChanged(subTypeId, val));
            envelopeSliders[subTypeId] = sliderObj.GetComponent<Slider>();

            GameObject pctObj = CreateLabel("0%", row.transform);
            pctLabels[subTypeId] = pctObj.GetComponent<Text>();

            GameObject remainObj = CreateLabel("$0", row.transform);
            remainingLabels[subTypeId] = remainObj.GetComponent<Text>();

            return row;
        }

        private void OnEnvelopeSliderChanged(int subTypeId, float value)
        {
            if (suppressCallbacks || budgetAllocation == null) return;
            budgetAllocation.SetEnvelopePct(subTypeId, value);
            RefreshFromModel();
        }

        private void RefreshFromModel()
        {
            if (budgetAllocation == null) return;
            suppressCallbacks = true;

            for (int i = 0; i < SubTypeCount; i++)
            {
                float pct = budgetAllocation.GetEnvelopePct(i);
                int remaining = budgetAllocation.GetRemaining(i);

                if (envelopeSliders[i] != null)
                    envelopeSliders[i].SetValueWithoutNotify(pct);
                if (pctLabels[i] != null)
                    pctLabels[i].text = $"{pct * 100f:F0}%";
                if (remainingLabels[i] != null)
                    remainingLabels[i].text = $"${remaining:N0}";
            }

            suppressCallbacks = false;
        }

        // BUG-61 W10 — instance method (was static) so chrome label color reads UiTheme.TextPrimary.
        private GameObject CreateLabel(string text, Transform parent)
        {
            GameObject obj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var t = obj.GetComponent<Text>();
            t.text = text;
            t.fontSize = 14;
            t.color = uiTheme != null ? uiTheme.TextPrimary : Color.white;
            t.raycastTarget = false;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null) t.font = font;
            var le = obj.AddComponent<LayoutElement>();
            le.minWidth = 80;
            return obj;
        }

        // BUG-61 W10 — instance method (was static) so slider chrome reads UiTheme tokens
        // (SurfaceElevated for track, AccentPrimary for fill).
        private GameObject CreateSlider(Transform parent, float min, float max, UnityEngine.Events.UnityAction<float> onChanged)
        {
            GameObject obj = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            obj.transform.SetParent(parent, false);

            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(obj.transform, false);
            bg.GetComponent<Image>().color = uiTheme != null ? uiTheme.SurfaceElevated : new Color(0.2f, 0.2f, 0.2f, 1f);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(obj.transform, false);
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            fill.GetComponent<Image>().color = uiTheme != null ? uiTheme.AccentPrimary : new Color(0.3f, 0.7f, 1f, 1f);
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;

            var slider = obj.GetComponent<Slider>();
            slider.fillRect = fillRt;
            slider.minValue = min;
            slider.maxValue = max;
            slider.onValueChanged.AddListener(onChanged);

            var le = obj.AddComponent<LayoutElement>();
            le.minWidth = 120;
            le.flexibleWidth = 1;

            return obj;
        }
    }
}
