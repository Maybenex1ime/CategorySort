// Importer chung: JSON timeline Figma → LitMotionAnimation (không tự chạy) trên một GameObject gốc.
// BuildHierarchy dựng trước các GameObject còn thiếu theo cây layer trong JSON ("nodes").
// Định dạng + cách lấy số từ Figma: README.md của module (Assets/_Modules/FigmaMotion).
//
// Một track = một thuộc tính của một node: các keyframe có thời điểm tuyệt đối, giá trị Figma và
// easing từng đoạn. Importer suy delay/duration từ key đầu/cuối, đổi px → unit, lật trục Y (Figma
// hướng xuống), lật gương khi cần, rồi dựng curve đúng cubic-bezier (FigmaEase). Import ghi ĐÈ toàn
// bộ component của LitMotionAnimation trên root — chỉnh tay trong Inspector trước đó mất.
using System;
using System.Collections.Generic;
using LitMotion;
using LitMotion.Animation;
using LitMotion.Animation.Components;
using UnityEditor;
using UnityEngine;

namespace FigmaMotion.Editor
{
    [Serializable]
    public class FigmaMotionSpec
    {
        public string schema;
        public string name;
        public string source;
        public float pxToUnit = 0.01f;    // 1 px Figma = bấy nhiêu unit trong không gian local của target
        public bool flipY = true;         // Figma y hướng xuống, Unity hướng lên
        public string timeUnit = "s";     // s | ms | % (phần trăm của cycle)
        public float cycle;               // giây của một vòng timeline Figma — chỉ dùng khi timeUnit = "%"
        public string mode = "parallel";  // parallel | sequential
        public string ease = "ease-out";  // easing mặc định cho đoạn nào không ghi riêng
        public FigmaMotionNode[] nodes;   // cây layer cho "Dựng hierarchy" (tuỳ chọn), cha đứng trước con
        public FigmaMotionTrack[] tracks;
    }

    /// <summary>Một layer Figma cho "Dựng hierarchy". Toạ độ = tâm layer so với tâm layer cha, trục của cha.</summary>
    [Serializable]
    public class FigmaMotionNode
    {
        public string path;                    // cùng cú pháp target: "Lit 3/Rectangle 309", "Star[1]"
        public float x, y;                     // px; y Figma hướng xuống
        public float rotation;                 // độ, dương = ngược chiều kim đồng hồ
        public float scaleX = 1f, scaleY = 1f; // âm = lật
        public float width, height;            // px — tham khảo khi gắn sprite
        public float opacity = 1f;             // của riêng layer
        public bool sprite;                    // lá: thêm SpriteRenderer trống để gắn sprite tay
    }

    [Serializable]
    public class FigmaMotionTrack
    {
        public string name;
        public string target;     // đường dẫn từ root: "" = root, "Middle/Top", "UpperLit[1]" = con thứ 2 tên UpperLit
        public string property;   // x | y | rotation | scale | scaleX | scaleY | opacity
        public string ease;       // easing mặc định của track, đè ease của file
        public FigmaMotionKey[] keys;
    }

    [Serializable]
    public class FigmaMotionKey
    {
        public float time;
        public float value;
        public string ease;       // easing của đoạn TỪ key này tới key sau, đè ease của track
    }

    public enum MotionKind { Position, Rotation, Scale, SpriteAlpha, CanvasAlpha }

    /// <summary>Một component sắp ghi vào LitMotionAnimation, đã quy đổi xong sang đơn vị Unity.</summary>
    public struct MotionTrack
    {
        public string name;
        public Transform target;
        public MotionKind kind;
        public bool relative;        // cộng lên giá trị author lúc Play (vị trí / xoay / scale)
        public Vector3 start, end;   // alpha chỉ dùng .x
        public float delay, duration;
        public AnimationCurve ease;  // chuẩn hoá: 0 = start, 1 = end
    }

    public sealed class FigmaMotionResult
    {
        public readonly List<MotionTrack> tracks = new List<MotionTrack>();
        public readonly List<string> errors = new List<string>();
        public readonly List<string> warnings = new List<string>();
        public readonly List<string> created = new List<string>();   // Dựng hierarchy: đường dẫn node vừa tạo
        public int kept;                                              // Dựng hierarchy: node đã có, giữ nguyên
        public bool sequential;
        public string name;
        public bool Ok => errors.Count == 0;
    }

    public static class FigmaMotionImporter
    {
        public const string Schema = "figma-motion/v1";

