using UnityEngine;
using UnityEngine.UIElements;

/// FindObjectOfTypeInUpdateHost — FindObjectOfType called inside Update() — violation.
public class FindObjectOfTypeInUpdateHost : MonoBehaviour
{
    [SerializeField] private UIDocument _doc;

    private void Update()
    {
        // Anti-pattern: FindObjectOfType inside hot-path Update() → find_object_of_type_in_update error
        var mgr = FindObjectOfType<GameManager>();
        if (mgr != null)
        {
            mgr.DoSomething();
        }
    }
}
