using UnityEngine;

namespace Territory.Editor.UiBake.KindRenderers
{
    /// <summary>Stage 2 kind-renderer contract. Render(paramsJson, parent) → spawned GameObject.</summary>
    public interface IKindRenderer
    {
        /// <summary>Render a widget of this kind onto <paramref name="parent"/>.</summary>
        /// <param name="paramsJson">Raw params_json string for the widget (may be null/empty).</param>
        /// <param name="parent">Parent Transform to attach the spawned GameObject to.</param>
        /// <returns>Spawned root GameObject; never null on success.</returns>
        GameObject Render(string paramsJson, Transform parent);
    }
}
