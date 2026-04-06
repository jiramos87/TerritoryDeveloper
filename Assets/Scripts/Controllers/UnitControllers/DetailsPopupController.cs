using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Territory.Utilities;

namespace Territory.UI
{
    /// <summary>
    /// UI controller for the cell details popup. Shows detailed information about a selected grid cell.
    /// </summary>
    public class DetailsPopupController : MonoBehaviour
    {
        public GameObject detailsPanel;
        public Text waterConsumptionText;
        public Text waterOutputText;

        [SerializeField] private UIManager uiManager;
        [SerializeField] private float popupFadeSeconds = 0.12f;

        private Coroutine fadeRoutine;

        private float FadeDuration => uiManager != null ? uiManager.PopupFadeDurationSeconds : popupFadeSeconds;

        private void Awake()
        {
            if (uiManager == null)
                uiManager = FindObjectOfType<UIManager>();
        }

        public void ShowDetails()
        {
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);
            detailsPanel.SetActive(true);
            fadeRoutine = StartCoroutine(FadeInRoutine());
        }

        private IEnumerator FadeInRoutine()
        {
            CanvasGroup cg = UiCanvasGroupUtility.EnsureCanvasGroup(detailsPanel);
            cg.blocksRaycasts = true;
            cg.interactable = false;
            cg.alpha = 0f;
            yield return UiCanvasGroupUtility.FadeUnscaled(cg, 0f, 1f, FadeDuration);
            cg.interactable = true;
            fadeRoutine = null;
        }

        public void CloseDetails()
        {
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);
            if (detailsPanel == null || !detailsPanel.activeSelf)
                return;
            fadeRoutine = StartCoroutine(FadeOutRoutine());
        }

        private IEnumerator FadeOutRoutine()
        {
            CanvasGroup cg = detailsPanel.GetComponent<CanvasGroup>();
            if (cg != null)
                yield return UiCanvasGroupUtility.FadeUnscaled(cg, cg.alpha, 0f, FadeDuration);
            detailsPanel.SetActive(false);
            fadeRoutine = null;
        }

        public bool IsOpen()
        {
            return detailsPanel != null && detailsPanel.activeSelf;
        }
    }
}
