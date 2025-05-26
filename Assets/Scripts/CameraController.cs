using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float zoomSpeed = 2f;
    public float[] zoomLevels = new float[] { 2f, 5f, 10f, 15f, 20f, 30f };
    private int currentZoomLevel = 0;
    private Camera mainCamera;
    public float startZoomLevel = 5f;
    public GridManager gridManager; // Reference to the GridManager if needed

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found.");
        }
        // Set the initial zoom level
        if (zoomLevels.Length > 0)
        {
            currentZoomLevel = Mathf.Clamp(System.Array.IndexOf(zoomLevels, startZoomLevel), 0, zoomLevels.Length - 1);
            mainCamera.orthographicSize = zoomLevels[currentZoomLevel];
        }
        else
        {
            Debug.LogWarning("No zoom levels defined.");
        }
    }

    void Update()
    {
        HandleMovement();
        HandleZoom();
        HandleScrollZoom();
    }

    public void MoveCameraToMapCenter()
    {
      if (gridManager != null)
        {
            Vector3 centerWorldPosition = gridManager.GetWorldPosition(gridManager.width / 2, gridManager.height / 2);
            Vector3 gridCenter = new Vector3(centerWorldPosition.x, centerWorldPosition.y, mainCamera.transform.position.z);
            mainCamera.transform.position = gridCenter;
        }
        else
        {
            Debug.LogWarning("GridManager reference is not set.");
        }
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(horizontal, vertical, 0) * moveSpeed * Time.deltaTime;
        mainCamera.transform.Translate(movement);
    }

    private void ZoomIn()
    {
        if (currentZoomLevel > 0)
        {
            currentZoomLevel--;
            mainCamera.orthographicSize = zoomLevels[currentZoomLevel];
        }
    }

    private void ZoomOut()
    {
        if (currentZoomLevel < zoomLevels.Length - 1)
        {
            currentZoomLevel++;
            mainCamera.orthographicSize = zoomLevels[currentZoomLevel];
        }
    }

    private void HandleScrollZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0)
        {
            ZoomIn();
        }
        else if (scroll < 0)
        {
            ZoomOut();
        }
    }

    private void HandleZoom()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            ZoomIn();
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            ZoomOut();
        }
    }
}
