// FigmaMotionImporter: JSON timeline Figma → track LitMotion. Mẫu thật (mở hộp khoá nhóm) phải ra đúng
// số đang nằm trong Box.prefab: bảng Figma của plan 2026-09-27-lock-box-open-animation, cộng phần chỉnh
// tay ở commit 86fac84 (thanh Middle/Upper, mờ + co dài 0.1 s; mờ/co bắt đầu 0.57 s).
using System.Linq;
using FigmaMotion;
using LitMotion.Animation;
using LitMotion.Animation.Components;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using FigmaMotion.Editor;

namespace WordStack.Board.Tests
{
    public class FigmaMotionImporterTests
    {
        const string LockOpenJson = "Assets/_Game/Art/Blocker/Blocker_Box/LockOpen.figma-motion.json";
        const float Eps = 1e-4f;
        GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        static Transform Child(Transform parent, string name, float x = 0f)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = new Vector3(x, 0f, 0f);
            return t;
        }

        // Giống Lock Root trong Box.prefab: scale 1.75, hai UpperLit trùng tên (trái trước, phải sau).
        Transform LockRoot()
        {
            root = new GameObject("Lock Root");
            var rt = root.transform;
            rt.localScale = Vector3.one * 1.75f;
            Child(rt, "Key Tile");
            Child(rt, "Lit", -0.183f);
            Child(rt, "Lit (1)", 0.174f);
            Child(rt, "UpperLit", -0.183f);
            Child(rt, "UpperLit", 0.174f);
            Child(rt, "Upper");
            var middle = Child(rt, "Middle");
            Child(middle, "Top");
            Child(middle, "Bottom");
            Child(rt, "Middle No Change");
            return rt;
        }

        Transform Plain(string child = "A")
        {
            root = new GameObject("Root");
            Child(root.transform, child);
            return root.transform;
        }

        static void AssertVec(Vector3 expected, Vector3 actual, string what)
        {
            Assert.That(Vector3.Distance(expected, actual), Is.LessThan(Eps), $"{what}: cần {expected}, ra {actual}");
        }

        static void AssertTrack(MotionTrack t, string target, MotionKind kind, Vector3 end, float delay, float duration)
        {
            Assert.AreEqual(target, t.target.name, t.name);
            Assert.AreEqual(kind, t.kind, t.name);
            AssertVec(end, t.end, t.name + " end");
            Assert.AreEqual(delay, t.delay, Eps, t.name + " delay");
            Assert.AreEqual(duration, t.duration, Eps, t.name + " duration");
        }

