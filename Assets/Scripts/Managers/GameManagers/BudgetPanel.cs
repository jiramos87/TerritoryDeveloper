using UnityEngine;
using UnityEngine.UI;
using Territory.UI;

namespace Territory.Economy
{
    /// <summary>
    /// HUD-accessible budget panel: seven envelope percentage sliders (one per Zone S sub-type),
    /// one global cap slider, and remaining-this-month readouts.
    /// Commits via <see cref="BudgetAllocationService.SetEnvelopePct"/>; UI re-reads
    /// normalized values after commit so sliders reflect stored state.
    /// </summary>
    public class BudgetPanel : MonoBehaviour
    {
        [SerializeField] private BudgetAllocationService budgetAllocation;
        [SerializeField] private ZoneSubTypeRegistry registry;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform sliderContainer;

        private Slider[] envelopeSliders;
        private Text[] remainingLabels;
        private Text[] pctLabels;
        private Slider capSlider;
        private Text capLabel;
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
            if (uiManager == null)
                uiManager = FindObjectOfType<UIManager>();
            if (panelRoot != null)
                panelRoot.SetActive(false);
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

            for (int i = 0; i < SubTypeCount; i++)
            {
                string label = entries != null && i < entries.Count ? entries[i].displayName : $"Sub-type {i}";
                int capturedId = i;

                GameObject row = CreateSliderRow(label, capturedId);
                row.transform.SetParent(sliderContainer, false);
            }

            GameObject capRow = CreateCapRow();
            capRow.transform.SetParent(sliderContainer, false);

            GameObject bondRow = new GameObject("BondActionsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            bondRow.transform.SetParent(sliderContainer, false);
            var hb = bondRow.GetComponent<HorizontalLayoutGroup>();
            hb.spacing = 8;
            hb.childAlignment = TextAnchor.MiddleLeft;
            CreateBondButton("Issue bond", bondRow.transform, () =>
            {
                if (uiManager != null)
                    uiManager.OpenBondIssuanceModal();
            });
            CreateBondButton("Bond status", bondRow.transform, () =>
            {
                if (uiManager != null)
                    uiManager.OpenBondDetailModal();
            });
        }

        private void CreateBondButton(string label, Transform parent, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.22f, 0.35f, 0.55f);
            var btn = go.GetComponent<Button>();
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 28;
            le.minWidth = 100;
            GameObject txtGo = new GameObject("Text", typeof(Text));
            txtGo.transform.SetParent(go.transform, false);
            var te = txtGo.GetComponent<Text>();
            te.text = label;
            te.fontSize = 13;
            te.color = Color.white;
            te.alignment = TextAnchor.MiddleCenter;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null) te.font = font;
            var rt = te.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            btn.onClick.AddListener(onClick);
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

        private GameObject CreateCapRow()
        {
            GameObject row = new GameObject("CapRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.spacing = 8;

            CreateLabel("Global Cap", row.transform);
            GameObject sliderObj = CreateSlider(row.transform, 0f, 50000f, OnCapSliderChanged);
            capSlider = sliderObj.GetComponent<Slider>();
            capSlider.wholeNumbers = true;

            GameObject capLabelObj = CreateLabel("$0", row.transform);
            capLabel = capLabelObj.GetComponent<Text>();

            return row;
        }

        private void OnEnvelopeSliderChanged(int subTypeId, float value)
        {
            if (suppressCallbacks || budgetAllocation == null) return;
            budgetAllocation.SetEnvelopePct(subTypeId, value);
            RefreshFromModel();
        }

        private void OnCapSliderChanged(float value)
        {
            if (suppressCallbacks || budgetAllocation == null) return;
            budgetAllocation.GlobalMonthlyCap = (int)value;
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

            if (capSlider != null)
                capSlider.SetValueWithoutNotify(budgetAllocation.GlobalMonthlyCap);
            if (capLabel != null)
                capLabel.text = $"${budgetAllocation.GlobalMonthlyCap:N0}";

            suppressCallbacks = false;
        }

        private static GameObject CreateLabel(string text, Transform parent)
        {
            GameObject obj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var t = obj.GetComponent<Text>();
            t.text = text;
            t.fontSize = 14;
            t.color = Color.white;
            t.raycastTarget = false;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null) t.font = font;
            var le = obj.AddComponent<LayoutElement>();
            le.minWidth = 80;
            return obj;
        }

        private static GameObject CreateSlider(Transform parent, float min, float max, UnityEngine.Events.UnityAction<float> onChanged)
        {
            GameObject obj = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            obj.transform.SetParent(parent, false);

            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(obj.transform, false);
            bg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
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
            fill.GetComponent<Image>().color = new Color(0.3f, 0.7f, 1f, 1f);
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
