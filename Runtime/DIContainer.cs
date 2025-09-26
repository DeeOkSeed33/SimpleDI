using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DeeOkSeed33.DI
{
    public sealed class DIContainer
    {
        private const BindingFlags BINDING_FLAGS =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        private const BindingFlags BINDING_FLAGS_STATIC =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        
        private readonly Dictionary<Type, object> _instanceByType = new();
        private readonly List<IPreInitializable> _preInitializables = new();
        private readonly List<IInitializable> _initializables = new();
        private readonly List<IPostInitializable> _postInitializables = new();

        private Type[] _baseStopTypes = {typeof(System.Object), typeof(UnityEngine.Object), typeof(MonoBehaviour)};

        public DIContainer()
        {
            Add(this);
            InjectStaticFields<GlobalInjector>();
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

        public void InjectStaticFields<T>()
        {
            Type type = typeof(T);
            
            foreach (FieldInfo fieldInfo in GetInjectableFields(type, BINDING_FLAGS_STATIC))
                InjectFieldAt(null, fieldInfo);
        }

        public void PreInitAll()
        {
            foreach (IPreInitializable preInitializable in _preInitializables)
                preInitializable.PreInitialize();
        }

        public void InitAll()
        {
            foreach (IInitializable initializable in _initializables)
                initializable.Initialize();
        }

        public void PostInitAll()
        {
            foreach (IPostInitializable postInitializable in _postInitializables)
                postInitializable.PostInitialize();
        }

        private void FindInterfaces(object instance)
        {
            if (instance is IPreInitializable preInitializable)
                _preInitializables.Add(preInitializable);
    
            if (instance is IInitializable initializable)
                _initializables.Add(initializable);
        
            if (instance is IPostInitializable postInitializable)
                _postInitializables.Add(postInitializable);
        }
        
        internal void InjectAt(object target)
        {
            Type type = target.GetType();

            while (!_baseStopTypes.Contains(type))
            {
                var fields = GetInjectableFields(type, BINDING_FLAGS);

                foreach (FieldInfo fieldInfo in fields)
                    InjectFieldAt(target, fieldInfo);
                
                type = type.BaseType;
            }
            
            FindInterfaces(target);
        }

        private FieldInfo[] GetInjectableFields(Type type, BindingFlags flags)
        {
            return type.GetFields(flags)
                .Where(m => m.GetCustomAttributes().Any(a => a is InjectAttribute)).ToArray();
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