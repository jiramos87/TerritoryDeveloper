using System.Collections.Generic;
using Territory.Persistence;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// MonoBehaviour Host — resolves SaveLoadViewVM and sets UIDocument.rootVisualElement.dataSource.
    /// Lives on the UIDocument GameObject in MainMenu scene (sidecar coexistence per Q2).
    /// Refreshes save slot list on enable; lazy-loads thumbnails off main thread.
    /// Legacy SaveLoadScreenDataAdapter remains alive until Stage 6.0 quarantine plan.
    /// </summary>
    public sealed class SaveLoadViewHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        [Header("Producers (Inspector first; Awake fallback)")]
        [SerializeField] GameSaveManager _saveManager;

        SaveLoadViewVM _vm;
        readonly List<SaveFileMeta> _slotMetas = new List<SaveFileMeta>();

        void Awake()
        {
            if (_saveManager == null)
                _saveManager = FindObjectOfType<GameSaveManager>();
        }

        void OnEnable()
        {
            _vm = new SaveLoadViewVM();
            WireCommands();
            RefreshSlotList();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.dataSource = _vm;
            else
                Debug.LogWarning("[SaveLoadViewHost] UIDocument or rootVisualElement null on enable — check PanelSettings wiring.");
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.dataSource = null;
        }

        void WireCommands()
        {
            _vm.LoadCommand = OnLoad;
            _vm.SaveCommand = OnSave;
            _vm.DeleteCommand = OnDelete;
            _vm.CancelCommand = OnCancel;
            _vm.SelectLoadTabCommand = () => { _vm.Mode = "load"; _vm.TitleText = "Load Game"; };
            _vm.SelectSaveTabCommand = () => { _vm.Mode = "save"; _vm.TitleText = "Save Game"; };
        }

        void RefreshSlotList()
        {
            _slotMetas.Clear();
            _vm.Slots.Clear();
            _vm.SelectedIndex = -1;

            string saveDir = Application.persistentDataPath;
            var metas = GameSaveManager.GetSaveFiles(saveDir);
            if (metas != null)
            {
                foreach (var m in metas)
                {
                    _slotMetas.Add(m);
                    _vm.Slots.Add($"{m.DisplayName}  {m.SortDate:yyyy-MM-dd HH:mm}");
                }
            }
        }

        void OnLoad()
        {
            if (_vm.SelectedIndex < 0 || _vm.SelectedIndex >= _slotMetas.Count)
            {
                Debug.LogWarning("[SaveLoadViewHost] No save slot selected.");
                return;
            }
            var meta = _slotMetas[_vm.SelectedIndex];
            GameStartInfo.SetPendingLoadPath(meta.FilePath);
            UnityEngine.SceneManagement.SceneManager.LoadScene(1);
        }

        void OnSave()
        {
            if (_saveManager == null)
            {
                Debug.LogWarning("[SaveLoadViewHost] GameSaveManager not found — stub save.");
                return;
            }
            _saveManager.SaveGame(null);
            RefreshSlotList();
        }

        void OnDelete()
        {
            if (_vm.SelectedIndex < 0 || _vm.SelectedIndex >= _slotMetas.Count)
            {
                Debug.LogWarning("[SaveLoadViewHost] No save slot selected for delete.");
                return;
            }
            var meta = _slotMetas[_vm.SelectedIndex];
            GameSaveManager.DeleteSave(meta.FilePath);
            RefreshSlotList();
        }

        void OnCancel()
        {
            gameObject.SetActive(false);
        }
    }
}
