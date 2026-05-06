using UnityEngine;

namespace Territory.UI
{
    /// <summary>
    /// Stateless SFX helper — one-shot UI audio via AudioSettings.PlayClipAtPoint.
    /// No AudioSource component required; clips fire from world-origin (UI audio bus).
    /// TECH-15225 — Stage 9.5 game-ui-catalog-bake.
    /// </summary>
    public static class UiSfxPlayer
    {
        /// <summary>Play a clip at world origin (standard UI bus placement). No-op when clip null.</summary>
        public static void Play(AudioClip clip)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, Vector3.zero);
        }

        /// <summary>Play a clip at world origin with custom volume. No-op when clip null.</summary>
        public static void Play(AudioClip clip, float volume)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, Vector3.zero, Mathf.Clamp01(volume));
        }
    }
}
