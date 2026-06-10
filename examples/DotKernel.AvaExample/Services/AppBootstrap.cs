using DotKernel.AvaExample.ViewModels;
using Microsoft.Extensions.Configuration;

namespace DotKernel.AvaExample.Services;

public static class AppBootstrap
{
    public static MainViewModel CreateMainViewModel(bool useUserSecrets)
    {
        var configuration = BuildConfiguration(useUserSecrets);
        var (host, twin, historyFilter, confirmationFilter, status) = KernelAppFactory.Create(configuration);
        return new MainViewModel(host, twin, historyFilter, confirmationFilter, status);
    }

    public static IConfiguration BuildConfiguration(bool useUserSecrets)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables(prefix: "DOTKERNEL_");

        if (useUserSecrets)
        {
            builder.AddUserSecrets(typeof(App).Assembly, optional: true);
        }

        return builder.Build();
    }
}
