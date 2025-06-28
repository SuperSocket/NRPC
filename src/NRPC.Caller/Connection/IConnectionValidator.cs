using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace NRPC.Caller.Connection
{
    public interface IConnectionValidator<TConnection>
    {
        bool Validate(TConnection connection);
    }

    class NullConnectionValidator<TConnection> : IConnectionValidator<TConnection>
    {
        public static readonly IConnectionValidator<TConnection> Instance = new NullConnectionValidator<TConnection>();

        private NullConnectionValidator()
        {
        }

        public bool Validate(TConnection connection)
        {
            return true;
        }
    }
}