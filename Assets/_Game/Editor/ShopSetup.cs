using System.Linq;
using LogosGame.Features.Currency;
using LogosGame.Features.Shop;
using LogosGame.Features.UI.Popups;
using LogosSDK.UI.Components;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace WordStack.Meta.Editor
{
    /// <summary>
    /// Dựng trọn Shop một trang (spec 2026-10-01-shop-single-page) bằng một menu: SO_ShopCatalog (chỉ điền phần còn rỗng — không đè số GD đã chỉnh),
    /// ShopInstaller trên ProjectScope, prefab ShopPopup dạng panel toàn màn hình kiểu Beads Drop, address "ShopPopup".
    /// ShopPopup bị dựng lại từ đầu — chạy lại là MẤT mọi chỉnh tay trên nó. Ba prefab ô (ShopCoinCell, ShopComboCell,
    /// ShopRewardItem) chỉ được tạo khi CHƯA có file — có rồi thì dùng nguyên, chỉnh tay trên chúng được giữ.
    /// Root (transition, nút X, ô coin) mượn từ BoosterPurchasePopup cho cùng style; khung hộp popup thì bỏ.
    /// </summary>
    internal static class ShopSetup
    {
        private const string CatalogPath = "Assets/_Game/Content/SO_ShopCatalog.asset";
        private const string ScopePath = "Assets/Prefabs/ProjectScope.prefab";
        private const string TemplatePath = "Assets/_Shared/Prefab/Popup/BoosterPurchasePopup.prefab";
        private const string PopupPath = "Assets/_Shared/Prefab/Popup/ShopPopup.prefab";
        private const string CoinCellPath = "Assets/_Shared/Prefab/Popup/ShopCoinCell.prefab";
        private const string ComboCellPath = "Assets/_Shared/Prefab/Popup/ShopComboCell.prefab";
        private const string RewardItemPath = "Assets/_Shared/Prefab/Popup/ShopRewardItem.prefab";
        private const string ShopArt = "Assets/_Game/Art/UI_New/Shop/";
        private const string CellBgPath = "Assets/_Game/Art/UI_New/Pop-Up/Popup In.png";
        private const string ComboBgPath = ShopArt + "Bundle Pack 1.png";
        private const string BannerBgPath = ShopArt + "Bundle Pack 2.png";
        private const string NoAdsIconPath = "Assets/_Game/Art/UI_New/UI Icon/Icon No Ads (big).png";
        private const string PanelBgPath = "Assets/_Game/Art/UI_New/Background/BG Home.png";
        private const string HeaderBgPath = "Assets/_Game/Art/UI_New/Main/Main Navigation Header.png";
        private const string TitlePath = "Assets/_Game/Art/UI_New/Pop-Up/Title Main Shop.png";
        private const string PopupAddress = "ShopPopup"; // UIManager load theo typeof(TPopup).Name

        private static readonly Vector2 CellSize = new Vector2(240f, 300f);
        private const float BannerHeight = 170f, ComboHeight = 210f, RestoreHeight = 70f;
        private const float HeaderHeight = 240f, SectionTitleHeight = 64f;

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

            SerializedProperty removeAds = so.FindProperty("_removeAds");
            if (string.IsNullOrEmpty(removeAds.FindPropertyRelative("ProductId").stringValue))
            {
                removeAds.FindPropertyRelative("ProductId").stringValue = ShopProductIds.RemoveAds;
                removeAds.FindPropertyRelative("PriceLabelFallback").stringValue = "4.99 $";
                removeAds.FindPropertyRelative("Icon").objectReferenceValue = LoadSprite(NoAdsIconPath);
                removeAds.FindPropertyRelative("Title").stringValue = "Remove ads";
                removeAds.FindPropertyRelative("Subtitle").stringValue = "Mua một lần, giữ mãi";
                Debug.Log("SHOP: điền Remove Ads vào catalog.");
            }

            (ResourceType type, string path)[] icons =
            {
                (ResourceType.Coin, $"{ShopArt}Icon Shop Coin 1.png"),
                (ResourceType.Heart, "Assets/_Game/Art/UI/more lives/heart.png"),
                (ResourceType.UnlimitedHeart, "Assets/_Game/Art/UI/more lives/heart.png"),
                (ResourceType.BoosterShuffle, "Assets/_Game/Art/Sprites/Shuffle.png"),
                (ResourceType.BoosterMagnet, "Assets/_Game/Art/UI/Booster/magnet.png"),
                (ResourceType.BoosterUndo, "Assets/_Game/Art/Sprites/Undo.png"),
            };
            SerializedProperty rewardIcons = so.FindProperty("_rewardIcons");
            if (rewardIcons.arraySize == 0)
            {
                rewardIcons.arraySize = icons.Length;
                for (int i = 0; i < icons.Length; i++)
                {
                    SerializedProperty e = rewardIcons.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("Type").enumValueIndex = (int)icons[i].type;
                    e.FindPropertyRelative("Icon").objectReferenceValue = LoadSprite(icons[i].path);
                }
                Debug.Log("SHOP: điền icon quà vào catalog.");
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

                // Prefab ô đã có thì dùng nguyên (GD chỉnh tay trên đó), chỉ dựng khi chưa có file.
                ShopCoinCellView coinCell = AssetDatabase.LoadAssetAtPath<ShopCoinCellView>(CoinCellPath);
                if (coinCell == null) coinCell = BuildCoinCell(root.transform, buttonTpl, textTpl);
                ShopComboCellView comboCell = AssetDatabase.LoadAssetAtPath<ShopComboCellView>(ComboCellPath);
                if (comboCell == null)
                {
                    ShopRewardItemView rewardItem = AssetDatabase.LoadAssetAtPath<ShopRewardItemView>(RewardItemPath);
                    if (rewardItem == null) rewardItem = BuildRewardItem(root.transform, textTpl);
                    comboCell = BuildComboCell(root.transform, buttonTpl, textTpl, rewardItem);
                }

                // Panel toàn màn hình kiểu Beads Drop: nền đục thay lớp tối của popup, bỏ khung hộp.
                Object.DestroyImmediate(root.GetComponent<BoosterPurchasePopup>());
                root.name = "ShopPopup";
                var rootImage = root.GetComponent<Image>();
                if (rootImage != null)
                {
                    rootImage.sprite = LoadSprite(PanelBgPath);
                    rootImage.color = Color.white;
                    rootImage.type = Image.Type.Simple;
                }

                // Nền tràn màn, mọi thứ bấm được nằm trong vùng an toàn (cùng lối MainMenuScreen).
                RectTransform safeArea = NewUI("Safe Area", root.transform);
                Stretch(safeArea, 0f, 0f, 0f, 0f);
                safeArea.gameObject.AddComponent<SafeAreaFitter>();

                BuildHeader(safeArea, root.transform.Find("CoinArea"), closeButton);

                RectTransform content = BuildScrollPage(safeArea);
                ShopRemoveAdsView banner = BuildRemoveAdsBanner(content, buttonTpl, textTpl);
                AddSectionTitle(content, textTpl, "Packs Title", "PACKS");
                Transform comboList = BuildVerticalList(content, "Combo List", 16f);
                AddSectionTitle(content, textTpl, "Coins Title", "COINS");
                Transform coinGrid = BuildCoinGrid(content);
                // Hàng giữ nút: content kéo dãn con theo chiều ngang, nút đặt thẳng vào sẽ dài full bề rộng.
                RectTransform restoreRow = NewUI("Restore Row", content);
                var restoreLayout = restoreRow.gameObject.AddComponent<HorizontalLayoutGroup>();
                restoreLayout.childAlignment = TextAnchor.MiddleCenter;
                restoreLayout.childControlWidth = false;
                restoreLayout.childControlHeight = false;
                restoreLayout.childForceExpandWidth = false;
                AddLayoutHeight(restoreRow.gameObject, RestoreHeight);
                Button restore = CloneButton(buttonTpl, restoreRow, "Restore Button", "RESTORE", Vector2.zero, new Vector2(220f, RestoreHeight));

                // Hộp popup cũ chỉ còn làm khuôn clone (nút, chữ) — X Button đã chuyển sang header.
                Object.DestroyImmediate(box.gameObject);

                var popup = root.AddComponent<ShopPopup>();
                SetRef(popup, "_coinCounterText", coinText.GetComponent<TextMeshProUGUI>());
                SetRef(popup, "_closeButton", closeButton.GetComponent<Button>());
                SetRef(popup, "_removeAdsView", banner);
                SetRef(popup, "_comboListRoot", comboList);
                SetRef(popup, "_comboCellPrefab", comboCell);
                SetRef(popup, "_coinGridRoot", coinGrid);
                SetRef(popup, "_coinCellPrefab", coinCell);
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
            Place(icon, new Vector2(0f, 50f), new Vector2(120f, 120f));
            var iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.preserveAspect = true;

            TextMeshProUGUI coins = CloneText(textTpl, cell, "Coins", "1,000", new Vector2(0f, -40f), new Vector2(220f, 40f));
            Button buy = CloneButton(buttonTpl, cell, "Buy Button", "0.99 $", new Vector2(0f, -105f), new Vector2(200f, 80f));

            GameObject popular = NewBadge(cell, "Popular Badge", $"{ShopArt}Icon Shop Tag 1.png");
            GameObject bestValue = NewBadge(cell, "BestValue Badge", $"{ShopArt}Icon Shop Tag 2.png");

            var view = cell.gameObject.AddComponent<ShopCoinCellView>();
            SetRef(view, "_icon", iconImage);
            SetRef(view, "_coinsText", coins);
            SetRef(view, "_priceText", buy.GetComponentInChildren<TextMeshProUGUI>(true));
            SetRef(view, "_popularBadge", popular);
            SetRef(view, "_bestValueBadge", bestValue);
            SetRef(view, "_buyButton", buy);

            return SaveCell<ShopCoinCellView>(cell.gameObject, CoinCellPath);
        }

        private static ShopRewardItemView BuildRewardItem(Transform scratch, TextMeshProUGUI textTpl)
        {
            RectTransform item = NewUI("ShopRewardItem", scratch);
            item.sizeDelta = new Vector2(110f, 50f);
            var row = item.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 4f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            RectTransform icon = NewUI("Icon", item);
            icon.sizeDelta = new Vector2(44f, 44f);
            var iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.preserveAspect = true;

            TextMeshProUGUI amount = CloneText(textTpl, item, "Amount", "x5", Vector2.zero, new Vector2(62f, 44f));
            amount.alignment = TextAlignmentOptions.Left;

            var view = item.gameObject.AddComponent<ShopRewardItemView>();
            SetRef(view, "_icon", iconImage);
            SetRef(view, "_amountText", amount);

            return SaveCell<ShopRewardItemView>(item.gameObject, RewardItemPath);
        }

        private static ShopComboCellView BuildComboCell(Transform scratch, Transform buttonTpl, TextMeshProUGUI textTpl,
            ShopRewardItemView rewardItem)
        {
            RectTransform cell = NewUI("ShopComboCell", scratch);
            cell.sizeDelta = new Vector2(760f, ComboHeight);
            cell.gameObject.AddComponent<Image>().sprite = LoadSprite(ComboBgPath);

            RectTransform icon = NewUI("Icon", cell);
            AnchorLeft(icon, 20f, new Vector2(160f, 160f));
            var iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.preserveAspect = true;

            TextMeshProUGUI title = CloneText(textTpl, cell, "Title", "Starter Pack", Vector2.zero, new Vector2(380f, 50f));
            AnchorLeft(title.rectTransform, 200f, new Vector2(380f, 50f), y: 55f);
            title.alignment = TextAlignmentOptions.Left;

            RectTransform rewards = NewUI("Rewards", cell);
            AnchorLeft(rewards, 200f, new Vector2(380f, 110f), y: -30f);
            var grid = rewards.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(110f, 50f);
            grid.spacing = new Vector2(8f, 6f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperLeft;

            Button buy = CloneButton(buttonTpl, cell, "Buy Button", "1.99 $", Vector2.zero, new Vector2(150f, 80f));
            var buyRt = (RectTransform)buy.transform;
            buyRt.anchorMin = buyRt.anchorMax = buyRt.pivot = new Vector2(1f, 0.5f);
            buyRt.anchoredPosition = new Vector2(-20f, 0f);

            AddLayoutHeight(cell.gameObject, ComboHeight);

            var view = cell.gameObject.AddComponent<ShopComboCellView>();
            SetRef(view, "_icon", iconImage);
            SetRef(view, "_titleText", title);
            SetRef(view, "_rewardRoot", rewards);
            SetRef(view, "_rewardItemPrefab", rewardItem);
            SetRef(view, "_priceText", buy.GetComponentInChildren<TextMeshProUGUI>(true));
            SetRef(view, "_buyButton", buy);

            return SaveCell<ShopComboCellView>(cell.gameObject, ComboCellPath);
        }

        private static T SaveCell<T>(GameObject cell, string path) where T : Component
        {
            PrefabUtility.SaveAsPrefabAsset(cell, path);
            Object.DestroyImmediate(cell);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        // Thanh trên cùng kiểu Beads Drop: ô coin bên trái, ảnh tiêu đề giữa, nút X bên phải. Không cuộn.
        private static void BuildHeader(Transform safeArea, Transform coinArea, Transform closeButton)
        {
            RectTransform header = NewUI("Header", safeArea);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = new Vector2(0f, HeaderHeight);
            var headerImage = header.gameObject.AddComponent<Image>();
            headerImage.sprite = LoadSprite(HeaderBgPath);

            if (coinArea != null)
            {
                coinArea.SetParent(header, false);
                Stretch((RectTransform)coinArea, 0f, 0f, 0f, 0f);
                Transform coinBox = coinArea.Find("CoinBox");
                if (coinBox != null)
                {
                    var coinBoxRt = (RectTransform)coinBox;
                    coinBoxRt.anchorMin = coinBoxRt.anchorMax = coinBoxRt.pivot = new Vector2(0f, 0.5f);
                    coinBoxRt.anchoredPosition = new Vector2(40f, 0f);
                }
            }

            RectTransform title = NewUI("Title", header);
            Place(title, Vector2.zero, new Vector2(440f, 180f));
            var titleImage = title.gameObject.AddComponent<Image>();
            titleImage.sprite = LoadSprite(TitlePath);
            titleImage.preserveAspect = true;

            closeButton.SetParent(header, false);
            var closeRt = (RectTransform)closeButton;
            closeRt.anchorMin = closeRt.anchorMax = closeRt.pivot = new Vector2(1f, 0.5f);
            closeRt.anchoredPosition = new Vector2(-40f, 0f);
        }

        // Dải tiêu đề một phần trong trang cuộn (SectionBanner của Beads Drop) — tạm là chữ, đổi ảnh trong prefab.
        private static void AddSectionTitle(Transform content, TextMeshProUGUI textTpl, string name, string text)
        {
            TextMeshProUGUI title = CloneText(textTpl, content, name, text, Vector2.zero, new Vector2(400f, SectionTitleHeight));
            AddLayoutHeight(title.gameObject, SectionTitleHeight);
        }

        // Vùng cuộn dọc duy nhất dưới header; con xếp theo thứ tự thêm vào.
        private static RectTransform BuildScrollPage(Transform safeArea)
        {
            RectTransform rootRt = NewUI("Scroll", safeArea);
            Stretch(rootRt, left: 30f, right: 30f, top: HeaderHeight + 10f, bottom: 20f);

            var scroll = rootRt.gameObject.AddComponent<ScrollRect>();
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

            var column = content.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 24f;
            column.padding = new RectOffset(0, 0, 10, 20);
            column.childAlignment = TextAnchor.UpperCenter;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        private static ShopRemoveAdsView BuildRemoveAdsBanner(Transform content, Transform buttonTpl, TextMeshProUGUI textTpl)
        {
            RectTransform banner = NewUI("Remove Ads Banner", content);
            banner.gameObject.AddComponent<Image>().sprite = LoadSprite(BannerBgPath);
            AddLayoutHeight(banner.gameObject, BannerHeight);

            RectTransform icon = NewUI("Icon", banner);
            AnchorLeft(icon, 20f, new Vector2(130f, 130f));
            var iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.sprite = LoadSprite(NoAdsIconPath);

            TextMeshProUGUI title = CloneText(textTpl, banner, "Title", "Remove ads", Vector2.zero, new Vector2(380f, 56f));
            AnchorLeft(title.rectTransform, 170f, new Vector2(380f, 56f), y: 28f);
            title.alignment = TextAlignmentOptions.Left;

            TextMeshProUGUI subtitle = CloneText(textTpl, banner, "Subtitle", "Mua một lần, giữ mãi", Vector2.zero, new Vector2(380f, 40f));
            AnchorLeft(subtitle.rectTransform, 170f, new Vector2(380f, 40f), y: -28f);
            subtitle.alignment = TextAlignmentOptions.Left;
            subtitle.fontSizeMax = 28f;

            Button buy = CloneButton(buttonTpl, banner, "Buy Button", "4.99 $", Vector2.zero, new Vector2(150f, 80f));
            var buyRt = (RectTransform)buy.transform;
            buyRt.anchorMin = buyRt.anchorMax = buyRt.pivot = new Vector2(1f, 0.5f);
            buyRt.anchoredPosition = new Vector2(-20f, 0f);

            var view = banner.gameObject.AddComponent<ShopRemoveAdsView>();
            SetRef(view, "_icon", iconImage);
            SetRef(view, "_titleText", title);
            SetRef(view, "_subtitleText", subtitle);
            SetRef(view, "_priceText", buy.GetComponentInChildren<TextMeshProUGUI>(true));
            SetRef(view, "_buyButton", buy);
            return view;
        }

        private static Transform BuildVerticalList(Transform content, string name, float spacing)
        {
            RectTransform list = NewUI(name, content);
            var column = list.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = spacing;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            return list;
        }

        private static Transform BuildCoinGrid(Transform content)
        {
            RectTransform gridRt = NewUI("Coin Grid", content);
            var grid = gridRt.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = CellSize;
            grid.spacing = new Vector2(20f, 20f);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            return gridRt;
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

        private static void AnchorLeft(RectTransform rt, float left, Vector2 size, float y = 0f)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(left, y);
            rt.sizeDelta = size;
        }

        // Trong VerticalLayoutGroup điều khiển chiều cao: LayoutElement chốt chiều cao ô.
        private static void AddLayoutHeight(GameObject go, float height)
        {
            var element = go.GetComponent<LayoutElement>();
            if (element == null) element = go.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
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
