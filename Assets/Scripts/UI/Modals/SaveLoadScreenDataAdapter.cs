using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Territory.Persistence;
using Territory.UI.Registry;
using Territory.UI.Themed;

namespace Territory.UI.Modals
{
    /// <summary>
    /// Wave A3 (TECH-27075) — refactored save/load screen adapter.
    /// Subscribes saveload.mode bind (save|load); renders list from GetSaveFiles() newest-first.
    /// Row click → highlight + set saveload.selectedSlot. Load button fires saveload.load.
    /// Trash icon → 3s confirm countdown → GameSaveManager.DeleteSave.
    /// Save mode: name-input pre-filled cityName-YYYY-MM-DD-HHmm; Save fires overwrite-confirm if existing.
    /// </summary>
    public class SaveLoadScreenDataAdapter : MonoBehaviour
    {
        [Header("Producer")]
        [SerializeField] private GameSaveManager _saveManager;

        [Header("Registry (resolved via FindObjectOfType when null)")]
        [SerializeField] private UiActionRegistry _actionRegistry;
        [SerializeField] private UiBindRegistry   _bindRegistry;
        [SerializeField] private ModalCoordinator _modalCoordinator;

        [Header("Consumers (legacy ThemedList/Button; nullable in baked-UI path)")]
        [SerializeField] private ThemedList   _slotList;
        [SerializeField] private ThemedButton _saveButton;
        [SerializeField] private ThemedButton _loadButton;
        [SerializeField] private ThemedButton _deleteButton;
        [SerializeField] private ThemedButton _cancelButton;

        // ── internal state ────────────────────────────────────────────────────
        private string _mode = "load";           // "save" | "load"
        private int    _selectedSlotIndex = -1;
        private readonly List<SaveFileMeta> _slotMetas = new List<SaveFileMeta>();

        // IDisposable subscription handles
        private IDisposable _modeSub;
        private IDisposable _listSub;
        private IDisposable _selectedSlotSub;

        // Trash confirm coroutine tracking (per row index)
        private Coroutine _trashConfirmCoroutine;
        private int       _trashPendingIndex = -1;

        // ── lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_saveManager == null)
                _saveManager = FindObjectOfType<GameSaveManager>();
            if (_actionRegistry == null)
                _actionRegistry = FindObjectOfType<UiActionRegistry>();
            if (_bindRegistry == null)
                _bindRegistry = FindObjectOfType<UiBindRegistry>();
            if (_modalCoordinator == null)
                _modalCoordinator = FindObjectOfType<ModalCoordinator>();
        }

        private void OnEnable()
        {
            // Register actions
            _actionRegistry?.Register("saveload.load",        _ => OnLoadConfirmed());
            _actionRegistry?.Register("saveload.save",        _ => OnSaveConfirmed());
            _actionRegistry?.Register("saveload.delete",      payload => OnDeleteRequested(payload));
            _actionRegistry?.Register("saveload.selectSlot",  payload => OnSelectSlot(payload));
            // Stage 10 hotfix — open Save/Load panel via ModalCoordinator (replaces UIManager.OnSaveGameButtonClicked toast).
            _actionRegistry?.Register("action.save-menu-open", _ => OnSaveMenuOpen());

            // Subscribe binds
            if (_bindRegistry != null)
            {
                _modeSub         = _bindRegistry.Subscribe<string>("saveload.mode",         OnModeChanged);
                _listSub         = _bindRegistry.Subscribe<object>("saveload.list",         _ => RefreshSlotList());
                _selectedSlotSub = _bindRegistry.Subscribe<int>   ("saveload.selectedSlot", OnSelectedSlotChanged);
            }

            // Seed initial mode from bind if already set
            try { _mode = _bindRegistry?.Get<string>("saveload.mode") ?? "load"; }
            catch { _mode = "load"; }

            // Wire legacy ThemedButton events (null-safe)
            if (_saveButton   != null) _saveButton.OnClicked   += OnSaveClicked;
            if (_loadButton   != null) _loadButton.OnClicked   += OnLoadClicked;
            if (_deleteButton != null) _deleteButton.OnClicked += OnDeleteClicked;
            if (_cancelButton != null) _cancelButton.OnClicked += OnCancelClicked;

            RefreshSlotList();
            PublishListBind();
        }

