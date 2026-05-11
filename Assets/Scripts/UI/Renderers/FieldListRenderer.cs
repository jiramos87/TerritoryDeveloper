using System;
using System.Collections.Generic;
using Territory.UI.Registry;
using TMPro;
using UnityEngine;

namespace Territory.UI.Renderers
{
    /// <summary>
    /// Stage 10 stats/budget panel — field-list companion. Subscribes to a string[] bind and
    /// clones <see cref="_prototype"/> into <see cref="_container"/>, pairing array entries
    /// as (key, value, key, value, ...). Cloned rows are pooled to avoid GC churn on rebind.
    /// </summary>
    public class FieldListRenderer : MonoBehaviour
    {
        [SerializeField] private UiBindRegistry _bindRegistry;
        [SerializeField] private RectTransform _container;
        [SerializeField] private GameObject _prototype;
        [SerializeField] private string _bindId;

        private readonly List<GameObject> _pool = new List<GameObject>();
        private IDisposable _sub;

        private void OnEnable()
        {
            if (_bindRegistry == null) _bindRegistry = FindObjectOfType<UiBindRegistry>();
            if (_bindRegistry == null || string.IsNullOrEmpty(_bindId)) return;
            _sub = _bindRegistry.Subscribe<string[]>(_bindId, OnFieldsChanged);
        }

        private void OnDisable()
        {
            _sub?.Dispose();
            _sub = null;
        }

        private void OnFieldsChanged(string[] arr)
        {
            if (_container == null || _prototype == null) return;
            int pairCount = (arr != null) ? arr.Length / 2 : 0;

            // Grow pool as needed.
            while (_pool.Count < pairCount)
            {
                var clone = Instantiate(_prototype, _container);
                clone.name = $"Row_{_pool.Count}";
                _pool.Add(clone);
            }

            for (int i = 0; i < _pool.Count; i++)
            {
                bool active = i < pairCount;
                _pool[i].SetActive(active);
                if (!active) continue;

                var keyTmp = _pool[i].transform.Find("FieldKey")?.GetComponent<TMP_Text>();
                var valTmp = _pool[i].transform.Find("FieldValue")?.GetComponent<TMP_Text>();
                if (keyTmp != null) keyTmp.text = arr[i * 2];
                if (valTmp != null) valTmp.text = arr[i * 2 + 1];
            }
        }
    }
}
