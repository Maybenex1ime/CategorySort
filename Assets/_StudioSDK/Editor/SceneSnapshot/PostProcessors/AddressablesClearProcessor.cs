#if UNITY_EDITOR
using System.Reflection;
using UnityEngine;

namespace LogosGameLab.Editor.SceneSnapshot
{
    public sealed class AddressablesClearProcessor : ISnapshotPostProcessor
    {
        private static readonly string[] HandleNeedles =
        {
            "AsyncOperationHandle"
        };

        public string Name => "Clear Addressables Handles";

        public bool ShouldRun(SnapshotOptions options) => options.clearAddressablesHandles;

        public void Process(GameObject clone, SnapshotResult result)
        {
            int cleared = ReflectionFieldClearer.Walk(clone, ShouldClear, "Addressables", result);
            result.ClearedReferences += cleared;
        }

        private static bool ShouldClear(FieldInfo field, MonoBehaviour _)
            => ReflectionFieldClearer.TypeNameMatches(field.FieldType, HandleNeedles);
    }
}
#endif
