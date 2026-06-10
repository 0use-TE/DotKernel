using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotKernel;
using DotKernel.AvaExample.Filters;
using DotKernel.AvaExample.Services;
using DotKernel.AvaExample.Twin;

namespace DotKernel.AvaExample.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly KernelHost _host;
    private readonly BuildingTwinState _twin;
    private readonly Dictionary<string, ToolCallHistoryEntry> _pendingHistory = new(StringComparer.Ordinal);

    public MainViewModel(
        KernelHost host,
        BuildingTwinState twin,
        ToolCallHistoryFilter filter,
        string status)
    {
        _host = host;
        _twin = twin;
        Twin = twin;
        ConnectionStatus = status;

        filter.Recorded += context =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                var entry = new ToolCallHistoryEntry(
                    context.FullName,
                    context.RawArgumentsJson,
                    context.ToolCallId);
                ToolCallHistory.Insert(0, entry);

                if (!string.IsNullOrEmpty(context.ToolCallId))
                {
                    _pendingHistory[context.ToolCallId] = entry;
                }

                PendingAction = context.FullName;
            });
        };
    }

    public BuildingTwinState Twin { get; }

    public ObservableCollection<ChatItemViewModel> Messages { get; } = [];

    public ObservableCollection<ToolCallHistoryEntry> ToolCallHistory { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private string _inputText = "请巡检产线并逐步优化：先查看状态，开启装配区照明和传送带，调度 AGV 到装配区，启动机器人装配，并更新库存";

    [ObservableProperty]
    private string _connectionStatus = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isStreaming;

    [ObservableProperty]
    private string _pendingAction = "待命";

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            return;
        }

        var prompt = InputText.Trim();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Messages.Add(new ChatLineViewModel("你", prompt, isUser: true));
            InputText = string.Empty;
            IsBusy = true;
            IsStreaming = true;
            PendingAction = "思考中…";
        });

        ChatLineViewModel? assistantLine = null;

        try
        {
            await foreach (var update in _host.StreamAsync(prompt).ConfigureAwait(false))
            {
                switch (update.Kind)
                {
                    case KernelStreamingUpdateKind.TextDelta:
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            assistantLine ??= new ChatLineViewModel("助手", string.Empty);
                            if (!Messages.Contains(assistantLine))
                            {
                                Messages.Add(assistantLine);
                            }

                            assistantLine.Append(update.TextDelta ?? string.Empty);
                        });
                        break;

                    case KernelStreamingUpdateKind.ToolCallCompleted:
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (!string.IsNullOrEmpty(update.ToolCallId)
                                && _pendingHistory.TryGetValue(update.ToolCallId, out var entry))
                            {
                                entry.MarkCompleted(update.ToolResult);
                                _pendingHistory.Remove(update.ToolCallId);
                            }

                            PendingAction = $"完成 {update.ToolName}";
                        });
                        break;

                    case KernelStreamingUpdateKind.Completed:
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (assistantLine is null && !string.IsNullOrEmpty(update.FinalText))
                            {
                                Messages.Add(new ChatLineViewModel("助手", update.FinalText));
                            }

                            PendingAction = "待命";
                        });
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Messages.Add(new ChatLineViewModel("错误", ex.Message, isError: true));
                PendingAction = "异常";
            });
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsBusy = false;
                IsStreaming = false;
            });
        }
    }

    private bool CanSend() => !IsBusy;

    [RelayCommand]
    private void Clear()
    {
        Messages.Clear();
        ToolCallHistory.Clear();
        _pendingHistory.Clear();
        _twin.Reset();
        _host.ResetConversation();
        PendingAction = "待命";
    }
}
