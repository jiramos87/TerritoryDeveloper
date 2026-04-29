using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Territory.Persistence;
using Territory.UI.Themed;

namespace Territory.UI.Modals
{
    /// <summary>
    /// Bridges <see cref="GameSaveManager"/> save-slot enumeration to ThemedList row population
    /// and ThemedButton actions to save/load/delete/cancel. Inspector producer slot with
    /// FindObjectOfType fallback (invariant #4); UiTheme cached in Awake (invariant #3).
    /// Slot list refreshes on modal-open (polling via OnEnable).
    /// </summary>
    public class SaveLoadScreenDataAdapter : MonoBehaviour
    {
        [Header("Producer")]
        [SerializeField] private GameSaveManager _saveManager;

        [Header("Consumers")]
        [SerializeField] private ThemedList _slotList;
        [SerializeField] private ThemedButton _saveButton;
        [SerializeField] private ThemedButton _loadButton;
        [SerializeField] private ThemedButton _deleteButton;
        [SerializeField] private ThemedButton _cancelButton;

        private int _selectedSlotIndex = -1;
        private readonly List<string> _slotPaths = new List<string>();

        private void Awake()
        {
            if (_saveManager == null)
                _saveManager = FindObjectOfType<GameSaveManager>();
        }

        private void OnEnable()
        {
            RefreshSlotList();
            if (_saveButton != null) _saveButton.OnClicked += OnSave;
            if (_loadButton != null) _loadButton.OnClicked += OnLoad;
            if (_deleteButton != null) _deleteButton.OnClicked += OnDelete;
            if (_cancelButton != null) _cancelButton.OnClicked += OnCancel;
        }

        private void OnDisable()
        {
            if (_saveButton != null) _saveButton.OnClicked -= OnSave;
            if (_loadButton != null) _loadButton.OnClicked -= OnLoad;
            if (_deleteButton != null) _deleteButton.OnClicked -= OnDelete;
            if (_cancelButton != null) _cancelButton.OnClicked -= OnCancel;
            _selectedSlotIndex = -1;
            UpdateActionButtonStates();
        }

        private void RefreshSlotList()
        {
            _slotPaths.Clear();
            var labels = new List<string>();
            string folder = Application.persistentDataPath;
            if (Directory.Exists(folder))
            {
                string[] files = Directory.GetFiles(folder, "*.json");
                var sorted = new List<(string path, string label, System.DateTime date)>();
                foreach (string path in files)
                {
                    var meta = GameSaveManager.GetSaveMetadata(path);
                    sorted.Add((path, meta.displayName, meta.sortDate));
                }
                sorted.Sort((a, b) => b.date.CompareTo(a.date));
                foreach (var entry in sorted)
                {
                    _slotPaths.Add(entry.path);
                    labels.Add($"{entry.label}  {entry.date:yyyy-MM-dd HH:mm}");
                }
            }

            _selectedSlotIndex = -1;
            if (_slotList != null)
                _slotList.Populate(labels, OnSlotSelected);
            UpdateActionButtonStates();
        }

        private void OnSlotSelected(int index)
        {
            _selectedSlotIndex = index;
            UpdateActionButtonStates();
        }

        private void UpdateActionButtonStates()
        {
            bool slotPicked = _selectedSlotIndex >= 0 && _selectedSlotIndex < _slotPaths.Count;
            SetButtonInteractable(_saveButton, slotPicked);
            SetButtonInteractable(_loadButton, slotPicked);
            SetButtonInteractable(_deleteButton, slotPicked);
        }

        private static void SetButtonInteractable(ThemedButton btn, bool interactable)
        {
            if (btn == null) return;
            var ugui = btn.GetComponent<UnityEngine.UI.Button>();
            if (ugui != null) ugui.interactable = interactable;
        }

        private void OnSave()
        {
            if (_saveManager == null || _selectedSlotIndex < 0 || _selectedSlotIndex >= _slotPaths.Count) return;
            _saveManager.SaveGame();
            RefreshSlotList();
        }

        private void OnLoad()
        {
            if (_saveManager == null || _selectedSlotIndex < 0 || _selectedSlotIndex >= _slotPaths.Count) return;
            _saveManager.LoadGame(_slotPaths[_selectedSlotIndex]);
        }

        private void OnDelete()
        {
            if (_selectedSlotIndex < 0 || _selectedSlotIndex >= _slotPaths.Count) return;
            string path = _slotPaths[_selectedSlotIndex];
            if (File.Exists(path)) File.Delete(path);
            RefreshSlotList();
        }

        private void OnCancel()
        {
            gameObject.SetActive(false);
        }
    }
}
