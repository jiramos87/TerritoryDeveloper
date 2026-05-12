using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Territory.Utilities;
using Domains.Notifications.Services;

namespace Territory.UI
{
/// <summary>
/// Hub: in-game notifications. SerializeField UI refs + lazy-init (guardrail #8) + coroutines.
/// Queue/state delegated to NotificationService (_svc). Stage 5.5 THIN.
/// </summary>
public class GameNotificationManager : MonoBehaviour
{
    public static GameNotificationManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private GameObject notificationPanel;
    [Header("Notification Settings")]
    [SerializeField] private float notificationDuration = 3.0f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private int maxQueueSize = 5;
    [Header("SFX — TECH-15225")]
    [SerializeField] private AudioClip sfxNotificationShow;
    [SerializeField] private AudioClip sfxErrorFeedback;
    [SerializeField] private AudioClip sfxSuccess;
    [SerializeField] private AudioClip sfxWarning;
    [SerializeField] private AudioClip sfxMilestone;
    // DS-* token audit — TECH-15227.
    [Header("DS Tokens — TECH-15227")]
    [Tooltip("Assign UiTheme SO — ds-* palette entries replace ad-hoc Color literals below in Stage N token-bake.")]
    [SerializeField] private UiTheme _dsTheme;
    [Header("Message Categories")]
    [SerializeField] private Color errorColor   = Color.red;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color infoColor    = Color.white;
    [SerializeField] private Color successColor = Color.green;

    public enum NotificationType { Info, Success, Warning, Error,
        /// <summary>Milestone tier — sticky gold-pulse toast (T9.0.5).</summary>
        Milestone }

    public struct NotificationMessage
    {
        public string message; public NotificationType type; public float duration;
        public bool sticky; public string subtitle; public Vector2Int? cellRef;
        public NotificationMessage(string msg, NotificationType t, float dur,
            bool sticky = false, string subtitle = null, Vector2Int? cellRef = null)
        { message = msg; type = t; duration = dur; this.sticky = sticky;
          this.subtitle = subtitle; this.cellRef = cellRef; }
    }

