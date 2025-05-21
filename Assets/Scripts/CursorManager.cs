using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public Texture2D cursorTexture;
    public Texture2D bulldozerTexture;
    public Texture2D detailsTexture;
    public Vector2 hotSpot;
    private GameObject previewInstance;
    public GridManager gridManager;

    void Start()
    {
        hotSpot = Vector2.zero;
        Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
    }

    public void SetBullDozerCursor()
    {
        hotSpot = new Vector2(0, bulldozerTexture.height);
        Cursor.SetCursor(bulldozerTexture, hotSpot, CursorMode.Auto);
    }

    public void SetDefaultCursor()
    {
        hotSpot = Vector2.zero;
        Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
    }

    public void SetDetailsCursor()
    {
        hotSpot = Vector2.zero;
        Cursor.SetCursor(detailsTexture, hotSpot, CursorMode.Auto);
    }

    public void ShowBuildingPreview(GameObject buildingPrefab, int buildingSize = 1)
    {
        try {
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
        catch (System.Exception ex) {
            Debug.LogError($"Error in ShowBuildingPreview: {ex.Message}\n{ex.StackTrace}");
        }
    }

    void Update()
    {
        if (previewInstance != null)
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = 0;

            Vector2 gridPosition = gridManager.GetGridPosition(mousePosition);
            
            UIManager uiManager = FindObjectOfType<UIManager>();
            int buildingSize = 1;
            
            if (uiManager != null && uiManager.GetSelectedBuilding() != null)
            {
                buildingSize = uiManager.GetSelectedBuilding().BuildingSize;
            }

            Vector2 worldPosition = gridManager.GetWorldPosition((int)gridPosition.x, (int)gridPosition.y);
            
            if (buildingSize > 1 && buildingSize % 2 == 0)
            {
                worldPosition.x += 0.5f;
            }

            previewInstance.transform.position = worldPosition;
        }
    }

    public void RemovePreview()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
        }
    }
}