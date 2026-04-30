using UnityEditor;
using UnityEngine;

namespace Territory.UI.Editor
{
    /// <summary>
    /// Resolves catalog sprite slugs to loaded <see cref="Sprite"/> assets at bake time.
    /// Stub implementation: returns null when no frame sprites are present under Assets/UI/Sprites/Frames/.
    /// Wire actual sprites here when per-slug .png assets land in that directory.
    /// </summary>
    public static class AtlasIndex
    {
        const string FramesRoot = "Assets/UI/Sprites/Frames/";

        /// <summary>
        /// Returns the <see cref="Sprite"/> asset for <paramref name="slug"/>, or null if not found.
        /// Loads via <see cref="AssetDatabase.LoadAssetAtPath{T}"/> — Editor-time only.
        /// </summary>
        public static Sprite Resolve(string slug)
        {
            if (string.IsNullOrEmpty(slug))
                return null;

            var path = $"{FramesRoot}{slug}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning($"[AtlasIndex] No sprite at '{path}' for slug '{slug}'. Place {slug}.png under {FramesRoot} to resolve.");
            return sprite;
        }
    }
}
