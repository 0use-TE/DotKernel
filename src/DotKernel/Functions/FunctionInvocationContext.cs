using System.Text.Json;

namespace DotKernel;

public sealed class FunctionInvocationContext
{
    public required KernelFunctionDescriptor Descriptor { get; init; }

    public required IReadOnlyDictionary<string, object?> Arguments { get; init; }

    public object? PluginInstance { get; init; }

    public Kernel? Kernel { get; init; }

    public T GetArgument<T>(string name)
    {
        if (!Arguments.TryGetValue(name, out var value) || value is null)
        {
            throw new KernelException($"Missing required argument '{name}' for '{Descriptor.FullName}'.");
        }

        if (value is T typed)
        {
            return typed;
        }

        if (value is string s && typeof(T) == typeof(string))
        {
            return (T)(object)s;
        }

        if (value is JsonElement json)
        {
            if (typeof(T) == typeof(string) && json.ValueKind == JsonValueKind.String)
            {
                return (T)(object)(json.GetString() ?? string.Empty);
            }

            if (typeof(T) == typeof(double) && json.ValueKind == JsonValueKind.Number)
            {
                return (T)(object)json.GetDouble();
            }

            if (typeof(T) == typeof(int) && json.ValueKind == JsonValueKind.Number)
            {
                return (T)(object)json.GetInt32();
            }

            if (typeof(T) == typeof(bool) && (json.ValueKind == JsonValueKind.True || json.ValueKind == JsonValueKind.False))
            {
                return (T)(object)json.GetBoolean();
            }
        }

        return (T)Convert.ChangeType(value, typeof(T));
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
