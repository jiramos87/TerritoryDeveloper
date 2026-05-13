using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Stage 5.0 (TECH-32923) — MonoBehaviour Host for time-controls HUD panel.
    /// Wires speed + pause commands; stub — wire TimeManager in next pass.
    /// </summary>
    public sealed class TimeControlsHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        TimeControlsVM _vm;
        ModalCoordinator _coordinator;

        void OnEnable()
        {
            _vm = new TimeControlsVM();
            WireCommands();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(_vm);
            else
                Debug.LogWarning("[TimeControlsHost] UIDocument or rootVisualElement null on enable.");
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        void WireCommands()
        {
            _vm.PauseCommand = OnTogglePause;
            _vm.SetSpeed1Command = () => OnSetSpeed(1);
            _vm.SetSpeed2Command = () => OnSetSpeed(2);
            _vm.SetSpeed3Command = () => OnSetSpeed(3);
        }

        void OnTogglePause()
        {
            if (_vm == null) return;
            _vm.Paused = !_vm.Paused;
            Debug.Log($"[TimeControlsHost] Pause toggled: {_vm.Paused} — stub; wire TimeManager.");
        }

        void OnSetSpeed(int speed)
        {
            if (_vm == null) return;
            _vm.TimeSpeed = speed;
            _vm.Paused = false;
            Debug.Log($"[TimeControlsHost] Speed set: {speed} — stub; wire TimeManager.");
        }
    }
}
