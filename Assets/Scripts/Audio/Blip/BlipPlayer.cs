using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Territory.Audio
{
    /// <summary>
    /// 16-source <see cref="AudioSource"/> pool for the Blip audio subsystem.
    /// <para>
    /// Spawns <see cref="poolSize"/> child GameObjects (<c>BlipVoice_0</c>…<c>BlipVoice_N</c>)
    /// in <see cref="Awake"/>, each carrying a pre-configured <see cref="AudioSource"/>.
    /// Holds the pool array and a round-robin cursor (advanced by
    /// <c>PlayOneShot</c> dispatch).
    /// </para>
    /// <para>
    /// Placed as a child of the <b>BlipBootstrap</b> prefab (Stage 1.1 —
    /// persistent <c>DontDestroyOnLoad</c> root).  Invariant #4 — MonoBehaviour,
    /// no singleton.  Invariant #3 — pool spawned once in <c>Awake</c>, never
    /// looked up per-frame.
    /// </para>
    /// <para>
    /// Full <see cref="BlipEngine.Bind"/> / <see cref="BlipEngine.Unbind"/> wiring
    /// arrives Stage 2.3 T2.3.2.
    /// </para>
    /// </summary>
    public sealed class BlipPlayer : MonoBehaviour
    {
        /// <summary>
        /// Number of <see cref="AudioSource"/> voices in the pool.
        /// Serialized so authoring can tune without recompile; default 16 per
        /// Stage 2.2 Exit bullet 5.
        /// </summary>
        [SerializeField] private int poolSize = 16;

        /// <summary>Pool of pre-allocated <see cref="AudioSource"/> voices.</summary>
        private AudioSource[] _pool;

        /// <summary>
        /// Read-only view of the voice pool for use by <see cref="BlipEngine.StopAll"/>
        /// (Stage 2.3 Phase 2).  Exposed <c>internal</c> — same <c>Territory.Audio</c>
        /// namespace; not public API.
        /// Invariant #4 rationale: <c>BlipEngine</c> is a stateless static facade; exposing
        /// this accessor avoids a new singleton while keeping pool ownership on the MonoBehaviour.
        /// </summary>
        internal IReadOnlyList<AudioSource> Pool => _pool;

        /// <summary>
        /// Round-robin cursor.  Starts at 0; advanced by
        /// <c>PlayOneShot</c> dispatch.
        /// </summary>
        private int _cursor;

        /// <summary>
        /// PlayMode / EditMode test-only accessor — exposes <c>_cursor</c> for
        /// pool-wrap assertions.  Not for production callers.
        /// </summary>
        internal int DebugCursor => _cursor;

        private void Awake()
        {
            _pool = new AudioSource[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject($"BlipVoice_{i}");
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                _pool[i] = src;
            }

            BlipEngine.Bind(this);
        }

        /// <summary>
        /// Round-robin dispatch — picks the next pool voice, configures it, and plays
        /// the supplied clip once.
        /// <para>
        /// <b>Round-robin contract (Stage 2.2 Phase 3):</b> <c>_cursor</c> advances
        /// <em>before</em> <c>Play()</c> so the next caller lands on the next voice
        /// even if the current <c>Play()</c> throws.  Wrap: <c>(_cursor + 1) % _pool.Length</c>.
        /// </para>
        /// <para>
        /// <b>Voice-steal overwrite:</b> if the chosen source is still playing, it is
        /// stopped and its clip replaced immediately — no crossfade (post-MVP per
        /// orchestration guardrails §390).  At MVP sound count (10) with a 16-voice
        /// pool, steal events are rare.
        /// </para>
        /// <para>
        /// <c>outputAudioMixerGroup</c> is assigned per-call (not cached per voice)
        /// because <c>BlipMixerRouter.Get(BlipId)</c> resolves the group upstream in
        /// <c>BlipEngine.Play</c> — keeps the voice group-agnostic.
        /// </para>
        /// </summary>
        /// <param name="clip">Clip to play.</param>
        /// <param name="pitch">Playback pitch (1 = normal).</param>
        /// <param name="gain">Playback volume/gain (0–1).</param>
        /// <param name="group">Mixer group routing for this voice.</param>
        public void PlayOneShot(AudioClip clip, float pitch, float gain, AudioMixerGroup group)
        {
            var source = _pool[_cursor];
            _cursor = (_cursor + 1) % _pool.Length;
            if (source.isPlaying) source.Stop();
            source.clip = clip;
            source.pitch = pitch;
            source.volume = gain;
            source.outputAudioMixerGroup = group;
            source.Play();
        }

        private void OnDestroy()
        {
            BlipEngine.Unbind(this);
        }
    }
}
