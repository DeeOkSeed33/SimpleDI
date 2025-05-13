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
        
        private Dictionary<Type, object> _instanceByType = new();
        private Dictionary<Type, Type> _instanceTypeByType = new();

        public DIContainer()
        {
            Type globalInjectorType = typeof(GlobalInjector);
            FieldInfo diContainerField = globalInjectorType.GetField("_diContainer", BINDING_FLAGS);
            diContainerField.SetValue(null, this);
        }
        
        public void Register<TConcrete>() where TConcrete : class
        {
            Bind<TConcrete, TConcrete>();
        }

        public void Bind<TAbstract, TConcrete>() where TAbstract : class where TConcrete : class
        {
            Type abstractType = typeof(TAbstract);
            Type concreteType = typeof(TConcrete);
            
            if (!_instanceTypeByType.TryAdd(abstractType, concreteType))
                throw new InvalidOperationException(
                    $"Type {abstractType.Name} is already registered in the DI container");
        }

        public TAbstract Resolve<TAbstract>() where TAbstract : class
        {
            Type abstractType = typeof(TAbstract);
            return Resolve(abstractType) as TAbstract;
        }

        internal void InjectAt(object target)
        {
            Type type = target.GetType();
            var members = type.GetMembers(BINDING_FLAGS)
                .Where(m => m.GetCustomAttributes().Any(a => a is InjectAttribute)).ToArray();

            foreach (MemberInfo memberInfo in members)
            {
                switch (memberInfo)
                {
                    case FieldInfo fieldInfo:
                        InjectFieldAt(target, fieldInfo);
                        break;
                    case MethodInfo methodInfo:
                        InjectMethodAt(target, methodInfo);
                        break;
                }
            }
        }
        
        private object Resolve(Type abstractType)
        {
            if (_instanceByType.TryGetValue(abstractType, out object instance))
                return instance;
            
            if (!_instanceTypeByType.TryGetValue(abstractType, out Type concreteType))
                throw new InvalidOperationException(
                    $"Type {abstractType.Name} was not registered in the DI container");

            ConstructorInfo constructor =
                concreteType.GetConstructors(BINDING_FLAGS).OrderByDescending(c => c.GetParameters().Length).First();

            ParameterInfo[] parameters = constructor.GetParameters();
            
            object[] values = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                values[i] = Resolve(parameter.ParameterType);
            }

            instance = constructor.Invoke(values);
            _instanceByType[abstractType] = instance;

            return instance;
        }

        private void InjectFieldAt(object target, FieldInfo fieldInfo)
        {
            Type fieldType = fieldInfo.FieldType;
            object instance = Resolve(fieldType);
            fieldInfo.SetValue(target, instance);
        }

        private void InjectMethodAt(object target, MethodInfo methodInfo)
        {
            ParameterInfo[] parameters = methodInfo.GetParameters();
            
            object[] values = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                values[i] = Resolve(parameter.ParameterType);
            }

            methodInfo.Invoke(target, values);
        }
    }
}