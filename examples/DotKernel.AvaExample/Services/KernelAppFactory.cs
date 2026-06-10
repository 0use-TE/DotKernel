using DotKernel;
using DotKernel.AvaExample.Filters;
using DotKernel.AvaExample.Infrastructure;
using DotKernel.AvaExample.Plugins;
using DotKernel.AvaExample.Twin;
using Microsoft.Extensions.Configuration;

namespace DotKernel.AvaExample.Services;

public static class KernelAppFactory
{
    public static (KernelHost Host, BuildingTwinState Twin, ToolCallHistoryFilter Filter, string Status) Create(
        IConfiguration configuration)
    {
        var twin = new BuildingTwinState();
        var plugin = new BuildingTwinPlugin(twin);
        var filter = new ToolCallHistoryFilter();
        var (client, status) = DeepSeekChatClientFactory.Create(configuration);

        var kernel = KernelBuilder.Create()
            .AddChatClient(client)
            .AddPlugin(plugin)
            .AddFilter(filter)
            .Build();

        return (new KernelHost(kernel), twin, filter, status);
    }
}
