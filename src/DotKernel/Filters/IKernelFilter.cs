namespace DotKernel;

public delegate ValueTask<ToolCallFilterResult> ToolCallFilterDelegate(
    ToolCallContext context,
    CancellationToken cancellationToken);

public interface IKernelFilter
{
    ValueTask<ToolCallFilterResult> OnToolCallAsync(
        ToolCallContext context,
        ToolCallFilterDelegate next,
        CancellationToken cancellationToken);
}
