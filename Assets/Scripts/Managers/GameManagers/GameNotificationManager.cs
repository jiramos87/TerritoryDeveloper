using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Territory.Utilities;

namespace Territory.UI
{
/// <summary>
/// Manage in-game notifications for game logic events. Persistent UI message
/// display → player feedback on actions + system states.
/// </summary>
public class GameNotificationManager : MonoBehaviour
{
    // Singleton instance
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

    // DS-* token audit — TECH-15227: notification surface.
    // errorColor → ds-accent-negative; warningColor → ds-accent-warning;
    // infoColor → ds-text-primary; successColor → ds-accent-positive.
    // Stage 9.5 marks ad-hoc Color literals for migration to UiTheme palette entries.
    [Header("DS Tokens — TECH-15227")]
    [Tooltip("Assign UiTheme SO — ds-* palette entries replace ad-hoc Color literals below in Stage N token-bake.")]
    [SerializeField] private UiTheme _dsTheme;

    [Header("Message Categories")]
    [SerializeField] private Color errorColor = Color.red;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color infoColor = Color.white;
    [SerializeField] private Color successColor = Color.green;


    // Notification queue and state management
    private Queue<NotificationMessage> messageQueue = new Queue<NotificationMessage>();
    private bool isDisplayingMessage = false;
    private Coroutine currentDisplayCoroutine;
    private CanvasGroup notificationCanvasGroup;

    /// <summary>
    /// Notification types with styling.
    /// </summary>
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error,
        /// <summary>Milestone tier — sticky gold-pulse toast (T9.0.5).</summary>
        Milestone
    }

    /// <summary>
    /// Queued notification message struct.
    /// </summary>
    public struct NotificationMessage
    {
        public string message;
        public NotificationType type;
        public float duration;
        public bool sticky;
        public string subtitle;
        public Vector2Int? cellRef;

        public NotificationMessage(string message, NotificationType type, float duration,
            bool sticky = false, string subtitle = null, Vector2Int? cellRef = null)
        {
            this.message = message;
            this.type = type;
            this.duration = duration;
            this.sticky = sticky;
            this.subtitle = subtitle;
            this.cellRef = cellRef;
        }
    }

    // Sticky queue (always rendered above non-sticky). Non-sticky uses messageQueue.
    private Queue<NotificationMessage> stickyQueue = new Queue<NotificationMessage>();

    void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad only works for root GameObjects
            DontDestroyOnLoad(transform.root.gameObject);
            InitializeComponents();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Init UI components + verify setup. Lazy-creates panel + text child under active Canvas
    /// when SerializeFields are unassigned (survives baked HUD reroots / domain reloads).
    /// </summary>
    private void InitializeComponents()
    {
        if (notificationPanel == null || notificationText == null)
        {
            LazyCreateNotificationUi();
        }

        if (notificationPanel == null)
        {
            Debug.LogError("GameNotificationManager: failed to lazy-create notificationPanel (no active Canvas in scene).");
            return;
        }

        // Get or add CanvasGroup for fade effects
        notificationCanvasGroup = notificationPanel.GetComponent<CanvasGroup>();
        if (notificationCanvasGroup == null)
        {
            notificationCanvasGroup = notificationPanel.AddComponent<CanvasGroup>();
        }

        // Start with notifications hidden
        notificationPanel.SetActive(false);
        notificationCanvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Build a hidden notification panel + text child under the first active Canvas in the scene.
    /// </summary>
    private void LazyCreateNotificationUi()
    {
        Canvas canvas = null;
        foreach (var c in Object.FindObjectsOfType<Canvas>())
        {
            if (c.isActiveAndEnabled && c.renderMode != RenderMode.WorldSpace)
            {
                canvas = c;
                break;
            }
        }
        if (canvas == null) return;

        if (notificationPanel == null)
        {
            var panelGo = new GameObject("NotificationPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvas.transform, worldPositionStays: false);
            var rt = (RectTransform)panelGo.transform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -32f);
            rt.sizeDelta = new Vector2(480f, 56f);
            var img = panelGo.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.65f);
            img.raycastTarget = false;
            notificationPanel = panelGo;
        }

        if (notificationText == null)
        {
            var textGo = new GameObject("NotificationText", typeof(RectTransform));
            textGo.transform.SetParent(notificationPanel.transform, worldPositionStays: false);
            var trt = (RectTransform)textGo.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(12f, 8f);
            trt.offsetMax = new Vector2(-12f, -8f);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 18f;
            tmp.color = infoColor;
            tmp.raycastTarget = false;
            notificationText = tmp;
        }
    }

    /// <summary>
    /// Post info notification.
    /// </summary>
    /// <param name="message">Message to display.</param>
    public void PostInfo(string message)
    {
        PostNotification(message, NotificationType.Info);
    }

