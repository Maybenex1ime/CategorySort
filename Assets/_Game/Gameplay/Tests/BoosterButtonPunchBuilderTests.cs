// Tool nảy nút booster HUD — chạy trên bản mở tạm của GamePlayUIRoot .prefab thật, KHÔNG lưu.
using LitMotion.Animation;
using LitMotion.Animation.Components;
using LogosGame.Features.Gameplay.Boosters.Views;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using WordStack.Meta.Editor;

namespace WordStack.Meta.Tests
{
    public class BoosterButtonPunchBuilderTests
    {
        [Test]
        public void Build_AddsScalePunchOnEveryBoosterButtonAndWires()
        {
            var root = PrefabUtility.LoadPrefabContents(BoosterButtonPunchBuilder.PrefabPath);
            try
            {
                var views = root.GetComponentsInChildren<BoosterButtonView>(true);
                Assert.AreEqual(3, views.Length, "Magnet / Shuffle / Undo");
                Assert.AreEqual(3, BoosterButtonPunchBuilder.Build(root));
                BoosterButtonPunchBuilder.Build(root);   // chạy lại không đẻ thêm component

                foreach (var view in views)
                {
                    var so = new SerializedObject(view);
                    var button = (Component)so.FindProperty("_button").objectReferenceValue;
                    var anim = (LitMotionAnimation)so.FindProperty("_clickPunch").objectReferenceValue;
                    Assert.IsNotNull(anim, view.name + ": _clickPunch chưa nối");
                    Assert.AreSame(button.gameObject, anim.gameObject, "anim nằm trên chính nút");
                    Assert.AreEqual(1, button.GetComponents<LitMotionAnimation>().Length);
                    Assert.AreEqual(1, anim.Components.Count);
                    Assert.IsTrue(anim.Components[0] is TransformScalePunchAnimation);

                    var c = new SerializedObject(anim).FindProperty("components").GetArrayElementAtIndex(0);
                    Assert.AreSame(button.transform, c.FindPropertyRelative("target").objectReferenceValue);
                    Assert.IsTrue(c.FindPropertyRelative("relative").boolValue);
                    var s = c.FindPropertyRelative("settings");
                    Assert.AreEqual(Vector3.one * BoosterButtonPunchBuilder.Strength, s.FindPropertyRelative("endValue").vector3Value);
                    Assert.AreEqual(BoosterButtonPunchBuilder.Duration, s.FindPropertyRelative("duration").floatValue, 1e-6f);
                    var o = s.FindPropertyRelative("options");
                    Assert.AreEqual(BoosterButtonPunchBuilder.Frequency, o.FindPropertyRelative("Frequency").intValue);
                    Assert.AreEqual(BoosterButtonPunchBuilder.DampingRatio, o.FindPropertyRelative("DampingRatio").floatValue, 1e-6f);
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
