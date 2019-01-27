using System;
using System.Linq;

namespace VisualAdjustments
{
    public delegate TResult FastGetter<TClass, TResult>(TClass source);
    public delegate object FastGetter(object source);
    public delegate void FastSetter(object source, object value);
    public delegate void FastSetter<TClass, TValue>(TClass source, TValue value);
    public delegate object FastInvoker(object target, params object[] paramters);
    public delegate TResult FastInvoker<TClass, TResult>(TClass target);
    public delegate TResult FastInvoker<TClass, T1, TResult>(TClass target, T1 arg1);
    public delegate TResult FastInvoker<TClass, T1, T2, TResult>(TClass target, T1 arg1, T2 arg2);
    public delegate TResult FastInvoker<TClass, T1, T2, T3, TResult>(TClass target, T1 arg1, T2 arg2, T3 arg);
    public delegate object FastStaticInvoker(params object[] parameters);
    public delegate TResult FastStaticInvoker<out TResult>();
    public delegate TResult FastStaticInvoker<in T1, out TResult>(T1 arg1);
    public delegate TResult FastStaticInvoker<in T1, in T2, out TResult>(T1 arg1, T2 arg2);
    public delegate TResult FastStaticInvoker<in T1, in T2, in T3, out TResult>(T1 arg1, T2 arg2, T3 arg3);
    public class Accessors
    {
        public static Harmony12.AccessTools.FieldRef<TClass, TResult> CreateFieldRef<TClass, TResult>(string name)
        {
            var classType = typeof(TClass);
            var resultType = typeof(TResult);
            var fieldInfo = Harmony12.AccessTools.Field(classType, name);
            if (fieldInfo == null)
            {
                throw new Exception($"{classType} does not contain field {name}");
            }
            if (!resultType.IsAssignableFrom(fieldInfo.FieldType))
            {
                throw new InvalidCastException($"Cannot cast field type {resultType} as {fieldInfo.FieldType} for class {classType} field {name}");
            }
            return Harmony12.AccessTools.FieldRefAccess<TClass, TResult>(name);
        }
        public static FastGetter CreateGetter(Type classType, Type resultType, string name)
        {
            var fieldInfo = Harmony12.AccessTools.Field(classType, name);
            var propInfo = Harmony12.AccessTools.Property(classType, name);
            if (fieldInfo == null && propInfo == null)
            {
                throw new Exception($"{classType} does not contain field or property {name}");
            }
            bool isProp = propInfo != null;
            Type memberType = isProp ? propInfo.PropertyType : fieldInfo.FieldType;
            string memberTypeName = isProp ? "property" : "field";
            if (!resultType.IsAssignableFrom(memberType))
            {
                throw new InvalidCastException($"Cannot cast field type {resultType} as {memberType} for class {classType} {memberTypeName} {name}");
            }
            var handler = isProp ?
                Harmony12.FastAccess.CreateGetterHandler(propInfo) :
                Harmony12.FastAccess.CreateGetterHandler(fieldInfo);
            return new FastGetter(handler);
        }
        public static FastGetter<TClass, TResult> CreateGetter<TClass, TResult>(string name)
        {
            var classType = typeof(TClass);
            var resultType = typeof(TResult);
            var handler = CreateGetter(classType, resultType, name);
            return new FastGetter<TClass, TResult>((instance) => (TResult)handler.Invoke(instance));
        }
        public static FastSetter CreateSetter(Type classType, Type valueType, string name)
        {
            var propertyInfo = Harmony12.AccessTools.Property(classType, name);
            var fieldInfo = Harmony12.AccessTools.Field(classType, name);
            if (propertyInfo == null && fieldInfo == null)
            {
                throw new Exception($"{classType} does not contain a field or property {name}");
            }
            bool isProperty = propertyInfo != null;
            Type memberType = isProperty ? propertyInfo.PropertyType : fieldInfo.FieldType;
            string memberTypeName = isProperty ? "property" : "field";
            if (!valueType.IsAssignableFrom(memberType))
            {
                throw new Exception($"Cannot cast property type {valueType} as {memberType} for class {classType} {memberTypeName} {name}");
            }
            var handler = isProperty ?
                Harmony12.FastAccess.CreateSetterHandler(propertyInfo) :
                Harmony12.FastAccess.CreateSetterHandler(fieldInfo);
            return new FastSetter(handler);
        }
        public static FastSetter<TClass, TValue> CreateSetter<TClass, TValue>(string name)
        {
            var classType = typeof(TClass);
            var valueType = typeof(TValue);
            var handler = CreateSetter(classType, valueType, name);
            return new FastSetter<TClass, TValue>((instance, value) => handler.Invoke(instance, value));
        }
        public static FastInvoker CreateInvoker(Type classType, string name, Type resultType, params Type[] parameters)
        {
            var methodInfo = Harmony12.AccessTools.Method(classType, name, parameters);
            if (methodInfo == null)
            {
                var argString = string.Join(", ", parameters.Select(t => t.ToString()));
                throw new Exception($"{classType} does not contain method {name} with arguments {argString}");
            }
            if (!resultType.IsAssignableFrom(methodInfo.ReturnType))
            {
                throw new Exception($"Cannot cast return type {resultType} as {methodInfo.ReturnType} for class {classType} method {name}");
            }
            var _parameters = methodInfo.GetParameters();
            if (_parameters.Length != parameters.Length)
            {
                throw new Exception($"Expected {parameters.Length} paramters for class {classType} method {name}");
            }
            for (int i = 0; i < parameters.Length; i++)
            {
                if (!parameters[i].IsAssignableFrom(_parameters[i].ParameterType))
                {
                    throw new Exception($"Cannot cast paramter type {parameters[i]} as {_parameters[i].ParameterType} for class {classType} method {name}");
                }
            }
            return new FastInvoker(Harmony12.MethodInvoker.GetHandler(methodInfo));
        }
        public static FastInvoker<TClass, TResult> CreateInvoker<TClass, TResult>(string name)
        {
            var classType = typeof(TClass);
            var resultType = typeof(TResult);
            var args = new Type[] { };
            var invoker = CreateInvoker(classType, name, resultType, args);
            return new FastInvoker<TClass, TResult>((instance) => (TResult)invoker.Invoke(instance));
        }
        public static FastInvoker<TClass, T1, TResult> CreateInvoker<TClass, T1, TResult>(string name)
        {
            var classType = typeof(TClass);
            var resultType = typeof(TResult);
            var args = new Type[] { typeof(T1) };
            var invoker = CreateInvoker(classType, name, resultType, args);
            return new FastInvoker<TClass, T1, TResult>((instance, arg1) => (TResult)invoker.Invoke(instance, new object[] { arg1 }));
        }
        public static FastInvoker<TClass, T1, T2, TResult> CreateInvoker<TClass, T1, T2, TResult>(string name)
        {
            var classType = typeof(TClass);
            var resultType = typeof(TResult);
            var args = new Type[] { typeof(T1), typeof(T2) };
            var invoker = CreateInvoker(classType, name, resultType, args);
            return new FastInvoker<TClass, T1, T2, TResult>((instance, arg1, arg2) => (TResult)invoker.Invoke(instance, new object[] { arg1, arg2 }));
        }
        public static FastInvoker<TClass, T1, T2, T3, TResult> CreateInvoker<TClass, T1, T2, T3, TResult>(string name)
        {
            var classType = typeof(TClass);
            var resultType = typeof(TResult);
            var args = new Type[] { typeof(T1), typeof(T2), typeof(T3) };
            var invoker = CreateInvoker(classType, name, resultType, args);
            return new FastInvoker<TClass, T1, T2, T3, TResult>((instance, arg1, arg2, arg3) => (TResult)invoker.Invoke(instance, new object[] { arg1, arg2, arg3 }));
        }
        private class StaticFastInvokeHandler
        {
            private readonly Type classType;
            private readonly Harmony12.FastInvokeHandler invoker;

