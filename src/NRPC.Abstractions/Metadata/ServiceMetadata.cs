using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NRPC.Abstractions.Metadata
{
    /// <summary>
    /// Represents metadata information about a service.
    /// </summary>
    public class ServiceMetadata
    {
        /// <summary>
        /// Gets or sets the service type.
        /// </summary>
        public Type ServiceType { get; }

        /// <summary>
        /// Gets or sets the list of method metadata for the service.
        /// </summary>
        public IReadOnlyDictionary<string, MethodMetadata> Methods { get; }

        private ServiceMetadata(Type serviceType, IEnumerable<MethodMetadata> methods)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
            Methods = methods.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        }

        public static ServiceMetadata Create<TService>(IParameterExpressionConverter parameterExpressionConverter = null)
        {
            if (parameterExpressionConverter == null)
                parameterExpressionConverter = DirectTypeParameterExpressionConverter.Singleton;

            var serviceType = typeof(TService);
            var methods = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => !m.IsSpecialName) // Exclude property accessors
                .Where(m => typeof(Task).IsAssignableFrom(m.ReturnType)) // Only include async methods
                .Select(m => new MethodMetadata<TService>(m, parameterExpressionConverter))
                .ToArray();

            return new ServiceMetadata(serviceType, methods);
        }
    }
}