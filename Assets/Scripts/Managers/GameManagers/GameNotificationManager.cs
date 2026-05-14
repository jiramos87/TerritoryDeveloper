using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Territory.Utilities;
using Domains.Notifications.Services;
using Territory.UI.Hosts;

namespace Territory.UI
{
/// <summary>
/// Hub: in-game notifications. SerializeField UI refs + lazy-init (guardrail #8) + coroutines.
/// Queue/state delegated to NotificationService (_svc).
/// Effort 3 (post iter-25): Post* routes through NotificationsToastHost (UI Toolkit toast) when present.
/// Legacy uGUI panel path retained for back-compat; silent Debug.Log fallback when neither surface is wired.
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
        // Effort 3 (post iter-25): UI Toolkit NotificationsToastHost is the primary surface.
        // Legacy uGUI panel path retained only when explicitly wired in Inspector — no more
        // lazy-create canvas overlay, no more "no panel" LogError spam.
        if (notificationPanel != null)
        {
            notificationCanvasGroup = notificationPanel.GetComponent<CanvasGroup>() ?? notificationPanel.AddComponent<CanvasGroup>();
            notificationPanel.SetActive(false); notificationCanvasGroup.alpha = 0f;
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
        if (TryPostToast(message, type, duration)) return;

        bool ready = notificationPanel != null && notificationText != null;
        if (_svc.EnqueueTyped(message, type, duration, ready)) StartNextNotification();
        else SilentLog(message, type);
    }
    public void PostNotification(string message, NotificationType type) => PostNotification(message, type, notificationDuration);

    public void PostMilestone(string title, string subtitle = null, Vector2Int? cellRef = null)
    {
        // Milestone routes to toast as success surface; subtitle/cellRef intentionally not surfaced on the toast strip.
        if (TryPostToast(title, NotificationType.Success, notificationDuration)) return;

        if (notificationPanel == null || notificationText == null) { SilentLog(title, NotificationType.Success); return; }
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

    // ── Routing ─────────────────────────────────────────────────────────────────
    private bool TryPostToast(string message, NotificationType type, float duration)
    {
        var host = NotificationsToastHost.Instance;
        if (host == null) return false;
        host.Post(message, MapKind(type), duration > 0f ? duration : notificationDuration);
        return true;
    }

    private static NotificationsToastHost.ToastKind MapKind(NotificationType t)
    {
        switch (t)
        {
            case NotificationType.Success:   return NotificationsToastHost.ToastKind.Success;
            case NotificationType.Warning:   return NotificationsToastHost.ToastKind.Warning;
            case NotificationType.Error:     return NotificationsToastHost.ToastKind.Error;
            case NotificationType.Milestone: return NotificationsToastHost.ToastKind.Success;
            default:                         return NotificationsToastHost.ToastKind.Info;
        }
    }

    private static void SilentLog(string message, NotificationType type)
    {
        // No toast host + no legacy panel = log only, no Console spam beyond the message itself.
        Debug.Log($"[Notification:{type}] {message}");
    }

    // ── Coroutine internals (legacy uGUI path) ──────────────────────────────────
    private void PostInternal(string message, NotificationType type)
    {
        if (string.IsNullOrEmpty(message)) { DebugHelper.LogWarning("GameNotificationManager: empty message"); return; }
        if (TryPostToast(message, type, notificationDuration)) return;

        bool ready = notificationPanel != null && notificationText != null;
        if (_svc.EnqueueTyped(message, type, notificationDuration, ready)) StartNextNotification();
        else SilentLog(message, type);
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
