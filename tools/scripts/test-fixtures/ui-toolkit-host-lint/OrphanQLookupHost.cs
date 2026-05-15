using UnityEngine;
using UnityEngine.UIElements;

/// OrphanQLookupHost — Q-lookup with name that has no matching UXML (orphan).
public class OrphanQLookupHost : MonoBehaviour
{
    [SerializeField] private UIDocument _doc;
    private Button _orphanBtn;

    private void OnEnable()
    {
        var root = _doc.rootVisualElement;
        // This element name does not exist in any UXML file → orphan_q_lookup error
        _orphanBtn = root.Q<Button>("does-not-exist-in-uxml-9999");
        if (_orphanBtn != null)
        {
            _orphanBtn.clicked += OnOrphanClicked;
        }
    }

    private void OnDisable()
    {
        // Properly unsubscribed — we only want orphan_q_lookup error, not missing_unsubscribe
        if (_orphanBtn != null)
        {
            _orphanBtn.clicked -= OnOrphanClicked;
        }
    }

    private void OnOrphanClicked()
    {
    }
}
