#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace LogosGameLab.Editor.SceneSnapshot
{
    public sealed class MissingScriptProcessor : ISnapshotPostProcessor
    {
        public string Name => "Strip Missing Scripts";

        public bool ShouldRun(SnapshotOptions options) => options.stripMissingScripts;

        public void Process(GameObject clone, SnapshotResult result)
        {
            int total = 0;
            var stack = new System.Collections.Generic.Stack<Transform>();
            stack.Push(clone.transform);
            while (stack.Count > 0)
            {
                var t = stack.Pop();
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                if (removed > 0)
                {
                    total += removed;
                    result.Warnings.Add($"[MissingScript] Stripped {removed} missing component(s) on '{t.name}'");
                }
                for (int i = 0; i < t.childCount; i++) stack.Push(t.GetChild(i));
            }
            result.StrippedMissingScripts += total;
        }
    }
}
#endif
