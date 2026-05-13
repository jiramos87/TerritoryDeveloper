using UnityEngine;
using UnityEngine.Pool;

namespace Domains.Grid.Services
{
/// <summary>ObjectPool wrapper for tile GameObjects. Pre-warm before InitializeGrid to eliminate per-tile Instantiate allocations.</summary>
public class TilePool : MonoBehaviour
{
    private ObjectPool<GameObject> _pool;
    private GameObject _pooledPrefab;
    private Transform _poolRoot;
    private int _activeCount;

    /// <summary>Count of live (Get-without-Return) tiles.</summary>
    public int ActiveCount => _activeCount;

    void Awake()
    {
        _poolRoot = new GameObject("TilePool_Root").transform;
        _poolRoot.SetParent(transform);
    }

    /// <summary>Allocate count inactive instances of prefab into pool stack.</summary>
    public void PreWarm(int count, GameObject prefab)
    {
        if (prefab == null) { Debug.LogWarning("[TilePool] PreWarm: prefab is null — skipped."); return; }
        _pooledPrefab = prefab;
        _pool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                var go = Instantiate(_pooledPrefab, _poolRoot);
                go.SetActive(false);
                return go;
            },
            actionOnGet: go => { go.SetActive(true); },
            actionOnRelease: go => { go.SetActive(false); go.transform.SetParent(_poolRoot); },
            actionOnDestroy: go => Destroy(go),
            collectionCheck: false,
            defaultCapacity: count,
            maxSize: count * 2
        );
        // pre-warm: fill internal stack then release back
        var temp = new GameObject[count];
        for (int i = 0; i < count; i++)
            temp[i] = _pool.Get();
        for (int i = 0; i < count; i++)
            _pool.Release(temp[i]);
        _activeCount = 0;
    }

    /// <summary>Pop tile from pool, position it, return it active.</summary>
    public GameObject Get(GameObject prefab, Vector3 position)
    {
        if (_pool == null)
        {
            Debug.LogWarning("[TilePool] Get called before PreWarm — falling back to Instantiate.");
            return Instantiate(prefab, position, Quaternion.identity);
        }
        var go = _pool.Get();
        go.transform.position = position;
        _activeCount++;
        return go;
    }

    /// <summary>Return tile to pool.</summary>
    public void Return(GameObject tile)
    {
        if (tile == null) return;
        if (_pool == null) { Destroy(tile); return; }
        _pool.Release(tile);
        if (_activeCount > 0) _activeCount--;
    }
}
}
