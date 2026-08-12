#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using LogosSDK.Core.Logging;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using ILogger = LogosSDK.Core.Logging.ILogger;

namespace LogosGameLab.Editor.SceneSnapshot
{
    public static class SceneSnapshotter
    {
        internal const string LogTag = "[SceneSnapshot]";
        internal static readonly ILogger Log = LogManager.GetLogger("SceneSnapshot");

        private static readonly List<ISnapshotPostProcessor> DefaultProcessors = new List<ISnapshotPostProcessor>
        {
            new MissingScriptProcessor(),
            new DOTweenKillProcessor(),
            new ReflexClearProcessor(),
            new R3ClearProcessor(),
            new AddressablesClearProcessor(),
        };

        public static IReadOnlyList<ISnapshotPostProcessor> AvailableProcessors => DefaultProcessors;

        public static SnapshotResult SnapshotActiveScene(string targetPath, SnapshotOptions options)
            => SnapshotActiveScene(targetPath, options, DefaultProcessors);

        /// <summary>
        /// In Unity 6, EditorSceneManager.NewScene/SaveScene are blocked during Play Mode.
        /// Strategy: clone the runtime hierarchy under a single root in a runtime-created
        /// staging scene, save that root as a temporary prefab (PrefabUtility works in
        /// play mode), record a pending entry, and let PendingSnapshotMaterializer convert
        /// the prefab into a real .unity scene the moment we exit Play Mode.
        /// </summary>
        public static SnapshotResult SnapshotActiveScene(
            string targetPath,
            SnapshotOptions options,
            IReadOnlyList<ISnapshotPostProcessor> processors)
        {
            var result = new SnapshotResult();
            options ??= SnapshotOptions.Default;
            processors ??= DefaultProcessors;

            try
            {
                if (!Application.isPlaying)
                {
                    result.ErrorMessage = "SnapshotActiveScene must be called while in Play Mode.";
                    Log.Error($"{LogTag} {result.ErrorMessage}");
                    throw new InvalidOperationException(result.ErrorMessage);
                }

                if (string.IsNullOrEmpty(targetPath))
                    throw new ArgumentException("targetPath is null/empty", nameof(targetPath));

                if (!targetPath.StartsWith("Assets/", StringComparison.Ordinal))
                    throw new ArgumentException("targetPath must be under 'Assets/'", nameof(targetPath));

                if (!targetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                    targetPath += ".unity";

                EnsureDirectory(targetPath);

                Log.Info($"{LogTag} Begin snapshot → {targetPath}");

                var sourceScene = SceneManager.GetActiveScene();
                Scene ddolScene = default;
                bool hasDdol = false;
                if (options.includeDontDestroyOnLoad)
                    ddolScene = TryGetDontDestroyOnLoadScene(out hasDdol);

                var stagingScene = SceneManager.CreateScene(
                    $"__SnapshotStaging_{DateTime.Now.Ticks}__");

                var stagingRoot = new GameObject("SnapshotRoot");
                SceneManager.MoveGameObjectToScene(stagingRoot, stagingScene);

                var roots = new List<GameObject>();
                CollectRoots(sourceScene, roots, options);
                if (hasDdol) CollectRoots(ddolScene, roots, options);

                int gameObjectCount = 0;
                foreach (var root in roots)
                {
                    var clone = UnityEngine.Object.Instantiate(root, stagingRoot.transform, worldPositionStays: true);
                    clone.name = StripCloneSuffix(root.name);
                    gameObjectCount += clone.GetComponentsInChildren<Transform>(true).Length;

                    for (int i = 0; i < processors.Count; i++)
                    {
                        var p = processors[i];
                        try
                        {
                            if (p.ShouldRun(options)) p.Process(clone, result);
                        }
                        catch (Exception ex)
                        {
                            result.Warnings.Add($"[{p.Name}] threw: {ex.Message}");
                            Log.Warn($"{LogTag} Processor '{p.Name}' threw: {ex}");
                        }
                    }
                }

                result.RootCount = roots.Count;
                result.GameObjectCount = gameObjectCount;

                string prefabPath = BuildTempPrefabPath(targetPath);
                EnsureDirectory(prefabPath);

                var savedPrefab = PrefabUtility.SaveAsPrefabAsset(stagingRoot, prefabPath, out bool prefabSuccess);
                if (!prefabSuccess || savedPrefab == null)
                {
                    result.ErrorMessage = $"PrefabUtility.SaveAsPrefabAsset failed for {prefabPath}";
                    Log.Error($"{LogTag} {result.ErrorMessage}");
                    UnloadStagingScene(stagingScene);
                    return result;
                }

                UnloadStagingScene(stagingScene);

                PendingSnapshotMaterializer.Enqueue(prefabPath, targetPath);

                result.SavedPath = targetPath;
                result.PendingMaterialize = true;
                result.Success = true;
                result.Warnings.Add(
                    "Saved as a temporary prefab — will be materialized into a .unity scene " +
                    "automatically when you exit Play Mode.");

                Log.Info($"{LogTag} Snapshot staged as prefab: {prefabPath} → will materialize to {targetPath} on Play Mode exit | " +
                         $"roots={result.RootCount} gos={result.GameObjectCount} " +
                         $"stripped={result.StrippedMissingScripts} cleared={result.ClearedReferences} " +
                         $"warnings={result.Warnings.Count}");

                if (Log.IsDebugEnabled)
                {
                    for (int i = 0; i < result.Warnings.Count; i++)
                        Log.Debug($"{LogTag} {result.Warnings[i]}");
                }
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                Log.Error(ex, $"{LogTag} Snapshot failed");
                return result;
            }
        }

        private static string BuildTempPrefabPath(string scenePath)
        {
            string dir = Path.GetDirectoryName(scenePath)?.Replace('\\', '/');
            string name = Path.GetFileNameWithoutExtension(scenePath);
            if (string.IsNullOrEmpty(dir)) dir = "Assets";
            return $"{dir}/{name}.snapshot.prefab";
        }

        private static void UnloadStagingScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            try
            {
                SceneManager.UnloadSceneAsync(scene);
            }
            catch (Exception ex)
            {
                Log.Warn($"{LogTag} Failed to unload staging scene: {ex.Message}");
            }
        }

        private static void CollectRoots(Scene scene, List<GameObject> output, SnapshotOptions options)
        {
            if (!scene.IsValid()) return;
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                var go = roots[i];
                if (go == null) continue;
                if ((go.hideFlags & options.excludedHideFlags) != 0) continue;
                if (options.rootFilter != null && !options.rootFilter(go)) continue;
                output.Add(go);
            }
        }

        private static Scene TryGetDontDestroyOnLoadScene(out bool found)
        {
            var probe = new GameObject("__SnapshotDDOLProbe__");
            probe.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(probe);
            var scene = probe.scene;
            UnityEngine.Object.Destroy(probe);
            found = scene.IsValid();
            return scene;
        }

        private static string StripCloneSuffix(string name)
        {
            const string suffix = "(Clone)";
            while (name.EndsWith(suffix, StringComparison.Ordinal))
                name = name.Substring(0, name.Length - suffix.Length).TrimEnd();
            return name;
        }

        internal static void EnsureDirectory(string assetPath)
        {
            string dir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(dir)) return;
            if (AssetDatabase.IsValidFolder(dir)) return;

            var parts = dir.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
