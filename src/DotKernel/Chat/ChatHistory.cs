using Microsoft.Extensions.AI;

namespace DotKernel;

public sealed class ChatHistory
{
    private readonly List<ChatMessage> _messages = [];

    public IReadOnlyList<ChatMessage> Messages => _messages;

    public void Add(ChatMessage message) => _messages.Add(message);

    public void AddSystemMessage(string content) =>
        _messages.Add(new ChatMessage(ChatRole.System, content));

    public void AddUserMessage(string content) =>
        _messages.Add(new ChatMessage(ChatRole.User, content));

    public void AddAssistantMessage(string content) =>
        _messages.Add(new ChatMessage(ChatRole.Assistant, content));

    public void AddToolResult(string toolCallId, string content) =>
        _messages.Add(new ChatMessage(ChatRole.Tool, [new FunctionResultContent(toolCallId, content)]));

    public ChatHistory Clone()
    {
        var clone = new ChatHistory();
        clone._messages.AddRange(_messages);
        return clone;
    }
}
