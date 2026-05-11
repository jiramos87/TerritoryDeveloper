using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Territory.UI;
using Territory.UI.Registry;

namespace Territory.UI.Modals
{
    /// <summary>
    /// Host-adapter for pause-menu modal. Implements a two-level navigated-view
    /// state machine: root (Resume/Settings/Save/Load/Main Menu/Quit buttons)
    /// → sub-view (settings | save | load). Mount hides the root sibling
    /// column, instantiates the sub-view inside the content-slot, and injects
    /// a runtime nav header (back arrow + screen title). OnBack restores root.
    /// Esc routing two-level: in sub-view → back; in root → close pause.
    /// </summary>
    public class PauseMenuDataAdapter : MonoBehaviour
    {
        [Header("Producer")]
        [SerializeField] private MainMenuController _mainMenu;
        [SerializeField] private ModalCoordinator _modalCoordinator;
        [SerializeField] private UiActionRegistry _actionRegistry;

        [Header("Sub-view prefabs (host-adapter slot mounts)")]
        [SerializeField] private GameObject _settingsViewPrefab;
        [SerializeField] private GameObject _saveViewPrefab;
        [SerializeField] private GameObject _loadViewPrefab;

        private Transform _contentSlot;
        private Transform[] _rootSiblings;

        private GameObject _currentSubView;
        private string _currentContentScreen = "root";

        // Saved LayoutElement values on content-slot so we can restore on unmount.
        // Bake emits view-slot with flexibleHeight=-1, so VLG (pause-menu root) gives
        // it 0 height when alone — sub-view rect collapses + nav-header renders off-screen.
        private float _slotSavedFlexHeight = -1f;
        private float _slotSavedPrefHeight = -1f;

        private void Awake()
        {
            if (_mainMenu == null)
                _mainMenu = FindObjectOfType<MainMenuController>();
            if (_modalCoordinator == null)
                _modalCoordinator = FindObjectOfType<ModalCoordinator>();
            if (_actionRegistry == null)
                _actionRegistry = FindObjectOfType<UiActionRegistry>();

            _contentSlot = SlotAnchorResolver.ResolveByPanel("pause-menu", transform);

            // Cache every direct child except the content-slot — root button column
            // hides as a group on sub-view entry, restores on back.
            var siblings = new List<Transform>(transform.childCount);
            for (int i = 0; i < transform.childCount; i++)
            {
                var c = transform.GetChild(i);
                if (c == _contentSlot) continue;
                siblings.Add(c);
            }
            _rootSiblings = siblings.ToArray();

            if (_contentSlot != null) _contentSlot.gameObject.SetActive(false);

            // UIManager.InstantiateBakedPanel adds this component via runtime
            // AddComponent so serialized prefab refs are dropped. Editor fallback
            // resolves baked sub-view prefabs from disk.
#if UNITY_EDITOR
            if (_settingsViewPrefab == null)
                _settingsViewPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/UI/Prefabs/Generated/settings-view.prefab");
            if (_saveViewPrefab == null)
                _saveViewPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/UI/Prefabs/Generated/save-view.prefab");
            if (_loadViewPrefab == null)
                _loadViewPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/UI/Prefabs/Generated/load-view.prefab");
#endif

            RegisterActions();
        }

        private void RegisterActions()
        {
            if (_actionRegistry == null) return;
            _actionRegistry.Register("pause.resume",          _ => OnResume());
            _actionRegistry.Register("pause.openSettings",    _ => OnSettings());
            _actionRegistry.Register("pause.openSave",        _ => OnSave());
            _actionRegistry.Register("pause.openLoad",        _ => OnLoad());
            _actionRegistry.Register("pause.back",            _ => OnBack());
            _actionRegistry.Register("pause.mainMenu",        _ => OnMainMenu());
            _actionRegistry.Register("pause.mainMenu.confirm", _ => OnMainMenu());
            _actionRegistry.Register("pause.quit",            _ => OnQuit());
            _actionRegistry.Register("pause.quit.confirm",    _ => OnQuit());
        }

        private void OnDisable()
        {
            // Panel hidden mid-sub-view → reset to root so reopen lands on button column.
            UnmountSubView();
        }

        // ── Action handlers ───────────────────────────────────────────────────

        private void OnResume()
        {
            if (UIManager.Instance != null) UIManager.Instance.ClosePopup(PopupType.PauseMenu);
        }

        private void OnSettings() { MountSubView("settings", _settingsViewPrefab); }
        private void OnSave()     { MountSubView("save",     _saveViewPrefab); }
        private void OnLoad()     { MountSubView("load",     _loadViewPrefab); }

        private void OnBack()
        {
            UnmountSubView();
        }

