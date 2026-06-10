namespace DotKernel;

public sealed class ToolCallFilterResult
{
    public FilterAction Action { get; init; } = FilterAction.Continue;

    public ToolCallContext? ModifiedContext { get; init; }
}
