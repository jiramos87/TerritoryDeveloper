using UnityEngine;

namespace Territory.Editor.UiBake
{
    /// <summary>
    /// Minimal child spec passed to IBakeHandler.Bake. Subset of PanelSnapshotChild
    /// fields needed by plugin dispatch — avoids cross-assembly coupling to Bridge.
    /// </summary>
    public sealed class BakeChildSpec
    {
        /// <summary>Outer kind from panels.json (e.g. "button", "label").</summary>
        public string kind;
        /// <summary>Raw params_json string for the child (may be null/empty).</summary>
        public string paramsJson;
        /// <summary>Instance slug for naming (e.g. "hud-bar-zoom-in-button").</summary>
        public string instanceSlug;
    }

    /// <summary>
    /// Layer 2 bake-handler plugin contract (TECH-28362).
    /// Implement to add a new bake kind without editing UiBakeHandler's switch.
    /// Dispatcher: <see cref="BakeHandlerRegistry"/>.
    /// </summary>
    public interface IBakeHandler
    {
        /// <summary>Kinds this handler claims (e.g. new[] { "button" }).</summary>
        string[] SupportedKinds { get; }

        /// <summary>Higher value wins when two handlers claim the same kind.</summary>
        int Priority { get; }

        /// <summary>Bake <paramref name="child"/> spec onto <paramref name="parent"/> transform.</summary>
        void Bake(BakeChildSpec child, Transform parent);
    }
}
