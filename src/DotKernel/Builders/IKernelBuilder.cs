using Microsoft.Extensions.AI;

namespace DotKernel;

public interface IKernelBuilder
{
    IKernelBuilder AddChatClient(IChatClient chatClient);

    /// <summary>
    /// Registers an <see cref="IServiceProvider"/> for injecting into <c>[KernelFunction]</c> parameters
    /// and optional resolution helpers.
    /// </summary>
    IKernelBuilder UseServiceProvider(IServiceProvider services);

    /// <summary>Configure default <see cref="KernelInvokeOptions"/> applied when a call omits options.</summary>
    IKernelBuilder ConfigureDefaults(Action<KernelInvokeOptions> configure);

    IKernelBuilder AddPlugin<TPlugin>() where TPlugin : class, IKernelPluginRegistration, new();

    IKernelBuilder AddPlugin<TPlugin>(TPlugin instance) where TPlugin : class, IKernelPluginRegistration;

    IKernelBuilder AddFilter<TFilter>() where TFilter : class, IKernelFilter, new();

    IKernelBuilder AddFilter(IKernelFilter filter);

    void AddFunction(KernelFunctionDescriptor descriptor);

    void AddPrompt(PromptDefinition prompt);

    void AddProperty(KernelPropertyDescriptor property);

    Kernel Build();
}
