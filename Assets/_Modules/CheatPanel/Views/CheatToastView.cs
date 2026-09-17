using System;
using LitMotion;
using LitMotion.Extensions;
using R3;
using Reflex.Attributes;
using TMPro;
using UnityEngine;

namespace LogosMeta.CheatPanel
{
    /// <summary>
    /// Fly-up notification banner. Subscribes to <see cref="ICheatNotificationSource.Notifications"/>
    /// and shows each message for ~2.5s before fading out. Stays out of the popup
    /// queue so it never blocks gameplay input.
    /// </summary>
    public sealed class CheatToastView : MonoBehaviour
    {
        [Inject] private readonly ICheatNotificationSource _notifications;

        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _root;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Color _successColor = new Color(0.2f, 0.8f, 0.3f);
        [SerializeField] private Color _failureColor = new Color(0.9f, 0.25f, 0.2f);
        [SerializeField] private float _floatDistance = 60f;
        [SerializeField] private float _visibleDuration = 2.5f;
        [SerializeField] private float _fadeDuration = 0.35f;

        private DisposableBag _disposables;
        private MotionHandle _activeSequence;
        private Vector2 _restAnchoredPosition;

        private void Awake()
        {
            if (_root != null) _restAnchoredPosition = _root.anchoredPosition;
            HideImmediate();
        }

        private void Start()
        {
            if (_notifications == null) return;
            _notifications.Notifications
                .Subscribe(OnNotification)
                .AddTo(ref _disposables);
        }

        private void OnDestroy()
        {
            _activeSequence.TryCancel();
            _disposables.Dispose();
        }

        private void OnNotification(CheatNotification notification)
        {
            ShowToast(notification);
        }

        private void ShowToast(CheatNotification notification)
        {
            _activeSequence.TryCancel();

            if (_label != null)
            {
                _label.text = notification.Message ?? string.Empty;
                _label.color = notification.Success ? _successColor : _failureColor;
            }

            if (_root != null) _root.anchoredPosition = _restAnchoredPosition;
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;

            // Mốc thời gian chép đúng bản DOTween: Append/AppendInterval đặt ở CUỐI toàn chuỗi,
            // tính cả cú trôi lên đã Join. Không SetEase ở bản cũ → OutQuad (mặc định DOTweenSettings).
            float fadeInEnd = _canvasGroup != null ? _fadeDuration : 0f;
            float floatEnd = _root != null ? _visibleDuration + _fadeDuration : 0f;
            float fadeOutAt = Math.Max(fadeInEnd, floatEnd) + Math.Max(0f, _visibleDuration - _fadeDuration);
            float end = fadeOutAt + (_canvasGroup != null ? _fadeDuration : 0f);

            var seq = LSequence.Create();
            if (_canvasGroup != null)
            {
                seq.Insert(0f, LMotion.Create(0f, 1f, _fadeDuration)
                                      .WithEase(Ease.OutQuad).WithCancelOnError().BindToAlpha(_canvasGroup));
                seq.Insert(fadeOutAt, LMotion.Create(1f, 0f, _fadeDuration)
                                             .WithEase(Ease.OutQuad).WithCancelOnError().BindToAlpha(_canvasGroup));
            }
            if (_root != null)
                seq.Insert(0f, LMotion.Create(_restAnchoredPosition.y, _restAnchoredPosition.y + _floatDistance, _visibleDuration + _fadeDuration)
                                      .WithEase(Ease.OutCubic).WithCancelOnError().BindToAnchoredPositionY(_root));
            // Mốc rỗng giữ đúng tổng thời lượng khi thiếu CanvasGroup (DOTween vẫn đếm AppendInterval).
            seq.Insert(end, LMotion.Create(0f, 0f, 0f).RunWithoutBinding());

            _activeSequence = seq.Run(b => b.WithCancelOnError().WithOnComplete(HideImmediate)).AddTo(gameObject);
        }

        private void HideImmediate()
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            if (_root != null) _root.anchoredPosition = _restAnchoredPosition;
        }
    }
}
