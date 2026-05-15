using UnityEngine;
using UnityEngine.UIElements;

/// MissingUnsubscribeHost — .clicked += without matching -= in OnDisable → memory leak violation.
public class MissingUnsubscribeHost : MonoBehaviour
{
    [SerializeField] private UIDocument _doc;
    private Button _submitBtn;

    private void OnEnable()
    {
        var root = _doc.rootVisualElement;
        _submitBtn = root.Q<Button>("submit-btn");
        if (_submitBtn != null)
        {
            // Registered here but never unregistered in OnDisable → missing_unsubscribe error
            _submitBtn.clicked += OnSubmitClicked;
        }
    }

    private void OnDisable()
    {
        // Missing: _submitBtn.clicked -= OnSubmitClicked;
    }

    private void OnSubmitClicked()
    {
    }
}