    private NotificationService _svc = new NotificationService();
    private Coroutine currentDisplayCoroutine;
    private CanvasGroup notificationCanvasGroup;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(transform.root.gameObject); _svc.WireConfig(maxQueueSize, notificationDuration); InitializeComponents(); }
        else Destroy(gameObject);
    }

    // ── Lazy-init (guardrail #8) ─────────────────────────────────────────────────
    private void InitializeComponents()
    {
        if (notificationPanel == null || notificationText == null) LazyCreateNotificationUi();
        if (notificationPanel == null) { Debug.LogError("GameNotificationManager: no panel."); return; }
        notificationCanvasGroup = notificationPanel.GetComponent<CanvasGroup>() ?? notificationPanel.AddComponent<CanvasGroup>();
        notificationPanel.SetActive(false); notificationCanvasGroup.alpha = 0f;
    }

    private void LazyCreateNotificationUi()
    {
        Canvas canvas = null;
        foreach (var c in Object.FindObjectsOfType<Canvas>())
            if (c.isActiveAndEnabled && c.renderMode != RenderMode.WorldSpace) { canvas = c; break; }
        if (canvas == null) return;
        if (notificationPanel == null)
        {
            var go = new GameObject("NotificationPanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f,1f); rt.anchorMax = new Vector2(0.5f,1f);
            rt.pivot = new Vector2(0.5f,1f); rt.anchoredPosition = new Vector2(0f,-32f); rt.sizeDelta = new Vector2(480f,56f);
            var img = go.GetComponent<Image>(); img.color = new Color(0f,0f,0f,0.65f); img.raycastTarget = false;
            notificationPanel = go;
        }
        if (notificationText == null)
        {
            var tgo = new GameObject("NotificationText", typeof(RectTransform));
            tgo.transform.SetParent(notificationPanel.transform, false);
            var trt = (RectTransform)tgo.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(12f,8f); trt.offsetMax = new Vector2(-12f,-8f);
            var tmp = tgo.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center; tmp.fontSize = 18f;
            tmp.color = infoColor; tmp.raycastTarget = false; notificationText = tmp;
        }
    }

    // ── Public API ───────────────────────────────────────────────────────────────
    public void PostInfo(string m)    => PostInternal(m, NotificationType.Info);
    public void PostSuccess(string m) => PostInternal(m, NotificationType.Success);
    public void PostWarning(string m) => PostInternal(m, NotificationType.Warning);
    public void PostError(string m)   => PostInternal(m, NotificationType.Error);

    public void PostNotification(string message, NotificationType type, float duration)
    {
        if (string.IsNullOrEmpty(message)) { DebugHelper.LogWarning("GameNotificationManager: empty message"); return; }
        bool ready = notificationPanel != null && notificationText != null;
        if (_svc.EnqueueTyped(message, type, duration, ready)) StartNextNotification();
    }
    public void PostNotification(string message, NotificationType type) => PostNotification(message, type, notificationDuration);

    public void PostMilestone(string title, string subtitle = null, Vector2Int? cellRef = null)
    {
        if (notificationPanel == null || notificationText == null) return;
        if (_svc.Enqueue(_svc.BuildMilestone(title, subtitle, cellRef), refsReady: true)) StartNextNotification();
    }

    public void ClearNotificationQueue()
    {
        _svc.Clear();
        if (currentDisplayCoroutine != null) { StopCoroutine(currentDisplayCoroutine); currentDisplayCoroutine = null; }
        if (notificationPanel != null) notificationPanel.SetActive(false);
    }
    public int GetQueueCount() => _svc.GetQueueCount();

    public void PostInsufficientFunds(string itemType, int cost) => PostError($"Insufficient funds for {itemType}. Need ${cost:N0}");
    public void PostBuildingPlacementError(string r = "area is not available") => PostWarning($"Cannot place building: {r}");
    public void PostBuildingConstructed(string name) => PostSuccess($"{name} constructed successfully");
    public void PostZoneGrowth(string zone, int count) => PostInfo($"{count} new {zone} building{(count > 1 ? "s" : "")} constructed");
    public void PostEconomicEvent(string desc, bool positive) => PostNotification(desc, positive ? NotificationType.Success : NotificationType.Warning);

    public void OnCardClicked(NotificationMessage notification)
    {
        if (notification.cellRef.HasValue) { var cc = FindObjectOfType<Territory.UI.CameraController>(); if (cc != null) cc.PanCameraTo(notification.cellRef.Value); }
        if (currentDisplayCoroutine != null) { StopCoroutine(currentDisplayCoroutine); currentDisplayCoroutine = null; }
        if (notificationPanel != null) notificationPanel.SetActive(false);
        _svc.IsDisplayingMessage = false; StartNextNotification();
    }

    // ── Coroutine internals ──────────────────────────────────────────────────────
    private void PostInternal(string message, NotificationType type)
    {
        if (string.IsNullOrEmpty(message)) { DebugHelper.LogWarning("GameNotificationManager: empty message"); return; }
        bool ready = notificationPanel != null && notificationText != null;
        if (_svc.EnqueueTyped(message, type, notificationDuration, ready)) StartNextNotification();
    }

    private void StartNextNotification()
    {
        if (!_svc.HasPending) { _svc.IsDisplayingMessage = false; return; }
        _svc.IsDisplayingMessage = true;
        if (!_svc.TryDequeueNext(out var next)) { _svc.IsDisplayingMessage = false; return; }
        if (currentDisplayCoroutine != null) StopCoroutine(currentDisplayCoroutine);
        currentDisplayCoroutine = StartCoroutine(DisplayNotificationCoroutine(next));
    }

    private IEnumerator DisplayNotificationCoroutine(NotificationMessage n)
    {
        SetupNotificationDisplay(n);
        notificationPanel.SetActive(true);
        UiSfxPlayer.Play(n.type == NotificationType.Error ? sfxErrorFeedback : sfxNotificationShow);
        yield return StartCoroutine(FadeCanvasGroup(notificationCanvasGroup, 0f, 1f, fadeInDuration));
        yield return new WaitForSeconds(n.duration);
        yield return StartCoroutine(FadeCanvasGroup(notificationCanvasGroup, 1f, 0f, fadeOutDuration));
        notificationPanel.SetActive(false); StartNextNotification();
    }

    private void SetupNotificationDisplay(NotificationMessage n)
    {
        notificationText.text = n.message;
        switch (n.type)
        {
            case NotificationType.Success:   notificationText.color = successColor; break;
            case NotificationType.Warning:   notificationText.color = warningColor; break;
            case NotificationType.Error:     notificationText.color = errorColor;   break;
            case NotificationType.Milestone: notificationText.color = new Color(1f, 0.843f, 0f, 1f); break;
            default:                         notificationText.color = infoColor;    break;
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur) { cg.alpha = Mathf.Lerp(from, to, t/dur); t += Time.deltaTime; yield return null; }
        cg.alpha = to;
    }
}
}
