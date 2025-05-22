using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
        public IReadOnlyList<MethodMetadata> Methods { get; }

        public ServiceMetadata(Type serviceType)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
            Methods = serviceType.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).Select(m => new MethodMetadata(m)).ToArray();
        }
    }
}