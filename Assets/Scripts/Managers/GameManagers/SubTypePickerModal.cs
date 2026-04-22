using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Territory.UI;

namespace Territory.Economy
{
    /// <summary>
    /// Modal listing seven <see cref="ZoneSubTypeRegistry"/> entries for Zone S placement.
    /// Player picks one → commits <c>currentSubTypeId</c> on <see cref="UIManager"/> and closes.
    /// Cancel (ESC / outside-click) resets id to -1 and exits S placement mode.
    /// See docs/zone-s-economy-exploration.md Review Note N3.
    /// </summary>
    public class SubTypePickerModal : MonoBehaviour
    {
        [SerializeField] private ZoneSubTypeRegistry registry;
        [SerializeField] private GameObject buttonPrefab;
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private GameObject backdrop;
        [SerializeField] private GameObject panelRoot;

        private UIManager uiManager;
        private readonly List<GameObject> spawnedButtons = new List<GameObject>();
        private bool isVisible;

        private void Awake()
        {
            if (registry == null)
                registry = FindObjectOfType<ZoneSubTypeRegistry>();
            if (panelRoot != null)
                panelRoot.SetActive(false);
            if (backdrop != null)
                backdrop.SetActive(false);
        }

        // ESC handled by UIManager.PopupStack — no local ESC check needed.

        /// <summary>Open picker; build buttons from registry.</summary>
        public void Show(UIManager caller)
        {
            uiManager = caller;
            if (panelRoot == null) return;

            ClearButtons();
            BuildButtons();

            panelRoot.SetActive(true);
            if (backdrop != null)
                backdrop.SetActive(true);
            isVisible = true;
        }

        /// <summary>
        /// Close picker. When <paramref name="cancelled"/> is true, resets sub-type id to -1
        /// and exits S placement mode (Review Note N3).
        /// </summary>
        public void Hide(bool cancelled)
        {
            if (!isVisible) return;
            isVisible = false;

            if (panelRoot != null)
                panelRoot.SetActive(false);
            if (backdrop != null)
                backdrop.SetActive(false);

            if (cancelled && uiManager != null)
            {
                uiManager.SetCurrentSubTypeId(-1);
                uiManager.OnGrassButtonClicked();
            }
        }

        /// <summary>Backdrop click → cancel.</summary>
        public void OnBackdropClicked()
        {
            Hide(cancelled: true);
        }

        private void BuildButtons()
        {
            if (registry == null || buttonContainer == null) return;

            var entries = registry.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                GameObject btnObj;
                if (buttonPrefab != null)
                {
                    btnObj = Instantiate(buttonPrefab, buttonContainer);
                }
                else
                {
                    btnObj = new GameObject($"SubTypeBtn_{entry.id}", typeof(RectTransform), typeof(Button), typeof(Image));
                    btnObj.transform.SetParent(buttonContainer, false);
                }

                var label = btnObj.GetComponentInChildren<Text>();
                if (label != null)
                {
                    if (registry != null && registry.TryGetPickerLabelForSubType(entry.id, out string line, out _))
                        label.text = line;
                    else
                    {
                        Debug.LogError("[SubTypePickerModal] [TECH-686] catalog-backed label failed for subType " + entry.id);
                        label.text = $"{entry.displayName} (—)";
                    }
                }

                int capturedId = entry.id;
                var button = btnObj.GetComponent<Button>();
                if (button != null)
                    button.onClick.AddListener(() => OnEntrySelected(capturedId));

                spawnedButtons.Add(btnObj);
            }
        }

        private void OnEntrySelected(int subTypeId)
        {
            if (uiManager != null)
                uiManager.SetCurrentSubTypeId(subTypeId);
            Hide(cancelled: false);
        }

        private void ClearButtons()
        {
            for (int i = 0; i < spawnedButtons.Count; i++)
            {
                if (spawnedButtons[i] != null)
                    Destroy(spawnedButtons[i]);
            }
            spawnedButtons.Clear();
        }
    }
}