    /// <summary>
    /// Post success notification.
    /// </summary>
    /// <param name="message">Message to display.</param>
    public void PostSuccess(string message)
    {
        PostNotification(message, NotificationType.Success);
    }

    /// <summary>
    /// Post warning notification.
    /// </summary>
    /// <param name="message">Message to display.</param>
    public void PostWarning(string message)
    {
        PostNotification(message, NotificationType.Warning);
    }

    /// <summary>
    /// Post error notification.
    /// </summary>
    /// <param name="message">Message to display.</param>
    public void PostError(string message)
    {
        PostNotification(message, NotificationType.Error);
    }

    /// <summary>
    /// Post notification with custom duration.
    /// </summary>
    /// <param name="message">Message to display.</param>
    /// <param name="type">Notification type.</param>
    /// <param name="customDuration">Display duration (seconds).</param>
    public void PostNotification(string message, NotificationType type, float customDuration)
    {
        // Skip silently when UI refs missing — avoids NRE in Awake propagating into game logic event paths.
        if (notificationPanel == null || notificationText == null)
        {
            return;
        }

        // Validate input
        if (string.IsNullOrEmpty(message))
        {
            DebugHelper.LogWarning("GameNotificationManager: Attempted to post empty notification message");
            return;
        }

        // Create notification message
        NotificationMessage notification = new NotificationMessage(message, type, customDuration);

        // Add to queue (remove oldest non-sticky if queue is full)
        if (messageQueue.Count >= maxQueueSize)
        {
            AgeOutOldestNonSticky();
            DebugHelper.LogWarning("GameNotificationManager: Message queue full, removing oldest message");
        }

        messageQueue.Enqueue(notification);

        // Start displaying if not already doing so
        if (!isDisplayingMessage)
        {
            StartNextNotification();
        }
    }

    /// <summary>
    /// Post notification with default duration.
    /// </summary>
    /// <param name="message">Message to display.</param>
    /// <param name="type">Notification type.</param>
    public void PostNotification(string message, NotificationType type)
    {
        PostNotification(message, type, notificationDuration);
    }

    /// <summary>
    /// Start displaying next queued notification. Sticky queue drains before non-sticky.
    /// </summary>
    private void StartNextNotification()
    {
        if (stickyQueue.Count == 0 && messageQueue.Count == 0)
        {
            isDisplayingMessage = false;
            return;
        }

        isDisplayingMessage = true;
        // Sticky always wins over non-sticky.
        NotificationMessage nextMessage = stickyQueue.Count > 0
            ? stickyQueue.Dequeue()
            : messageQueue.Dequeue();

        if (currentDisplayCoroutine != null)
        {
            StopCoroutine(currentDisplayCoroutine);
        }

        currentDisplayCoroutine = StartCoroutine(DisplayNotificationCoroutine(nextMessage));
    }

    /// <summary>
    /// Coroutine → full notification display lifecycle.
    /// </summary>
    /// <param name="notification">Notification to display.</param>
    private IEnumerator DisplayNotificationCoroutine(NotificationMessage notification)
    {
        // Setup message content and styling
        SetupNotificationDisplay(notification);

        // Fade in + panel-show SFX (TECH-15225).
        notificationPanel.SetActive(true);
        UiSfxPlayer.Play(notification.type == NotificationType.Error ? sfxErrorFeedback : sfxNotificationShow);
        yield return StartCoroutine(FadeCanvasGroup(notificationCanvasGroup, 0f, 1f, fadeInDuration));

        // Display duration
        yield return new WaitForSeconds(notification.duration);

        // Fade out
        yield return StartCoroutine(FadeCanvasGroup(notificationCanvasGroup, 1f, 0f, fadeOutDuration));
        notificationPanel.SetActive(false);

        // Start next notification
        StartNextNotification();
    }

    /// <summary>
    /// Configure display per message type + content.
    /// </summary>
    /// <param name="notification">Notification to configure.</param>
    private void SetupNotificationDisplay(NotificationMessage notification)
    {
        // Set message text
        notificationText.text = notification.message;

        // Set color based on notification type
        switch (notification.type)
        {
            case NotificationType.Info:
                notificationText.color = infoColor;
                break;
            case NotificationType.Success:
                notificationText.color = successColor;
                break;
            case NotificationType.Warning:
                notificationText.color = warningColor;
                break;
            case NotificationType.Error:
                notificationText.color = errorColor;
                break;
            case NotificationType.Milestone:
                // Gold-pulse milestone — #FFD700.
                notificationText.color = new Color(1f, 0.843f, 0f, 1f);
                break;
        }
    }

