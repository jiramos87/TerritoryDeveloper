using UnityEngine;
using Territory.Economy;
using Territory.Timing;
using Territory.UI.Juice;
using Territory.UI.StudioControls;

namespace Territory.UI.HUD
{
    /// <summary>
    /// Bridges live sim producers (<see cref="CityStats"/> SO money/population/happiness;
    /// <see cref="EconomyManager"/> finance live values; <see cref="TimeManager"/> speed index)
    /// into baked StudioControl SO refs on the new <c>hud-bar</c> prefab.
    /// </summary>
    /// <remarks>
    /// Read-only consumer. All refs cached in <see cref="Awake"/> (invariant #3 — never
    /// <see cref="MonoBehaviour.FindObjectOfType{T}()"/> in <see cref="Update"/>). Per-channel
    /// null-check on producer refs (guardrail #14) so partial init still surfaces ready channels.
    /// MonoBehaviour producers fall back to <see cref="MonoBehaviour.FindObjectOfType{T}()"/> in
    /// <see cref="Awake"/> when Inspector slot empty (invariant #4); SO ref must be Inspector-assigned.
    /// </remarks>
    public class HudBarDataAdapter : MonoBehaviour
    {
        // ── Producer refs (invariants #3 + #4 — Inspector + Awake fallback for MonoBehaviours)

        [Header("Producers")]
        [SerializeField] private CityStats _cityStats;
        [SerializeField] private EconomyManager _economyManager;
        [SerializeField] private TimeManager _timeManager;

        // ── Theme cache (invariant #3 — caching contract regardless of immediate read)

        [Header("Theme")]
        [SerializeField] private UiTheme _uiTheme;

        // ── Consumer refs (StudioControl variants on baked hud-bar prefab)

        [Header("Consumers")]
        [SerializeField] private SegmentedReadout _moneyReadout;
        [SerializeField] private SegmentedReadout _populationReadout; // optional — null-tolerant
        [SerializeField] private VUMeter _happinessMeter;
        [SerializeField] private NeedleBallistics _happinessNeedle; // optional — preferred input surface for happiness; falls back to none if absent
        [SerializeField] private IlluminatedButton[] _speedButtons; // length 5: paused / 0.5x / 1x / 2x / 4x

        private void Awake()
        {
            // MonoBehaviour producers — Inspector first, FindObjectOfType fallback (invariant #4).
            if (_economyManager == null) _economyManager = FindObjectOfType<EconomyManager>();
            if (_timeManager == null) _timeManager = FindObjectOfType<TimeManager>();
            // CityStats is a MonoBehaviour in this codebase per Assets/Scripts/Managers/GameManagers/CityStats.cs.
            if (_cityStats == null) _cityStats = FindObjectOfType<CityStats>();
            // UiTheme is a ScriptableObject — Inspector-only assignment per StudioControlBase pattern.
            // No FindObjectOfType for SOs.
        }

        private void Update()
        {
            // money channel
            if (_cityStats != null && _moneyReadout != null)
            {
                _moneyReadout.CurrentValue = _cityStats.money;
            }

            // population channel (optional consumer)
            if (_cityStats != null && _populationReadout != null)
            {
                _populationReadout.CurrentValue = _cityStats.population;
            }

            // happiness channel — preferred path is NeedleBallistics.TargetValue (Stage 5 contract);
            // VUMeter has no direct Value setter (read-only Detail). When the needle juice
            // sibling absent, write nothing — VUMeter ignored.
            if (_cityStats != null && _happinessNeedle != null)
            {
                _happinessNeedle.TargetValue = _cityStats.happiness;
            }

            // speed channel — exactly-one-illuminated mirroring TimeManager.CurrentTimeSpeedIndex
            if (_timeManager != null && _speedButtons != null && _speedButtons.Length > 0)
            {
                int idx = _timeManager.CurrentTimeSpeedIndex;
                if (idx >= 0 && idx < _speedButtons.Length)
                {
                    for (int i = 0; i < _speedButtons.Length; i++)
                    {
                        var btn = _speedButtons[i];
                        if (btn == null) continue;
                        btn.IlluminationAlpha = (i == idx) ? 1f : 0f;
                    }
                }
            }
        }
    }
}