        /// <summary>Đọc JSON + quy đổi lên hierarchy dưới root. Không ghi gì — dùng để kiểm tra trước.</summary>
        public static FigmaMotionResult Resolve(string json, Transform root)
        {
            var r = new FigmaMotionResult();
            var spec = new FigmaMotionSpec();   // FromJsonOverwrite giữ mặc định cho field JSON không ghi
            try { JsonUtility.FromJsonOverwrite(json, spec); }
            catch (Exception e) { r.errors.Add("JSON hỏng: " + e.Message); return r; }
            Resolve(spec, root, r);
            return r;
        }

        public static void Resolve(FigmaMotionSpec spec, Transform root, FigmaMotionResult r)
        {
            r.name = spec.name;
            if (root == null) { r.errors.Add("Chưa chọn GameObject gốc."); return; }
            if (!string.IsNullOrEmpty(spec.schema) && spec.schema != Schema)
                r.warnings.Add($"schema \"{spec.schema}\" khác {Schema} — vẫn thử đọc.");
            if (spec.tracks == null || spec.tracks.Length == 0) { r.errors.Add("JSON không có track nào (\"tracks\": [...])."); return; }
            if (spec.pxToUnit <= 0f) { r.errors.Add("pxToUnit phải > 0."); return; }

            float timeScale;
            switch ((spec.timeUnit ?? "").Trim().ToLowerInvariant())
            {
                case "": case "s": timeScale = 1f; break;
                case "ms": timeScale = 0.001f; break;
                case "%":
                    if (spec.cycle <= 0f) { r.errors.Add("timeUnit \"%\" cần \"cycle\" (giây của một vòng Figma) > 0."); return; }
                    timeScale = spec.cycle / 100f;
                    break;
                default: r.errors.Add($"timeUnit \"{spec.timeUnit}\" không hiểu (s | ms | %)."); return;
            }

            switch ((spec.mode ?? "").Trim().ToLowerInvariant())
            {
                case "": case "parallel": r.sequential = false; break;
                case "sequential": r.sequential = true; break;
                default: r.errors.Add($"mode \"{spec.mode}\" không hiểu (parallel | sequential)."); return;
            }

            for (int i = 0; i < spec.tracks.Length; i++)
                ResolveTrack(spec, spec.tracks[i], i, root, timeScale, r);
        }

