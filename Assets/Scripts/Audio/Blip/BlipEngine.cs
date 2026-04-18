using System.Collections.Generic;
using System.Threading;

namespace Territory.Audio
{
    /// <summary>
    /// Facade for the Blip audio subsystem.
    /// <para>
    /// Hosts (<see cref="BlipCatalog"/>, <see cref="BlipPlayer"/>) call
    /// <see cref="Bind"/> in <c>Awake</c> and <see cref="Unbind"/> in
    /// <c>OnDestroy</c>.  The facade caches the references in static fields;
    /// internal <c>Resolve*</c> helpers use a lazy
    /// <see cref="UnityEngine.Object.FindObjectOfType{T}"/> fallback (one-time
    /// cost, cached immediately — satisfies invariant #3: no per-frame lookup).
    /// </para>
    /// </summary>
    public static class BlipEngine
    {
        // ── Cached host references ─────────────────────────────────────────
        private static BlipCatalog _catalog;
        private static BlipPlayer  _player;

        // ── Bind / Unbind ──────────────────────────────────────────────────

        /// <summary>
        /// Called by <see cref="BlipCatalog.Awake"/>.
        /// Null arg is silently ignored (programming-error protection; callers
        /// must call <see cref="Unbind(BlipCatalog)"/> to clear).
        /// </summary>
        public static void Bind(BlipCatalog c)
        {
            if (c != null) _catalog = c;
        }

        /// <summary>
        /// Called by <see cref="BlipCatalog.OnDestroy"/>.
        /// Identity-guarded: only clears the cache when <paramref name="c"/> is
        /// the same object that was bound, preventing a late-arriving
        /// <c>OnDestroy</c> from a previous scene from wiping a freshly-reloaded
        /// catalog during additive-scene transitions.
        /// </summary>
        public static void Unbind(BlipCatalog c)
        {
            if (ReferenceEquals(_catalog, c)) _catalog = null;
        }

        /// <summary>
        /// Called by <see cref="BlipPlayer.Awake"/> after the pool is built.
        /// Null arg is silently ignored.
        /// </summary>
        public static void Bind(BlipPlayer p)
        {
            if (p != null) _player = p;
        }

        /// <summary>
        /// Called by <see cref="BlipPlayer.OnDestroy"/>.
        /// Identity-guarded: same stale-instance guard as
        /// <see cref="Unbind(BlipCatalog)"/>.
        /// </summary>
        public static void Unbind(BlipPlayer p)
        {
            if (ReferenceEquals(_player, p)) _player = null;
        }

        // ── Lazy resolvers ─────────────────────────────────────────────────

        /// <summary>
        /// Returns the cached <see cref="BlipCatalog"/> when set; otherwise
        /// performs a one-time <see cref="UnityEngine.Object.FindObjectOfType{T}"/>
        /// and caches the result.  Returns <c>null</c> only when no
        /// <see cref="BlipCatalog"/> component exists in the active scene.
        /// <para>
        /// Invariant #3 rationale: lookup is one-time and immediately cached;
        /// never called per-frame.
        /// </para>
        /// </summary>
        internal static BlipCatalog ResolveCatalog()
        {
            if (_catalog == null)
                _catalog = UnityEngine.Object.FindObjectOfType<BlipCatalog>();
            return _catalog;
        }

        /// <summary>
        /// Returns the cached <see cref="BlipPlayer"/> when set; otherwise
        /// performs a one-time <see cref="UnityEngine.Object.FindObjectOfType{T}"/>
        /// and caches the result.  Returns <c>null</c> only when no
        /// <see cref="BlipPlayer"/> component exists in the active scene.
        /// <para>
        /// Invariant #3 rationale: lookup is one-time and immediately cached;
        /// never called per-frame.
        /// </para>
        /// </summary>
        internal static BlipPlayer ResolvePlayer()
        {
            if (_player == null)
                _player = UnityEngine.Object.FindObjectOfType<BlipPlayer>();
            return _player;
        }

        // ── Public entry points ────────────────────────────────────────────

