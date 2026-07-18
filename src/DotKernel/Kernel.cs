using System.Text.Json;
using Microsoft.Extensions.AI;

namespace DotKernel;

public sealed class Kernel
{
    private IChatClient _chatClient;
    private readonly IReadOnlyDictionary<string, KernelFunctionDescriptor> _functionsByToolName;
    private readonly IReadOnlyDictionary<string, PromptDefinition> _promptsByFullName;
    private readonly IReadOnlyList<KernelPropertyDescriptor> _properties;
    private readonly IReadOnlyDictionary<string, object?> _pluginInstances;
    private readonly FilterPipeline _filterPipeline;
    private readonly IList<AIFunction> _aiFunctions;

    internal Kernel(
        IChatClient chatClient,
        IReadOnlyList<KernelFunctionDescriptor> functions,
        IReadOnlyList<PromptDefinition> prompts,
        IReadOnlyList<KernelPropertyDescriptor> properties,
        IReadOnlyDictionary<string, object?> pluginInstances,
        IReadOnlyList<IKernelFilter> filters)
    {
        _chatClient = chatClient;
        _functionsByToolName = functions.ToDictionary(f => f.ToolName, StringComparer.OrdinalIgnoreCase);
        _promptsByFullName = prompts.ToDictionary(p => p.FullName, StringComparer.OrdinalIgnoreCase);
        _properties = properties;
        _pluginInstances = pluginInstances;
        _filterPipeline = new FilterPipeline(filters);
        _aiFunctions = functions.Select(f => (AIFunction)new KernelAIFunction(f)).ToList();
    }