        static void ResolveTrack(FigmaMotionSpec spec, FigmaMotionTrack tr, int index, Transform root, float timeScale, FigmaMotionResult r)
        {
            string where = $"track {index}" + (string.IsNullOrEmpty(tr.name) ? "" : $" \"{tr.name}\"");
            if (!TryFind(root, tr.target, out var target, out var findError)) { r.errors.Add($"{where}: {findError}"); return; }

            var keys = tr.keys;
            if (keys == null || keys.Length < 2) { r.errors.Add($"{where}: cần ít nhất 2 key."); return; }
            for (int k = 1; k < keys.Length; k++)
                if (!(keys[k].time > keys[k - 1].time)) { r.errors.Add($"{where}: time của key phải tăng dần (key {k})."); return; }

            var segs = new Vector4[keys.Length - 1];
            for (int k = 0; k < segs.Length; k++)
            {
                string e = !string.IsNullOrEmpty(keys[k].ease) ? keys[k].ease : !string.IsNullOrEmpty(tr.ease) ? tr.ease : spec.ease;
                if (!FigmaEase.TryParse(e, out segs[k]))
                {
                    r.errors.Add($"{where}: easing \"{e}\" không hiểu (linear | ease | ease-in | ease-out | ease-in-out | cubic-bezier(x1, y1, x2, y2)).");
                    return;
                }
            }

            // Giá trị Figma → đại lượng vô hướng phía Unity (u), và trục mà u nhân vào. Số đã tính theo
            // group gốc, kể cả lật/xoay của layer (Exporter/export-figma-motion.js).
            float v0 = keys[0].value, my = spec.flipY ? -1f : 1f, px = spec.pxToUnit;
            var s = target.localScale;
            Func<float, float> map;
            Vector3 axis;
            MotionKind kind;
            bool relative = true;
            string prop = (tr.property ?? "").Trim().ToLowerInvariant();
            switch (prop)
            {
                case "x": kind = MotionKind.Position; axis = Vector3.right; map = v => (v - v0) * px; break;
                case "y": kind = MotionKind.Position; axis = Vector3.up; map = v => (v - v0) * px * my; break;
                // Figma: độ dương = ngược chiều kim đồng hồ trên màn — trùng chiều z+ của Unity.
                case "rotation": kind = MotionKind.Rotation; axis = Vector3.forward; map = v => v - v0; break;
                case "scale":
                case "scalex":
                case "scaley":
                    // Scale tính theo tỉ lệ so với key đầu, nhân lên scale author của target.
                    if (Mathf.Approximately(v0, 0f)) { r.errors.Add($"{where}: scale của key đầu không được = 0."); return; }
                    kind = MotionKind.Scale;
                    axis = prop == "scalex" ? new Vector3(s.x, 0f, 0f) : prop == "scaley" ? new Vector3(0f, s.y, 0f) : new Vector3(s.x, s.y, 0f);
                    map = v => v / v0 - 1f;
                    break;
                case "opacity":
                    // Tuyệt đối 0..1. UI mờ qua CanvasGroup; sprite nhân hệ số lên alpha author (SpriteGroupAlphaAnimation).
                    kind = target is RectTransform ? MotionKind.CanvasAlpha : MotionKind.SpriteAlpha;
                    axis = Vector3.right;
                    relative = false;
                    map = v => v;
                    break;
                default:
                    r.errors.Add($"{where}: property \"{tr.property}\" không hiểu (x | y | rotation | scale | scaleX | scaleY | opacity).");
                    return;
            }

            var u = new float[keys.Length];
            for (int k = 0; k < keys.Length; k++) u[k] = map(keys[k].value);

            // Curve chuẩn hoá theo key lệch xa nhất so với key đầu — đi được cả track "đi rồi về".
            int far = 0;
            for (int k = 1; k < u.Length; k++) if (Mathf.Abs(u[k] - u[0]) > Mathf.Abs(u[far] - u[0])) far = k;
            if (Mathf.Abs(u[far] - u[0]) < 1e-6f) { r.warnings.Add($"{where}: giá trị không đổi — bỏ qua track."); return; }

            float t0 = keys[0].time, span = keys[keys.Length - 1].time - t0;
            var norm = new (float t, float v)[keys.Length];
            for (int k = 0; k < keys.Length; k++) norm[k] = ((keys[k].time - t0) / span, (u[k] - u[0]) / (u[far] - u[0]));

            r.tracks.Add(new MotionTrack
            {
                name = string.IsNullOrEmpty(tr.name) ? $"{PathOf(root, target)} · {tr.property}" : tr.name,
                target = target,
                kind = kind,
                relative = relative,
                start = axis * u[0],   // quan hệ tương đối: u[0] = 0
                end = axis * u[far],
                delay = t0 * timeScale,
                duration = span * timeScale,
                ease = FigmaEase.Curve(norm, segs),
            });
            WarnPivot(where, target, kind, r);
        }

        // Figma scale/xoay quanh tâm node. Sprite hay RectTransform lệch pivot thì kết quả lệch theo.
        static void WarnPivot(string where, Transform t, MotionKind kind, FigmaMotionResult r)
        {
            if (kind != MotionKind.Scale && kind != MotionKind.Rotation) return;
            var center = new Vector2(0.5f, 0.5f);
            if (t is RectTransform rt)
            {
                if ((rt.pivot - center).sqrMagnitude > 1e-4f)
                    r.warnings.Add($"{where}: pivot RectTransform {rt.pivot} — Figma scale/xoay quanh tâm, nên để (0.5, 0.5).");
                return;
            }
            var sr = t.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return;
            var p = sr.sprite.pivot / sr.sprite.rect.size;
            if ((p - center).sqrMagnitude > 1e-4f)
                r.warnings.Add($"{where}: pivot sprite \"{sr.sprite.name}\" = {p} — Figma scale/xoay quanh tâm, nên để Center.");
        }

        /// <summary>
        /// "" hoặc "." = root. Đường dẫn tách bằng "/". Đoạn "Tên[i]" = con thứ i (tính từ 0, theo thứ tự
        /// anh em) tên đúng "Tên" — bắt buộc khi có nhiều con trùng tên, không tự đoán.
        /// </summary>
        public static bool TryFind(Transform root, string path, out Transform found, out string error)
        {
            found = root;
            error = null;
            if (string.IsNullOrEmpty(path) || path == ".") return true;
            foreach (var raw in path.Split('/'))
            {
                var (seg, want) = Segment(raw);
                Transform hit = null;
                int n = 0;
                foreach (Transform c in found)
                {
                    if (c.name != seg) continue;
                    if (n == (want < 0 ? 0 : want)) hit = c;
                    n++;
                }
                if (n == 0) { error = $"không thấy \"{seg}\" dưới \"{found.name}\"."; return false; }
                if (want < 0 && n > 1) { error = $"có {n} con tên \"{seg}\" dưới \"{found.name}\" — ghi rõ \"{seg}[0]\" … \"{seg}[{n - 1}]\"."; return false; }
                if (hit == null) { error = $"\"{seg}[{want}]\" vượt số con cùng tên ({n})."; return false; }
                found = hit;
            }
            return true;
        }

