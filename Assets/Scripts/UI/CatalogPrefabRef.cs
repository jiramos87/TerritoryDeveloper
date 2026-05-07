using UnityEngine;

namespace Territory.UI
{
    /// <summary>
    /// Stage 9.14 / TECH-22667 — catalog prefab identity marker.
    /// Attached to the root of a catalog-baked prefab instance in the scene so that
    /// tests and tooling can assert the correct prefab was instantiated.
    /// The bake pipeline or <c>SceneReplaceWithPrefab</c> bridge sets <see cref="slug"/>
    /// to match the catalog entity slug (e.g. <c>"hud-bar"</c>).
    /// </summary>
    public sealed class CatalogPrefabRef : MonoBehaviour
    {
        [SerializeField] public string slug;
    }
}
