using UnityEngine;
using Territory.Economy;
using Territory.UI.Registry;

namespace Territory.UI.HUD
{
    /// <summary>
    /// Stage 10 hotfix — publish economyManager.totalBudget + economyManager.budgetDelta
    /// binds consumed by the baked HUD-bar BUDGET button text (panels.json bind ids).
    /// Runs in CityScene only; auto-resolves EconomyManager + UiBindRegistry on Awake.
    /// Throttled to twice per second to avoid GC churn.
    /// </summary>
    public class EconomyHudBindPublisher : MonoBehaviour
    {
        private const float PublishIntervalSeconds = 0.5f;

        [SerializeField] private EconomyManager  _economyManager;
        [SerializeField] private UiBindRegistry  _bindRegistry;

        private float _nextPublishTime;
        private int   _lastTotalBudget = int.MinValue;
        private int   _lastBudgetDelta = int.MinValue;

        private void Awake()
        {
            if (_economyManager == null) _economyManager = FindObjectOfType<EconomyManager>();
            if (_bindRegistry == null)   _bindRegistry   = FindObjectOfType<UiBindRegistry>();
        }

        private void Start()
        {
            // Seed binds on first frame so text widgets render immediately.
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
            if (_economyManager == null || _bindRegistry == null) return;

            int total = _economyManager.GetCurrentMoney();
            int delta = _economyManager.GetMonthlyIncomeDelta();

            if (total != _lastTotalBudget)
            {
                _bindRegistry.Set("economyManager.totalBudget", total);
                _lastTotalBudget = total;
            }
            if (delta != _lastBudgetDelta)
            {
                _bindRegistry.Set("economyManager.budgetDelta", delta);
                _lastBudgetDelta = delta;
            }
        }
    }
}
