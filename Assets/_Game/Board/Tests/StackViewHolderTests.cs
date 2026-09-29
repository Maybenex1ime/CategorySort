// StackView holder: vẽ hoàn toàn từ (consumed, danh sách hộp). Holder bị tiêu thụ từ trên xuống; Magnet xoá
// hộp chôn thì hộp sau dồn lên, holder cuối tắt; thẻ mini i bật ⇔ ô i có thẻ.
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace WordStack.Board.Tests
{
    public class StackViewHolderTests
    {
        GameObject go;
        StackView sv;
        StackView.Holder[] hs;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("Stack");
            sv = go.AddComponent<StackView>();
            hs = new StackView.Holder[StackView.HolderCount];
            for (int k = 0; k < hs.Length; k++)
            {
                var h = new StackView.Holder { root = new GameObject("Peek" + (k + 1)) };
                h.root.transform.SetParent(go.transform, false);
                for (int i = 0; i < h.minis.Length; i++)
                {
                    h.minis[i] = new GameObject("Mini " + i);
                    h.minis[i].transform.SetParent(h.root.transform, false);
                }
                hs[k] = h;
            }
            typeof(StackView).GetField("holders", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(sv, hs);
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(go); }

        static Box B(params bool[] filled)
        {
            var b = new Box();
            for (int i = 0; i < filled.Length; i++) if (filled[i]) b.Slots[i] = new Tile { Uid = "t" + i };
            return b;
        }

        bool[] On() { var r = new bool[hs.Length]; for (int k = 0; k < hs.Length; k++) r[k] = hs[k].root.activeSelf; return r; }

        [Test]
        public void HolderIndexOf_MapsBuriedBoxToHolder()
        {
            Assert.AreEqual(0, StackView.HolderIndexOf(0, 1));
            Assert.AreEqual(2, StackView.HolderIndexOf(1, 2));
            Assert.AreEqual(-1, StackView.HolderIndexOf(3, 3), "vượt 5 holder");
            Assert.AreEqual(-1, StackView.HolderIndexOf(0, 0), "d = 0 là hộp trên cùng, không có holder");
        }

        [Test]
        public void ShowHolders_ConsumedTopDown_MagnetDropsLast()
        {
            var top = B(true, true, false, false);
            var b1 = B(true, false, false, true);
            var b2 = B(false, true, true, false);
            var b3 = B(true, true, true, true);

            sv.ShowHolders(0, new List<Box> { top, b1, b2, b3 });   // dựng bàn: 3 hộp chôn
            CollectionAssert.AreEqual(new[] { true, true, true, false, false }, On());
            Assert.IsTrue(hs[0].minis[0].activeSelf && !hs[0].minis[1].activeSelf && hs[0].minis[3].activeSelf, "Peek1 = b1");

            sv.ShowHolders(1, new List<Box> { b1, b2, b3 });        // clear: b1 lên trên, Peek1 đi
            CollectionAssert.AreEqual(new[] { false, true, true, false, false }, On());
            Assert.IsTrue(!hs[1].minis[0].activeSelf && hs[1].minis[1].activeSelf && hs[1].minis[2].activeSelf, "Peek2 = b2");

            sv.ShowHolders(1, new List<Box> { b1, b3 });            // Magnet xoá b2: b3 dồn lên Peek2, Peek3 tắt
            CollectionAssert.AreEqual(new[] { false, true, false, false, false }, On());
            Assert.IsTrue(hs[1].minis[0].activeSelf && hs[1].minis[3].activeSelf, "Peek2 = b3");
        }

        [Test]
        public void MiniOf_ReturnsOnlyVisibleMini()
        {
            sv.ShowHolders(0, new List<Box> { B(), B(true, false, false, false) });
            Assert.AreSame(hs[0].minis[0].transform, sv.MiniOf(0, 1, 0));
            Assert.IsNull(sv.MiniOf(0, 1, 1), "ô trống → mini tắt");
            Assert.IsNull(sv.MiniOf(0, 2, 0), "không có hộp chôn thứ 2");
        }

        [Test]
        public void LimitWarnings_TooDeepOrBuriedBlocker()
        {
            var ok = new Stack(); for (int i = 0; i < 6; i++) ok.Boxes.Add(B());
            var deep = new Stack(); for (int i = 0; i < 7; i++) deep.Boxes.Add(B());
            var locked = new Stack(); locked.Boxes.Add(B()); locked.Boxes.Add(B());
            locked.Boxes[1].Lock = new Lock { Kind = LockKind.Clears, Need = 2 };

            Assert.AreEqual(0, StackView.LimitWarnings(new List<Stack> { ok }).Count);
            Assert.AreEqual(1, StackView.LimitWarnings(new List<Stack> { deep }).Count);
            Assert.AreEqual(1, StackView.LimitWarnings(new List<Stack> { locked }).Count);
        }
    }
}