        private void Update()
        {
            // Stage 10 hotfix — Esc closes the screen (no UI cancel button).
            // Guard: only fire when active; UIManager.HandleEscapePress runs in parallel
            // but this adapter takes priority by handling the press before stack pop.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_modalCoordinator != null)
                    _modalCoordinator.Close("save-load-view");
                else
                    gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            // Dispose bind subscriptions
            _modeSub?.Dispose();         _modeSub = null;
            _listSub?.Dispose();         _listSub = null;
            _selectedSlotSub?.Dispose(); _selectedSlotSub = null;

            // Unwire legacy buttons
            if (_saveButton   != null) _saveButton.OnClicked   -= OnSaveClicked;
            if (_loadButton   != null) _loadButton.OnClicked   -= OnLoadClicked;
            if (_deleteButton != null) _deleteButton.OnClicked -= OnDeleteClicked;
            if (_cancelButton != null) _cancelButton.OnClicked -= OnCancelClicked;

            _selectedSlotIndex = -1;
            _trashPendingIndex = -1;
            if (_trashConfirmCoroutine != null)
            {
                StopCoroutine(_trashConfirmCoroutine);
                _trashConfirmCoroutine = null;
            }
            UpdateActionButtonStates();
        }

        // ── slot list ─────────────────────────────────────────────────────────

        private void RefreshSlotList()
        {
            _slotMetas.Clear();
            string saveDir = Application.persistentDataPath;

            var metas = GameSaveManager.GetSaveFiles(saveDir);
            if (metas != null)
            {
                foreach (var m in metas)
                    _slotMetas.Add(m);
            }

            _selectedSlotIndex = -1;
            var labels = new List<string>();
            foreach (var m in _slotMetas)
                labels.Add($"{m.DisplayName}  {m.SortDate:yyyy-MM-dd HH:mm}");

            if (_slotList != null)
                _slotList.Populate(labels, OnSlotSelected);

            UpdateActionButtonStates();
        }

        /// <summary>Publish current list to bind registry so reactive rows can render.</summary>
        private void PublishListBind()
        {
            _bindRegistry?.Set("saveload.list", _slotMetas as object);
            PushLoadDisabledBind();
            PushSaveNameBind();
        }

        private void PushLoadDisabledBind()
        {
            bool disabled = _selectedSlotIndex < 0 || _selectedSlotIndex >= _slotMetas.Count;
            _bindRegistry?.Set("saveload.loadDisabled", disabled);
        }

        private void PushSaveNameBind()
        {
            if (_mode != "save") return;
            string cityName = _saveManager != null
                ? (_saveManager.cityName ?? "City")
                : "City";
            string autoName = $"{cityName}-{DateTime.Now:yyyy-MM-dd-HHmm}";
            _bindRegistry?.Set("saveload.saveName", autoName);
        }

        // ── bind callbacks ────────────────────────────────────────────────────

        private void OnModeChanged(string mode)
        {
            _mode = mode ?? "load";
            PushSaveNameBind();
        }

        private void OnSelectedSlotChanged(int index)
        {
            _selectedSlotIndex = index;
            PushLoadDisabledBind();
            UpdateActionButtonStates();
        }

        // ── action callbacks ──────────────────────────────────────────────────

        private void OnSelectSlot(object payload)
        {
            if (payload is int idx)
            {
                _selectedSlotIndex = idx;
                _bindRegistry?.Set("saveload.selectedSlot", idx);
                PushLoadDisabledBind();
                UpdateActionButtonStates();
            }
        }

