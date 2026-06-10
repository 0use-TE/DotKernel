namespace DotKernel;

internal sealed class FilterPipeline(IReadOnlyList<IKernelFilter> filters)
{
    public async ValueTask<ToolCallFilterResult> ExecuteAsync(
        ToolCallContext context,
        CancellationToken cancellationToken)
    {
        ToolCallFilterDelegate pipeline = static (ctx, _) =>
            new ValueTask<ToolCallFilterResult>(new ToolCallFilterResult { Action = FilterAction.Continue, ModifiedContext = ctx });

        for (var i = filters.Count - 1; i >= 0; i--)
        {
            var filter = filters[i];
            var next = pipeline;
            pipeline = (ctx, ct) => filter.OnToolCallAsync(ctx, next, ct);
        }

        return await pipeline(context, cancellationToken).ConfigureAwait(false);
    }
}
