using System;
using System.Collections.Generic;
using Territory.Editor.UiBake.KindRenderers;
using UnityEngine;

namespace Territory.Editor.UiBake
{
    /// <summary>
    /// Stage 2 kind-renderer matrix. Replaces UiBakeHandler switch for panel widget kinds.
    /// Strict mode: unknown kind → BakeException. Inv #3: render-time only, never per-frame.
    /// </summary>
    public static class KindRendererMatrix
    {
        // owner annotations per finding F1 (drop-on-the-floor prevention).
        private static readonly Dictionary<string, IKindRenderer> _matrix =
            new Dictionary<string, IKindRenderer>(StringComparer.Ordinal)
            {
                // owner: stage-2 kind-renderer matrix (T2.0.1)
                { "slider-row",      new SliderRowRenderer() },
                { "toggle-row",      new ToggleRowRenderer() },
                { "dropdown-row",    new DropdownRowRenderer() },
                { "section-header",  new SectionHeaderRenderer() },
                { "list-row",        new ListRowRenderer() },
            };

        /// <summary>All registered kind slugs. Used by validate:bake-handler-kind-coverage lint.</summary>
        public static IEnumerable<string> RegisteredKinds => _matrix.Keys;

        /// <summary>
        /// Render <paramref name="kind"/> widget onto <paramref name="parent"/>.
        /// Strict mode: throws <see cref="BakeException"/> on unknown kind.
        /// </summary>
        public static GameObject Render(string kind, string paramsJson, Transform parent)
        {
            if (!_matrix.TryGetValue(kind, out var renderer))
            {
                throw new BakeException($"unknown_kind: '{kind}' not registered in KindRendererMatrix");
            }
            return renderer.Render(paramsJson, parent);
        }

        /// <summary>Returns true when <paramref name="kind"/> has a registered renderer.</summary>
        public static bool IsRegistered(string kind) => _matrix.ContainsKey(kind);
    }

    /// <summary>Thrown by <see cref="KindRendererMatrix.Render"/> on unknown kind (strict mode).</summary>
    public sealed class BakeException : Exception
    {
        public BakeException(string message) : base(message) { }
    }
}
