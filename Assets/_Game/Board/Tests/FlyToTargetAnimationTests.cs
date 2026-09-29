// FlyToTargetAnimation: bay từ vị trí lúc Play tới vị trí world của Destination gán lúc chạy; Stop trả chỗ cũ.
using System.Reflection;
using LitMotion;
using NUnit.Framework;
using UnityEngine;

namespace WordStack.Board.Tests
{
    public class FlyToTargetAnimationTests
    {
        static void Set(object o, string field, object value)
        {
            o.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(o, value);
        }

        [Test]
        public void Play_ReachesDestination_Stop_RestoresStart()
        {
            var mini = new GameObject("mini").transform;
            var dst = new GameObject("dst").transform;
            try
            {
                mini.position = new Vector3(1f, 2f, 0f);
                dst.position = new Vector3(-3f, 5f, 0f);
                var fly = new FlyToTargetAnimation();
                Set(fly, "target", mini);
                Set(fly, "slot", 2);
                Assert.AreEqual(2, fly.Slot);

                fly.Destination = dst;
                var h = fly.Play();
                Assert.IsTrue(h.IsActive(), "có đích thì phải có motion");
                h.Complete();
                Assert.That(Vector3.Distance(dst.position, mini.position), Is.LessThan(1e-4f), "tới đúng đích");

                fly.OnStop();
                Assert.That(Vector3.Distance(new Vector3(1f, 2f, 0f), mini.position), Is.LessThan(1e-4f), "Stop trả chỗ cũ");

                fly.Destination = null;
                Assert.IsFalse(fly.Play().IsActive(), "không đích thì không bay");
            }
            finally
            {
                Object.DestroyImmediate(mini.gameObject);
                Object.DestroyImmediate(dst.gameObject);
            }
        }
    }
}
