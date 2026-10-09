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

    /// <summary>Called before each model request. Default: no-op.</summary>
    ValueTask OnBeforeModelCallAsync(ModelCallContext context, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    /// <summary>Called after each model response (or streaming turn). Default: no-op.</summary>
    ValueTask OnAfterModelCallAsync(ModelCallContext context, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}
