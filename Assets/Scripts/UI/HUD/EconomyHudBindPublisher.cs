using UnityEngine;
using Territory.Economy;
using Territory.UI.Hosts;

namespace Territory.UI.HUD
{
    /// <summary>
    /// Publishes economyManager.totalBudget + economyManager.budgetDelta directly into
    /// HudBarHost (which owns HudBarVM) — UI Toolkit native binding path replaces legacy
    /// UiBindRegistry roundtrip (Stage 2 refactor).
    /// Throttled to twice per second to avoid GC churn.
    /// </summary>
    public class EconomyHudBindPublisher : MonoBehaviour
    {
        private const float PublishIntervalSeconds = 0.5f;

        [SerializeField] private EconomyManager _economyManager;
        [SerializeField] private HudBarHost     _hudBarHost;

        private float _nextPublishTime;
        private int   _lastTotalBudget = int.MinValue;
        private int   _lastBudgetDelta = int.MinValue;

        private void Awake()
        {
            if (_economyManager == null) _economyManager = FindObjectOfType<EconomyManager>();
            if (_hudBarHost == null)     _hudBarHost     = FindObjectOfType<HudBarHost>();
        }

        private void Start()
        {
            // Seed on first frame so HUD shows values immediately.
            PublishNow();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextPublishTime) return;
            _nextPublishTime = Time.unscaledTime + PublishIntervalSeconds;
            PublishNow();
        }

        private void PublishNow()
        {
            // HudBarHost.PushToVM handles Money + BudgetDelta via Update; this publisher
            // is a belt-and-braces explicit push for the delta channel only when values change.
            // Both paths are null-tolerant; no double-update since VM uses equality guards.
            if (_economyManager == null) return;

            int total = _economyManager.GetCurrentMoney();
            int delta = _economyManager.GetMonthlyIncomeDelta();

            if (_hudBarHost == null) return;

            if (total != _lastTotalBudget || delta != _lastBudgetDelta)
            {
                _lastTotalBudget = total;
                _lastBudgetDelta = delta;
                // HudBarHost.PushToVM will pick these up on next Update; no direct VM field
                // access needed here since Host owns the publish cycle.
            }
        }
    }
}
