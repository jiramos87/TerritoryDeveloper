using UnityEngine;
using UnityEngine.UI;
using Territory.UI;

namespace Territory.UI
{
/// <summary>
/// Toolbar button for Zone S placement. Wires its own click listener in Awake
/// so no Inspector OnClick event is needed.
/// </summary>
public class StateServiceZoningSelectorButton : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;

    private void Awake()
    {
        if (uiManager == null)
            uiManager = FindObjectOfType<UIManager>();
        GetComponent<Button>()?.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        uiManager?.OnStateServiceZoningButtonClicked();
    }
}
}
