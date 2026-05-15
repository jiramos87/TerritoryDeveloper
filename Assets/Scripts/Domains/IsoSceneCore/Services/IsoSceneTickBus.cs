using System.Collections.Generic;
using UnityEngine;

namespace Territory.IsoSceneCore
{
    /// <summary>Tick multiplexer. TimeManager publishes GlobalTick once per sim day; subscribers register via Subscribe in Start (invariant #12). Empty subscriber list guards prevent NRE.</summary>
    public sealed class IsoSceneTickBus
    {
        private readonly List<IIsoSceneTickHandler> _globalSubscribers = new List<IIsoSceneTickHandler>();
        private readonly List<IIsoSceneTickHandler> _regionSubscribers = new List<IIsoSceneTickHandler>();

        /// <summary>True when at least one subscriber is registered (any kind).</summary>
        public bool HasSubscribers => _globalSubscribers.Count > 0 || _regionSubscribers.Count > 0;

        /// <summary>Register handler for the given tick kind. Call in Start (invariant #12).</summary>
        public void Subscribe(IIsoSceneTickHandler handler, IsoTickKind kind)
        {
            if (handler == null) return;
            var list = kind == IsoTickKind.RegionTick ? _regionSubscribers : _globalSubscribers;
            if (!list.Contains(handler))
                list.Add(handler);
        }

        /// <summary>Unsubscribe handler. Safe to call even if not registered.</summary>
        public void Unsubscribe(IIsoSceneTickHandler handler, IsoTickKind kind)
        {
            if (handler == null) return;
            var list = kind == IsoTickKind.RegionTick ? _regionSubscribers : _globalSubscribers;
            list.Remove(handler);
        }

        /// <summary>Publish tick to all registered subscribers of the given kind. No-op when list empty.</summary>
        public void Publish(IsoTickKind kind)
        {
            var list = kind == IsoTickKind.RegionTick ? _regionSubscribers : _globalSubscribers;
            if (list.Count == 0) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                try { list[i].OnIsoTick(kind); }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[IsoSceneTickBus] Subscriber {list[i]} threw on Publish({kind}): {ex.Message}");
                }
            }
        }
    }
}