            public StaticFastInvokeHandler(Type classType, Harmony12.FastInvokeHandler invoker)
            {
                this.classType = classType;
                this.invoker = invoker;
            }

            public object Invoke(params object[] args)
            {
                return invoker.Invoke(classType, args);
            }
        }
        public static FastStaticInvoker CreateStateInvoker(Type classType, string name, Type resultType, params Type[] parameters)
        {
            var methodInfo = Harmony12.AccessTools.Method(classType, name, parameters);
            if (methodInfo == null)
            {
                var argString = string.Join(", ", parameters.Select(t => t.ToString()));
                throw new Exception($"{classType} does not contain method {name} with arguments {argString}");
            }
            if (!resultType.IsAssignableFrom(methodInfo.ReturnType))
            {
                throw new Exception($"Cannot cast return type {resultType} as {methodInfo.ReturnType} for class {classType} method {name}");
            }
            var _parameters = methodInfo.GetParameters();
            if (_parameters.Length != parameters.Length)
            {
                throw new Exception($"Expected {parameters.Length} paramters for class {classType} method {name}");
            }
            for (int i = 0; i < parameters.Length; i++)
            {
                if (!parameters[i].IsAssignableFrom(_parameters[i].ParameterType))
                {
                    throw new Exception($"Cannot cast paramter type {parameters[i]} as {_parameters[i].ParameterType} for class {classType} method {name}");
                }
            }
            return new StaticFastInvokeHandler(classType, Harmony12.MethodInvoker.GetHandler(methodInfo)).Invoke;
        }
    }
}