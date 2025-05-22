using System;
using System.Reflection;

namespace NRPC.Abstractions.Metadata
{
    /// <summary>
    /// Represents metadata information about a method parameter.
    /// </summary>
    public class ParameterMetadata
    {
        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the type of the parameter.
        /// </summary>
        public Type ParameterType { get; }

        /// <summary>
        /// Gets a value indicating whether the parameter has a default value.
        /// </summary>
        public bool HasDefaultValue { get; }

        /// <summary>
        /// Gets the default value of the parameter, if any.
        /// </summary>
        public object DefaultValue { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterMetadata"/> class.
        /// </summary>
        /// <param name="parameterInfo">The parameter info to extract metadata from.</param>
        public ParameterMetadata(ParameterInfo parameterInfo)
        {
            if (parameterInfo == null)
                throw new ArgumentNullException(nameof(parameterInfo));

            Name = parameterInfo.Name ?? string.Empty;
            ParameterType = parameterInfo.ParameterType;
            HasDefaultValue = parameterInfo.HasDefaultValue;
            DefaultValue = parameterInfo.HasDefaultValue ? parameterInfo.DefaultValue : null;
        }
    }
}