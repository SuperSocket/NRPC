using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;

namespace NRPC.Proxy
{
    public static class RpcProxyGenerator
    {        
        internal static object CreateProxyInstance(Type baseType, Type interfaceType)
        {
            var type = GenerateProxyType(baseType, interfaceType);
            return Activator.CreateInstance(type);
        }
        
        public static Task Invoke<T>(IRpcProxy proxy, MethodInfo targetMethod, object[] args)
        {
            return proxy.Invoke<T>(targetMethod, args);
        }
        
        private static Dictionary<string, MethodInfo> m_MethodInfoLibs = new Dictionary<string, MethodInfo>(StringComparer.OrdinalIgnoreCase);
        
        public static MethodInfo GetMethodInfo(string name)
        {
            lock (m_MethodInfoLibs)
            {
                return m_MethodInfoLibs[name];
            }
        }
        
        private static void RegisterMethodInfo(string name, MethodInfo method)
        {
            lock (m_MethodInfoLibs)
            {
                m_MethodInfoLibs.Add(name, method);
            }
        }
        
        private static void EmitMethod(TypeBuilder typeBuilder, MethodInfo method)
        {
            var returnType = method.ReturnType.GetTypeInfo();
                
            var returnElementType = default(Type);
            
            if (returnType.GenericTypeArguments != null && returnType.GenericTypeArguments.Any())
            {
                returnElementType = returnType.GenericTypeArguments.FirstOrDefault();
            }
            
            if (!typeof(Task).GetTypeInfo().IsAssignableFrom(returnType))
            {
                Debug.WriteLine($"ReturnType is not allowed: {returnType}");
                throw new Exception("Only task base return type is supported.");
            }
            
            RegisterMethodInfo(method.Name, method);
            
            var paramTypes = method.GetParameters()
                .Where(p => !p.IsOut)
                .Select(p => p.ParameterType)
                .ToArray();
                
            var mdb = typeBuilder.DefineMethod(method.Name,
                MethodAttributes.Public | MethodAttributes.Virtual,
                method.ReturnType,
                paramTypes);
            
            var il = mdb.GetILGenerator();
            
            var methodInfoLabel = il.DeclareLocal(typeof(MethodInfo));
            var argsLabel = il.DeclareLocal(typeof(object[]));
            
            il.Emit(OpCodes.Nop);
            
            var getMethodMethod = typeof(RpcProxyGenerator).GetTypeInfo().GetDeclaredMethod("GetMethodInfo");
            
            il.Emit(OpCodes.Ldstr, method.Name);
            il.Emit(OpCodes.Call, getMethodMethod);
            il.Emit(OpCodes.Stloc, methodInfoLabel);

            var invokeMethod = typeof(RpcProxyGenerator).GetTypeInfo().GetDeclaredMethod("Invoke");
            invokeMethod = invokeMethod.MakeGenericMethod(returnElementType);
            
            il.Emit(OpCodes.Ldc_I4, paramTypes.Length);
            il.Emit(OpCodes.Newarr, typeof(object));
            il.Emit(OpCodes.Stloc, argsLabel);
            //il.Emit(OpCodes.Ldloc, argsLabel);
            //il.Emit(OpCodes.Call, typeof(Console).GetRuntimeMethod("WriteLine", new Type[] { typeof(object) }));
            

            for (int i = 0; i < paramTypes.Length; i++)
            {
                var paramType = paramTypes[i];
                
                il.Emit(OpCodes.Ldloc, argsLabel);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldarg, i + 1);
                
                if (paramType.GetTypeInfo().IsValueType)
                {
                    il.Emit(OpCodes.Box, paramType);
                }
                
                il.Emit(OpCodes.Stelem_Ref);
            }
            
            //il.Emit(OpCodes.Ldstr, "{0},{1},{2}");
            il.Emit(OpCodes.Ldarg_0);
            //il.Emit(OpCodes.Call, typeof(Console).GetRuntimeMethod("WriteLine", new Type[] { typeof(object) }));
            il.Emit(OpCodes.Ldloc_0);
            //il.Emit(OpCodes.Call, typeof(Console).GetRuntimeMethod("WriteLine", new Type[] { typeof(object) }));
            il.Emit(OpCodes.Ldloc_1);
            //il.Emit(OpCodes.Call, typeof(Console).GetRuntimeMethod("WriteLine", new Type[] { typeof(object) }));
            
            //Console.WriteLine(invokeMethod);
            
            //il.Emit(OpCodes.Call, typeof(Console).GetRuntimeMethod("WriteLine", new Type[] { typeof(string), typeof(object), typeof(object), typeof(object) }));
            il.Emit(OpCodes.Call, invokeMethod);
            //il.Emit(OpCodes.Call, typeof(Console).GetRuntimeMethod("WriteLine", new Type[] { typeof(object) }));
            
            //il.Emit(OpCodes.Ldstr, "SumResult");
            il.Emit(OpCodes.Ret);
            
            typeBuilder.DefineMethodOverride(mdb, method);
        }
                
        private static Type GenerateProxyType(Type baseType, Type interfaceType)
        {
            var baseTypeInfo = baseType.GetTypeInfo();
            var interfaceTypeInfo = interfaceType.GetTypeInfo();
            var taskType = typeof(Task).GetTypeInfo();
            
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ProxyAssembly"), AssemblyBuilderAccess.Run);
            
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("ProxyModule");
            
            var typeName = interfaceType.Name + "Imp";
            
            Debug.WriteLine($"TypeName:{typeName}");
            
            var tb = moduleBuilder.DefineType(typeName, TypeAttributes.Public, baseType);
            
            tb.AddInterfaceImplementation(interfaceType);

            foreach (var method in interfaceType.GetRuntimeMethods())
            {
                EmitMethod(tb, method);
            }
            
            return tb.CreateTypeInfo().AsType();
        }
    }
}