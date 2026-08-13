using UnityEngine;

namespace BoosterModule
{
    public abstract class SingletonComp<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this as T;
            RightAfterAwake();
        }
        
        protected virtual void RightAfterAwake() { }
    }
}
