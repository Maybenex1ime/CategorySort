using System;
using DG.Tweening;
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
        private Sequence _activeSequence;
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
            _activeSequence?.Kill();
            _disposables.Dispose();
        }

        private void OnNotification(CheatNotification notification)
        {
            ShowToast(notification);
        }

        private void ShowToast(CheatNotification notification)
        {
            _activeSequence?.Kill();

            if (_label != null)
            {
                _label.text = notification.Message ?? string.Empty;
                _label.color = notification.Success ? _successColor : _failureColor;
            }

            if (_root != null) _root.anchoredPosition = _restAnchoredPosition;
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;

            _activeSequence = DOTween.Sequence().SetLink(gameObject);

            if (_canvasGroup != null)
                _activeSequence.Append(_canvasGroup.DOFade(1f, _fadeDuration));

            if (_root != null)
                _activeSequence.Join(_root.DOAnchorPosY(_restAnchoredPosition.y + _floatDistance, _visibleDuration + _fadeDuration).SetEase(Ease.OutCubic));

            _activeSequence.AppendInterval(Math.Max(0f, _visibleDuration - _fadeDuration));

            if (_canvasGroup != null)
                _activeSequence.Append(_canvasGroup.DOFade(0f, _fadeDuration));

            _activeSequence.OnComplete(HideImmediate);
        }

        private void HideImmediate()
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            if (_root != null) _root.anchoredPosition = _restAnchoredPosition;
        }
    }
}
