using UnityEngine;
using UnityEngine.UI;

namespace Territory.Testing
{
    /// <summary>
    /// Create minimal on-screen <b>TEST-MODE</b> label when <see cref="TestModeSessionState.ActiveThisSession"/> true.
    /// Lives on <c>DontDestroyOnLoad</c> object created by <see cref="TestModeCommandLineBootstrap"/>.
    /// </summary>
    public sealed class TestModeHudDriver : MonoBehaviour
    {
        void Start()
        {
            if (!TestModeSessionState.ActiveThisSession)
                return;

            var canvasGo = new GameObject("TestModeHudCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var textGo = new GameObject("TestModeLabel");
            textGo.transform.SetParent(canvasGo.transform, false);
            var rect = textGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(12f, -8f);
            rect.sizeDelta = new Vector2(400f, 36f);

            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(1f, 0.85f, 0.2f, 1f);
            text.text = "TEST-MODE";
        }
    }
}
