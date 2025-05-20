namespace NRPC.Test;

public class TestService : ITestService
{
    public virtual Task<int> Add(int a, int b) => Task.FromResult(a + b);

    public virtual Task<string> Concat(string a, string b) => Task.FromResult(a + b);

    public virtual Task ExecuteVoid(string command)
    {
        return Task.Delay(1000);
    }
}