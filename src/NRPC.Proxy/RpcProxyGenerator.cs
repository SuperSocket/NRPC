using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace NRPC.Proxy
{
    internal static class RpcProxyGenerator
    {
        internal static object CreateProxyInstance(Type baseType, Type interfaceType)
        {
            var type = GenerateProxyType(baseType, interfaceType);
            return Activator.CreateInstance(type);
        }
        
        private static Type GenerateProxyType(Type baseType, Type interfaceType)
        {
            var baseTypeInfo = baseType.GetTypeInfo();
            var interfaceTypeInfo = interfaceType.GetTypeInfo();
            
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ProxyAssembly"), AssemblyBuilderAccess.Run);
            
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("ProxyModule");
            
            var tb = moduleBuilder.DefineType(interfaceType.Name + "Imp", TypeAttributes.Public, baseType);
            
            tb.AddInterfaceImplementation(interfaceType);
            
            var invokeMethod = baseType.GetTypeInfo().GetDeclaredMethod("Invoke");
            
            foreach (var method in interfaceType.GetRuntimeMethods())
            {
                var paramTypes = method.GetParameters()
                    .Where(p => !p.IsOut)
                    .Select(p => p.ParameterType)
                    .ToArray();
                    
                var mdb = tb.DefineMethod(method.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    method.ReturnType,
                    paramTypes);
                    
               if (method.ContainsGenericParameters)
                {
                    Type[] ts = method.GetGenericArguments();
                    
                    var genericParameters = mdb.DefineGenericParameters(ts.Select(t => t.Name).ToArray());
                    
                    for (int i = 0; i < genericParameters.Length; i++)
                    {
                        genericParameters[i].SetGenericParameterAttributes(ts[i].GetTypeInfo().GenericParameterAttributes);
                    }
                }
                
                var il = mdb.GetILGenerator();

                for (int i = 0; i < paramTypes.Length; i++)
                {
                    il.Emit(OpCodes.Ldarg, i + 1);
                    Console.WriteLine(i);
                }
                
                var ctor = typeof(NotImplementedException)
                    .GetTypeInfo()
                    .DeclaredConstructors.FirstOrDefault(c => !c.GetParameters().Any());
                    
                il.Emit(OpCodes.Newobj, ctor);
                il.Emit(OpCodes.Throw);
                
                tb.DefineMethodOverride(mdb, method);
            }
            
            return tb.CreateTypeInfo().AsType();
        }
    }
}