
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float zoomSpeed = 2f;
    public float[] zoomLevels = new float[] { 2f, 5f, 10f, 15f, 20f, 30f };
    private int currentZoomLevel = 0;
    private Camera mainCamera;
    public float startZoomLevel = 5f;
    public GridManager gridManager;
    public CameraButtonsController cameraButtonsController;

    /// <summary>
    /// Initialize camera early in the lifecycle
    /// </summary>
    void Awake()
    {
        InitializeCamera();
    }

    /// <summary>
    /// Initialize camera and zoom settings
    /// </summary>
    private void InitializeCamera()
    {
        // Get main camera reference
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            // If Camera.main doesn't work, try to find camera on this GameObject
            mainCamera = GetComponent<Camera>();
        }

        if (mainCamera == null)
        {
            // As last resort, find any camera in the scene
            mainCamera = FindObjectOfType<Camera>();
        }

        if (mainCamera == null)
        {
            Debug.LogError("CameraController: No camera found! Make sure there's a camera in the scene with MainCamera tag.");
            return;
        }

        // Set the initial zoom level
        if (zoomLevels.Length > 0)
        {
            currentZoomLevel = Mathf.Clamp(System.Array.IndexOf(zoomLevels, startZoomLevel), 0, zoomLevels.Length - 1);
            if (currentZoomLevel == -1) // startZoomLevel not found in array
            {
                currentZoomLevel = 0;
            }
            mainCamera.orthographicSize = zoomLevels[currentZoomLevel];
        }
        else
        {
            Debug.LogWarning("CameraController: No zoom levels defined.");
        }

        Debug.Log("CameraController: Camera initialized successfully");
    }

    void Start()
    {
        // Ensure camera is initialized (should already be done in Awake)
        if (mainCamera == null)
        {
            InitializeCamera();
        }
    }

    void Update()
    {
        // Only handle input if camera is properly initialized
        if (mainCamera != null)
        {
            HandleMovement();
            HandleZoom();
            HandleScrollZoom();
        }
    }

    /// <summary>
    /// Move camera to the center of the map
    /// </summary>
    /// <param name="centerWorldPosition">World position to center the camera on</param>
    public void MoveCameraToMapCenter(Vector3 centerWorldPosition)
    {
        Debug.Log("MoveCameraToMapCenter centerWorldPosition: " + centerWorldPosition);

        // Ensure camera is initialized before moving it
        if (mainCamera == null)
        {
            InitializeCamera();
        }

        if (mainCamera == null)
        {
            Debug.LogError("CameraController: Cannot move camera - mainCamera is still null!");
            return;
        }

        // Move camera to center position while preserving Z coordinate
        Vector3 gridCenter = new Vector3(
            centerWorldPosition.x,
            centerWorldPosition.y,
            mainCamera.transform.position.z
        );

        mainCamera.transform.position = gridCenter;
        Debug.Log("CameraController: Camera moved to " + gridCenter);
    }


    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(horizontal, vertical, 0) * moveSpeed * Time.deltaTime;
        mainCamera.transform.Translate(movement);
    }

    public void ZoomIn()
    {
        if (currentZoomLevel > 0)
        {
            currentZoomLevel--;
            mainCamera.orthographicSize = zoomLevels[currentZoomLevel];
        }
    }

    public void ZoomOut()
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
