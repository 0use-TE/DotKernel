using Microsoft.Extensions.AI;

namespace DotKernel;

public sealed class KernelBuilder : IKernelBuilder
{
    private IChatClient? _chatClient;
    private IServiceProvider? _services;
    private readonly KernelInvokeOptions _defaults = new();
    private readonly List<KernelFunctionDescriptor> _functions = [];
    private readonly Dictionary<string, object?> _pluginInstances = new(StringComparer.Ordinal);
    private readonly List<PromptDefinition> _prompts = [];
    private readonly List<KernelPropertyDescriptor> _properties = [];
    private readonly List<IKernelFilter> _filters = [];

    public static IKernelBuilder Create() => new KernelBuilder();

    public IKernelBuilder AddChatClient(IChatClient chatClient)
    {
        _chatClient = chatClient;
        return this;
    }

    public IKernelBuilder UseServiceProvider(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        return this;
    }

    public IKernelBuilder ConfigureDefaults(Action<KernelInvokeOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_defaults);
        return this;
    }

    public IKernelBuilder AddPlugin<TPlugin>() where TPlugin : class, IKernelPluginRegistration, new()
    {
        var instance = new TPlugin();
        TPlugin.Register(this);
        _pluginInstances[typeof(TPlugin).FullName ?? typeof(TPlugin).Name] = instance;
        return this;
    }

    public IKernelBuilder AddPlugin<TPlugin>(TPlugin instance) where TPlugin : class, IKernelPluginRegistration
    {
        TPlugin.Register(this);
        _pluginInstances[typeof(TPlugin).FullName ?? typeof(TPlugin).Name] = instance;
        return this;
    }

    public IKernelBuilder AddFilter<TFilter>() where TFilter : class, IKernelFilter, new()
    {
        _filters.Add(new TFilter());
        return this;
    }

    public IKernelBuilder AddFilter(IKernelFilter filter)
    {
        _filters.Add(filter);
        return this;
    }

    public void AddFunction(KernelFunctionDescriptor descriptor) => _functions.Add(descriptor);

    public void AddPrompt(PromptDefinition prompt) => _prompts.Add(prompt);

    public void AddProperty(KernelPropertyDescriptor property) => _properties.Add(property);

    public Kernel Build()
    {
        if (_chatClient is null)
        {
            throw new KernelException("A chat client must be configured via AddChatClient.");
        }

        if (_defaults.MaxToolCallRounds < 1)
        {
            throw new KernelException("MaxToolCallRounds must be at least 1.");
        }

        return new Kernel(
            _chatClient,
            _functions,
            _prompts,
            _properties,
            _pluginInstances,
            _filters,
            _services,
            _defaults.Clone());
    }
}
