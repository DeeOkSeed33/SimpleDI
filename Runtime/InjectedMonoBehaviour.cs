using UnityEngine;

namespace DeeOkSeed33.DI
{
    public abstract class InjectedMonoBehaviour : MonoBehaviour
    {
        private void Awake()
        {
            GlobalInjector.InjectAt(this);
            Init();
        }
        
        protected virtual void Init() {}
    }
}