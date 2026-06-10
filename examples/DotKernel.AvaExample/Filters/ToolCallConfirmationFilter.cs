using DotKernel;

namespace DotKernel.AvaExample.Filters;

public sealed class ToolCallConfirmationFilter : IKernelFilter
{
    private readonly object _gate = new();
    private volatile bool _autoApprove = true;
    private TaskCompletionSource<bool>? _pending;

    public bool AutoApproveTools
    {
        get => _autoApprove;
        set
        {
            _autoApprove = value;
            if (value)
            {
                ApprovePending();
            }
        }
    }

    public ToolCallContext? PendingContext { get; private set; }

    public event Action<ToolCallContext>? ConfirmationRequired;

    public void ApprovePending()
    {
        lock (_gate)
        {
            _pending?.TrySetResult(true);
        }
    }

    public void DenyPending()
    {
        lock (_gate)
        {
            _pending?.TrySetResult(false);
        }
    }

    public async ValueTask<ToolCallFilterResult> OnToolCallAsync(
        ToolCallContext context,
        ToolCallFilterDelegate next,
        CancellationToken cancellationToken)
    {
        if (AutoApproveTools)
        {
            return await next(context, cancellationToken).ConfigureAwait(false);
        }

        TaskCompletionSource<bool> tcs;
        lock (_gate)
        {
            tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending = tcs;
            PendingContext = context;
        }

        ConfirmationRequired?.Invoke(context);

        bool approved;
        try
        {
            approved = await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            lock (_gate)
            {
                _pending = null;
                PendingContext = null;
            }
        }

        if (!approved)
        {
            context.CustomResult = "用户已拒绝执行此工具。";
            return new ToolCallFilterResult
            {
                Action = FilterAction.Skip,
                ModifiedContext = context,
            };
        }

        return await next(context, cancellationToken).ConfigureAwait(false);
    }
}
