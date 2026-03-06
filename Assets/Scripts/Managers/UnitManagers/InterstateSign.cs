using UnityEngine;
using Territory.Geography;

namespace Territory.Roads
{
/// <summary>
/// Visual component for a highway sign at the border of the map.
/// Shows the destination city name, population hint, and directional arrow.
/// Rendered in world-space (not UI Canvas) so it integrates with isometric sorting.
/// </summary>
public class InterstateSign : MonoBehaviour
{
    [Header("Child References (set in prefab)")]
    [SerializeField] private SpriteRenderer signBackground;
    [SerializeField] private TextMesh cityNameText;
    [SerializeField] private TextMesh categoryText;

    [Header("Arrow sprites (set in prefab, enable one at a time)")]
    [SerializeField] private GameObject arrowNorth;
    [SerializeField] private GameObject arrowSouth;
    [SerializeField] private GameObject arrowEast;
    [SerializeField] private GameObject arrowWest;

    [Header("Text position (local offset from sign center, applied in Initialize)")]
    [Tooltip("Local position of city name so it sits inside the sign background.")]
    [SerializeField] private Vector3 cityNameLocalOffset = new Vector3(0f, 0.02f, 0f);
    [Tooltip("Local position of category line so it sits inside the sign background.")]
    [SerializeField] private Vector3 categoryLocalOffset = new Vector3(0f, -0.02f, 0f);

    [Header("Text size (world-space, applied in Initialize)")]
    [Tooltip("Character size for the city name. Category uses a smaller size so it fits inside the sign.")]
    [SerializeField] private float cityNameCharacterSize = 0.14f;
    [SerializeField] private float categoryCharacterSize = 0.08f;
    [SerializeField] private int cityNameFontSize = 28;
    [SerializeField] private int categoryFontSize = 18;

    [Header("Camera distance scaling")]
    [Tooltip("Scale sign by distance to camera so it stays readable at all zoom levels.")]
    [SerializeField] private bool scaleByCameraDistance = true;
    [SerializeField] private float distanceScaleFactor = 0.15f;
    [SerializeField] private float minScale = 0.35f;
    [SerializeField] private float maxScale = 2f;

    private string destinationCityName;
    private int border;
    private Camera _cachedCamera;

    void LateUpdate()
    {
        if (!scaleByCameraDistance) return;
        if (_cachedCamera == null) _cachedCamera = Camera.main;
        if (_cachedCamera == null) return;
        float distance = Vector3.Distance(_cachedCamera.transform.position, transform.position);
        float scale = Mathf.Clamp(distance * distanceScaleFactor, minScale, maxScale);
        transform.localScale = Vector3.one * scale;
    }

    public void Initialize(
        string cityName,
        int population,
        TerritoryData.CityCategory category,
        int borderSide,
        int baseSortingOrder)
    {
        destinationCityName = cityName;
        border = borderSide;

        if (cityNameText != null)
        {
            cityNameText.transform.localPosition = cityNameLocalOffset;
            cityNameText.text = cityName;
            cityNameText.characterSize = cityNameCharacterSize;
            cityNameText.fontSize = cityNameFontSize;
            MeshRenderer nameRenderer = cityNameText.GetComponent<MeshRenderer>();
            if (nameRenderer != null)
                nameRenderer.sortingOrder = baseSortingOrder + 21;
        }

        if (categoryText != null)
        {
            categoryText.transform.localPosition = categoryLocalOffset;
            string popDisplay = FormatPopulation(population);
            categoryText.text = GetCategoryLabel(category) + " - Pop. " + popDisplay;
            categoryText.characterSize = categoryCharacterSize;
            categoryText.fontSize = categoryFontSize;
            MeshRenderer catRenderer = categoryText.GetComponent<MeshRenderer>();
            if (catRenderer != null)
                catRenderer.sortingOrder = baseSortingOrder + 21;
        }

        if (signBackground != null)
            signBackground.sortingOrder = baseSortingOrder + 20;

        if (arrowNorth != null) arrowNorth.SetActive(border == 1);
        if (arrowSouth != null) arrowSouth.SetActive(border == 0);
        if (arrowEast != null) arrowEast.SetActive(border == 3);
        if (arrowWest != null) arrowWest.SetActive(border == 2);
    }

    private string GetCategoryLabel(TerritoryData.CityCategory cat)
    {
        switch (cat)
        {
            case TerritoryData.CityCategory.Village: return "Village";
            case TerritoryData.CityCategory.Town: return "Town";
            case TerritoryData.CityCategory.City: return "City";
            case TerritoryData.CityCategory.Metropolis: return "Metropolis";
            default: return "";
        }
    }

    private string FormatPopulation(int pop)
    {
        if (pop >= 1000000) return (pop / 1000000f).ToString("F1") + "M";
        if (pop >= 1000) return (pop / 1000f).ToString("F0") + "K";
        return pop.ToString();
    }
}
}
