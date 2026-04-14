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
    // Public constants — future Settings UI binds same keys without duplication.
    public const string SfxVolumeDbKey = "BlipSfxVolumeDb";
    public const string SfxVolumeParam = "SfxVolume";
    public const float SfxVolumeDbDefault = 0f;

    [SerializeField] private AudioMixer blipMixer;

    // Child slot refs — empty MVP; populated Step 2 with BlipCatalog / BlipPlayer / etc.
    [SerializeField] private Transform catalogSlot;       // BlipCatalog Step 2
    [SerializeField] private Transform playerSlot;        // BlipPlayer Step 2
    [SerializeField] private Transform mixerRouterSlot;   // BlipMixerRouter Step 2
    [SerializeField] private Transform cooldownSlot;      // BlipCooldownRegistry Step 2

    private void Awake()
    {
        // DontDestroyOnLoad — persistent across scene loads from MainMenu onward (pattern per GameNotificationManager.cs).
        DontDestroyOnLoad(transform.root.gameObject);

        float db = PlayerPrefs.GetFloat(SfxVolumeDbKey, SfxVolumeDbDefault);

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
}