        /// <summary>"Tên[i]" → (Tên, i); không có chỉ số → (Tên, -1).</summary>
        static (string name, int index) Segment(string seg)
        {
            int lb = seg.LastIndexOf('[');
            if (lb > 0 && seg.EndsWith("]") && int.TryParse(seg.Substring(lb + 1, seg.Length - lb - 2), out int idx) && idx >= 0)
                return (seg.Substring(0, lb), idx);
            return (seg, -1);
        }

        /// <summary>
        /// Dựng các node còn thiếu dưới root theo "nodes" của JSON, trong một bước Undo. Node đã có giữ
        /// nguyên (kể cả vị trí chỉnh tay). Lá được SpriteRenderer trống — gắn sprite tay — với sortingOrder
        /// theo thứ tự layer Figma và alpha = tích opacity của nó với các group cha.
        /// </summary>
        public static FigmaMotionResult BuildHierarchy(string json, GameObject root)
        {
            var r = new FigmaMotionResult();
            var spec = new FigmaMotionSpec();
            try { JsonUtility.FromJsonOverwrite(json, spec); }
            catch (Exception e) { r.errors.Add("JSON hỏng: " + e.Message); return r; }
            r.name = spec.name;
            if (root == null) { r.errors.Add("Chưa chọn GameObject gốc."); return r; }
            if (root.transform is RectTransform) { r.errors.Add("Root là UI (RectTransform) — Dựng hierarchy mới hỗ trợ sprite."); return r; }
            if (spec.nodes == null || spec.nodes.Length == 0) { r.errors.Add("JSON không có \"nodes\" — xuất lại bằng export-figma-motion.js bản mới."); return r; }
            if (spec.pxToUnit <= 0f) { r.errors.Add("pxToUnit phải > 0."); return r; }

            Undo.SetCurrentGroupName("Dựng hierarchy Figma");
            int group = Undo.GetCurrentGroup();
            float px = spec.pxToUnit, my = spec.flipY ? -1f : 1f;
            var alpha = new Dictionary<string, float>();
            int order = 0;
            foreach (var n in spec.nodes)
            {
                if (string.IsNullOrEmpty(n.path)) { r.errors.Add("Có node thiếu \"path\"."); continue; }
                int slash = n.path.LastIndexOf('/');
                string parentPath = slash < 0 ? "" : n.path.Substring(0, slash);
                float a = (alpha.TryGetValue(parentPath, out var pa) ? pa : 1f) * n.opacity;
                alpha[n.path] = a;
                int sortingOrder = n.sprite ? order++ : 0;   // node đã có vẫn giữ chỗ trong thứ tự

                if (!TryFind(root.transform, parentPath, out var parent, out var err)) { r.errors.Add($"{n.path}: {err}"); continue; }
                var (name, index) = Segment(slash < 0 ? n.path : n.path.Substring(slash + 1));
                int same = 0;
                foreach (Transform c in parent) if (c.name == name) same++;
                if (same > Math.Max(index, 0))
                {
                    if (index < 0 && same > 1) r.warnings.Add($"{n.path}: đã có {same} con tên \"{name}\" — không tạo thêm.");
                    r.kept++;
                    continue;
                }

                var go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, "Dựng hierarchy Figma");
                var t = go.transform;
                t.SetParent(parent, false);
                t.localPosition = new Vector3(n.x * px, n.y * px * my, 0f);
                t.localRotation = Quaternion.Euler(0f, 0f, n.rotation);
                t.localScale = new Vector3(n.scaleX, n.scaleY, 1f);
                if (n.sprite)
                {
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = sortingOrder;
                    sr.color = new Color(1f, 1f, 1f, a);
                }
                r.created.Add(n.path);
            }
            Undo.CollapseUndoOperations(group);
            if (r.created.Count > 0) EditorUtility.SetDirty(root);
            return r;
        }

