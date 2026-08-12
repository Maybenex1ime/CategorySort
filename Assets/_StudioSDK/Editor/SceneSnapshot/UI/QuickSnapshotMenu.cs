#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LogosGameLab.Editor.SceneSnapshot
{
    public static class QuickSnapshotMenu
    {
        private const string MenuPath = "Tools/Logos/Take Snapshot";

        [MenuItem(MenuPath, priority = 101)]
        public static void QuickSnapshot()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Take Snapshot",
                    "Take Snapshot requires Play Mode.",
                    "OK");
                return;
            }

            string sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName)) sceneName = "Untitled";
            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string path = $"Assets/_Snapshots/{sceneName}_{ts}.unity";

            var result = SceneSnapshotter.SnapshotActiveScene(path, SnapshotOptions.Default);
            if (result.Success)
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(result.SavedPath);
                if (asset != null) EditorGUIUtility.PingObject(asset);
            }
        }

        [MenuItem(MenuPath, validate = true)]
        public static bool ValidateQuickSnapshot() => Application.isPlaying;
    }
}
#endif
