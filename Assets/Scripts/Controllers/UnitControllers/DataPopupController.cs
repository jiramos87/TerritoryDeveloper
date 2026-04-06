using System.Collections;
using UnityEngine;
using Territory.Economy;
using Territory.Utilities;

namespace Territory.UI
{
    /// <summary>
    /// UI controller for the data/statistics popup panel. Displays city stats from UIManager and CityStats.
    /// Opens and closes with short <see cref="CanvasGroup"/> fades when supported.
    /// </summary>
    public class DataPopupController : MonoBehaviour
    {
        public GameObject statsPanel;
        public GameObject taxPanel;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private CityStats cityStats;
        [Tooltip("Growth budget sliders container; shown when tax panel is open and simulate growth is on.")]
        public GameObject growthBudgetSlidersContainer;

        private Coroutine fadeRoutine;

        private float FadeDuration => uiManager != null ? uiManager.PopupFadeDurationSeconds : 0.12f;

        private void Awake()
        {
            if (uiManager == null)
                uiManager = FindObjectOfType<UIManager>();
        }

        public void ShowStats()
        {
            StopFadeRoutine();
            taxPanel.SetActive(false);
            statsPanel.SetActive(true);
            RegisterWithUIManager(PopupType.StatsPanel);
            fadeRoutine = StartCoroutine(FadeInRoutine(statsPanel));
        }

        public void ToggleStats()
        {
            StopFadeRoutine();
            if (statsPanel.activeSelf)
            {
                fadeRoutine = StartCoroutine(FadeOutRoutine(statsPanel));
            }
            else
            {
                taxPanel.SetActive(false);
                statsPanel.SetActive(true);
                RegisterWithUIManager(PopupType.StatsPanel);
                fadeRoutine = StartCoroutine(FadeInRoutine(statsPanel));
            }
        }

        public void ToggleTaxes()
        {
            StopFadeRoutine();
            if (taxPanel.activeSelf)
            {
                fadeRoutine = StartCoroutine(FadeOutRoutine(taxPanel));
            }
            else
            {
                statsPanel.SetActive(false);
                taxPanel.SetActive(true);
                RegisterWithUIManager(PopupType.TaxPanel);
                if (growthBudgetSlidersContainer != null && cityStats != null)
                    growthBudgetSlidersContainer.SetActive(cityStats.simulateGrowth);
                fadeRoutine = StartCoroutine(FadeInRoutine(taxPanel));
            }
        }

        public void CloseAll()
        {
            StopFadeRoutine();
            if (statsPanel != null)
                statsPanel.SetActive(false);
            if (taxPanel != null)
                taxPanel.SetActive(false);
        }

        public void CloseStats()
        {
            StopFadeRoutine();
            if (statsPanel == null)
                return;
            if (statsPanel.activeSelf)
                fadeRoutine = StartCoroutine(FadeOutRoutine(statsPanel));
            else
                statsPanel.SetActive(false);
        }

        public void CloseTaxes()
        {
            StopFadeRoutine();
            if (taxPanel == null)
                return;
            if (taxPanel.activeSelf)
                fadeRoutine = StartCoroutine(FadeOutRoutine(taxPanel));
            else
                taxPanel.SetActive(false);
        }

        private void StopFadeRoutine()
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }
        }

        private IEnumerator FadeInRoutine(GameObject root)
        {
            if (root == null)
                yield break;
            CanvasGroup cg = UiCanvasGroupUtility.EnsureCanvasGroup(root);
            cg.blocksRaycasts = true;
            cg.interactable = false;
            cg.alpha = 0f;
            root.SetActive(true);
            yield return UiCanvasGroupUtility.FadeUnscaled(cg, 0f, 1f, FadeDuration);
            cg.interactable = true;
            fadeRoutine = null;
        }

        private IEnumerator FadeOutRoutine(GameObject root)
        {
            if (root == null)
                yield break;
            CanvasGroup cg = root.GetComponent<CanvasGroup>();
            if (cg != null)
                yield return UiCanvasGroupUtility.FadeUnscaled(cg, cg.alpha, 0f, FadeDuration);
            root.SetActive(false);
            fadeRoutine = null;
        }

        private void RegisterWithUIManager(PopupType type)
        {
            if (uiManager == null)
                uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
                uiManager.RegisterPopupOpened(type);
        }
    }
}
