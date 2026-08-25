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

            if (root.GetComponent<ShopInstaller>() == null)
            {
                root.AddComponent<ShopInstaller>();
                added = true;
                Debug.Log("WIRE: thêm ShopInstaller vào ProjectScope.");
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

            // Shop: khác hai cái còn lại — asset CHƯA tồn tại thì tự tạo rỗng luôn
            // (GD điền gói coin + mã giao dịch trong Inspector sau), vì không có
            // asset nào để trỏ tới thì shop mở ra trống trơn mà chẳng ai biết vì sao.
            var shop = root.GetComponent<ShopInstaller>();
            if (shop != null)
            {
                var so = new SerializedObject(shop);
                var shopCatalogProp = so.FindProperty("_shopCatalog");
                if (shopCatalogProp != null && shopCatalogProp.objectReferenceValue == null)
                {
                    const string shopCatalogPath = "Assets/_Game/Content/SO_ShopCatalog.asset";
                    var shopCatalog =
                        AssetDatabase.LoadAssetAtPath<LogosGame.Features.Shop.ShopCatalog>(shopCatalogPath);

                    if (shopCatalog == null)
                    {
                        shopCatalog = ScriptableObject.CreateInstance<LogosGame.Features.Shop.ShopCatalog>();
                        AssetDatabase.CreateAsset(shopCatalog, shopCatalogPath);
                        Debug.Log($"WIRE: tạo {shopCatalogPath} (RỖNG — điền gói coin + mã giao dịch trong Inspector).");
                    }

                    shopCatalogProp.objectReferenceValue = shopCatalog;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    added = true;
                    Debug.Log("WIRE: gán SO_ShopCatalog vào ShopInstaller.");
                }
            }

            // Lịch mở khoá booster: gán y như catalog ở trên. Thiếu asset thì
            // BoosterPurchaseFlow chỉ mất icon + tên riêng, không sập — nên chỉ warn.
            var progression = root.GetComponent<ProgressionInstaller>();
            if (progression != null)
            {
                var so = new SerializedObject(progression);
                var scheduleProp = so.FindProperty("_unlockSchedule");
                if (scheduleProp != null && scheduleProp.objectReferenceValue == null)
                {
                    var schedule = AssetDatabase.LoadAssetAtPath<LogosGame.Features.Gameplay.Content.SO_UnlockSchedule>(
                        "Assets/_Game/Content/SO_UnlockSchedule.asset");
                    if (schedule != null)
                    {
                        scheduleProp.objectReferenceValue = schedule;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        added = true;
                        Debug.Log("WIRE: gán SO_UnlockSchedule vào ProgressionInstaller.");
                    }
                    else
                    {
                        Debug.LogWarning("WIRE: không thấy Assets/_Game/Content/SO_UnlockSchedule.asset — popup mua booster sẽ thiếu icon.");
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
