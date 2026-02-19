using UnityEngine;
using UnityEngine.EventSystems;

public class CursorManager : MonoBehaviour
{
    public Texture2D cursorTexture;
    public Texture2D bulldozerTexture;
    public Texture2D detailsTexture;
    public Vector2 hotSpot;
    private GameObject previewInstance;
    public GridManager gridManager;
    private GameObject currentRoadGhostPrefab;
    private Texture2D activeCursorTexture;
    private Vector2 activeCursorHotSpot;
    private bool isOverUI;
    private Texture2D scaledBulldozerTexture;

    void Start()
    {
        hotSpot = Vector2.zero;
        activeCursorTexture = null;
        activeCursorHotSpot = Vector2.zero;
        isOverUI = false;
        Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
    }

    public void SetBullDozerCursor()
    {
        Texture2D targetTexture = GetScaledBulldozerTexture();
        if (targetTexture == null)
        {
            return;
        }

        activeCursorTexture = targetTexture;
        activeCursorHotSpot = new Vector2(0, targetTexture.height);

        if (!IsPointerOverUI())
        {
            Cursor.SetCursor(activeCursorTexture, activeCursorHotSpot, CursorMode.Auto);
        }
    }

    public void SetDefaultCursor()
    {
        hotSpot = Vector2.zero;
        activeCursorTexture = null;
        activeCursorHotSpot = Vector2.zero;
        Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
    }

    public void SetDetailsCursor()
    {
        activeCursorTexture = detailsTexture;
        activeCursorHotSpot = Vector2.zero;

        if (!IsPointerOverUI())
        {
            Cursor.SetCursor(activeCursorTexture, activeCursorHotSpot, CursorMode.Auto);
        }
    }

    public void ShowBuildingPreview(GameObject buildingPrefab, int buildingSize = 1)
    {
        try
        {
            currentRoadGhostPrefab = null;
            if (previewInstance != null)
            {
                Destroy(previewInstance);
            }

            // Instantiate a preview of the buildingPrefab
            previewInstance = Instantiate(buildingPrefab);

            // Get the SpriteRenderer component
            SpriteRenderer spriteRenderer = previewInstance.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = previewInstance.GetComponentInChildren<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1, 1, 1, 0.5f); // Set transparency
                // Set a high sorting order to ensure preview appears on top
                spriteRenderer.sortingOrder = 10000;
            }
            else
            {
                Debug.LogError("No SpriteRenderer found on building prefab or its children!");
            }

            // Optionally disable colliders or other components
            Collider2D[] colliders = previewInstance.GetComponentsInChildren<Collider2D>();
            foreach (var col in colliders)
            {
                col.enabled = false; // Disable collision for the preview
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error in ShowBuildingPreview: {ex.Message}\n{ex.StackTrace}");
        }
    }

    void Update()
    {
        if (previewInstance != null)
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePosition2 = new Vector2(mousePosition.x, mousePosition.y);

            Vector2 gridPosition = gridManager.GetGridPositionWithHeight(mousePosition2);

            if (!gridManager.IsValidGridPosition(gridPosition))
            {
                previewInstance.SetActive(false);
                UpdateCursorForUIHover();
                return;
            }

            previewInstance.SetActive(true);

            UIManager uiManager = FindObjectOfType<UIManager>();

            if (uiManager != null && uiManager.GetSelectedZoneType() == Zone.ZoneType.Road && gridManager.roadManager != null)
            {
                gridManager.roadManager.GetRoadGhostPreviewForCell(gridPosition, out GameObject roadPrefab, out Vector2 worldPos, out int sortingOrder);
                if (roadPrefab != currentRoadGhostPrefab)
                {
                    currentRoadGhostPrefab = roadPrefab;
                    Destroy(previewInstance);
                    previewInstance = Instantiate(roadPrefab);
                    SpriteRenderer sr = previewInstance.GetComponent<SpriteRenderer>();
                    if (sr == null) sr = previewInstance.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null) sr.color = new Color(1, 1, 1, 0.5f);
                    foreach (var col in previewInstance.GetComponentsInChildren<Collider2D>())
                        col.enabled = false;
                }
                previewInstance.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
                SpriteRenderer[] renderers = previewInstance.GetComponentsInChildren<SpriteRenderer>();
                foreach (SpriteRenderer sr in renderers)
                    if (sr != null) sr.sortingOrder = sortingOrder;
            }
            else
            {
                currentRoadGhostPrefab = null;
                int buildingSize = 1;
                if (uiManager != null && uiManager.GetSelectedBuilding() != null)
                    buildingSize = uiManager.GetSelectedBuilding().BuildingSize;
                Cell cell = gridManager.GetCell((int)gridPosition.x, (int)gridPosition.y);
                if (cell == null)
                {
                    previewInstance.SetActive(false);
                }
                else
                {
                    Vector2 newWorldPos = gridManager.GetBuildingPlacementWorldPosition(gridPosition, buildingSize);
                    previewInstance.transform.position = newWorldPos;
                }
            }
        }

        UpdateCursorForUIHover();
    }

    private void UpdateCursorForUIHover()
    {
        bool overUI = IsPointerOverUI();
        if (overUI == isOverUI)
        {
            return;
        }

        isOverUI = overUI;
        if (isOverUI)
        {
            Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
            return;
        }

        if (activeCursorTexture != null)
        {
            Cursor.SetCursor(activeCursorTexture, activeCursorHotSpot, CursorMode.Auto);
        }
        else
        {
            Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
        }
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private Texture2D GetScaledBulldozerTexture()
    {
        if (bulldozerTexture == null)
        {
            return null;
        }

        int targetWidth = bulldozerTexture.width / 2;
        int targetHeight = bulldozerTexture.height / 2;
        if (targetWidth <= 0 || targetHeight <= 0)
        {
            return bulldozerTexture;
        }

        if (scaledBulldozerTexture != null
            && scaledBulldozerTexture.width == targetWidth
            && scaledBulldozerTexture.height == targetHeight)
        {
            return scaledBulldozerTexture;
        }

        if (scaledBulldozerTexture != null)
        {
            Destroy(scaledBulldozerTexture);
        }

        scaledBulldozerTexture = ScaleTexture(bulldozerTexture, targetWidth, targetHeight);
        return scaledBulldozerTexture;
    }

    private Texture2D ScaleTexture(Texture2D source, int newWidth, int newHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        Graphics.Blit(source, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D result = new Texture2D(newWidth, newHeight);
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }
    public void RemovePreview()
    {
        currentRoadGhostPrefab = null;
        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
        }
    }
}
