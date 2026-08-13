#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;

namespace LogosGameLab.Editor.SceneSnapshot
{
    public sealed class R3ClearProcessor : ISnapshotPostProcessor
    {
        private static readonly string[] TypeNeedles =
        {
            "R3.Subject",
            "R3.ReactiveProperty",
            "R3.ReadOnlyReactiveProperty",
            "R3.Observable",
            "R3.DisposableBag",
            "R3.CompositeDisposable",
            "Subject`",
            "ReactiveProperty`"
        };

        public string Name => "Clear R3 References";

        public bool ShouldRun(SnapshotOptions options) => options.clearR3References;

        public void Process(GameObject clone, SnapshotResult result)
        {
            int cleared = ReflectionFieldClearer.Walk(clone, ShouldClear, "R3", result);
            result.ClearedReferences += cleared;
        }

        private static bool ShouldClear(FieldInfo field, MonoBehaviour _)
        {
            var t = field.FieldType;
            if (t == typeof(IDisposable)) return true;
            if (typeof(IDisposable).IsAssignableFrom(t) &&
                ReflectionFieldClearer.TypeNameMatches(t, TypeNeedles))
                return true;
            return ReflectionFieldClearer.TypeNameMatches(t, TypeNeedles);
        }
    }
}
#endif
