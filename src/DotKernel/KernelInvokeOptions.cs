using Microsoft.Extensions.AI;

namespace DotKernel;

/// <summary>Per-call (or default) options for kernel invoke and streaming APIs.</summary>
public sealed class KernelInvokeOptions
{
    /// <summary>Default options: 16 tool rounds, system prompts and property context enabled.</summary>
    public static KernelInvokeOptions Default { get; } = new();

    /// <summary>
    /// Merged into the model request. Kernel always sets <see cref="ChatOptions.Tools"/> from registered functions.
    /// Use this for Temperature, MaxOutputTokens, ModelId, etc.
    /// </summary>
    public ChatOptions? ChatOptions { get; set; }

    /// <summary>Maximum model→tool→model rounds before failing. Default 16. Must be &gt;= 1.</summary>
    public int MaxToolCallRounds { get; set; } = 16;

    /// <summary>
    /// When true, registered <c>[KernelPrompt]</c> templates with <see cref="PromptRole.System"/>
    /// are injected (temporarily) before each model call. Does not mutate <see cref="ChatHistory"/>.
    /// </summary>
    public bool IncludeSystemPrompts { get; set; } = true;

    /// <summary>
    /// When true, <c>[KernelProperty]</c> live context is injected before each model call.
    /// </summary>
    public bool IncludePropertyContext { get; set; } = true;

    /// <summary>Create a shallow copy (ChatOptions reference is shared).</summary>
    public KernelInvokeOptions Clone() => new()
    {
        ChatOptions = ChatOptions,
        MaxToolCallRounds = MaxToolCallRounds,
        IncludeSystemPrompts = IncludeSystemPrompts,
        IncludePropertyContext = IncludePropertyContext,
    };
}
