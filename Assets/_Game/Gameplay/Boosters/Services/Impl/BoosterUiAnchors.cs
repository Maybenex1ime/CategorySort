using System.Collections.Generic;
using UnityEngine;

namespace LogosGame.Features.Gameplay.Boosters.Services.Impl
{
    public sealed class BoosterUiAnchors : IBoosterUiAnchors
    {
        private readonly Dictionary<BoosterAnchorKey, Transform> _anchors = new();

        public void Register(BoosterAnchorKey key, Transform anchor)
        {
            if (anchor == null) return;
            _anchors[key] = anchor;
        }

        public void Unregister(BoosterAnchorKey key, Transform anchor)
        {
            // Only clear if the live registration is the one being released.
            // A rebuilt view registers its new anchor before the old view's
            // deferred OnDestroy fires; without this check the stale Unregister
            // would wipe the newer anchor and break flying-number targeting.
            if (_anchors.TryGetValue(key, out var current) && current == anchor)
            {
                _anchors.Remove(key);
            }
        }

        public Transform Get(BoosterAnchorKey key)
        {
            return _anchors.TryGetValue(key, out var t) ? t : null;
        }
    }
}
