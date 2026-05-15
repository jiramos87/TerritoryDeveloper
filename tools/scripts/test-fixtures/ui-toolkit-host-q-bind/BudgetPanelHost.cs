using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Fixture Host for ui_toolkit_host_q_bind tests.</summary>
public class BudgetPanelHost : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    private Button _openBtn;

    private void OnEnable()
    {
        var root = _document.rootVisualElement;
        _openBtn = root.Q<Button>("open-budget-btn");
        _openBtn.RegisterCallback<ClickEvent>(_ => OnOpenClicked());
    }

    private void OnOpenClicked()
    {
        Debug.Log("budget panel opened");
    }
}
