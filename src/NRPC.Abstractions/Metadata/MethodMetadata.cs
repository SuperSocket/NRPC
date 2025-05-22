using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NRPC.Abstractions.Metadata
{
    public class MethodMetadata
    {
        public string Name { get; }

        public Type ReturnType { get; }

        public IReadOnlyList<ParameterMetadata> Parameters { get; }

        public MethodInfo MethodInfo { get; }

        public MethodMetadata(MethodInfo methodInfo)
        {
            MethodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));
            Name = methodInfo.Name;
            ReturnType = methodInfo.ReturnType;
            Parameters = methodInfo.GetParameters().Select(p => new ParameterMetadata(p)).ToArray();
        }
    }
}