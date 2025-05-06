using System;

namespace NRPC.Client
{
    /// <summary>
    /// Indicates that the class is a service contract.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, Inherited = false)]
    public class ServiceContractAtribute : Attribute
    {
    }
}