using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static Territory.Utilities.UiCanvasGroupUtility;

namespace Territory.UI
{
    /// <summary>
    /// First-session welcome panel, <see cref="CanvasGroup"/> fade (partial of <see cref="UIManager"/>).
    /// </summary>
    public partial class UIManager
    {
        private const string WelcomeBriefingPrefsKey = "Fe50WelcomeBriefingSeen";
        private Coroutine welcomeBriefingDismissRoutine;

        void TryShowWelcomeBriefingAfterStart()
        {
            if (!showWelcomeBriefingOnFirstRun)
                return;
            if (PlayerPrefs.GetInt(WelcomeBriefingPrefsKey, 0) != 0)
                return;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
                return;

            if (welcomeBriefingRoot == null)
                welcomeBriefingRoot = BuildWelcomeBriefingUi(canvas.transform);
            StartCoroutine(WelcomeBriefingShowRoutine());
        }

        private GameObject BuildWelcomeBriefingUi(Transform canvasRoot)
        {
            GameObject root = new GameObject("Fe50WelcomeBriefing", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(canvasRoot, false);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            GameObject dim = new GameObject("Dimmer", typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(root.transform, false);
            RectTransform dimRt = dim.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            Image dimImg = dim.GetComponent<Image>();
            dimImg.raycastTarget = true;
            dimImg.color = hudUiTheme != null ? hudUiTheme.ModalDimmerColor : new Color(0f, 0f, 0f, 0.67f);

            GameObject card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(root.transform, false);
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(440f, 240f);
            cardRt.anchoredPosition = Vector2.zero;
            Image cardImg = card.GetComponent<Image>();
            cardImg.raycastTarget = true;
            cardImg.color = hudUiTheme != null ? hudUiTheme.SurfaceCardHud : new Color(0.15f, 0.16f, 0.2f, 0.95f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(card.transform, false);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.72f);
            titleRt.anchorMax = new Vector2(1f, 0.95f);
            titleRt.offsetMin = new Vector2(24f, 0f);
            titleRt.offsetMax = new Vector2(-24f, 0f);
            Text titleText = titleGo.GetComponent<Text>();
            titleText.font = font;
            titleText.fontSize = hudUiTheme != null ? hudUiTheme.FontSizeHeading : 18;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = hudUiTheme != null ? hudUiTheme.TextPrimary : Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = "Welcome to Territory Developer";

            GameObject bodyGo = new GameObject("Body", typeof(RectTransform), typeof(Text));
            bodyGo.transform.SetParent(card.transform, false);
            RectTransform bodyRt = bodyGo.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0f, 0.28f);
            bodyRt.anchorMax = new Vector2(1f, 0.68f);
            bodyRt.offsetMin = new Vector2(24f, 0f);
            bodyRt.offsetMax = new Vector2(-24f, 0f);
            Text bodyText = bodyGo.GetComponent<Text>();
            bodyText.font = font;
            bodyText.fontSize = hudUiTheme != null ? hudUiTheme.FontSizeBody : 14;
            bodyText.color = hudUiTheme != null ? hudUiTheme.TextSecondary : new Color(0.85f, 0.86f, 0.9f);
            bodyText.alignment = TextAnchor.MiddleCenter;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Truncate;
            bodyText.text = "Use the toolbar to build roads and zones. Press Esc to close panels. Good luck building your city.";

            GameObject btnGo = new GameObject("ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(card.transform, false);
            RectTransform btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0.08f);
            btnRt.anchorMax = new Vector2(0.5f, 0.08f);
            btnRt.pivot = new Vector2(0.5f, 0f);
            btnRt.sizeDelta = new Vector2(200f, 40f);
            btnRt.anchoredPosition = Vector2.zero;
            Image btnImg = btnGo.GetComponent<Image>();
            btnImg.color = hudUiTheme != null ? hudUiTheme.SurfaceElevated : new Color(0.2f, 0.22f, 0.27f, 1f);
            Button btn = btnGo.GetComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(DismissWelcomeBriefing);

            GameObject btnLabelGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            btnLabelGo.transform.SetParent(btnGo.transform, false);
            RectTransform blRt = btnLabelGo.GetComponent<RectTransform>();
            blRt.anchorMin = Vector2.zero;
            blRt.anchorMax = Vector2.one;
            blRt.offsetMin = Vector2.zero;
            blRt.offsetMax = Vector2.zero;
            Text btnLabel = btnLabelGo.GetComponent<Text>();
            btnLabel.font = font;
            btnLabel.fontSize = hudUiTheme != null ? hudUiTheme.FontSizeBody : 14;
            btnLabel.color = hudUiTheme != null ? hudUiTheme.TextPrimary : Color.white;
            btnLabel.alignment = TextAnchor.MiddleCenter;
            btnLabel.text = "Continue";
            btnLabel.raycastTarget = false;

            root.SetActive(false);
            return root;
        }

        private IEnumerator WelcomeBriefingShowRoutine()
        {
            CanvasGroup cg = EnsureCanvasGroup(welcomeBriefingRoot);
            cg.blocksRaycasts = true;
            cg.interactable = false;
            cg.alpha = 0f;
            welcomeBriefingRoot.SetActive(true);
            yield return FadeUnscaled(cg, 0f, 1f, PopupFadeDurationSeconds);
            cg.interactable = true;
        }

        /// <summary>
        /// Hide welcome panel with short fade + persist dismissal in <see cref="PlayerPrefs"/>.
        /// </summary>
        public void DismissWelcomeBriefing()
        {
            if (welcomeBriefingRoot == null || !welcomeBriefingRoot.activeSelf)
                return;
            if (welcomeBriefingDismissRoutine != null)
                StopCoroutine(welcomeBriefingDismissRoutine);
            welcomeBriefingDismissRoutine = StartCoroutine(WelcomeBriefingHideRoutine());
        }

        private IEnumerator WelcomeBriefingHideRoutine()
        {
            PlayerPrefs.SetInt(WelcomeBriefingPrefsKey, 1);
            PlayerPrefs.Save();
            CanvasGroup cg = welcomeBriefingRoot.GetComponent<CanvasGroup>();
            if (cg != null)
                yield return FadeUnscaled(cg, cg.alpha, 0f, PopupFadeDurationSeconds);
            welcomeBriefingRoot.SetActive(false);
            welcomeBriefingDismissRoutine = null;
        }

        private bool IsWelcomeBriefingVisible()
        {
            return welcomeBriefingRoot != null && welcomeBriefingRoot.activeSelf;
        }
    }
}
