using Territory.Audio;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// Scaffolding stub — fields + Bind + InitListeners + empty handler bodies.
/// Logic bodies filled by Stage 4.2 (TECH-239 range).
/// Mounts on OptionsPanel GameObject; no static singleton (invariant #4).
/// </summary>
public sealed class BlipVolumeController : MonoBehaviour
{
    private Slider _sfxSlider;
    private Toggle _sfxToggle;
    private AudioMixer _mixer;
    private int _lastBlipStep = -1;

    private void Awake()
    {
        _mixer = BlipBootstrap.Instance?.BlipMixer;
        if (_mixer == null)
        {
            Debug.LogWarning("[Blip] BlipVolumeController: BlipBootstrap.BlipMixer null — volume UI disabled");
            enabled = false;
            return;
        }
    }

    /// <summary>Assigns slider and toggle refs from parent UI builder.</summary>
    public void Bind(Slider s, Toggle t)
    {
        _sfxSlider = s;
        _sfxToggle = t;
    }

    /// <summary>
    /// Registers value-change listeners. Called once after Bind by CreateOptionsPanel wiring.
    /// No per-frame work (invariant #3).
    /// </summary>
    public void InitListeners()
    {
        _sfxSlider.onValueChanged.AddListener(OnSliderChanged);
        _sfxToggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnEnable()
    {
        float db = PlayerPrefs.GetFloat(BlipBootstrap.SfxVolumeDbKey, 0f);
        float linear = db <= -79f ? 0f : Mathf.Clamp01(Mathf.Pow(10f, db / 20f));
        _sfxSlider.SetValueWithoutNotify(linear);
        _lastBlipStep = Mathf.RoundToInt(linear * 10);

        bool muted = PlayerPrefs.GetInt(BlipBootstrap.SfxMutedKey, 0) != 0;
        _sfxToggle.SetIsOnWithoutNotify(muted);
    }

    /// <summary>
    /// Converts linear 0..1 slider value to dB (20·log10), writes PlayerPrefs,
    /// and applies to mixer only when not muted (mute dominates).
    /// </summary>
    private void OnSliderChanged(float v)
    {
        float db = v > 0.0001f ? 20f * Mathf.Log10(v) : -80f;
        PlayerPrefs.SetFloat(BlipBootstrap.SfxVolumeDbKey, db);

        if (!_sfxToggle.isOn && _mixer != null)
        {
            _mixer.SetFloat(BlipBootstrap.SfxVolumeParam, db);
        }

        // Play preview blip at each 10% step so the user hears the current volume while dragging.
        int step = Mathf.RoundToInt(v * 10);
        if (step != _lastBlipStep)
        {
            _lastBlipStep = step;
            if (!_sfxToggle.isOn)
                BlipEngine.Play(BlipId.UiButtonHover);
        }
    }

    /// <summary>
    /// Writes mute state to PlayerPrefs. Mute → clamps mixer to -80 dB;
    /// unmute → re-reads stored dB from PlayerPrefs and applies.
    /// </summary>
    private void OnToggleChanged(bool mute)
    {
        PlayerPrefs.SetInt(BlipBootstrap.SfxMutedKey, mute ? 1 : 0);

        if (_mixer == null) return;

        if (mute)
        {
            _mixer.SetFloat(BlipBootstrap.SfxVolumeParam, -80f);
        }
        else
        {
            float db = PlayerPrefs.GetFloat(BlipBootstrap.SfxVolumeDbKey, 0f);
            _mixer.SetFloat(BlipBootstrap.SfxVolumeParam, db);
        }
    }

}
