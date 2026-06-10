using DotKernel;

namespace DotKernel.AvaExample.Filters;

public sealed class ToolCallHistoryFilter : IKernelFilter
{
    public event Action<ToolCallContext>? Recorded;

    public ValueTask<ToolCallFilterResult> OnToolCallAsync(
        ToolCallContext context,
        ToolCallFilterDelegate next,
        CancellationToken cancellationToken)
    {
        Recorded?.Invoke(context);
        return next(context, cancellationToken);
    }
}
