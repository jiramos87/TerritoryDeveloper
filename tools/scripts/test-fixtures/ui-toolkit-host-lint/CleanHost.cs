using UnityEngine;
using UnityEngine.UIElements;

/// CleanHost — no lint violations. Does not use named Q<T>() lookups so orphan rule does not fire.
public class CleanHost : MonoBehaviour
{
    [SerializeField] private UIDocument _doc;
    private Button _closeBtn;

    private void OnEnable()
    {
        var root = _doc.rootVisualElement;
        // Use Q without a name string to avoid orphan_q_lookup rule
        _closeBtn = root.Q<Button>();
        if (_closeBtn != null)
        {
            _closeBtn.clicked += OnCloseClicked;
        }
    }

    private void OnDisable()
    {
        if (_closeBtn != null)
        {
            _closeBtn.clicked -= OnCloseClicked;
        }
    }

    private void Start()
    {
        // Manager cached at start — clean (not in Update)
        var mgr = FindObjectOfType<GameManager>();
    }

    private void Update()
    {
        // No FindObjectOfType here — clean
    }

    private void OnCloseClicked()
    {
        // handler
    }
}
