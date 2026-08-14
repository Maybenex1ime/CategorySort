using UnityEngine;

namespace LogosGame.Features.Gameplay.Boosters.Services
{
    public enum BoosterAnchorKey
    {
        AddQueueButton,
        QueueCounter,
        AddBeltButton,
        BeltCounter,
        HammerButton,
    }

    /// <summary>
    /// Runtime registry mapping booster-UI anchor keys to live Transforms.
    /// Anchors may be UI RectTransforms (booster button on a Canvas) or plain
    /// world-space Transforms (queue badge attached to a 3D GameObject).
    /// Consumers (e.g. flying-number animator) must convert positions through
    /// screen space when source and target live in different coordinate systems.
    /// </summary>
    public interface IBoosterUiAnchors
    {
        void Register(BoosterAnchorKey key, Transform anchor);

        /// <summary>
        /// Releases <paramref name="key"/> only if the currently-registered anchor
        /// is <paramref name="anchor"/>. This guards against a destroyed view's
        /// deferred OnDestroy wiping a newer registration made by a rebuilt
        /// replacement that shares the same key (e.g. RebuildVisuals destroys the
        /// old belt counter after the new one already re-registered).
        /// </summary>
        void Unregister(BoosterAnchorKey key, Transform anchor);
        Transform Get(BoosterAnchorKey key);
    }
}
