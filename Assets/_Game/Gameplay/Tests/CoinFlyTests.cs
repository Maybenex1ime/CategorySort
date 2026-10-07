// CoinFly: coin bung ra rồi bay về ví (report UI animation #1).
using LitMotion;
using LogosGame.Features.UI.Common;
using NUnit.Framework;
using UnityEngine;

namespace WordStack.Meta.Tests
{
    public class CoinFlyTests
    {
        GameObject _go;
        CoinFly _fly;
        ManualMotionDispatcher _clock;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("fly", typeof(RectTransform));
            _fly = _go.AddComponent<CoinFly>();
            _clock = new ManualMotionDispatcher();
            _fly.Scheduler = _clock.Scheduler;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        // Bước nhỏ như frame thật: motion tạo trong OnComplete chỉ chạy từ lần Update sau.
        void Tick(float seconds)
        {
            for (float t = 0f; t < seconds; t += 0.02f) _clock.Update(0.02);
        }

        [Test]
        public void EachCallbackFiresTheRightNumberOfTimes()
        {
            int first = 0, done = 0, each = 0;
            _fly.Play(Vector3.zero, new Vector3(500f, 0f, 0f), () => first++, () => done++, () => each++);
            Tick(5f);

            Assert.AreEqual(1, first, "coin đầu tới nơi");
            Assert.AreEqual(1, done, "xong cả lượt");
            Assert.AreEqual(_fly.Count, each, "mỗi coin tới nơi một lần");
            foreach (Transform coin in _go.transform)
                Assert.IsFalse(coin.gameObject.activeSelf, "coin phải ẩn khi tới nơi");
        }

        [Test]
        public void ReplayDropsTheOldRoundCallbacks()
        {
            int oldDone = 0, newDone = 0;
            _fly.Play(Vector3.zero, Vector3.right * 500f, null, () => oldDone++);
            Tick(0.1f);
            _fly.Play(Vector3.zero, Vector3.right * 500f, null, () => newDone++);
            Tick(5f);

            Assert.AreEqual(0, oldDone);
            Assert.AreEqual(1, newDone);
            Assert.AreEqual(_fly.Count, _go.transform.childCount, "coin được dùng lại, không tạo thêm");
        }
    }
}
