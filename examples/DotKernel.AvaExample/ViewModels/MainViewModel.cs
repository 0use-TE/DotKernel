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
    private readonly ToolCallConfirmationFilter _confirmationFilter;
    private readonly Dictionary<string, ToolCallHistoryEntry> _pendingHistory = new(StringComparer.Ordinal);

    public MainViewModel(
        KernelHost host,
        BuildingTwinState twin,
        ToolCallHistoryFilter historyFilter,
        ToolCallConfirmationFilter confirmationFilter,
        string status)
    {
        _host = host;
        _twin = twin;
        _confirmationFilter = confirmationFilter;
        Twin = twin;
        ConnectionStatus = status;
        AutoApproveTools = confirmationFilter.AutoApproveTools;

        historyFilter.Recorded += context =>
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

                if (!AutoApproveTools)
                {
                    entry.MarkAwaitingConfirmation();
                }

                PendingAction = AutoApproveTools ? context.FullName : $"待确认 {context.FullName}";
            });
        };

        confirmationFilter.ConfirmationRequired += context =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                PendingToolName = context.FullName;
                PendingToolArguments = context.RawArgumentsJson ?? "{}";
                HasPendingConfirmation = true;
                PendingAction = $"待确认 {context.FullName}";
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

    [ObservableProperty]
    private bool _autoApproveTools = true;

    [ObservableProperty]
    private bool _hasPendingConfirmation;

    [ObservableProperty]
    private string _pendingToolName = "";

    [ObservableProperty]
    private string _pendingToolArguments = "";

    partial void OnAutoApproveToolsChanged(bool value)
    {
        _confirmationFilter.AutoApproveTools = value;
        if (value)
        {
            HasPendingConfirmation = false;
        }
    }

    [RelayCommand]
    private void ApproveToolCall()
    {
        _confirmationFilter.ApprovePending();
        HasPendingConfirmation = false;
    }

    [RelayCommand]
    private void DenyToolCall()
    {
        _confirmationFilter.DenyPending();
        HasPendingConfirmation = false;
        PendingAction = "已拒绝";
    }

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
                                assistantLine = new ChatLineViewModel("助手", update.FinalText);
                                Messages.Add(assistantLine);
                            }

                            assistantLine?.FinishStreaming();
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
                assistantLine?.FinishStreaming();
                IsBusy = false;
                IsStreaming = false;
            });
        }
    }

    private bool CanSend() => !IsBusy;

    [RelayCommand]
    private void Clear()
    {
        _confirmationFilter.DenyPending();
        Messages.Clear();
        ToolCallHistory.Clear();
        _pendingHistory.Clear();
        _twin.Reset();
        _host.ResetConversation();
        HasPendingConfirmation = false;
        PendingAction = "待命";
    }
}
