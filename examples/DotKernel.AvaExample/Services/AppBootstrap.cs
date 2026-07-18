using DotKernel.AvaExample.ViewModels;
using Microsoft.Extensions.Configuration;

namespace DotKernel.AvaExample.Services;

public static class AppBootstrap
{
    public static MainViewModel CreateMainViewModel(bool useUserSecrets)
    {
        var configuration = BuildConfiguration(useUserSecrets);
        var (host, twin, historyFilter, confirmationFilter, status) = KernelAppFactory.Create(configuration);
        return new MainViewModel(
            host,
            twin,
            historyFilter,
            confirmationFilter,
            status,
            apiKey: configuration["DeepSeek:ApiKey"],
            endpoint: configuration["DeepSeek:Endpoint"],
            modelId: configuration["DeepSeek:ModelId"]);
    }

    public static IConfiguration BuildConfiguration(bool useUserSecrets)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddEnvironmentVariables(prefix: "DOTKERNEL_");

        if (useUserSecrets)
        {
            builder.AddUserSecrets(typeof(App).Assembly, optional: true);
        }

        // Optional local/CI file (not committed). UI can also set Endpoint / Model / API key at runtime.
        var appsettings = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(appsettings))
        {
            builder.AddJsonFile(appsettings, optional: true);
        }

        return builder.Build();
    }
}
