using DotKernel;
using DotKernel.Example;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets(typeof(Program).Assembly, optional: true)
    .AddEnvironmentVariables(prefix: "DOTKERNEL_")
    .Build();

var filter = new ConsolePreviewFilter();
var twin = new FactoryTwinState();

var kernel = KernelBuilder.Create()
    .AddChatClient(DeepSeekChatClientFactory.Create(configuration))
    .AddPlugin(new FactoryTwinPlugin(twin))
    .AddPlugin<EmailPromptModel>()
    .AddFilter(filter)
    .Build();

Console.WriteLine("=== Prompt 渲染 ===");
var email = new EmailPromptModel
{
    Recipient = "李经理",
    Tone = "简洁",
};
var rendered = kernel.RenderPrompt("draft", new Dictionary<string, string?>
{
    ["input"] = "申请延期一周",
}, email);
Console.WriteLine(rendered);
Console.WriteLine();

Console.WriteLine("=== 产线数字孪生多轮调度 ===");
var history = new ChatHistory();
history.AddSystemMessage("""
    你是产线调度 AI。先 get_snapshot，再组合工具完成用户目标，可多轮调用。
    区域: assembly, storage, shipping。回复使用 Markdown 格式。
    """);

var answer = await kernel.InvokeAsync("巡检产线：开装配区灯和传送带，调度 AGV 到装配区，启动机器人装配。", history);
Console.WriteLine($"回答: {answer}");
Console.WriteLine();
Console.WriteLine("孪生快照:");
Console.WriteLine(twin.ToSnapshotJson());

internal static partial class Program;

[KernelPromptClass("Email")]
public partial class EmailPromptModel
{
    [PromptVariable(Description = "收件人")]
    public string Recipient { get; set; } = "";

    [PromptVariable(Default = "正式")]
    public string Tone { get; set; } = "正式";

    [KernelPrompt("draft")]
    public string DraftTemplate => "给{{$Recipient}}写一封{{$Tone}}邮件：{{$input}}";
}

public sealed class ConsolePreviewFilter : IKernelFilter
{
    public async ValueTask<ToolCallFilterResult> OnToolCallAsync(
        ToolCallContext context,
        ToolCallFilterDelegate next,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"[过滤器] AI 即将调用: {context.FullName}");
        Console.WriteLine($"[过滤器] 参数 JSON: {context.RawArgumentsJson ?? "(null)"}");

        var result = await next(context, cancellationToken).ConfigureAwait(false);
        if (result.Action == FilterAction.Continue)
        {
            Console.WriteLine($"[过滤器] 已执行 {context.FullName}");
        }

        return result;
    }
}
