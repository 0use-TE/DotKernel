using DotKernel;
using DotKernel.AvaExample.Filters;
using DotKernel.AvaExample.Infrastructure;
using DotKernel.AvaExample.Plugins;
using DotKernel.AvaExample.Twin;
using Microsoft.Extensions.Configuration;

namespace DotKernel.AvaExample.Services;

public static class KernelAppFactory
{
    public static (
        KernelHost Host,
        BuildingTwinState Twin,
        ToolCallHistoryFilter HistoryFilter,
        ToolCallConfirmationFilter ConfirmationFilter,
        string Status) Create(IConfiguration configuration)
    {
        var twin = new BuildingTwinState();
        var plugin = new BuildingTwinPlugin(twin);
        var historyFilter = new ToolCallHistoryFilter();
        var confirmationFilter = new ToolCallConfirmationFilter();
        var (client, status) = DeepSeekChatClientFactory.Create(configuration);

        var kernel = KernelBuilder.Create()
            .AddChatClient(client)
            .AddPlugin(plugin)
            .AddFilter(historyFilter)
            .AddFilter(confirmationFilter)
            .Build();

        return (new KernelHost(kernel), twin, historyFilter, confirmationFilter, status);
    }
}
