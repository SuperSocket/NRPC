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
        internal static object CreateProxyInstance(Type baseType, Type interfaceType)
        {
            var type = GenerateProxyType(baseType, interfaceType);
            return Activator.CreateInstance(type);
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
            
            var methodInfoLabel = il.DeclareLocal(typeof(MethodInfo));
            var argsLabel = il.DeclareLocal(typeof(object[]));
            var interfaceInstanceLabel = il.DeclareLocal(typeof(IRpcProxy));
            
            PutArgumentsToArray(il, paramTypes, argsLabel);
            
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
            il.Emit(OpCodes.Stloc, interfaceInstanceLabel);
            
            // ((IRpxProxy)this).GetMethod(name)
            il.Emit(OpCodes.Ldloc, interfaceInstanceLabel);
            il.Emit(OpCodes.Ldc_I4, methodIndex);
            il.Emit(OpCodes.Callvirt, getMethodInfoMethod);
            il.Emit(OpCodes.Stloc, methodInfoLabel);
            
            // (IRpxProxy)this
            il.Emit(OpCodes.Ldloc, interfaceInstanceLabel);

            // load methodInfo
            il.Emit(OpCodes.Ldloc, methodInfoLabel);
 
            // load args
            il.Emit(OpCodes.Ldloc, argsLabel);

            // invoke
            il.Emit(OpCodes.Callvirt, invokeMethod);
            
            // return result
            il.Emit(OpCodes.Ret);
            
            typeBuilder.DefineMethodOverride(mdb, method);
        }
                
        private static Type GenerateProxyType(Type baseType, Type interfaceType)
        {
            var baseTypeInfo = baseType.GetTypeInfo();
            var interfaceTypeInfo = interfaceType.GetTypeInfo();
            var taskType = typeof(Task).GetTypeInfo();
            
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("NRPC.ProxyAssembly"), AssemblyBuilderAccess.Run);
            
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("ProxyModule");
            
            var typeName = interfaceType.Name + "Imp";
            
            Debug.WriteLine($"TypeName:{typeName}");
            
            var tb = moduleBuilder.DefineType(typeName, TypeAttributes.Public, baseType);
            
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