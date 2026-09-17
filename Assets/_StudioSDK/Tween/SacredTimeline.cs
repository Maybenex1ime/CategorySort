// Chuỗi animation dựng trong Inspector: mỗi bước nối tiếp (Append) hoặc chạy cùng bước trước
// (Join), loại bước là LitMotionAnimation / UnityEvent / âm thanh. Port từ Mukbang ASMR
// (Utils/SacredTimeline.cs @ b050e5bb). Khác bản gốc:
//   - bỏ bước Spine/SpineUI: dự án không có Spine;
//   - bỏ nút xem trước trong Editor: dự án không có EditorCoroutines;
//   - bước Audio phát qua IAudioService.PlaySFX(id) thay cho AudioClip;
//   - bước Animation thiếu animation không còn làm treo chuỗi (bản gốc yield break
//     trước activeCount--, đếm không bao giờ về 0).
using System;
using System.Collections;
using LitMotion.Animation;
using LogosSDK.Audio;
using Reflex.Attributes;
using UnityEngine;
using UnityEngine.Events;

namespace LogosSDK.Tween
{
    public enum AutoPlayMode
    {
        None,
        OnStart,
        OnEnable
    }

    public class SacredTimeline : MonoBehaviour
    {
        [SerializeField] AutoPlayMode autoPlayMode = AutoPlayMode.None;
        [SerializeField] AnimationSequenceItem[] items;

        [Inject] IAudioService _audio;

        public bool IsPlaying { get; private set; }
        int activeCount;

        void OnEnable()
        {
            if (autoPlayMode == AutoPlayMode.OnEnable) Play();
        }

        void Start()
        {
            if (autoPlayMode == AutoPlayMode.OnStart) Play();
        }

        public void Play()
        {
            StartCoroutine(RunSequenceInternal());
        }

        public void Stop()
        {
            for (int i = items.Length - 1; i >= 0; i--)
            {
                var item = items[i];
                if (item != null && item.animation != null) item.animation.Stop();
            }
        }

        public IEnumerator PlayCoroutine()
        {
            return RunSequenceInternal();
        }

        [ContextMenu("Play")]
        void PlayFromContextMenu()
        {
            if (Application.isPlaying) Play();
        }

        IEnumerator RunSequenceInternal()
        {
            IsPlaying = true;
            var waitForEmpty = new WaitUntil(() => activeCount == 0);

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].sequence == AnimationSequenceItem.SequenceType.Append)
                    yield return waitForEmpty;
                StartCoroutine(PlayItem(items[i]));
            }

            yield return waitForEmpty;
            IsPlaying = false;
        }

        IEnumerator PlayItem(AnimationSequenceItem item)
        {
            activeCount++;
            if (item.delay > 0) yield return new WaitForSeconds(item.delay);
            switch (item.type)
            {
                case AnimationSequenceItem.Type.Animation:
                    if (item.animation != null) yield return item.animation.RunCoroutine(!item.isReverse);
                    break;
                case AnimationSequenceItem.Type.Event:
                    item.unityEvent?.Invoke();
                    break;
                case AnimationSequenceItem.Type.Audio:
                    if (!string.IsNullOrEmpty(item.sfxId)) _audio?.PlaySFX(item.sfxId);
                    break;
            }
            activeCount--;
        }
    }

    [Serializable]
    public class AnimationSequenceItem
    {
        public enum SequenceType
        {
            Append,
            Join
        }

        public enum Type
        {
            Animation,
            Delay,
            Event,
            Audio
        }

        public SequenceType sequence = SequenceType.Append;
        public float delay;
        public Type type;

        public LitMotionAnimation animation;
        public bool isReverse;
        public UnityEvent unityEvent;
        [Tooltip("Id truyền cho IAudioService.PlaySFX")]
        public string sfxId;
    }
}
