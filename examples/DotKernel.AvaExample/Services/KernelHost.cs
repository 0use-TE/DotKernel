using DotKernel;

namespace DotKernel.AvaExample.Services;

public sealed class KernelHost
{
    private const string SystemPrompt = """
        你是智能制造产线调度 AI，通过工具控制数字孪生产线。
        三个区域：assembly(装配区)、storage(仓储区)、shipping(出货区)。

        工作方式：
        1. 复杂任务先调用 Twin.get_snapshot 了解现状
        2. 按需组合多个工具（照明、传送带、AGV、机器人、库存、告警）
        3. 可多轮调用，直到完成用户目标
        4. 每次执行后右侧孪生面板会实时反映效果

        回复使用 Markdown 格式（标题、列表、表格、加粗、代码块），用简洁中文说明已执行的操作与当前产线状态。
        不要使用 GitHub Alert 语法（如 > [!NOTE]、> [!TIP]），不要插入工具图标或 emoji 列表符号。
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
