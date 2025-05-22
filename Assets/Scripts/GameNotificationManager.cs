using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages in-game notifications for game logic events, providing persistent 
/// message display in the UI for player feedback on actions and system states.
/// </summary>
public class GameNotificationManager : MonoBehaviour
{
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
    
    // Singleton instance
    public static GameNotificationManager Instance { get; private set; }
    
    // Notification queue and state management
    private Queue<NotificationMessage> messageQueue = new Queue<NotificationMessage>();
    private bool isDisplayingMessage = false;
    private Coroutine currentDisplayCoroutine;
    private CanvasGroup notificationCanvasGroup;
    
    /// <summary>
    /// Represents different types of notifications with appropriate styling
    /// </summary>
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
    
    /// <summary>
    /// Internal structure for queued notification messages
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
            DontDestroyOnLoad(gameObject);
            InitializeComponents();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Initialize UI components and ensure proper setup
    /// </summary>
    private void InitializeComponents()
    {
        // Get or add CanvasGroup for fade effects
        notificationCanvasGroup = notificationPanel.GetComponent<CanvasGroup>();
        if (notificationCanvasGroup == null)
        {
            notificationCanvasGroup = notificationPanel.AddComponent<CanvasGroup>();
        }
        
        // Start with notifications hidden
        notificationPanel.SetActive(false);
        notificationCanvasGroup.alpha = 0f;
        
        // Validate required components
        if (notificationText == null)
        {
            Debug.LogError("GameNotificationManager: notificationText is not assigned!");
        }
        if (notificationPanel == null)
        {
            Debug.LogError("GameNotificationManager: notificationPanel is not assigned!");
        }
    }
    
    /// <summary>
    /// Post an informational notification message
    /// </summary>
    /// <param name="message">The message to display</param>
    public void PostInfo(string message)
    {
        PostNotification(message, NotificationType.Info);
    }
    
    /// <summary>
    /// Post a success notification message
    /// </summary>
    /// <param name="message">The message to display</param>
    public void PostSuccess(string message)
    {
        PostNotification(message, NotificationType.Success);
    }
    
    /// <summary>
    /// Post a warning notification message
    /// </summary>
    /// <param name="message">The message to display</param>
    public void PostWarning(string message)
    {
        PostNotification(message, NotificationType.Warning);
    }
    
    /// <summary>
    /// Post an error notification message
    /// </summary>
    /// <param name="message">The message to display</param>
    public void PostError(string message)
    {
        PostNotification(message, NotificationType.Error);
    }
    
    /// <summary>
    /// Post a notification with custom duration
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="type">The type of notification</param>
    /// <param name="customDuration">Custom display duration in seconds</param>
    public void PostNotification(string message, NotificationType type, float customDuration)
    {
        // Validate input
        if (string.IsNullOrEmpty(message))
        {
            Debug.LogWarning("GameNotificationManager: Attempted to post empty notification message");
            return;
        }
        
        // Create notification message
        NotificationMessage notification = new NotificationMessage(message, type, customDuration);
        
        // Add to queue (remove oldest if queue is full)
        if (messageQueue.Count >= maxQueueSize)
        {
            messageQueue.Dequeue();
            Debug.LogWarning("GameNotificationManager: Message queue full, removing oldest message");
        }
        
        messageQueue.Enqueue(notification);
        
        // Start displaying if not already doing so
        if (!isDisplayingMessage)
        {
            StartNextNotification();
        }
    }
    
    /// <summary>
    /// Post a notification with default duration
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="type">The type of notification</param>
    public void PostNotification(string message, NotificationType type)
    {
        PostNotification(message, type, notificationDuration);
    }
    
    /// <summary>
    /// Start displaying the next notification in the queue
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
    /// Coroutine to handle the full lifecycle of displaying a notification
    /// </summary>
    /// <param name="notification">The notification to display</param>
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
    /// Configure the notification display based on message type and content
    /// </summary>
    /// <param name="notification">The notification to configure</param>
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
    /// Utility coroutine for fading CanvasGroup alpha values
    /// </summary>
    /// <param name="canvasGroup">The CanvasGroup to fade</param>
    /// <param name="startAlpha">Starting alpha value</param>
    /// <param name="endAlpha">Ending alpha value</param>
    /// <param name="duration">Duration of the fade</param>
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
    /// Clear all pending notifications from the queue
    /// </summary>
    public void ClearNotificationQueue()
    {
        messageQueue.Clear();
        
        if (currentDisplayCoroutine != null)
        {
            StopCoroutine(currentDisplayCoroutine);
            currentDisplayCoroutine = null;
        }
        
        notificationPanel.SetActive(false);
        isDisplayingMessage = false;
    }
    
    /// <summary>
    /// Get the current number of pending notifications
    /// </summary>
    /// <returns>Number of notifications in queue</returns>
    public int GetQueueCount()
    {
        return messageQueue.Count;
    }
    
    #region Convenience Methods for Common Game Events
    
    /// <summary>
    /// Post insufficient funds notification
    /// </summary>
    /// <param name="itemType">Type of item (building, infrastructure, etc.)</param>
    /// <param name="cost">Cost of the item</param>
    public void PostInsufficientFunds(string itemType, int cost)
    {
        string message = $"Insufficient funds for {itemType}. Need ${cost:N0}";
        PostError(message);
    }
    
    /// <summary>
    /// Post building placement error notification
    /// </summary>
    /// <param name="reason">Reason for placement failure</param>
    public void PostBuildingPlacementError(string reason = "area is not available")
    {
        string message = $"Cannot place building: {reason}";
        PostWarning(message);
    }
    
    /// <summary>
    /// Post successful building construction notification
    /// </summary>
    /// <param name="buildingName">Name of the building constructed</param>
    public void PostBuildingConstructed(string buildingName)
    {
        string message = $"{buildingName} constructed successfully";
        PostSuccess(message);
    }
    
    /// <summary>
    /// Post zone growth notification
    /// </summary>
    /// <param name="zoneType">Type of zone that grew</param>
    /// <param name="count">Number of new buildings</param>
    public void PostZoneGrowth(string zoneType, int count)
    {
        string message = $"{count} new {zoneType} building{(count > 1 ? "s" : "")} constructed";
        PostInfo(message);
    }
    
    /// <summary>
    /// Post economic event notification
    /// </summary>
    /// <param name="eventDescription">Description of the economic event</param>
    /// <param name="isPositive">Whether the event is positive or negative</param>
    public void PostEconomicEvent(string eventDescription, bool isPositive)
    {
        NotificationType type = isPositive ? NotificationType.Success : NotificationType.Warning;
        PostNotification(eventDescription, type);
    }
    
    #endregion
}