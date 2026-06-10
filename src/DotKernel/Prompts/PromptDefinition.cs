namespace DotKernel;

public delegate string PromptTemplateAccessor(object? instance);

public sealed class PromptDefinition
{
    public required string PluginName { get; init; }

    public required string PromptName { get; init; }

    public string FullName => $"{PluginName}.{PromptName}";

    public string? Description { get; init; }

    public PromptRole Role { get; init; } = PromptRole.User;

    public PromptTemplateAccessor GetTemplate { get; init; } = static _ => string.Empty;

    public IReadOnlyList<PromptVariableDefinition> Variables { get; init; } = [];

    public Type? DeclaringType { get; init; }
}

public sealed class PromptVariableDefinition
{
    public required string Name { get; init; }

    public string? Description { get; init; }

    public string? Default { get; init; }

    public bool Required { get; init; } = true;

    public Func<object?, object?>? Getter { get; init; }
}
