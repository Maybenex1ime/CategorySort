// Màu chìa và ổ theo id. Một asset dùng chung cho Tile.prefab (chìa) và Box.prefab (ổ),
// nên chìa "k1" và ổ "k1" luôn cùng màu. Id chưa khai ở đây hiện màu fallback — mặc định
// hồng tím cho dễ thấy: gặp màu đó là asset thiếu một dòng.
using System;
using UnityEngine;

namespace WordStack.Board
{
    [CreateAssetMenu(menuName = "WordStack/Key Color Palette", fileName = "SO_KeyColors")]
    public class KeyColorPalette : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string id;
            public Color color;
        }

        [SerializeField] Entry[] entries = new Entry[0];
        [SerializeField] Color fallback = Color.magenta;

        public Color Get(string id)
        {
            foreach (var e in entries) if (e.id == id) return e.color;
            return fallback;
        }
    }
}
