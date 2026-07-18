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
    public async Task InvokeAsync_runs_sync_tool_without_cancellation_token()
    {
        var mock = new MockChatClient();
        mock.Enqueue(new ChatResponse([
            new ChatMessage(ChatRole.Assistant, [
                new FunctionCallContent("call-1", "SimpleWeather_get_weather", new Dictionary<string, object?>
                {
                    ["city"] = "西雅图",
                }),
            ]),
        ]));
        mock.Enqueue(new ChatResponse(new ChatMessage(ChatRole.Assistant, "西雅图晴天。")));

        var capturing = new ToolResultCapturingClient(mock);
        var kernel = KernelBuilder.Create()
            .AddChatClient(capturing)
            .AddPlugin(new SimpleWeatherPlugin())
            .Build();

        var result = await kernel.InvokeAsync("西雅图天气怎么样？");

        Assert.Equal("西雅图晴天。", result);
        Assert.Equal("西雅图 晴天", Assert.Single(capturing.ToolResults));
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
    public void KernelProperty_is_registered_and_rendered_into_context()
    {
        var plugin = new WeatherPlugin { TemperatureUnit = "F" };
        var kernel = KernelBuilder.Create()
            .AddChatClient(new MockChatClient())
            .AddPlugin(plugin)
            .Build();

        var map = kernel.GetPropertyContext();
        Assert.Equal("F", map["Weather.temperature_unit"]);

        var rendered = kernel.RenderPropertyContext();
        Assert.Contains("Weather.temperature_unit", rendered, StringComparison.Ordinal);
        Assert.Contains("F", rendered, StringComparison.Ordinal);
        Assert.Contains("Unit used when reporting weather", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvokeAsync_injects_kernel_property_context_into_request()
    {
        var mock = new MockChatClient();
        mock.Enqueue(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));

        var plugin = new WeatherPlugin { TemperatureUnit = "F" };
        var kernel = KernelBuilder.Create()
            .AddChatClient(mock)
            .AddPlugin(plugin)
            .Build();

        await kernel.InvokeAsync("hi");

        var first = Assert.Single(mock.LastRequestMessages!, m => m.Role == ChatRole.System
            && m.Text is not null
            && m.Text.Contains("Live context", StringComparison.Ordinal));
        Assert.Contains("temperature_unit", first.Text!, StringComparison.Ordinal);
        Assert.Contains(": F", first.Text!, StringComparison.Ordinal);
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
