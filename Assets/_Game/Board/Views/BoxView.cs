// Hộp trên cùng của một stack. Một instance sống suốt level: khi hộp bị xoá, instance này
// mờ đi rồi bind lại thành hộp vừa lộ (BoardController.RevealBox) — không tạo/huỷ.
//
// Kích thước hộp + vị trí 4 slot author trong prefab (Mục 2 của view-prefabs.md). Hit-test ô
// thẻ của BoardController lấy từ bounds sprite Shadow của slot (SlotRect bên dưới), không còn
// tính từ hằng layout nữa — chỉ zone Stack bên đó còn dùng BoxSize.
using System.Collections;
using DG.Tweening;
using LitMotion.Animation;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace WordStack.Board
{
    public class BoxView : MonoBehaviour
    {
        [SerializeField] Transform[] slotAnchors = new Transform[4];

        // Blocker của hộp — mỗi loại một root riêng trong Box.prefab, dựng art độc lập.
        // Hộp mở thì cả hai root tắt. Field nào chưa nối thì bỏ qua, bàn vẫn chạy như thường.
        [Header("Hộp khoá theo số nhóm (locked)")]
        [SerializeField] GameObject lockedRoot;
        [SerializeField] TextMeshPro lockedCountText;   // số nhóm CÒN phải gom (TMP 3D, world-space)

        [Header("Hộp khoá theo nhóm (grouplock)")]
        [FormerlySerializedAs("keyLockRoot")] [SerializeField] GameObject groupLockRoot;
        // Renderer hiện art của nhóm phải gom sạch để mở — sprite do bên gọi load (GroupDef.Art).
        // Xích, khiên là lớp riêng giữ màu gốc.
        [FormerlySerializedAs("keyLockTint")] [SerializeField] SpriteRenderer groupArt;

        [Header("Animation khoá")]
        [Tooltip("Hộp khoá theo số: root nảy bao nhiêu mỗi lần số còn cần giảm (0 = tắt)")]
        [SerializeField] float lockPunch = 0.12f;
        [SerializeField] float lockPunchDur = 0.25f;
        [Tooltip("Mở khoá: root bung lên rồi co về 0 (giây)")]
        [SerializeField] float unlockDur = 0.3f;

        [Header("Mở group lock sau khi nhóm bay vào icon")]
        [Tooltip("Thẻ khoá (Key Tile, gồm icon nhóm) mờ dần về 0 trước khi cửa chớp chạy. Để trống = cha của Group Art")]
        [SerializeField] Transform groupTile;
        [Tooltip("Thời gian mờ Key Tile (giây), 0 = bỏ qua")]
        [FormerlySerializedAs("groupIconPopDur")] [SerializeField] float groupTileFadeDur = 0.2f;

        SpriteRenderer[] renderers;
        float[] baseAlpha;
        SpriteRenderer[] slotShadows;   // sprite Shadow của từng slot — lấy ở Awake, trước khi thẻ mount vào
        Vector3 lockedScale = Vector3.one, groupScale = Vector3.one;   // scale author trong prefab của hai root
        // OpenGroupLock đang chạy: tween hiện tại, hàm trả root về trạng thái author, và token để
        // coroutine cũ tự thoát khi bị cắt ngang (FinishOpen tăng token).
        Tween openTween;
        System.Action openRestore;
        int openToken;
        LitMotionAnimation openAnim;   // timeline mở trên groupLockRoot (Tools ▸ WordStack ▸ Build Lock Open Animation), null = không có

        void Awake()
        {
            // Lấy lúc Awake, TRƯỚC khi thẻ được mount vào slot — nên mảng này chỉ gồm phần
            // thân hộp, fade hộp không kéo theo thẻ đang nằm trong nó.
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            baseAlpha = new float[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseAlpha[i] = renderers[i].color.a;
            slotShadows = new SpriteRenderer[slotAnchors.Length];
            for (int i = 0; i < slotAnchors.Length; i++)
                if (slotAnchors[i] != null) slotShadows[i] = slotAnchors[i].GetComponentInChildren<SpriteRenderer>(true);
            if (lockedRoot != null) lockedScale = lockedRoot.transform.localScale;
            if (groupLockRoot != null) groupScale = groupLockRoot.transform.localScale;
            if (groupLockRoot != null) openAnim = groupLockRoot.GetComponent<LitMotionAnimation>();
        }

        /// <summary>Tâm icon nhóm trên lồng — đích cho 4 thẻ bay vào. Chưa nối icon thì lấy root.</summary>
        public Vector3 GroupIconWorld
        {
            get
            {
                if (groupArt != null) return groupArt.transform.position;
                return (groupLockRoot != null ? groupLockRoot.transform : transform).position;
            }
        }

        Vector3 BaseScale(GameObject root) { return root == lockedRoot ? lockedScale : groupScale; }

        public Transform Slot(int i) { return slotAnchors[i]; }

        /// <summary>Slot i có sprite Shadow để suy vùng chạm không.</summary>
        public bool HasSlotRect(int i) { return slotShadows != null && i < slotShadows.Length && slotShadows[i] != null; }

        /// <summary>
        /// Vùng slot i theo world = AABB của sprite Shadow, nên đã gồm scale của Stack (1.15) lẫn
        /// Slot (1.1) và khớp đúng hình thẻ nằm trong đó. Hit-test bên BoardController lấy từ đây
        /// thay vì hằng layout. Gọi khi hộp đã đứng yên (RefreshZones chỉ chạy lúc đó).
        /// </summary>
        public Rect SlotRect(int i)
        {
            var b = slotShadows[i].bounds;
            return new Rect((Vector2)b.center - (Vector2)b.size / 2f, b.size);
        }

        // Hộp đóng: không nhặt ra, không thả vào, không tự nổ (luật ở Domain). Ba trạng thái
        // nhìn: mở, khoá theo số nhóm (label = số nhóm còn cần), khoá theo nhóm (sprite nhóm).
        // Animation suy từ chuyển trạng thái giữa hai lần gọi: số giảm = nảy, đóng → mở = bung.
        // Lần gọi đầu sau Awake/ResetVisual (hộp vừa dựng / vừa lộ / Undo) thì snap.
        // ResetVisual() cố ý KHÔNG đụng hai root: hộp vừa lộ ra có thể vẫn đang khoá,
        // RefreshBlockerVisuals mới là chỗ quyết định.
        bool lockKnown;
        GameObject shownRoot;   // root đang hiện, null = mở
        string lastLabel;

        public void SetOpen()
        {
            var was = lockKnown ? shownRoot : null;
            lockKnown = true; shownRoot = null;
            if (was != null) Unlock(was);
            else ShowRoots(null);
        }

        public void SetCountLock(string label)
        {
            bool progressed = lockKnown && shownRoot == lockedRoot && label != lastLabel;
            lockKnown = true; shownRoot = lockedRoot; lastLabel = label;
            ShowRoots(lockedRoot);
            // Chỉ đổi chữ — font, size, outline giữ nguyên như author trong prefab.
            if (lockedCountText != null) lockedCountText.text = label ?? "";
            if (progressed && lockedRoot != null && lockPunch > 0f)
                lockedRoot.transform.DOPunchScale(lockedScale * lockPunch, lockPunchDur, 8, 0.6f).SetLink(lockedRoot);
        }

        public void SetGroupLock(Sprite sprite)
        {
            lockKnown = true; shownRoot = groupLockRoot;
            ShowRoots(groupLockRoot);
            if (groupArt == null) return;
            groupArt.sprite = sprite;
            var c = groupArt.color;
            groupArt.color = new Color(1f, 1f, 1f, c.a);   // art nhóm tự mang màu; alpha thuộc SetAlpha
        }

        // Mở group lock: Key Tile mờ dần → LitMotionAnimation trên Lock Root chạy trọn timeline Figma
        // (cửa chớp gập, thanh Middle ép dẹt, Upper rơi, cả khối mờ + co 0.9) → tắt root, trả alpha/scale
        // về author để lần bind sau còn dùng. Đặt shownRoot = null ngay: SetOpen ở cuối Settle sẽ snap,
        // không Unlock lần hai. Coroutine (bên gọi yield return) vì LitMotionAnimation không cho biết
        // trước thời lượng — chờ IsPlaying.
        public IEnumerator OpenGroupLock()
        {
            var root = groupLockRoot;
            lockKnown = true; shownRoot = null;
            // Cắt coroutine dở TRƯỚC khi xét activeSelf: restore dở có thể SetActive(false) root
            // — để nó chạy sau check là root qua được check rồi bị tắt ngay dưới chân.
            FinishOpen();
            if (root == null || !root.activeSelf) yield break;

            int token = ++openToken;
            var tr = root.transform;
            var srs = root.GetComponentsInChildren<SpriteRenderer>(true);
            var a0 = new float[srs.Length];
            for (int i = 0; i < srs.Length; i++) a0[i] = srs[i].color.a;
            openRestore = () =>
            {
                if (openAnim != null) openAnim.Stop();   // OnStop từng component trả vị trí/scale/alpha lúc Play
                root.SetActive(false);
                tr.localScale = groupScale;
                for (int i = 0; i < srs.Length; i++) { var c = srs[i].color; c.a = a0[i]; srs[i].color = c; }
            };
            tr.DOKill(true);

            // Key Tile mờ hẳn trước, rồi timeline Figma mới chạy.
            var tile = groupTile != null ? groupTile : (groupArt != null ? groupArt.transform.parent : null);
            if (tile == tr && groupArt != null) tile = groupArt.transform;   // icon gắn thẳng lên root: chỉ mờ icon
            if (tile != null && groupTileFadeDur > 0f)
            {
                var fade = DOTween.Sequence().SetLink(root);
                foreach (var sr in tile.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    var r = sr;
                    fade.Join(DOTween.ToAlpha(() => r.color, c => r.color = c, 0f, groupTileFadeDur).SetEase(Ease.OutQuad));
                }
                openTween = fade;
                yield return fade.WaitForCompletion();
                if (token != openToken) yield break;
            }

            if (openAnim != null)
            {
                openAnim.Stop();
                openAnim.Play();
                while (openAnim.IsPlaying)
                {
                    yield return null;
                    if (token != openToken) yield break;
                }
            }
            FinishOpen();
        }

        // Cắt OpenGroupLock đang dở (nếu có) và trả root về trạng thái author. Coroutine cũ thấy
        // token đổi thì tự thoát ở lần kiểm kế tiếp.
        void FinishOpen()
        {
            openToken++;
            if (openTween != null) { openTween.Kill(); openTween = null; }
            var restore = openRestore; openRestore = null;
            restore?.Invoke();
        }

        // Bật đúng một root (hoặc không cái nào), giết tween dở và trả scale về giá trị author.
        void ShowRoots(GameObject keep)
        {
            FinishOpen();   // root về trạng thái author trước khi bật lại
            foreach (var r in new[] { lockedRoot, groupLockRoot })
            {
                if (r == null) continue;
                r.transform.DOKill(true);
                r.transform.localScale = BaseScale(r);
                r.SetActive(r == keep);
            }
        }

        void Unlock(GameObject root)
        {
            var tr = root.transform;
            var s0 = BaseScale(root);
            tr.DOKill(true);
            var seq = DOTween.Sequence().SetLink(root);
            seq.Append(tr.DOScale(s0 * 1.2f, unlockDur * 0.35f).SetEase(Ease.OutQuad));
            seq.Append(tr.DOScale(0f, unlockDur * 0.65f).SetEase(Ease.InBack));
            seq.OnComplete(() => { root.SetActive(false); tr.localScale = s0; });
        }

        public void SetAlpha(float a)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                var c = renderers[i].color;
                c.a = baseAlpha[i] * a;
                renderers[i].color = c;
            }
        }

        public void ResetVisual()
        {
            transform.localScale = Vector3.one;
            SetAlpha(1f);
            lockKnown = false;   // hộp vừa lộ / vừa khôi phục: lần Set* tiếp theo snap, không diễn
        }
    }
}
