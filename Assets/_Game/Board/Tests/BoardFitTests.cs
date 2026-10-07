// BoardFit: khung bàn phải nằm trọn vùng trống giữa HUD, tâm trùng tâm vùng trống.
using NUnit.Framework;
using UnityEngine;

namespace WordStack.Board.Tests
{
    public class BoardFitTests
    {
        static readonly Rect Board = Rect.MinMaxRect(-1f, -8f, 5f, 1f);   // 6 × 9 world
        const float Eps = 0.5f;   // pixel

        // Khung world → pixel theo camera ortho vừa fit.
        static Rect ToPx(Rect world, Vector2 cam, float size, Vector2 screen)
        {
            float pxPerWorld = screen.y / (2f * size);
            Vector2 min = (world.min - cam) * pxPerWorld + screen / 2f;
            Vector2 max = (world.max - cam) * pxPerWorld + screen / 2f;
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        [TestCase(1080f, 1920f)]
        [TestCase(1080f, 2400f)]
        [TestCase(1536f, 2048f)]
        public void BoardFitsInsideFreeAreaAndIsCentred(float w, float h)
        {
            var screen = new Vector2(w, h);
            var free = Rect.MinMaxRect(0f, h * 0.12f, w, h * 0.85f);
            BoardFit.FitOrtho(Board, free, screen, out var cam, out var size);
            var px = ToPx(Board, cam, size, screen);

            Assert.GreaterOrEqual(px.xMin, free.xMin - Eps);
            Assert.LessOrEqual(px.xMax, free.xMax + Eps);
            Assert.GreaterOrEqual(px.yMin, free.yMin - Eps);
            Assert.LessOrEqual(px.yMax, free.yMax + Eps);
            Assert.AreEqual(free.center.x, px.center.x, Eps);
            Assert.AreEqual(free.center.y, px.center.y, Eps);
            Assert.IsTrue(Mathf.Abs(px.width - free.width) < Eps || Mathf.Abs(px.height - free.height) < Eps,
                "phải chạm một cặp cạnh — không thu nhỏ thừa");
        }

        [Test]
        public void TallScreenIsWidthBound()
        {
            var screen = new Vector2(1080f, 2400f);
            BoardFit.FitOrtho(Board, new Rect(Vector2.zero, screen), screen, out var cam, out var size);
            Assert.AreEqual(1080f, ToPx(Board, cam, size, screen).width, Eps);
        }

        [Test]
        public void WideScreenIsHeightBound()
        {
            var screen = new Vector2(1536f, 2048f);
            BoardFit.FitOrtho(Board, new Rect(Vector2.zero, screen), screen, out var cam, out var size);
            Assert.AreEqual(2048f, ToPx(Board, cam, size, screen).height, Eps);
        }

        [Test]
        public void EmptyFreeAreaFallsBackToWholeScreen()
        {
            var screen = new Vector2(1080f, 1920f);
            BoardFit.FitOrtho(Board, Rect.MinMaxRect(0f, 900f, 1080f, 800f), screen, out var camA, out var sizeA);
            BoardFit.FitOrtho(Board, new Rect(Vector2.zero, screen), screen, out var camB, out var sizeB);
            Assert.AreEqual(sizeB, sizeA, 1e-4f);
            Assert.AreEqual(camB.y, camA.y, 1e-4f);
        }

        [Test]
        public void FreeAreaStopsAtTheNearestBlockers()
        {
            var screen = new Vector2(1080f, 1920f);
            var free = BoardFit.FreeArea(new Rect(Vector2.zero, screen), screen, new[]
            {
                Rect.MinMaxRect(0f, 1700f, 1080f, 1920f),   // dải HUD trên
                Rect.MinMaxRect(400f, 1650f, 700f, 1760f),  // LevelBox thò xuống thấp hơn
                Rect.MinMaxRect(0f, 0f, 1080f, 250f),       // thanh booster
            });
            Assert.AreEqual(1650f, free.yMax);
            Assert.AreEqual(250f, free.yMin);
            Assert.AreEqual(0f, free.xMin);
            Assert.AreEqual(1080f, free.xMax);
        }
    }
}
