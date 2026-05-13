using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Stage 5.0 (TECH-32925) — MonoBehaviour Host for load-view fullscreen panel.
    /// Updates progress + status from SceneLoader.
    /// </summary>
    public sealed class LoadViewHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        LoadViewVM _vm;
        ModalCoordinator _coordinator;

        void OnEnable()
        {
            _vm = new LoadViewVM();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(_vm);
            else
                Debug.LogWarning("[LoadViewHost] UIDocument or rootVisualElement null on enable.");

            _coordinator = FindObjectOfType<ModalCoordinator>();
            if (_coordinator != null && _doc != null && _doc.rootVisualElement != null)
                _coordinator.RegisterMigratedPanel("load-view", _doc.rootVisualElement);
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        /// <summary>Update loading progress from scene loader.</summary>
        public void SetProgress(float progress, string status = "")
        {
            if (_vm == null) return;
            _vm.Progress = progress;
            _vm.StatusText = status;
        }
    }
}
