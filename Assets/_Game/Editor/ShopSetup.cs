using System.Linq;
using LogosGame.Features.Shop;
using LogosGame.Features.UI.Popups;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace WordStack.Meta.Editor
{
    /// <summary>
    /// Dựng trọn Shop bằng một menu: SO_ShopCatalog (chỉ điền khi còn rỗng — không đè số GD đã chỉnh),
    /// ShopInstaller trên ProjectScope, prefab ShopPopup + ô gói (dùng chung cho gói coin và gói combo),
    /// address Addressables "ShopPopup". Catalog đã có dữ liệu thì giữ nguyên, nhưng 2 prefab bị dựng
    /// lại từ đầu — chạy lại là MẤT mọi chỉnh tay trên ShopPopup / ShopCoinCell.
    /// Khung popup (nền, nút X, ô coin, transition) mượn từ BoosterPurchasePopup cho cùng style.
    /// </summary>
    internal static class ShopSetup
    {
        private const string CatalogPath = "Assets/_Game/Content/SO_ShopCatalog.asset";
        private const string ScopePath = "Assets/Prefabs/ProjectScope.prefab";
        private const string TemplatePath = "Assets/_Shared/Prefab/Popup/BoosterPurchasePopup.prefab";
        private const string PopupPath = "Assets/_Shared/Prefab/Popup/ShopPopup.prefab";
        private const string CoinCellPath = "Assets/_Shared/Prefab/Popup/ShopCoinCell.prefab";
        private const string ShopArt = "Assets/_Game/Art/UI_New/Shop/";
        private const string CellBgPath = "Assets/_Game/Art/UI_New/Pop-Up/Popup In.png";
        private const string PopupAddress = "ShopPopup"; // UIManager load theo typeof(TPopup).Name

        private static readonly Vector2 CellSize = new Vector2(240f, 300f);

        [MenuItem("WordStack/Setup/Build Shop")]
        private static void Run()
        {
            ShopCatalog catalog = EnsureCatalog();
            WireInstaller(catalog);
            if (!BuildPrefabs()) return;
            RegisterAddress();
            AssetDatabase.SaveAssets();
            Debug.Log("SHOP: xong — catalog, ShopInstaller, ShopPopup prefab, address 'ShopPopup'.");
        }

        // --- Catalog ----------------------------------------------------------

        private static ShopCatalog EnsureCatalog()
        {
            ShopCatalog catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ShopCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
                Debug.Log($"SHOP: tạo {CatalogPath}.");
            }

            var so = new SerializedObject(catalog);

            // Giá chỉ là nhãn dự phòng (đề xuất, GD chốt) — giá thật store trả theo vùng.
            (string id, int coins, string price, int icon, ShopTag tag)[] rows =
            {
                (ShopProductIds.Coins1000, 1000, "0.99 $", 1, ShopTag.None),
                (ShopProductIds.Coins5000, 5000, "3.99 $", 2, ShopTag.None),
                (ShopProductIds.Coins10000, 10000, "6.99 $", 3, ShopTag.Popular),
                (ShopProductIds.Coins25000, 25000, "14.99 $", 4, ShopTag.None),
                (ShopProductIds.Coins50000, 50000, "24.99 $", 5, ShopTag.None),
                (ShopProductIds.Coins100000, 100000, "39.99 $", 6, ShopTag.BestValue),
            };

            SerializedProperty bundles = so.FindProperty("_coinBundles");
            if (bundles.arraySize == 0)
            {
                bundles.arraySize = rows.Length;
                for (int i = 0; i < rows.Length; i++)
                {
                    SerializedProperty e = bundles.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("ProductId").stringValue = rows[i].id;
                    e.FindPropertyRelative("Coins").intValue = rows[i].coins;
                    e.FindPropertyRelative("PriceLabelFallback").stringValue = rows[i].price;
                    e.FindPropertyRelative("Icon").objectReferenceValue = LoadSprite($"{ShopArt}Icon Shop Coin {rows[i].icon}.png");
                    e.FindPropertyRelative("Tag").enumValueIndex = (int)rows[i].tag;
                }
                Debug.Log("SHOP: điền 6 gói coin vào catalog.");
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void WireInstaller(ShopCatalog catalog)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ScopePath);
            try
            {
                ShopInstaller installer = root.GetComponent<ShopInstaller>();
                if (installer == null) installer = root.AddComponent<ShopInstaller>();
                SetRef(installer, "_shopCatalog", catalog);
                PrefabUtility.SaveAsPrefabAsset(root, ScopePath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // --- Prefabs ----------------------------------------------------------

        private static bool BuildPrefabs()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(TemplatePath);
            try
            {
                Transform box = root.transform.Find("Box ");
                Transform closeButton = box != null ? box.Find("X Button") : null;
                Transform buttonTpl = box != null ? box.Find("Coin Button") : null;
                Transform titleTpl = box != null ? box.Find("Image/Title Text (TMP)") : null;
                Transform coinText = root.transform.Find("CoinArea/CoinBox/CoinTxt");
                if (box == null || closeButton == null || buttonTpl == null || titleTpl == null || coinText == null)
                {
                    Debug.LogError($"SHOP FAIL: {TemplatePath} đổi cấu trúc — cần 'Box '/X Button, Coin Button, Image/Title Text (TMP), CoinArea/CoinBox/CoinTxt.");
                    return false;
                }

                var textTpl = titleTpl.GetComponent<TextMeshProUGUI>();

                ShopCoinCellView coinCell = BuildCoinCell(root.transform, buttonTpl, textTpl);

                // Khung popup: bỏ nội dung booster, giữ nền / nút X / tiêu đề / ô coin.
                Object.DestroyImmediate(root.GetComponent<BoosterPurchasePopup>());
                foreach (string child in new[] { "Revive Image", "Ad Button" })
                {
                    Transform t = box.Find(child);
                    if (t != null) Object.DestroyImmediate(t.gameObject);
                }
                root.name = "ShopPopup";
                textTpl.text = "SHOP";

                Button coinTab = CloneButton(buttonTpl, box, "Coin Tab", "COINS", new Vector2(-140f, 360f), new Vector2(250f, 90f));
                Button itemTab = CloneButton(buttonTpl, box, "Item Tab", "PACKS", new Vector2(140f, 360f), new Vector2(250f, 90f));
                Button restore = CloneButton(buttonTpl, box, "Restore Button", "RESTORE", new Vector2(0f, -440f), new Vector2(220f, 70f));
                Object.DestroyImmediate(buttonTpl.gameObject);

                Transform coinGrid = BuildScrollGrid(box, "Coin Tab Root", out GameObject coinTabRoot);
                Transform itemGrid = BuildScrollGrid(box, "Item Tab Root", out GameObject itemTabRoot);

                var popup = root.AddComponent<ShopPopup>();
                SetRef(popup, "_coinCounterText", coinText.GetComponent<TextMeshProUGUI>());
                SetRef(popup, "_closeButton", closeButton.GetComponent<Button>());
                SetRef(popup, "_coinTabButton", coinTab);
                SetRef(popup, "_itemTabButton", itemTab);
                SetRef(popup, "_coinTabRoot", coinTabRoot);
                SetRef(popup, "_itemTabRoot", itemTabRoot);
                SetRef(popup, "_coinGridRoot", coinGrid);
                SetRef(popup, "_coinCellPrefab", coinCell);
                SetRef(popup, "_itemGridRoot", itemGrid);
                SetRef(popup, "_restoreButton", restore);

                PrefabUtility.SaveAsPrefabAsset(root, PopupPath);
                return true;
            }
            finally
            {
                // Không lưu ngược vào template — BoosterPurchasePopup giữ nguyên.
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static ShopCoinCellView BuildCoinCell(Transform scratch, Transform buttonTpl, TextMeshProUGUI textTpl)
        {
            RectTransform cell = NewUI("ShopCoinCell", scratch);
            cell.sizeDelta = CellSize;
            cell.gameObject.AddComponent<Image>().sprite = LoadSprite(CellBgPath);

            RectTransform icon = NewUI("Icon", cell);
            Place(icon, new Vector2(0f, 60f), new Vector2(110f, 110f));
            var iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.preserveAspect = true;

            TextMeshProUGUI coins = CloneText(textTpl, cell, "Coins", "1,000", new Vector2(0f, -45f), new Vector2(220f, 40f));

            // Chỉ gói combo dùng tới: ShopCoinCellView tự ẩn khi gói không có Title/Items.
            TextMeshProUGUI title = CloneText(textTpl, cell, "Title", "Starter Pack", new Vector2(0f, 128f), new Vector2(220f, 36f));
            TextMeshProUGUI items = CloneText(textTpl, cell, "Items", "+5 shuffle", new Vector2(0f, -10f), new Vector2(220f, 30f));
            items.fontSizeMax = 24f;
            Button buy = CloneButton(buttonTpl, cell, "Buy Button", "0.99 $", new Vector2(0f, -110f), new Vector2(200f, 80f));

            GameObject popular = NewBadge(cell, "Popular Badge", $"{ShopArt}Icon Shop Tag 1.png");
            GameObject bestValue = NewBadge(cell, "BestValue Badge", $"{ShopArt}Icon Shop Tag 2.png");

            var view = cell.gameObject.AddComponent<ShopCoinCellView>();
            SetRef(view, "_icon", iconImage);
            SetRef(view, "_coinsText", coins);
            SetRef(view, "_priceText", buy.GetComponentInChildren<TextMeshProUGUI>(true));
            SetRef(view, "_titleText", title);
            SetRef(view, "_itemsText", items);
            SetRef(view, "_popularBadge", popular);
            SetRef(view, "_bestValueBadge", bestValue);
            SetRef(view, "_buyButton", buy);

            return SaveCell<ShopCoinCellView>(cell.gameObject, CoinCellPath);
        }

        private static T SaveCell<T>(GameObject cell, string path) where T : Component
        {
            PrefabUtility.SaveAsPrefabAsset(cell, path);
            Object.DestroyImmediate(cell);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static Transform BuildScrollGrid(Transform box, string name, out GameObject tabRoot)
        {
            RectTransform rootRt = NewUI(name, box);
            Stretch(rootRt, left: 40f, right: 40f, top: 220f, bottom: 130f);
            tabRoot = rootRt.gameObject;

            var scroll = tabRoot.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            RectTransform viewport = NewUI("Viewport", rootRt);
            Stretch(viewport, 0f, 0f, 0f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = NewUI("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = CellSize;
            grid.spacing = new Vector2(20f, 20f);
            grid.padding = new RectOffset(0, 0, 10, 10);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        // --- UI helpers -------------------------------------------------------

        private static Button CloneButton(Transform tpl, Transform parent, string name, string label,
            Vector2 pos, Vector2 size)
        {
            GameObject go = Object.Instantiate(tpl.gameObject, parent);
            go.name = name;
            Transform icn = go.transform.Find("Coin icn");
            if (icn != null) Object.DestroyImmediate(icn.gameObject);

            Place((RectTransform)go.transform, pos, size);

            TextMeshProUGUI text = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
            {
                text.text = label;
                text.enableAutoSizing = true;
                Stretch(text.rectTransform, 10f, 10f, 5f, 5f);
            }

            return go.GetComponent<Button>();
        }

        private static TextMeshProUGUI CloneText(TextMeshProUGUI tpl, Transform parent, string name, string text,
            Vector2 pos, Vector2 size)
        {
            GameObject go = Object.Instantiate(tpl.gameObject, parent);
            go.name = name;
            Place((RectTransform)go.transform, pos, size);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.enableAutoSizing = true;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private static GameObject NewBadge(Transform cell, string name, string spritePath)
        {
            RectTransform rt = NewUI(name, cell);
            Place(rt, new Vector2(80f, 120f), new Vector2(100f, 100f));
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(spritePath);
            image.preserveAspect = true;
            rt.gameObject.SetActive(false);
            return rt.gameObject;
        }

        private static RectTransform NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static void Place(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt, float left, float right, float top, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        // Ảnh import dạng Multiple — sprite là sub-asset.
        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (sprite == null) Debug.LogWarning($"SHOP: không thấy sprite ở {path}.");
            return sprite;
        }

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"SHOP FAIL: {target.GetType().Name} không có field '{field}'.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // --- Addressables -----------------------------------------------------

        private static void RegisterAddress()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("SHOP FAIL: project chưa có Addressables settings.");
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(PopupPath);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.address = PopupAddress;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true);
        }
    }
}
