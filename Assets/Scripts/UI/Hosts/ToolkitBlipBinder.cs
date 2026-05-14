using System.Runtime.CompilerServices;
using Territory.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Effort 2 (iter-21) — central helper for binding UI Toolkit interaction events
    /// to <see cref="BlipId"/> playback through <see cref="BlipEngine"/>. Hosts call
    /// <see cref="BindClick"/> / <see cref="BindHover"/> per Button (or VisualElement)
    /// in OnEnable, and <see cref="UnbindAll"/> in OnDisable.
    ///
    /// Catalog tolerance: <see cref="BlipEngine.Play"/> throws
    /// <see cref="System.ArgumentOutOfRangeException"/> when the catalog has no entry
    /// for the requested id. This helper swallows that exception so a missing catalog
    /// row never breaks the UI thread — the click still flows to the underlying handler.
    /// </summary>
    public static class ToolkitBlipBinder
    {
        sealed class BindingToken
        {
            public EventCallback<ClickEvent> click;
            public EventCallback<MouseEnterEvent> hover;
        }

        // ConditionalWeakTable so a VisualElement can GC normally without leaking the token.
        static readonly ConditionalWeakTable<VisualElement, BindingToken> _tokens =
            new ConditionalWeakTable<VisualElement, BindingToken>();

        static BindingToken EnsureToken(VisualElement ve)
        {
            if (ve == null) return null;
            if (_tokens.TryGetValue(ve, out var tok)) return tok;
            tok = new BindingToken();
            _tokens.Add(ve, tok);
            return tok;
        }

        public static void BindClick(VisualElement ve, BlipId id)
        {
            if (ve == null) return;
            var tok = EnsureToken(ve);
            if (tok.click != null) return;
            tok.click = _ => SafePlay(id);
            ve.RegisterCallback(tok.click);
        }

        public static void BindHover(VisualElement ve, BlipId id)
        {
            if (ve == null) return;
            var tok = EnsureToken(ve);
            if (tok.hover != null) return;
            tok.hover = _ => SafePlay(id);
            ve.RegisterCallback(tok.hover);
        }

        public static void UnbindClick(VisualElement ve)
        {
            if (ve == null) return;
            if (!_tokens.TryGetValue(ve, out var tok)) return;
            if (tok.click == null) return;
            ve.UnregisterCallback(tok.click);
            tok.click = null;
        }

        public static void UnbindHover(VisualElement ve)
        {
            if (ve == null) return;
            if (!_tokens.TryGetValue(ve, out var tok)) return;
            if (tok.hover == null) return;
            ve.UnregisterCallback(tok.hover);
            tok.hover = null;
        }

        public static void BindClickAndHover(VisualElement ve, BlipId clickId, BlipId hoverId)
        {
            BindClick(ve, clickId);
            BindHover(ve, hoverId);
        }

        public static void UnbindAll(VisualElement ve)
        {
            UnbindClick(ve);
            UnbindHover(ve);
        }

        static void SafePlay(BlipId id)
        {
            try
            {
                BlipEngine.Play(id);
            }
            catch (System.ArgumentOutOfRangeException)
            {
                // Catalog has no entry for this id — silently skip. UI click still flows.
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ToolkitBlipBinder] BlipEngine.Play({id}) threw {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
