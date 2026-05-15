using UnityEngine;
using UnityEngine.UIElements;

/// MissingModalSlugHost — RegisterMigratedPanel with a slug that has no UXML on disk.
public class MissingModalSlugHost : MonoBehaviour
{
    [SerializeField] private UIDocument _doc;

    private void OnEnable()
    {
        var root = _doc.rootVisualElement;
        // Slug "nonexistent-modal-slug-99999" has no UXML → modal_slug_missing error
        ModalCoordinator.RegisterMigratedPanel("nonexistent-modal-slug-99999", root);
    }

    private void OnDisable()
    {
    }
}
