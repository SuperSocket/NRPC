using NRPC.Caller;

namespace NRPC.Test;

[ServiceContractAtribute]
public interface ITestService
{
    Task<int> Add(int x, int y);

    Task<string> Concat(string x, string y);

    Task ExecuteVoid(string command);
}