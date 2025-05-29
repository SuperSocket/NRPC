using System;
using System.Threading.Tasks;
using NRPC.Abstractions.Metadata;

namespace NRPC.Caller
{
    interface ITypedResponseHandler : IResponseHandler
    {
        void Initialize(IExpressionConverter expressionConverter);
    }
}