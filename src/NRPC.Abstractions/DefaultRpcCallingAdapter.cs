using System;
using System.Threading.Tasks;

namespace NRPC.Abstractions
{
    public class DefaultRpcCallingAdapter : IRpcCallingAdapter
    {
        public static DefaultRpcCallingAdapter Singleton { get; } = new DefaultRpcCallingAdapter();
    }
}