        // CityScene has no MainMenuController instance; FindObjectOfType returns null
        // so the legacy delegation path was a silent no-op. MainMenu = build index 0.
        private void OnMainMenu()
        {
            SceneManager.LoadScene(0);
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ── Esc routing hook ──────────────────────────────────────────────────

        /// <summary>
        /// Called by UIManager.HandleEscapePress before pause-menu close. Returns
        /// true when Esc was consumed (sub-view → root). False = bubble to default
        /// close-pause behaviour.
        /// </summary>
        public bool TryHandleBackButton()
        {
            if (_currentContentScreen != "root")
            {
                OnBack();
                return true;
            }
            return false;
        }

        // ── Sub-view slot mount ───────────────────────────────────────────────

        private void MountSubView(string screen, GameObject prefab)
        {
            if (_currentContentScreen == screen) return;
            UnmountSubView();

            if (_contentSlot == null || prefab == null)
            {
                _currentContentScreen = screen;
                return;
            }

            // Hide root button column — sub-view occupies modal body.
            for (int i = 0; i < _rootSiblings.Length; i++)
            {
                if (_rootSiblings[i] != null) _rootSiblings[i].gameObject.SetActive(false);
            }
            _contentSlot.gameObject.SetActive(true);

            // Expand content-slot so VLG (pause-menu root) gives it the full available
            // height; otherwise sub-view renders inside a 0-height rect and the nav-header
            // ends up off-screen for tall sub-views (settings = 13 rows).
            var slotLe = _contentSlot.GetComponent<LayoutElement>();
            if (slotLe == null) slotLe = _contentSlot.gameObject.AddComponent<LayoutElement>();
            _slotSavedFlexHeight = slotLe.flexibleHeight;
            _slotSavedPrefHeight = slotLe.preferredHeight;
            slotLe.flexibleHeight = 1f;
            slotLe.preferredHeight = -1f;

            _currentSubView = Instantiate(prefab, _contentSlot, worldPositionStays: false);
            var rt = _currentSubView.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }

            if (screen == "save")
            {
                var input = _currentSubView.GetComponentInChildren<TMP_InputField>(includeInactive: true);
                if (input != null)
                {
                    var cityStats = FindObjectOfType<Territory.Economy.CityStats>();
                    string cityName = (cityStats != null && !string.IsNullOrEmpty(cityStats.cityName))
                        ? cityStats.cityName : "City";
                    input.text = $"{cityName}-{System.DateTime.Now:yyyy-MM-dd-HHmm}";
                }
            }

            InjectNavHeader(_currentSubView, screen);
            _currentContentScreen = screen;
        }

        private void UnmountSubView()
        {
            if (_currentSubView != null)
            {
                Destroy(_currentSubView);
                _currentSubView = null;
            }
            if (_contentSlot != null)
            {
                // Restore content-slot LayoutElement before hiding so root buttons re-layout cleanly.
                var slotLe = _contentSlot.GetComponent<LayoutElement>();
                if (slotLe != null)
                {
                    slotLe.flexibleHeight = _slotSavedFlexHeight;
                    slotLe.preferredHeight = _slotSavedPrefHeight;
                }
                _contentSlot.gameObject.SetActive(false);
            }
            if (_rootSiblings != null)
            {
                for (int i = 0; i < _rootSiblings.Length; i++)
                {
                    if (_rootSiblings[i] != null) _rootSiblings[i].gameObject.SetActive(true);
                }
            }
            _currentContentScreen = "root";
        }

        // Injects a horizontal nav-header (back arrow + title) as first child of
        // the sub-view. Plain Button + TMP — no theme-token wiring; visual is
        // intentionally minimal (dark chip + white glyph).
        private void InjectNavHeader(GameObject subView, string screen)
        {
            if (subView == null) return;

            var headerGo = new GameObject("nav-header", typeof(RectTransform),
                typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            headerGo.transform.SetParent(subView.transform, worldPositionStays: false);
            headerGo.transform.SetAsFirstSibling();

            var hlg = headerGo.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.padding = new RectOffset(4, 4, 4, 4);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childControlHeight = true;
            hlg.childControlWidth = true;

            var headerLe = headerGo.GetComponent<LayoutElement>();
            headerLe.preferredHeight = 48f;
            headerLe.flexibleWidth = 1f;

            // Back button — 40x40 dark chip with "<" glyph.
            var backGo = new GameObject("back-button", typeof(RectTransform),
                typeof(Image), typeof(Button), typeof(LayoutElement));
            backGo.transform.SetParent(headerGo.transform, worldPositionStays: false);
            var backImg = backGo.GetComponent<Image>();
            backImg.color = new Color(0.18f, 0.18f, 0.18f, 0.9f);
            var backLe = backGo.GetComponent<LayoutElement>();
            backLe.preferredWidth = 40f;
            backLe.preferredHeight = 40f;
            backLe.minWidth = 40f;
            backLe.minHeight = 40f;
            var backBtn = backGo.GetComponent<Button>();
            backBtn.targetGraphic = backImg;
            backBtn.onClick.AddListener(OnBack);

            var backLabelGo = new GameObject("Label", typeof(RectTransform));
            backLabelGo.transform.SetParent(backGo.transform, worldPositionStays: false);
            var backTmp = backLabelGo.AddComponent<TextMeshProUGUI>();
            backTmp.text = "<";
            backTmp.alignment = TextAlignmentOptions.Center;
            backTmp.fontSize = 24f;
            backTmp.fontStyle = FontStyles.Bold;
            backTmp.color = Color.white;
            backTmp.raycastTarget = false;
            var backLabelRt = backLabelGo.GetComponent<RectTransform>();
            backLabelRt.anchorMin = Vector2.zero;
            backLabelRt.anchorMax = Vector2.one;
            backLabelRt.offsetMin = backLabelRt.offsetMax = Vector2.zero;

            // Title label.
            var titleGo = new GameObject("title", typeof(RectTransform), typeof(LayoutElement));
            titleGo.transform.SetParent(headerGo.transform, worldPositionStays: false);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = ScreenToTitle(screen);
            titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
            titleTmp.fontSize = 22f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = Color.white;
            titleTmp.raycastTarget = false;
            var titleLe = titleGo.GetComponent<LayoutElement>();
            titleLe.flexibleWidth = 1f;
            titleLe.preferredHeight = 40f;
        }

        private static string ScreenToTitle(string screen)
        {
            switch (screen)
            {
                case "settings": return "Settings";
                case "save":     return "Save";
                case "load":     return "Load";
                default:          return string.Empty;
            }
        }
    }
}
