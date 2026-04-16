using System.Threading;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Bootstraps Blip audio at scene load. Reads SFX volume from PlayerPrefs and applies it
/// to the BlipMixer once on Awake. No per-frame work (invariant #3).
/// Child-slot Transform fields (catalogSlot, playerSlot, mixerRouterSlot, cooldownSlot) populated in Step 2.
/// </summary>
/// <remarks>
/// Scene-load suppression: no Blip fires until <c>BlipCatalog</c> sets ready flag on <c>Awake</c>
/// (lands Step 2). Until then, any <c>BlipEngine.Play</c> call returns early. Prevents boot-race
/// clicks during <c>MainMenu → Game.unity</c> transition.
/// </remarks>
public class BlipBootstrap : MonoBehaviour
{
    /// <summary>
    /// Singleton-style accessor for the persistent bootstrap root.
    /// Set in <c>Awake</c> after <c>DontDestroyOnLoad</c>; cleared in <c>OnDestroy</c>.
    /// PlayMode tests assert <c>BlipBootstrap.Instance != null</c> after scene load.
    /// Not a true singleton — invariant #4: MonoBehaviour placed in scene, not created via <c>new</c>.
    /// </summary>
    public static BlipBootstrap Instance { get; private set; }

    // Main-thread id — captured in Awake; read by BlipBaker.AssertMainThread (Stage 2.1)
    // and later by BlipEngine entry-point asserts (Stage 2.3 T2.3.1).
    public static int MainThreadId { get; private set; }

    // Public constants — future Settings UI binds same keys without duplication.
    public const string SfxVolumeDbKey = "BlipSfxVolumeDb";
    public const string SfxVolumeParam = "SfxVolume";
    public const float SfxVolumeDbDefault = 0f;
    public const string SfxMutedKey = "BlipSfxMuted";

    /// <summary>
    /// Returns the serialized BlipMixer ref. Stage 4.2 consumer caches this once in Awake (invariant #3).
    /// </summary>
    public AudioMixer BlipMixer => blipMixer;

    [SerializeField] private AudioMixer blipMixer;

    // Child slot refs — empty MVP; populated Step 2 with BlipCatalog / BlipPlayer / etc.
    [SerializeField] private Transform catalogSlot;       // BlipCatalog Step 2
    [SerializeField] private Transform playerSlot;        // BlipPlayer Step 2
    [SerializeField] private Transform mixerRouterSlot;   // BlipMixerRouter Step 2
    [SerializeField] private Transform cooldownSlot;      // BlipCooldownRegistry Step 2

    private void Awake()
    {
        // Capture main-thread id first — BlipBaker.AssertMainThread reads this.
        MainThreadId = Thread.CurrentThread.ManagedThreadId;

        // DontDestroyOnLoad — persistent across scene loads from MainMenu onward (pattern per GameNotificationManager.cs).
        DontDestroyOnLoad(transform.root.gameObject);

        Instance = this;

        float db = PlayerPrefs.GetFloat(SfxVolumeDbKey, SfxVolumeDbDefault);

        // Boot-time mute restore — clamps dB ahead of mixer apply if persisted mute = 1.
        // Cold-start guarantee: muted state honored from first Blip play, not only after Options opens.
        int muted = PlayerPrefs.GetInt(SfxMutedKey, 0);
        if (muted != 0) db = -80f;

        if (blipMixer == null)
        {
            Debug.LogWarning("[Blip] BlipBootstrap: blipMixer ref missing — SfxVolume not bound");
            return;
        }

        if (blipMixer.SetFloat(SfxVolumeParam, db))
        {
            Debug.Log($"[Blip] SfxVolume bound headless: {db} dB");
        }
        else
        {
            Debug.LogWarning($"[Blip] BlipMixer.SetFloat('{SfxVolumeParam}', {db}) failed — param not exposed on mixer?");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
