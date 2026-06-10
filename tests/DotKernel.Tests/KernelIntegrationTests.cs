using DotKernel.Tests.Mocks;
using DotKernel.Tests.Plugins;
using Microsoft.Extensions.AI;

namespace DotKernel.Tests;

public class KernelIntegrationTests
{
    [Fact]
    public void Builder_registers_generated_plugin_and_prompt()
    {
        var builder = KernelBuilder.Create();
        builder.AddPlugin<WeatherPlugin>();
        builder.AddPlugin<EmailPrompt>();
        builder.AddChatClient(new MockChatClient());

        var kernel = builder.Build();

        var rendered = kernel.RenderPrompt<EmailPrompt>("compose", new Dictionary<string, string?>
        {
            ["input"] = "汇报项目进度",
        });

        Assert.Contains("张总", rendered, StringComparison.Ordinal);
        Assert.Contains("汇报项目进度", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvokeAsync_runs_tool_call_through_filter()
    {
        var mock = new MockChatClient();
        mock.Enqueue(new ChatResponse([
            new ChatMessage(ChatRole.Assistant, [
                new FunctionCallContent("call-1", "Weather_get_weather", new Dictionary<string, object?>
                {
                    ["city"] = "北京",
                }),
            ]),
        ]));
        mock.Enqueue(new ChatResponse(new ChatMessage(ChatRole.Assistant, "北京今天天气不错。")));

        var filter = new RecordingFilter();
        var capturing = new ToolResultCapturingClient(mock);
        var kernel = KernelBuilder.Create()
            .AddChatClient(capturing)
            .AddPlugin<WeatherPlugin>()
            .AddFilter(filter)
            .Build();

        var result = await kernel.InvokeAsync("北京天气怎么样？");

        Assert.Equal("北京今天天气不错。", result);
        Assert.Single(filter.Calls);
        Assert.Equal("Weather.get_weather", filter.Calls[0].FullName);
        Assert.Equal("北京", filter.Calls[0].Arguments["city"]);
        Assert.Equal("北京：晴，25°C", Assert.Single(capturing.ToolResults));
    }

    [Fact]
    public async Task InvokeStreamingAsync_yields_text_deltas_and_completes()
    {
        var mock = new MockChatClient();
        mock.Enqueue(new ChatResponse(new ChatMessage(ChatRole.Assistant, "你好，世界。")));

        var kernel = KernelBuilder.Create()
            .AddChatClient(mock)
            .AddPlugin<WeatherPlugin>()
            .Build();

        var updates = new List<KernelStreamingUpdate>();
        await foreach (var update in kernel.InvokeStreamingAsync("hello"))
        {
            updates.Add(update);
        }

        Assert.Contains(updates, u => u.Kind == KernelStreamingUpdateKind.TextDelta);
        Assert.Equal(KernelStreamingUpdateKind.Completed, updates[^1].Kind);
        Assert.Equal("你好，世界。", updates[^1].FinalText);
    }

    [Fact]
    public async Task InvokeStreamingAsync_runs_tool_call_through_filter()
    {
        var mock = new MockChatClient();
        mock.Enqueue(new ChatResponse([
            new ChatMessage(ChatRole.Assistant, [
                new FunctionCallContent("call-1", "Weather_get_weather", new Dictionary<string, object?>
                {
                    ["city"] = "北京",
                }),
            ]),
        ]));
        mock.Enqueue(new ChatResponse(new ChatMessage(ChatRole.Assistant, "北京今天天气不错。")));

        var filter = new RecordingFilter();
        var capturing = new ToolResultCapturingClient(mock);
        var kernel = KernelBuilder.Create()
            .AddChatClient(capturing)
            .AddPlugin<WeatherPlugin>()
            .AddFilter(filter)
            .Build();

        var updates = new List<KernelStreamingUpdate>();
        await foreach (var update in kernel.InvokeStreamingAsync("北京天气怎么样？"))
        {
            updates.Add(update);
        }

        Assert.Contains(updates, u => u.Kind == KernelStreamingUpdateKind.ToolCallStarted);
        Assert.Contains(updates, u => u.Kind == KernelStreamingUpdateKind.ToolCallCompleted);
        Assert.Equal("北京今天天气不错。", updates[^1].FinalText);
        Assert.Single(filter.Calls);
        Assert.Equal("北京：晴，25°C", Assert.Single(capturing.ToolResults));
    }

    [Fact]
    public async Task Filter_can_skip_tool_call()
    {
        var mock = new MockChatClient();
        mock.Enqueue(new ChatResponse([
            new ChatMessage(ChatRole.Assistant, [
                new FunctionCallContent("call-1", "Weather_get_weather", new Dictionary<string, object?>
                {
                    ["city"] = "上海",
                }),
            ]),
        ]));
        mock.Enqueue(new ChatResponse(new ChatMessage(ChatRole.Assistant, "已处理。")));

        var kernel = KernelBuilder.Create()
            .AddChatClient(mock)
            .AddPlugin<WeatherPlugin>()
            .AddFilter(new SkipFilter())
            .Build();

        var result = await kernel.InvokeAsync("上海天气");

        Assert.Equal("已处理。", result);
    }
}

internal sealed class SkipFilter : IKernelFilter
{
    public ValueTask<ToolCallFilterResult> OnToolCallAsync(
        ToolCallContext context,
        ToolCallFilterDelegate next,
        CancellationToken cancellationToken)
    {
        context.CustomResult = "已跳过工具调用。";
        return ValueTask.FromResult(new ToolCallFilterResult
        {
            Action = FilterAction.Skip,
            ModifiedContext = context,
        });
    }
}
