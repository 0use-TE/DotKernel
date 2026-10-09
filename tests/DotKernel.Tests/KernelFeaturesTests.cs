using System.Text.Json;
using DotKernel.Tests.Mocks;
using DotKernel.Tests.Plugins;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace DotKernel.Tests;

public class KernelFeaturesTests
{
    [Fact]
    public async Task InvokeFunctionAsync_calls_tool_without_model()
    {
        var kernel = KernelBuilder.Create()
            .AddChatClient(new MockChatClient())
            .AddPlugin(new WeatherPlugin { TemperatureUnit = "C" })
            .Build();

        var result = await kernel.InvokeFunctionAsync(
            "Weather.get_weather",
            new Dictionary<string, object?> { ["city"] = "杭州" });

        Assert.Equal("杭州：晴，25°C", result);
    }

    [Fact]
    public async Task InvokeFunctionAsync_accepts_tool_name()
    {
        var kernel = KernelBuilder.Create()
            .AddChatClient(new MockChatClient())
            .AddPlugin(new SimpleWeatherPlugin())
            .Build();

        var result = await kernel.InvokeFunctionAsync(
            "SimpleWeather_get_weather",
            new Dictionary<string, object?> { ["city"] = "南京" });

        Assert.Equal("南京 晴天", result);
    }

    [Fact]
    public async Task InvokeAsync_injects_registered_system_prompts()
    {
        var mock = new MockChatClient();
        mock.Enqueue(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));

        var kernel = KernelBuilder.Create()
            .AddChatClient(mock)
            .AddPlugin(new WeatherPlugin())
            .Build();

        await kernel.InvokeAsync("hi");

