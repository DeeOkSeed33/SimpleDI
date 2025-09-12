using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DeeOkSeed33.DI
{
    public sealed class DIContainer
    {
        private const BindingFlags BINDING_FLAGS =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | 
            BindingFlags.Static;
        
        private readonly Dictionary<Type, object> _instanceByType = new();
        private readonly List<IInitializable> _initializables = new();

        private readonly bool _autoInitialize;

        public DIContainer(bool autoInitialize = false)
        {
            _autoInitialize = autoInitialize;
            
            Type globalInjectorType = typeof(GlobalInjector);
            FieldInfo diContainerField = globalInjectorType.GetField("_diContainer", BINDING_FLAGS);
            diContainerField.SetValue(null, this);
        }
        
        public void Register<TInstance>() where TInstance : class => Bind<TInstance, TInstance>();

        public void Bind<TAbstract, TInstance>() where TAbstract : class where TInstance : class
        {
            Type abstractType = typeof(TAbstract);
            Type instanceType = typeof(TInstance);
            
            object instance = Activator.CreateInstance(instanceType);
            
            if (!_instanceByType.TryAdd(abstractType, instance))
                throw new InvalidOperationException(
                    $"Type {abstractType.Name} is already registered in the DI container");
        }
        
        public void Add(object instance)
        {
            Type abstractType = instance.GetType();
            
            if (!_instanceByType.TryAdd(abstractType, instance))
                throw new InvalidOperationException(
                    $"Type {abstractType.Name} is already registered in the DI container");
        }

        public void AddAs<TAbstract>(object instance)
        {
            Type abstractType = typeof(TAbstract);
            
            if (!_instanceByType.TryAdd(abstractType, instance))
                throw new InvalidOperationException(
                    $"Type {abstractType.Name} is already registered in the DI container");
        }

        public TAbstract Get<TAbstract>()
        {
            Type abstractType = typeof(TAbstract);
            
            if (!_instanceByType.TryGetValue(abstractType, out object instance))
                throw new InvalidOperationException(
                    $"Type {abstractType.Name} doesn't exist in the DI container");

            return (TAbstract)instance;
        }

        public void InjectAll()
        {
            foreach ((_, object instance) in _instanceByType)
                InjectAt(instance);
        }

        public void InitAll()
        {
            foreach (IInitializable initializable in _initializables)
                initializable.Initialize();
        }

        private void FindInterfaces(object instance)
        {
            switch (instance)
            {
                case IInitializable initializable:
                    if (_autoInitialize)
                        initializable.Initialize();
                    else
                        _initializables.Add(initializable);
                    break;
            }
        }
        
        internal void InjectAt(object target)
        {
            Type type = target.GetType();
            var fields = type.GetFields(BINDING_FLAGS)
                .Where(m => m.GetCustomAttributes().Any(a => a is InjectAttribute)).ToArray();

            foreach (FieldInfo fieldInfo in fields)
                InjectFieldAt(target, fieldInfo);
            
            FindInterfaces(target);
        }

        private void InjectFieldAt(object target, FieldInfo fieldInfo)
        {
            Type fieldType = fieldInfo.FieldType;

            if (!_instanceByType.TryGetValue(fieldType, out object instance))
                throw new InvalidOperationException(
                    $"Type {fieldType.Name} doesn't exist in the DI container");
            
            fieldInfo.SetValue(target, instance);
        }
    }
}