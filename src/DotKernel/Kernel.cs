using System.Text.Json;
using Microsoft.Extensions.AI;

namespace DotKernel;

public sealed class Kernel
{
    private IChatClient _chatClient;
    private readonly IReadOnlyDictionary<string, KernelFunctionDescriptor> _functionsByToolName;
    private readonly IReadOnlyDictionary<string, KernelFunctionDescriptor> _functionsByFullName;
    private readonly IReadOnlyDictionary<string, PromptDefinition> _promptsByFullName;
    private readonly IReadOnlyList<KernelFunctionDescriptor> _functions;
    private readonly IReadOnlyList<PromptDefinition> _prompts;
    private readonly IReadOnlyList<KernelPropertyDescriptor> _properties;
    private readonly IReadOnlyDictionary<string, object?> _pluginInstances;
    private readonly FilterPipeline _filterPipeline;
    private readonly IList<AIFunction> _aiFunctions;
    private readonly KernelInvokeOptions _defaults;

    internal Kernel(
        IChatClient chatClient,
        IReadOnlyList<KernelFunctionDescriptor> functions,
        IReadOnlyList<PromptDefinition> prompts,
        IReadOnlyList<KernelPropertyDescriptor> properties,
        IReadOnlyDictionary<string, object?> pluginInstances,
        IReadOnlyList<IKernelFilter> filters,
        IServiceProvider? services,
        KernelInvokeOptions defaults)
    {
        _chatClient = chatClient;
        _functions = functions;
        _prompts = prompts;
        _functionsByToolName = functions.ToDictionary(f => f.ToolName, StringComparer.OrdinalIgnoreCase);
        _functionsByFullName = functions.ToDictionary(f => f.FullName, StringComparer.OrdinalIgnoreCase);
        _promptsByFullName = prompts.ToDictionary(p => p.FullName, StringComparer.OrdinalIgnoreCase);
        _properties = properties;
        _pluginInstances = pluginInstances;
        _filterPipeline = new FilterPipeline(filters);
        Services = services;
        _defaults = defaults;
        _aiFunctions = functions.Select(f => (AIFunction)new KernelAIFunction(f)).ToList();
    }

    /// <summary>Optional DI container for <c>IServiceProvider</c> injection into kernel functions.</summary>
    public IServiceProvider? Services { get; }

    /// <summary>Default invoke options (cloned at build time).</summary>
    public KernelInvokeOptions Defaults => _defaults;

    /// <summary>Registered tool functions.</summary>
    public IReadOnlyList<KernelFunctionDescriptor> Functions => _functions;

    /// <summary>Registered prompt templates.</summary>
    public IReadOnlyList<PromptDefinition> Prompts => _prompts;

    /// <summary>Registered live-context properties.</summary>
    public IReadOnlyList<KernelPropertyDescriptor> Properties => _properties;

