using Microsoft.Extensions.AI;

namespace DotKernel;

/// <summary>Context for before/after model-call filter hooks.</summary>
public sealed class ModelCallContext
{
    /// <summary>Persistent chat history (user/assistant/tool turns).</summary>
    public required ChatHistory History { get; init; }

    /// <summary>
    /// Messages about to be sent (or just sent) to the model, including temporary system injections.
    /// Filters may mutate this list in <see cref="IKernelFilter.OnBeforeModelCallAsync"/>.
    /// </summary>
    public required IList<ChatMessage> Messages { get; set; }

    /// <summary>Chat options for this call (Tools already applied). Filters may replace or mutate.</summary>
    public ChatOptions? ChatOptions { get; set; }

    /// <summary>Set after a non-streaming model response is received.</summary>
    public ChatResponse? Response { get; set; }

    /// <summary>Set after a streaming turn completes (final assistant text, if any).</summary>
    public string? StreamingFinalText { get; set; }
}
