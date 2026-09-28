// Cửa sổ + thanh tool của Figma Motion Importer.
//   Tools ▸ Figma Motion ▸ Importer          — cửa sổ đầy đủ: chọn root + file JSON (hoặc dán), Kiểm tra,
//                                              Dựng hierarchy (tạo GameObject còn thiếu), Import.
//   Scene view ▸ overlay "Figma Motion"     — nút mở cửa sổ. Cửa sổ nhớ file JSON lần trước.
// Import ghi đè LitMotionAnimation trên root; xem trước bằng nút Play trong Inspector của nó.
using System.Text;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;

namespace FigmaMotion.Editor
{
    public sealed class FigmaMotionWindow : EditorWindow
    {
        static string LastJsonKey => "FigmaMotion.LastJson:" + Application.dataPath;   // EditorPrefs dùng chung mọi project

        [SerializeField] GameObject root;
        [SerializeField] TextAsset json;
        [SerializeField] bool usePasted;
        [SerializeField] string pasted = "";
        enum ReportKind { Check, Import, Build }

        FigmaMotionResult report;
        ReportKind reportKind;
        Vector2 scroll;

        [MenuItem("Tools/Figma Motion/Importer")]
        public static void Open()
        {
            var w = GetWindow<FigmaMotionWindow>("Figma Motion");
            w.minSize = new Vector2(380f, 280f);
            if (Selection.activeGameObject != null) w.root = Selection.activeGameObject;
        }

        void OnEnable()
        {
            if (json == null) json = LoadLastJson();
            if (root == null) root = Selection.activeGameObject;
        }

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button(new GUIContent("Kiểm tra", "Đọc JSON + tra node, không ghi gì"), EditorStyles.toolbarButton))
                {
                    report = FigmaMotionImporter.Resolve(Source(), root != null ? root.transform : null);
                    reportKind = ReportKind.Check;
                }
                using (new EditorGUI.DisabledScope(root == null))
                {
                    if (GUILayout.Button(new GUIContent("Dựng hierarchy", "Tạo GameObject còn thiếu dưới Root theo cây layer Figma; lá có SpriteRenderer trống (Undo được)"), EditorStyles.toolbarButton))
                    {
                        report = FigmaMotionImporter.BuildHierarchy(Source(), root);
                        reportKind = ReportKind.Build;
                        RememberJson();
                    }
                    if (GUILayout.Button(new GUIContent("Import", "Ghi đè LitMotionAnimation trên Root (Undo được)"), EditorStyles.toolbarButton))
                        DoImport();
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("← Selection", "Lấy GameObject đang chọn làm Root"), EditorStyles.toolbarButton))
                    root = Selection.activeGameObject;
            }

            EditorGUILayout.Space(4f);
            root = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Root", "GameObject nhận LitMotionAnimation; target trong JSON tính từ đây"),
                                                           root, typeof(GameObject), true);
            usePasted = EditorGUILayout.Toggle(new GUIContent("Dán JSON", "Dán thẳng thay vì chọn file"), usePasted);
            if (usePasted)
                pasted = EditorGUILayout.TextArea(pasted, GUILayout.MinHeight(90f));
            else
                json = (TextAsset)EditorGUILayout.ObjectField(new GUIContent("File JSON", ".json trong Assets"), json, typeof(TextAsset), false);

            if (root != null && root.GetComponent<LitMotion.Animation.LitMotionAnimation>() != null)
                EditorGUILayout.HelpBox("Root đã có LitMotionAnimation — Import sẽ ghi đè mọi component của nó.", MessageType.Info);

            DrawReport();
        }

        string Source() { return usePasted ? pasted : (json != null ? json.text : ""); }

        void DoImport()
        {
            report = FigmaMotionImporter.Import(Source(), root);
            reportKind = ReportKind.Import;
            RememberJson();
            Log(report, root, usePasted ? "JSON dán" : json != null ? json.name : "?");
        }

        void RememberJson()
        {
            if (report.Ok && !usePasted && json != null)
                EditorPrefs.SetString(LastJsonKey, AssetDatabase.GetAssetPath(json));
        }

        void DrawReport()
        {
            if (report == null) return;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var e in report.errors) EditorGUILayout.HelpBox(e, MessageType.Error);
            foreach (var w in report.warnings) EditorGUILayout.HelpBox(w, MessageType.Warning);
            if (report.Ok && reportKind == ReportKind.Build)
            {
                EditorGUILayout.LabelField($"Đã tạo {report.created.Count} node, giữ nguyên {report.kept} node đã có. Gắn sprite rồi Import.",
                                           EditorStyles.boldLabel);
                foreach (var p in report.created) EditorGUILayout.LabelField(p);
            }
            else if (report.Ok)
            {
                EditorGUILayout.LabelField(reportKind == ReportKind.Import ? $"Đã ghi {report.tracks.Count} component. Nhớ Save prefab/scene."
                                                                           : $"Hợp lệ: {report.tracks.Count} track ({(report.sequential ? "Sequential" : "Parallel")}).",
                                           EditorStyles.boldLabel);
                foreach (var t in report.tracks)
                    EditorGUILayout.LabelField(t.name, $"{t.kind}  {t.delay:0.###}s → {t.delay + t.duration:0.###}s  {Fmt(t)}");
            }
            EditorGUILayout.EndScrollView();
        }

        static string Fmt(MotionTrack t)
        {
            bool alpha = t.kind == MotionKind.SpriteAlpha || t.kind == MotionKind.CanvasAlpha;
            return alpha ? $"{t.start.x:0.##} → {t.end.x:0.##}" : $"Δ{t.end}";
        }

        static void Log(FigmaMotionResult r, GameObject go, string source)
        {
            var sb = new StringBuilder();
            foreach (var w in r.warnings) sb.Append("\n  ⚠ ").Append(w);
            if (r.Ok)
            {
                Debug.Log($"[Figma Motion] {source} → {r.tracks.Count} component trên {go.name}. Nhớ Save prefab/scene.{sb}", go);
                return;
            }
            foreach (var e in r.errors) sb.Append("\n  ✖ ").Append(e);
            Debug.LogWarning($"[Figma Motion] {source}: không import — {r.errors.Count} lỗi.{sb}", go);
        }

        static TextAsset LoadLastJson()
        {
            var path = EditorPrefs.GetString(LastJsonKey, "");
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        }
    }

    // Thanh tool trong Scene view (bật/tắt ở menu ⋮ Overlays của Scene view).
    [Overlay(typeof(SceneView), "figma-motion", "Figma Motion", true)]
    sealed class FigmaMotionToolbar : ToolbarOverlay
    {
        FigmaMotionToolbar() : base(FigmaMotionOpenButton.Id) { }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    sealed class FigmaMotionOpenButton : EditorToolbarButton
    {
        public const string Id = "FigmaMotion/Open";

        public FigmaMotionOpenButton()
        {
            text = "Figma Motion";
            tooltip = "Mở Figma Motion Importer: JSON timeline Figma → LitMotionAnimation";
            clicked += FigmaMotionWindow.Open;
        }
    }
}
