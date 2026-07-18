namespace DotKernel;

public sealed class KernelPropertyDescriptor
{
    public required string PluginName { get; init; }

    public required string PropertyName { get; init; }

    public string FullName => $"{PluginName}.{PropertyName}";

    public string? Description { get; init; }

    public Type? DeclaringType { get; init; }

    public Func<object?, object?> Getter { get; init; } = static _ => null;
}
