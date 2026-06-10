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
                    ["message"] = "AGV 到位，装配线已启动",
                    ["severity"] = "info",
                }),
            ]),
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, """
                ## 产线调度完成

                | 步骤 | 结果 |
                |------|------|
                | 装配区照明 | 已开启 |
                | 传送带 | 72% 运行中 |
                | AGV | 仓储区 → 装配区 |
                | 机器人 | 装配任务 |
                | 库存 | 仓储区 -8 |

                右侧**数字孪生面板**已同步更新。
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
