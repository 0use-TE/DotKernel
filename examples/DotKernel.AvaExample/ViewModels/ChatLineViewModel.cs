using CommunityToolkit.Mvvm.ComponentModel;

namespace DotKernel.AvaExample.ViewModels;

public partial class ChatLineViewModel : ChatItemViewModel
{
    public ChatLineViewModel(string role, string content, bool isUser = false, bool isError = false)
    {
        Role = role;
        _content = content;
        IsUser = isUser;
        IsError = isError;
        UseMarkdown = !isUser && !isError;
    }

    public string Role { get; }

    public bool IsUser { get; }

    public bool IsError { get; }

    public bool UseMarkdown { get; }

    [ObservableProperty]
    private string _content;

    public void Append(string delta) => Content += delta;
}
