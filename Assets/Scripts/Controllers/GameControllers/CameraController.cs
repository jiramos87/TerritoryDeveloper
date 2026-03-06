
using UnityEngine;
using Territory.Core;

namespace Territory.UI
{
/// <summary>
/// Controls camera movement, zoom, and panning for the isometric grid view.
/// Coordinates with GridManager for viewport bounds.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Base movement speed (no smoothing - immediate response)")]
    public float moveSpeed = 22f;

    [Header("Zoom")]
    public float[] zoomLevels = new float[] { 2f, 5f, 10f, 15f, 20f, 30f };
    [Tooltip("Scroll units required to change one zoom level (higher = more precise, less jumpy)")]
    [Range(0.05f, 0.5f)]
    public float scrollThresholdPerLevel = 0.2f;
    [Tooltip("Min seconds between zoom level changes (avoids touchpad overshooting)")]
    [Range(0.05f, 0.3f)]
    public float zoomStepCooldown = 0.12f;
    [Tooltip("Zoom lerp speed toward target level (smooth transition)")]
    [Range(5f, 30f)]
    public float zoomSmoothSpeed = 18f;

    private int currentZoomLevel = 0;
    private float scrollAccumulator;
    private float lastZoomStepTime = -1f;
    private float targetOrthoSize;
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
        Camera.main.backgroundColor = Color.black;

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
            targetOrthoSize = zoomLevels[currentZoomLevel];
            mainCamera.orthographicSize = targetOrthoSize;
        }
        else
        {
            Debug.LogWarning("CameraController: No zoom levels defined.");
        }
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
            ApplySmoothZoom();
        }
    }

    /// <summary>
    /// Move camera to the center of the map
    /// </summary>
    /// <param name="centerWorldPosition">World position to center the camera on</param>
    public void MoveCameraToMapCenter(Vector3 centerWorldPosition)
    {

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
    }


    private void HandleMovement()
    {
        // Raw axis = no built-in smoothing; immediate response
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 movement = new Vector3(horizontal, vertical, 0).normalized * moveSpeed * Time.deltaTime;
        mainCamera.transform.Translate(movement);
    }

    public void ZoomIn()
    {
        if (currentZoomLevel > 0)
        {
            currentZoomLevel--;
            targetOrthoSize = zoomLevels[currentZoomLevel];
        }
    }

    public void ZoomOut()
    {
        if (currentZoomLevel < zoomLevels.Length - 1)
        {
            currentZoomLevel++;
            targetOrthoSize = zoomLevels[currentZoomLevel];
        }
    }

    private void HandleScrollZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f))
            return;

        scrollAccumulator += scroll;
        float t = Time.time - lastZoomStepTime;
        if (t < zoomStepCooldown)
            return;

        float threshold = scrollThresholdPerLevel;
        if (scrollAccumulator >= threshold)
        {
            int steps = Mathf.FloorToInt(scrollAccumulator / threshold);
            steps = Mathf.Min(steps, currentZoomLevel);
            if (steps > 0)
            {
                currentZoomLevel -= steps;
                targetOrthoSize = zoomLevels[currentZoomLevel];
                scrollAccumulator -= steps * threshold;
                lastZoomStepTime = Time.time;
            }
            else
                scrollAccumulator = 0f;
        }
        else if (scrollAccumulator <= -threshold)
        {
            int steps = Mathf.FloorToInt(-scrollAccumulator / threshold);
            int maxSteps = zoomLevels.Length - 1 - currentZoomLevel;
            steps = Mathf.Min(steps, maxSteps);
            if (steps > 0)
            {
                currentZoomLevel += steps;
                targetOrthoSize = zoomLevels[currentZoomLevel];
                scrollAccumulator += steps * threshold;
                lastZoomStepTime = Time.time;
            }
            else
                scrollAccumulator = 0f;
        }

        scrollAccumulator = Mathf.Clamp(scrollAccumulator, -threshold * 1.5f, threshold * 1.5f);
    }

    private void ApplySmoothZoom()
    {
        if (zoomLevels.Length == 0) return;
        float current = mainCamera.orthographicSize;
        if (Mathf.Approximately(current, targetOrthoSize)) return;
        mainCamera.orthographicSize = Mathf.Lerp(current, targetOrthoSize, zoomSmoothSpeed * Time.deltaTime);
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
}
