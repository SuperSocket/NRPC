using System;

namespace NRPC.Caller
{
    /// <summary>
    /// Indicates that the class is a service contract.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, Inherited = false)]
    public class ServiceContractAtribute : Attribute
    {
    }
}