using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Territory.Editor.UiBake
{
    /// <summary>
    /// Layer 2 plugin dispatcher (TECH-28362).
    /// Holds an ordered list of <see cref="IBakeHandler"/> plugins; first match
    /// by priority wins for each kind. Thread-safe reads; not safe for concurrent mutation.
    /// </summary>
    public sealed class BakeHandlerRegistry
    {
        private readonly IBakeHandler[] _handlers;

        /// <param name="handlers">All registered plugins. Sorted descending by Priority at construction.</param>
        public BakeHandlerRegistry(IEnumerable<IBakeHandler> handlers)
        {
            if (handlers == null) throw new ArgumentNullException(nameof(handlers));
            _handlers = handlers.OrderByDescending(h => h.Priority).ToArray();
        }

        /// <summary>
        /// Dispatch <paramref name="kind"/> to the highest-priority matching plugin.
        /// Throws <see cref="BakeException"/> when no plugin claims the kind.
        /// </summary>
        public void Dispatch(string kind, GameObject childGo, Transform parent)
        {
            foreach (var handler in _handlers)
            {
                if (handler.SupportedKinds == null) continue;
                foreach (var k in handler.SupportedKinds)
                {
                    if (string.Equals(k, kind, StringComparison.Ordinal))
                    {
                        var spec = new BakeChildSpec
                        {
                            kind         = kind,
                            instanceSlug = childGo != null ? childGo.name : string.Empty,
                        };
                        handler.Bake(spec, parent);
                        return;
                    }
                }
            }
            throw new BakeException($"unknown_kind: '{kind}' not registered in BakeHandlerRegistry");
        }

        /// <summary>Returns true when at least one plugin claims <paramref name="kind"/>.</summary>
        public bool IsRegistered(string kind)
        {
            foreach (var handler in _handlers)
            {
                if (handler.SupportedKinds == null) continue;
                foreach (var k in handler.SupportedKinds)
                    if (string.Equals(k, kind, StringComparison.Ordinal)) return true;
            }
            return false;
        }
    }
}
