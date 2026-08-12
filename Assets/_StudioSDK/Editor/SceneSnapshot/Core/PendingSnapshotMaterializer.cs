#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LogosGameLab.Editor.SceneSnapshot
{
    /// <summary>
    /// Watches the editor for Play Mode exit. Any snapshot that was staged as a
    /// temporary prefab during Play Mode is converted into a real .unity scene
    /// here, and the prefab is deleted. State persists across the play-mode
    /// boundary via SessionState (per editor session).
    /// </summary>
    [InitializeOnLoad]
    internal static class PendingSnapshotMaterializer
    {
        private const string SessionKey = "LogosSceneSnapshot.Pending";
        private const char EntrySeparator = ';';
        private const char FieldSeparator = '|';

        static PendingSnapshotMaterializer()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        public static void Enqueue(string prefabPath, string scenePath)
        {
            var entries = LoadEntries();
            entries.Add(new Entry { PrefabPath = prefabPath, ScenePath = scenePath });
            SaveEntries(entries);
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;

            var entries = LoadEntries();
            if (entries.Count == 0) return;

            // Clear immediately so a failed entry can't loop forever.
            SaveEntries(new List<Entry>());

            foreach (var entry in entries)
            {
                try
                {
                    Materialize(entry);
                }
                catch (Exception ex)
                {
                    SceneSnapshotter.Log.Error(ex,
                        $"{SceneSnapshotter.LogTag} Failed to materialize {entry.PrefabPath} → {entry.ScenePath}");
                }
            }
        }

        private static void Materialize(Entry entry)
        {
            if (string.IsNullOrEmpty(entry.PrefabPath) || string.IsNullOrEmpty(entry.ScenePath))
                return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.PrefabPath);
            if (prefab == null)
            {
                SceneSnapshotter.Log.Warn(
                    $"{SceneSnapshotter.LogTag} Pending prefab missing at {entry.PrefabPath} — skipping materialize.");
                return;
            }

            SceneSnapshotter.EnsureDirectory(entry.ScenePath);

            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, newScene);
            PrefabUtility.UnpackPrefabInstance(instance,
                PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);

            // The staging root was a synthetic container — flatten it so its children
            // become real scene roots.
            var children = new List<Transform>();
            for (int i = 0; i < instance.transform.childCount; i++)
                children.Add(instance.transform.GetChild(i));
            foreach (var child in children)
                child.SetParent(null, worldPositionStays: true);
            UnityEngine.Object.DestroyImmediate(instance);

            bool saved = EditorSceneManager.SaveScene(newScene, entry.ScenePath, saveAsCopy: false);
            EditorSceneManager.CloseScene(newScene, removeScene: true);

            if (!saved)
            {
                SceneSnapshotter.Log.Error(
                    $"{SceneSnapshotter.LogTag} SaveScene failed for {entry.ScenePath}");
                return;
            }

            AssetDatabase.DeleteAsset(entry.PrefabPath);
            AssetDatabase.ImportAsset(entry.ScenePath, ImportAssetOptions.ForceUpdate);

            SceneSnapshotter.Log.Info(
                $"{SceneSnapshotter.LogTag} Materialized snapshot → {entry.ScenePath}");

            var asset = AssetDatabase.LoadMainAssetAtPath(entry.ScenePath);
            if (asset != null) EditorGUIUtility.PingObject(asset);
        }

        private struct Entry
        {
            public string PrefabPath;
            public string ScenePath;
        }

        private static List<Entry> LoadEntries()
        {
            var raw = SessionState.GetString(SessionKey, string.Empty);
            var list = new List<Entry>();
            if (string.IsNullOrEmpty(raw)) return list;
            var parts = raw.Split(EntrySeparator);
            foreach (var p in parts)
            {
                if (string.IsNullOrEmpty(p)) continue;
                var pair = p.Split(FieldSeparator);
                if (pair.Length != 2) continue;
                list.Add(new Entry { PrefabPath = pair[0], ScenePath = pair[1] });
            }
            return list;
        }

        private static void SaveEntries(List<Entry> entries)
        {
            if (entries.Count == 0)
            {
                SessionState.EraseString(SessionKey);
                return;
            }
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0) sb.Append(EntrySeparator);
                sb.Append(entries[i].PrefabPath).Append(FieldSeparator).Append(entries[i].ScenePath);
            }
            SessionState.SetString(SessionKey, sb.ToString());
        }
    }
}
#endif
