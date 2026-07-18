namespace DotKernel.Tests.Plugins;

[KernelPlugin("Weather")]
public partial class WeatherPlugin
{
    [KernelProperty("temperature_unit", "Unit used when reporting weather (C or F)")]
    public string TemperatureUnit { get; set; } = "C";

    [KernelPrompt("system", Role = PromptRole.System)]
    public string SystemPrompt => "你是天气助手，回答简洁。";

    [KernelFunction("get_weather")]
    [KernelDescription("获取指定城市的天气")]
    public Task<string> GetWeatherAsync(
        [KernelDescription("城市名称")] string city,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"{city}：晴，25°{TemperatureUnit}");
    }
}

[KernelPromptClass("Email")]
public partial class EmailPrompt
{
    [PromptVariable(Description = "收件人")]
    public string Recipient { get; set; } = "张总";

    [PromptVariable(Description = "语气", Default = "正式")]
    public string Tone { get; set; } = "正式";

    [KernelPrompt("compose", Description = "撰写邮件")]
    public string ComposeTemplate => """
        请用{{$Tone}}语气给{{$Recipient}}写一封邮件。
        要求：{{$input}}
        """;
}

public sealed class RecordingFilter : IKernelFilter
{
    public List<ToolCallContext> Calls { get; } = [];

    public async ValueTask<ToolCallFilterResult> OnToolCallAsync(
        ToolCallContext context,
        ToolCallFilterDelegate next,
        CancellationToken cancellationToken)
    {
        Calls.Add(context);
        return await next(context, cancellationToken).ConfigureAwait(false);
    }
}