        private void OnDeleteRequested(object payload)
        {
            int index = payload is int i ? i : _selectedSlotIndex;
            if (index < 0 || index >= _slotMetas.Count) return;

            if (_trashPendingIndex == index)
            {
                // Second fire within confirm window — execute delete immediately
                ExecuteDelete(index);
                return;
            }

            // Start 3s confirm window
            _trashPendingIndex = index;
            if (_trashConfirmCoroutine != null)
                StopCoroutine(_trashConfirmCoroutine);
            _trashConfirmCoroutine = StartCoroutine(TrashConfirmCountdown(index, 3f));
        }

        private IEnumerator TrashConfirmCountdown(int index, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            // Countdown expired without second confirm → cancel
            if (_trashPendingIndex == index)
                _trashPendingIndex = -1;
            _trashConfirmCoroutine = null;
        }

        private void ExecuteDelete(int index)
        {
            if (index < 0 || index >= _slotMetas.Count) return;
            var meta = _slotMetas[index];
            GameSaveManager.DeleteSave(meta.FilePath);
            _trashPendingIndex = -1;
            if (_trashConfirmCoroutine != null)
            {
                StopCoroutine(_trashConfirmCoroutine);
                _trashConfirmCoroutine = null;
            }
            RefreshSlotList();
            PublishListBind();
        }

        private void OnLoadConfirmed()
        {
            if (_saveManager == null || _selectedSlotIndex < 0 || _selectedSlotIndex >= _slotMetas.Count) return;
            var meta = _slotMetas[_selectedSlotIndex];
            GameStartInfo.SetPendingLoadPath(meta.FilePath);
            UnityEngine.SceneManagement.SceneManager.LoadScene(2); // 2=CityScene (0=CoreScene,1=MainMenu)
        }

        private void OnSaveConfirmed()
        {
            if (_saveManager == null) return;
            string customName = null;
            try { customName = _bindRegistry?.Get<string>("saveload.saveName"); } catch { }
            _saveManager.SaveGame(customName);
            RefreshSlotList();
            PublishListBind();
        }

        private void OnSaveMenuOpen()
        {
            // Stage 10 hotfix — open save-load-view modal in save mode + seed list.
            _bindRegistry?.Set("saveload.mode", "save");
            _mode = "save";
            if (_modalCoordinator != null)
                _modalCoordinator.TryOpen("save-load-view");
            else if (gameObject != null)
                gameObject.SetActive(true);
            RefreshSlotList();
            PublishListBind();
        }

        // ── legacy ThemedButton callbacks (baked-UI fallback) ─────────────────

        private void OnSlotSelected(int index)
        {
            _selectedSlotIndex = index;
            _bindRegistry?.Set("saveload.selectedSlot", index);
            PushLoadDisabledBind();
            UpdateActionButtonStates();
        }

        private void OnSaveClicked()
        {
            if (_saveManager == null || _selectedSlotIndex < 0) return;
            OnSaveConfirmed();
        }

        private void OnLoadClicked()
        {
            if (_saveManager == null || _selectedSlotIndex < 0 || _selectedSlotIndex >= _slotMetas.Count) return;
            OnLoadConfirmed();
        }

        private void OnDeleteClicked()
        {
            OnDeleteRequested(_selectedSlotIndex);
        }

        private void OnCancelClicked()
        {
            gameObject.SetActive(false);
        }

        // ── button states ─────────────────────────────────────────────────────

        private void UpdateActionButtonStates()
        {
            bool slotPicked = _selectedSlotIndex >= 0 && _selectedSlotIndex < _slotMetas.Count;
            bool isSaveMode = _mode == "save";
            SetButtonInteractable(_loadButton,   slotPicked && !isSaveMode);
            SetButtonInteractable(_saveButton,   isSaveMode);
            SetButtonInteractable(_deleteButton, slotPicked);
        }

        private static void SetButtonInteractable(ThemedButton btn, bool interactable)
        {
            if (btn == null) return;
            var ugui = btn.GetComponent<UnityEngine.UI.Button>();
            if (ugui != null) ugui.interactable = interactable;
        }
    }
}
