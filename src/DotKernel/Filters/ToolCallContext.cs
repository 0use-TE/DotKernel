namespace DotKernel;

public sealed class ToolCallContext
{
    public required string ToolCallId { get; init; }

    public required string PluginName { get; init; }

    public required string FunctionName { get; init; }

    public string FullName => $"{PluginName}.{FunctionName}";

    public required IReadOnlyDictionary<string, object?> Arguments { get; init; }

    public string? RawArgumentsJson { get; init; }

    public required KernelFunctionDescriptor Descriptor { get; init; }

    public ChatHistory History { get; init; } = new();

    public string? CustomResult { get; set; }

    public ToolCallContext WithArguments(IReadOnlyDictionary<string, object?> arguments) =>
        new()
        {
            ToolCallId = ToolCallId,
            PluginName = PluginName,
            FunctionName = FunctionName,
            Arguments = arguments,
            RawArgumentsJson = RawArgumentsJson,
            Descriptor = Descriptor,
            History = History,
            CustomResult = CustomResult,
        };
}
