using UnityEngine;

namespace ClientCore
{
    [RequireComponent(typeof(Animation))]
    public class ManualUpdateAnimation : MonoBehaviour
    {
        private Animation _cachedAnimation;
        public Animation CachedAnimation
        {
            get
            {
                if (_cachedAnimation == null)
                {
                    _cachedAnimation = GetComponent<Animation>();
                    _cachedAnimation.enabled = false;
                }
                return _cachedAnimation;
            }
        }
        
        private AnimationState _currentAnimationState;
        private bool _play = false;
        private bool _loop = false;
        
        private void Update()
        {
            if (_play && _currentAnimationState)
            {
                _currentAnimationState.time += Time.deltaTime;
                CachedAnimation.Sample();

                if (!_loop && _currentAnimationState.time > _currentAnimationState.length)
                {
                    _play = false;
                }
            }
        }
        
        public void Play(string animation)
        {
            var animationState = CachedAnimation[animation];
            if (animationState)
            {
                _cachedAnimation.Play(animation);
                _currentAnimationState = animationState;
                _play = true;

                _loop = (_currentAnimationState.wrapMode != WrapMode.Once &&
                         _currentAnimationState.wrapMode != WrapMode.Default);
            }
            
            CachedAnimation.Sample();
        }

        public void Stop()
        {
            
        }
    }
}