using Territory.Timing;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// MonoBehaviour Host for time-controls HUD strip. Wires three speed buttons (1×/2×/3×)
    /// directly to <see cref="TimeManager.SetTimeSpeedIndex(int)"/>. Pause is owned by the
    /// hud-bar pause button (recovery plan Phase B); time-controls is speed-only.
    /// Update() mirrors active class so the pressed speed lights up.
    /// </summary>
    public sealed class TimeControlsHost : MonoBehaviour
    {
        const string ActiveClass = "time-controls__btn--active";

        [SerializeField] UIDocument _doc;
        [SerializeField] TimeManager _time;

        TimeControlsVM _vm;
        Button _btn1x;
        Button _btn2x;
        Button _btn3x;

        void Awake()
        {
            if (_time == null) _time = FindObjectOfType<TimeManager>();
        }

        void OnEnable()
        {
            _vm = new TimeControlsVM();

            if (_doc == null || _doc.rootVisualElement == null)
            {
                Debug.LogWarning("[TimeControlsHost] UIDocument or rootVisualElement null on enable.");
                return;
            }
            var root = _doc.rootVisualElement;
            root.style.position = Position.Absolute;
            root.style.top = 0;
            root.style.left = 0;
            root.style.right = 0;
            root.style.bottom = 0;
            root.SetCompatDataSource(_vm);

            _btn1x = root.Q<Button>("btn-speed1");
            _btn2x = root.Q<Button>("btn-speed2");
            _btn3x = root.Q<Button>("btn-speed3");

            if (_btn1x != null) _btn1x.clicked += OnSpeed1;
            if (_btn2x != null) _btn2x.clicked += OnSpeed2;
            if (_btn3x != null) _btn3x.clicked += OnSpeed3;
        }

        void OnDisable()
        {
            if (_btn1x != null) _btn1x.clicked -= OnSpeed1;
            if (_btn2x != null) _btn2x.clicked -= OnSpeed2;
            if (_btn3x != null) _btn3x.clicked -= OnSpeed3;
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        void Update()
        {
            if (_time == null) return;
            int idx = _time.CurrentTimeSpeedIndex;
            _btn1x?.EnableInClassList(ActiveClass, idx == 1);
            _btn2x?.EnableInClassList(ActiveClass, idx == 2);
            _btn3x?.EnableInClassList(ActiveClass, idx == 3);
        }

        void OnSpeed1() { if (_time != null) _time.SetTimeSpeedIndex(1); }
        void OnSpeed2() { if (_time != null) _time.SetTimeSpeedIndex(2); }
        void OnSpeed3() { if (_time != null) _time.SetTimeSpeedIndex(3); }
    }
}
