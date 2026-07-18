using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotKernel;
using DotKernel.AvaExample.Filters;
using DotKernel.AvaExample.Infrastructure;
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
        string status,
        string? apiKey = null,
        string? endpoint = null,
        string? modelId = null)
    {
        _host = host;
        _twin = twin;
        _confirmationFilter = confirmationFilter;
        Twin = twin;
        ConnectionStatus = status;
        AutoApproveTools = confirmationFilter.AutoApproveTools;
        ApiKey = apiKey ?? string.Empty;
        Endpoint = string.IsNullOrWhiteSpace(endpoint) ? "https://api.deepseek.com" : endpoint;
        ModelId = string.IsNullOrWhiteSpace(modelId) ? "deepseek-chat" : modelId;

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

                PendingAction = AutoApproveTools ? context.FullName : $"Awaiting {context.FullName}";
            });
        };

        confirmationFilter.ConfirmationRequired += context =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                PendingToolName = context.FullName;
                PendingToolArguments = context.RawArgumentsJson ?? "{}";
                HasPendingConfirmation = true;
                PendingAction = $"Awaiting {context.FullName}";
            });
        };
    }

    public BuildingTwinState Twin { get; }

    public ObservableCollection<ChatItemViewModel> Messages { get; } = [];

    public ObservableCollection<ToolCallHistoryEntry> ToolCallHistory { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private string _inputText = "Inspect the line and optimize step by step: snapshot, lights and conveyor in assembly, move AGV from storage, start robot assembly, update inventory.";

    [ObservableProperty]
    private string _connectionStatus = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApplyChatSettingsCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isStreaming;

    [ObservableProperty]
    private string _pendingAction = "Idle";

    [ObservableProperty]
    private bool _autoApproveTools = true;

    [ObservableProperty]
    private bool _hasPendingConfirmation;

    [ObservableProperty]
    private string _pendingToolName = "";

    [ObservableProperty]
    private string _pendingToolArguments = "";

    [ObservableProperty]
    private bool _showSettings = true;

    [ObservableProperty]
    private string _apiKey = "";

    [ObservableProperty]
    private string _endpoint = "https://api.deepseek.com";

    [ObservableProperty]
    private string _modelId = "deepseek-chat";

    partial void OnAutoApproveToolsChanged(bool value)
    {
        _confirmationFilter.AutoApproveTools = value;
        if (value)
        {
            HasPendingConfirmation = false;
        }
    }

    [RelayCommand]
    private void ToggleSettings() => ShowSettings = !ShowSettings;

    [RelayCommand(CanExecute = nameof(CanApplySettings))]
    private void ApplyChatSettings()
    {
        try
        {
            var (client, status) = DeepSeekChatClientFactory.Create(ApiKey, Endpoint, ModelId);
            _host.SetChatClient(client);
            ConnectionStatus = status;
            PendingAction = "Chat client updated";
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatLineViewModel("Error", ex.Message, isError: true));
            ConnectionStatus = "Settings error";
        }
    }

    private bool CanApplySettings() => !IsBusy;

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
        PendingAction = "Denied";
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
            Messages.Add(new ChatLineViewModel("You", prompt, isUser: true));
            InputText = string.Empty;
            IsBusy = true;
            IsStreaming = true;
            PendingAction = "Thinking…";
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
                            assistantLine ??= new ChatLineViewModel("Assistant", string.Empty);
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

                            PendingAction = $"Done {update.ToolName}";
                        });
                        break;

                    case KernelStreamingUpdateKind.Completed:
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (assistantLine is null && !string.IsNullOrEmpty(update.FinalText))
                            {
                                assistantLine = new ChatLineViewModel("Assistant", update.FinalText);
                                Messages.Add(assistantLine);
                            }

                            assistantLine?.FinishStreaming();
                            PendingAction = "Idle";
                        });
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Messages.Add(new ChatLineViewModel("Error", ex.Message, isError: true));
                PendingAction = "Error";
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
        PendingAction = "Idle";
    }
}
