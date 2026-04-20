using UnityEngine;
using UnityEngine.UI;
using Territory.UI;

namespace Territory.Economy
{
    /// <summary>
    /// Bond issuance + read-only detail modal on <see cref="UIManager"/> popup stack.
    /// Principal (min 100), term 12/24/48 months, live monthly repayment preview from
    /// <see cref="BondLedgerService.FixedInterestRate"/>. Issue calls <see cref="IBondLedger.TryIssueBond"/>.
    /// </summary>
    public class BondIssuanceModal : MonoBehaviour
    {
        private const int MinPrincipal = 100;
        private static readonly int[] TermChoices = { 12, 24, 48 };

        [SerializeField] private BondLedgerService bondLedger;
        [SerializeField] private EconomyManager economyManager;

        [SerializeField] private GameObject backdrop;
        [SerializeField] private GameObject panelRoot;

        private UIManager uiManager;
        private InputField principalField;
        private Text previewText;
        private Text statusText;
        private Button issueButton;
        private Button[] termButtons = new Button[3];
        private int selectedTermMonths = 24;
        private bool readOnlyMode;
        private bool isVisible;
        private bool uiBuilt;

        private void Awake()
        {
            if (bondLedger == null)
                bondLedger = FindObjectOfType<BondLedgerService>();
            if (economyManager == null)
                economyManager = FindObjectOfType<EconomyManager>();

            if (panelRoot != null)
                panelRoot.SetActive(false);
            if (backdrop != null)
                backdrop.SetActive(false);
        }

        /// <summary>Open modal in issuance mode.</summary>
        public void ShowIssue(UIManager caller)
        {
            readOnlyMode = false;
            uiManager = caller;
            EnsureUiBuilt();
            if (statusText != null)
                statusText.gameObject.SetActive(false);
            principalField.interactable = true;
            foreach (var b in termButtons)
                if (b != null) b.interactable = true;
            issueButton.gameObject.SetActive(true);
            principalField.text = MinPrincipal.ToString();
            SelectTerm(24);
            RefreshPreview();
            ShowPanel();
        }

        /// <summary>Open modal showing active bond; issue controls disabled.</summary>
        public void ShowReadOnly(UIManager caller, BondData bond)
        {
            readOnlyMode = true;
            uiManager = caller;
            EnsureUiBuilt();
            principalField.text = bond.principal.ToString();
            SelectTerm(bond.termMonths);
            principalField.interactable = false;
            foreach (var b in termButtons)
                if (b != null) b.interactable = false;
            issueButton.gameObject.SetActive(false);
            previewText.text = $"Monthly repayment: ${bond.monthlyRepayment:N0} — {bond.monthsRemaining} mo left";
            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = bond.arrears ? "Arrears — repayment missed" : "Bond in good standing";
                statusText.color = bond.arrears ? new Color(1f, 0.35f, 0.35f) : Color.white;
            }
            ShowPanel();
        }

        /// <summary>Close modal.</summary>
        public void Hide()
        {
            if (!isVisible) return;
            isVisible = false;
            if (panelRoot != null)
                panelRoot.SetActive(false);
            if (backdrop != null)
                backdrop.SetActive(false);
        }

        private void ShowPanel()
        {
            if (panelRoot == null) return;
            panelRoot.SetActive(true);
            if (backdrop != null)
                backdrop.SetActive(true);
            isVisible = true;
        }

