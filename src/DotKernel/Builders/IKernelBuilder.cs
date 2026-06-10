using Microsoft.Extensions.AI;

namespace DotKernel;

public interface IKernelBuilder
{
    IKernelBuilder AddChatClient(IChatClient chatClient);

    IKernelBuilder AddPlugin<TPlugin>() where TPlugin : class, IKernelPluginRegistration, new();

    IKernelBuilder AddPlugin<TPlugin>(TPlugin instance) where TPlugin : class, IKernelPluginRegistration;

    IKernelBuilder AddFilter<TFilter>() where TFilter : class, IKernelFilter, new();

    IKernelBuilder AddFilter(IKernelFilter filter);

    void AddFunction(KernelFunctionDescriptor descriptor);

    void AddPrompt(PromptDefinition prompt);

    Kernel Build();
}
