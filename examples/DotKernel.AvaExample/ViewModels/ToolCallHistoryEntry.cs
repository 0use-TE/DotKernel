using CommunityToolkit.Mvvm.ComponentModel;

namespace DotKernel.AvaExample.ViewModels;

public partial class ToolCallHistoryEntry : ObservableObject
{
    public ToolCallHistoryEntry(string toolName, string? argumentsJson, string? toolCallId)
    {
        ToolName = toolName;
        ArgumentsJson = string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson;
        ToolCallId = toolCallId ?? string.Empty;
        Timestamp = DateTime.Now;
    }

    public string ToolName { get; }

    public string ArgumentsJson { get; }

    public string ToolCallId { get; }

    public DateTime Timestamp { get; }

    public string TimeLabel => Timestamp.ToString("HH:mm:ss");

    [ObservableProperty]
    private string? _result;

    [ObservableProperty]
    private string _status = "Running";

    public void MarkAwaitingConfirmation()
    {
        Status = "Awaiting";
    }

    public void MarkCompleted(string? result)
    {
        Result = result;
        Status = result?.Contains("denied", StringComparison.OrdinalIgnoreCase) == true ? "Denied" : "Done";
    }
}
