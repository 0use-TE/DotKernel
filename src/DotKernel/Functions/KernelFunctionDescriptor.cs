namespace DotKernel;

public delegate ValueTask<object?> KernelFunctionInvoker(
    FunctionInvocationContext context,
    CancellationToken cancellationToken);

public sealed class KernelFunctionDescriptor
{
    public required string PluginName { get; init; }

    public required string FunctionName { get; init; }

    /// <summary>Human-readable name for filters and logs.</summary>
    public string FullName => $"{PluginName}.{FunctionName}";

    /// <summary>API-safe tool name (OpenAI / DeepSeek pattern).</summary>
    public string ToolName => $"{PluginName}_{FunctionName}";

    public string? Description { get; init; }

    public string ParametersSchemaJson { get; init; } = """{"type":"object","properties":{}}""";

    public KernelFunctionInvoker Invoker { get; init; } = static (_, _) => default;

    public Type? DeclaringType { get; init; }
}
