// Hộp trên cùng của một stack. Một instance sống suốt level: khi hộp bị xoá, instance này
// mờ đi rồi bind lại thành hộp vừa lộ (BoardController.RevealBox) — không tạo/huỷ.
//
// Kích thước hộp + vị trí 4 slot author trong prefab (Mục 2 của view-prefabs.md). Hit-test ô
// thẻ của BoardController lấy từ bounds sprite Shadow của slot (SlotRect bên dưới), không còn
// tính từ hằng layout nữa — chỉ zone Stack bên đó còn dùng BoxSize.
using System.Collections;
using System.Collections.Generic;
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
        // Núm két sắt: n eclipse chia đều quanh Handle (n = số nhóm cần gom); mỗi nhóm gom được Handle
        // xoay ngược chiều kim đồng hồ 360°/n tới eclipse kế tiếp; về 0 thì xoay nốt rồi nắp nhấc lên.
        [Tooltip("Núm xoay của nắp; kim của nó lúc author chỉ vào Eclipse Template")]
        [SerializeField] Transform handle;
        [Tooltip("Eclipse số 0, đặt đúng chỗ kim Handle đang chỉ. n−1 cái còn lại nhân bản quanh tâm Handle, ngược chiều kim đồng hồ")]
        [SerializeField] SpriteRenderer eclipseTemplate;
        [Tooltip("Thời gian xoay một nấc (giây)")]
        [SerializeField] float handleStepDur = 0.25f;
        [Tooltip("Nghỉ giữa hai nấc khi số giảm hơn 1 cùng lúc (giây)")]
        [SerializeField] float handleStepPause = 0.06f;
        [SerializeField] Ease handleEase = Ease.OutBack;
        [Tooltip("Số eclipse dựng thử bằng menu ⋮ ▸ Xem trước vòng eclipse (Edit mode)")]
        [SerializeField] int previewEclipses = 5;

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

        // Núm két sắt. eclipses[0] = template; bản nhân bản tạo lần đầu cần rồi tái dùng giữa các hộp.
        readonly List<SpriteRenderer> eclipses = new List<SpriteRenderer>();
        Vector3 eclipseCenter, eclipseOffset0;   // tâm Handle + vị trí template so với tâm, trong không gian cha của template
        Quaternion eclipseRot0;
        float handleZ0;                          // góc author của Handle
        int dialNeed, dialStep;                  // n đang dựng; nấc Handle đang đứng (0..n)
        MotionHandle handleMotion;

        void Awake()
        {
            // Bản xem trước còn sót (Reload Scene tắt): xoá TRƯỚC khi gom renderers, không để lẫn với eclipse thật.
            ClearPreview();
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
            if (handle != null) handleZ0 = handle.localEulerAngles.z;
            if (eclipseTemplate != null)
            {
                var et = eclipseTemplate.transform;
                eclipses.Add(eclipseTemplate);
                eclipseRot0 = et.localRotation;
                eclipseCenter = handle != null ? et.parent.InverseTransformPoint(handle.position) : Vector3.zero;
                eclipseOffset0 = et.localPosition - eclipseCenter;
            }
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
        // Animation suy từ chuyển trạng thái giữa hai lần gọi: số giảm = nảy + núm xoay một nấc,
        // đóng → mở = bung (hộp khoá theo số: núm xoay nốt nấc cuối rồi nắp nhấc lên).
        // Lần gọi đầu sau Awake/ResetVisual (hộp vừa dựng / vừa lộ / Undo) thì snap.
        // ResetVisual() cố ý KHÔNG đụng hai root: hộp vừa lộ ra có thể vẫn đang khoá,
        // RefreshBlockerVisuals mới là chỗ quyết định.
        bool lockKnown;
        GameObject shownRoot;   // root đang hiện, null = mở
        int lastLeft;

        public void SetOpen()
        {
            var was = lockKnown ? shownRoot : null;
            lockKnown = true; shownRoot = null;
            if (was == lockedRoot && dialStep < dialNeed)
            {
                // Hộp khoá theo số về 0: Handle xoay nốt về eclipse 0 (tròn một vòng) rồi nắp mới nhấc lên.
                if (lockedCountText != null) lockedCountText.text = "0";
                TurnHandle(dialNeed, () => Unlock(was));
            }
            else if (was != null) Unlock(was);
            else ShowRoots(null);
        }

        /// <param name="left">Số nhóm còn phải gom (hiện trên nắp)</param>
        /// <param name="need">Tổng số nhóm hộp cần — số eclipse trên núm</param>
        public void SetCountLock(int left, int need)
        {
            bool progressed = lockKnown && shownRoot == lockedRoot && need == dialNeed && left < lastLeft;
            lockKnown = true; shownRoot = lockedRoot; lastLeft = left;
            ShowRoots(lockedRoot);
            // Chỉ đổi chữ — font, size, outline giữ nguyên như author trong prefab.
            if (lockedCountText != null) lockedCountText.text = left.ToString();
            // Hộp vừa dựng / vừa lộ giữa màn / Undo: đặt thẳng Handle vào nấc đã gom, không diễn.
            if (progressed) TurnHandle(need - left);
            else { BuildDial(need); SnapHandle(need - left); }
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

        // Dựng n eclipse quanh tâm Handle: [0] là template, cái thứ k xoay 360°/n × k ngược chiều kim đồng
        // hồ quanh tâm (cả vị trí lẫn hướng). Bản nhân bản vào mảng renderers để SetAlpha kéo theo.
        void BuildDial(int need)
        {
            dialNeed = Mathf.Max(need, 1);
            if (eclipseTemplate == null) return;
            while (eclipses.Count < dialNeed)
            {
                var c = Instantiate(eclipseTemplate, eclipseTemplate.transform.parent);
                c.name = eclipseTemplate.name + " " + eclipses.Count;
                eclipses.Add(c);
                int t = System.Array.IndexOf(renderers, eclipseTemplate);
                System.Array.Resize(ref renderers, renderers.Length + 1);
                System.Array.Resize(ref baseAlpha, baseAlpha.Length + 1);
                renderers[renderers.Length - 1] = c;
                baseAlpha[baseAlpha.Length - 1] = t >= 0 ? baseAlpha[t] : 1f;
            }
            for (int k = 0; k < eclipses.Count; k++)
            {
                eclipses[k].gameObject.SetActive(k < dialNeed);
                PlaceEclipse(eclipses[k].transform, eclipseCenter, eclipseOffset0, eclipseRot0, k, dialNeed);
            }
        }

        // Eclipse thứ k trong n: xoay 360°/n × k ngược chiều kim đồng hồ quanh tâm Handle, cả vị trí lẫn hướng.
        static void PlaceEclipse(Transform e, Vector3 center, Vector3 offset0, Quaternion rot0, int k, int n)
        {
            var q = Quaternion.Euler(0f, 0f, 360f / n * k);
            e.localPosition = center + q * offset0;
            e.localRotation = q * rot0;
        }

        // Dựng thử vòng eclipse trong Edit mode để căn Eclipse Template. Bản thử mang HideFlags.DontSave:
        // không lưu vào prefab/scene; menu Xoá hoặc Play (Awake) dọn đi.
        [ContextMenu("Xem trước vòng eclipse")]
        void PreviewDial()
        {
            ClearPreview();
            if (eclipseTemplate == null || handle == null) { Debug.LogWarning("[BoxView] Cần nối Handle và Eclipse Template.", this); return; }
            var et = eclipseTemplate.transform;
            var center = et.parent.InverseTransformPoint(handle.position);
            int n = Mathf.Max(previewEclipses, 1);
            for (int k = 1; k < n; k++)
            {
                var c = Instantiate(eclipseTemplate, et.parent);
                c.name = eclipseTemplate.name + " (xem trước " + k + ")";
                c.gameObject.hideFlags = HideFlags.DontSave;
                PlaceEclipse(c.transform, center, et.localPosition - center, et.localRotation, k, n);
            }
        }

        [ContextMenu("Xoá xem trước vòng eclipse")]
        void ClearPreview()
        {
            if (eclipseTemplate == null) return;
            var old = new List<GameObject>();
            foreach (Transform c in eclipseTemplate.transform.parent)
                if ((c.gameObject.hideFlags & HideFlags.DontSave) != 0) old.Add(c.gameObject);
            // Xoá tức thì cả lúc Play: Awake gom renderers ngay sau đây, Destroy thì đợi hết frame.
            foreach (var o in old) DestroyImmediate(o);
        }

        float HandleZ(int step) { return handleZ0 + 360f / dialNeed * step; }

        void SnapHandle(int step)
        {
            handleMotion.TryCancel();
            dialStep = step;
            if (handle != null) handle.localRotation = Quaternion.Euler(0f, 0f, HandleZ(step));
        }

        // Xoay Handle từ nấc đang đứng tới nấc `to`, từng nấc một (ngược chiều kim đồng hồ), rồi gọi done.
        // ShowRoots TryComplete handle này: về đúng nấc cuối và vẫn gọi done.
        void TurnHandle(int to, System.Action done = null)
        {
            handleMotion.TryComplete();
            int from = dialStep;
            dialStep = to;
            if (handle == null || to <= from)
            {
                done?.Invoke();
                return;
            }
            var seq = LSequence.Create();
            for (int s = from; s < to; s++)
            {
                if (s > from && handleStepPause > 0f) seq.AppendInterval(handleStepPause);
                seq.Append(LMotion.Create(HandleZ(s), HandleZ(s + 1), handleStepDur).WithEase(handleEase)
                                  .Bind(handle, (z, h) => h.localRotation = Quaternion.Euler(0f, 0f, z)));
            }
            handleMotion = seq.Run(b =>
            {
                b.WithCancelOnError();
                if (done != null) b.WithOnComplete(done);
            }).AddTo(handle.gameObject);
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
            handleMotion.TryComplete();  // núm xoay dở về nấc cuối; nếu là nấc mở khoá thì Unlock bắt đầu — cắt ngay dòng dưới
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
