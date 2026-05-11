using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Territory.UI.Registry
{
    /// <summary>
    /// Wave A0 (TECH-27059) — shell-only action dispatch table.
    /// Register named action handlers; Dispatch routes payload to first matching handler.
    /// MonoBehaviour; mount under UI host GameObject per scene (MainMenu.unity, CityScene.unity — T2.0.5+).
    /// Stage 4 T4.0.3: Dispatch writes action-fire telemetry to Diagnostics/action-fire.log.
    /// </summary>
    public class UiActionRegistry : MonoBehaviour
    {
        private readonly Dictionary<string, Action<object>> _handlers =
            new Dictionary<string, Action<object>>();

        // Handler class cache: action_id → handler class name (set at Register time).
        private readonly Dictionary<string, string> _handlerClassNames =
            new Dictionary<string, string>();

        /// <summary>Register handler for actionId. Replaces existing handler.</summary>
        public void Register(string actionId, Action<object> handler)
        {
            if (string.IsNullOrEmpty(actionId)) throw new ArgumentNullException(nameof(actionId));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _handlers[actionId] = handler;
            // Capture declaring type name for telemetry.
            _handlerClassNames[actionId] = handler.Method?.DeclaringType?.Name ?? "unknown";
        }

        /// <summary>Dispatch payload to registered handler. Returns false when actionId not found.</summary>
        public bool Dispatch(string actionId, object payload)
        {
            if (_handlers.TryGetValue(actionId, out var handler))
            {
                string handlerClass = _handlerClassNames.TryGetValue(actionId, out var cn) ? cn : "unknown";
                handler(payload);
                WriteActionFireLog(actionId, handlerClass);
                return true;
            }
            return false;
        }

        /// <summary>Returns snapshot of all registered action ids.</summary>
        public IReadOnlyList<string> ListRegistered()
        {
            return new List<string>(_handlers.Keys);
        }

        // ── Telemetry ─────────────────────────────────────────────────────────

        /// <summary>Write one JSON line to Diagnostics/action-fire.log on every dispatch.</summary>
        private static void WriteActionFireLog(string actionId, string handlerClass)
        {
            try
            {
                string logDir  = Path.Combine(Application.persistentDataPath, "Diagnostics");
                string logPath = Path.Combine(logDir, "action-fire.log");
                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                string ts    = DateTime.UtcNow.ToString("o");
                string entry = $"{{\"action_id\":\"{Escape(actionId)}\",\"handler_class\":\"{Escape(handlerClass)}\",\"ts\":\"{ts}\",\"marker\":\"fired\"}}";
                File.AppendAllText(logPath, entry + Environment.NewLine);
            }
            catch
            {
                // Best-effort telemetry; never throw from Dispatch.
            }
        }

        private static string Escape(string s)
            => s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? string.Empty;
    }
}
