using UnityEngine;
using UnityEngine.UI;
using Territory.Economy;

namespace Territory.UI
{
/// <summary>
/// UI toggle that enables/disables automatic city growth simulation in CityStats.
/// </summary>
public class SimulateGrowthToggle : MonoBehaviour
{
    public CityStats cityStats;
    public Button toggleButton;
    public Image buttonImage;
    [Header("AUTO ON = simulación activa (luz verde)")]
    public Color colorWhenOn = new Color(0.2f, 0.85f, 0.2f);
    [Header("AUTO OFF = simulación detenida")]
    public Color colorWhenOff = new Color(0.9f, 0.25f, 0.25f);
    [Tooltip("Escala del botón cuando está ON (aspecto presionado). 1 = sin cambio.")]
    public float scaleWhenOn = 0.96f;

    private RectTransform _rectTransform;
    private Vector3 _normalScale = Vector3.one;

    void Start()
    {
        if (cityStats == null)
            cityStats = FindObjectOfType<CityStats>();
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(OnToggleClick);
            _rectTransform = toggleButton.GetComponent<RectTransform>();
            if (_rectTransform != null)
                _normalScale = _rectTransform.localScale;
        }
        RefreshVisual();
    }

    void OnDestroy()
    {
        if (toggleButton != null)
            toggleButton.onClick.RemoveListener(OnToggleClick);
    }

    void OnToggleClick()
    {
        if (cityStats == null) return;
        cityStats.simulateGrowth = !cityStats.simulateGrowth;
        RefreshVisual();
    }

    void RefreshVisual()
    {
        bool on = cityStats != null && cityStats.simulateGrowth;
        if (buttonImage != null)
            buttonImage.color = on ? colorWhenOn : colorWhenOff;
        if (_rectTransform != null)
            _rectTransform.localScale = on ? _normalScale * scaleWhenOn : _normalScale;
    }

    void Update()
    {
        RefreshVisual();
    }
}
}
