// Hộp trên cùng của một stack. Một instance sống suốt level: khi hộp bị xoá, instance này
// mờ đi rồi bind lại thành hộp vừa lộ (BoardController.RevealBox) — không tạo/huỷ.
//
// Kích thước hộp + vị trí 4 slot author trong prefab (Mục 2 của view-prefabs.md). Hit-test ô
// thẻ của BoardController lấy từ bounds sprite Shadow của slot (SlotRect bên dưới), không còn
// tính từ hằng layout nữa — chỉ zone Stack bên đó còn dùng BoxSize.
using System.Collections;
using LitMotion;
using LitMotion.Animation;
using LitMotion.Extensions;
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
        [Tooltip("Mở khoá (hộp khoá theo số): root trượt nhẹ lên + mờ dần — hiệu ứng cũ của Lock Box (giây)")]
        [SerializeField] float unlockDur = 0.35f;
        [Tooltip("Mở khoá: trượt lên bấy nhiêu unit (local của root) trong lúc mờ")]
        [SerializeField] float unlockLift = 0.25f;
        [SerializeField] Ease unlockEase = Ease.OutCubic;

        [Header("Mở group lock sau khi nhóm bay vào icon")]
        [Tooltip("Thẻ khoá (Key Tile, gồm icon nhóm) mờ dần về 0 trước khi cửa chớp chạy. Để trống = cha của Group Art")]
        [SerializeField] Transform groupTile;
        [Tooltip("Thời gian mờ Key Tile (giây), 0 = bỏ qua")]
        [FormerlySerializedAs("groupIconPopDur")] [SerializeField] float groupTileFadeDur = 0.2f;

        SpriteRenderer[] renderers;
        float[] baseAlpha;
        SpriteRenderer[] slotShadows;   // sprite Shadow của từng slot — lấy ở Awake, trước khi thẻ mount vào
        Vector3 lockedScale = Vector3.one, groupScale = Vector3.one;   // scale author trong prefab của hai root
        // OpenGroupLock đang chạy: motion hiện tại, hàm trả root về trạng thái author, và token để
        // coroutine cũ tự thoát khi bị cắt ngang (FinishOpen tăng token).
        MotionHandle openTween;
        MotionHandle lockPunchH;   // cú nảy của hộp khoá theo số — TryComplete trả scale author
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
            {
                // DOPunchScale(…, vibrato 8, elasticity 0.6) cũ. DampingRatio ≈ 18.85/frequency để biên
                // độ tắt còn ~5% lúc hết giờ — cùng cách quy đổi với các punch khác, chờ so bằng mắt.
                lockPunchH.TryComplete();
                lockPunchH = LMotion.Punch.Create(lockedScale, lockedScale * lockPunch, lockPunchDur)
                                          .WithFrequency(8).WithDampingRatio(2.4f).WithCancelOnError()
                                          .BindToLocalScale(lockedRoot.transform).AddTo(lockedRoot);
            }
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

            // Key Tile mờ hẳn trước, rồi timeline Figma mới chạy. Một motion hệ số 1→0 nhân lên alpha
            // của từng sprite: bản cũ mờ từng sprite từ alpha hiện tại về 0, cùng ease — y hệt nhau.
            var tile = groupTile != null ? groupTile : (groupArt != null ? groupArt.transform.parent : null);
            if (tile == tr && groupArt != null) tile = groupArt.transform;   // icon gắn thẳng lên root: chỉ mờ icon
            if (tile != null && groupTileFadeDur > 0f)
            {
                var tileSrs = tile.GetComponentsInChildren<SpriteRenderer>(true);
                var tileA = new float[tileSrs.Length];
                for (int i = 0; i < tileSrs.Length; i++) tileA[i] = tileSrs[i].color.a;
                openTween = LMotion.Create(1f, 0f, groupTileFadeDur).WithEase(Ease.OutQuad).WithCancelOnError()
                                   .Bind(k =>
                                   {
                                       for (int i = 0; i < tileSrs.Length; i++)
                                       {
                                           var c = tileSrs[i].color; c.a = tileA[i] * k; tileSrs[i].color = c;
                                       }
                                   })
                                   .AddTo(root);
                yield return openTween.ToYieldInstruction();
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
            openTween.TryCancel();
            var restore = openRestore; openRestore = null;
            restore?.Invoke();
        }

        // Bật đúng một root (hoặc không cái nào), cắt motion dở và trả scale về giá trị author.
        void ShowRoots(GameObject keep)
        {
            FinishOpen();                // root về trạng thái author trước khi bật lại
            unlockMotion.TryComplete();  // mở khoá dở: OnComplete trả vị trí/alpha trước khi bật lại
            lockPunchH.TryComplete();
            foreach (var r in new[] { lockedRoot, groupLockRoot })
            {
                if (r == null) continue;
                r.transform.localScale = BaseScale(r);
                r.SetActive(r == keep);
            }
        }

        // Mở khoá root đang hiện (hộp khoá theo số; group lock đi đường OpenGroupLock riêng): trượt
        // nhẹ lên + mờ dần cả sprite lẫn chữ, xong tắt root và trả vị trí/scale/alpha về author.
        // Giữ handle để ShowRoots cắt ngang được (TryComplete → OnComplete trả trạng thái).
        MotionHandle unlockMotion;

        void Unlock(GameObject root)
        {
            unlockMotion.TryComplete();
            lockPunchH.TryComplete();    // cú nảy dở về scale author trước khi trượt
            var tr = root.transform;
            var pos0 = tr.localPosition;
            var s0 = BaseScale(root);
            var srs = root.GetComponentsInChildren<SpriteRenderer>(true);
            var texts = root.GetComponentsInChildren<TMP_Text>(true);
            var sa = new float[srs.Length];
            var ta = new float[texts.Length];
            for (int i = 0; i < srs.Length; i++) sa[i] = srs[i].color.a;
            for (int i = 0; i < texts.Length; i++) ta[i] = texts[i].alpha;

            // Một motion 0→1 lái cả trượt lẫn mờ: bản cũ cho mọi track cùng thời lượng + cùng ease.
            unlockMotion = LMotion.Create(0f, 1f, unlockDur).WithEase(unlockEase).WithCancelOnError()
                                  .WithOnComplete(() =>
                                  {
                                      root.SetActive(false);
                                      tr.localPosition = pos0;
                                      tr.localScale = s0;
                                      for (int i = 0; i < srs.Length; i++) { var c = srs[i].color; c.a = sa[i]; srs[i].color = c; }
                                      for (int i = 0; i < texts.Length; i++) texts[i].alpha = ta[i];
                                  })
                                  .Bind(k =>
                                  {
                                      var p = pos0; p.y += unlockLift * k; tr.localPosition = p;
                                      for (int i = 0; i < srs.Length; i++) { var c = srs[i].color; c.a = sa[i] * (1f - k); srs[i].color = c; }
                                      for (int i = 0; i < texts.Length; i++) texts[i].alpha = ta[i] * (1f - k);
                                  })
                                  .AddTo(root);
        }

        /// <summary>
        /// Hộp rỗng bị xoá: nhấc lên + mờ hẳn, cùng unlockLift / unlockDur / unlockEase với mở khoá hộp
        /// khoá theo số (Unlock) để hai cú "lùi ra" trông như một. Xong thì trả vị trí về chỗ cũ nhưng
        /// giữ alpha 0 — RevealBox bind lại hộp vừa lộ và ResetVisual mới hiện nó lên.
        /// </summary>
        public MotionHandle LiftAway()
        {
            var tr = transform;
            var pos0 = tr.localPosition;
            return LMotion.Create(0f, 1f, unlockDur).WithEase(unlockEase).WithCancelOnError()
                          .WithOnComplete(() => tr.localPosition = pos0)
                          .Bind(k =>
                          {
                              var p = pos0; p.y += unlockLift * k; tr.localPosition = p;
                              SetAlpha(1f - k);
                          })
                          .AddTo(gameObject);
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
