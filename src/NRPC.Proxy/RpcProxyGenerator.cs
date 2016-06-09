using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;

namespace NRPC.Proxy
{
    static class RpcProxyGenerator
    {
        private static string ComposeProxyTypeName(Type baseType, Type interfaceType)
        {
            return baseType.Name + "_" + interfaceType.Name;
        }

        private static Dictionary<string, Type> m_ProxyTypesCache = new Dictionary<string, Type>();

        internal static object CreateProxyInstance(Type baseType, Type interfaceType)
        {
            return Activator.CreateInstance(GetProxyType(baseType, interfaceType));
        }

        internal static Type GetProxyType(Type baseType, Type interfaceType)
        {
            var proxyTypeName = ComposeProxyTypeName(baseType, interfaceType);

            Type proxyType = null;

            lock (m_ProxyTypesCache)
            {
                if(!m_ProxyTypesCache.TryGetValue(proxyTypeName, out proxyType))
                {
                    proxyType = GenerateProxyType(baseType, interfaceType, proxyTypeName);
                    m_ProxyTypesCache.Add(proxyTypeName, proxyType);
                }
            };

            return proxyType;
        }
        
        private static List<MethodInfo> m_MethodInfoLibs = new List<MethodInfo>();
        
        internal static MethodInfo GetMethodInfo(int index)
        {
            return m_MethodInfoLibs[index];
        }
        
        private static int RegisterMethodInfo(MethodInfo method)
        {
            lock (m_MethodInfoLibs)
            {
                var count = m_MethodInfoLibs.Count;
                m_MethodInfoLibs.Add(method);
                return count;
            }
        }
        
        private static Type GetReturnElementType(MethodInfo method)
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
            
            return returnElementType;
        }
        
        private static void PutArgumentsToArray(ILGenerator il, Type[] paramTypes, LocalBuilder argsLabel)
        {
            il.Emit(OpCodes.Nop);
            
            il.Emit(OpCodes.Ldc_I4, paramTypes.Length);
            il.Emit(OpCodes.Newarr, typeof(object));
            il.Emit(OpCodes.Stloc, argsLabel);

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
        }
        
        private static void EmitMethod(TypeBuilder typeBuilder, MethodInfo method)
        {
            var returnElementType = GetReturnElementType(method);
            
            var methodIndex = RegisterMethodInfo(method);
            
            var paramTypes = method.GetParameters()
                .Where(p => !p.IsOut)
                .Select(p => p.ParameterType)
                .ToArray();
                
            var mdb = typeBuilder.DefineMethod(method.Name,
                MethodAttributes.Public | MethodAttributes.Virtual,
                method.ReturnType,
                paramTypes);
            
            var il = mdb.GetILGenerator();
            
            var methodInfoLocal = il.DeclareLocal(typeof(MethodInfo));
            var argsLocal = il.DeclareLocal(typeof(object[]));
            var interfaceInstanceLocal = il.DeclareLocal(typeof(IRpcProxy));
            
            PutArgumentsToArray(il, paramTypes, argsLocal);
            
            var rpcProxyInterfaceType = typeof(IRpcProxy).GetTypeInfo();
            
            var getMethodInfoMethod = rpcProxyInterfaceType.GetDeclaredMethod("GetMethodInfo");
            
            var invokeMethod = rpcProxyInterfaceType.GetDeclaredMethod("Invoke");

            if (returnElementType == null) // Task only (void)
                invokeMethod = invokeMethod.MakeGenericMethod(typeof(object));
            else
                invokeMethod = invokeMethod.MakeGenericMethod(returnElementType);
            
            // this
            il.Emit(OpCodes.Ldarg_0);
            // as IRpxProxy
            il.Emit(OpCodes.Castclass, typeof(IRpcProxy));
            il.Emit(OpCodes.Stloc, interfaceInstanceLocal);
            
            // ((IRpxProxy)this).GetMethod(name)
            il.Emit(OpCodes.Ldloc, interfaceInstanceLocal);
            il.Emit(OpCodes.Ldc_I4, methodIndex);
            il.Emit(OpCodes.Callvirt, getMethodInfoMethod);
            il.Emit(OpCodes.Stloc, methodInfoLocal);
            
            // (IRpxProxy)this
            il.Emit(OpCodes.Ldloc, interfaceInstanceLocal);

            // load methodInfo
            il.Emit(OpCodes.Ldloc, methodInfoLocal);
 
            // load args
            il.Emit(OpCodes.Ldloc, argsLocal);

            // invoke
            il.Emit(OpCodes.Callvirt, invokeMethod);
            
            // return result
            il.Emit(OpCodes.Ret);
            
            typeBuilder.DefineMethodOverride(mdb, method);
        }

        private static void EmitConstructors(TypeBuilder typeBuilder, Type baseType)
        {
            foreach (var constructor in baseType.GetTypeInfo().DeclaredConstructors)
            {
                var parameters = constructor.GetParameters();
                
                var constructorBuilder = typeBuilder.DefineConstructor(
                        MethodAttributes.Public | 
                        MethodAttributes.SpecialName | 
                        MethodAttributes.RTSpecialName, 
                        CallingConventions.Standard, 
                        parameters.Select(p => p.GetType()).ToArray());

                var il = constructorBuilder.GetILGenerator();

                il.Emit(OpCodes.Nop);

                // this
                il.Emit(OpCodes.Ldarg_0);

                for(var i = 0; i < parameters.Length; i++)
                {
                    il.Emit(OpCodes.Ldarg, i + 1);
                }

                // base(xx)
                il.Emit(OpCodes.Call, constructor);

                il.Emit(OpCodes.Ret);
            }
        }
                
        private static Type GenerateProxyType(Type baseType, Type interfaceType, string proxyTypeName)
        {
            var baseTypeInfo = baseType.GetTypeInfo();
            var interfaceTypeInfo = interfaceType.GetTypeInfo();
            var taskType = typeof(Task).GetTypeInfo();
            
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("NRPC.ProxyAssembly"), AssemblyBuilderAccess.Run);
            
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("ProxyModule");
            
            Debug.WriteLine($"TypeName:{proxyTypeName}");
            
            var tb = moduleBuilder.DefineType(proxyTypeName, TypeAttributes.Public, baseType);
            
            EmitConstructors(tb, baseType);

            tb.AddInterfaceImplementation(interfaceType);

            foreach (var method in interfaceType.GetRuntimeMethods())
            {
                lock (m_MethodInfoLibs)
                {
                    EmitMethod(tb, method);
                }
            }
            
            return tb.CreateTypeInfo().AsType();
        }
    }
}