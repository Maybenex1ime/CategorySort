#if UNITY_EDITOR
using LogosGameLab.Editor.SceneSnapshot.Tests;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LogosGameLab.Editor.SceneSnapshot
{
    /// <summary>
    /// Creates the acceptance-test scene programmatically. Hand-authored .unity
    /// YAML is fragile (script GUID drift, missing settings blocks) — a generated
    /// scene is far more robust and produces an identical asset.
    /// </summary>
    public static class SnapshotTestSceneCreator
    {
        private const string ScenePath =
            "Assets/_StudioSDK/Editor/SceneSnapshot/Tests/SnapshotTestScene.unity";

        [MenuItem("Tools/Logos/Scene Snapshot Tests/Create Test Scene", priority = 200)]
        public static void Create()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Create Test Scene",
                    "Exit Play Mode before creating the test scene.",
                    "OK");
                return;
            }

            if (System.IO.File.Exists(ScenePath))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Overwrite Test Scene?",
                    $"'{ScenePath}' already exists. Overwrite?",
                    "Overwrite",
                    "Cancel");
                if (!overwrite) return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var spawnerGo = new GameObject("Spawner");
            SceneManager.MoveGameObjectToScene(spawnerGo, scene);
            spawnerGo.AddComponent<SnapshotTestSpawner>();

            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            if (saved)
            {
                AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
                var asset = AssetDatabase.LoadMainAssetAtPath(ScenePath);
                if (asset != null) EditorGUIUtility.PingObject(asset);
                Debug.Log($"[SceneSnapshot] Test scene created: {ScenePath}");
            }
            else
            {
                Debug.LogError($"[SceneSnapshot] Failed to save test scene to {ScenePath}");
            }
        }
    }
}
#endif
