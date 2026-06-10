namespace DotKernel.Tests;

public class GlobalPluginTests
{
    [Fact]
    public async Task Async_plugin_returns_unwrapped_result_not_task_type_name()
    {
        var mock = new Mocks.MockChatClient();
        mock.Enqueue(new Microsoft.Extensions.AI.ChatResponse([
            new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.Assistant, [
                new Microsoft.Extensions.AI.FunctionCallContent("call-1", "Global_ping", new Dictionary<string, object?>()),
            ]),
        ]));
        mock.Enqueue(new Microsoft.Extensions.AI.ChatResponse(
            new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.Assistant, "ok")));

        var capturing = new ToolResultCapturingClient(mock);
        var kernel = KernelBuilder.Create()
            .AddChatClient(capturing)
            .AddPlugin<GlobalNamespacePlugin>()
            .Build();

        await kernel.InvokeAsync("ping");

        Assert.Equal("pong", Assert.Single(capturing.ToolResults));
    }
}

internal sealed class ToolResultCapturingClient : Microsoft.Extensions.AI.IChatClient
{
    private readonly Mocks.MockChatClient _inner;

    public ToolResultCapturingClient(Mocks.MockChatClient inner) => _inner = inner;

    public List<string> ToolResults { get; } = [];

    public async Task<Microsoft.Extensions.AI.ChatResponse> GetResponseAsync(
        IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
        Microsoft.Extensions.AI.ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var message in messages)
        {
            foreach (var content in message.Contents)
            {
                if (content is Microsoft.Extensions.AI.FunctionResultContent result)
                {
                    ToolResults.Add(result.Result?.ToString() ?? string.Empty);
                }
            }
        }

        return await _inner.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<Microsoft.Extensions.AI.ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
        Microsoft.Extensions.AI.ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
        foreach (var update in response.ToChatResponseUpdates())
        {
            yield return update;
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}

internal sealed class CapturingChatClient(Mocks.MockChatClient inner, List<string> capturedResults) : Microsoft.Extensions.AI.IChatClient
{
    public async Task<Microsoft.Extensions.AI.ChatResponse> GetResponseAsync(
        IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
        Microsoft.Extensions.AI.ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var message in messages)
        {
            foreach (var content in message.Contents)
            {
                if (content is Microsoft.Extensions.AI.FunctionResultContent result)
                {
                    capturedResults.Add(result.Result?.ToString() ?? string.Empty);
                }
            }
        }

        return await inner.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<Microsoft.Extensions.AI.ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
        Microsoft.Extensions.AI.ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
        foreach (var update in response.ToChatResponseUpdates())
        {
            yield return update;
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