        static string PathOf(Transform root, Transform t)
        {
            if (t == root) return root.name;
            var parts = new List<string>();
            for (var x = t; x != null && x != root; x = x.parent) parts.Insert(0, x.name);
            return string.Join("/", parts);
        }

        /// <summary>Resolve + ghi trong một nhóm Undo. Lỗi thì không ghi gì.</summary>
        public static FigmaMotionResult Import(string json, GameObject root)
        {
            var r = Resolve(json, root != null ? root.transform : null);
            if (!r.Ok) return r;
            Undo.SetCurrentGroupName("Import Figma Motion");
            int group = Undo.GetCurrentGroup();
            Write(root, r.tracks, r.sequential);
            Undo.CollapseUndoOperations(group);
            return r;
        }

        /// <summary>Ghi đè toàn bộ component của LitMotionAnimation trên root (thêm nếu chưa có).</summary>
        public static LitMotionAnimation Write(GameObject root, IReadOnlyList<MotionTrack> tracks, bool sequential)
        {
            var anim = root.GetComponent<LitMotionAnimation>();
            if (anim == null) anim = Undo.AddComponent<LitMotionAnimation>(root);
            // CanvasGroupAlphaAnimation cần CanvasGroup trên target — thêm trước khi ghi tham chiếu.
            foreach (var t in tracks)
                if (t.kind == MotionKind.CanvasAlpha && t.target.GetComponent<CanvasGroup>() == null)
                    Undo.AddComponent<CanvasGroup>(t.target.gameObject);
            Undo.RecordObject(anim, "Import Figma Motion");

            var so = new SerializedObject(anim);
            // version = 1 + playOnAwake = false: né migration cũ trong LitMotionAnimation.OnAfterDeserialize
            // (nó suy autoPlayMode TỪ playOnAwake khi version < 1, ghi đè đúng giá trị None vừa set dưới đây).
            so.FindProperty("version").intValue = 1;
            so.FindProperty("playOnAwake").boolValue = false;
            so.FindProperty("autoPlayMode").enumValueIndex = 0;                  // None — code tự gọi Play
            so.FindProperty("animationMode").enumValueIndex = sequential ? 1 : 0;   // Parallel | Sequential
            var arr = so.FindProperty("components");
            arr.arraySize = tracks.Count;
            for (int i = 0; i < tracks.Count; i++) arr.GetArrayElementAtIndex(i).managedReferenceValue = Create(tracks[i].kind);
            so.ApplyModifiedProperties();   // phải có instance rồi mới có property con để ghi
            so.Update();
            for (int i = 0; i < tracks.Count; i++) Fill(arr.GetArrayElementAtIndex(i), tracks[i]);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(root);
            return anim;
        }

        static LitMotionAnimationComponent Create(MotionKind kind)
        {
            switch (kind)
            {
                case MotionKind.Position: return new TransformPositionAnimation();
                case MotionKind.Rotation: return new TransformRotationAnimation();
                case MotionKind.Scale: return new TransformScaleAnimation();
                case MotionKind.CanvasAlpha: return new CanvasGroupAlphaAnimation();
                default: return new SpriteGroupAlphaAnimation();
            }
        }

        static void Fill(SerializedProperty el, MotionTrack t)
        {
            bool alpha = t.kind == MotionKind.SpriteAlpha || t.kind == MotionKind.CanvasAlpha;
            el.FindPropertyRelative("displayName").stringValue = t.name;
            el.FindPropertyRelative("target").objectReferenceValue =
                t.kind == MotionKind.CanvasAlpha ? (UnityEngine.Object)t.target.GetComponent<CanvasGroup>() : t.target;
            el.FindPropertyRelative("relative").boolValue = t.relative;
            var ws = el.FindPropertyRelative("useWorldSpace");   // Position/Rotation có, Scale không
            if (ws != null) ws.boolValue = false;
            var s = el.FindPropertyRelative("settings");
            if (alpha)
            {
                s.FindPropertyRelative("startValue").floatValue = t.start.x;
                s.FindPropertyRelative("endValue").floatValue = t.end.x;
            }
            else
            {
                s.FindPropertyRelative("startValue").vector3Value = t.start;
                s.FindPropertyRelative("endValue").vector3Value = t.end;
            }
            s.FindPropertyRelative("duration").floatValue = t.duration;
            s.FindPropertyRelative("delay").floatValue = t.delay;
            s.FindPropertyRelative("ease").intValue = (int)Ease.CustomAnimationCurve;
            s.FindPropertyRelative("customEaseCurve").animationCurveValue = t.ease;
        }
    }
}