        [Test]
        public void LockOpenJson_ReproducesPlanTable()
        {
            var rt = LockRoot();
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(LockOpenJson);
            Assert.IsNotNull(json, LockOpenJson);

            var r = FigmaMotionImporter.Resolve(json.text, rt);
            Assert.IsTrue(r.Ok, string.Join("\n", r.errors));
            Assert.IsFalse(r.sequential);
            Assert.AreEqual(15, r.tracks.Count);
            var t = r.tracks;
            var upperLeft = rt.GetChild(3);
            var upperRight = rt.GetChild(4);

            // Cửa chớp: 1 px Figma = 0.005; tấm trái trượt −37/−39 px, tấm phải lật dấu.
            AssertTrack(t[0], "Lit", MotionKind.Position, new Vector3(-0.185f, 0f, 0f), 0f, 0.458f);
            AssertTrack(t[1], "Lit", MotionKind.Scale, new Vector3(-1f, 0f, 0f), 0.0577f, 0.4003f);
            AssertTrack(t[2], "UpperLit", MotionKind.Position, new Vector3(-0.195f, 0f, 0f), 0f, 0.458f);
            Assert.AreSame(upperLeft, t[2].target);
            AssertTrack(t[3], "UpperLit", MotionKind.Scale, new Vector3(-1f, 0f, 0f), 0.063f, 0.395f);
            AssertTrack(t[4], "Lit (1)", MotionKind.Position, new Vector3(0.185f, 0f, 0f), 0f, 0.458f);
            AssertTrack(t[6], "UpperLit", MotionKind.Position, new Vector3(0.195f, 0f, 0f), 0f, 0.458f);
            Assert.AreSame(upperRight, t[6].target);

            // Thanh Middle + Upper: +5 / +20 px Figma (xuống) → âm trên Unity. Scale Y về 0.
            AssertTrack(t[8], "Top", MotionKind.Position, new Vector3(0f, -0.025f, 0f), 0.459f, 0.1f);
            AssertTrack(t[9], "Top", MotionKind.Scale, new Vector3(0f, -1f, 0f), 0.459f, 0.1f);
            AssertTrack(t[10], "Bottom", MotionKind.Position, new Vector3(0f, -0.025f, 0f), 0.459f, 0.1f);
            AssertTrack(t[12], "Upper", MotionKind.Position, new Vector3(0f, -0.1f, 0f), 0.46f, 0.1f);

            // Cả khối mờ tuyệt đối 1 → 0, co còn 0.9 theo scale author 1.75.
            AssertTrack(t[13], "Lock Root", MotionKind.SpriteAlpha, Vector3.zero, 0.57f, 0.1f);
            Assert.IsFalse(t[13].relative);
            Assert.AreEqual(1f, t[13].start.x, Eps);
            AssertTrack(t[14], "Lock Root", MotionKind.Scale, new Vector3(-0.175f, -0.175f, 0f), 0.57f, 0.1f);
            Assert.IsTrue(t.Where(x => x.kind != MotionKind.SpriteAlpha).All(x => x.relative && x.start == Vector3.zero));

            // Curve: nền X là 2 đoạn ease-out qua mốc 30/37 giữa chừng; nan X nội suy thẳng 8 mẫu.
            Assert.AreEqual(30f / 37f, t[0].ease.Evaluate(0.5f), Eps);
            Assert.AreEqual(30f / 37f * 0.6846f, t[0].ease.Evaluate(0.25f), 0.005f);
            Assert.AreEqual(18.268f / 39f, t[2].ease.Evaluate(0.1f / 0.458f), Eps);
            Assert.AreEqual((18.268f + 30.007f) / 2f / 39f, t[2].ease.Evaluate(0.15f / 0.458f), Eps);
            Assert.AreEqual(0.6846f, t[12].ease.Evaluate(0.5f), 0.005f);
        }

        [Test]
        public void DuplicateNameWithoutIndex_IsErrorNamingTheIndexes()
        {
            var rt = LockRoot();
            var r = FigmaMotionImporter.Resolve(
                "{\"tracks\":[{\"target\":\"UpperLit\",\"property\":\"x\",\"keys\":[{\"time\":0,\"value\":0},{\"time\":1,\"value\":5}]}]}", rt);
            Assert.IsFalse(r.Ok);
            StringAssert.Contains("UpperLit[1]", r.errors[0]);
        }

        [Test]
        public void MissingTargetOrIndexOutOfRange_IsError()
        {
            var rt = Plain();
            Assert.IsFalse(FigmaMotionImporter.TryFind(rt, "B", out _, out var e1));
            StringAssert.Contains("\"B\"", e1);
            Assert.IsFalse(FigmaMotionImporter.TryFind(rt, "A[1]", out _, out _));
            Assert.IsTrue(FigmaMotionImporter.TryFind(rt, "A[0]", out var a, out _));
            Assert.AreEqual("A", a.name);
            Assert.IsTrue(FigmaMotionImporter.TryFind(rt, "", out var self, out _));
            Assert.AreSame(rt, self);
        }

