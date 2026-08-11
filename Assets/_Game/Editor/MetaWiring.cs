using System.Linq;
using Reflex.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WordStack.Meta.Editor
{
    /// <summary>
    /// Gắn các component meta vào prefab root scope và vào scene. Chạy lại được
    /// nhiều lần: đã có thì bỏ qua, không nhân bản. Tồn tại vì component mới sinh
    /// ra từ code không tự vào prefab/scene được — cách khác là sửa YAML tay, rủi ro hơn.
    /// </summary>
    internal static class MetaWiring
    {
        private const string ScopePath = "Assets/Prefabs/ProjectScope.prefab";
        private const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("WordStack/Setup/Wire meta components")]
        private static void Run()
        {
            WirePrefab();
            WireScene();
            AssetDatabase.SaveAssets();
        }

        private static void WirePrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(ScopePath);
            if (root == null)
            {
                Debug.LogError($"WIRE FAIL: không thấy {ScopePath}");
                return;
            }

            var added = false;
            if (root.GetComponent<ProgressionInstaller>() == null)
            {
                root.AddComponent<ProgressionInstaller>();
                added = true;
            }

            if (added)
            {
                PrefabUtility.SaveAsPrefabAsset(root, ScopePath);
                Debug.Log("WIRE: thêm ProgressionInstaller vào ProjectScope.");
            }
            else
            {
                Debug.Log("WIRE: ProjectScope đã đủ component, không đổi gì.");
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void WireScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var scope = scene.GetRootGameObjects()
                             .Select(go => go.GetComponentInChildren<ContainerScope>(true))
                             .FirstOrDefault(c => c != null);

            if (scope == null)
            {
                Debug.LogError("WIRE FAIL: scene chưa có ContainerScope — chạy lại phần dựng spine trước.");
                return;
            }

            if (scope.GetComponent<MetaSession>() != null)
            {
                Debug.Log("WIRE: scene đã có MetaSession, không đổi gì.");
                return;
            }

            scope.gameObject.AddComponent<MetaSession>();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"WIRE: thêm MetaSession vào '{scope.gameObject.name}'.");
        }
    }
}
