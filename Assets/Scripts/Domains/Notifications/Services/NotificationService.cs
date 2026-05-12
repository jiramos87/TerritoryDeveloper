using System.Collections.Generic;
using UnityEngine;
using Territory.UI;

namespace Domains.Notifications.Services
{
    /// <summary>
    /// POCO service extracted from GameNotificationManager (Stage 5.5 Tier-C NO-PORT).
    /// Queue management: enqueue, dequeue, clear, age-out oldest non-sticky.
    /// GameNotificationManager.NotificationType/NotificationMessage live in hub — callers unaffected.
    /// Guardrail #8: lazy-init panel pattern stays in hub — service never touches notificationPanel.
    /// Invariants #3 (no per-frame FindObjectOfType), #4 (no new singletons) observed.
    /// autoReferenced:false — Services/ routes into TerritoryDeveloper.Game via .asmref.
    /// </summary>
    public class NotificationService
    {
        // ── State ────────────────────────────────────────────────────────────────────

        private readonly Queue<GameNotificationManager.NotificationMessage> _messageQueue
            = new Queue<GameNotificationManager.NotificationMessage>();
        private readonly Queue<GameNotificationManager.NotificationMessage> _stickyQueue
            = new Queue<GameNotificationManager.NotificationMessage>();

        /// <summary>True when hub coroutine is actively displaying a message.</summary>
        public bool IsDisplayingMessage { get; set; }

        // ── Config (wired from hub) ───────────────────────────────────────────────────

        private int   _maxQueueSize    = 5;
        private float _defaultDuration = 3.0f;

        /// <summary>Wire hub config. Call once from hub Awake after SerializeFields resolved.</summary>
        public void WireConfig(int maxQueueSize, float defaultDuration)
        {
            _maxQueueSize    = maxQueueSize;
            _defaultDuration = defaultDuration;
        }

        // ── Enqueue ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Enqueue a notification. Returns true when hub should call StartNextNotification.
        /// Silently drops when refsReady=false (guardrail #8 — missing panel is non-fatal).
        /// </summary>
        public bool Enqueue(GameNotificationManager.NotificationMessage msg, bool refsReady)
        {
            if (!refsReady) return false;
            if (string.IsNullOrEmpty(msg.message)) return false;

            if (msg.sticky)
            {
                _stickyQueue.Enqueue(msg);
            }
            else
            {
                if (_messageQueue.Count >= _maxQueueSize)
                    AgeOutOldestNonSticky();
                _messageQueue.Enqueue(msg);
            }

            return !IsDisplayingMessage;
        }

        /// <summary>Build typed message and enqueue.</summary>
        public bool EnqueueTyped(string message, GameNotificationManager.NotificationType type,
            float duration, bool refsReady)
        {
            return Enqueue(
                new GameNotificationManager.NotificationMessage(message, type, duration),
                refsReady);
        }

        // ── Dequeue ──────────────────────────────────────────────────────────────────

        /// <summary>Returns true when a message is available; populates next. Sticky drains first.</summary>
        public bool TryDequeueNext(out GameNotificationManager.NotificationMessage next)
        {
            if (_stickyQueue.Count > 0) { next = _stickyQueue.Dequeue(); return true; }
            if (_messageQueue.Count > 0) { next = _messageQueue.Dequeue(); return true; }
            next = default(GameNotificationManager.NotificationMessage);
            return false;
        }

        /// <summary>True when either queue has pending messages.</summary>
        public bool HasPending => _stickyQueue.Count > 0 || _messageQueue.Count > 0;

        // ── Queue ops ────────────────────────────────────────────────────────────────

        /// <summary>Clear all queued messages (sticky + non-sticky).</summary>
        public void Clear()
        {
            _messageQueue.Clear();
            _stickyQueue.Clear();
            IsDisplayingMessage = false;
        }

        /// <summary>Non-sticky queue count (excludes sticky).</summary>
        public int GetQueueCount() => _messageQueue.Count;

        // ── Helpers ──────────────────────────────────────────────────────────────────

        private void AgeOutOldestNonSticky()
        {
            if (_messageQueue.Count > 0) _messageQueue.Dequeue();
        }

        // ── Convenience builders ──────────────────────────────────────────────────────

        /// <summary>Build milestone sticky message (T9.0.5).</summary>
        public GameNotificationManager.NotificationMessage BuildMilestone(
            string title, string subtitle = null, Vector2Int? cellRef = null)
        {
            return new GameNotificationManager.NotificationMessage(
                title, GameNotificationManager.NotificationType.Milestone, 0f,
                sticky: true, subtitle: subtitle, cellRef: cellRef);
        }
    }
}