        [Test]
        public void ConvertsUnits_FlipsY_AndScalesByAuthorScale()
        {
            var rt = Plain();
            rt.GetChild(0).localScale = new Vector3(2f, 2f, 1f);
            var r = FigmaMotionImporter.Resolve(@"{ ""pxToUnit"": 0.01, ""tracks"": [
                { ""target"": ""A"", ""property"": ""y"", ""keys"": [ { ""time"": 0, ""value"": 100 }, { ""time"": 1, ""value"": 110 } ] },
                { ""target"": ""A"", ""property"": ""x"", ""keys"": [ { ""time"": 0, ""value"": 0 }, { ""time"": 1, ""value"": -10 } ] },
                { ""target"": ""A"", ""property"": ""rotation"", ""keys"": [ { ""time"": 0, ""value"": 0 }, { ""time"": 1, ""value"": 90 } ] },
                { ""target"": ""A"", ""property"": ""scaleY"", ""keys"": [ { ""time"": 0, ""value"": 1 }, { ""time"": 1, ""value"": 0.5 } ] } ] }", rt);
            Assert.IsTrue(r.Ok, string.Join("\n", r.errors));
            AssertVec(new Vector3(0f, -0.1f, 0f), r.tracks[0].end, "y lật");
            AssertVec(new Vector3(-0.1f, 0f, 0f), r.tracks[1].end, "x giữ dấu");
            AssertVec(new Vector3(0f, 0f, 90f), r.tracks[2].end, "rotation");
            Assert.AreEqual(MotionKind.Rotation, r.tracks[2].kind);
            AssertVec(new Vector3(0f, -1f, 0f), r.tracks[3].end, "scaleY theo scale author 2");

