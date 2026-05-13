using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Stage 5.0 (TECH-32924) — MonoBehaviour Host for glossary-panel modal.
    /// Wires search + close; stub — wire GlossaryService in next pass.
    /// </summary>
    public sealed class GlossaryPanelHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        GlossaryPanelVM _vm;
        ModalCoordinator _coordinator;

        void OnEnable()
        {
            _vm = new GlossaryPanelVM();
            _vm.CloseCommand = OnClose;

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.dataSource = _vm;
            else
                Debug.LogWarning("[GlossaryPanelHost] UIDocument or rootVisualElement null on enable.");

            _coordinator = FindObjectOfType<ModalCoordinator>();
            if (_coordinator != null && _doc != null && _doc.rootVisualElement != null)
                _coordinator.RegisterMigratedPanel("glossary-panel", _doc.rootVisualElement);
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.dataSource = null;
        }

        void OnClose()
        {
            if (_coordinator != null)
                _coordinator.HideMigrated("glossary-panel");
            else
                gameObject.SetActive(false);
        }
    }
}
