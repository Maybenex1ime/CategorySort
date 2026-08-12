#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;

namespace LogosGameLab.Editor.SceneSnapshot
{
    /// <summary>
    /// Kills any DOTween tweens attached (as target) to the cloned hierarchy.
    /// Uses reflection so the asmdef does not require a hard ref to DOTween.
    /// </summary>
    public sealed class DOTweenKillProcessor : ISnapshotPostProcessor
    {
        public string Name => "Kill DOTween Tweens";

        public bool ShouldRun(SnapshotOptions options) => options.killDOTweenTweens;

        public void Process(GameObject clone, SnapshotResult result)
        {
            var doTweenType = FindDoTweenType();
            if (doTweenType == null) return;

            var killMethod = doTweenType.GetMethod(
                "Kill",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(object), typeof(bool) },
                modifiers: null);
            if (killMethod == null) return;

            int killed = 0;
            var transforms = clone.GetComponentsInChildren<Transform>(includeInactive: true);
            for (int i = 0; i < transforms.Length; i++)
            {
                try
                {
                    object res = killMethod.Invoke(null, new object[] { transforms[i], false });
                    if (res is int n) killed += n;
                    if (res is int) continue;
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"[DOTween] Kill failed on '{transforms[i].name}': {ex.Message}");
                }
            }
            if (killed > 0)
                result.Warnings.Add($"[DOTween] Killed {killed} tween(s) on cloned hierarchy");
        }

        private static Type FindDoTweenType()
        {
            var asms = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < asms.Length; i++)
            {
                var t = asms[i].GetType("DG.Tweening.DOTween", throwOnError: false);
                if (t != null) return t;
            }
            return null;
        }
    }
}
#endif
