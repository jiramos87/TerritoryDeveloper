using UnityEngine;
using UnityEngine.UI;
using Territory.Geography;

namespace Territory.UI
{
/// <summary>
/// Full-screen Canvas overlay that hides the empty-grid flash during CityScene boot.
/// Activates in Awake, deactivates when GeographyManager fires OnGeographyInitialized.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class LoadingVeilController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Assign GeographyManager in Inspector; falls back to FindObjectOfType in Awake.")]
    public GeographyManager geographyManager;

    private Canvas _canvas;

    /// <summary>Forwarded to optional progress-bar widget; no behavior in this task.</summary>
    public float Progress { get; set; }

    void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _canvas.gameObject.SetActive(true);

        if (geographyManager == null)
            geographyManager = Object.FindObjectOfType<GeographyManager>();
    }

    void OnEnable()
    {
        if (geographyManager != null)
            geographyManager.OnGeographyInitialized += OnGeographyInitialized;
    }

    void OnDisable()
    {
        if (geographyManager != null)
            geographyManager.OnGeographyInitialized -= OnGeographyInitialized;
    }

    private void OnGeographyInitialized()
    {
        _canvas.gameObject.SetActive(false);
    }
}
}