    /// <summary>
    /// Coroutine → fade CanvasGroup alpha.
    /// </summary>
    /// <param name="canvasGroup">CanvasGroup to fade.</param>
    /// <param name="startAlpha">Start alpha.</param>
    /// <param name="endAlpha">End alpha.</param>
    /// <param name="duration">Fade duration.</param>
    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            canvasGroup.alpha = alpha;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
    }

    /// <summary>
    /// Clear all pending queued notifications (sticky + non-sticky).
    /// </summary>
    public void ClearNotificationQueue()
    {
        messageQueue.Clear();
        stickyQueue.Clear();

        if (currentDisplayCoroutine != null)
        {
            StopCoroutine(currentDisplayCoroutine);
            currentDisplayCoroutine = null;
        }

        if (notificationPanel != null) notificationPanel.SetActive(false);
        isDisplayingMessage = false;
    }

    /// <summary>
    /// Get pending notification count.
    /// </summary>
    /// <returns>Queue count.</returns>
    public int GetQueueCount()
    {
        return messageQueue.Count;
    }

    #region Milestone Tier (T9.0.5)

    /// <summary>
    /// Post sticky milestone toast (gold-pulse variant). Sticks until player clicks dismiss.
    /// </summary>
    /// <param name="title">Milestone title (e.g. "Population: 1,000").</param>
    /// <param name="subtitle">Optional subtitle.</param>
    /// <param name="cellRef">Optional grid coord — camera jumps to cell on card click + dismiss.</param>
    public void PostMilestone(string title, string subtitle = null, Vector2Int? cellRef = null)
    {
        if (notificationPanel == null || notificationText == null) return;
        if (string.IsNullOrEmpty(title)) return;

        var msg = new NotificationMessage(title, NotificationType.Milestone, 0f,
            sticky: true, subtitle: subtitle, cellRef: cellRef);

        // Sticky always enqueues to front of sticky queue.
        stickyQueue.Enqueue(msg);

        if (!isDisplayingMessage)
            StartNextNotification();
    }

    /// <summary>
    /// Card click handler — if cellRef present, pan camera then dismiss.
    /// </summary>
    public void OnCardClicked(NotificationMessage notification)
    {
        if (notification.cellRef.HasValue)
        {
            var cc = FindObjectOfType<Territory.UI.CameraController>();
            if (cc != null) cc.PanCameraTo(notification.cellRef.Value);
        }
        // Dismiss + continue queue.
        if (currentDisplayCoroutine != null)
        {
            StopCoroutine(currentDisplayCoroutine);
            currentDisplayCoroutine = null;
        }
        if (notificationPanel != null) notificationPanel.SetActive(false);
        isDisplayingMessage = false;
        StartNextNotification();
    }

    /// <summary>
    /// Age out oldest non-sticky message when queue overflow. Sticky messages never aged out.
    /// </summary>
    private void AgeOutOldestNonSticky()
    {
        if (messageQueue.Count > 0) messageQueue.Dequeue();
    }

    #endregion

    #region Convenience Methods for Common Game Events

    /// <summary>
    /// Post insufficient funds notification.
    /// </summary>
    /// <param name="itemType">Item type (building, infrastructure, etc.).</param>
    /// <param name="cost">Item cost.</param>
    public void PostInsufficientFunds(string itemType, int cost)
    {
        string message = $"Insufficient funds for {itemType}. Need ${cost:N0}";
        PostError(message);
    }

    /// <summary>
    /// Post building placement error notification.
    /// </summary>
    /// <param name="reason">Placement failure reason.</param>
    public void PostBuildingPlacementError(string reason = "area is not available")
    {
        string message = $"Cannot place building: {reason}";
        PostWarning(message);
    }

    /// <summary>
    /// Post building constructed notification.
    /// </summary>
    /// <param name="buildingName">Constructed building name.</param>
    public void PostBuildingConstructed(string buildingName)
    {
        string message = $"{buildingName} constructed successfully";
        PostSuccess(message);
    }

    /// <summary>
    /// Post zone growth notification.
    /// </summary>
    /// <param name="zoneType">Zone type that grew.</param>
    /// <param name="count">New building count.</param>
    public void PostZoneGrowth(string zoneType, int count)
    {
        string message = $"{count} new {zoneType} building{(count > 1 ? "s" : "")} constructed";
        PostInfo(message);
    }

    /// <summary>
    /// Post economic event notification.
    /// </summary>
    /// <param name="eventDescription">Economic event description.</param>
    /// <param name="isPositive">True → positive, false → negative.</param>
    public void PostEconomicEvent(string eventDescription, bool isPositive)
    {
        NotificationType type = isPositive ? NotificationType.Success : NotificationType.Warning;
        PostNotification(eventDescription, type);
    }

    #endregion
}
}
