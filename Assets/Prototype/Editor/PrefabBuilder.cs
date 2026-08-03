// ============================================================================
// Dựng 5 prefab + Main.unity bằng code Editor. Mở bằng menu: WordStack ▸ Build Prefabs + Scene
//
// Vì sao có file này thay vì dựng tay theo checklist Mục 6 của docs/architecture/view-prefabs.md:
// mọi con số hình thù (1.76 · 1.60 · ±0.375 · order -13..101) đều đã nằm sẵn trong doc, mà dựng
// tay 5 hierarchy + kéo 20 tham chiếu thì sai một cái là Console báo mơ hồ. Để Unity tự serialize
// thì GUID/tham chiếu luôn đúng, và chạy lại được khi thiết kế đổi.
//
// Đây KHÔNG phải nguồn chân lý của thiết kế — doc mới là. File này chỉ chép doc thành API.
// Chạy lại sẽ GHI ĐÈ 5 prefab; hình thù chỉnh tay trong Editor sẽ mất, số feel trên
// BoardController/GhostView cũng về mặc định.
//
// Nằm trong Editor/ nên không vào player build.
// ============================================================================
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WordStack.Prototype
{
    static class PrefabBuilder
    {
        const string PrefabDir = "Assets/Prefabs";
        const string SceneDir = "Assets/Scenes";
        const string ScenePath = SceneDir + "/Main.unity";
        const string WhitePath = "Assets/Prototype/Sprites/white.png";

        // ---- số hình thù, chép từ Mục 2 + 3 của doc ----
        const float BoxSize = 1.6f;
        const float BoxBorder = 0.08f;
        const float SlotSize = 0.67f;          // = (1.6 - 2×0.09 - 0.08) / 2, khớp BoardController
        const float SlotStep = SlotSize + 0.08f;
        const float PeekStep = 0.10f;
        const int PeekMax = 3;

        static readonly Color Edge = Hex(0xA5A5A5);
        static readonly Color BoxBg = Hex(0x6B5CA8);
        static readonly Color PeekBg = Hex(0x544593);
        static readonly Color TileBg = Hex(0xDEDEDE);
        static readonly Color Ink = Hex(0x111111);
        static readonly Color Header = Hex(0x6A5BA5);
        static readonly Color CamBg = Hex(0x3A2E5F);

        [MenuItem("WordStack/Build Prefabs + Scene")]
        static void Build()
        {
            if (!EditorUtility.DisplayDialog("Dựng prefab + scene",
                    "Ghi đè 5 prefab trong " + PrefabDir + " và tạo lại " + ScenePath + ".\n\n" +
                    "Chỉnh tay trên mấy prefab đó (nếu có) sẽ mất.", "Dựng", "Thôi"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildAll();
        }

        // Tách khỏi Build() để gọi được mà không vướng hộp thoại — hộp thoại modal sẽ treo
        // mọi thứ gọi từ ngoài Editor (MCP bridge, batch mode).
        internal static void BuildAll()
        {
            Directory.CreateDirectory(PrefabDir);
            Directory.CreateDirectory(SceneDir);
            AssetDatabase.Refresh();

            var white = White();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var tile = BuildTile(white, font);
            var box = BuildBox(white);
            var stack = BuildStack(white, font);
            var ghost = BuildGhost(white);
            var hud = BuildHud(white, font);

            BuildScene(tile, box, stack, ghost, hud);

            AssetDatabase.SaveAssets();
            Debug.Log("Dựng xong 5 prefab + " + ScenePath + ". Bấm Play.");
        }

        // -------------------------------------------------------------- prefab

        static GameObject BuildTile(Sprite white, Font font)
        {
            var root = new GameObject("Tile", typeof(TileView));
            var bg = Quad(root.transform, "Bg", Vector2.zero, Vector2.one, TileBg, 3, white);
            var art = new GameObject("Art", typeof(SpriteRenderer));
            art.transform.SetParent(root.transform, false);
            art.GetComponent<SpriteRenderer>().sortingOrder = 4;
            var label = Text(root.transform, "Label", Vector2.zero, Ink, 4, font);

            var v = root.GetComponent<TileView>();
            Wire(v, "bg", bg);
            Wire(v, "art", art.GetComponent<SpriteRenderer>());
            Wire(v, "label", label);
            return Save(root);
        }

        static GameObject BuildBox(Sprite white)
        {
            var root = new GameObject("Box", typeof(BoxView));
            var edge = Quad(root.transform, "Edge", Vector2.zero,
                            Vector2.one * (BoxSize + BoxBorder * 2f), Edge, 0, white);
            Quad(root.transform, "Bg", Vector2.zero, Vector2.one * BoxSize, BoxBg, 1, white);

            var slots = new GameObject("Slots");
            slots.transform.SetParent(root.transform, false);
            var anchors = new Object[4];
            for (int i = 0; i < 4; i++)
            {
                int c = i % 2, r = i / 2;
                var a = new GameObject("Slot" + i);
                a.transform.SetParent(slots.transform, false);
                a.transform.localPosition = new Vector3((c - 0.5f) * SlotStep, (0.5f - r) * SlotStep, 0f);
                // Bóng đáy slot: thẻ mount ĐÈ lên nó, và nó cùng mờ đi khi hộp bị xoá.
                Quad(a.transform, "Shadow", Vector2.zero, Vector2.one * SlotSize,
                     new Color(0, 0, 0, 0.14f), 2, white);
                anchors[i] = a.transform;
            }

            var v = root.GetComponent<BoxView>();
            Wire(v, "edge", edge);
            WireArray(v, "slotAnchors", anchors);
            return Save(root);
        }

        static GameObject BuildStack(Sprite white, Font font)
        {
            var root = new GameObject("Stack", typeof(StackView));
            var boxAnchor = new GameObject("BoxAnchor");
            boxAnchor.transform.SetParent(root.transform, false);

            // Lớp hộp ẩn: tụt xuống để hở mép dưới, lớp sâu vẽ trước nên nằm sau.
            var peeks = new Object[PeekMax];
            for (int d = 1; d <= PeekMax; d++)
            {
                var p = new GameObject("Peek" + d);
                p.transform.SetParent(root.transform, false);
                p.transform.localPosition = new Vector3(0f, -PeekStep * d, 0f);
                Quad(p.transform, "Edge", Vector2.zero, Vector2.one * (BoxSize + BoxBorder * 2f),
                     Edge, -10 - d, white);
                Quad(p.transform, "Bg", Vector2.zero, Vector2.one * BoxSize, PeekBg, -9 - d, white);
                p.SetActive(false);
                peeks[d - 1] = p;
            }

            var overflow = Text(root.transform, "Overflow",
                                new Vector2(0f, -(BoxSize / 2f + PeekStep * PeekMax + 0.18f)),
                                new Color(1, 1, 1, 0.7f), 5, font);
            overflow.gameObject.SetActive(false);

            var v = root.GetComponent<StackView>();
            Wire(v, "boxAnchor", boxAnchor.transform);
            WireArray(v, "peekLayers", peeks);
            Wire(v, "overflow", overflow);
            return Save(root);
        }

        static GameObject BuildGhost(Sprite white)
        {
            var root = new GameObject("Ghost", typeof(GhostView));
            var tilt = new GameObject("Tilt");
            tilt.transform.SetParent(root.transform, false);
            var shadow = Quad(tilt.transform, "Shadow", new Vector2(0.05f, -0.06f),
                              Vector2.one * (SlotSize + 0.06f), new Color(0, 0, 0, 0.35f), 98, white);
            var anchor = new GameObject("TileAnchor");
            anchor.transform.SetParent(tilt.transform, false);

            var v = root.GetComponent<GhostView>();
            Wire(v, "tilt", tilt.transform);
            Wire(v, "tileAnchor", anchor.transform);
            Wire(v, "shadow", shadow.transform);
            return Save(root);
        }

        static GameObject BuildHud(Sprite white, Font font)
        {
            var root = new GameObject("Hud", typeof(HudView));
            // Bề rộng header + panel "hết nước" do HudView.Layout đặt theo kích thước bàn.
            var headerBg = Quad(root.transform, "HeaderBg", Vector2.zero, Vector2.one, Header, 5, white);
            var title = Text(root.transform, "Title", Vector2.zero, Color.white, 6, font);
            var help = Text(root.transform, "Help", Vector2.zero, new Color(1, 1, 1, 0.55f), 6, font);

            var win = new GameObject("WinPanel");
            win.transform.SetParent(root.transform, false);
            Quad(win.transform, "Overlay", Vector2.zero, Vector2.one * 60f,
                 new Color(0.08f, 0.05f, 0.16f, 0.82f), 50, white);
            var winTitle = Text(win.transform, "WinTitle", new Vector2(0f, 0.45f), Color.white, 51, font);
            var winHint = Text(win.transform, "WinHint", new Vector2(0f, -0.6f),
                               new Color(1, 1, 1, 0.8f), 51, font);
            win.SetActive(false);

            var stuck = new GameObject("StuckPanel");
            stuck.transform.SetParent(root.transform, false);
            var stuckBg = Quad(stuck.transform, "Bg", Vector2.zero, Vector2.one,
                               new Color(0.11f, 0.09f, 0.2f, 0.95f), 50, white);
            var stuckLabel = Text(stuck.transform, "StuckLabel", Vector2.zero, Color.white, 51, font);
            stuck.SetActive(false);

            var v = root.GetComponent<HudView>();
            Wire(v, "headerBg", headerBg.transform);
            Wire(v, "title", title);
            Wire(v, "help", help);
            Wire(v, "winPanel", win);
            Wire(v, "winTitle", winTitle);
            Wire(v, "winHint", winHint);
            Wire(v, "stuckPanel", stuck);
            Wire(v, "stuckBg", stuckBg.transform);
            Wire(v, "stuckLabel", stuckLabel);
            return Save(root);
        }

        // --------------------------------------------------------------- scene

        static void BuildScene(GameObject tile, GameObject box, GameObject stack,
                               GameObject ghost, GameObject hud)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = CamBg;

            var game = new GameObject("Game", typeof(BoardController));
            var bc = game.GetComponent<BoardController>();
            Wire(bc, "stackPrefab", stack.GetComponent<StackView>());
            Wire(bc, "boxPrefab", box.GetComponent<BoxView>());
            Wire(bc, "tilePrefab", tile.GetComponent<TileView>());
            Wire(bc, "ghostPrefab", ghost.GetComponent<GhostView>());
            Wire(bc, "hudPrefab", hud.GetComponent<HudView>());

            EditorSceneManager.SaveScene(scene, ScenePath);

            if (!EditorBuildSettings.scenes.Any(s => s.path == ScenePath))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes
                    .Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
        }

        // -------------------------------------------------------------- helper

        static GameObject Save(GameObject go)
        {
            string path = PrefabDir + "/" + go.name + ".prefab";
            var asset = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return asset;
        }

        static SpriteRenderer Quad(Transform parent, string name, Vector2 pos, Vector2 size,
                                   Color color, int order, Sprite white)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = white;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        // characterSize do ViewText.Apply đặt lúc chạy; ở đây chỉ dựng hình + màu + order.
        static TextMesh Text(Transform parent, string name, Vector2 pos, Color color, int order, Font font)
        {
            var go = new GameObject(name, typeof(TextMesh));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            var tm = go.GetComponent<TextMesh>();
            tm.font = font;
            tm.fontSize = 64;
            tm.characterSize = 0.021f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = font.material;
            mr.sortingOrder = order;
            return tm;
        }

        // Sprite trắng 1×1 với PixelsPerUnit = 1: scale của SpriteRenderer CHÍNH LÀ kích thước
        // world. Tự sinh thay vì mượn asset builtin để không phụ thuộc GUID nội bộ của Unity.
        static Sprite White()
        {
            if (!File.Exists(WhitePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(WhitePath));
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                File.WriteAllBytes(WhitePath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(WhitePath, ImportAssetOptions.ForceSynchronousImport);
            }
            var imp = (TextureImporter)AssetImporter.GetAtPath(WhitePath);
            if (imp.textureType != TextureImporterType.Sprite || imp.spritePixelsPerUnit != 1f)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 1f;
                imp.filterMode = FilterMode.Point;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(WhitePath);
        }

        static void Wire(Component c, string field, Object value)
        {
            var so = new SerializedObject(c);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError(c.GetType().Name + " không có field '" + field + "'"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireArray(Component c, string field, Object[] values)
        {
            var so = new SerializedObject(c);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError(c.GetType().Name + " không có field '" + field + "'"); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
        }
    }
}
