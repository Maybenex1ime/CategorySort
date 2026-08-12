#if UNITY_EDITOR
using System;

namespace LogosGameLab.Editor.SceneSnapshot
{
    public sealed class ReflexClearProcessor : ISnapshotPostProcessor
    {
        private static readonly string[] ReflexTypeNeedles =
        {
            "Reflex.Core.Container",
            "Reflex.Core.Scope",
            "Reflex.IContainer",
            "Reflex.Container"
        };

        private const string InjectAttributeFullName = "Reflex.Attributes.InjectAttribute";

        public string Name => "Clear Reflex Container Refs";

        public bool ShouldRun(SnapshotOptions options) => options.clearReflexContainerRefs;

        public void Process(UnityEngine.GameObject clone, SnapshotResult result)
        {
            int cleared = ReflectionFieldClearer.Walk(clone, ShouldClear, "Reflex", result);
            result.ClearedReferences += cleared;
        }

        private static bool ShouldClear(System.Reflection.FieldInfo field, UnityEngine.MonoBehaviour _)
        {
            if (ReflectionFieldClearer.TypeNameMatches(field.FieldType, ReflexTypeNeedles))
                return true;

            var attrs = field.GetCustomAttributes(inherit: true);
            for (int i = 0; i < attrs.Length; i++)
            {
                var attrType = attrs[i].GetType();
                if (attrType.FullName == InjectAttributeFullName ||
                    attrType.Name == "InjectAttribute")
                    return true;
            }
            return false;
        }
    }
}
#endif
