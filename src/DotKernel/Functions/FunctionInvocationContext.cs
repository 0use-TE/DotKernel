namespace DotKernel;

public sealed class FunctionInvocationContext
{
    public required KernelFunctionDescriptor Descriptor { get; init; }

    public required IReadOnlyDictionary<string, object?> Arguments { get; init; }

    public object? PluginInstance { get; init; }

    public Kernel? Kernel { get; init; }

    public bool HasArgument(string name) =>
        Arguments.TryGetValue(name, out var value) && value is not null;

    public T GetArgument<T>(string name)
    {
        if (!Arguments.TryGetValue(name, out var value) || value is null)
        {
            throw new KernelException($"Missing required argument '{name}' for '{Descriptor.FullName}'.");
        }

        return ArgumentConverter.ConvertTo<T>(value, name, Descriptor.FullName);
    }

    public T GetArgument<T>(string name, T defaultValue)
    {
        if (!Arguments.TryGetValue(name, out var value) || value is null)
        {
            return defaultValue;
        }

        return ArgumentConverter.ConvertTo<T>(value, name, Descriptor.FullName);
    }

    public T GetPlugin<T>() where T : class
    {
        if (PluginInstance is T plugin)
        {
            return plugin;
        }

        throw new KernelException($"Plugin instance is not of type {typeof(T).Name}.");
    }
}
