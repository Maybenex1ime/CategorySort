#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LogosGameLab.Editor.SceneSnapshot
{
    /// <summary>
    /// Shared helper: walks every MonoBehaviour on a clone hierarchy and clears
    /// fields matching a predicate. Used by Reflex/R3/Addressables processors.
    /// </summary>
    internal static class ReflectionFieldClearer
    {
        private const BindingFlags Flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static int Walk(
            GameObject root,
            Func<FieldInfo, MonoBehaviour, bool> shouldClearField,
            string warningTag,
            SnapshotResult result)
        {
            int cleared = 0;
            var components = root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
            for (int i = 0; i < components.Length; i++)
            {
                var mb = components[i];
                if (mb == null) continue;
                var type = mb.GetType();
                while (type != null && type != typeof(MonoBehaviour) && type != typeof(UnityEngine.Object))
                {
                    var fields = type.GetFields(Flags | BindingFlags.DeclaredOnly);
                    for (int f = 0; f < fields.Length; f++)
                    {
                        var field = fields[f];
                        if (field.IsLiteral || field.IsInitOnly) continue;
                        if (!shouldClearField(field, mb)) continue;
                        try
                        {
                            var current = field.GetValue(mb);
                            if (current == null) continue;
                            if (field.FieldType.IsValueType)
                                field.SetValue(mb, Activator.CreateInstance(field.FieldType));
                            else
                                field.SetValue(mb, null);
                            cleared++;
                            result.Warnings.Add(
                                $"[{warningTag}] Cleared {type.Name}.{field.Name} on '{mb.gameObject.name}'");
                        }
                        catch (Exception ex)
                        {
                            result.Warnings.Add(
                                $"[{warningTag}] Failed to clear {type.Name}.{field.Name}: {ex.Message}");
                        }
                    }
                    type = type.BaseType;
                }
            }
            return cleared;
        }

        public static bool TypeNameMatches(Type t, IReadOnlyList<string> needles)
        {
            if (t == null) return false;
            string full = t.FullName ?? string.Empty;
            string simple = t.Name ?? string.Empty;
            for (int i = 0; i < needles.Count; i++)
            {
                if (full.Contains(needles[i]) || simple.Contains(needles[i])) return true;
            }
            return false;
        }
    }
}
#endif