    /// <summary>Swap the underlying chat client (e.g. after the user changes API settings).</summary>
    public void SetChatClient(IChatClient chatClient) =>
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));

    /// <summary>Read all [KernelProperty] values from registered plugin instances.</summary>
    public IReadOnlyDictionary<string, string?> GetPropertyContext()
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in _properties)
        {
            var instance = ResolveInstance(property.DeclaringType);
            var value = property.Getter(instance);
            result[property.FullName] = value?.ToString();
        }

        return result;
    }

    /// <summary>Render property context as text for prompts / debugging.</summary>
    public string RenderPropertyContext()
    {
        if (_properties.Count == 0)
        {
            return string.Empty;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## Live context");
        foreach (var property in _properties)
        {
            var instance = ResolveInstance(property.DeclaringType);
            var value = property.Getter(instance)?.ToString() ?? "(null)";
            sb.Append("- ").Append(property.FullName);
            if (!string.IsNullOrWhiteSpace(property.Description))
            {
                sb.Append(" (").Append(property.Description).Append(')');
            }

            sb.Append(": ").AppendLine(value);
        }

        return sb.ToString().TrimEnd();
    }

    public string RenderPrompt(string fullName, IReadOnlyDictionary<string, string?>? variables = null, object? instance = null)
    {
        if (!_promptsByFullName.TryGetValue(fullName, out var prompt))
        {
            throw new KernelException($"Prompt '{fullName}' is not registered.");
        }

        instance ??= ResolveInstance(prompt.DeclaringType);
        return PromptRenderer.Render(prompt, instance, variables);
    }

    public string RenderPrompt<T>(string promptName, IReadOnlyDictionary<string, string?>? variables = null, T? instance = default)
        where T : class
    {
        var prompt = _promptsByFullName.Values.FirstOrDefault(
            p => p.DeclaringType == typeof(T) && string.Equals(p.PromptName, promptName, StringComparison.OrdinalIgnoreCase));

        if (prompt is null)
        {
            throw new KernelException($"Prompt '{promptName}' on type '{typeof(T).Name}' is not registered.");
        }

        instance ??= ResolveInstance(typeof(T)) as T;
        return PromptRenderer.Render(prompt, instance, variables);
    }

    public async Task<string> InvokePromptAsync(
        string fullName,
        IReadOnlyDictionary<string, string?>? variables = null,
        object? instance = null,
        ChatHistory? history = null,
        CancellationToken cancellationToken = default)
    {
        if (!_promptsByFullName.TryGetValue(fullName, out var prompt))
        {
            throw new KernelException($"Prompt '{fullName}' is not registered.");
        }

        instance ??= ResolveInstance(prompt.DeclaringType);
        var rendered = PromptRenderer.Render(prompt, instance, variables);
        history ??= new ChatHistory();

        var role = prompt.Role switch
        {
            PromptRole.System => ChatRole.System,
            PromptRole.Assistant => ChatRole.Assistant,
            _ => ChatRole.User,
        };

        history.Add(new ChatMessage(role, rendered));
        return await CompleteAsync(history, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> InvokeAsync(
        string input,
        ChatHistory? history = null,
        CancellationToken cancellationToken = default)
    {
        history ??= new ChatHistory();
        history.AddUserMessage(input);

        while (true)
        {
            var response = await GetChatResponseAsync(history, cancellationToken).ConfigureAwait(false);
            var toolCalls = ExtractToolCalls(response);

            if (response.Text is { Length: > 0 } text && toolCalls.Count == 0)
            {
                history.AddAssistantMessage(text);
                return text;
            }

            if (toolCalls.Count > 0)
            {
                await ProcessToolCallsAsync(toolCalls, response, history, cancellationToken).ConfigureAwait(false);
                continue;
            }

            var fallback = response.Text ?? string.Empty;
            if (fallback.Length > 0)
            {
                history.AddAssistantMessage(fallback);
            }

            return fallback;
        }
    }

    public async IAsyncEnumerable<KernelStreamingUpdate> InvokeStreamingAsync(
        string input,
        ChatHistory? history = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        history ??= new ChatHistory();
        history.AddUserMessage(input);

        while (true)
        {
            var options = new ChatOptions { Tools = [.. _aiFunctions] };
            var updates = new List<ChatResponseUpdate>();

            await foreach (var update in _chatClient
                .GetStreamingResponseAsync(BuildMessagesWithContext(history), options, cancellationToken)
                .ConfigureAwait(false))
            {
                updates.Add(update);
                if (update.Text is { Length: > 0 } delta)
                {
                    yield return KernelStreamingUpdate.Delta(delta);
                }
            }

            var response = updates.ToChatResponse();
            var toolCalls = ExtractToolCalls(response);

            if (toolCalls.Count > 0)
            {
                foreach (var message in response.Messages)
                {
                    history.Add(message);
                }

                foreach (var toolCall in DeduplicateToolCalls(toolCalls))
                {
                    var toolName = toolCall.Name ?? string.Empty;
                    yield return KernelStreamingUpdate.ToolStarted(toolName, toolCall.CallId);

                    var result = await InvokeToolCallWithFiltersAsync(toolCall, history, cancellationToken)
                        .ConfigureAwait(false);
                    history.Add(new ChatMessage(ChatRole.Tool, [new FunctionResultContent(toolCall.CallId, result)]));
                    yield return KernelStreamingUpdate.ToolCompleted(toolName, toolCall.CallId, result);
                }

                continue;
            }

            var text = response.Text ?? string.Empty;
            if (text.Length > 0)
            {
                history.AddAssistantMessage(text);
            }

            yield return KernelStreamingUpdate.Done(text);
            yield break;
        }
    }

    private async Task<ChatResponse> GetChatResponseAsync(ChatHistory history, CancellationToken cancellationToken)
    {
        var options = new ChatOptions { Tools = [.. _aiFunctions] };
        return await _chatClient
            .GetResponseAsync(BuildMessagesWithContext(history), options, cancellationToken)
            .ConfigureAwait(false);
    }

    private IList<ChatMessage> BuildMessagesWithContext(ChatHistory history)
    {
        var context = RenderPropertyContext();
        if (string.IsNullOrWhiteSpace(context))
        {
            return history.Messages is IList<ChatMessage> list ? list : history.Messages.ToList();
        }

        var messages = new List<ChatMessage>(history.Messages.Count + 1)
        {
            new(ChatRole.System, context),
        };
        messages.AddRange(history.Messages);
        return messages;
    }

    private async Task ProcessToolCallsAsync(
        IReadOnlyList<FunctionCallContent> toolCalls,
        ChatResponse response,
        ChatHistory history,
        CancellationToken cancellationToken)
    {
        foreach (var message in response.Messages)
        {
            history.Add(message);
        }

        foreach (var toolCall in DeduplicateToolCalls(toolCalls))
        {
            var result = await InvokeToolCallWithFiltersAsync(toolCall, history, cancellationToken)
                .ConfigureAwait(false);
            history.Add(new ChatMessage(ChatRole.Tool, [new FunctionResultContent(toolCall.CallId, result)]));
        }
    }

    private async Task<string> InvokeToolCallWithFiltersAsync(
        FunctionCallContent toolCall,
        ChatHistory history,
        CancellationToken cancellationToken)
    {
        var functionName = toolCall.Name ?? string.Empty;
        if (!_functionsByToolName.TryGetValue(functionName, out var descriptor))
        {
            return $"Error: unknown function '{functionName}'.";
        }

        var arguments = ParseArguments(toolCall.Arguments);
        var context = new ToolCallContext
        {
            ToolCallId = toolCall.CallId,
            PluginName = descriptor.PluginName,
            FunctionName = descriptor.FunctionName,
            Arguments = arguments,
            RawArgumentsJson = ToolCallArgumentFormatter.FormatRawJson(toolCall.Arguments),
            Descriptor = descriptor,
            History = history,
        };

        var filterResult = await _filterPipeline.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
        context = filterResult.ModifiedContext ?? context;

        switch (filterResult.Action)
        {
            case FilterAction.Skip:
                return context.CustomResult ?? string.Empty;
            case FilterAction.Cancel:
                throw new ToolCallCancelledException(context.FullName);
        }

        var invocation = new FunctionInvocationContext
        {
            Descriptor = descriptor,
            Arguments = context.Arguments,
            PluginInstance = ResolveInstance(descriptor.DeclaringType),
            Kernel = this,
        };

        var raw = await descriptor.Invoker(invocation, cancellationToken).ConfigureAwait(false);
        return raw?.ToString() ?? string.Empty;
    }

    private async Task<string> CompleteAsync(ChatHistory history, CancellationToken cancellationToken)
    {
        var response = await _chatClient
            .GetResponseAsync(BuildMessagesWithContext(history), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var text = response.Text ?? string.Empty;
        history.AddAssistantMessage(text);
        return text;
    }

    private static List<FunctionCallContent> ExtractToolCalls(ChatResponse response)
    {
        var calls = new List<FunctionCallContent>();
        foreach (var message in response.Messages)
        {
            foreach (var content in message.Contents)
            {
                if (content is FunctionCallContent call)
                {
                    calls.Add(call);
                }
            }
        }

        return calls;
    }

    private static IEnumerable<FunctionCallContent> DeduplicateToolCalls(IEnumerable<FunctionCallContent> toolCalls)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var toolCall in toolCalls)
        {
            var id = toolCall.CallId;
            if (string.IsNullOrEmpty(id))
            {
                yield return toolCall;
                continue;
            }

            if (seen.Add(id))
            {
                yield return toolCall;
            }
        }
    }

    private static Dictionary<string, object?> ParseArguments(object? arguments)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (arguments is null)
        {
            return result;
        }

        if (arguments is JsonElement json)
        {
            if (json.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in json.EnumerateObject())
                {
                    result[property.Name] = property.Value;
                }
            }

            return result;
        }

        if (arguments is IReadOnlyDictionary<string, object?> readOnly)
        {
            foreach (var pair in readOnly)
            {
                result[pair.Key] = pair.Value;
            }

            return result;
        }

        if (arguments is IDictionary<string, object?> dict)
        {
            foreach (var pair in dict)
            {
                result[pair.Key] = pair.Value;
            }
        }

        return result;
    }

    private object? ResolveInstance(Type? type)
    {
        if (type is null)
        {
            return null;
        }

        var key = type.FullName ?? type.Name;
        return _pluginInstances.TryGetValue(key, out var instance) ? instance : null;
    }
}