        /// <summary>
        /// Plays the blip identified by <paramref name="id"/>.
        /// <para>
        /// <b>Gates (silent return on failure, no throw):</b>
        /// <list type="bullet">
        ///   <item>Catalog null or not ready (scene-load race).</item>
        ///   <item>Player null (pool not yet built).</item>
        ///   <item>Cooldown registry blocks the id (inter-play gap not elapsed).</item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Variant selection:</b> deterministic patch → variant 0 unconditionally;
        /// non-deterministic patch → per-id xorshift32 RNG on catalog (stable sequence
        /// per boot).
        /// </para>
        /// </summary>
        public static void Play(BlipId id, float pitchMult = 1f, float gainMult = 1f)
        {
            AssertMainThread();

            var cat = ResolveCatalog();
            if (cat == null || !cat.IsReady) return;

            var player = ResolvePlayer();
            if (player == null) return;

            double nowDsp = UnityEngine.AudioSettings.dspTime;
            ref readonly BlipPatchFlat patch = ref cat.Resolve(id);

            if (!cat.CooldownRegistry.TryConsume(id, nowDsp, patch.cooldownMs)) return;

            int variantIndex = patch.deterministic
                ? 0
                : cat.NextVariant(id, patch.variantCount);

            int patchHash = cat.PatchHash(id);
            UnityEngine.AudioClip clip = cat.Baker.BakeOrGet(in patch, patchHash, variantIndex);
            UnityEngine.Audio.AudioMixerGroup group = cat.MixerRouter.Get(id);
            player.PlayOneShot(clip, pitchMult, gainMult, group);
        }

        /// <summary>
        /// Stops all active <see cref="UnityEngine.AudioSource"/> voices that are currently
        /// playing a clip baked from <paramref name="id"/>'s <b>patch hash</b>.
        /// <para>
        /// <b>Gate semantics (match <see cref="Play"/> null-silent contract):</b>
        /// returns silently when catalog is null, not ready, or player is null — no throw.
        /// </para>
        /// <para>
        /// <b>Non-destructive:</b> LRU cache entries and byte accounting are untouched;
        /// only active playback is halted.  <c>source.clip</c> references are left intact
        /// so the next <see cref="Play"/> call can overwrite them normally.
        /// </para>
        /// <para>
        /// <b>Algorithm:</b> materialise matching clips into a <see cref="HashSet{T}"/> for
        /// O(1) per-source lookup, then iterate the pool and call
        /// <see cref="UnityEngine.AudioSource.Stop"/> where
        /// <c>src.isPlaying &amp;&amp; hits.Contains(src.clip)</c>.
        /// </para>
        /// </summary>
        /// <param name="id">Blip id whose voices to stop.</param>
        public static void StopAll(BlipId id)
        {
            AssertMainThread();

            var cat = ResolveCatalog();
            if (cat == null || !cat.IsReady) return;

            var player = ResolvePlayer();
            if (player == null) return;

            int patchHash = cat.PatchHash(id);

            // Materialise matching clips into a HashSet for O(1) per-source lookup.
            var hits = new HashSet<UnityEngine.AudioClip>(
                cat.Baker.EnumerateClipsForPatchHash(patchHash));

            if (hits.Count == 0) return;

            var pool = player.Pool;
            for (int i = 0; i < pool.Count; i++)
            {
                var src = pool[i];
                if (src.isPlaying && hits.Contains(src.clip))
                    src.Stop();
            }
        }

        // ── Entry-point helpers ────────────────────────────────────────────

        /// <summary>
        /// Throws <see cref="System.InvalidOperationException"/> when called from any thread
        /// other than the Unity main thread.  Compares
        /// <see cref="Thread.CurrentThread"/>.<see cref="Thread.ManagedThreadId"/> to
        /// <see cref="BlipBootstrap.MainThreadId"/> captured in <c>BlipBootstrap.Awake</c>.
        /// </summary>
        private static void AssertMainThread()
        {
            int main = BlipBootstrap.MainThreadId;
            if (main == 0) return;
            int actual = Thread.CurrentThread.ManagedThreadId;
            if (actual != main)
                throw new System.InvalidOperationException(
                    $"BlipEngine entry point invoked off main thread (expected {main}, got {actual})");
        }
    }
}