        private void EnsureUiBuilt()
        {
            if (uiBuilt) return;
            uiBuilt = true;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (backdrop == null)
            {
                GameObject bd = new GameObject("BondBackdrop", typeof(RectTransform), typeof(Image));
                bd.transform.SetParent(transform, false);
                var img = bd.GetComponent<Image>();
                img.color = new Color(0f, 0f, 0f, 0.55f);
                var rt = bd.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                var btn = bd.AddComponent<Button>();
                btn.onClick.AddListener(Hide);
                backdrop = bd;
            }

            if (panelRoot == null)
            {
                GameObject pr = new GameObject("BondPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
                pr.transform.SetParent(transform, false);
                var bg = pr.GetComponent<Image>();
                bg.color = new Color(0.12f, 0.12f, 0.15f, 1f);
                var v = pr.GetComponent<VerticalLayoutGroup>();
                v.padding = new RectOffset(16, 16, 16, 16);
                v.spacing = 10;
                v.childAlignment = TextAnchor.UpperCenter;
                v.childControlWidth = true;
                v.childControlHeight = false;
                v.childForceExpandWidth = true;
                var prt = pr.GetComponent<RectTransform>();
                prt.sizeDelta = new Vector2(420, 360);
                prt.anchorMin = new Vector2(0.5f, 0.5f);
                prt.anchorMax = new Vector2(0.5f, 0.5f);
                prt.pivot = new Vector2(0.5f, 0.5f);
                panelRoot = pr;
            }

            Transform parent = panelRoot.transform;

            CreateLabel("Issue municipal bond", parent, font, 18, FontStyle.Bold);

            GameObject rowP = new GameObject("PrincipalRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowP.transform.SetParent(parent, false);
            var h0 = rowP.GetComponent<HorizontalLayoutGroup>();
            h0.spacing = 8;
            h0.childAlignment = TextAnchor.MiddleLeft;
            CreateLabel("Principal ($)", rowP.transform, font, 14, FontStyle.Normal);

            GameObject inputObj = new GameObject("PrincipalInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputObj.transform.SetParent(rowP.transform, false);
            var inputImg = inputObj.GetComponent<Image>();
            inputImg.color = new Color(0.2f, 0.2f, 0.22f);
            var inputLe = inputObj.AddComponent<LayoutElement>();
            inputLe.minWidth = 140;
            inputLe.flexibleWidth = 1;
            principalField = inputObj.GetComponent<InputField>();
            principalField.lineType = InputField.LineType.SingleLine;
            principalField.contentType = InputField.ContentType.IntegerNumber;

            GameObject textChild = new GameObject("Text", typeof(Text));
            textChild.transform.SetParent(inputObj.transform, false);
            var tChild = textChild.GetComponent<Text>();
            tChild.font = font;
            tChild.fontSize = 14;
            tChild.color = Color.white;
            tChild.supportRichText = false;
            tChild.alignment = TextAnchor.MiddleLeft;
            var tRt = textChild.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(8, 4);
            tRt.offsetMax = new Vector2(-8, -4);
            principalField.textComponent = tChild;

            GameObject phChild = new GameObject("Placeholder", typeof(Text));
            phChild.transform.SetParent(inputObj.transform, false);
            var ph = phChild.GetComponent<Text>();
            ph.font = font;
            ph.fontSize = 14;
            ph.color = new Color(1f, 1f, 1f, 0.35f);
            ph.text = "min 100";
            ph.alignment = TextAnchor.MiddleLeft;
            var phRt = phChild.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = new Vector2(8, 4);
            phRt.offsetMax = new Vector2(-8, -4);
            principalField.placeholder = ph;

            principalField.onValueChanged.AddListener(_ => RefreshPreview());

            CreateLabel("Term (months)", parent, font, 14, FontStyle.Normal);

            GameObject termRow = new GameObject("TermRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            termRow.transform.SetParent(parent, false);
            var h1 = termRow.GetComponent<HorizontalLayoutGroup>();
            h1.spacing = 6;
            h1.childAlignment = TextAnchor.MiddleCenter;
            for (int i = 0; i < 3; i++)
            {
                int term = TermChoices[i];
                Button tb = CreateSmallButton($"{term} mo", termRow.transform, font, () => SelectTerm(term));
                termButtons[i] = tb;
            }

            GameObject prevGo = new GameObject("Preview", typeof(Text));
            prevGo.transform.SetParent(parent, false);
            previewText = prevGo.GetComponent<Text>();
            previewText.font = font;
            previewText.fontSize = 14;
            previewText.color = new Color(0.7f, 0.95f, 1f);
            previewText.alignment = TextAnchor.MiddleLeft;

            GameObject stGo = new GameObject("Status", typeof(Text));
            stGo.transform.SetParent(parent, false);
            statusText = stGo.GetComponent<Text>();
            statusText.font = font;
            statusText.fontSize = 13;
            statusText.alignment = TextAnchor.MiddleLeft;
            statusText.gameObject.SetActive(false);

            GameObject btnRow = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            btnRow.transform.SetParent(parent, false);
            var hb = btnRow.GetComponent<HorizontalLayoutGroup>();
            hb.spacing = 12;
            hb.childAlignment = TextAnchor.MiddleCenter;

            issueButton = CreateButton("Issue", btnRow.transform, font, OnIssueClicked);
            CreateButton("Close", btnRow.transform, font, Hide);
        }

        private static void CreateLabel(string txt, Transform parent, Font font, int size, FontStyle style)
        {
            GameObject go = new GameObject("Label", typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = Color.white;
            t.text = txt;
            t.alignment = TextAnchor.MiddleLeft;
        }

        private static Button CreateButton(string label, Transform parent, Font font, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(label, typeof(Button), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.25f, 0.45f, 0.75f);
            var le = go.GetComponent<LayoutElement>();
            le.minWidth = 100;
            le.minHeight = 32;
            var btn = go.GetComponent<Button>();
            GameObject txtGo = new GameObject("Text", typeof(Text));
            txtGo.transform.SetParent(go.transform, false);
            var te = txtGo.GetComponent<Text>();
            te.font = font;
            te.fontSize = 14;
            te.color = Color.white;
            te.text = label;
            te.alignment = TextAnchor.MiddleCenter;
            var rt = te.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            btn.onClick.AddListener(onClick);
            return btn;
        }

        private Button CreateSmallButton(string label, Transform parent, Font font, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(label, typeof(Button), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.3f, 0.32f, 0.36f);
            var le = go.GetComponent<LayoutElement>();
            le.minWidth = 72;
            le.minHeight = 28;
            var btn = go.GetComponent<Button>();
            GameObject txtGo = new GameObject("Text", typeof(Text));
            txtGo.transform.SetParent(go.transform, false);
            var te = txtGo.GetComponent<Text>();
            te.font = font;
            te.fontSize = 13;
            te.color = Color.white;
            te.text = label;
            te.alignment = TextAnchor.MiddleCenter;
            var rt = te.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            btn.onClick.AddListener(onClick);
            return btn;
        }

        private void SelectTerm(int months)
        {
            selectedTermMonths = months;
            for (int i = 0; i < termButtons.Length; i++)
            {
                if (termButtons[i] == null) continue;
                var img = termButtons[i].GetComponent<Image>();
                bool on = TermChoices[i] == months;
                img.color = on ? new Color(0.35f, 0.55f, 0.85f) : new Color(0.3f, 0.32f, 0.36f);
            }
            RefreshPreview();
        }

        private void RefreshPreview()
        {
            if (readOnlyMode || previewText == null) return;
            if (!int.TryParse(principalField != null ? principalField.text : "0", out int principal))
                principal = 0;
            float rate = bondLedger != null ? bondLedger.FixedInterestRate : 0.12f;
            int monthly = 0;
            if (principal > 0 && selectedTermMonths > 0)
                monthly = (int)((principal * (1f + rate)) / selectedTermMonths);
            previewText.text = $"Estimated monthly repayment: ${monthly:N0} (at {(rate * 100f):F0}% fixed)";
        }

        private void OnIssueClicked()
        {
            if (readOnlyMode || bondLedger == null || economyManager == null) return;
            int tier = economyManager.GetCityScaleTier();
            if (bondLedger.GetActiveBond(tier) != null)
            {
                if (GameNotificationManager.Instance != null)
                    GameNotificationManager.Instance.PostWarning("An active bond already exists for this city tier.");
                return;
            }
            if (!int.TryParse(principalField != null ? principalField.text : "0", out int principal))
                principal = 0;
            if (principal < MinPrincipal)
            {
                if (GameNotificationManager.Instance != null)
                    GameNotificationManager.Instance.PostWarning($"Principal must be at least ${MinPrincipal:N0}.");
                return;
            }
            bool ok = bondLedger.TryIssueBond(tier, principal, selectedTermMonths);
            if (ok)
            {
                Hide();
                if (GameNotificationManager.Instance != null)
                    GameNotificationManager.Instance.PostSuccess("Bond issued.");
            }
            else if (GameNotificationManager.Instance != null)
                GameNotificationManager.Instance.PostWarning("Could not issue bond.");
        }
    }
}
