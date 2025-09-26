using System.Collections.Generic;
using UnityEngine;

namespace DeeOkSeed33.DI
{
    public sealed class ComponentInjector : MonoBehaviour
    {
        [SerializeField] private List<Component> _components = new();

        private void Awake()
        {
            foreach (Component component in _components)
            {
                GlobalInjector.InjectAt(component);
                if (component is IInjectable injectable)
                    injectable.OnInjected();
            }
        }
    }
}