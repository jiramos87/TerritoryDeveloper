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
        Error
    }

    /// <summary>
    /// Queued notification message struct.
    /// </summary>
    private struct NotificationMessage
    {
        public string message;
        public NotificationType type;
        public float duration;

        public NotificationMessage(string message, NotificationType type, float duration)
        {
            this.message = message;
            this.type = type;
            this.duration = duration;
        }
    }

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
    /// Init UI components + verify setup.
    /// </summary>
    private void InitializeComponents()
    {
        // Validate required components first — Inspector wiring may be missing on a freshly-baked HUD root.
        if (notificationPanel == null)
        {
            Debug.LogError("GameNotificationManager: notificationPanel is not assigned — notifications disabled until SerializeField is wired in scene.");
            return;
        }
        if (notificationText == null)
        {
            Debug.LogError("GameNotificationManager: notificationText is not assigned!");
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

        // Add to queue (remove oldest if queue is full)
        if (messageQueue.Count >= maxQueueSize)
        {
            messageQueue.Dequeue();
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
    /// Start displaying next queued notification.
    /// </summary>
    private void StartNextNotification()
    {
        if (messageQueue.Count == 0)
        {
            isDisplayingMessage = false;
            return;
        }

        isDisplayingMessage = true;
        NotificationMessage nextMessage = messageQueue.Dequeue();

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

        // Fade in
        notificationPanel.SetActive(true);
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
    /// Clear all pending queued notifications.
    /// </summary>
    public void ClearNotificationQueue()
    {
        messageQueue.Clear();

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
