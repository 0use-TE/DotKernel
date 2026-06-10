using DotKernel;

[KernelPlugin("Global")]
public partial class GlobalNamespacePlugin
{
    [KernelFunction("ping")]
    public Task<string> PingAsync(CancellationToken cancellationToken = default)
        => Task.FromResult("pong");
}
