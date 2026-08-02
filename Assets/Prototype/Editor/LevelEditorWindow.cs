// ============================================================================
// THROWAWAY PROTOTYPE — tool xếp level trong Unity Editor. KHÔNG mang sang production.
// Mở bằng menu: WordStack ▸ Level Editor
//
// Nhận file .json theo schema ở Assets/Prototype/Resources/Levels/, dựng lên Inspector
// để sửa, kéo-thả Sprite từ Project Window vào field art, lưu ngược ra .json.
//
// Vì sao lưu art bằng TÊN FILE chứ không phải path/GUID: runtime load bằng
// Resources.Load<Sprite>("Art/" + key) — đó là cách duy nhất load asset bằng chuỗi mà
// không cần Addressables. Tool ép sprite phải nằm dưới ArtRoot để đường đó luôn resolve
// được; kéo sprite ngoài gốc thì CHẶN LƯU chứ không lưu im lặng rồi vỡ lúc chơi.
//
// Nằm trong thư mục Editor/ nên tự động không vào player build.
// ============================================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace WordStack.Prototype
{
    // ---- Bản dựng để Unity tự vẽ Inspector. Sprite là object field thật nên
    // ---- kéo-thả từ Project Window hoạt động sẵn, không phải viết GUI riêng.
    // Ô chỉ-đọc: hiện giá trị nhưng không cho sửa.
    public class ReadOnlyAttribute : PropertyAttribute { }

    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect r, SerializedProperty p, GUIContent label)
        {
            using (new EditorGUI.DisabledScope(true)) EditorGUI.PropertyField(r, p, label, true);
        }
        public override float GetPropertyHeight(SerializedProperty p, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(p, label, true);
        }
    }

    // artKey là chuỗi thật sự được ghi xuống JSON. Nó bám theo Sprite (SyncArtKeys đồng bộ
    // mỗi lần vẽ), và chỉ đứng độc lập trong đúng một trường hợp: mở file mà chưa có ảnh —
    // lúc đó Sprite null nhưng key gốc vẫn được giữ, nên mở-rồi-lưu không xoá mất key.
    // Read-only vì nó là hệ quả của Art, không phải thứ để gõ tay.
    [Serializable] public class EGroup { public string id; public string text; [ReadOnly] public string artKey; public Sprite art; }
    [Serializable] public class ECard  { public string id; public string group; public string text; [ReadOnly] public string artKey; public Sprite art; }
    [Serializable] public class EBox   { public string[] slots = new string[Rules.BoxCapacity]; }
    [Serializable] public class EStack { public Vector2 pos; public List<EBox> boxes = new List<EBox>(); }

    public class LevelProxy : ScriptableObject
    {
        public string id = "lv-000";
        public string title = "Untitled";
        [TextArea(2, 4)] public string note;
        public List<EStack> stacks = new List<EStack>();
        public List<EGroup> groups = new List<EGroup>();
        public List<ECard> cards = new List<ECard>();
    }

    public class LevelEditorWindow : EditorWindow
    {
        const string ArtRoot = "Assets/Prototype/Resources/Art";
        const string LevelRoot = "Assets/Prototype/Resources/Levels";

        LevelProxy proxy;
        SerializedObject so;
        Vector2 scroll;
        string path;                 // đường dẫn file .json đang mở ("" = chưa lưu)
        string status = "";
        bool statusOk = true;

        [MenuItem("WordStack/Level Editor")]
        static void Open()
        {
            GetWindow<LevelEditorWindow>("Level Editor").minSize = new Vector2(420, 500);
        }

        void OnEnable()
        {
            if (proxy == null) NewLevel();
        }

        void OnDisable()
        {
            if (proxy != null) DestroyImmediate(proxy);
        }

        void Bind()
        {
            so = new SerializedObject(proxy);
        }

        // Gán Sprite thì key phải đổi theo ngay, nếu không ô read-only sẽ hiện giá trị cũ.
        // Gỡ Sprite ra thì giữ nguyên key — đó là ca "chưa có file ảnh" mà artKey tồn tại để phục vụ.
        void SyncArtKeys()
        {
            var ignore = new List<string>();
            foreach (var g in proxy.groups) { var k = KeyOf(g.art, "", ignore); if (k != null) g.artKey = k; }
            foreach (var c in proxy.cards) { var k = KeyOf(c.art, "", ignore); if (k != null) c.artKey = k; }
        }

        // ------------------------------------------------------------------ GUI

        void OnGUI()
        {
            DrawToolbar();

            if (proxy == null) return;
            SyncArtKeys();
            so.Update();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            var it = so.GetIterator();
            bool enter = true;
            while (it.NextVisible(enter))
            {
                enter = false;
                if (it.propertyPath == "m_Script") continue;
                EditorGUILayout.PropertyField(it, true);
            }
            EditorGUILayout.EndScrollView();

            so.ApplyModifiedProperties();

            DrawFooter();
        }

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(48))) NewLevel();
            if (GUILayout.Button("Open…", EditorStyles.toolbarButton, GUILayout.Width(58))) OpenFile();
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(path)))
                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(48))) Save(path);
            if (GUILayout.Button("Save As…", EditorStyles.toolbarButton, GUILayout.Width(70))) SaveAs();
            GUILayout.FlexibleSpace();
            GUILayout.Label(string.IsNullOrEmpty(path) ? "(chưa lưu)" : Path.GetFileName(path),
                            EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        void DrawFooter()
        {
            if (string.IsNullOrEmpty(status)) return;
            EditorGUILayout.HelpBox(status, statusOk ? MessageType.Info : MessageType.Error);
        }

        void Say(bool ok, string msg) { statusOk = ok; status = msg; Repaint(); }

        // ----------------------------------------------------------------- file

        void NewLevel()
        {
            if (proxy != null) DestroyImmediate(proxy);
            proxy = CreateInstance<LevelProxy>();
            proxy.hideFlags = HideFlags.DontSave;
            proxy.stacks.Add(new EStack { pos = Vector2.zero, boxes = { new EBox() } });
            path = "";
            Bind();
            Say(true, "Level mới. Thêm group + card, rồi điền id thẻ vào slots.");
        }

        void OpenFile()
        {
            string abs = EditorUtility.OpenFilePanel("Mở level JSON", LevelRoot, "json");
            if (string.IsNullOrEmpty(abs)) return;
            try
            {
                var lv = LevelData.Parse(File.ReadAllText(abs));
                FromLevelData(lv);
                path = ToProjectPath(abs);
                int missing = proxy.cards.Count(c => c.art == null && HasArtKey(lv, c.id));
                Say(true, "Đã mở " + Path.GetFileName(abs) + " — " + proxy.cards.Count + " thẻ, " +
                          proxy.groups.Count + " nhóm, " + proxy.stacks.Count + " stack" +
                          (missing > 0 ? "  ·  " + missing + " thẻ không tìm thấy ảnh trong " + ArtRoot : ""));
            }
            catch (Exception e)
            {
                Say(false, "Không đọc được file: " + e.Message);
            }
        }

        static bool HasArtKey(LevelData lv, string cardId)
        {
            var c = lv.Cards.FirstOrDefault(x => x.Id == cardId);
            return c != null && !string.IsNullOrEmpty(c.Art);
        }

        void SaveAs()
        {
            string abs = EditorUtility.SaveFilePanel("Lưu level JSON", LevelRoot,
                            string.IsNullOrEmpty(proxy.id) ? "lv-000" : proxy.id, "json");
            if (string.IsNullOrEmpty(abs)) return;
            Save(abs);
        }

        void Save(string target)
        {
            // 1. Sprite → key, chặn ngay nếu nằm ngoài ArtRoot (runtime sẽ không load được).
            var bad = new List<string>();
            foreach (var g in proxy.groups) KeyOf(g.art, "group " + g.id, bad);
            foreach (var c in proxy.cards) KeyOf(c.art, "card " + c.id, bad);
            if (bad.Count > 0)
            {
                Say(false, "Không lưu được — ảnh phải nằm trong " + ArtRoot + ":\n• " +
                           string.Join("\n• ", bad.ToArray()));
                return;
            }

            string json = ToJson(proxy);

            // 2. Chạy đúng bộ validate của game trước khi ghi đĩa. Level hỏng không ra khỏi tool.
            try
            {
                LevelData.Parse(json).Validate(k => ResolveArt(k) != null);
            }
            catch (Exception e)
            {
                Say(false, "Level chưa hợp lệ, chưa ghi file:\n" + e.Message);
                return;
            }

            try
            {
                File.WriteAllText(target, json);
                path = ToProjectPath(target);
                AssetDatabase.Refresh();
                Say(true, "Đã lưu " + Path.GetFileName(target) + " — validate sạch.");
            }
            catch (Exception e)
            {
                Say(false, "Ghi file lỗi: " + e.Message);
            }
        }

        static string ToProjectPath(string abs)
        {
            abs = abs.Replace('\\', '/');
            string root = Application.dataPath.Replace('\\', '/');
            return abs.StartsWith(root) ? "Assets" + abs.Substring(root.Length) : abs;
        }

        // ------------------------------------------------------------- art ↔ key

        // Sprite → tên file (không đuôi). Ngoài ArtRoot thì ghi vào `bad` để chặn lưu.
        static string KeyOf(Sprite s, string where, List<string> bad)
        {
            if (s == null) return null;
            string p = AssetDatabase.GetAssetPath(s).Replace('\\', '/');
            if (!p.StartsWith(ArtRoot + "/"))
            {
                bad.Add(where + " → " + p);
                return null;
            }
            return Path.GetFileNameWithoutExtension(p);
        }

        // key → Sprite, tìm trong ArtRoot theo đúng tên file. Dùng cho cả lúc mở file
        // lẫn lúc validate (thay cho Resources.Load, vì asset có thể chưa import xong).
        static Sprite ResolveArt(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (!AssetDatabase.IsValidFolder(ArtRoot)) return null;
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite " + key, new[] { ArtRoot }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(p) != key) continue;   // FindAssets khớp mờ
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (s != null) return s;
            }
            return null;
        }

        // ------------------------------------------------------- proxy ↔ LevelData

        void FromLevelData(LevelData lv)
        {
            if (proxy != null) DestroyImmediate(proxy);
            proxy = ToProxy(lv);
            Bind();
        }

        // Chuyển đổi thuần, tách khỏi window để test được bằng script.
        public static LevelProxy ToProxy(LevelData lv)
        {
            var proxy = CreateInstance<LevelProxy>();
            proxy.hideFlags = HideFlags.DontSave;
            proxy.id = lv.Id;
            proxy.title = lv.Title;
            proxy.note = lv.Note;

            foreach (var g in lv.Groups)
                proxy.groups.Add(new EGroup { id = g.Id, text = g.Text, artKey = g.Art, art = ResolveArt(g.Art) });
            foreach (var c in lv.Cards)
                proxy.cards.Add(new ECard { id = c.Id, group = c.Group, text = c.Text, artKey = c.Art, art = ResolveArt(c.Art) });
            foreach (var s in lv.Stacks)
            {
                var es = new EStack { pos = new Vector2((float)s.Pos[0], (float)s.Pos[1]) };
                foreach (var b in s.Boxes)
                {
                    var eb = new EBox();
                    for (int i = 0; i < eb.slots.Length && i < b.Slots.Length; i++) eb.slots[i] = b.Slots[i];
                    es.boxes.Add(eb);
                }
                proxy.stacks.Add(es);
            }
            return proxy;
        }

        public static string ToJson(LevelProxy proxy)
        {
            var bad = new List<string>();     // đã chặn ở Save, ở đây chỉ để gọi lại KeyOf
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append("  \"id\": ").Append(Str(proxy.id)).Append(",\n");
            sb.Append("  \"title\": ").Append(Str(proxy.title)).Append(",\n");
            if (!string.IsNullOrEmpty(proxy.note))
                sb.Append("  \"note\": ").Append(Str(proxy.note)).Append(",\n");

            sb.Append("\n  \"layout\": {\n    \"stacks\": [\n");
            for (int i = 0; i < proxy.stacks.Count; i++)
            {
                var st = proxy.stacks[i];
                sb.Append("      { \"pos\": [").Append(Num(st.pos.x)).Append(",").Append(Num(st.pos.y))
                  .Append("], \"boxes\": [\n");
                for (int b = 0; b < st.boxes.Count; b++)
                {
                    sb.Append("          { \"slots\": [");
                    var slots = st.boxes[b].slots;
                    for (int k = 0; k < slots.Length; k++)
                    {
                        if (k > 0) sb.Append(",");
                        sb.Append(string.IsNullOrEmpty(slots[k]) ? "null" : Str(slots[k]));
                    }
                    sb.Append("] }").Append(b < st.boxes.Count - 1 ? ",\n" : "\n");
                }
                sb.Append("      ]}").Append(i < proxy.stacks.Count - 1 ? ",\n" : "\n");
            }
            sb.Append("    ]\n  },\n");

            sb.Append("\n  \"meaning\": {\n    \"groups\": [\n");
            for (int i = 0; i < proxy.groups.Count; i++)
            {
                var g = proxy.groups[i];
                sb.Append("      { \"id\":").Append(Str(g.id));
                if (!string.IsNullOrEmpty(g.text)) sb.Append(", \"text\":").Append(Str(g.text));
                var gk = KeyOf(g.art, "", bad) ?? NullIfEmpty(g.artKey);
                if (gk != null) sb.Append(", \"art\":").Append(Str(gk));
                sb.Append(" }").Append(i < proxy.groups.Count - 1 ? ",\n" : "\n");
            }
            sb.Append("    ],\n    \"cards\": [\n");
            for (int i = 0; i < proxy.cards.Count; i++)
            {
                var c = proxy.cards[i];
                sb.Append("      { \"id\":").Append(Str(c.id)).Append(", \"group\":").Append(Str(c.group));
                if (!string.IsNullOrEmpty(c.text)) sb.Append(", \"text\":").Append(Str(c.text));
                var ck = KeyOf(c.art, "", bad) ?? NullIfEmpty(c.artKey);
                if (ck != null) sb.Append(", \"art\":").Append(Str(ck));
                sb.Append(" }").Append(i < proxy.cards.Count - 1 ? ",\n" : "\n");
            }
            sb.Append("    ]\n  }\n}\n");
            return sb.ToString();
        }

        static string NullIfEmpty(string s) { return string.IsNullOrEmpty(s) ? null : s; }

        static string Num(float v)
        {
            return Mathf.Approximately(v, Mathf.Round(v))
                ? ((int)Mathf.Round(v)).ToString(CultureInfo.InvariantCulture)
                : v.ToString("0.###", CultureInfo.InvariantCulture);
        }

        static string Str(string s)
        {
            if (s == null) return "null";
            var sb = new StringBuilder("\"");
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.Append('"').ToString();
        }
    }
}