        Assert.Contains(
            mock.LastRequestMessages!,
            m => m.Role == ChatRole.System && m.Text!.Contains("天气助手", StringComparison.Ordinal));
    }

    [Fact]
    public async Task InvokeAsync_can_disable_system_prompt_injection()
    {
        var mock = new MockChatClient();
        mock.Enqueue(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));

        var kernel = KernelBuilder.Create()
            .AddChatClient(mock)
            .AddPlugin(new WeatherPlugin())
            .Build();

        await kernel.InvokeAsync("hi", history: null, new KernelInvokeOptions
        {
            IncludeSystemPrompts = false,
            IncludePropertyContext = false,
        });

        Assert.DoesNotContain(
            mock.LastRequestMessages!,
            m => m.Role == ChatRole.System);
    }

    [Fact]
    public async Task InvokeAsync_passes_chat_options_temperature()
    {
        var mock = new MockChatClient();
        mock.Enqueue(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));

        var kernel = KernelBuilder.Create()
            .AddChatClient(mock)
            .AddPlugin(new WeatherPlugin())
            .Build();

        await kernel.InvokeAsync("hi", history: null, new KernelInvokeOptions
        {
            ChatOptions = new ChatOptions { Temperature = 0.2f, MaxOutputTokens = 128 },
        });

        Assert.NotNull(mock.LastRequestOptions);
        Assert.Equal(0.2f, mock.LastRequestOptions!.Temperature);
        Assert.Equal(128, mock.LastRequestOptions.MaxOutputTokens);
        Assert.NotNull(mock.LastRequestOptions.Tools);
        Assert.NotEmpty(mock.LastRequestOptions.Tools!);
    }

    [Fact]
    public async Task InvokeAsync_throws_when_max_tool_rounds_exceeded()
    {
        var mock = new MockChatClient();
        mock.Enqueue(new ChatResponse([
            new ChatMessage(ChatRole.Assistant, [
                new FunctionCallContent("c1", "Weather_get_weather", new Dictionary<string, object?>
                {
                    ["city"] = "北京",
                }),
            ]),
        ]));
        mock.Enqueue(new ChatResponse([
            new ChatMessage(ChatRole.Assistant, [
                new FunctionCallContent("c2", "Weather_get_weather", new Dictionary<string, object?>
                {
                    ["city"] = "北京",
                }),
            ]),
        ]));

        var kernel = KernelBuilder.Create()
            .AddChatClient(mock)
            .AddPlugin(new WeatherPlugin())
            .ConfigureDefaults(o => o.MaxToolCallRounds = 1)
            .Build();

        await Assert.ThrowsAsync<MaxToolCallRoundsExceededException>(() => kernel.InvokeAsync("loop"));
    }

    [Fact]
    public async Task UseServiceProvider_injects_into_kernel_function()
    {
        var services = new ServiceCollection()
            .AddSingleton(new GreetingService("你好"))
            .BuildServiceProvider();

        var mock = new MockChatClient();
        mock.Enqueue(new ChatResponse([
            new ChatMessage(ChatRole.Assistant, [
                new FunctionCallContent("c1", "Di_greet", new Dictionary<string, object?>
                {
                    ["name"] = "Dot",
                }),
            ]),
        ]));
        mock.Enqueue(new ChatResponse(new ChatMessage(ChatRole.Assistant, "done")));

        var capturing = new ToolResultCapturingClient(mock);
        var kernel = KernelBuilder.Create()
            .AddChatClient(capturing)
            .UseServiceProvider(services)
            .AddPlugin(new DiPlugin())
            .Build();

        await kernel.InvokeAsync("greet");

        Assert.Equal("你好, Dot", Assert.Single(capturing.ToolResults));
    }

    [Fact]
    public void Introspection_exposes_functions_prompts_and_properties()
    {
        var kernel = KernelBuilder.Create()
            .AddChatClient(new MockChatClient())
            .AddPlugin(new WeatherPlugin())
            .AddPlugin(new EmailPrompt())
            .Build();

        Assert.Contains(kernel.Functions, f => f.FullName == "Weather.get_weather");
        Assert.Contains(kernel.Prompts, p => p.FullName == "Weather.system");
        Assert.Contains(kernel.Prompts, p => p.FullName == "Email.compose");
        Assert.Contains(kernel.Properties, p => p.FullName == "Weather.temperature_unit");
    }

    [Fact]
    public async Task Model_filters_run_before_and_after_call()
    {
        var mock = new MockChatClient();
        mock.Enqueue(new ChatResponse(new ChatMessage(ChatRole.Assistant, "reply")));
        var filter = new ModelHookFilter();

        var kernel = KernelBuilder.Create()
            .AddChatClient(mock)
            .AddPlugin(new WeatherPlugin())
            .AddFilter(filter)
            .Build();

        await kernel.InvokeAsync("hi");

        Assert.Equal(1, filter.BeforeCount);
        Assert.Equal(1, filter.AfterCount);
        Assert.NotNull(filter.LastResponse);
    }

    [Fact]
    public async Task Argument_binding_supports_int_enum_and_object_json()
    {
        var result = await KernelBuilder.Create()
            .AddChatClient(new MockChatClient())
            .AddPlugin(new TypedArgsPlugin())
            .Build()
            .InvokeFunctionAsync("Typed.echo", new Dictionary<string, object?>
            {
                ["count"] = JsonSerializer.SerializeToElement(3),
                ["level"] = "Warning",
                ["payload"] = JsonSerializer.SerializeToElement(new { City = "上海" }),
            });

        Assert.Equal("3|Warning|上海", result);
    }

    [Fact]
    public async Task Object_return_value_is_json_serialized()
    {
        var result = await KernelBuilder.Create()
            .AddChatClient(new MockChatClient())
            .AddPlugin(new TypedArgsPlugin())
            .Build()
            .InvokeFunctionAsync("Typed.make_info", new Dictionary<string, object?>
            {
                ["city"] = "深圳",
            });

        Assert.Contains("\"city\":\"深圳\"", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"temp\":26", result, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class GreetingService(string prefix)
{
    public string Prefix { get; } = prefix;
}

[KernelPlugin("Di")]
public partial class DiPlugin
{
    [KernelFunction("greet")]
    public string Greet(string name, IServiceProvider services)
    {
        var greeting = services.GetRequiredService<GreetingService>();
        return $"{greeting.Prefix}, {name}";
    }
}

public enum AlertLevel
{
    Info,
    Warning,
    Error,
}

public sealed class WeatherInfo
{
    public string City { get; set; } = "";
    public int Temp { get; set; }
}

[KernelPlugin("Typed")]
public partial class TypedArgsPlugin
{
    [KernelFunction("echo")]
    public string Echo(int count, AlertLevel level, WeatherInfo payload) =>
        $"{count}|{level}|{payload.City}";

    [KernelFunction("make_info")]
    public WeatherInfo MakeInfo(string city) => new() { City = city, Temp = 26 };
}

internal sealed class ModelHookFilter : IKernelFilter
{
    public int BeforeCount { get; private set; }
    public int AfterCount { get; private set; }
    public ChatResponse? LastResponse { get; private set; }

    public ValueTask<ToolCallFilterResult> OnToolCallAsync(
        ToolCallContext context,
        ToolCallFilterDelegate next,
        CancellationToken cancellationToken) =>
        next(context, cancellationToken);

    public ValueTask OnBeforeModelCallAsync(ModelCallContext context, CancellationToken cancellationToken)
    {
        BeforeCount++;
        return ValueTask.CompletedTask;
    }

    public ValueTask OnAfterModelCallAsync(ModelCallContext context, CancellationToken cancellationToken)
    {
        AfterCount++;
        LastResponse = context.Response;
        return ValueTask.CompletedTask;
    }
}
