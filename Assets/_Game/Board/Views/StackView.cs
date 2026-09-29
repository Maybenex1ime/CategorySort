// Một vị trí trên lưới: hộp trên cùng + tối đa 5 Tile Holder bên dưới. Mỗi holder đại diện một hộp chôn
// và mang thẻ mini của nó (spec docs/superpowers/specs/2026-09-29-stack-tile-holder-design.md).
// Holder bị tiêu thụ từ trên xuống: `consumed` holder đầu đã bay đi (BoardController giữ số này — nó phải
// sống qua RebuildBoardViews), hộp chôn thứ d nằm trên holder (consumed + d − 1). View không nhớ gì: mọi
// lần vẽ đều suy từ (consumed, danh sách hộp). Animation fill/lift do Tools ▸ WordStack ▸ Build Stack Holder
// Animations dựng; code chỉ gán đích bay, Stop/Play và đọc IsPlaying.
using System;
using System.Collections.Generic;
using LitMotion.Animation;
using UnityEngine;

namespace WordStack.Board
{
    public class StackView : MonoBehaviour
    {
        public const int HolderCount = 5;

        [Serializable]
        public class Holder
        {
            public GameObject root;                    // Peek k
            public HorizontalSpriteLayout layout;      // TileMarkerHolder — dồn thẻ mini đang bật vào giữa
            public GameObject[] minis = new GameObject[Rules.BoxCapacity];   // ứng với ô 0..3 của hộp
            public LitMotionAnimation fill;            // thẻ mini bay vào ô + to lên cỡ thẻ thật
            public LitMotionAnimation lift;            // holder nhấc lên + mờ
        }

        [SerializeField] Transform boxAnchor;
        [SerializeField] Holder[] holders = new Holder[HolderCount];

        public Transform BoxAnchor => boxAnchor;

        /// <summary>Hộp chôn thứ d (1 = ngay dưới hộp trên cùng) nằm ở holder nào; -1 = không có holder.</summary>
        public static int HolderIndexOf(int consumed, int d)
        {
            int k = consumed + d - 1;
            return d >= 1 && k >= 0 && k < HolderCount ? k : -1;
        }

        Holder At(int k) { return holders != null && k >= 0 && k < holders.Length ? holders[k] : null; }

        /// <summary>Vẽ lại mọi holder. boxes[0] là hộp trên cùng; holder không mang hộp nào thì tắt.</summary>
        public void ShowHolders(int consumed, IReadOnlyList<Box> boxes)
        {
            for (int k = 0; k < HolderCount; k++)
            {
                var h = At(k);
                if (h == null || h.root == null) continue;
                int d = k - consumed + 1;
                bool on = d >= 1 && d < boxes.Count;
                h.root.SetActive(on);
                for (int i = 0; i < h.minis.Length; i++)
                    if (h.minis[i] != null) h.minis[i].SetActive(on && boxes[d].Slots[i] != null);
            }
        }

        /// <summary>Thẻ mini ô `slot` của hộp chôn thứ d nếu đang hiện, null nếu không — điểm xuất phát thẻ Magnet.</summary>
        public Transform MiniOf(int consumed, int d, int slot)
        {
            var h = At(HolderIndexOf(consumed, d));
            if (h == null || h.root == null || !h.root.activeSelf || slot < 0 || slot >= h.minis.Length) return null;
            var m = h.minis[slot];
            return m != null && m.activeSelf ? m.transform : null;
        }

        /// <summary>
        /// Thẻ mini của holder k bay vào ô: slots[i] là ô i của hộp. Tắt layout — nó ghi đè vị trí mỗi
        /// LateUpdate. False = holder không hiện hoặc chưa dựng animation (bên gọi hiện thẻ ngay như cũ).
        /// </summary>
        public bool BeginFill(int k, IReadOnlyList<Transform> slots)
        {
            var h = At(k);
            if (h == null || h.root == null || !h.root.activeSelf || h.fill == null) return false;
            // Căn trước: StackView vừa dựng lại (sau Magnet) thì layout chưa chạy LateUpdate nào.
            if (h.layout != null) { h.layout.Apply(); h.layout.enabled = false; }
            foreach (var c in h.fill.Components)
                if (c is FlyToTargetAnimation fly)
                    fly.Destination = fly.Slot >= 0 && fly.Slot < slots.Count ? slots[fly.Slot] : null;
            h.fill.Stop();
            h.fill.Play();
            return true;
        }

        /// <summary>Thẻ thật thay chỗ thẻ mini.</summary>
        public void HideMinis(int k)
        {
            var h = At(k);
            if (h == null) return;
            foreach (var m in h.minis) if (m != null) m.SetActive(false);
        }

        public void BeginLift(int k)
        {
            var h = At(k);
            if (h == null || h.lift == null) return;
            h.lift.Stop();
            h.lift.Play();
        }

        public bool IsAnimating(int k)
        {
            var h = At(k);
            return h != null && ((h.fill != null && h.fill.IsPlaying) || (h.lift != null && h.lift.IsPlaying));
        }

        /// <summary>Stop trả holder + thẻ mini về vị trí/alpha gốc (OnStop từng component), bật lại layout, tắt holder.</summary>
        public void EndHolder(int k)
        {
            var h = At(k);
            if (h == null) return;
            if (h.lift != null) h.lift.Stop();
            if (h.fill != null) h.fill.Stop();
            if (h.layout != null) h.layout.enabled = true;
            if (h.root != null) h.root.SetActive(false);
        }

        /// <summary>Cảnh báo lúc nạp level: stack sâu hơn 6 lớp, hoặc hộp chôn có blocker (spec Mục 11).</summary>
        public static List<string> LimitWarnings(IReadOnlyList<Stack> stacks)
        {
            var w = new List<string>();
            for (int s = 0; s < stacks.Count; s++)
            {
                var boxes = stacks[s].Boxes;
                if (boxes.Count - 1 > HolderCount)
                    w.Add("stack " + s + " có " + boxes.Count + " lớp — tối đa " + (HolderCount + 1) +
                          " (1 hộp + " + HolderCount + " Tile Holder); hộp thứ " + (HolderCount + 2) + " trở đi không có holder.");
                for (int b = 1; b < boxes.Count; b++)
                    if (boxes[b].Lock.Kind != LockKind.None)
                        w.Add("stack " + s + " hộp chôn " + b + " có blocker " + boxes[b].Lock.Kind + " — blocker chỉ đặt ở hộp trên cùng.");
            }
            return w;
        }
    }
}
