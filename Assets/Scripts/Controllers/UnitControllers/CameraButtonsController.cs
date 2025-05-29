using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CameraButtonsController : MonoBehaviour
{
    [Header("References")]
    public CameraController cameraController;

    [Header("Camera Buttons")]
    public Button zoomInButton;
    public Button zoomOutButton;

    private List<Button> allCameraButtons = new List<Button>();
    private int currentSelectedZoomLevelIndex = 0;
    
    private void Start()
    {
        InitializeButtons();
    }

    private void InitializeButtons()
    {
        allCameraButtons.Clear();

        if (zoomInButton != null) allCameraButtons.Add(zoomInButton);
        if (zoomOutButton != null) allCameraButtons.Add(zoomOutButton);

        Debug.Log($"Initialized {allCameraButtons.Count} camera buttons for state management");
    }

    public void OnZoomInClick()
    {
        Debug.Log("Zoom In button clicked");
        cameraController.ZoomIn();
    }

    public void OnZoomOutClick()
    {
        Debug.Log("Zoom Out button clicked");
        cameraController.ZoomOut();
    }

    public int GetCurrentZoomLevelIndex()
    {
        return currentSelectedZoomLevelIndex;
    }
}