    /// <summary>Swap the underlying chat client (e.g. after the user changes API settings).</summary>
    public void SetChatClient(IChatClient chatClient) =>
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));

    /// <summary>Current chat client used for model calls and optional plugin injection.</summary>
    public IChatClient ChatClient => _chatClient;

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
        KernelInvokeOptions? options = null,
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
        return await CompleteAsync(history, ResolveOptions(options), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Invoke a registered function by <c>Plugin.function</c> or tool name (<c>Plugin_function</c>),
    /// without calling the language model. Runs the filter pipeline.
    /// </summary>
    public async Task<string> InvokeFunctionAsync(
        string name,
        IReadOnlyDictionary<string, object?>? arguments = null,
        ChatHistory? history = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolveFunction(name, out var descriptor))
        {
            throw new KernelException($"Function '{name}' is not registered.");
        }

        var toolCall = new FunctionCallContent(
            $"local-{Guid.NewGuid():N}",
            descriptor.ToolName,
            arguments is null ? null : new Dictionary<string, object?>(arguments));

        history ??= new ChatHistory();
        return await InvokeToolCallWithFiltersAsync(toolCall, history, cancellationToken).ConfigureAwait(false);
    }

    public Task<string> InvokeAsync(
        string input,
        ChatHistory? history = null,
        CancellationToken cancellationToken = default) =>
        InvokeAsync(input, history, options: null, cancellationToken);

    public async Task<string> InvokeAsync(
        string input,
        ChatHistory? history,
        KernelInvokeOptions? options,
        CancellationToken cancellationToken = default)
    {
        history ??= new ChatHistory();
        history.AddUserMessage(input);
        var resolved = ResolveOptions(options);
        var rounds = 0;

        while (true)
        {
            if (rounds++ >= resolved.MaxToolCallRounds)
            {
                throw new MaxToolCallRoundsExceededException(resolved.MaxToolCallRounds);
            }

            var response = await GetChatResponseAsync(history, resolved, cancellationToken).ConfigureAwait(false);
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

    public IAsyncEnumerable<KernelStreamingUpdate> InvokeStreamingAsync(
        string input,
        ChatHistory? history = null,
        CancellationToken cancellationToken = default) =>
        InvokeStreamingAsync(input, history, options: null, cancellationToken);

    public async IAsyncEnumerable<KernelStreamingUpdate> InvokeStreamingAsync(
        string input,
        ChatHistory? history,
        KernelInvokeOptions? options,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        history ??= new ChatHistory();
        history.AddUserMessage(input);
        var resolved = ResolveOptions(options);
        var rounds = 0;

        while (true)
        {
            if (rounds++ >= resolved.MaxToolCallRounds)
            {
                throw new MaxToolCallRoundsExceededException(resolved.MaxToolCallRounds);
            }

            var chatOptions = CreateChatOptions(resolved);
            var messages = BuildMessagesWithContext(history, resolved);
            var modelContext = new ModelCallContext
            {
                History = history,
                Messages = messages,
                ChatOptions = chatOptions,
            };

            await _filterPipeline.InvokeBeforeModelCallAsync(modelContext, cancellationToken).ConfigureAwait(false);

            var updates = new List<ChatResponseUpdate>();
            await foreach (var update in _chatClient
                .GetStreamingResponseAsync(modelContext.Messages, modelContext.ChatOptions, cancellationToken)
                .ConfigureAwait(false))
            {
                updates.Add(update);
                if (update.Text is { Length: > 0 } delta)
                {
                    yield return KernelStreamingUpdate.Delta(delta);
                }
            }

            var response = updates.ToChatResponse();
            modelContext.Response = response;
            modelContext.StreamingFinalText = response.Text;
            await _filterPipeline.InvokeAfterModelCallAsync(modelContext, cancellationToken).ConfigureAwait(false);

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

    private KernelInvokeOptions ResolveOptions(KernelInvokeOptions? options)
    {
        if (options is null)
        {
            return _defaults;
        }

        if (options.MaxToolCallRounds < 1)
        {
            throw new KernelException("MaxToolCallRounds must be at least 1.");
        }

        return options;
    }

    private async Task<ChatResponse> GetChatResponseAsync(
        ChatHistory history,
        KernelInvokeOptions options,
        CancellationToken cancellationToken)
    {
        var chatOptions = CreateChatOptions(options);
        var messages = BuildMessagesWithContext(history, options);
        var modelContext = new ModelCallContext
        {
            History = history,
            Messages = messages,
            ChatOptions = chatOptions,
        };

        await _filterPipeline.InvokeBeforeModelCallAsync(modelContext, cancellationToken).ConfigureAwait(false);
        var response = await _chatClient
            .GetResponseAsync(modelContext.Messages, modelContext.ChatOptions, cancellationToken)
            .ConfigureAwait(false);
        modelContext.Response = response;
        await _filterPipeline.InvokeAfterModelCallAsync(modelContext, cancellationToken).ConfigureAwait(false);
        return response;
    }

    private ChatOptions CreateChatOptions(KernelInvokeOptions options)
    {
        var chatOptions = options.ChatOptions is null
            ? new ChatOptions()
            : CloneChatOptions(options.ChatOptions);

        chatOptions.Tools = [.. _aiFunctions];
        return chatOptions;
    }

    private static ChatOptions CloneChatOptions(ChatOptions source) => new()
    {
        AdditionalProperties = source.AdditionalProperties,
        AllowMultipleToolCalls = source.AllowMultipleToolCalls,
        ConversationId = source.ConversationId,
        Instructions = source.Instructions,
        MaxOutputTokens = source.MaxOutputTokens,
        ModelId = source.ModelId,
        RawRepresentationFactory = source.RawRepresentationFactory,
        Temperature = source.Temperature,
        TopP = source.TopP,
        TopK = source.TopK,
        Seed = source.Seed,
        FrequencyPenalty = source.FrequencyPenalty,
        PresencePenalty = source.PresencePenalty,
        ResponseFormat = source.ResponseFormat,
        StopSequences = source.StopSequences is null ? null : [.. source.StopSequences],
        ToolMode = source.ToolMode,
        Tools = source.Tools is null ? null : [.. source.Tools],
    };

    private IList<ChatMessage> BuildMessagesWithContext(ChatHistory history, KernelInvokeOptions options)
    {
        List<ChatMessage>? prefix = null;

        if (options.IncludeSystemPrompts)
        {
            foreach (var prompt in _prompts)
            {
                if (prompt.Role != PromptRole.System)
                {
                    continue;
                }

                var instance = ResolveInstance(prompt.DeclaringType);
                var text = PromptRenderer.Render(prompt, instance, extra: null);
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                prefix ??= [];
                prefix.Add(new ChatMessage(ChatRole.System, text));
            }
        }

        if (options.IncludePropertyContext)
        {
            var context = RenderPropertyContext();
            if (!string.IsNullOrWhiteSpace(context))
            {
                prefix ??= [];
                prefix.Add(new ChatMessage(ChatRole.System, context));
            }
        }

        if (prefix is null || prefix.Count == 0)
        {
            return history.Messages is IList<ChatMessage> list ? list : history.Messages.ToList();
        }

        var messages = new List<ChatMessage>(prefix.Count + history.Messages.Count);
        messages.AddRange(prefix);
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
        if (!_functionsByToolName.TryGetValue(functionName, out var descriptor) &&
            !_functionsByFullName.TryGetValue(functionName, out descriptor))
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
        return ToolResultFormatter.Format(raw);
    }

    private async Task<string> CompleteAsync(
        ChatHistory history,
        KernelInvokeOptions options,
        CancellationToken cancellationToken)
    {
        var response = await GetChatResponseAsync(history, options, cancellationToken).ConfigureAwait(false);
        var text = response.Text ?? string.Empty;
        history.AddAssistantMessage(text);
        return text;
    }

    private bool TryResolveFunction(string name, out KernelFunctionDescriptor descriptor)
    {
        if (_functionsByFullName.TryGetValue(name, out descriptor!))
        {
            return true;
        }

        if (_functionsByToolName.TryGetValue(name, out descriptor!))
        {
            return true;
        }

        descriptor = null!;
        return false;
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
