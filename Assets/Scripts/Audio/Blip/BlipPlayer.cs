using UnityEngine;

namespace Territory.Audio
{
    /// <summary>
    /// 16-source <see cref="AudioSource"/> pool for the Blip audio subsystem.
    /// <para>
    /// Spawns <see cref="poolSize"/> child GameObjects (<c>BlipVoice_0</c>…<c>BlipVoice_N</c>)
    /// in <see cref="Awake"/>, each carrying a pre-configured <see cref="AudioSource"/>.
    /// Holds the pool array and a round-robin cursor (advanced by TECH-174
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
    /// arrives Stage 2.3 T2.3.2 (TECH-174+ consumer).
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
        /// Round-robin cursor.  Starts at 0; advanced by TECH-174
        /// <c>PlayOneShot</c> dispatch.
        /// </summary>
        private int _cursor;

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

        private void OnDestroy()
        {
            BlipEngine.Unbind(this);
        }
    }
}
