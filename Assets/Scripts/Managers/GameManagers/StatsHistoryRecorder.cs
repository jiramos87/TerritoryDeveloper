using System;
using System.Collections.Generic;
using Territory.Economy;
using UnityEngine;

namespace Territory.Simulation
{
    /// <summary>
    /// Wave B2 (TECH-27085) — monthly aggregate recorder for stats-panel series.
    /// Subscribes to TimeManager monthly tick (day==1 check).
    /// Snapshots population / service-saturation / economy-summary into 3mo + 12mo + all-time ring buffers.
    /// Exposes GetRange(rangeKind, seriesId) + GetCurrentSnapshot(seriesId) for StatsPanelAdapter.
    /// MonoBehaviour; mount in CityScene (Inv #4).
    /// </summary>
    public class StatsHistoryRecorder : MonoBehaviour
    {
        private const int Buf3Mo   = 3;
        private const int Buf12Mo  = 12;
        private const int BufAll   = 120; // 10 years max ring

        [SerializeField] private CityStats _cityStats;
        [SerializeField] private EconomyManager _economyManager;

        // Ring buffers per seriesId.
        private readonly Dictionary<string, float[]> _buf3mo   = new Dictionary<string, float[]>();
        private readonly Dictionary<string, float[]> _buf12mo  = new Dictionary<string, float[]>();
        private readonly Dictionary<string, float[]> _bufAll   = new Dictionary<string, float[]>();
        private readonly Dictionary<string, int>     _head3    = new Dictionary<string, int>();
        private readonly Dictionary<string, int>     _head12   = new Dictionary<string, int>();
        private readonly Dictionary<string, int>     _headAll  = new Dictionary<string, int>();
        private readonly Dictionary<string, int>     _count3   = new Dictionary<string, int>();
        private readonly Dictionary<string, int>     _count12  = new Dictionary<string, int>();
        private readonly Dictionary<string, int>     _countAll = new Dictionary<string, int>();

        // Last snapshot per seriesId for service-rows.
        private readonly Dictionary<string, float> _latest = new Dictionary<string, float>();

        private static readonly string[] AllSeriesIds =
        {
            "population",
            "services", "economy",
            "svc.power", "svc.water", "svc.waste", "svc.police",
            "svc.fire", "svc.health", "svc.education", "svc.parks",
            "svc.transit", "svc.roads", "svc.happiness",
        };

        private void Awake()
        {
            if (_cityStats     == null) _cityStats     = FindObjectOfType<CityStats>();
            if (_economyManager == null) _economyManager = FindObjectOfType<EconomyManager>();

            foreach (var id in AllSeriesIds)
                InitBuffers(id);
        }

        private void InitBuffers(string id)
        {
            _buf3mo[id]   = new float[Buf3Mo];
            _buf12mo[id]  = new float[Buf12Mo];
            _bufAll[id]   = new float[BufAll];
            _head3[id]    = 0; _head12[id] = 0; _headAll[id] = 0;
            _count3[id]   = 0; _count12[id] = 0; _countAll[id] = 0;
            _latest[id]   = 0f;
        }

        /// <summary>Called by TimeManager Update loop when day==1 (monthly tick).</summary>
        public void OnMonthlyTick()
        {
            var snapshot = BuildSnapshot();
            foreach (var kv in snapshot)
                Push(kv.Key, kv.Value);
        }

        private Dictionary<string, float> BuildSnapshot()
        {
            var map = new Dictionary<string, float>();

            float pop = _cityStats != null ? _cityStats.population : 0f;
            map["population"] = pop;

            // Economy summary — projected monthly income as proxy.
            float econ = _economyManager != null ? (float)_economyManager.GetProjectedMonthlyIncome() : 0f;
            map["economy"] = econ;

            // Services saturation stubs (0–1 normalized).
            float svc = 0.5f; // placeholder until service managers expose saturation.
            map["services"] = svc;

            string[] svcIds = { "svc.power", "svc.water", "svc.waste", "svc.police",
                                 "svc.fire", "svc.health", "svc.education", "svc.parks",
                                 "svc.transit", "svc.roads", "svc.happiness" };
            foreach (var id in svcIds)
                map[id] = svc;

            return map;
        }

        private void Push(string id, float value)
        {
            _latest[id] = value;

            // 3mo ring
            if (_buf3mo.TryGetValue(id, out var b3))
            {
                b3[_head3[id]] = value;
                _head3[id] = (_head3[id] + 1) % Buf3Mo;
                if (_count3[id] < Buf3Mo) _count3[id]++;
            }
            // 12mo ring
            if (_buf12mo.TryGetValue(id, out var b12))
            {
                b12[_head12[id]] = value;
                _head12[id] = (_head12[id] + 1) % Buf12Mo;
                if (_count12[id] < Buf12Mo) _count12[id]++;
            }
            // all-time ring
            if (_bufAll.TryGetValue(id, out var ba))
            {
                ba[_headAll[id]] = value;
                _headAll[id] = (_headAll[id] + 1) % BufAll;
                if (_countAll[id] < BufAll) _countAll[id]++;
            }
        }

        /// <summary>Returns ordered float[] for the given range and series. Empty when no data yet.</summary>
        public float[] GetRange(string rangeKind, string seriesId)
        {
            switch (rangeKind)
            {
                case "3mo":      return ExtractOrdered(_buf3mo, _head3, _count3, seriesId, Buf3Mo);
                case "12mo":     return ExtractOrdered(_buf12mo, _head12, _count12, seriesId, Buf12Mo);
                case "all-time": return ExtractOrdered(_bufAll, _headAll, _countAll, seriesId, BufAll);
                default:
                    Debug.LogWarning($"[StatsHistoryRecorder] Unknown rangeKind '{rangeKind}'");
                    return Array.Empty<float>();
            }
        }

        /// <summary>Returns latest snapshot value for service-rows (non-series read).</summary>
        public float GetCurrentSnapshot(string seriesId)
        {
            return _latest.TryGetValue(seriesId, out var v) ? v : 0f;
        }

        private static float[] ExtractOrdered(
            Dictionary<string, float[]> bufs,
            Dictionary<string, int> heads,
            Dictionary<string, int> counts,
            string id, int cap)
        {
            if (!bufs.TryGetValue(id, out var buf)) return Array.Empty<float>();
            int count = counts.TryGetValue(id, out var c) ? c : 0;
            if (count == 0) return Array.Empty<float>();

            int head = heads.TryGetValue(id, out var h) ? h : 0;
            // Ring buffer oldest-first ordered read.
            var result = new float[count];
            int startIdx = (head - count + cap) % cap;
            for (int i = 0; i < count; i++)
                result[i] = buf[(startIdx + i) % cap];
            return result;
        }
    }
}
