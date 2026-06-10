namespace DotKernel;

public enum KernelStreamingUpdateKind
{
    TextDelta,
    ToolCallStarted,
    ToolCallCompleted,
    Completed,
}

public readonly record struct KernelStreamingUpdate(
    KernelStreamingUpdateKind Kind,
    string? TextDelta = null,
    string? ToolName = null,
    string? ToolCallId = null,
    string? ToolResult = null,
    string? FinalText = null)
{
    public static KernelStreamingUpdate Delta(string text) =>
        new(KernelStreamingUpdateKind.TextDelta, TextDelta: text);

    public static KernelStreamingUpdate ToolStarted(string toolName, string? toolCallId) =>
        new(KernelStreamingUpdateKind.ToolCallStarted, ToolName: toolName, ToolCallId: toolCallId);

    public static KernelStreamingUpdate ToolCompleted(string toolName, string? toolCallId, string result) =>
        new(KernelStreamingUpdateKind.ToolCallCompleted, ToolName: toolName, ToolCallId: toolCallId, ToolResult: result);

    public static KernelStreamingUpdate Done(string finalText) =>
        new(KernelStreamingUpdateKind.Completed, FinalText: finalText);
}