            var noFlip = FigmaMotionImporter.Resolve(@"{ ""pxToUnit"": 0.01, ""flipY"": false, ""tracks"": [
                { ""target"": ""A"", ""property"": ""y"", ""keys"": [ { ""time"": 0, ""value"": 0 }, { ""time"": 1, ""value"": 10 } ] } ] }", rt);
            AssertVec(new Vector3(0f, 0.1f, 0f), noFlip.tracks[0].end, "y không lật");
        }

        [Test]
        public void TimeUnits_MillisecondsAndPercentOfCycle()
        {
            var rt = Plain();
            var ms = FigmaMotionImporter.Resolve(@"{ ""timeUnit"": ""ms"", ""tracks"": [
                { ""target"": ""A"", ""property"": ""x"", ""keys"": [ { ""time"": 100, ""value"": 0 }, { ""time"": 400, ""value"": 5 } ] } ] }", rt);
            Assert.AreEqual(0.1f, ms.tracks[0].delay, Eps);
            Assert.AreEqual(0.3f, ms.tracks[0].duration, Eps);

            var pct = FigmaMotionImporter.Resolve(@"{ ""timeUnit"": ""%"", ""cycle"": 2, ""tracks"": [
                { ""target"": ""A"", ""property"": ""x"", ""keys"": [ { ""time"": 10, ""value"": 0 }, { ""time"": 35, ""value"": 5 } ] } ] }", rt);
            Assert.AreEqual(0.2f, pct.tracks[0].delay, Eps);
            Assert.AreEqual(0.5f, pct.tracks[0].duration, Eps);

            var noCycle = FigmaMotionImporter.Resolve(@"{ ""timeUnit"": ""%"", ""tracks"": [
                { ""target"": ""A"", ""property"": ""x"", ""keys"": [ { ""time"": 0, ""value"": 0 }, { ""time"": 50, ""value"": 5 } ] } ] }", rt);
            Assert.IsFalse(noCycle.Ok);
        }

        [Test]
        public void PerKeyEaseOverridesTrackAndFileEase()
        {
            var rt = Plain();
            var r = FigmaMotionImporter.Resolve(@"{ ""ease"": ""ease-in"", ""tracks"": [
                { ""target"": ""A"", ""property"": ""x"", ""ease"": ""ease-out"", ""keys"": [
                    { ""time"": 0, ""value"": 0, ""ease"": ""linear"" }, { ""time"": 1, ""value"": 10 }, { ""time"": 2, ""value"": 20 } ] } ] }", rt);
            Assert.IsTrue(r.Ok, string.Join("\n", r.errors));
            var c = r.tracks[0].ease;
            Assert.AreEqual(0.25f, c.Evaluate(0.25f), 0.005f);                 // đoạn 1: linear (của key)
            Assert.AreEqual(0.5f + 0.5f * 0.6846f, c.Evaluate(0.75f), 0.005f);   // đoạn 2: ease-out (của track)
        }

        [Test]
        public void FlatTrackIsSkippedWithWarning_BadInputIsError()
        {
            var rt = Plain();
            var flat = FigmaMotionImporter.Resolve(@"{ ""tracks"": [
                { ""target"": ""A"", ""property"": ""x"", ""keys"": [ { ""time"": 0, ""value"": 3 }, { ""time"": 1, ""value"": 3 } ] } ] }", rt);
            Assert.IsTrue(flat.Ok);
            Assert.AreEqual(0, flat.tracks.Count);
            Assert.AreEqual(1, flat.warnings.Count);

            string Track(string body) => @"{ ""tracks"": [ { ""target"": ""A"", " + body + " } ] }";
            Assert.IsFalse(FigmaMotionImporter.Resolve(Track(@"""property"": ""x"", ""ease"": ""bounce"", ""keys"": [ { ""time"": 0, ""value"": 0 }, { ""time"": 1, ""value"": 1 } ]"), rt).Ok, "easing lạ");
            Assert.IsFalse(FigmaMotionImporter.Resolve(Track(@"""property"": ""skew"", ""keys"": [ { ""time"": 0, ""value"": 0 }, { ""time"": 1, ""value"": 1 } ]"), rt).Ok, "property lạ");
            Assert.IsFalse(FigmaMotionImporter.Resolve(Track(@"""property"": ""x"", ""keys"": [ { ""time"": 1, ""value"": 0 }, { ""time"": 1, ""value"": 1 } ]"), rt).Ok, "time không tăng");
            Assert.IsFalse(FigmaMotionImporter.Resolve(Track(@"""property"": ""x"", ""keys"": [ { ""time"": 0, ""value"": 0 } ]"), rt).Ok, "một key");
            Assert.IsFalse(FigmaMotionImporter.Resolve(Track(@"""property"": ""scale"", ""keys"": [ { ""time"": 0, ""value"": 0 }, { ""time"": 1, ""value"": 1 } ]"), rt).Ok, "scale từ 0");
            Assert.IsFalse(FigmaMotionImporter.Resolve("{ not json", rt).Ok, "JSON hỏng");
        }

        const string TreeJson = @"{ ""pxToUnit"": 0.01, ""nodes"": [
            { ""path"": ""Bg"", ""x"": 0, ""y"": 0, ""rotation"": 0, ""scaleX"": 1, ""scaleY"": 1, ""opacity"": 1, ""sprite"": true },
            { ""path"": ""Panel"", ""x"": 100, ""y"": 20, ""rotation"": 90, ""scaleX"": -1, ""scaleY"": 1, ""opacity"": 0.5, ""sprite"": false },
            { ""path"": ""Panel/Door"", ""x"": -10, ""y"": 0, ""rotation"": 0, ""scaleX"": 1, ""scaleY"": 1, ""opacity"": 0.5, ""sprite"": true },
            { ""path"": ""Star[0]"", ""x"": 0, ""y"": 0, ""rotation"": 0, ""scaleX"": 1, ""scaleY"": 1, ""opacity"": 1, ""sprite"": true },
            { ""path"": ""Star[1]"", ""x"": 0, ""y"": 0, ""rotation"": 0, ""scaleX"": 1, ""scaleY"": 1, ""opacity"": 1, ""sprite"": true } ] }";

        [Test]
        public void BuildHierarchy_CreatesMissingNodes_KeepsExisting()
        {
            root = new GameObject("Root");
            var bg = Child(root.transform, "Bg", 5f);   // đã có, chỉnh tay: phải giữ nguyên

            var r = FigmaMotionImporter.BuildHierarchy(TreeJson, root);
            Assert.IsTrue(r.Ok, string.Join("\n", r.errors));
            Assert.AreEqual(1, r.kept);
            CollectionAssert.AreEqual(new[] { "Panel", "Panel/Door", "Star[0]", "Star[1]" }, r.created);
            AssertVec(new Vector3(5f, 0f, 0f), bg.localPosition, "node đã có giữ nguyên");
            Assert.IsNull(bg.GetComponent<SpriteRenderer>());

            var panel = root.transform.Find("Panel");
            AssertVec(new Vector3(1f, -0.2f, 0f), panel.localPosition, "px → unit, y lật");
            AssertVec(new Vector3(-1f, 1f, 1f), panel.localScale, "lật");
            Assert.AreEqual(90f, panel.localEulerAngles.z, 1e-3f);
            Assert.IsNull(panel.GetComponent<SpriteRenderer>(), "group không có sprite");

            var door = panel.Find("Door").GetComponent<SpriteRenderer>();
            Assert.IsNull(door.sprite, "sprite gắn tay");
            Assert.AreEqual(0.25f, door.color.a, Eps, "alpha = 0.5 của Panel × 0.5 của Door");
            Assert.AreEqual(1, door.sortingOrder, "Bg (đã có) vẫn giữ chỗ 0");
            var stars = root.transform.Cast<Transform>().Where(t => t.name == "Star").ToArray();
            Assert.AreEqual(2, stars.Length);
            Assert.AreEqual(3, stars[1].GetComponent<SpriteRenderer>().sortingOrder);

            var again = FigmaMotionImporter.BuildHierarchy(TreeJson, root);
            Assert.AreEqual(0, again.created.Count, "chạy lại không tạo trùng");
            Assert.AreEqual(5, again.kept);

            Assert.IsTrue(FigmaMotionImporter.Resolve(@"{ ""tracks"": [ { ""target"": ""Panel/Door"", ""property"": ""x"",
                ""keys"": [ { ""time"": 0, ""value"": 0 }, { ""time"": 1, ""value"": 10 } ] } ] }", root.transform).Ok, "track trỏ vào node vừa dựng");
        }

        [Test]
        public void BuildHierarchy_NeedsNodesAndSpriteRoot()
        {
            root = new GameObject("Root");
            Assert.IsFalse(FigmaMotionImporter.BuildHierarchy(@"{ ""tracks"": [] }", root).Ok, "thiếu nodes");
            var ui = new GameObject("Ui", typeof(RectTransform));
            try { Assert.IsFalse(FigmaMotionImporter.BuildHierarchy(TreeJson, ui).Ok, "Root UI"); }
            finally { Object.DestroyImmediate(ui); }
        }

        [Test]
        public void Write_BuildsComponentsInOrder_AddsCanvasGroupForUiOpacity_NoAutoPlay()
        {
            root = new GameObject("Root");
            Child(root.transform, "Sprite");
            var ui = new GameObject("Ui", typeof(RectTransform));
            ui.transform.SetParent(root.transform, false);

            var r = FigmaMotionImporter.Resolve(@"{ ""mode"": ""sequential"", ""tracks"": [
                { ""target"": ""Sprite"", ""property"": ""x"", ""keys"": [ { ""time"": 0, ""value"": 0 }, { ""time"": 1, ""value"": 5 } ] },
                { ""target"": ""Sprite"", ""property"": ""opacity"", ""keys"": [ { ""time"": 0, ""value"": 1 }, { ""time"": 1, ""value"": 0 } ] },
                { ""target"": ""Ui"", ""property"": ""opacity"", ""keys"": [ { ""time"": 0, ""value"": 0 }, { ""time"": 1, ""value"": 1 } ] } ] }", root.transform);
            Assert.IsTrue(r.Ok, string.Join("\n", r.errors));
            Assert.AreEqual(MotionKind.CanvasAlpha, r.tracks[2].kind);

            var anim = FigmaMotionImporter.Write(root, r.tracks, r.sequential);
            Assert.IsInstanceOf<TransformPositionAnimation>(anim.Components[0]);
            Assert.IsInstanceOf<SpriteGroupAlphaAnimation>(anim.Components[1]);
            Assert.IsInstanceOf<CanvasGroupAlphaAnimation>(anim.Components[2]);
            Assert.IsNotNull(ui.GetComponent<CanvasGroup>());

            var so = new SerializedObject(anim);
            Assert.AreEqual(0, so.FindProperty("autoPlayMode").enumValueIndex, "None");
            Assert.AreEqual(1, so.FindProperty("animationMode").enumValueIndex, "Sequential");
            var ui3 = so.FindProperty("components").GetArrayElementAtIndex(2);
            Assert.AreSame(ui.GetComponent<CanvasGroup>(), ui3.FindPropertyRelative("target").objectReferenceValue);
            Assert.AreEqual(1f, ui3.FindPropertyRelative("settings").FindPropertyRelative("endValue").floatValue, Eps);
        }
    }
}
