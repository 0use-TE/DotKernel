using Microsoft.Extensions.AI;

namespace DotKernel.AvaExample.Infrastructure;

internal sealed class EchoChatClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var turn = ResolveTurn(messages);

        return Task.FromResult(turn switch
        {
            0 => ToolResponse([
                new FunctionCallContent("snap-1", "Twin_get_snapshot", new Dictionary<string, object?>()),
            ]),
            1 => ToolResponse([
                new FunctionCallContent("light-1", "Twin_set_light", new Dictionary<string, object?>
                {
                    ["zone"] = "assembly",
                    ["on"] = true,
                }),
                new FunctionCallContent("conv-1", "Twin_set_conveyor", new Dictionary<string, object?>
                {
                    ["zone"] = "assembly",
                    ["speed"] = 72,
                    ["running"] = true,
                }),
            ]),
            2 => ToolResponse([
                new FunctionCallContent("agv-1", "Twin_move_agv", new Dictionary<string, object?>
                {
                    ["from_zone"] = "storage",
                    ["to_zone"] = "assembly",
                }),
            ]),
            3 => ToolResponse([
                new FunctionCallContent("robot-1", "Twin_deploy_robot", new Dictionary<string, object?>
                {
                    ["zone"] = "assembly",
                    ["task"] = "assemble",
                }),
                new FunctionCallContent("inv-1", "Twin_adjust_inventory", new Dictionary<string, object?>
                {
                    ["zone"] = "storage",
                    ["delta"] = -8,
                }),
            ]),
            4 => ToolResponse([
                new FunctionCallContent("alert-1", "Twin_raise_alert", new Dictionary<string, object?>
                {
                    ["zone"] = "assembly",
                    ["message"] = "AGV arrived, assembly line started",
                    ["severity"] = "info",
                }),
            ]),
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, """
                ## Line scheduling complete

                | Step | Result |
                |------|--------|
                | Assembly lighting | On |
                | Conveyor | Running at 72% |
                | AGV | Storage → Assembly |
                | Robot | Assemble task |
                | Inventory | Storage -8 |

                The **digital twin panel** on the right is up to date.
                """)),
        });
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
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

    private static int ResolveTurn(IEnumerable<ChatMessage> messages)
    {
        var list = messages as IList<ChatMessage> ?? messages.ToList();
        var nonSystem = list.Count(m => m.Role != ChatRole.System);
        if (nonSystem <= 1)
        {
            return 0;
        }

        return list
            .SelectMany(m => m.Contents)
            .OfType<FunctionResultContent>()
            .Count();
    }

    private static ChatResponse ToolResponse(IReadOnlyList<FunctionCallContent> calls) =>
        new([new ChatMessage(ChatRole.Assistant, calls.Cast<AIContent>().ToList())]);
}
