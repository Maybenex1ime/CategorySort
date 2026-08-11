using DG.Tweening;
using UnityEngine;

namespace LogosSDK.UI.Animation
{
    [CreateAssetMenu(menuName = "LogosSDK/UI/Stagger Animation", fileName = "SO_Stagger_")]
    public sealed class UIStaggerAnimationSO : ScriptableObject
    {
        [SerializeField] private StaggerType _staggerType = StaggerType.Pop;
        [SerializeField] private float _delayBetweenElements = 0.07f;
        [SerializeField] private float _elementDuration = 0.18f;
        [SerializeField] private Ease _elementEase = Ease.OutBack;

        public StaggerType StaggerType => _staggerType;
        public float DelayBetweenElements => _delayBetweenElements;
        public float ElementDuration => _elementDuration;
        public Ease ElementEase => _elementEase;

#if UNITY_EDITOR
        public void SetForTest(
            StaggerType staggerType = StaggerType.Pop,
            float delay = 0.07f,
            float duration = 0.18f,
            Ease ease = Ease.OutBack)
        {
            _staggerType = staggerType;
            _delayBetweenElements = delay;
            _elementDuration = duration;
            _elementEase = ease;
        }
#endif
    }
}
