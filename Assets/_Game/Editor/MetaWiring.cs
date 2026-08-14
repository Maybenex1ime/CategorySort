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
                Debug.Log("WIRE: thêm ProgressionInstaller vào ProjectScope.");
            }

            // Hệ mua: CurrencyInstaller cần SO_TransactionCatalog (field private
            // → đi qua SerializedObject). Chưa gán thì IPurchaseService vắng mặt,
            // nút Mua trong BoosterPurchasePopup chỉ log stub.
            var currency = root.GetComponent<CurrencyInstaller>();
            if (currency != null)
            {
                var so = new SerializedObject(currency);
                var catalogProp = so.FindProperty("_catalog");
                if (catalogProp != null && catalogProp.objectReferenceValue == null)
                {
                    var catalog = AssetDatabase.LoadAssetAtPath<LogosGame.Features.Currency.Transactions.TransactionCatalog>(
                        "Assets/_Game/Content/SO_TransactionCatalog.asset");
                    if (catalog != null)
                    {
                        catalogProp.objectReferenceValue = catalog;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        added = true;
                        Debug.Log("WIRE: gán SO_TransactionCatalog vào CurrencyInstaller.");
                    }
                    else
                    {
                        Debug.LogError("WIRE FAIL: không thấy Assets/_Game/Content/SO_TransactionCatalog.asset");
                    }
                }
            }

            if (added)
            {
                PrefabUtility.SaveAsPrefabAsset(root, ScopePath);
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
