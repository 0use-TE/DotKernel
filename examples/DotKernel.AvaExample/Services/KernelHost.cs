using DotKernel;

namespace DotKernel.AvaExample.Services;

public sealed class KernelHost
{
    private const string SystemPrompt = """
        You are a smart manufacturing line scheduling AI. Control the digital twin via tools.
        Three zones: assembly, storage, shipping.

        Workflow:
        1. For complex tasks, call Twin.get_snapshot first
        2. Combine tools as needed (lights, conveyor, AGV, robot, inventory, alerts)
        3. Multi-turn tool calls until the user goal is met
        4. The twin panel on the right updates after each execution

        Reply in English Markdown (headings, lists, tables, bold, code blocks). Summarize actions and line state concisely.
        Do not use GitHub Alert syntax (e.g. > [!NOTE], > [!TIP]) or emoji list markers.
        """;

    private ChatHistory _history = new();

    public KernelHost(Kernel kernel)
    {
        Kernel = kernel;
        ResetConversation();
    }

    public Kernel Kernel { get; }

    public IAsyncEnumerable<KernelStreamingUpdate> StreamAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        return Kernel.InvokeStreamingAsync(input, _history, cancellationToken);
    }

    public async Task<string> SendAsync(string input, CancellationToken cancellationToken = default)
    {
        return await Kernel.InvokeAsync(input, _history, cancellationToken).ConfigureAwait(false);
    }

    public void ResetConversation()
    {
        _history = new ChatHistory();
        _history.AddSystemMessage(SystemPrompt);
    }
}
